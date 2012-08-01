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

#pragma once

#ifndef __CURL_INSTANCE_DEFINED
#define __CURL_INSTANCE_DEFINED

#include "Logger.h"
#include "LinearBuffer.h"

#include <curl/curl.h>

#define METHOD_CREATE_CURL_WORKER_NAME                                        L"CreateCurlWorker()"
#define METHOD_DESTROY_CURL_WORKER_NAME                                       L"DestroyCurlWorker()"
#define METHOD_CURL_WORKER_NAME                                               L"CurlWorker()"
#define METHOD_CURL_DEBUG_CALLBACK                                            L"CurlDebugCallback()"
#define METHOD_INITIALIZE_NAME                                                L"Initialize()"
#define METHOD_CURL_DEBUG_NAME                                                L"CurlDebug()"
#define METHOD_CURL_RECEIVE_DATA_NAME                                         L"CurlReceiveData()"

#define METHOD_CURL_ERROR_MESSAGE                                             L"%s: %s: %s: %s"

#define CURL_STATE_NONE                                                       0
#define CURL_STATE_CREATED                                                    1
#define CURL_STATE_INITIALIZED                                                2
#define CURL_STATE_RECEIVING_DATA                                             3
#define CURL_STATE_RECEIVED_ALL_DATA                                          4

#define MINIMUM_BUFFER_SIZE                                                   256 * 1024

class CCurlInstance
{
public:
  // initializes a new instance of CCurlInstance class
  // @param logger : logger for logging purposes
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CCurlInstance(CLogger *logger, const wchar_t *url, const wchar_t *protocolName);

  // destructor
  virtual ~CCurlInstance(void);

  // gets CURL handle
  // @return : CURL handle
  virtual CURL *GetCurlHandle(void);

  // gets CURL error code
  // @return : CURL error code
  virtual CURLcode GetErrorCode(void);

  // gets human readable error message
  // @param errorCode : the error code returned by libcurl
  // @return : human readable error message or NULL if error
  const wchar_t *GetCurlErrorMessage(CURLcode errorCode);

  // report libcurl error into log file
  // @param logLevel : the verbosity level of logged message
  // @param protocolName : name of protocol calling ReportCurlErrorMessage()
  // @param functionName : name of function calling ReportCurlErrorMessage()
  // @param message : optional message to log (can be NULL)
  // @param errorCode : the error code returned by libcurl
  virtual void ReportCurlErrorMessage(unsigned int logLevel, const wchar_t *protocolName, const wchar_t *functionName, const wchar_t *message, CURLcode errorCode);

  // initializes CURL instance
  // @return : true if successful, false otherwise
  virtual bool Initialize(void);

  // gets receive data timeout
  // @return : receive data timeout or UINT_MAX if not specified
  virtual unsigned int GetReceiveDataTimeout(void);

  // sets receive data timeout
  // @param timeout : receive data timeout (UINT_MAX if not specified)
  virtual void SetReceivedDataTimeout(unsigned int timeout);

  // sets write callback for CURL
  // @param writeCallback : callback method for writing data received by CURL
  // @param writeData : user specified data supplied to write callback method
  virtual void SetWriteCallback(curl_write_callback writeCallback, void *writeData);

  // starts receiving data
  // @return : true if successful, false otherwise
  virtual bool StartReceivingData(void);

  // gets response code
  // @param : reference to variable which holds response code
  // @return : CURLE_OK if successful, other means error
  virtual CURLcode GetResponseCode(long *responseCode);

  // gets if connection be closed without waiting
  // @return : true if connection be closed without waiting, false otherwise
  virtual bool GetCloseWithoutWaiting(void);

  // gets CURL state
  // @return : one of CURL_STATE values
  virtual unsigned int GetCurlState(void);

  // sets if connection be closed without waiting
  // @param closeWithoutWaiting : true if connection be closed without waiting, false otherwise
  virtual void SetCloseWithoutWaiting(bool closeWithoutWaiting);

  // gets libcurl version
  // caller is responsible for freeing memory
  // @return : libcurl version or NULL if error
  static wchar_t *GetCurlVersion(void);

  // gets url
  // @return : url
  virtual const wchar_t *GetUrl(void);

  // gets receive data buffer
  // @return : receive data buffer or NULL if error
  CLinearBuffer *GetReceiveDataBuffer(void);

protected:
  CURL *curl;
  CLogger *logger;

  // libcurl worker thread
  HANDLE hCurlWorkerThread;
  DWORD dwCurlWorkerThreadId;
  CURLcode curlWorkerErrorCode;
  static DWORD WINAPI CurlWorker(LPVOID lpParam);

  // the stream url
  wchar_t *url;

  // the protocol implementation name (for logging purposes)
  wchar_t *protocolName;

  // holds CURL error message
  wchar_t *curlErrorMessage;

  // creates libcurl worker
  // @return : S_OK if successful
  HRESULT CreateCurlWorker(void);

  // destroys libcurl worker
  // @return : S_OK if successful
  HRESULT DestroyCurlWorker(void);

  // holds receive data timeout
  unsigned int receiveDataTimeout;

  // write callback for CURL
  curl_write_callback writeCallback;

  // user specified data supplied to write callback
  void *writeData;

  // holds internal state
  unsigned int state;

  // specifies if current connection have to be closed without waiting
  bool closeWithoutWaiting;

  // holds received data
  CLinearBuffer *receivedDataBuffer;

  // callback function for receiving data from libcurl
  // its default write callback when not specified other callback
  static size_t CurlReceiveDataCallback(char *buffer, size_t size, size_t nmemb, void *userdata);

  // debug callback of libcurl
  // @param handle : the handle / transfer this concerns
  // @param type : what kind of data
  // @param data : points to the data
  // @param size : size of the data pointed to
  // @param userptr : user defined pointer
  static int CurlDebugCallback(CURL *handle, curl_infotype type, char *data, size_t size, void *userptr);

  // called when CURL debug message arives
  // @param type : CURL message type
  // @param data : received CURL message data
  virtual void CurlDebug(curl_infotype type, const wchar_t *data);

  // process received data
  // @param buffer : buffer with received data
  // @param length : the length of buffer
  // @return : the length of processed data (lower value than length means error)
  virtual size_t CurlReceiveData(const unsigned char *buffer, size_t length);
};

#endif