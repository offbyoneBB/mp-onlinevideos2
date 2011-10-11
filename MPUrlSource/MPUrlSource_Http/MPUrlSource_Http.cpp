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

#include "MPUrlSource_HTTP.h"
#include "Network.h"
#include "Utilities.h"
#include "LockMutex.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#define PROTOCOL_IMPLEMENTATION_NAME                                    _T("CMPUrlSource_Http")

#define METHOD_CURL_ERROR_MESSAGE                                       _T("%s: %s: %s: %s")

PIProtocol CreateProtocolInstance(void)
{
  return new CMPUrlSource_Http();
}

void DestroyProtocolInstance(PIProtocol pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSource_Http *pClass = (CMPUrlSource_Http *)pProtocol;
    delete pClass;
  }
}

CMPUrlSource_Http::CMPUrlSource_Http()
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  this->configurationParameters = new CParameterCollection();
  this->loadParameters = new CParameterCollection();
  this->receiveDataTimeout = 0;
  this->openConnetionMaximumAttempts = HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->filter = NULL;
  this->streamLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->url = NULL;
  this->curl = NULL;
  this->internalExitRequest = false;
  this->wholeStreamDownloaded = false;

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSource_Http::~CMPUrlSource_Http()
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }

  if (this->curl != NULL)
  {
    curl_easy_cleanup(this->curl);
    this->curl = NULL;
  }
  
  delete this->configurationParameters;
  delete this->loadParameters;
  FREE_MEM(this->url);

  if (this->lockMutex != NULL)
  {
    CloseHandle(this->lockMutex);
    this->lockMutex = NULL;
  }

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);
}

int CMPUrlSource_Http::ClearSession(void)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }
 
  this->internalExitRequest = false;
  this->loadParameters->Clear();
  this->streamLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  FREE_MEM(this->url);
  this->wholeStreamDownloaded = false;

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return STATUS_OK;
}

int CMPUrlSource_Http::Initialize(IOutputStream *filter, CParameterCollection *configuration)
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

  this->configurationParameters->Clear();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }
  this->configurationParameters->LogCollection(&this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_RECEIVE_DATA_TIMEOUT, true, HTTP_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  // ---

  //this->configurationParameters->Clear();
  //if (configuration != NULL)
  //{
  //  this->configurationParameters->Append(configuration);
  //}
  //this->configurationParameters->LogCollection(&this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  //long iptvBufferSize = this->configurationParameters->GetValueLong(CONFIGURATION_IPTV_BUFFER_SIZE, true, IPTV_BUFFER_SIZE_DEFAULT);
  //long defaultMultiplier = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_INTERNAL_BUFFER_MULTIPLIER, true, HTTP_INTERNAL_BUFFER_MULTIPLIER_DEFAULT);
  //long maxMultiplier = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_INTERNAL_BUFFER_MAX_MULTIPLIER, true, HTTP_INTERNAL_BUFFER_MAX_MULTIPLIER_DEFAULT);
  //this->defaultBufferSize = defaultMultiplier * iptvBufferSize;
  //this->maxBufferSize = maxMultiplier * iptvBufferSize;
  //this->receiveDataTimeout = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_RECEIVE_DATA_TIMEOUT, true, HTTP_RECEIVE_DATA_TIMEOUT_DEFAULT);
  //this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(CONFIGURATION_HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  //this->lockMutex = lockMutex;
  //if (this->lockMutex == NULL)
  //{
  //  return STATUS_ERROR;
  //}

  //if (this->defaultBufferSize > 0)
  //{
  //  this->receiveBuffer = ALLOC_MEM(char, this->defaultBufferSize);
  //  if (this->receiveBuffer == NULL)
  //  {
  //    this->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME, _T("cannot initialize internal buffer"));
  //    this->logger.Log(LOGGER_INFO, METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);
  //    return STATUS_ERROR;
  //  }
  //  this->logger.Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME, _T("internal buffer initialized"));

  //  // initialize internal buffer
  //  this->buffer.InitializeBuffer(this->defaultBufferSize);
  //  this->logger.Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME, _T("internal linear buffer initialized"));
  //}
  //else
  //{
  //  this->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME, _T("not valid size of IPTV buffer"));
  //  this->logger.Log(LOGGER_INFO, METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);
  //  return STATUS_ERROR;
  //}

  // ---

  return STATUS_OK;
}

TCHAR *CMPUrlSource_Http::GetProtocolName(void)
{
  return Duplicate(CONFIGURATION_SECTION_HTTP);
}

