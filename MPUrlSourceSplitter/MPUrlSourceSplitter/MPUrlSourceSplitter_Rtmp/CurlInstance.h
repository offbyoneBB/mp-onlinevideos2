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

#include "MPUrlSourceSplitter_Rtmp_Exports.h"
#include "Logger.h"

#include <curl/curl.h>
#include <librtmp/rtmp.h>

#define METHOD_CREATE_CURL_WORKER_NAME                      L"CreateCurlWorker()"
#define METHOD_DESTROY_CURL_WORKER_NAME                     L"DestroyCurlWorker()"
#define METHOD_CURL_WORKER_NAME                             L"CurlWorker()"

#define METHOD_CURL_ERROR_MESSAGE                           L"%s: %s: %s: %s"

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
#define RTMP_AUTH_DEFAULT                                   NULL
#define RTMP_ARBITRARY_DATA_DEFAULT                         NULL
#define RTMP_PLAY_PATH_DEFAULT                              NULL
#define RTMP_PLAYLIST_DEFAULT                               false
#define RTMP_LIVE_DEFAULT                                   false
#define RTMP_SUBSCRIBE_DEFAULT                              NULL
#define RTMP_START_DEFAULT                                  INT64_MAX
#define RTMP_STOP_DEFAULT                                   INT64_MAX
#define RTMP_BUFFER_DEFAULT                                 30000
#define RTMP_TOKEN_DEFAULT                                  NULL
#define RTMP_JTV_DEFAULT                                    NULL
#define RTMP_SWF_VERIFY_DEFAULT                             false
#define RTMP_SWF_AGE_DEFAULT                                0

// define tokens for librtmp

/* CONNECTION PARAMETERS */

#define RTMP_TOKEN_APP                                      L"app"
#define RTMP_TOKEN_TC_URL                                   L"tcUrl"
#define RTMP_TOKEN_PAGE_URL                                 L"pageUrl"
#define RTMP_TOKEN_SWF_URL                                  L"swfUrl"
#define RTMP_TOKEN_FLASHVER                                 L"flashVer"
#define RTMP_TOKEN_AUTH                                     L"auth"

// arbitrary data are passed as they are = they MUST be escaped
// so no token for abitrary data is needed

/* SESSION PARAMETERS */

#define RTMP_TOKEN_PLAY_PATH                                L"playpath"
#define RTMP_TOKEN_PLAYLIST                                 L"playlist"
#define RTMP_TOKEN_LIVE                                     L"live"
#define RTMP_TOKEN_SUBSCRIBE                                L"subscribe"
#define RTMP_TOKEN_START                                    L"start"
#define RTMP_TOKEN_STOP                                     L"stop"
#define RTMP_TOKEN_BUFFER                                   L"buffer"
#define RTMP_TOKEN_RECEIVE_DATA_TIMEOUT                     L"timeout"

 /* SECURITY PARAMETERS */

#define RTMP_TOKEN_TOKEN                                    L"token"
#define RTMP_TOKEN_JTV                                      L"jtv"
#define RTMP_TOKEN_SWF_VERIFY                               L"swfVfy"
#define RTMP_TOKEN_SWF_AGE                                  L"swfAge"

// Special characters in values may need to be escaped to prevent misinterpretation by the option parser.
// The escape encoding uses a backslash followed by two hexadecimal digits representing the ASCII value of the character.
// E.g., spaces must be escaped as \20 and backslashes must be escaped as \5c.

class MPURLSOURCESPLITTER_RTMP_API CCurlInstance
{
public:
  // initializes a new instance of CCurlInstance class
  // @param logger : logger for logging purposes
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CCurlInstance(CLogger *logger, wchar_t *url, wchar_t *protocolName);
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
  wchar_t *GetCurlErrorMessage(CURLcode errorCode);

  // report libcurl error into log file
  // @param logLevel : the verbosity level of logged message
  // @param protocolName : name of protocol calling ReportCurlErrorMessage()
  // @param functionName : name of function calling ReportCurlErrorMessage()
  // @param message : optional message to log (can be NULL)
  // @param errorCode : the error code returned by libcurl
  void ReportCurlErrorMessage(unsigned int logLevel, const wchar_t *protocolName, const wchar_t *functionName, const wchar_t *message, CURLcode errorCode);

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

  // starts receiving data
  // @return : true if successful, false otherwise
  bool StartReceivingData(void);

  // gets CURL state
  // @return : one of CURL_STATE values
  unsigned int GetCurlState(void);

  // gets if connection be closed without waiting
  // @return : true if connection be closed without waiting, false otherwise
  bool GetCloseWithoutWaiting(void);

