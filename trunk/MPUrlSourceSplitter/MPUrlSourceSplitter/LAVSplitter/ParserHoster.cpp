/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#include "stdafx.h"

#include "ParserHoster.h"

#include <Shlwapi.h>
#include <Shlobj.h>

CParserHoster::CParserHoster(CLogger *logger, CParameterCollection *configuration, IParserOutputStream *parserOutputStream)
  : COutputStreamHoster(logger, configuration, L"ParserHoster", L"mpurlsourcesplitter_parser_*.dll", parserOutputStream)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_PARSER_HOSTER_NAME, METHOD_CONSTRUCTOR_NAME);

  this->parserOutputStream = parserOutputStream;

  this->protocolHoster = new CProtocolHoster(this->logger, this->configuration, this);
  if (this->protocolHoster != NULL)
  {
    this->protocolHoster->LoadPlugins();
  }

  this->receiveDataWorkerShouldExit = false;
  this->parsingPlugin = NULL;
  this->parseMediaPackets = true;
  this->setTotalLengthCalled = false;
  this->endOfStreamReachedCalled = false;

  this->hReceiveDataWorkerThread = NULL;
  this->status = STATUS_NONE;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_PARSER_HOSTER_NAME, METHOD_CONSTRUCTOR_NAME);
}

CParserHoster::~CParserHoster(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_PARSER_HOSTER_NAME, METHOD_DESTRUCTOR_NAME);

  this->StopReceivingData();

  if (this->protocolHoster != NULL)
  {
    this->protocolHoster->RemoveAllPlugins();
    delete this->protocolHoster;
    this->protocolHoster = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_PARSER_HOSTER_NAME, METHOD_DESTRUCTOR_NAME);
}

// hoster methods
PluginImplementation *CParserHoster::AllocatePluginsMemory(unsigned int maxPlugins)
{
  return ALLOC_MEM(ParserImplementation, maxPlugins);
}

PluginImplementation *CParserHoster::GetPluginImplementation(unsigned int position)
{
  if ((this->pluginImplementations != NULL) && (position < this->pluginImplementationsCount))
  {
    return (((ParserImplementation *)this->pluginImplementations) + position);
  }

  return NULL;
}

bool CParserHoster::AppendPluginImplementation(HINSTANCE hLibrary, DESTROYPLUGININSTANCE destroyPluginInstance, PIPlugin plugin)
{
  bool result = __super::AppendPluginImplementation(hLibrary, destroyPluginInstance, plugin);
  if (result)
  {
    ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(this->pluginImplementationsCount - 1);
    implementation->result = ParseResult_Unspecified;
  }
  return result;
}

void CParserHoster::RemovePluginImplementation(void)
{
  __super::RemovePluginImplementation();
}

PluginConfiguration *CParserHoster::GetPluginConfiguration(void)
{
  ALLOC_MEM_DEFINE_SET(pluginConfiguration, ParserPluginConfiguration, 1, 0);
  if (pluginConfiguration != NULL)
  {
    pluginConfiguration->configuration = this->configuration;
  }

  return pluginConfiguration;
}

// IOutputStream interface implementation

HRESULT CParserHoster::SetTotalLength(int64_t total, bool estimate)
{
  if (this->outputStream != NULL)
  {
    if (status == STATUS_RECEIVING_DATA)
    {
      return this->outputStream->SetTotalLength(total, estimate);
    }
    else
    {
      this->total = total;
      this->estimate = estimate;
      this->setTotalLengthCalled = true;
    }

    return S_OK;
  }

  return E_NOT_VALID_STATE;
}

