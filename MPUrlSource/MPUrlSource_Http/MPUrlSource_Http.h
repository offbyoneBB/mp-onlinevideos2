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

#ifndef __MPURLSOURCE_HTTP_DEFINED
#define __MPURLSOURCE_HTTP_DEFINED

#include "MPURLSOURCE_HTTP_Exports.h"
#include "Logger.h"
#include "ProtocolInterface.h"
#include "LinearBuffer.h"
#include "CurlInstance.h"

#include <curl/curl.h>

#include <WinSock2.h>

// we should get data in twenty seconds
#define HTTP_RECEIVE_DATA_TIMEOUT_DEFAULT                   20000
#define HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT       3

#define OUTPUT_PIN_NAME                                     _T("Output")

#define PROTOCOL_NAME                                       _T("HTTP")

#define TOTAL_SUPPORTED_PROTOCOLS                           1
TCHAR *SUPPORTED_PROTOCOLS[TOTAL_SUPPORTED_PROTOCOLS] = { _T("HTTP") };

// size of buffers used for comparison if ranges are supported or not
#define RANGES_SUPPORTED_BUFFER_SIZE                        256 * 1024

#define RANGES_STATE_UNKNOWN                                0
#define RANGES_STATE_NOT_SUPPORTED                          1
#define RANGES_STATE_PENDING_REQUEST                        2
#define RANGES_STATE_SUPPORTED                              3

#define PARAMETER_NAME_HTTP_RECEIVE_DATA_TIMEOUT                  _T("HttpReceiveDataTimeout")
#define PARAMETER_NAME_HTTP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS      _T("HttpOpenConnectionMaximumAttempts")
#define PARAMETER_NAME_HTTP_REFERER                               _T("HttpReferer")
#define PARAMETER_NAME_HTTP_USER_AGENT                            _T("HttpUserAgent")
#define PARAMETER_NAME_HTTP_COOKIE                                _T("HttpCookie")
#define PARAMETER_NAME_HTTP_VERSION                               _T("HttpVersion")
#define PARAMETER_NAME_HTTP_IGNORE_CONTENT_LENGTH                 _T("HttpIgnoreContentLength")

// returns protocol class instance
PIProtocol CreateProtocolInstance(CParameterCollection *configuration);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the MPUrlSource_Http.dll
class MPURLSOURCE_HTTP_API CMPUrlSource_Http : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSource_Http class
  CMPUrlSource_Http(CParameterCollection *configuration);

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
  HRESULT QueryStreamAvailableLength(CStreamAvailableLength *availableLength);
  HRESULT QueryRangesSupported(CRangesSupported *rangesSupported);

protected:
  CLogger *logger;

  // holds received data from start
  LinearBuffer *receivedDataFromStart;
  // holds received data from specified reange
  LinearBuffer *receivedDataFromRange;
  // holds supported ranges status
  int rangesSupported;
  // holds if received data from start buffer was filled
  bool filledReceivedDataFromStart;
  // holds if received data from range buffer was filled
  bool filledReceivedDataFromRange;

  // source filter that created this instance
  IOutputStream *filter;

  // holds various parameters supplied by caller
  CParameterCollection *configurationParameters;

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

  // main instance of CURL
  CCurlInstance *mainCurlInstance;
  // CURL instance for ranges detection
  CCurlInstance *rangesDetectionCurlInstance;

  // callback function for receiving data from libcurl
  static size_t CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);
  static size_t CurlRangesDetectionReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);

  // reference to variable that signalize if protocol is requested to exit
  bool shouldExit;
  // internal variable for requests to interrupt transfers
  bool internalExitRequest;
  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;

  // compares ranges buffers and set ranges supported state
  void CompareRangesBuffers(void);
};

#endif
