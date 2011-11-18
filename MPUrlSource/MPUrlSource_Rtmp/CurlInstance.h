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

#ifndef __CURLINSTANCE_RTMP_DEFINED
#define __CURLINSTANCE_RTMP_DEFINED

#include "MPURLSOURCE_RTMP_Exports.h"
#include "Logger.h"

#include <curl/curl.h>
#include <librtmp/rtmp.h>

#define METHOD_CREATE_CURL_WORKER_NAME                      _T("CreateCurlWorker()")
#define METHOD_DESTROY_CURL_WORKER_NAME                     _T("DestroyCurlWorker()")
#define METHOD_CURL_WORKER_NAME                             _T("CurlWorker()")

#define METHOD_CURL_ERROR_MESSAGE                           _T("%s: %s: %s: %s")

#define CURL_STATE_NONE                                     0
#define CURL_STATE_CREATED                                  1
#define CURL_STATE_INITIALIZED                              2
#define CURL_STATE_RECEIVING_DATA                           3
#define CURL_STATE_RECEIVED_ALL_DATA                        4

// we should get data in twenty seconds
#define RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT                   20000
#define RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT       3

// define default values for RTMP protocol

#define RTMP_APP_DEFAULT                                    NULL
#define RTMP_TC_URL_DEFAULT                                 NULL
#define RTMP_PAGE_URL_DEFAULT                               NULL
#define RTMP_SWF_URL_DEFAULT                                NULL
#define RTMP_FLASH_VER_DEFAULT                              NULL
#define RTMP_ARBITRARY_DATA_DEFAULT                         NULL
#define RTMP_PLAY_PATH_DEFAULT                              NULL
#define RTMP_PLAYLIST_DEFAULT                               false
#define RTMP_LIVE_DEFAULT                                   false
#define RTMP_SUBSCRIBE_DEFAULT                              NULL
#define RTMP_START_DEFAULT                                  UINT_MAX
#define RTMP_STOP_DEFAULT                                   UINT_MAX
#define RTMP_BUFFER_DEFAULT                                 30000
#define RTMP_TOKEN_DEFAULT                                  NULL
#define RTMP_JTV_DEFAULT                                    NULL
#define RTMP_SWF_VERIFY_DEFAULT                             true
#define RTMP_SWF_AGE_DEFAULT                                0

// define tokens for librtmp

/* CONNECTION PARAMETERS */

#define RTMP_TOKEN_APP                                      _T("app")
#define RTMP_TOKEN_TC_URL                                   _T("tcUrl")
#define RTMP_TOKEN_PAGE_URL                                 _T("pageUrl")
#define RTMP_TOKEN_SWF_URL                                  _T("swfUrl")
#define RTMP_TOKEN_FLASHVER                                 _T("flashVer")

// arbitrary data are passed as they are = they MUST be escaped
// so no token for abitrary data is needed

/* SESSION PARAMETERS */

#define RTMP_TOKEN_PLAY_PATH                                _T("playpath")
#define RTMP_TOKEN_PLAYLIST                                 _T("playlist")
#define RTMP_TOKEN_LIVE                                     _T("live")
#define RTMP_TOKEN_SUBSCRIBE                                _T("subscribe")
#define RTMP_TOKEN_START                                    _T("start")
#define RTMP_TOKEN_STOP                                     _T("stop")
#define RTMP_TOKEN_BUFFER                                   _T("buffer")
#define RTMP_TOKEN_RECEIVE_DATA_TIMEOUT                     _T("timeout")

 /* SECURITY PARAMETERS */

#define RTMP_TOKEN_TOKEN                                    _T("token")
#define RTMP_TOKEN_JTV                                      _T("jtv")
#define RTMP_TOKEN_SWF_VERIFY                               _T("swfVfy")
#define RTMP_TOKEN_SWF_AGE                                  _T("swfAge")

// Special characters in values may need to be escaped to prevent misinterpretation by the option parser.
// The escape encoding uses a backslash followed by two hexadecimal digits representing the ASCII value of the character.
// E.g., spaces must be escaped as \20 and backslashes must be escaped as \5c.