HRESULT CParserHoster::PushMediaPacket(CMediaPacket *mediaPacket)
{
  HRESULT result = E_NOT_VALID_STATE;

  if ((this->parseMediaPackets) && (this->pluginImplementationsCount != 0))
  {
    bool pendingPlugin = false;
    bool pendingPluginsBeforeParsing = false;

    for (unsigned int i = 0; i < this->pluginImplementationsCount; i++)
    {
      ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(i);
      if (implementation->result == ParseResult_Pending)
      {
        pendingPluginsBeforeParsing = true;
        break;
      }
    }

    if (this->parsingPlugin != NULL)
    {
      // is there is plugin which returned ParseResult::Known result
      this->parsingPlugin->ParseMediaPacket(mediaPacket);
      result = S_OK;
    }
    else 
    {
      // send received media packet to parsers
      for (unsigned int i = 0; (i < this->pluginImplementationsCount) && (this->parsingPlugin == NULL); i++)
      {
        ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(i);
        IParserPlugin *plugin = (IParserPlugin *)implementation->pImplementation;

        if ((implementation->result == ParseResult_Unspecified) ||
            (implementation->result == ParseResult_Pending))
        {
          // parse data only in case when parser can process data
          // if parser returned ParseResult::NotKnown result than parser surely 
          // doesn't recognize any pattern in stream

          ParseResult pluginParseResult = plugin->ParseMediaPacket(mediaPacket);
          implementation->result = pluginParseResult;

          switch(pluginParseResult)
          {
          case ParseResult_Unspecified:
            this->logger->Log(LOGGER_WARNING, L"%s: %s: parser '%s' return unspecified result", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, implementation->name);
            break;
          case ParseResult_NotKnown:
            this->logger->Log(LOGGER_INFO, L"%s: %s: parser '%s' doesn't recognize any pattern", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, implementation->name);
            break;
          case ParseResult_Pending:
            this->logger->Log(LOGGER_INFO, L"%s: %s: parser '%s' waits for more data", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, implementation->name);
            pendingPlugin = true;
            break;
          case ParseResult_Known:
            this->logger->Log(LOGGER_INFO, L"%s: %s: parser '%s' recognizes pattern", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, implementation->name);
            this->parsingPlugin = plugin;
            break;
          default:
            this->logger->Log(LOGGER_WARNING, L"%s: %s: parser '%s' return unknown result", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, implementation->name);
            break;
          }
        }
      }
    }

    if ((!pendingPlugin) && (this->parsingPlugin == NULL))
    {
      // all parsers don't recognize any pattern in stream
      // do not parse media packets, just send them directly to filter
      this->parseMediaPackets = false;

      this->status = STATUS_RECEIVING_DATA;

      if (pendingPluginsBeforeParsing)
      {
        // we need to resend any store media packets
        CMediaPacketCollection *mediaPacketsToResend = NULL;
        unsigned int mediaPacketsToResendCount = 0;
        for (unsigned int i = 0; i < this->pluginImplementationsCount; i++)
        {
          ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(i);
          IParserPlugin *plugin = (IParserPlugin *)implementation->pImplementation;

          CMediaPacketCollection *mediaPackets = plugin->GetStoredMediaPackets();
          if (mediaPackets != NULL)
          {
            if (mediaPackets->Count() > mediaPacketsToResendCount)
            {
              mediaPacketsToResendCount = mediaPackets->Count();
              mediaPacketsToResend = mediaPackets;
            }
          }
        }

        if ((mediaPacketsToResend != NULL) && (this->outputStream != NULL))
        {
          for (unsigned int i = 0; i < mediaPacketsToResend->Count(); i++)
          {
            CMediaPacket *mediaPacketToResend = mediaPacketsToResend->GetItem(i);
            result = this->outputStream->PushMediaPacket(mediaPacketToResend);
            if (FAILED(result))
            {
              this->logger->Log(LOGGER_WARNING, L"%s: %s: resending media packet failed: 0x%08X, start: %lld, end: %lld", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, result, mediaPacketToResend->GetStart(), mediaPacketToResend->GetEnd());
            }
          }
        }
      }
      else if (this->outputStream != NULL)
      {
        result = this->outputStream->PushMediaPacket(mediaPacket);
      }
    }
    else if (this->parsingPlugin != NULL)
    {
      // there is plugin, which recognize pattern in stream

      Action action = this->parsingPlugin->GetAction();
      wchar_t *name = this->parsingPlugin->GetName();

      switch (action)
      {
      case Action_Unspecified:
        this->logger->Log(LOGGER_WARNING, L"%s: %s: parser '%s' returns unspecified action", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, name);
        result = E_FAIL;
        break;
      case Action_GetNewConnection:
        this->status = STATUS_NEW_URL_SPECIFIED;
        this->logger->Log(LOGGER_INFO, L"%s: %s: parser '%s' specifies new connection", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, name);
        result = S_OK;
        break;
      default:
        this->logger->Log(LOGGER_WARNING, L"%s: %s: parser '%s' returns unknown action", MODULE_PARSER_HOSTER_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, name);
        result = E_FAIL;
        break;
      }

      FREE_MEM(name);
    }
    else
    {
      // there is pending plugin
      this->status = STATUS_PARSER_PENDING;
      result = S_OK;
    }
  }
  else
  {
    this->status = STATUS_RECEIVING_DATA;

    if (this->outputStream != NULL)
    {
      result = this->outputStream->PushMediaPacket(mediaPacket);
    }
  }

  return result;
}

