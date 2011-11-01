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

#include "MPURLSOURCE_FILE_Exports.h"
#include "Logger.h"
#include "ProtocolInterface.h"
#include "LinearBuffer.h"

// we should get data in two seconds
#define FILE_RECEIVE_DATA_TIMEOUT_DEFAULT                   2000
#define FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT       3

#define DEFAULT_BUFFER_SIZE                                 32 * 1024
#define OUTPUT_PIN_NAME                                     _T("Output File")

#define CONFIGURATION_SECTION_FILE                          _T("FILE")

#define CONFIGURATION_FILE_RECEIVE_DATA_TIMEOUT             _T("FileReceiveDataTimeout")
#define CONFIGURATION_FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS _T("FileOpenConnectionMaximumAttempts")

// returns protocol class instance
PIProtocol CreateProtocolInstance(void);

// destroys protocol class instance
void DestroyProtocolInstance(PIProtocol pProtocol);

// This class is exported from the MPUrlSource_File.dll
class MPURLSOURCE_FILE_API CMPUrlSource_File : public IProtocol
{
public:
  // constructor
  // create instance of CMPUrlSource_File class
  CMPUrlSource_File(void);

  // destructor
  ~CMPUrlSource_File(void);

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
  CLogger logger;

  // source filter that created this instance
  IOutputStream *filter;

  // holds file path
  TCHAR *filePath;
  // holds file
  FILE *fileStream;

  // holds various parameters supplied by TvService
  CParameterCollection *configurationParameters;
  // holds various parameters supplied by TvService when loading file
  CParameterCollection *loadParameters;

  // holds receive data timeout
  unsigned int receiveDataTimeout;

  // holds open connection maximum attempts
  unsigned int openConnetionMaximumAttempts;

  // the lenght of file
  LONGLONG fileLength;

  // holds if length of stream was set
  bool setLenght;

  // stream time
  REFERENCE_TIME streamTime;

  // mutex for locking access to file, buffer, ...
  HANDLE lockMutex;

  // specifies if whole stream is downloaded
  bool wholeStreamDownloaded;
};

#endif
