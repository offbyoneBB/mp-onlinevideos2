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
#include "LockMutex.h"

CCurlInstance::CCurlInstance(CLogger *logger, HANDLE mutex, const wchar_t *url, const wchar_t *protocolName, const wchar_t *instanceName)
{
  this->logger = logger;
  this->url = Duplicate(url);
  this->protocolName = FormatString(L"%s: instance '%s'", protocolName, instanceName);
  this->curl = NULL;
  this->hCurlWorkerThread = NULL;
  this->dwCurlWorkerThreadId = 0;
  this->curlWorkerErrorCode = CURLE_OK;
  this->receiveDataTimeout = UINT_MAX;
  this->writeCallback = NULL;
  this->writeData = NULL;
  this->state = CURL_STATE_CREATED;
  this->closeWithoutWaiting = false;
  this->curlErrorMessage = NULL;
  this->mutex = mutex;
  this->startReceivingTicks = 0;
  this->stopReceivingTicks = 0;
  this->totalReceivedBytes = 0;

  this->SetWriteCallback(CCurlInstance::CurlReceiveDataCallback, this);
  this->receivedDataBuffer = new CLinearBuffer();
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
  FREE_MEM(this->curlErrorMessage);
  FREE_MEM_CLASS(this->receivedDataBuffer);
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
  bool result = (this->logger != NULL) && (this->url != NULL) && (this->protocolName != NULL) && (this->receivedDataBuffer != NULL);

  if (result)
  {
    this->curl = curl_easy_init();
    result = (this->curl != NULL);

    CURLcode errorCode = CURLE_OK;
    if (this->receiveDataTimeout != UINT_MAX)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_CONNECTTIMEOUT, (long)(this->receiveDataTimeout / 1000));
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting connection timeout", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      char *curlUrl = ConvertToMultiByte(this->url);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_URL, curlUrl);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting url", errorCode);
        result = false;
      }
      FREE_MEM(curlUrl);
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEFUNCTION, this->writeCallback);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting write callback", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_WRITEDATA, this->writeData);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting write callback data", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_DEBUGFUNCTION, CCurlInstance::CurlDebugCallback);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting debug callback", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_DEBUGDATA, this);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting debug callback data", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_VERBOSE, 1L);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting verbose level", errorCode);
        result = false;
      }
    }
  }

  if (result)
  {
    result = this->receivedDataBuffer->InitializeBuffer(MINIMUM_BUFFER_SIZE);
  }

  this->state = (result) ? CURL_STATE_INITIALIZED : CURL_STATE_CREATED;
  return result;
}

const wchar_t *CCurlInstance::GetCurlErrorMessage(CURLcode errorCode)
{
  FREE_MEM(this->curlErrorMessage);
  const char *error = curl_easy_strerror(errorCode);
  this->curlErrorMessage = ConvertToUnicodeA(error);

  return this->curlErrorMessage;
}

void CCurlInstance::ReportCurlErrorMessage(unsigned int logLevel, const wchar_t *protocolName, const wchar_t *functionName, const wchar_t *message, CURLcode errorCode)
{
  this->logger->Log(logLevel, METHOD_CURL_ERROR_MESSAGE, protocolName, functionName, (message == NULL) ? L"libcurl error" : message, this->GetCurlErrorMessage(errorCode));
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
    this->logger->Log(LOGGER_ERROR, L"%s: %s: CreateThread() error: 0x%08X", this->protocolName, METHOD_CREATE_CURL_WORKER_NAME, result);
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
    if (WaitForSingleObject(this->hCurlWorkerThread, this->closeWithoutWaiting ? 1 : 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME, L"thread didn't exit, terminating thread");
      TerminateThread(this->hCurlWorkerThread, 0);
    }
  }

  if (this->stopReceivingTicks == 0)
  {
    this->stopReceivingTicks = GetTickCount();
  }
  this->hCurlWorkerThread = NULL;

  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: start: %u, end: %u, received bytes: %lld", this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME, this->startReceivingTicks, this->stopReceivingTicks, this->totalReceivedBytes);
  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, this->protocolName, METHOD_DESTROY_CURL_WORKER_NAME, result);
  return result;
}