HRESULT CParserHoster::EndOfStreamReached(int64_t streamPosition)
{
  if (this->outputStream != NULL)
  {
    if (status == STATUS_RECEIVING_DATA)
    {
      return this->outputStream->EndOfStreamReached(streamPosition);
    }
    else
    {
      this->streamPosition = streamPosition;
      this->endOfStreamReachedCalled = true;
    }

    return S_OK;
  }

  return E_NOT_VALID_STATE;
}

// ISimpleProtocol implementation

unsigned int CParserHoster::GetReceiveDataTimeout(void)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->GetReceiveDataTimeout() : UINT_MAX;
}

HRESULT CParserHoster::StartReceivingData(const CParameterCollection *parameters)
{
  HRESULT retval = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(retval, parameters);
  CHECK_POINTER_DEFAULT_HRESULT(retval, this->protocolHoster);

  if (SUCCEEDED(retval))
  {
    CParameterCollection *urlConnection = new CParameterCollection();
    retval = (urlConnection == NULL) ? E_OUTOFMEMORY : S_OK;

    if (SUCCEEDED(retval))
    {
      urlConnection->Append((CParameterCollection *)parameters);
      bool newUrlSpecified = false;

      do
      {
        this->status = STATUS_NONE;
        newUrlSpecified = false;

        this->setTotalLengthCalled = false;
        this->endOfStreamReachedCalled = false;

        // clear all protocol plugins and parse url connection
        this->protocolHoster->ClearSession();
        retval = this->protocolHoster->ParseUrl(urlConnection);

        if (SUCCEEDED(retval))
        {
          for (unsigned int i = 0; i < this->pluginImplementationsCount; i++)
          {
            ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(i);
            IParserPlugin *plugin = (IParserPlugin *)implementation->pImplementation;

            // clear parser session and notify about new url and parameters
            plugin->ClearSession();
            plugin->SetConnectionParameters(urlConnection);
          }
        }

        if (SUCCEEDED(retval))
        {
          // now we have active protocol with loaded url, but still not working
          // create thread for receiving data

          retval = this->CreateReceiveDataWorker();
        }

        if (SUCCEEDED(retval))
        {
          DWORD ticks = GetTickCount();
          DWORD timeout = 0;

          // get receive data timeout for active protocol
          timeout = this->protocolHoster->GetReceiveDataTimeout();
          wchar_t *protocolName = this->protocolHoster->GetName();
          if (protocolName != NULL)
          {
            this->logger->Log(LOGGER_INFO, L"%s: %s: active protocol '%s' timeout: %d (ms)", this->moduleName, METHOD_START_RECEIVING_DATA_NAME, protocolName, timeout);
          }
          else
          {
            this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, this->moduleName, METHOD_START_RECEIVING_DATA_NAME, L"no active protocol");
            retval = E_NOT_VALID_STATE;
          }
          FREE_MEM(protocolName);

          if (SUCCEEDED(retval))
          {
            // wait for receiving data, timeout or exit
            while ((this->status != STATUS_NEW_URL_SPECIFIED) && (this->status != STATUS_RECEIVING_DATA) && (this->status != STATUS_NO_DATA_ERROR) && ((GetTickCount() - ticks) <= timeout) && (!this->receiveDataWorkerShouldExit))
            {
              Sleep(1);
            }

            switch(this->status)
            {
            case STATUS_NONE:
              retval = E_FAIL;
              break;
            case STATUS_NO_DATA_ERROR:
              retval = -1;
              break;
            case STATUS_RECEIVING_DATA:
              retval = S_OK;
              break;
            case STATUS_PARSER_PENDING:
              retval = -2;
              break;
            case STATUS_NEW_URL_SPECIFIED:
              this->DestroyReceiveDataWorker();
              newUrlSpecified = true;
              retval = S_OK;
              break;
            default:
              retval = E_UNEXPECTED;
              break;
            }

            if (FAILED(retval))
            {
              this->DestroyReceiveDataWorker();
            }

            if (this->status == STATUS_NEW_URL_SPECIFIED)
            {
              // known plugin will be cleared in StopReceivingData()
              IParserPlugin *plugin = this->parsingPlugin;
              this->StopReceivingData();
              urlConnection->Clear();
              retval = plugin->GetConnectionParameters(urlConnection);
            }
            else
            {
              // stop cycle
              break;
            }
          }
        }
      } while ((newUrlSpecified) && SUCCEEDED(retval));

      delete urlConnection;
    }
  }

  if (SUCCEEDED(retval))
  {
    // call SetTotalLength() or EndOfStreamReached() if there is need

    if (this->setTotalLengthCalled)
    {
      this->SetTotalLength(this->total, this->estimate);
    }

    if (this->endOfStreamReachedCalled)
    {
      this->EndOfStreamReached(this->streamPosition);
    }
  }

  return retval;
}

