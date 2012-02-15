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

#ifndef __MPURLSOURCE_FILE_DEFINED
#define __MPURLSOURCE_FILE_DEFINED

#include "MPUrlSourceSplitter_File_Exports.h"
#include "Logger.h"
#include "IProtocol.h"
#include "LinearBuffer.h"

#include <stdio.h>

// we should get data in two seconds
#define FILE_RECEIVE_DATA_TIMEOUT_DEFAULT                         2000
#define FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT             3

#define DEFAULT_BUFFER_SIZE                                       32 * 1024

#define PROTOCOL_NAME                                             L"FILE"

#define TOTAL_SUPPORTED_PROTOCOLS                                 1
wchar_t *SUPPORTED_PROTOCOLS[TOTAL_SUPPORTED_PROTOCOLS] = { L"FILE" };

#define PARAMETER_NAME_FILE_RECEIVE_DATA_TIMEOUT                  L"FileReceiveDataTimeout"
#define PARAMETER_NAME_FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS      L"FileOpenConnectionMaximumAttempts"

// returns protocol class instance
PIProtocol CreateProtocolInstance(CParameterCollection *configuration);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the MPUrlSourceSplitter_File.dll
class MPURLSOURCESPLITTER_FILE_API CMPUrlSourceSplitter_File : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSourceSplitter_File class
  CMPUrlSourceSplitter_File(CParameterCollection *configuration);

  // destructor
  ~CMPUrlSourceSplitter_File(void);

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

  // holds file path
  wchar_t *filePath;
  // holds file
  FILE *fileStream;

  // holds various parameters supplied by caller
  CParameterCollection *configurationParameters;

  // holds receive data timeout
  unsigned int receiveDataTimeout;

  // holds open connection maximum attempts
  unsigned int openConnetionMaximumAttempts;

  // the lenght of file
  LONGLONG fileLength;

  // holds if length of stream was set
  bool setLenght;

  // stream time
  int64_t streamTime;

  // mutex for locking access to file, buffer, ...
  HANDLE lockMutex;

  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;

  // specifies if filter requested supressing data
  bool supressData;
};

#endif