int CMPUrlSource_Http::ParseUrl(const TCHAR *url, const CParameterCollection *parameters)
{
  int result = STATUS_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  this->ClearSession();
  this->loadParameters->Append((CParameterCollection *)parameters);
  this->loadParameters->LogCollection(&this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
  if (urlComponents == NULL)
  {
    this->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'url components'"));
    result = STATUS_ERROR;
  }

  if (result == STATUS_OK)
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    this->logger.Log(LOGGER_INFO, _T("%s: %s: url: %s"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, url);

    if (!InternetCrackUrl(url, 0, 0, urlComponents))
    {
      this->logger.Log(LOGGER_ERROR, _T("%s: %s: InternetCrackUrl() error: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, GetLastError());
      result = STATUS_ERROR;
    }
  }

  if (result == STATUS_OK)
  {
    int length = urlComponents->dwSchemeLength + 1;
    ALLOC_MEM_DEFINE_SET(protocol, TCHAR, length, 0);
    if (protocol == NULL) 
    {
      this->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'protocol'"));
      result = STATUS_ERROR;
    }

    if (result == STATUS_OK)
    {
      _tcsncat_s(protocol, length, urlComponents->lpszScheme, urlComponents->dwSchemeLength);

      if (_tcsncicmp(urlComponents->lpszScheme, _T("HTTP"), urlComponents->dwSchemeLength) != 0)
      {
        // not supported protocol
        this->logger.Log(LOGGER_INFO, _T("%s: %s: unsupported protocol '%s'"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, protocol);
        result = STATUS_ERROR;
      }
    }
    FREE_MEM(protocol);

    if (result == STATUS_OK)
    {
      this->url = Duplicate(url);
      if (this->url == NULL)
      {
        this->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'url'"));
        result = STATUS_ERROR;
      }
    }
  }

  FREE_MEM(urlComponents);

  this->logger.Log(LOGGER_INFO, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);
  return result;
}

int CMPUrlSource_Http::OpenConnection(void)
{
  int result = (this->url != NULL) ? STATUS_OK : STATUS_ERROR;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);
  this->wholeStreamDownloaded = false;

  if (result == STATUS_OK)
  {
    this->curl = curl_easy_init();
    result = (this->curl != NULL) ? STATUS_OK : STATUS_ERROR;
  }

  if (result == STATUS_OK)
  {
    CParameterCollection *parameters = new CParameterCollection();
    parameters->Append(this->configurationParameters);
    parameters->Append(this->loadParameters);

    CURLcode errorCode = CURLE_OK;
    errorCode = curl_easy_setopt(this->curl, CURLOPT_CONNECTTIMEOUT, (long)(this->receiveDataTimeout / 2000));
    if (errorCode != CURLE_OK)
    {
      this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting connection timeout"), errorCode);
      result = STATUS_ERROR;
    }

    errorCode = curl_easy_setopt(this->curl, CURLOPT_FOLLOWLOCATION, 1L);
    if (errorCode != CURLE_OK)
    {
      this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting follow location"), errorCode);
      result = STATUS_ERROR;
    } 

    if (errorCode == CURLE_OK)
    {
      char *curlUrl = ConvertToMultiByte(this->url);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_URL, curlUrl);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting url"), errorCode);
        result = STATUS_ERROR;
      }
      FREE_MEM(curlUrl);
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEFUNCTION, CMPUrlSource_Http::CurlReceiveData);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting write callback"), errorCode);
        result = STATUS_ERROR;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEDATA, this);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting write callback data"), errorCode);
        result = STATUS_ERROR;
      }
    }

    if (errorCode == CURLE_OK)
    {
      TCHAR *range = FormatString(_T("%llu-"), this->streamTime);
      char *curlRange = ConvertToMultiByte(range);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_RANGE, curlRange);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, _T("error while setting range"), errorCode);
        result = STATUS_ERROR;
      }
      FREE_MEM(curlRange);
      FREE_MEM(range);
    }

    delete parameters;
  }  

  this->logger.Log(LOGGER_INFO, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);
  return result;
}

bool CMPUrlSource_Http::IsConnected(void)
{
  return ((this->curl != NULL) || (this->wholeStreamDownloaded));
}

void CMPUrlSource_Http::CloseConnection(void)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);

  if (this->curl != NULL)
  {
    curl_easy_cleanup(this->curl);
    this->curl = NULL;
  }

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);
}

