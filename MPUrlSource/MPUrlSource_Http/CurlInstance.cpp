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

#include "StdAfx.h"

#include "CurlInstance.h"
#include "Logger.h"

CCurlInstance::CCurlInstance(CLogger *logger, TCHAR *url, TCHAR *protocolName)
{
  this->logger = logger;
  this->url = Duplicate(url);
  this->protocolName = Duplicate(protocolName);
  this->curl = NULL;
  this->hCurlWorkerThread = NULL;
  this->dwCurlWorkerThreadId = 0;
  this->curlWorkerErrorCode = CURLE_OK;
  this->receiveDataTimeout = UINT_MAX;
  this->writeCallback = NULL;
  this->writeData = NULL;
  this->state = CURL_STATE_CREATED;
}


CCurlInstance::~CCurlInstance(void)
{
  this->DestroyCurlWorker();

  if (this->curl != NULL)
  {
    curl_easy_cleanup(this->curl);
    this->curl = NULL;
  }

  FREE_MEM(this->url);
  FREE_MEM(this->protocolName);
}

CURL *CCurlInstance::GetCurlHandle(void)
{
  return this->curl;
}

CURLcode CCurlInstance::GetErrorCode(void)
{
  return this->curlWorkerErrorCode;
}

bool CCurlInstance::Initialize(void)
{
  bool result = (this->logger != NULL) && (this->url != NULL) && (this->protocolName != NULL);

  if (result)
  {
    this->curl = curl_easy_init();
    result = (this->curl != NULL);

    CURLcode errorCode = CURLE_OK;
    if (this->receiveDataTimeout != UINT_MAX)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_CONNECTTIMEOUT, (long)(this->receiveDataTimeout / 2000));
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting connection timeout"), errorCode);
        result = false;
      }
    }

    errorCode = curl_easy_setopt(this->curl, CURLOPT_FOLLOWLOCATION, 1L);
    if (errorCode != CURLE_OK)
    {
      this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting follow location"), errorCode);
      result = false;
    } 

    if (errorCode == CURLE_OK)
    {
      char *curlUrl = ConvertToMultiByte(this->url);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_URL, curlUrl);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting url"), errorCode);
        result = false;
      }
      FREE_MEM(curlUrl);
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEFUNCTION, this->writeCallback);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting write callback"), errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEDATA, this->writeData);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting write callback data"), errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      TCHAR *range = FormatString((this->endStreamTime <= this->startStreamTime) ? _T("%llu-") : _T("%llu-%llu"), this->startStreamTime, this->endStreamTime);
      this->logger->Log(LOGGER_VERBOSE, _T("%s: %s: requesting range: %s"), this->protocolName, METHOD_OPEN_CONNECTION_NAME, range);
      char *curlRange = ConvertToMultiByte(range);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_RANGE, curlRange);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_OPEN_CONNECTION_NAME, _T("error while setting range"), errorCode);
        result = false;
      }
      FREE_MEM(curlRange);
      FREE_MEM(range);
    }
  }

  if (result)
  {
    this->state = CURL_STATE_INITIALIZED;
  }

  return result;
}

TCHAR *CCurlInstance::GetCurlErrorMessage(CURLcode errorCode)
{
  const char *error = curl_easy_strerror(errorCode);
  TCHAR *result = NULL;
#ifdef _MBCS
  result = ConvertToMultiByteA(error);
#else
  result = ConvertToUnicodeA(error);
#endif

  // there is no need to free error message

  return result;
}

void CCurlInstance::ReportCurlErrorMessage(unsigned int logLevel, const TCHAR *protocolName, const TCHAR *functionName, const TCHAR *message, CURLcode errorCode)
{
  TCHAR *curlError = this->GetCurlErrorMessage(errorCode);

  this->logger->Log(logLevel, METHOD_CURL_ERROR_MESSAGE, protocolName, functionName, (message == NULL) ? _T("libcurl error") : message, curlError);

  FREE_MEM(curlError);
}

HRESULT CCurlInstance::CreateCurlWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, this->protocolName, METHOD_CREATE_CURL_WORKER_NAME);

  // clear curl error code
  this->curlWorkerErrorCode = CURLE_OK;

  this->hCurlWorkerThread = CreateThread( 
    NULL,                                   // default security attributes
    0,                                      // use default stack size  
    &CCurlInstance::CurlWorker,             // thread function name
    this,                                   // argument to thread function 
    0,                                      // use default creation flags 
    &dwCurlWorkerThreadId);                 // returns the thread identifier

  if (this->hCurlWorkerThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger->Log(LOGGER_ERROR, _T("%s: %s: CreateThread() error: 0x%08X"), this->protocolName, METHOD_CREATE_CURL_WORKER_NAME, result);
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, this->protocolName, METHOD_CREATE_CURL_WORKER_NAME, result);
  return result;
}

HRESULT CCurlInstance::DestroyCurlWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME);

  // wait for the receive data worker thread to exit      
  if (this->hCurlWorkerThread != NULL)
  {
    if (WaitForSingleObject(this->hCurlWorkerThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME, _T("thread didn't exit, terminating thread"));
      TerminateThread(this->hCurlWorkerThread, 0);
    }
  }

  this->hCurlWorkerThread = NULL;

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME, result);
  return result;
}

DWORD WINAPI CCurlInstance::CurlWorker(LPVOID lpParam)
{
  CCurlInstance *caller = (CCurlInstance *)lpParam;
  caller->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, caller->protocolName, METHOD_CURL_WORKER_NAME);

  caller->state = CURL_STATE_RECEIVING_DATA;

  // on next line will be stopped processing of code - until something happens
  caller->curlWorkerErrorCode = curl_easy_perform(caller->curl);

  caller->state = CURL_STATE_RECEIVED_ALL_DATA;

  if ((caller->curlWorkerErrorCode != CURLE_OK) && (caller->curlWorkerErrorCode != CURLE_WRITE_ERROR))
  {
    caller->ReportCurlErrorMessage(LOGGER_ERROR, caller->protocolName, METHOD_CURL_WORKER_NAME, _T("error while receiving data"), caller->curlWorkerErrorCode);
  }

  caller->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, caller->protocolName, METHOD_CURL_WORKER_NAME);
  return S_OK;
}

unsigned int CCurlInstance::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

void CCurlInstance::SetReceivedDataTimeout(unsigned int timeout)
{
  this->receiveDataTimeout = timeout;
}

void CCurlInstance::SetWriteCallback(curl_write_callback writeCallback, void *writeData)
{
  this->writeCallback = writeCallback;
  this->writeData = writeData;
}

REFERENCE_TIME CCurlInstance::GetStartStreamTime(void)
{
  return this->startStreamTime;
}

void CCurlInstance::SetStartStreamTime(REFERENCE_TIME startStreamTime)
{
  this->startStreamTime = startStreamTime;
}

REFERENCE_TIME CCurlInstance::GetEndStreamTime(void)
{
  return this->endStreamTime;
}

void CCurlInstance::SetEndStreamTime(REFERENCE_TIME endStreamTime)
{
  this->endStreamTime = endStreamTime;
}

bool CCurlInstance::StartReceivingData(void)
{
  return (this->CreateCurlWorker() == S_OK);
}

CURLcode CCurlInstance::GetResponseCode(long *responseCode)
{
  return curl_easy_getinfo(this->curl, CURLINFO_RESPONSE_CODE, responseCode);
}

unsigned int CCurlInstance::GetCurlState(void)
{
  return this->state;
}