DWORD WINAPI CCurlInstance::CurlWorker(LPVOID lpParam)
{
  CCurlInstance *caller = (CCurlInstance *)lpParam;
  caller->logger->Log(LOGGER_INFO, L"%s: %s: Start, url: '%s'", caller->protocolName, METHOD_CURL_WORKER_NAME, caller->GetUrl());
  caller->startReceivingTicks = GetTickCount();

  // on next line will be stopped processing of code - until something happens
  caller->curlWorkerErrorCode = curl_easy_perform(caller->curl);

  if (caller->stopReceivingTicks == 0)
  {
    caller->stopReceivingTicks = GetTickCount();
  }

  caller->state = CURL_STATE_RECEIVED_ALL_DATA;

  if ((caller->curlWorkerErrorCode != CURLE_OK) && (caller->curlWorkerErrorCode != CURLE_WRITE_ERROR))
  {
    caller->ReportCurlErrorMessage(LOGGER_ERROR, caller->protocolName, METHOD_CURL_WORKER_NAME, L"error while receiving data", caller->curlWorkerErrorCode);
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

size_t CCurlInstance::CurlReceiveDataCallback(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  CCurlInstance *caller = (CCurlInstance *)userdata;

  caller->state = CURL_STATE_RECEIVING_DATA;

  return caller->CurlReceiveData((unsigned char *)buffer, (size_t)(size * nmemb));
}

size_t CCurlInstance::CurlReceiveData(const unsigned char *buffer, size_t length)
{
  if (length != 0)
  {
    // lock access to receive data buffer
    // if mutex is NULL then access to received data buffer is not locked
    CLockMutex lock(this->mutex, INFINITE);

    this->totalReceivedBytes += length;

    unsigned int bufferSize = this->receivedDataBuffer->GetBufferSize();
    unsigned int freeSpace = this->receivedDataBuffer->GetBufferFreeSpace();
    unsigned int newBufferSize = max(bufferSize * 2, bufferSize + length);

    if (freeSpace < length)
    {
      this->logger->Log(LOGGER_INFO, L"%s: %s: not enough free space in buffer for received data, buffer size: %d, free size: %d, received data: %d, new buffer size: %d", this->protocolName, METHOD_CURL_RECEIVE_DATA_NAME, bufferSize, freeSpace, length, newBufferSize);
      if (!this->receivedDataBuffer->ResizeBuffer(newBufferSize))
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, this->protocolName, METHOD_CURL_RECEIVE_DATA_NAME, L"resizing of buffer unsuccessful");
        // it indicates error
        length = 0;
      }
    }

    if (length != 0)
    {
      this->receivedDataBuffer->AddToBuffer(buffer, length);              
    }
  }

  return length;
}

int CCurlInstance::CurlDebugCallback(CURL *handle, curl_infotype type, char *data, size_t size, void *userptr)
{
  CCurlInstance *caller = (CCurlInstance *)userptr;

  // warning: data ARE NOT terminated with null character !!
  if (size > 0)
  {
    size_t length = size + 1;
    ALLOC_MEM_DEFINE_SET(tempData, char, length, 0);
    if (tempData != NULL)
    {
      memcpy(tempData, data, size);

      // now convert data to used character set
      wchar_t *curlData = ConvertToUnicodeA(tempData);

      if (curlData != NULL)
      {
        // we have converted and null terminated data
        caller->CurlDebug(type, curlData);
      }

      FREE_MEM(curlData);
    }
    FREE_MEM(tempData);
  }

  return 0;
}

bool CCurlInstance::GetCloseWithoutWaiting(void)
{
  return this->closeWithoutWaiting;
}

void CCurlInstance::SetCloseWithoutWaiting(bool closeWithoutWaiting)
{
  this->closeWithoutWaiting = closeWithoutWaiting;
}

wchar_t *CCurlInstance::GetCurlVersion(void)
{
  char *curlVersion = curl_version();

  return ConvertToUnicodeA(curlVersion);
}

void CCurlInstance::CurlDebug(curl_infotype type, const wchar_t *data)
{
}

const wchar_t *CCurlInstance::GetUrl(void)
{
  return this->url;
}

CLinearBuffer *CCurlInstance::GetReceiveDataBuffer(void)
{
  return this->receivedDataBuffer;
}