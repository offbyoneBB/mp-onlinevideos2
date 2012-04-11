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

#include "MPUrlSourceSplitter_Mms.h"
#include "Utilities.h"
#include "LockMutex.h"
#include "VersionInfo.h"
#include "..\LAVSplitter\VersionInfo.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#ifdef _DEBUG
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_Mmsd"
#else
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_Mms"
#endif

#ifndef AV_RL16
#   define AV_RL16(x)                           \
    ((((const uint8_t*)(x))[1] << 8) |          \
      ((const uint8_t*)(x))[0])
#endif
#ifndef AV_RL32
#   define AV_RL32(x)                                \
    (((uint32_t)((const uint8_t*)(x))[3] << 24) |    \
               (((const uint8_t*)(x))[2] << 16) |    \
               (((const uint8_t*)(x))[1] <<  8) |    \
                ((const uint8_t*)(x))[0])
#endif
#ifndef AV_RL64
#   define AV_RL64(x)                                   \
    (((uint64_t)((const uint8_t*)(x))[7] << 56) |       \
     ((uint64_t)((const uint8_t*)(x))[6] << 48) |       \
     ((uint64_t)((const uint8_t*)(x))[5] << 40) |       \
     ((uint64_t)((const uint8_t*)(x))[4] << 32) |       \
     ((uint64_t)((const uint8_t*)(x))[3] << 24) |       \
     ((uint64_t)((const uint8_t*)(x))[2] << 16) |       \
     ((uint64_t)((const uint8_t*)(x))[1] <<  8) |       \
      (uint64_t)((const uint8_t*)(x))[0])
#endif


PIProtocol CreateProtocolInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Mms(configuration);
}