HRESULT CParserHoster::StopReceivingData(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, this->moduleName, METHOD_STOP_RECEIVING_DATA_NAME);

  // stop receive data worker
  this->DestroyReceiveDataWorker();

  // stop receiving data
  this->protocolHoster->StopReceivingData();
  this->parsingPlugin = NULL;
  this->parseMediaPackets = true;

  for (unsigned int i = 0; i < this->pluginImplementationsCount; i++)
  {
    ParserImplementation *implementation = (ParserImplementation *)this->GetPluginImplementation(i);
    implementation->result = ParseResult_Unspecified;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, this->moduleName, METHOD_STOP_RECEIVING_DATA_NAME);
  return S_OK;
}

HRESULT CParserHoster::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->QueryStreamProgress(total, current) : E_NOT_VALID_STATE;
}
  
HRESULT CParserHoster::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->QueryStreamAvailableLength(availableLength) : E_NOT_VALID_STATE;
}

HRESULT CParserHoster::ClearSession(void)
{
  // stop receiving data
  this->protocolHoster->StopReceivingData();

  if (this->pluginImplementations != NULL)
  {
    for(unsigned int i = 0; i < this->pluginImplementationsCount; i++)
    {
      ParserImplementation *parserImplementation = (ParserImplementation *)this->GetPluginImplementation(i);
      parserImplementation->result = ParseResult_Unspecified;

      this->logger->Log(LOGGER_INFO, L"%s: %s: reseting parser: %s", this->moduleName, METHOD_CLEAR_SESSION_NAME, parserImplementation->name);

      if (parserImplementation->pImplementation != NULL)
      {
        IParserPlugin *parser = (IParserPlugin *)parserImplementation->pImplementation;
        parser->ClearSession();
      }
    }
  }

  // reset all protocol implementations
  this->protocolHoster->ClearSession();

  this->setTotalLengthCalled = false;
  this->endOfStreamReachedCalled = false;
  return S_OK;
}

// ISeeking interface implementation

unsigned int CParserHoster::GetSeekingCapabilities(void)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->GetSeekingCapabilities() : SEEKING_METHOD_NONE;
}

