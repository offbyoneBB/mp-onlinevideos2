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

#ifndef __MPURLSOURCESPLITTER_PROTOCOL_RTMP_DEFINED
#define __MPURLSOURCESPLITTER_PROTOCOL_RTMP_DEFINED

#include "MPUrlSourceSplitter_Protocol_Rtmp_Exports.h"
#include "Logger.h"
#include "IProtocolPlugin.h"
#include "LinearBuffer.h"
#include "CurlInstance.h"

#include <curl/curl.h>

#include <WinSock2.h>

#define PROTOCOL_NAME                                             L"RTMP"

#define TOTAL_SUPPORTED_PROTOCOLS                                 6
wchar_t *SUPPORTED_PROTOCOLS[TOTAL_SUPPORTED_PROTOCOLS] = { L"RTMP", L"RTMPT", L"RTMPE", L"RTMPTE", L"RTMPS", L"RTMPTS" };

#define MINIMUM_RECEIVED_DATA_FOR_SPLITTER                        1 * 1024 * 1024
#define BUFFER_FOR_PROCESSING_SIZE_DEFAULT                        256 * 1024

/* CONNECTION PARAMETERS */

// These options define the content of the RTMP Connect request packet.
// If correct values are not provided, the media server will reject the connection attempt.

// Name of application to connect to on the RTMP server.
// Overrides the app in the RTMP URL.
// Sometimes the librtmp URL parser cannot determine the app name automatically,
// so it must be given explicitly using this option. 
#define PARAMETER_NAME_RTMP_APP                                   L"RtmpApp"

// URL of the target stream. Defaults to rtmp[t][e|s]://host[:port]/app. 
#define PARAMETER_NAME_RTMP_TC_URL                                L"RtmpTcUrl"

// URL of the web page in which the media was embedded. By default no value will be sent. 
#define PARAMETER_NAME_RTMP_PAGE_URL                              L"RtmpPageUrl"

// URL of the SWF player for the media. By default no value will be sent.
#define PARAMETER_NAME_RTMP_SWF_URL                               L"RtmpSwfUrl"

// Version of the Flash plugin used to run the SWF player. The default is "WIN 10,0,32,18".
#define PARAMETER_NAME_RTMP_FLASHVER                              L"RtmpFlashVer"

// Authentication string to be appended to the connect string
#define PARAMETER_NAME_RTMP_AUTH                                  L"RtmpAuth"

// Append arbitrary AMF data to the Connect message.
// The type must be B for Boolean, N for number, S for string, O for object, or Z for null.
// For Booleans the data must be either 0 or 1 for FALSE or TRUE, respectively.
// Likewise for Objects the data must be 0 or 1 to end or begin an object, respectively.
// Data items in subobjects may be named, by prefixing the type with 'N' and specifying the name before the value,
// e.g. NB:myFlag:1. This option may be used multiple times to construct arbitrary AMF sequences. E.g.
// conn=B:1 conn=S:authMe conn=O:1 conn=NN:code:1.23 conn=NS:flag:ok conn=O:0
#define PARAMETER_NAME_RTMP_ARBITRARY_DATA                        L"RtmpArbitraryData"

// The maximum attempts for opening connection to RTMP server.
#define PARAMETER_NAME_RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS      L"RtmpOpenConnectionMaximumAttempts"

/* SESSION PARAMETERS */

// These options take effect after the Connect request has succeeded.

// Overrides the playpath parsed from the RTMP URL.
// Sometimes the rtmpdump URL parser cannot determine the correct playpath automatically,
// so it must be given explicitly using this option.
#define PARAMETER_NAME_RTMP_PLAY_PATH                             L"RtmpPlayPath"

// If the value is 1 or TRUE, issue a set_playlist command before sending the play command.
// The playlist will just contain the current playpath.
// If the value is 0 or FALSE, the set_playlist command will not be sent.
// The default is FALSE. 
#define PARAMETER_NAME_RTMP_PLAYLIST                              L"RtmpPlaylist"

// Specify that the media is a live stream. No resuming or seeking in live streams is possible.
#define PARAMETER_NAME_RTMP_LIVE                                  L"RtmpLive"

// Name of live stream to subscribe to. Defaults to playpath.
#define PARAMETER_NAME_RTMP_SUBSCRIBE                             L"RtmpSubscribe"

// Start at num seconds into the stream. Not valid for live streams.
#define PARAMETER_NAME_RTMP_START                                 L"RtmpStart"

// Stop at num seconds into the stream.
#define PARAMETER_NAME_RTMP_STOP                                  L"RtmpStop"

// Set buffer time to num milliseconds.
#define PARAMETER_NAME_RTMP_BUFFER                                L"RtmpBuffer"

// Timeout the session after num seconds without receiving any data from the server.
#define PARAMETER_NAME_RTMP_RECEIVE_DATA_TIMEOUT                  L"RtmpReceiveDataTimeout"

/* SECURITY PARAMETERS */

// These options handle additional authentication requests from the server.

// Key for SecureToken response, used if the server requires SecureToken authentication.
#define PARAMETER_NAME_RTMP_TOKEN                                 L"RtmpToken"

// JSON token used by legacy Justin.tv servers. Invokes NetStream.Authenticate.UsherToken
#define PARAMETER_NAME_RTMP_JTV                                   L"RtmpJtv"

