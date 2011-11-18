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

#include "MPUrlSource_RTMP.h"
#include "Network.h"
#include "Utilities.h"
#include "LockMutex.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#define PROTOCOL_IMPLEMENTATION_NAME                                    _T("CMPUrlSource_Rtmp")

PIProtocol CreateProtocolInstance(CParameterCollection *configuration)
{
  return new CMPUrlSource_Rtmp(configuration);
}

void DestroyProtocolInstance(PIProtocol pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSource_Rtmp *pClass = (CMPUrlSource_Rtmp *)pProtocol;
    delete pClass;
  }
}

CMPUrlSource_Rtmp::CMPUrlSource_Rtmp(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
  
  this->receiveDataTimeout = RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->filter = NULL;
  this->streamLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  this->endStreamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->url = NULL;
  this->internalExitRequest = false;
  this->wholeStreamDownloaded = false;
  this->mainCurlInstance = NULL;
  this->streamDuration = 0;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSource_Rtmp::~CMPUrlSource_Rtmp()
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

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

int CMPUrlSource_Rtmp::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }
 
  this->internalExitRequest = false;
  this->streamLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  this->endStreamTime = 0;
  FREE_MEM(this->url);
  this->wholeStreamDownloaded = false;
  this->receiveDataTimeout = RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->streamDuration = 0;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return STATUS_OK;
}

