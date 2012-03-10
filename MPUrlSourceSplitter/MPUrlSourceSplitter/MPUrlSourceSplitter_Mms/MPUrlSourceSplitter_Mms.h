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

#ifndef __MPURLSOURCESPLITTER_MMS_DEFINED
#define __MPURLSOURCESPLITTER_MMS_DEFINED

#include "MPUrlSourceSplitter_Mms_Exports.h"
#include "Logger.h"
#include "IProtocol.h"
#include "CurlInstance.h"
#include "MMSContext.h"
#include "MMSChunk.h"

#include <curl/curl.h>

#include <WinSock2.h>

// see Ref 2.2.1.8
#define USERAGENT                                                 L"User-Agent: NSPlayer/4.1.0.3856"
// see Ref 2.2.1.4.33
// the guid value can be changed to any valid value.
#define CLIENTGUID                                                L"Pragma: xClientGUID={c77e7400-738a-11d2-9add-0020af0a3278}"


// we should get data in twenty seconds
#define MMS_RECEIVE_DATA_TIMEOUT_DEFAULT                          20000
#define MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT              3

#define PROTOCOL_NAME                                             L"MMS"

#define METHOD_GET_MMS_HEADER_DATA_NAME                           L"GetMmsHeaderData()"
#define METHOD_GET_CHUNK_HEADER_NAME                              L"GetChunkHeader()"
#define METHOD_PARSE_MMS_ASF_HEADER_NAME                          L"ParseMmsAsfHeader()"

#define TOTAL_SUPPORTED_PROTOCOLS                                 1
wchar_t *SUPPORTED_PROTOCOLS[TOTAL_SUPPORTED_PROTOCOLS] = { L"MMS" };

#define MINIMUM_RECEIVED_DATA_FOR_SPLITTER                        1 * 1024 * 1024

#define PARAMETER_NAME_MMS_RECEIVE_DATA_TIMEOUT                   L"MmsReceiveDataTimeout"
#define PARAMETER_NAME_MMS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS       L"MmsOpenConnectionMaximumAttempts"
#define PARAMETER_NAME_MMS_REFERER                                L"MmsReferer"
#define PARAMETER_NAME_MMS_USER_AGENT                             L"MmsUserAgent"
#define PARAMETER_NAME_MMS_COOKIE                                 L"MmsCookie"
#define PARAMETER_NAME_MMS_VERSION                                L"MmsVersion"
#define PARAMETER_NAME_MMS_IGNORE_CONTENT_LENGTH                  L"MmsIgnoreContentLength"

#define CHUNK_HEADER_LENGTH                                       4   // 2bytes chunk type and 2bytes chunk length
#define EXT_HEADER_LENGTH                                         8   // 4bytes sequence, 2bytes useless and 2bytes chunk length


// returns protocol class instance
PIProtocol CreateProtocolInstance(CParameterCollection *configuration);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the CMPUrlSourceSplitter_Mms.dll
class MPURLSOURCESPLITTER_MMS_API CMPUrlSourceSplitter_Mms : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSourceSplitter_Mms class
  CMPUrlSourceSplitter_Mms(CParameterCollection *configuration);

  // destructor
  ~CMPUrlSourceSplitter_Mms(void);

  /* IProtocol interface */
  wchar_t *GetProtocolName(void);
  HRESULT Initialize(IOutputStream *filter, CParameterCollection *configuration);
  HRESULT ClearSession(void);
  HRESULT ParseUrl(const wchar_t *url, const CParameterCollection *parameters);
  HRESULT OpenConnection(void);
  bool IsConnected(void);
  void CloseConnection(void);
  void ReceiveData(bool *shouldExit);
  unsigned int GetReceiveDataTimeout(void);
  GUID GetInstanceId(void);
  unsigned int GetOpenConnectionMaximumAttempts(void);
  HRESULT AbortStreamReceive();  
  HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current);
  HRESULT QueryStreamAvailableLength(CStreamAvailableLength *availableLength);
  unsigned int GetSeekingCapabilities(void);
  int64_t SeekToTime(int64_t time);
  int64_t SeekToPosition(int64_t start, int64_t end);
  void SetSupressData(bool supressData);

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

  // holds if length of stream was set
  bool setLenght;

  // stream time
  int64_t streamTime;

  // specifies position in buffer
  // it is always reset on seek
  int64_t bytePosition;

  // mutex for locking access to file, buffer, ...
  HANDLE lockMutex;

  // the stream url
  wchar_t *url;

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

  // specifies if filter requested supressing data
  bool supressData;

  // specifies request sequencenumber
  unsigned int sequenceNumber;

  // specifies if receiving data or issuing commands
  bool receivingData;

  // holds MMS context
  MMSContext *mmsContext;

  // gets MMS header data
  // @param mmsContext : reference to MMS context
  // @param mmsChunk : reference to already acquired MMS chunk (can be NULL)
  // @return : S_OK if successfull, error code otherwise
  HRESULT GetMmsHeaderData(MMSContext *mmsContext, MMSChunk *mmsChunk);

  // gets MMS chunk data
  // @param mmsContext : reference to MMS context
  // @param mmsChunk : reference to MMS chunk
  // @return : S_OK if successful or error code
  HRESULT GetMmsChunk(MMSContext *mmsContext, MMSChunk *mmsChunk);

  // parses MMS ASF header
  // @param mmsContext : reference to MMS context
  // @param mmsChunk : reference to MMS chunk
  // @return : S_OK if successful or error code
  HRESULT ParseMmsAsfHeader(MMSContext *mmsContext, MMSChunk *mmsChunk);
};

#endif