  // sets if connection be closed without waiting
  // @param closeWithoutWaiting : true if connection be closed without waiting, false otherwise
  void SetCloseWithoutWaiting(bool closeWithoutWaiting);

  // RTMP protocol specific variables setters
  void SetRtmpApp(const wchar_t *rtmpApp);
  void SetRtmpTcUrl(const wchar_t *rtmpTcUrl);
  void SetRtmpPageUrl(const wchar_t *rtmpPageUrl);
  void SetRtmpSwfUrl(const wchar_t *rtmpSwfUrl);
  void SetRtmpFlashVersion(const wchar_t *rtmpFlashVersion);
  void SetRtmpAuth(const wchar_t *rtmpAuth);
  void SetRtmpArbitraryData(const wchar_t *rtmpArbitraryData);
  void SetRtmpPlayPath(const wchar_t *rtmpPlayPath);
  void SetRtmpPlaylist(bool rtmpPlaylist);
  void SetRtmpLive(bool rtmpLive);
  void SetRtmpSubscribe(const wchar_t *rtmpSubscribe);
  void SetRtmpStart(int64_t rtmpStart);
  void SetRtmpStop(int64_t rtmpStop);
  void SetRtmpBuffer(unsigned int rtmpBuffer);
  void SetRtmpToken(const wchar_t *rtmpToken);
  void SetRtmpJtv(const wchar_t *rtmpJtv);
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

  // the stream url
  wchar_t *url;

  // RTMP protocol specific variables

  // name of application to connect to on the RTMP server
  // if not NULL than overrides the app in the RTMP URL
  wchar_t *rtmpApp;

  // URL of the target stream
  // if not NULL than overrides the tcUrl in the RTMP URL
  wchar_t *rtmpTcUrl;

  // URL of the web page in which the media was embedded
  // if not NULL than value is sent
  wchar_t *rtmpPageUrl;

  // URL of the SWF player for the media
  // if not NULL than value is sent
  wchar_t *rtmpSwfUrl;

  // version of the Flash plugin used to run the SWF player
  // if not NULL than overrides the default flash version "LNX 10,0,32,18"
  wchar_t *rtmpFlashVersion;

  // authentication string to be appended to the connect string
  wchar_t *rtmpAuth;

  // if not NULL, than append arbitrary AMF data to the Connect message
  wchar_t *rtmpArbitraryData;

  // timeout the session after num of milliseconds without receiving any data from the server
  // if not set (UINT_MAX) then default value of 120 seconds is used
  unsigned int rtmpReceiveDataTimeout;

  // if not NULL than overrides the playpath parsed from the RTMP URL
  wchar_t *rtmpPlayPath;

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
  wchar_t *rtmpSubscribe;

  // start at num seconds into the stream
  // not valid for live streams
  // the default value is not set (INT64_MAX)
  int64_t rtmpStart;

  // stop at num seconds into the stream
  // the default value is not set (INT64_MAX)
  int64_t rtmpStop;  

  // set buffer time to num milliseconds
  // the default is RTMP_BUFFER_DEFAULT
  unsigned int rtmpBuffer;

  // key for SecureToken response, used if the server requires SecureToken authentication
  wchar_t *rtmpToken;

  // JSON token used by legacy Justin.tv servers, invokes NetStream.Authenticate.UsherToken
  wchar_t *rtmpJtv;

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
  wchar_t *protocolName;

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

  // specifies if current connection have to be closed without waiting
  bool closeWithoutWaiting;

  // encodes string to be used by librtmp
  // @return : encoded string (null terminated) or NULL if error
  wchar_t *EncodeString(const wchar_t *string);

  // creates librtmp parameter
  // @return : parameter or NULL if error
  wchar_t *CreateRtmpParameter(const wchar_t *name, const wchar_t *value);

  // creates librtmp parameter, first encoded value
  // @return : parameter or NULL if error
  wchar_t *CreateRtmpEncodedParameter(const wchar_t *name, const wchar_t *value);

  // adds to librtmp connection string new parameter with specified name and value
  // @param connectionString : librtmp connection string
  // @param name : the name of parameter
  // @param value : the value of parameter
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, unsigned int value);

  // adds to librtmp connection string new parameter with specified name and value
  // @param connectionString : librtmp connection string
  // @param name : the name of parameter
  // @param value : the value of parameter
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, int64_t value);

  // adds to librtmp connection string new parameter with specified name and value
  // @param connectionString : librtmp connection string
  // @param name : the name of parameter
  // @param value : the value of parameter
  // @param encode : specifies if value have to be first encoded
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, const wchar_t *value, bool encode);

  // adds to librtmp connection string new specified string
  // @param connectionString : librtmp connection string
  // @param string : the string to add to librtmp connection string
  // @return : true if successful, false otherwise
  bool AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *string);
};

#endif