class MPURLSOURCE_RTMP_API CCurlInstance
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

  // gets end stream time
  // @return : end stream time
  REFERENCE_TIME GetEndStreamTime(void);

  // sets end stream time
  // @param endStreamTime : the end stream time to set
  void SetEndStreamTime(REFERENCE_TIME endStreamTime);

  // starts receiving data
  // @return : true if successful, false otherwise
  bool StartReceivingData(void);

  // gets CURL state
  // @return : one of CURL_STATE values
  unsigned int GetCurlState(void);

  // RTMP protocol specific variables setters
  void SetRtmpApp(const TCHAR *rtmpApp);
  void SetRtmpTcUrl(const TCHAR *rtmpTcUrl);
  void SetRtmpPageUrl(const TCHAR *rtmpPageUrl);
  void SetRtmpSwfUrl(const TCHAR *rtmpSwfUrl);
  void SetRtmpFlashVersion(const TCHAR *rtmpFlashVersion);
  void SetRtmpArbitraryData(const TCHAR *rtmpArbitraryData);
  void SetRtmpPlayPath(const TCHAR *rtmpPlayPath);
  void SetRtmpPlaylist(bool rtmpPlaylist);
  void SetRtmpLive(bool rtmpLive);
  void SetRtmpSubscribe(const TCHAR *rtmpSubscribe);
  void SetRtmpStart(unsigned int rtmpStart);
  void SetRtmpStop(unsigned int rtmpStop);
  void SetRtmpBuffer(unsigned int rtmpBuffer);
  void SetRtmpToken(const TCHAR *rtmpToken);
  void SetRtmpJtv(const TCHAR *rtmpJtv);
  void SetRtmpSwfVerify(bool rtmpSwfVerify);
  void SetRtmpSwfAge(unsigned int rtmpSwfAge);

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

  // RTMP protocol specific variables

  // name of application to connect to on the RTMP server
  // if not NULL than overrides the app in the RTMP URL
  TCHAR *rtmpApp;

  // URL of the target stream
  // if not NULL than overrides the tcUrl in the RTMP URL
  TCHAR *rtmpTcUrl;

  // URL of the web page in which the media was embedded
  // if not NULL than value is sent
  TCHAR *rtmpPageUrl;

  // URL of the SWF player for the media
  // if not NULL than value is sent
  TCHAR *rtmpSwfUrl;

  // version of the Flash plugin used to run the SWF player
  // if not NULL than overrides the default flash version "LNX 10,0,32,18"
  TCHAR *rtmpFlashVersion;

  // if not NULL, than append arbitrary AMF data to the Connect message
  TCHAR *rtmpArbitraryData;

  // timeout the session after num of milliseconds without receiving any data from the server
  // if not set (UINT_MAX) then default value of 120 seconds is used
  unsigned int rtmpReceiveDataTimeout;

  // if not NULL than overrides the playpath parsed from the RTMP URL
  TCHAR *rtmpPlayPath;

  // if the value is true than issue a set_playlist command before sending the play command
  // the playlist will just contain the current playpath
  // if the value is false than the set_playlist command will not be sent
  // the default is RTMP_PLAYLIST_DEFAULT
  bool rtmpPlaylist;

  // specify that the media is a live stream
  // no resuming or seeking in live streams is possible
  bool rtmpLive;

  // name of live stream to subscribe to
  // defaults to playpath, if not NULL than value is sent
  TCHAR *rtmpSubscribe;

  // start at num seconds into the stream
  // not valid for live streams
  // the default value is not set (UINT_MAX)
  unsigned int rtmpStart;

  // stop at num seconds into the stream
  // the default value is not set (UINT_MAX)
  unsigned int rtmpStop;

  // set buffer time to num milliseconds
  // the default is RTMP_BUFFER_DEFAULT
  unsigned int rtmpBuffer;

  // key for SecureToken response, used if the server requires SecureToken authentication
  TCHAR *rtmpToken;

  // JSON token used by legacy Justin.tv servers, invokes NetStream.Authenticate.UsherToken
  TCHAR *rtmpJtv;

  // if the value is true, the SWF player is retrieved from the specified swfUrl for performing SWF Verification
  // the SWF hash and size (used in the verification step) are computed automatically
  // also the SWF information is cached in a .swfinfo file in the user's home directory,
  // so that it doesn't need to be retrieved and recalculated every time
  // the .swfinfo file records the SWF URL, the time it was fetched, the modification timestamp of the SWF file,
  // its size, and its hash
  // by default, the cached info will be used for 30 days before re-checking
  // the default value is RTMP_SWF_VERIFY_DEFAULT
  bool rtmpSwfVerify;

  // specify how many days to use the cached SWF info before re-checking
  // use 0 to always check the SWF URL
  // note that if the check shows that the SWF file has the same modification timestamp as before,
  // it will not be retrieved again
  // the default value is RTMP_SWF_AGE_DEFAULT
  unsigned int rtmpSwfAge;

  // the protocol implementation name (for logging purposes)
  TCHAR *protocolName;

  // creates libcurl worker
  // @return : S_OK if successful
  HRESULT CreateCurlWorker(void);

  // destroys libcurl worker
  // @return : S_OK if successful
  HRESULT DestroyCurlWorker(void);

  // write callback for CURL
  curl_write_callback writeCallback;

  // our write callback
  static size_t CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);

  // logging callback from librtmp
  static void RtmpLogCallback(struct RTMP *r, int level, const char *format, va_list vl);

  // user specified data supplied to write callback
  void *writeData;

  // holds internal state
  unsigned int state;

  // encodes string to be used by librtmp
  // @return : encoded string (null terminated) or NULL if error
  TCHAR *EncodeString(const TCHAR *string);

  // creates librtmp parameter
  // @return : parameter or NULL if error
  TCHAR *CreateRtmpParameter(const TCHAR *name, const TCHAR *value);

  // creates librtmp parameter, first encoded value
  // @return : parameter or NULL if error
  TCHAR *CreateRtmpEncodedParameter(const TCHAR *name, const TCHAR *value);

  // adds to librtmp connection string new parameter with specified name and value
  // @param connectionString : librtmp connection string
  // @param name : the name of parameter
  // @param value : the value of parameter
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(TCHAR **connectionString, const TCHAR *name, unsigned int value);

  // adds to librtmp connection string new parameter with specified name and value
  // @param connectionString : librtmp connection string
  // @param name : the name of parameter
  // @param value : the value of parameter
  // @param encode : specifies if value have to be first encoded
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(TCHAR **connectionString, const TCHAR *name, const TCHAR *value, bool encode);

  // adds to librtmp connection string new specified string
  // @param connectionString : librtmp connection string
  // @param string : the string to add to librtmp connection string
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(TCHAR **connectionString, const TCHAR *string);
};

#endif