void DestroyProtocolInstance(PIProtocol pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Mms *pClass = (CMPUrlSourceSplitter_Mms *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Mms::CMPUrlSourceSplitter_Mms(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_MMS, COMPILE_INFO_MPURLSOURCESPLITTER_MMS);
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);

  version = CCurlInstance::GetCurlVersion();
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);
  
  this->receiveDataTimeout = MMS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->filter = NULL;
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->bytePosition = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->url = NULL;
  this->internalExitRequest = false;
  this->wholeStreamDownloaded = false;
  this->mainCurlInstance = NULL;
  this->supressData = false;
  this->sequenceNumber = 1;
  this->receivingData = false;
  this->mmsContext = NULL;
  this->shouldExit = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Mms::~CMPUrlSourceSplitter_Mms()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }

  if (this->mainCurlInstance != NULL)
  {
    delete this->mainCurlInstance;
    this->mainCurlInstance = NULL;
  }

  delete this->configurationParameters;
  FREE_MEM(this->url);

  if (this->lockMutex != NULL)
  {
    CloseHandle(this->lockMutex);
    this->lockMutex = NULL;
  }

  if (this->mmsContext != NULL)
  {
    delete this->mmsContext;
    this->mmsContext = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

HRESULT CMPUrlSourceSplitter_Mms::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }
 
  this->internalExitRequest = false;
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->bytePosition = 0;
  FREE_MEM(this->url);
  this->wholeStreamDownloaded = false;
  this->receiveDataTimeout = MMS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->sequenceNumber = 1;
  this->receivingData = false;
  this->shouldExit = false;

  if (this->mmsContext != NULL)
  {
    delete this->mmsContext;
    this->mmsContext = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Mms::Initialize(IOutputStream *filter, CParameterCollection *configuration)
{
  this->filter = filter;
  if (this->filter == NULL)
  {
    return E_POINTER;
  }

  if (this->lockMutex == NULL)
  {
    return E_FAIL;
  }

  if (configuration != NULL)
  {
    this->configurationParameters->Clear();
    this->configurationParameters->Append(configuration);
  }
  this->configurationParameters->LogCollection(this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_RECEIVE_DATA_TIMEOUT, true, MMS_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? MMS_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return S_OK;
}

wchar_t *CMPUrlSourceSplitter_Mms::GetProtocolName(void)
{
  return Duplicate(PROTOCOL_NAME);
}

HRESULT CMPUrlSourceSplitter_Mms::ParseUrl(const wchar_t *url, const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  this->ClearSession();
  if (parameters != NULL)
  {
    this->configurationParameters->Clear();
    this->Initialize(this->filter, (CParameterCollection *)parameters);
  }

  ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
  if (urlComponents == NULL)
  {
    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'url components'");
    result = E_OUTOFMEMORY;
  }

  if (result == S_OK)
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    this->logger->Log(LOGGER_INFO, L"%s: %s: url: %s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, url);

    if (!InternetCrackUrl(url, 0, 0, urlComponents))
    {
      this->logger->Log(LOGGER_ERROR, L"%s: %s: InternetCrackUrl() error: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, GetLastError());
      result = E_FAIL;
    }
  }

  if (result == S_OK)
  {
    int length = urlComponents->dwSchemeLength + 1;
    ALLOC_MEM_DEFINE_SET(protocol, wchar_t, length, 0);
    if (protocol == NULL) 
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'protocol'");
      result = E_OUTOFMEMORY;
    }

    if (result == S_OK)
    {
      wcsncat_s(protocol, length, urlComponents->lpszScheme, urlComponents->dwSchemeLength);

      bool supportedProtocol = false;
      for (int i = 0; i < TOTAL_SUPPORTED_PROTOCOLS; i++)
      {
        if (_wcsnicmp(urlComponents->lpszScheme, SUPPORTED_PROTOCOLS[i], urlComponents->dwSchemeLength) == 0)
        {
          supportedProtocol = true;
          break;
        }
      }

      if (!supportedProtocol)
      {
        // not supported protocol
        this->logger->Log(LOGGER_INFO, L"%s: %s: unsupported protocol '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, protocol);
        result = E_FAIL;
      }
    }
    FREE_MEM(protocol);

    if (result == S_OK)
    {
      this->url = ReplaceString(url, L"mms://", L"http://");
      if (this->url == NULL)
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'url'");
        result = E_OUTOFMEMORY;
      }
    }
  }

  FREE_MEM(urlComponents);

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);
  return result;
}

HRESULT CMPUrlSourceSplitter_Mms::OpenConnection(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, this->url);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);

  // open connection and send first command
  {
    // lock access to stream
    CLockMutex lock(this->lockMutex, INFINITE);

    this->wholeStreamDownloaded = false;

    if (SUCCEEDED(result))
    {
      this->mainCurlInstance = new CCurlInstance(this->logger, this->url, PROTOCOL_IMPLEMENTATION_NAME);
      result = (this->mainCurlInstance != NULL) ? S_OK : E_POINTER;
    }

    if (SUCCEEDED(result))
    {
      this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
      this->mainCurlInstance->SetWriteCallback(CMPUrlSourceSplitter_Mms::CurlReceiveData, this);
      this->mainCurlInstance->SetStartStreamTime(this->streamTime);
      this->mainCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_REFERER, true, NULL));
      this->mainCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_USER_AGENT, true, NULL));
      this->mainCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_COOKIE, true, NULL));
      this->mainCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_VERSION, true, HTTP_VERSION_DEFAULT));
      this->mainCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

      result = this->mainCurlInstance->AppendToHeaders(L"Accept: */*") ? S_OK : E_FAIL;
      result = this->mainCurlInstance->AppendToHeaders(USERAGENT) ? result : E_FAIL;
      this->sequenceNumber = 1;
      wchar_t *pragma = FormatString(L"Pragma: no-cache,rate=1.000000,stream-time=0,stream-offset=0:0,request-context=%u,max-duration=0", this->sequenceNumber++);
      result = this->mainCurlInstance->AppendToHeaders(pragma) ? result : E_FAIL;
      FREE_MEM(pragma);
      result = this->mainCurlInstance->AppendToHeaders(CLIENTGUID) ? result : E_FAIL;
      result = this->mainCurlInstance->AppendToHeaders(L"Connection: Close") ? result : E_FAIL;

      if (SUCCEEDED(result))
      {
        result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;
      }

      if (SUCCEEDED(result))
      {
        if (this->mmsContext != NULL)
        {
          delete this->mmsContext;
          this->mmsContext = NULL;
        }

        this->mmsContext = new MMSContext();
        result = (this->mmsContext != NULL) ? S_OK : E_OUTOFMEMORY;
      }

      if (SUCCEEDED(result))
      {
        this->mmsContext->SetTimeout(this->GetReceiveDataTimeout() / 2);

        // all parameters set
        // start receiving data

        result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
      }

      if (SUCCEEDED(result))
      {
        // wait for HTTP status code

        long responseCode = 0;
        while (responseCode == 0)
        {
          CURLcode errorCode = this->mainCurlInstance->GetResponseCode(&responseCode);
          if (errorCode == CURLE_OK)
          {
            if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
            {
              // response code 200 - 299 = OK
              // response code 300 - 399 = redirect (OK)
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
              result = E_FAIL;
            }
          }
          else
          {
            this->mainCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
            result = E_FAIL;
            break;
          }

          if ((responseCode == 0) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
          {
            // we received all data, but no response code
            break;
          }

          // wait some time
          Sleep(1);
        }
      }
    }
  }

  if (SUCCEEDED(result))
  {
    // get MMS header data and parse it
    result = this->GetMmsHeaderData(this->mmsContext, NULL);

    if (FAILED(result))
    {
      this->logger->Log(LOGGER_ERROR, L"%s: %s: get MMS header data failed with error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, result);
    }
    else
    {
      {
        // lock access to stream
        CLockMutex lock(this->lockMutex, INFINITE);

        if (this->mainCurlInstance != NULL)
        {
          this->mainCurlInstance->SetCloseWithoutWaiting(true);
        }
        this->CloseConnection();
      }
    }
  }

  if (SUCCEEDED(result))
  {
    // send play command
    {
      // lock access to stream
      CLockMutex lock(this->lockMutex, INFINITE);

      this->wholeStreamDownloaded = false;

      if (SUCCEEDED(result))
      {
        this->mainCurlInstance = new CCurlInstance(this->logger, this->url, PROTOCOL_IMPLEMENTATION_NAME);
        result = (this->mainCurlInstance != NULL) ? S_OK : E_POINTER;
      }

      if (SUCCEEDED(result))
      {
        this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
        this->mainCurlInstance->SetWriteCallback(CMPUrlSourceSplitter_Mms::CurlReceiveData, this);
        this->mainCurlInstance->SetStartStreamTime(this->streamTime);
        this->mainCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_REFERER, true, NULL));
        this->mainCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_USER_AGENT, true, NULL));
        this->mainCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MMS_COOKIE, true, NULL));
        this->mainCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_VERSION, true, HTTP_VERSION_DEFAULT));
        this->mainCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MMS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

        result = this->mainCurlInstance->AppendToHeaders(L"Accept: */*") ? S_OK : E_FAIL;
        result = this->mainCurlInstance->AppendToHeaders(USERAGENT) ? result : E_FAIL;
        wchar_t *pragma = FormatString(L"Pragma: no-cache,rate=1.000000,request-context=%u", this->sequenceNumber++);
        result = this->mainCurlInstance->AppendToHeaders(pragma) ? result : E_FAIL;
        FREE_MEM(pragma);
        result = this->mainCurlInstance->AppendToHeaders(L"Pragma: xPlayStrm=1") ? result : E_FAIL;
        result = this->mainCurlInstance->AppendToHeaders(CLIENTGUID) ? result : E_FAIL;
        pragma = FormatString(L"Pragma: stream-switch-count=%d", this->mmsContext->GetStreams()->Count());
        result = this->mainCurlInstance->AppendToHeaders(pragma) ? result : E_FAIL;
        FREE_MEM(pragma);

        for (unsigned int i = 0; i < this->mmsContext->GetStreams()->Count(); i++)
        {
          MMSStream *stream = mmsContext->GetStreams()->GetItem(i);
          wchar_t *temp = FormatString(L"%sffff:%d:0 ", (pragma == NULL) ? L"Pragma: stream-switch-entry=" : pragma, stream->GetId());
          FREE_MEM(pragma);
          pragma = temp;
        }
        result = this->mainCurlInstance->AppendToHeaders(pragma) ? result : E_FAIL;
        FREE_MEM(pragma);

        pragma = FormatString(L"Pragma: no-cache,rate=1.000000,stream-time=%u", this->streamTime);
        result = this->mainCurlInstance->AppendToHeaders(pragma) ? result : E_FAIL;
        FREE_MEM(pragma);
        result = this->mainCurlInstance->AppendToHeaders(L"Connection: Close") ? result : E_FAIL;
        
        if (SUCCEEDED(result))
        {
          result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;
        }

        if (SUCCEEDED(result))
        {
          // all parameters set
          // start receiving data

          result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
        }

        if (SUCCEEDED(result))
        {
          // wait for HTTP status code

          long responseCode = 0;
          while (responseCode == 0)
          {
            CURLcode errorCode = this->mainCurlInstance->GetResponseCode(&responseCode);
            if (errorCode == CURLE_OK)
            {
              if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
              {
                // response code 200 - 299 = OK
                // response code 300 - 399 = redirect (OK)
                this->logger->Log(LOGGER_VERBOSE, L"%s: %s: HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
                result = E_FAIL;
              }
            }
            else
            {
              this->mainCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
              result = E_FAIL;
              break;
            }

            if ((responseCode == 0) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
            {
              // we received all data, but no response code
              break;
            }

            // wait some time
            Sleep(1);
          }
        }
      }
    }
  }

  if (SUCCEEDED(result))
  {
    // get MMS header data and parse it
    result = this->GetMmsHeaderData(this->mmsContext, NULL);

    if (FAILED(result))
    {
      this->logger->Log(LOGGER_ERROR, L"%s: %s: get MMS header data failed with error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, result);
    }
    else
    {
      {
        // lock access to stream
        CLockMutex lock(this->lockMutex, INFINITE);

        // set that we are receiving data
        this->receivingData = true;
      }
    }
  }

  if (FAILED(result))
  {
    this->CloseConnection();
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);
  return result;
}

