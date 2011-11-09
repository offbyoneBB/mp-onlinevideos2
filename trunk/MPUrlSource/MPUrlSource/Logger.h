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

#ifndef __LOGGER_DEFINED
#define __LOGGER_DEFINED

#include "ParameterCollection.h"

#include <tchar.h>

#define LOGGER_NONE                 0
#define LOGGER_ERROR                1
#define LOGGER_WARNING              2
#define LOGGER_INFO                 3
#define LOGGER_VERBOSE              4
#define LOGGER_DATA                 5

#define MPURLSOURCEFILTER_LOG_FILE          _T("log\\MPUrlSource.log")
#define MPURLSOURCEFILTER_LOG_FILE_BAK      _T("log\\MPUrlSource.bak")

// logging constants

// methods' names
#define METHOD_CONSTRUCTOR_NAME                                         _T("ctor()")
#define METHOD_DESTRUCTOR_NAME                                          _T("dtor()")
#define METHOD_CLEAR_SESSION_NAME                                       _T("ClearSession()")
#define METHOD_INITIALIZE_NAME                                          _T("Initialize()")
#define METHOD_PARSE_URL_NAME                                           _T("ParseUrl()")
#define METHOD_OPEN_CONNECTION_NAME                                     _T("OpenConnection()")
#define METHOD_CLOSE_CONNECTION_NAME                                    _T("CloseConnection()")
#define METHOD_RECEIVE_DATA_NAME                                        _T("ReceiveData()")
#define METHOD_PUSH_DATA_NAME                                           _T("PushData()")
#define METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME                         _T("ReceiveDataFromTimestamp()")
#define METHOD_ABORT_STREAM_RECEIVE_NAME                                _T("AbortStreamReceive()")
#define METHOD_QUERY_STREAM_PROGRESS_NAME                               _T("QueryStreamProgress()")
#define METHOD_END_OF_STREAM_REACHED_NAME                               _T("EndOfStreamReached()")

// methods' common string formats
#define METHOD_START_FORMAT                                             _T("%s: %s: Start")
#define METHOD_END_FORMAT                                               _T("%s: %s: End")
#define METHOD_END_HRESULT_FORMAT                                       _T("%s: %s: End, result: 0x%08X")
#define METHOD_END_FAIL_FORMAT                                          _T("%s: %s: End, Fail")
#define METHOD_END_FAIL_HRESULT_FORMAT                                  _T("%s: %s: End, Fail, result: 0x%08X")
#define METHOD_MESSAGE_FORMAT                                           _T("%s: %s: %s")

class CParameterCollection;

class MPURLSOURCE_API CLogger
{
public:
  CLogger(CParameterCollection *configuration);
  ~CLogger(void);

  // log message to log file
  // @param logLevel : the log level of message
  // @param format : the formating string
  void Log(unsigned int logLevel, const TCHAR *format, ...);

  // the logger identifier
  GUID loggerInstance;
private:
  HANDLE mutex;

  static TCHAR *GetLogLevel(unsigned int level);

  DWORD maxLogSize;
  unsigned int allowedLogVerbosity;
};

#endif