void CMPUrlSource_Http::ReceiveData(bool *shouldExit)
{
  this->logger.Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  this->shouldExit = shouldExit;

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
      // on next line will be stopped processing of code - until something happens
      CURLcode errorCode = curl_easy_perform(this->curl);
      if (errorCode == CURLE_OK)
      {
        // whole stream donwloaded
        this->wholeStreamDownloaded = true;
        // connection is no longer needed
        this->CloseConnection();
      }
      else if ((errorCode != CURLE_OK) && (errorCode != CURLE_WRITE_ERROR))
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, _T("error while receiving data"), errorCode);
      }
    }
  }
  else
  {
    this->logger.Log(LOGGER_WARNING,  METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, _T("connection closed, opening new one"));
    // re-open connection if previous is lost
    if (this->OpenConnection() != STATUS_OK)
    {
      this->CloseConnection();
    }
  }

  this->logger.Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
}

unsigned int CMPUrlSource_Http::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

GUID CMPUrlSource_Http::GetInstanceId(void)
{
  return this->logger.loggerInstance;
}

unsigned int CMPUrlSource_Http::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

CStringCollection *CMPUrlSource_Http::GetStreamNames(void)
{
  CStringCollection *streamNames = new CStringCollection();

  streamNames->Add(Duplicate(OUTPUT_PIN_NAME));

  return streamNames;
}

HRESULT CMPUrlSource_Http::ReceiveDataFromTimestamp(REFERENCE_TIME time)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME);
  this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: requested time: %llu"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, time);

  HRESULT result = (this->internalExitRequest) ? S_OK : E_NOT_VALID_STATE;

  if ((this->IsConnected()) && (!this->internalExitRequest))
  {
    {
      // lock access to file
      CLockMutex lock(this->lockMutex, INFINITE);

      if (((time - this->streamTime) < 0) || ((time - this->streamTime) > 1048576))
      {
        // difference between requests are bigger
        // only way how to "request" curl to interrupt transfer is set shouldExit to true
        this->internalExitRequest = true;

        // set stream time to new value
        this->streamTime = time;
      }

      // connection should be reopened automatically
      result = S_OK;
    }
  }

  this->logger.Log(LOGGER_VERBOSE, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, result);
  return result;
}

HRESULT CMPUrlSource_Http::AbortStreamReceive()
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  this->logger.Log(LOGGER_VERBOSE, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);

  return S_OK;
}

HRESULT CMPUrlSource_Http::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, current);

  if (result == S_OK)
  {
    *total = this->streamLength;
    *current = this->streamLength;
  }

  this->logger.Log(LOGGER_VERBOSE, (SUCCEEDED(result)) ? METHOD_END_HRESULT_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME, result);
  return result;
}

TCHAR *CMPUrlSource_Http::GetCurlErrorMessage(CURLcode errorCode)
{
  const char *error = curl_easy_strerror(errorCode);
  TCHAR *result = NULL;
#ifdef _MBCS
  result = ConvertToMultiByteA(error);
#else
  result = ConvertToUnicodeA(error);
#endif

  if (error != NULL)
  {
    curl_free((void *)error);
  }

  return result;
}

void CMPUrlSource_Http::ReportCurlErrorMessage(unsigned int logLevel, const TCHAR *protocolName, const TCHAR *functionName, const TCHAR *message, CURLcode errorCode)
{
  TCHAR *curlError = this->GetCurlErrorMessage(errorCode);

  this->logger.Log(logLevel, METHOD_CURL_ERROR_MESSAGE, protocolName, functionName, (message == NULL) ? _T("libcurl error") : message, curlError);

  FREE_MEM(curlError);
}

size_t CMPUrlSource_Http::CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  CMPUrlSource_Http *caller = (CMPUrlSource_Http *)userdata;
  CLockMutex lock(caller->lockMutex, INFINITE);
  unsigned int bytesRead = size * nmemb;

  if (!((*caller->shouldExit) || (caller->internalExitRequest)))
  {
    double streamSize = 0;
    CURLcode errorCode = curl_easy_getinfo(caller->curl, CURLINFO_CONTENT_LENGTH_DOWNLOAD, &streamSize);
    if ((errorCode == CURLE_OK) && (streamSize > 0) && (!caller->setLenght))
    {
      LONGLONG total = LONGLONG(streamSize);
      caller->streamLength = total;
      caller->logger.Log(LOGGER_VERBOSE, _T("%s: %s: setting total length: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, total);
      caller->filter->SetTotalLength(OUTPUT_PIN_NAME, total, false);
      caller->setLenght = true;
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
        caller->logger.Log(LOGGER_WARNING, _T("%s: %s: stream time not set, error: 0x%08X"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
      }

      caller->streamTime += bytesRead;
      caller->filter->PushMediaPacket(OUTPUT_PIN_NAME, mediaPacket);
    }
  }

  return ((*caller->shouldExit) || (caller->internalExitRequest)) ? 0 : (bytesRead);
}