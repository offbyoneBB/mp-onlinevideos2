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

#ifndef __MPURLSOURCE_HTTP_DEFINED
#define __MPURLSOURCE_HTTP_DEFINED

#include "MPURLSOURCE_HTTP_Exports.h"
#include "Logger.h"
#include "ProtocolInterface.h"
#include "LinearBuffer.h"

#include <curl/curl.h>

#include <WinSock2.h>

// we should get data in twenty seconds
#define HTTP_RECEIVE_DATA_TIMEOUT_DEFAULT                   20000
#define HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT       3

//#define DEFAULT_BUFFER_SIZE                                 32 * 1024
#define OUTPUT_PIN_NAME                                     _T("Output File")

#define CONFIGURATION_SECTION_HTTP                          _T("HTTP")

#define CONFIGURATION_HTTP_RECEIVE_DATA_TIMEOUT             _T("HttpReceiveDataTimeout")
#define CONFIGURATION_HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS _T("HttpOpenConnectionMaximumAttempts")

#define METHOD_CREATE_CURL_WORKER_NAME                      _T("CreateCurlWorker()")
#define METHOD_DESTROY_CURL_WORKER_NAME                     _T("DestroyCurlWorker()")
#define METHOD_CURL_WORKER_NAME                             _T("CurlWorker()")

// returns protocol class instance
PIProtocol CreateProtocolInstance(void);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the MPUrlSource_Http.dll
class MPURLSOURCE_HTTP_API CMPUrlSource_Http : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSource_Http class
  CMPUrlSource_Http(void);

  // destructor
  ~CMPUrlSource_Http(void);

  /* IProtocol interface */
  TCHAR *GetProtocolName(void);
  int Initialize(IOutputStream *filter, CParameterCollection *configuration);
  int ClearSession(void);
  int ParseUrl(const TCHAR *url, const CParameterCollection *parameters);
  int OpenConnection(void);
  bool IsConnected(void);
  void CloseConnection(void);
  void ReceiveData(bool *shouldExit);
  unsigned int GetReceiveDataTimeout(void);
  GUID GetInstanceId(void);
  unsigned int GetOpenConnectionMaximumAttempts(void);
  CStringCollection *GetStreamNames(void);
  HRESULT ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime);
  HRESULT AbortStreamReceive();  
  HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current);
  HRESULT QueryStreamAvailableLength(LONGLONG *available);
  HRESULT QueryRangesSupported(bool *rangesSupported);

protected:
  CLogger logger;

  // source filter that created this instance
  IOutputStream *filter;

  // holds various parameters supplied by TvService
  CParameterCollection *configurationParameters;
  // holds various parameters supplied by TvService when loading file
  CParameterCollection *loadParameters;

  // holds receive data timeout
  unsigned int receiveDataTimeout;

  // holds open connection maximum attempts
  unsigned int openConnetionMaximumAttempts;

  // the lenght of stream
  LONGLONG streamLength;

  // holds if length of stream was set
  bool setLenght;

  // stream time and end stream time
  REFERENCE_TIME streamTime;
  REFERENCE_TIME endStreamTime;

  // mutex for locking access to file, buffer, ...
  HANDLE lockMutex;

  // the stream url
  TCHAR *url;

  CURL *curl;

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

  // callback function for receiving data from libcurl
  static size_t CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);

  // reference to variable that signalize if protocol is requested to exit
  bool shouldExit;
  // internal variable for requests to interrupt transfers
  bool internalExitRequest;
  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;

  // libcurl worker thread
  HANDLE hCurlWorkerThread;
  DWORD dwCurlWorkerThreadId;
  CURLcode curlWorkerErrorCode;
  static DWORD WINAPI CurlWorker(LPVOID lpParam);

  // creates libcurl worker
  // @return : S_OK if successful
  HRESULT CreateCurlWorker(void);

  // destroys libcurl worker
  // @return : S_OK if successful
  HRESULT DestroyCurlWorker(void);
};

#endif