// If the value is 1 or TRUE, the SWF player is retrieved from the specified swfUrl for performing SWF Verification.
// The SWF hash and size (used in the verification step) are computed automatically.
// Also the SWF information is cached in a .swfinfo file in the user's home directory,
// so that it doesn't need to be retrieved and recalculated every time.
// The .swfinfo file records the SWF URL, the time it was fetched,
// the modification timestamp of the SWF file, its size, and its hash.
// By default, the cached info will be used for 30 days before re-checking. 
#define PARAMETER_NAME_RTMP_SWF_VERIFY                            L"RtmpSwfVerify"

// Specify how many days to use the cached SWF info before re-checking.
// Use 0 to always check the SWF URL.
// Note that if the check shows that the SWF file has the same modification timestamp as before,
// it will not be retrieved again. 
#define PARAMETER_NAME_RTMP_SWF_AGE                               L"RtmpSwfAge"


// This class is exported from the CMPUrlSourceSplitter_Protocol_Rtmp.dll
class MPURLSOURCESPLITTER_PROTOCOL_RTMP_API CMPUrlSourceSplitter_Protocol_Rtmp : public IProtocolPlugin
{
public:
  // constructor
  // create instance of CMPUrlSourceSplitter_Protocol_Rtmp class
  CMPUrlSourceSplitter_Protocol_Rtmp(CParameterCollection *configuration);

  // destructor
  ~CMPUrlSourceSplitter_Protocol_Rtmp(void);

  // IProtocol interface

  // test if connection is opened
  // @return : true if connected, false otherwise
  bool IsConnected(void);

  // get protocol maximum open connection attempts
  // @return : maximum attempts of opening connections or UINT_MAX if error
  unsigned int GetOpenConnectionMaximumAttempts(void);

  // parse given url to internal variables for specified protocol
  // errors should be logged to log file
  // @param parameters : the url and connection parameters
  // @return : S_OK if successfull
  HRESULT ParseUrl(const CParameterCollection *parameters);

  // receive data and stores them into internal buffer
  // @param shouldExit : the reference to variable specifying if method have to be finished immediately
  void ReceiveData(bool *shouldExit);

  // ISimpleProtocol interface

  // get timeout (in ms) for receiving data
  // @return : timeout (in ms) for receiving data
  unsigned int GetReceiveDataTimeout(void);

  // starts receiving data from specified url and configuration parameters
  // @param parameters : the url and parameters used for connection
  // @return : S_OK if url is loaded, false otherwise
  HRESULT StartReceivingData(const CParameterCollection *parameters);

  // request protocol implementation to cancel the stream reading operation
  // @return : S_OK if successful
  HRESULT StopReceivingData(void);

  // retrieves the progress of the stream reading operation
  // @param total : reference to a variable that receives the length of the entire stream, in bytes
  // @param current : reference to a variable that receives the length of the downloaded portion of the stream, in bytes
  // @return : S_OK if successful, VFW_S_ESTIMATED if returned values are estimates, E_UNEXPECTED if unexpected error
  HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current);
  
  // retrieves available lenght of stream
  // @param available : reference to instance of class that receives the available length of stream, in bytes
  // @return : S_OK if successful, other error codes if error
  HRESULT QueryStreamAvailableLength(CStreamAvailableLength *availableLength);

  // clear current session
  // @return : S_OK if successfull
  HRESULT ClearSession(void);

  // ISeeking interface

  // gets seeking capabilities of protocol
  // @return : bitwise combination of SEEKING_METHOD flags
  unsigned int GetSeekingCapabilities(void);

  // request protocol implementation to receive data from specified time (in ms)
  // @param time : the requested time (zero is start of stream)
  // @return : time (in ms) where seek finished or lower than zero if error
  int64_t SeekToTime(int64_t time);

  // request protocol implementation to receive data from specified position to specified position
  // @param start : the requested start position (zero is start of stream)
  // @param end : the requested end position, if end position is lower or equal to start position than end position is not specified
  // @return : position where seek finished or lower than zero if error
  int64_t SeekToPosition(int64_t start, int64_t end);

  // sets if protocol implementation have to supress sending data to filter
  // @param supressData : true if protocol have to supress sending data to filter, false otherwise
  void SetSupressData(bool supressData);

  // IPlugin interface

  // return reference to null-terminated string which represents plugin name
  // function have to allocate enough memory for plugin name string
  // errors should be logged to log file and returned NULL
  // @return : reference to null-terminated string
  wchar_t *GetName(void);

  // get plugin instance ID
  // @return : GUID, which represents instance identifier or GUID_NULL if error
  GUID GetInstanceId(void);

  // initialize plugin implementation with configuration parameters
  // @param configuration : the reference to additional configuration parameters (created by plugin's hoster class)
  // @return : S_OK if successfull
  HRESULT Initialize(PluginConfiguration *configuration);

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
  bool setLength;

  // stream time
  int64_t streamTime;

  // specifies position in buffer
  // it is always reset on seek
  int64_t bytePosition;

  // mutex for locking access to file, buffer, ...
  HANDLE lockMutex;

  // main instance of CURL
  CCurlInstance *mainCurlInstance;

  // callback function for receiving data from libcurl
  static size_t CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata);

  // reference to variable that signalize if protocol is requested to exit
  bool shouldExit;
  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;
  // specifies if seeking (cleared when first data arrive)
  bool seekingActive;
  // specifies if filter requested supressing data
  bool supressData;

  // buffer for processing data before are send to filter
  LinearBuffer *bufferForProcessing;

  // holds first FLV packet timestamp for correction of video packet timestamps
  int firstTimestamp;

  // holds first video FLV packet timestamp for correction of video packet timestamps
  int firstVideoTimestamp;
};

#endif