bool CMPUrlSourceSplitter_Mms::IsConnected(void)
{
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded));
}

void CMPUrlSourceSplitter_Mms::CloseConnection(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->mainCurlInstance != NULL)
  {
    delete this->mainCurlInstance;
    this->mainCurlInstance = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);
}

void CMPUrlSourceSplitter_Mms::ReceiveData(bool *shouldExit)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  this->shouldExit = *shouldExit;

  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->internalExitRequest)
  {
    // there is internal exit request pending == changed timestamp
    if (this->mainCurlInstance != NULL)
    {
      this->mainCurlInstance->SetCloseWithoutWaiting(true);
    }

    // close connection
    this->CloseConnection();

    // reopen connection
    // OpenConnection() reset wholeStreamDownloaded
    this->OpenConnection();

    this->internalExitRequest = false;
  }

  if (this->IsConnected())
  {
    if (!this->wholeStreamDownloaded)
    {
      if (this->receivingData)
      {
        if (this->mmsContext->GetAsfHeaderLength() != 0)
        {
          // ASF header was not send to filter

          // create media packet
          // set values of media packet
          CMediaPacket *mediaPacket = new CMediaPacket();
          mediaPacket->GetBuffer()->InitializeBuffer(this->mmsContext->GetAsfHeaderLength());
          mediaPacket->GetBuffer()->AddToBuffer(this->mmsContext->GetAsfHeader(), this->mmsContext->GetAsfHeaderLength());
          mediaPacket->SetStart(this->streamTime);
          mediaPacket->SetEnd(this->streamTime + this->mmsContext->GetAsfHeaderLength() - 1);
          if (FAILED(this->filter->PushMediaPacket(mediaPacket)))
          {
            this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
          }
          this->streamTime += this->mmsContext->GetAsfHeaderLength();
          this->mmsContext->ClearAsfHeader();
        }

        MMSChunk *chunk = new MMSChunk();
        if (chunk != NULL)
        {
          if (SUCCEEDED(this->GetMmsChunk(this->mmsContext, chunk)))
          {
            switch (chunk->GetChunkType())
            {
            case CHUNK_TYPE_END:
              {
                this->mmsContext->SetChunkSequence(0);
                this->logger->Log(LOGGER_INFO, L"%s: %s: stream ended", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
              }
              break;
            case CHUNK_TYPE_STREAM_CHANGE:
              {
                this->receivingData = false;
                this->mmsContext->SetHeaderParsed(false);
                HRESULT result = this->GetMmsHeaderData(this->mmsContext, chunk);
                if (FAILED(result))
                {
                  this->logger->Log(LOGGER_ERROR, L"%s: %s: get MMS header data failed with error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
                  this->CloseConnection();
                }
              }
              break;
            case CHUNK_TYPE_DATA:
              {
                // create media packet
                // set values of media packet
                unsigned int packetLength = this->mmsContext->GetAsfPacketLength();
                if (packetLength < chunk->GetChunkDataLength())
                {
                  // error : ASF packet length is less than parsed chunk
                  this->logger->Log(LOGGER_WARNING, L"%s: %s: parsed chunk is bigger than ASF packet length, chunk length: %d, ASF packet length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, chunk->GetChunkDataLength(), packetLength);
                }
                else
                {
                  CMediaPacket *mediaPacket = new CMediaPacket();
                  mediaPacket->GetBuffer()->InitializeBuffer(packetLength, 0);
                  mediaPacket->GetBuffer()->AddToBuffer(chunk->GetChunkData(), chunk->GetChunkDataLength());
                  mediaPacket->SetStart(this->streamTime);
                  mediaPacket->SetEnd(this->streamTime + packetLength - 1);
                  if (FAILED(this->filter->PushMediaPacket(mediaPacket)))
                  {
                    this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
                  }
                  this->streamTime += packetLength;
                }
              }
              break;
            default:
              // ignore unknown packets
              break;
            }
          }

          delete chunk;
          chunk = NULL;
        }
      }
      
      if (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA)
      {
        // all data received, we're not receiving data

        if (this->mainCurlInstance->GetErrorCode() == CURLE_OK)
        {
          // whole stream downloaded
          this->wholeStreamDownloaded = true;

          if (!this->setLength)
          {
            this->streamLength = this->streamTime;
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
            this->filter->SetTotalLength(this->streamLength, false);
            this->setLength = true;
          }

          // notify filter the we reached end of stream
          // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
          int64_t streamTime = this->streamTime;
          this->streamTime = this->streamLength;
          this->filter->EndOfStreamReached(max(0, streamTime - 1));
        }

        // connection is no longer needed
        this->CloseConnection();
      }
    }
  }
  else
  {
    this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"connection closed, opening new one");
    // re-open connection if previous is lost
    if (this->OpenConnection() != S_OK)
    {
      this->CloseConnection();
    }
  }

  this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
}

unsigned int CMPUrlSourceSplitter_Mms::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

GUID CMPUrlSourceSplitter_Mms::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

unsigned int CMPUrlSourceSplitter_Mms::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

int64_t CMPUrlSourceSplitter_Mms::SeekToPosition(int64_t start, int64_t end)
{
  //this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  //this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from position: %llu, to position: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  //int64_t result = -1;

  //// lock access to stream
  //CLockMutex lock(this->lockMutex, INFINITE);

  //if (start >= this->streamLength)
  //{
  //  result = -2;
  //}
  //else if (this->internalExitRequest)
  //{
  //  // there is pending request exit request
  //  // set stream time to new value
  //  this->streamTime = start;
  //  this->endStreamTime = end;

  //  // connection should be reopened automatically
  //  result = start;
  //}
  //else
  //{
  //  // only way how to "request" curl to interrupt transfer is set internalExitRequest to true
  //  this->internalExitRequest = true;

  //  // set stream time to new value
  //  this->streamTime = start;
  //  this->endStreamTime = end;

  //  // connection should be reopened automatically
  //  result = start;
  //}

  //this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  //return result;

  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu, to time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  return result;
}

HRESULT CMPUrlSourceSplitter_Mms::AbortStreamReceive()
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  CLockMutex lock(this->lockMutex, INFINITE);

  // close connection and set that whole stream downloaded
  this->CloseConnection();
  this->wholeStreamDownloaded = true;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Mms::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, current);

  if (result == S_OK)
  {
    *total = this->streamLength;
    *current = this->streamTime;
  }

  this->logger->Log(LOGGER_DATA, (SUCCEEDED(result)) ? METHOD_END_HRESULT_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME, result);
  return result;
}