int CMPUrlSource_Rtmp::Initialize(IOutputStream *filter, CParameterCollection *configuration)
{
  this->filter = filter;
  if (this->filter == NULL)
  {
    return STATUS_ERROR;
  }

  if (this->lockMutex == NULL)
  {
    return STATUS_ERROR;
  }

  if (configuration != NULL)
  {
    this->configurationParameters->Clear();
    this->configurationParameters->Append(configuration);
  }
  this->configurationParameters->LogCollection(this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_RTMP_RECEIVE_DATA_TIMEOUT, true, RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return STATUS_OK;
}

TCHAR *CMPUrlSource_Rtmp::GetProtocolName(void)
{
  return Duplicate(PROTOCOL_NAME);
}

int CMPUrlSource_Rtmp::ParseUrl(const TCHAR *url, const CParameterCollection *parameters)
{
  int result = STATUS_OK;
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
    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'url components'"));
    result = STATUS_ERROR;
  }

  if (result == STATUS_OK)
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    this->logger->Log(LOGGER_INFO, _T("%s: %s: url: %s"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, url);

    if (!InternetCrackUrl(url, 0, 0, urlComponents))
    {
      this->logger->Log(LOGGER_ERROR, _T("%s: %s: InternetCrackUrl() error: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, GetLastError());
      result = STATUS_ERROR;
    }
  }

  if (result == STATUS_OK)
  {
    int length = urlComponents->dwSchemeLength + 1;
    ALLOC_MEM_DEFINE_SET(protocol, TCHAR, length, 0);
    if (protocol == NULL) 
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'protocol'"));
      result = STATUS_ERROR;
    }

    if (result == STATUS_OK)
    {
      _tcsncat_s(protocol, length, urlComponents->lpszScheme, urlComponents->dwSchemeLength);

      bool supportedProtocol = false;
      for (int i = 0; i < TOTAL_SUPPORTED_PROTOCOLS; i++)
      {
        if (_tcsncicmp(urlComponents->lpszScheme, SUPPORTED_PROTOCOLS[i], urlComponents->dwSchemeLength) == 0)
        {
          supportedProtocol = true;
          break;
        }
      }

      if (!supportedProtocol)
      {
        // not supported protocol
        this->logger->Log(LOGGER_INFO, _T("%s: %s: unsupported protocol '%s'"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, protocol);
        result = STATUS_ERROR;
      }
    }
    FREE_MEM(protocol);

    if (result == STATUS_OK)
    {
      this->url = Duplicate(url);
      if (this->url == NULL)
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'url'"));
        result = STATUS_ERROR;
      }
    }
  }

  FREE_MEM(urlComponents);

  this->logger->Log(LOGGER_INFO, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);
  return result;
}

int CMPUrlSource_Rtmp::OpenConnection(void)
{
  int result = (this->url != NULL) ? STATUS_OK : STATUS_ERROR;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  this->wholeStreamDownloaded = false;

  if (result == STATUS_OK)
  {
    this->mainCurlInstance = new CCurlInstance(this->logger, this->url, PROTOCOL_IMPLEMENTATION_NAME);
    result = (this->mainCurlInstance != NULL) ? STATUS_OK : STATUS_ERROR;
  }

  if (result == STATUS_OK)
  {
    this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
    this->mainCurlInstance->SetWriteCallback(CMPUrlSource_Rtmp::CurlReceiveData, this);
    this->mainCurlInstance->SetStartStreamTime(this->streamTime);
    this->mainCurlInstance->SetEndStreamTime(this->endStreamTime);

    this->mainCurlInstance->SetRtmpApp(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_APP, true, RTMP_APP_DEFAULT));
    this->mainCurlInstance->SetRtmpArbitraryData(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_ARBITRARY_DATA, true, NULL));
    this->mainCurlInstance->SetRtmpBuffer(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_BUFFER, true, RTMP_BUFFER_DEFAULT));
    this->mainCurlInstance->SetRtmpFlashVersion(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_FLASHVER, true, RTMP_FLASH_VER_DEFAULT));
    this->mainCurlInstance->SetRtmpJtv(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_JTV, true, RTMP_JTV_DEFAULT));
    this->mainCurlInstance->SetRtmpLive(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_LIVE, true, RTMP_LIVE_DEFAULT));
    this->mainCurlInstance->SetRtmpPageUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PAGE_URL, true, RTMP_PAGE_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpPlaylist(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_PLAYLIST, true, RTMP_PLAYLIST_DEFAULT));
    this->mainCurlInstance->SetRtmpPlayPath(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PLAY_PATH, true, RTMP_PLAY_PATH_DEFAULT));
    this->mainCurlInstance->SetRtmpStart(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_START, true, RTMP_START_DEFAULT));
    this->mainCurlInstance->SetRtmpStop(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_STOP, true, RTMP_STOP_DEFAULT));
    this->mainCurlInstance->SetRtmpSubscribe(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SUBSCRIBE, true, RTMP_SUBSCRIBE_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfAge(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_SWF_AGE, true, RTMP_SWF_AGE_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SWF_URL, true, RTMP_SWF_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfVerify(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_SWF_VERIFY, true, RTMP_SWF_VERIFY_DEFAULT));
    this->mainCurlInstance->SetRtmpTcUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TC_URL, true, RTMP_TC_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpToken(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TOKEN, true, RTMP_TOKEN_DEFAULT));

    result = (this->mainCurlInstance->Initialize()) ? STATUS_OK : STATUS_ERROR;

    if (result == STATUS_OK)
    {
      // all parameters set
      // start receiving data

      result = (this->mainCurlInstance->StartReceivingData()) ? STATUS_OK : STATUS_ERROR;
    }
  }

  if (result == STATUS_ERROR)
  {
    this->CloseConnection();
  }

  this->logger->Log(LOGGER_INFO, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);
  return result;
}

bool CMPUrlSource_Rtmp::IsConnected(void)
{
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded));
}

void CMPUrlSource_Rtmp::CloseConnection(void)
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

