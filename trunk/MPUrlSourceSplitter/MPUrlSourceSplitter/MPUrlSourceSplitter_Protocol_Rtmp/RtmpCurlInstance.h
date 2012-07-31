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

#ifndef __RTMP_CURL_INSTANCE_DEFINED
#define __RTMP_CURL_INSTANCE_DEFINED

#include "MPUrlSourceSplitter_Protocol_Rtmp_Parameters.h"
#include "CurlInstance.h"

#include <librtmp/rtmp.h>

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

 /* SECURITY PARAMETERS */

#define RTMP_TOKEN_TOKEN                                    L"token"
#define RTMP_TOKEN_JTV                                      L"jtv"
#define RTMP_TOKEN_SWF_VERIFY                               L"swfVfy"
#define RTMP_TOKEN_SWF_AGE                                  L"swfAge"

// Special characters in values may need to be escaped to prevent misinterpretation by the option parser.
// The escape encoding uses a backslash followed by two hexadecimal digits representing the ASCII value of the character.
// E.g., spaces must be escaped as \20 and backslashes must be escaped as \5c.

class CRtmpCurlInstance :
  public CCurlInstance
{
public:
  // initializes a new instance of CRtmpCurlInstance class
  // @param logger : logger for logging purposes
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CRtmpCurlInstance(CLogger *logger, const wchar_t *url, const wchar_t *protocolName);

  // destructor
  virtual ~CRtmpCurlInstance(void);

  // initializes CURL instance
  // @return : true if successful, false otherwise
  virtual bool Initialize(void);

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

protected:
  // called when CURL debug message arives
  // @param type : CURL message type
  // @param data : received CURL message data
  virtual void CurlDebug(curl_infotype type, const wchar_t *data);

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

  // logging callback from librtmp
  static void RtmpLogCallback(struct RTMP *r, int level, const char *format, va_list vl);

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