HRESULT CMPUrlSourceSplitter_Mms::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, availableLength);

  if (result == S_OK)
  {
    availableLength->SetQueryResult(S_OK);
    availableLength->SetAvailableLength(this->streamLength);
  }

  return result;
}

size_t CMPUrlSourceSplitter_Mms::CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  CMPUrlSourceSplitter_Mms *caller = (CMPUrlSourceSplitter_Mms *)userdata;
  CLockMutex lock(caller->lockMutex, INFINITE);
  unsigned int bytesRead = size * nmemb;

  while ((caller->supressData) && (!caller->shouldExit) && (!caller->internalExitRequest))
  {
    // while we have to supress data and we don't have to exit
    // just wait
    Sleep(10);
  }

  if (!((caller->shouldExit) || (caller->internalExitRequest)))
  {
    long responseCode = 0;
    CURLcode errorCode = caller->mainCurlInstance->GetResponseCode(&responseCode);
    if (errorCode == CURLE_OK)
    {
      if ((responseCode < 200) && (responseCode >= 400))
      {
        // response code 200 - 299 = OK
        // response code 300 - 399 = redirect (OK)
        caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: error response code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
        // return error
        bytesRead = 0;
      }
    }

    if ((responseCode >= 200) && (responseCode < 400))
    {
      if (caller->receivingData)
      {
        if (!caller->setLength)
        {
          double streamSize = 0;
          CURLcode errorCode = curl_easy_getinfo(caller->mainCurlInstance->GetCurlHandle(), CURLINFO_CONTENT_LENGTH_DOWNLOAD, &streamSize);
          if ((errorCode == CURLE_OK) && (streamSize > 0) && (caller->streamTime < streamSize))
          {
            LONGLONG total = LONGLONG(streamSize);
            caller->streamLength = total;
            caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, total);
            caller->filter->SetTotalLength(total, false);
            caller->setLength = true;
          }
          else
          {
            if (caller->streamLength == 0)
            {
              // stream length not set
              // just make guess
              caller->streamLength = LONGLONG(MINIMUM_RECEIVED_DATA_FOR_SPLITTER);
              caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, caller->streamLength);
              caller->filter->SetTotalLength(caller->streamLength, true);
            }
            else if ((caller->streamTime > (caller->streamLength * 3 / 4)))
            {
              // it is time to adjust stream length, we are approaching to end but still we don't know total length
              caller->streamLength = caller->streamTime * 2;
              caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, caller->streamLength);
              caller->filter->SetTotalLength(caller->streamLength, true);
            }
          }
        }
      }

      if (bytesRead != 0)
      {
        unsigned int bufferSize = caller->mmsContext->GetBuffer()->GetBufferSize();
        unsigned int freeSpace = caller->mmsContext->GetBuffer()->GetBufferFreeSpace();
        unsigned int newBufferSize = max(bufferSize * 2, bufferSize + bytesRead);

        if (freeSpace < bytesRead)
        {
          caller->logger->Log(LOGGER_INFO, L"%s: %s: not enough free space in buffer for received data, buffer size: %d, free size: %d, received data: %d, new buffer size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, bufferSize, freeSpace, bytesRead, newBufferSize);
          if (!caller->mmsContext->GetBuffer()->ResizeBuffer(newBufferSize))
          {
            caller->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"resizing of buffer unsuccessful");
            // it indicates error
            bytesRead = 0;
          }
        }

        if (bytesRead != 0)
        {
          caller->mmsContext->GetBuffer()->AddToBuffer(buffer, bytesRead);
        }
      }
    }
  }

  // if returned 0 (or lower value than bytesRead) it cause transfer interruption
  return ((caller->shouldExit) || (caller->internalExitRequest)) ? 0 : (bytesRead);
}

