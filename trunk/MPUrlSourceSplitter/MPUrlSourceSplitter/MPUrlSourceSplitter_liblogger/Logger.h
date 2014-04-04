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

#define LOGGER_NONE                                                           0
#define LOGGER_ERROR                                                          1
#define LOGGER_WARNING                                                        2
#define LOGGER_INFO                                                           3
#define LOGGER_VERBOSE                                                        4
#define LOGGER_DATA                                                           5

#define MPURLSOURCESPLITTER_LOG_FILE                                          L"log\\MPUrlSourceSplitter.log"
#define MPURLSOURCESPLITTER_LOG_FILE_BAK                                      L"log\\MPUrlSourceSplitter.bak"

#define MAX_LOG_SIZE_DEFAULT                                                  10485760
#define LOG_VERBOSITY_DEFAULT                                                 LOGGER_VERBOSE

// logging constants

// methods' names
#define METHOD_CONSTRUCTOR_NAME                                               L"ctor()"
#define METHOD_DESTRUCTOR_NAME                                                L"dtor()"

// methods' common string formats
#define METHOD_START_FORMAT                                                   L"%s: %s: Start"
#define METHOD_CONSTRUCTOR_START_FORMAT                                       L"%s: %s: Start, instance address: 0x%p"
#define METHOD_END_FORMAT                                                     L"%s: %s: End"
#define METHOD_END_HRESULT_FORMAT                                             L"%s: %s: End, result: 0x%08X"
#define METHOD_END_INT_FORMAT                                                 L"%s: %s: End, result: %d"
#define METHOD_END_INT64_FORMAT                                               L"%s: %s: End, result: %lld"
#define METHOD_END_FAIL_FORMAT                                                L"%s: %s: End, Fail"
#define METHOD_END_FAIL_HRESULT_FORMAT                                        L"%s: %s: End, Fail, result: 0x%08X"
#define METHOD_MESSAGE_FORMAT                                                 L"%s: %s: %s"

class CParameterCollection;

class CLogger
{
public:
  CLogger(CParameterCollection *configuration);
  ~CLogger(void);

  // log message to log file
  // @param logLevel : the log level of message
  // @param format : the formating string
  void Log(unsigned int logLevel, const wchar_t *format, ...);

  // the logger identifier
  GUID loggerInstance;

  void Log(unsigned int logLevel, const wchar_t *format, va_list vl);
  void LogMessage(unsigned int logLevel, const wchar_t *message);
  wchar_t *GetLogMessage(unsigned int logLevel, const wchar_t *format, va_list vl);

  void SetParameters(CParameterCollection *configuration);

  // sets allowed log verbosity
  // @param allowedLogVerbosity : allowed log verbosity level to set
  void SetAllowedLogVerbosity(unsigned int allowedLogVerbosity);

protected:
  HANDLE mutex;

  DWORD maxLogSize;
  unsigned int allowedLogVerbosity;

  static wchar_t *GetLogLevel(unsigned int level);
private:
};

#endif