void CMPUrlSource_Rtmp::ReceiveData(bool *shouldExit)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  this->shouldExit = *shouldExit;

  if (this->internalExitRequest)
  {
    // there is internal exit request pending == changed timestamp
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
      if (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA)
      {
        // all data received, we're not receiving data

        if (this->mainCurlInstance->GetErrorCode() == CURLE_OK)
        {
          // whole stream downloaded
          this->wholeStreamDownloaded = true;

          // notify filter of length of data (if needed)
          if (!this->setLenght)
          {
            this->streamLength = this->streamTime;
            this->logger->Log(LOGGER_VERBOSE, _T("%s: %s: setting total length: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
            this->filter->SetTotalLength(OUTPUT_PIN_NAME, this->streamLength, false);
            this->setLenght = true;
          }

          // notify filter the we reached end of stream
          this->filter->EndOfStreamReached(OUTPUT_PIN_NAME);
        }

        // connection is no longer needed
        this->CloseConnection();
      }
    }
  }
  else
  {
    this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, _T("connection closed, opening new one"));
    // re-open connection if previous is lost
    if (this->OpenConnection() != STATUS_OK)
    {
      this->CloseConnection();
    }
  }

  this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
}

unsigned int CMPUrlSource_Rtmp::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

GUID CMPUrlSource_Rtmp::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

unsigned int CMPUrlSource_Rtmp::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

CStringCollection *CMPUrlSource_Rtmp::GetStreamNames(void)
{
  CStringCollection *streamNames = new CStringCollection();

  streamNames->Add(Duplicate(OUTPUT_PIN_NAME));

  return streamNames;
}

HRESULT CMPUrlSource_Rtmp::ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME);
  this->logger->Log(LOGGER_VERBOSE, _T("%s: %s: from time: %llu, to time: %llu"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, startTime, endTime);

  HRESULT result = E_FAIL;

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  if ((this->setLenght) && (startTime >= this->streamLength))
  {
    result = E_INVALIDARG;
  }
  else if (this->internalExitRequest)
  {
    // there is pending request exit request
    // set stream time to new value
    this->streamTime = startTime;
    this->endStreamTime = endTime;

    // connection should be reopened automatically
    result = S_OK;
  }
  else
  {
    // only way how to "request" curl to interrupt transfer is set internalExitRequest to true
    this->internalExitRequest = true;

    // set stream time to new value
    this->streamTime = startTime;
    this->endStreamTime = endTime;

    // connection should be reopened automatically
    result = S_OK;
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, result);
  return result;
}

HRESULT CMPUrlSource_Rtmp::AbortStreamReceive()
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  this->logger->Log(LOGGER_VERBOSE, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);

  return S_OK;
}

HRESULT CMPUrlSource_Rtmp::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, current);

  if (result == S_OK)
  {
    *total = (this->streamLength == 0) ? 1 : this->streamLength;
    *current = (this->streamLength == 0) ? 0 : this->streamTime;

    if (!this->setLenght)
    {
      result = VFW_S_ESTIMATED;
    }
  }

  this->logger->Log(LOGGER_DATA, (SUCCEEDED(result)) ? METHOD_END_HRESULT_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME, result);
  return result;
}

HRESULT CMPUrlSource_Rtmp::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, availableLength);

  if (result == S_OK)
  {
    availableLength->SetQueryResult(S_OK);
    if (availableLength->IsFilterConnectedToAnotherPin())
    {
      if (!this->setLenght)
      {
        availableLength->SetAvailableLength(this->streamTime);
      }
      else
      {
        availableLength->SetAvailableLength(this->streamLength);
      }
    }
    else
    {
      availableLength->SetAvailableLength(this->streamTime);
    }
  }

  return result;
}

HRESULT CMPUrlSource_Rtmp::QueryRangesSupported(CRangesSupported *rangesSupported)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, rangesSupported);

  if (result == S_OK)
  {
    // RTMP protocol doesn't have ranges to support
    rangesSupported->SetQueryResult(S_OK);
    rangesSupported->SetRangesSupported(false);

    //if (rangesSupported->IsFilterConnectedToAnotherPin())
    //{
    //  // if we don't have live RTMP session than ranges are supported
    //  bool rtmpLive = this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_LIVE, true, false);

    //  rangesSupported->SetQueryResult(S_OK);
    //  rangesSupported->SetRangesSupported(!rtmpLive);
    //}
    //else
    //{
    //  // we are not connected to another pin, assume that ranges are not supported (it makes connection to another filter more faster)
    //  // in the case on web streams we can assume that we don't need data from end of stream
    //  rangesSupported->SetQueryResult(S_OK);
    //  rangesSupported->SetRangesSupported(false);
    //}
  }

  return result;
}