unsigned int CMPUrlSourceSplitter_Mms::GetSeekingCapabilities(void)
{
  return SEEKING_METHOD_TIME;
}

int64_t CMPUrlSourceSplitter_Mms::SeekToTime(int64_t time)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, time);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, result);
  return result;
}

void CMPUrlSourceSplitter_Mms::SetSupressData(bool supressData)
{
  this->supressData = supressData;
}

HRESULT CMPUrlSourceSplitter_Mms::GetMmsChunk(MMSContext *mmsContext, MMSChunk *mmsChunk)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, mmsContext);
  CHECK_POINTER_DEFAULT_HRESULT(result, mmsChunk);

  if (SUCCEEDED(result))
  {
    result = mmsContext->IsValid() ? S_OK : E_INVALIDARG;
    if (FAILED(result))
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_CHUNK_HEADER_NAME, L"MMS context is not valid");
    }
  }

  if (SUCCEEDED(result))
  {
    ALLOC_MEM_DEFINE_SET(chunkHeader, char, CHUNK_HEADER_LENGTH, 0);
    ALLOC_MEM_DEFINE_SET(extHeader, char, EXT_HEADER_LENGTH, 0);

    CHECK_POINTER_HRESULT(result, chunkHeader, S_OK, E_OUTOFMEMORY);
    CHECK_POINTER_HRESULT(result, extHeader, S_OK, E_OUTOFMEMORY);
    result = (mmsContext->GetBuffer()->GetBufferOccupiedSpace() >= CHUNK_HEADER_LENGTH) ? result : HRESULT_FROM_WIN32(ERROR_NO_DATA);

    if (SUCCEEDED(result))
    {
      unsigned int chunkType = 0;
      unsigned int chunkLength = 0;
      unsigned int extHeaderLength = 0;

      mmsContext->GetBuffer()->CopyFromBuffer(chunkHeader, CHUNK_HEADER_LENGTH, 0, 0);

      chunkType = AV_RL16(chunkHeader);
      chunkLength  = AV_RL16(chunkHeader + 2);

      result = (mmsContext->GetBuffer()->GetBufferOccupiedSpace() >= chunkLength) ? result : HRESULT_FROM_WIN32(ERROR_NO_DATA);

      if (SUCCEEDED(result))
      {
        switch (chunkType)
        {
        case CHUNK_TYPE_END:
        case CHUNK_TYPE_STREAM_CHANGE:
          extHeaderLength = 4;
          break;
        case CHUNK_TYPE_ASF_HEADER:
        case CHUNK_TYPE_DATA:
          extHeaderLength = 8;
          break;
        default:
          this->logger->Log(LOGGER_WARNING, L"%s: %s: unknown chunk type: %d, length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_CHUNK_HEADER_NAME, chunkType, chunkLength);
          result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
      }

      if (SUCCEEDED(result))
      {
        mmsContext->GetBuffer()->CopyFromBuffer(extHeader, extHeaderLength, 0, CHUNK_HEADER_LENGTH);

        mmsChunk->SetChunkType(chunkType);
        result = mmsChunk->SetChunkDataLength(chunkLength - extHeaderLength) ? S_OK : E_OUTOFMEMORY;
        result = mmsChunk->SetExtraHeaderLength(extHeaderLength) ? result : E_OUTOFMEMORY;

        if (SUCCEEDED(result))
        {
          mmsContext->GetBuffer()->CopyFromBuffer(mmsChunk->GetChunkData(), mmsChunk->GetChunkDataLength(), 0, CHUNK_HEADER_LENGTH + extHeaderLength);
          mmsContext->GetBuffer()->CopyFromBuffer(mmsChunk->GetExtraHeaderData(), extHeaderLength, 0, CHUNK_HEADER_LENGTH);

          if ((chunkType == CHUNK_TYPE_END) || (chunkType == CHUNK_TYPE_DATA))
          {
            mmsContext->SetChunkSequence(AV_RL32(extHeader));
          }

          mmsContext->GetBuffer()->RemoveFromBufferAndMove(chunkLength + 4);
        }
        else
        {
          mmsChunk->Clear();
        }
      }
    }

    FREE_MEM(chunkHeader);
    FREE_MEM(extHeader);
  }
  return result;
}

HRESULT CMPUrlSourceSplitter_Mms::ParseMmsAsfHeader(MMSContext *mmsContext, MMSChunk *mmsChunk)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, mmsContext);
  CHECK_POINTER_DEFAULT_HRESULT(result, mmsChunk);

  if (FAILED(result))
  {
    return result;
  }

  mmsContext->GetStreams()->Clear();

  if ((mmsChunk->GetChunkDataLength() < (sizeof(ASF_GUID) * 2 + 22)) ||
    (memcmp(mmsChunk->GetChunkData(), ASF_HEADER, sizeof(ASF_GUID))))
  {
    this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupt stream, invalid ASF header, size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME, mmsChunk->GetChunkDataLength());
    return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
  }

  char *start = mmsChunk->GetChunkData();
  char *end = mmsChunk->GetChunkData() + mmsChunk->GetChunkDataLength();

  start += sizeof(ASF_GUID) + 14;
  int flags;
  int stream_id;

  while ((SUCCEEDED(result)) && ((end - start) >= (sizeof(ASF_GUID) + 8)))
  {
    uint64_t chunksize;

    if (!memcmp(start, ASF_DATA_HEADER, sizeof(ASF_GUID)))
    {
      chunksize = 50; // see Reference [2] section 5.1
    }
    else
    {
      chunksize = AV_RL64(start + sizeof(ASF_GUID));
    }

    if (!chunksize || (chunksize > (end - start)))
    {
      this->logger->Log(LOGGER_ERROR, L"%s: %s: header chunk size is invalid: %I64d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME, chunksize);
      return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    if (!memcmp(start, ASF_FILE_HEADER, sizeof(ASF_GUID)))
    {
      /* read packet size */
      if ((end - start) > (sizeof(ASF_GUID) * 2 + 68))
      {
        mmsContext->SetAsfPacketLength(AV_RL32(start + sizeof(ASF_GUID) * 2 + 64));
        this->logger->Log(LOGGER_INFO, L"%s: %s: ASF packet length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME, mmsContext->GetAsfPacketLength());
        if (mmsContext->GetAsfPacketLength() > HEADER_BUFFER_SIZE)
        {
          this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupted stream, too large ASF packet length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME, mmsContext->GetAsfPacketLength());
          return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
      }
    }
    else if (!memcmp(start, ASF_STREAM_HEADER, sizeof(ASF_GUID)))
    {
      flags = AV_RL16(start + sizeof(ASF_GUID) * 3 + 24);
      stream_id = flags & 0x7F;

      if (mmsContext->GetStreams()->Count() < MMS_MAX_STREAMS)
      {
        MMSStream *stream = new MMSStream();
        if (stream == NULL)
        {
          this->logger->Log(LOGGER_ERROR, L"%s: %s: not enough memory to create stream", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
          return E_OUTOFMEMORY;
        }

        stream->SetId(stream_id);
        if (!mmsContext->GetStreams()->Add(stream))
        {
          // cannot add MMS stream to MMS context
          // delete stream and return error
          delete stream;
          stream = NULL;
          this->logger->Log(LOGGER_ERROR, L"%s: %s: cannot add stream to MMS context", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
          return E_FAIL;
        }
        else
        {
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: added stream with id: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME, stream->GetId());
        }
      }
      else
      {
        this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupted stream, too many audio/video streams", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
        return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
      }
    }
    else if (!memcmp(start, ASF_EXT_STREAM_HEADER, sizeof(ASF_GUID)))
    {
      if ((end - start) >= 88)
      {
        int stream_count = AV_RL16(start + 84);
        int ext_len_count = AV_RL16(start + 86);
        uint64_t skip_bytes = 88;

        while (stream_count--)
        {
          if ((end - start) < (skip_bytes + 4))
          {
            this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupted stream, next stream name length is not in the buffer", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
            return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
          }
          skip_bytes += 4 + AV_RL16(start + skip_bytes + 2);
        }

        while (ext_len_count--)
        {
          if ((end - start) < (skip_bytes + 22))
          {
            this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupted stream, next extension system info length is not in the buffer", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
            return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
          }
          skip_bytes += 22 + AV_RL32(start + skip_bytes + 18);
        }

        if ((end - start) < skip_bytes)
        {
          this->logger->Log(LOGGER_ERROR, L"%s: %s: corrupted stream, the last extension system info length is invalid", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_MMS_ASF_HEADER_NAME);
          return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }

        if (chunksize - skip_bytes > 24)
        {
          chunksize = skip_bytes;
        }
      }
    }
    else if (!memcmp(start, ASF_HEAD1_GUID, sizeof(ASF_GUID)))
    {
      chunksize = 46; // see references [2] section 3.4. This should be set 46.
    }

    start += chunksize;
  }

  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Mms::GetMmsHeaderData(MMSContext *mmsContext, MMSChunk *mmsChunk)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, mmsContext);

  if (SUCCEEDED(result))
  {
    result = mmsContext->IsValid() ? S_OK : E_INVALIDARG;
  }

  if (SUCCEEDED(result))
  {
    bool finish = false;
    DWORD startTime = GetTickCount();
    MMSChunk *tempMmsChunk = new MMSChunk(mmsChunk);
    result = (tempMmsChunk != NULL) ? S_OK : E_OUTOFMEMORY;

    while (SUCCEEDED(result) && (!finish))
    {
      if (tempMmsChunk->IsCleared())
      {
        result = this->GetMmsChunk(mmsContext, tempMmsChunk);
      }

      if (SUCCEEDED(result) || (result == HRESULT_FROM_WIN32(ERROR_INVALID_DATA)))
      {
        switch (tempMmsChunk->GetChunkType())
        {
        case CHUNK_TYPE_END:
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: chunk type: CHUNK_TYPE_END, length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME, tempMmsChunk->GetChunkDataLength());
          break;
        case CHUNK_TYPE_STREAM_CHANGE:
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: chunk type: CHUNK_TYPE_STREAM_CHANGE, length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME, tempMmsChunk->GetChunkDataLength());
          break;
        case CHUNK_TYPE_ASF_HEADER:
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: chunk type: CHUNK_TYPE_ASF_HEADER, length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME, tempMmsChunk->GetChunkDataLength());
          break;
        case CHUNK_TYPE_DATA:
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: chunk type: CHUNK_TYPE_DATA, length: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME, tempMmsChunk->GetChunkDataLength());
          break;
        }

        switch(tempMmsChunk->GetChunkType())
        {
        case CHUNK_TYPE_ASF_HEADER:
          // get asf header and stored it
          {
            if (!mmsContext->GetHeaderParsed())
            {
              result = this->ParseMmsAsfHeader(this->mmsContext, tempMmsChunk);

              if (SUCCEEDED(result))
              {
                this->logger->Log(LOGGER_VERBOSE, L"%s: %s: ASF header successfully parsed", PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME);
                mmsContext->SetHeaderParsed(true);
                if (!mmsContext->InitializeAsfHeader(tempMmsChunk->GetChunkDataLength()))
                {
                  result = E_OUTOFMEMORY;
                }
                if (SUCCEEDED(result))
                {
                  memcpy(mmsContext->GetAsfHeader(), tempMmsChunk->GetChunkData(), tempMmsChunk->GetChunkDataLength());
                }
                finish = true;
              }
            }
            else
            {
              finish = true;
            }
          }
          break;

        case CHUNK_TYPE_DATA:
          // read data packet and do padding
          //return read_data_packet(mmsh, len);
          finish = true;
          break;

        default:
          result = S_OK;
          break;
        }
      }

      tempMmsChunk->Clear();

      // test for HRESULT_FROM_WIN32(ERROR_NO_DATA)
      if (result == HRESULT_FROM_WIN32(ERROR_NO_DATA))
      {
        if ((GetTickCount() - startTime) <= mmsContext->GetTimeout())
        {
          // reset error, wait some time
          result = S_OK;
          Sleep(1);
        }
      }
    }

    // free MMS chunk
    if (tempMmsChunk != NULL)
    {
      delete tempMmsChunk;
      tempMmsChunk = NULL;
    }
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_GET_MMS_HEADER_DATA_NAME, result);
  return result;
}