int64_t CParserHoster::SeekToTime(int64_t time)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->SeekToTime(time) : (-1);
}

int64_t CParserHoster::SeekToPosition(int64_t start, int64_t end)
{
  return (this->protocolHoster != NULL) ? this->protocolHoster->SeekToPosition(start, end) : (-1);
}

void CParserHoster::SetSupressData(bool supressData)
{
  if (this->protocolHoster != NULL)
  {
    this->protocolHoster->SetSupressData(supressData);
  }
}

HRESULT CParserHoster::CreateReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, this->moduleName, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME);

  this->hReceiveDataWorkerThread = CreateThread( 
    NULL,                                   // default security attributes
    0,                                      // use default stack size  
    &CParserHoster::ReceiveDataWorker,      // thread function name
    this,                                   // argument to thread function 
    0,                                      // use default creation flags 
    &dwReceiveDataWorkerThreadId);          // returns the thread identifier

  if (this->hReceiveDataWorkerThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger->Log(LOGGER_ERROR, L"%s: %s: CreateThread() error: 0x%08X", this->moduleName, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  }

  if (result == S_OK)
  {
    if (!SetThreadPriority(::GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger->Log(LOGGER_WARNING, L"%s: %s: cannot set thread priority for main thread, error: %u", this->moduleName, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
    if (!SetThreadPriority(this->hReceiveDataWorkerThread, THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger->Log(LOGGER_WARNING, L"%s: %s: cannot set thread priority for receive data thread, error: %u", this->moduleName, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, this->moduleName, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

HRESULT CParserHoster::DestroyReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, this->moduleName, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME);

  this->receiveDataWorkerShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hReceiveDataWorkerThread != NULL)
  {
    if (WaitForSingleObject(this->hReceiveDataWorkerThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, this->moduleName, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, L"thread didn't exit, terminating thread");
      TerminateThread(this->hReceiveDataWorkerThread, 0);
    }
  }

  this->hReceiveDataWorkerThread = NULL;
  this->receiveDataWorkerShouldExit = false;

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, this->moduleName, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

DWORD WINAPI CParserHoster::ReceiveDataWorker(LPVOID lpParam)
{
  CParserHoster *caller = (CParserHoster *)lpParam;
  caller->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, caller->moduleName, METHOD_RECEIVE_DATA_WORKER_NAME);

  unsigned int attempts = 0;
  bool stopReceivingData = false;

  HRESULT result = S_OK;
  while ((!caller->receiveDataWorkerShouldExit) && (!stopReceivingData))
  {
    Sleep(1);

    if (caller->protocolHoster != NULL)
    {
      unsigned int maximumAttempts = caller->protocolHoster->GetOpenConnectionMaximumAttempts();
      if (maximumAttempts != UINT_MAX)
      {

        // if in active protocol is opened connection than receive data
        // if not than open connection
        if (caller->protocolHoster->IsConnected())
        {
          caller->protocolHoster->ReceiveData(&caller->receiveDataWorkerShouldExit);
        }
        else
        {
          if (attempts < maximumAttempts)
          {
            result = caller->protocolHoster->StartReceivingData(NULL);
            if (SUCCEEDED(result))
            {
              // set attempts to zero
              attempts = 0;
            }
            else
            {
              // increase attempts
              attempts++;
            }
          }
          else
          {
            caller->logger->Log(LOGGER_ERROR, L"%s: %s: maximum attempts of opening connection reached, attempts: %u, maximum attempts: %u", caller->moduleName, METHOD_RECEIVE_DATA_WORKER_NAME, attempts, maximumAttempts);
            caller->status = STATUS_NO_DATA_ERROR;
            stopReceivingData = true;

            if (caller->parserOutputStream->IsDownloading())
            {
              caller->parserOutputStream->FinishDownload(result);
            }
          }
        }
      }
    }
  }

  caller->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, caller->moduleName, METHOD_RECEIVE_DATA_WORKER_NAME);
  return S_OK;
}