size_t CMPUrlSource_Rtmp::CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  CMPUrlSource_Rtmp *caller = (CMPUrlSource_Rtmp *)userdata;
  CLockMutex lock(caller->lockMutex, INFINITE);
  unsigned int bytesRead = size * nmemb;

  if (!((caller->shouldExit) || (caller->internalExitRequest)))
  {
    if (!caller->setLenght)
    {
      double streamSize = 0;
      CURLcode errorCode = curl_easy_getinfo(caller->mainCurlInstance->GetCurlHandle(), CURLINFO_CONTENT_LENGTH_DOWNLOAD, &streamSize);
      if ((errorCode == CURLE_OK) && (streamSize > 0))
      {
        LONGLONG total = LONGLONG(streamSize);
        caller->streamLength = total;
        caller->logger->Log(LOGGER_VERBOSE, _T("%s: %s: setting total length: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, total);
        caller->filter->SetTotalLength(OUTPUT_PIN_NAME, total, false);
        caller->setLenght = true;
      }
    }

    if (caller->streamDuration == 0)
    {
      double streamDuration = 0;
      CURLcode errorCode = curl_easy_getinfo(caller->mainCurlInstance->GetCurlHandle(), CURLINFO_RTMP_TOTAL_DURATION, &streamDuration);
      if ((errorCode == CURLE_OK) && (streamDuration > 0))
      {
        caller->streamDuration = streamDuration;
        caller->logger->Log(LOGGER_VERBOSE, _T("%s: %s: setting total duration: %lf"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, streamDuration);
      }
    }

    if (!caller->setLenght)
    {
      if ((caller->streamLength == 0) || (caller->streamTime > (caller->streamLength * 3 / 4)))
      {
        double currentTime = 0;
        CURLcode errorCode = curl_easy_getinfo(caller->mainCurlInstance->GetCurlHandle(), CURLINFO_RTMP_CURRENT_TIME, &currentTime);
        if ((errorCode == CURLE_OK) && (currentTime > 0) && (caller->streamDuration != 0))
        {
          LONGLONG tempLength = static_cast<LONGLONG>(caller->streamTime * caller->streamDuration / currentTime);
          if (tempLength > caller->streamLength)
          {
            caller->streamLength = tempLength;
            caller->logger->Log(LOGGER_VERBOSE, _T("%s: %s: setting total length: %llu"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, caller->streamLength);
            caller->filter->SetTotalLength(OUTPUT_PIN_NAME, caller->streamLength, false);
          }
        }
      }
    }

    if (bytesRead != 0)
    {
      // create media packet
      // set values of media packet
      CMediaPacket *mediaPacket = new CMediaPacket();
      mediaPacket->GetBuffer()->InitializeBuffer(bytesRead);
      mediaPacket->GetBuffer()->AddToBuffer(buffer, bytesRead);

      REFERENCE_TIME timeEnd = caller->streamTime + bytesRead - 1;
      HRESULT result = mediaPacket->SetTime(&caller->streamTime, &timeEnd);
      if (result != S_OK)
      {
        caller->logger->Log(LOGGER_WARNING, _T("%s: %s: stream time not set, error: 0x%08X"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
      }

      caller->filter->PushMediaPacket(OUTPUT_PIN_NAME, mediaPacket);
      caller->streamTime += bytesRead;
    }
  }

  // if returned 0 (or lower value than bytesRead) it cause transfer interruption
  return ((caller->shouldExit) || (caller->internalExitRequest)) ? 0 : (bytesRead);
}