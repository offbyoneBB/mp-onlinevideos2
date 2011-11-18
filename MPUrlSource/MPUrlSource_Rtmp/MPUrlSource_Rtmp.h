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

#ifndef __MPURLSOURCE_RTMP_DEFINED
#define __MPURLSOURCE_RTMP_DEFINED

#include "MPURLSOURCE_RTMP_Exports.h"
#include "Logger.h"
#include "ProtocolInterface.h"
#include "LinearBuffer.h"
#include "CurlInstance.h"

#include <curl/curl.h>

#include <WinSock2.h>

#define OUTPUT_PIN_NAME                                           _T("Output")

#define PROTOCOL_NAME                                             _T("RTMP")

#define TOTAL_SUPPORTED_PROTOCOLS                                 6
TCHAR *SUPPORTED_PROTOCOLS[TOTAL_SUPPORTED_PROTOCOLS] = { _T("RTMP"), _T("RTMPT"), _T("RTMPE"), _T("RTMPTE"), _T("RTMPS"), _T("RTMPTS") };

/* CONNECTION PARAMETERS */

// These options define the content of the RTMP Connect request packet.
// If correct values are not provided, the media server will reject the connection attempt.

// Name of application to connect to on the RTMP server.
// Overrides the app in the RTMP URL.
// Sometimes the librtmp URL parser cannot determine the app name automatically,
// so it must be given explicitly using this option. 
#define PARAMETER_NAME_RTMP_APP                                   _T("RtmpApp")

// URL of the target stream. Defaults to rtmp[t][e|s]://host[:port]/app. 
#define PARAMETER_NAME_RTMP_TC_URL                                _T("RtmpTcUrl")

// URL of the web page in which the media was embedded. By default no value will be sent. 
#define PARAMETER_NAME_RTMP_PAGE_URL                              _T("RtmpPageUrl")

// URL of the SWF player for the media. By default no value will be sent.
#define PARAMETER_NAME_RTMP_SWF_URL                               _T("RtmpSwfUrl")

// Version of the Flash plugin used to run the SWF player. The default is "WIN 10,0,32,18".
#define PARAMETER_NAME_RTMP_FLASHVER                              _T("RtmpFlashVer")

// Append arbitrary AMF data to the Connect message.
// The type must be B for Boolean, N for number, S for string, O for object, or Z for null.
// For Booleans the data must be either 0 or 1 for FALSE or TRUE, respectively.
// Likewise for Objects the data must be 0 or 1 to end or begin an object, respectively.
// Data items in subobjects may be named, by prefixing the type with 'N' and specifying the name before the value,
// e.g. NB:myFlag:1. This option may be used multiple times to construct arbitrary AMF sequences. E.g.
// conn=B:1 conn=S:authMe conn=O:1 conn=NN:code:1.23 conn=NS:flag:ok conn=O:0
#define PARAMETER_NAME_RTMP_ARBITRARY_DATA                        _T("RtmpArbitraryData")

// The maximum attempts for opening connection to RTMP server.
#define PARAMETER_NAME_RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS      _T("RtmpOpenConnectionMaximumAttempts")

/* SESSION PARAMETERS */

// These options take effect after the Connect request has succeeded.

// Overrides the playpath parsed from the RTMP URL.
// Sometimes the rtmpdump URL parser cannot determine the correct playpath automatically,
// so it must be given explicitly using this option.
#define PARAMETER_NAME_RTMP_PLAY_PATH                             _T("RtmpPlayPath")

// If the value is 1 or TRUE, issue a set_playlist command before sending the play command.
// The playlist will just contain the current playpath.
// If the value is 0 or FALSE, the set_playlist command will not be sent.
// The default is FALSE. 
#define PARAMETER_NAME_RTMP_PLAYLIST                              _T("RtmpPlaylist")

// Specify that the media is a live stream. No resuming or seeking in live streams is possible.
#define PARAMETER_NAME_RTMP_LIVE                                  _T("RtmpLive")

// Name of live stream to subscribe to. Defaults to playpath.
#define PARAMETER_NAME_RTMP_SUBSCRIBE                             _T("RtmpSubscribe")

// Start at num seconds into the stream. Not valid for live streams.
#define PARAMETER_NAME_RTMP_START                                 _T("RtmpStart")

// Stop at num seconds into the stream.
#define PARAMETER_NAME_RTMP_STOP                                  _T("RtmpStop")

// Set buffer time to num milliseconds.
#define PARAMETER_NAME_RTMP_BUFFER                                _T("RtmpBuffer")

// Timeout the session after num seconds without receiving any data from the server.
#define PARAMETER_NAME_RTMP_RECEIVE_DATA_TIMEOUT                  _T("RtmpReceiveDataTimeout")

/* SECURITY PARAMETERS */

// These options handle additional authentication requests from the server.

// Key for SecureToken response, used if the server requires SecureToken authentication.
#define PARAMETER_NAME_RTMP_TOKEN                                 _T("RtmpToken")

// JSON token used by legacy Justin.tv servers. Invokes NetStream.Authenticate.UsherToken
#define PARAMETER_NAME_RTMP_JTV                                   _T("RtmpJtv")

// If the value is 1 or TRUE, the SWF player is retrieved from the specified swfUrl for performing SWF Verification.
// The SWF hash and size (used in the verification step) are computed automatically.
// Also the SWF information is cached in a .swfinfo file in the user's home directory,
// so that it doesn't need to be retrieved and recalculated every time.
// The .swfinfo file records the SWF URL, the time it was fetched,
// the modification timestamp of the SWF file, its size, and its hash.
// By default, the cached info will be used for 30 days before re-checking. 
#define PARAMETER_NAME_RTMP_SWF_VERIFY                            _T("RtmpSwfVerify")

// Specify how many days to use the cached SWF info before re-checking.
// Use 0 to always check the SWF URL.
// Note that if the check shows that the SWF file has the same modification timestamp as before,
// it will not be retrieved again. 
#define PARAMETER_NAME_RTMP_SWF_AGE                               _T("RtmpSwfAge")



// returns protocol class instance
PIProtocol CreateProtocolInstance(CParameterCollection *configuration);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the MPUrlSource_Rtmp.dll
class MPURLSOURCE_RTMP_API CMPUrlSource_Rtmp : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSource_Rtmp class
  CMPUrlSource_Rtmp(CParameterCollection *configuration);

  // destructor
  ~CMPUrlSource_Rtmp(void);

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

  // the duration of stream
  // mostly we get only stream duration instead of stream length
  double streamDuration;

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

  // callback function for receiving data from libcurl
  static size_t CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);

  // reference to variable that signalize if protocol is requested to exit
  bool shouldExit;
  // internal variable for requests to interrupt transfers
  bool internalExitRequest;
  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;
};

#endif
