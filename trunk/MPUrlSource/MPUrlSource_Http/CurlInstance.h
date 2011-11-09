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

#ifndef __CURLINSTANCE_HTTP_DEFINED
#define __CURLINSTANCE_HTTP_DEFINED

#include "MPURLSOURCE_HTTP_Exports.h"
#include "Logger.h"

#include <curl/curl.h>

#define HTTP_VERSION_NONE                                   0
#define HTTP_VERSION_FORCE_HTTP10                           1
#define HTTP_VERSION_FORCE_HTTP11                           2

#define HTTP_VERSION_DEFAULT                                HTTP_VERSION_NONE
#define HTTP_IGNORE_CONTENT_LENGTH_DEFAULT                  false

#define METHOD_CREATE_CURL_WORKER_NAME                      _T("CreateCurlWorker()")
#define METHOD_DESTROY_CURL_WORKER_NAME                     _T("DestroyCurlWorker()")
#define METHOD_CURL_WORKER_NAME                             _T("CurlWorker()")

#define METHOD_CURL_ERROR_MESSAGE                           _T("%s: %s: %s: %s")

#define CURL_STATE_NONE                                     0
#define CURL_STATE_CREATED                                  1
#define CURL_STATE_INITIALIZED                              2
#define CURL_STATE_RECEIVING_DATA                           3
#define CURL_STATE_RECEIVED_ALL_DATA                        4

class MPURLSOURCE_HTTP_API CCurlInstance
{
public:
  // initializes a new instance of CCurlInstance class
  // @param logger : logger for logging purposes
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CCurlInstance(CLogger *logger, TCHAR *url, TCHAR *protocolName);
  ~CCurlInstance(void);

  // gets CURL handle
  // @return : CURL handle
  CURL *GetCurlHandle(void);

  // gets CURL error code
  // @return : CURL error code
  CURLcode GetErrorCode(void);

  // gets human readable error message
  // @param errorCode : the error code returned by libcurl
  // @return : human readable error message or NULL if error
  TCHAR *GetCurlErrorMessage(CURLcode errorCode);

  // report libcurl error into log file
  // @param logLevel : the verbosity level of logged message
  // @param protocolName : name of protocol calling ReportCurlErrorMessage()
  // @param functionName : name of function calling ReportCurlErrorMessage()
  // @param message : optional message to log (can be NULL)
  // @param errorCode : the error code returned by libcurl
  void ReportCurlErrorMessage(unsigned int logLevel, const TCHAR *protocolName, const TCHAR *functionName, const TCHAR *message, CURLcode errorCode);

  // initializes CURL instance
  // @return : true if successful, false otherwise
  bool Initialize(void);

  // gets receive data timeout
  // @return : receive data timeout or UINT_MAX if not specified
  unsigned int GetReceiveDataTimeout(void);

  // sets receive data timeout
  // @param timeout : receive data timeout (UINT_MAX if not specified)
  void SetReceivedDataTimeout(unsigned int timeout);

  // sets write callback for CURL
  // @param writeCallback : callback method for writing data received by CURL
  // @param writeData : user specified data supplied to write callback method
  void SetWriteCallback(curl_write_callback writeCallback, void *writeData);

  // gets start stream time
  // @return : start stream time
  REFERENCE_TIME GetStartStreamTime(void);

  // sets start stream time
  // @param startStreamTime : the start stream time to set
  void SetStartStreamTime(REFERENCE_TIME startStreamTime);

  // gets start stream time
  // @return : start stream time
  REFERENCE_TIME GetEndStreamTime(void);

  // sets end stream time
  // @param endStreamTime : the end stream time to set
  void SetEndStreamTime(REFERENCE_TIME endStreamTime);

  // starts receiving data
  // @return : true if successful, false otherwise
  bool StartReceivingData(void);

  // gets response code
  // @param : reference to variable which holds response code
  // @return : CURLE_OK if successful, other means error
  CURLcode GetResponseCode(long *responseCode);

  // gets CURL state
  // @return : one of CURL_STATE values
  unsigned int GetCurlState(void);

  // sets referer
  // @param referer : the referer to set
  void SetReferer(const TCHAR *referer);

  // sets user agent
  // @param user agent : the user agent to set
  void SetUserAgent(const TCHAR *userAgent);

  // sets cookie
  // @param cookie : the cookie to set
  void SetCookie(const TCHAR *cookie);

  // sets HTTP version
  // @param version : the HTTP version to set
  void SetHttpVersion(int version);

  // sets ignore content length
  // @param ignoreContentLength : the ignore content length to set
  void SetIgnoreContentLength(bool ignoreContentLength);

private:
  CURL *curl;
  CLogger *logger;

  // libcurl worker thread
  HANDLE hCurlWorkerThread;
  DWORD dwCurlWorkerThreadId;
  CURLcode curlWorkerErrorCode;
  static DWORD WINAPI CurlWorker(LPVOID lpParam);

  // start stream time and end stream time
  REFERENCE_TIME startStreamTime;
  REFERENCE_TIME endStreamTime;

  // the stream url
  TCHAR *url;

  // the protocol implementation name (for logging purposes)
  TCHAR *protocolName;

  // referer header in HTTP request
  TCHAR *referer;

  // user agent header in HTTP request
  TCHAR *userAgent;

  // cookie header in HTTP request
  TCHAR *cookie;

  // the HTTP protocol version
  int version;

  // specifies if CURL have to ignore content length
  bool ignoreContentLength;

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
};

#endif