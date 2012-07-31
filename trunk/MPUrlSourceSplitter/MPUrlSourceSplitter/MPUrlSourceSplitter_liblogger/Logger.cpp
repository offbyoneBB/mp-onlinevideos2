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

#include "stdafx.h"

#include "Logger.h"
#include "Utilities.h"

#include <stdio.h>

CLogger::CLogger(CParameterCollection *configuration)
{
  // create mutex, can return NULL
  this->mutex = CreateMutex(NULL, false, L"Global\\MPUrlSourceFilter");
  if (CoCreateGuid(&this->loggerInstance) != S_OK)
  {
    this->loggerInstance = GUID_NULL;
  }

  this->maxLogSize = MAX_LOG_SIZE_DEFAULT;
  this->allowedLogVerbosity = LOG_VERBOSITY_DEFAULT;

  this->SetParameters(configuration);
}

CLogger::~CLogger(void)
{
  if (this->mutex != NULL)
  {
    CloseHandle(this->mutex);
  }
}

void CLogger::SetParameters(CParameterCollection *configuration)
{
  if (configuration != NULL)
  {
    // set maximum log size
    this->maxLogSize = configuration->GetValueLong(PARAMETER_NAME_MAX_LOG_SIZE, true, MAX_LOG_SIZE_DEFAULT);
    this->allowedLogVerbosity = configuration->GetValueLong(PARAMETER_NAME_LOG_VERBOSITY, true, LOG_VERBOSITY_DEFAULT);

    // check value
    this->maxLogSize = (this->maxLogSize <= 0) ? MAX_LOG_SIZE_DEFAULT : this->maxLogSize;
    this->allowedLogVerbosity = (this->allowedLogVerbosity < 0) ? LOG_VERBOSITY_DEFAULT : this->allowedLogVerbosity;
  }
}

void CLogger::Log(unsigned int level, const wchar_t *format, ...)
{
  va_list vl;
  va_start(vl, format);

  this->Log(level, format, vl);

  va_end(vl);
}

wchar_t *CLogger::GetLogLevel(unsigned int level)
{
  switch(level)
  {
  case LOGGER_NONE:
    return FormatString(L"         ");
  case LOGGER_ERROR:
    return FormatString(L"[Error]  ");
  case LOGGER_WARNING:
    return FormatString(L"[Warning]");
  case LOGGER_INFO:
    return FormatString(L"[Info]   ");
  case LOGGER_VERBOSE:
    return FormatString(L"[Verbose]");
  case LOGGER_DATA:
    return FormatString(L"[Data]   ");
  default:
    return FormatString(L"         ");
  }
}

void CLogger::Log(unsigned int level, const wchar_t *format, va_list vl)
{
  if (level <= this->allowedLogVerbosity)
  {
    if (this->mutex != NULL)
    {
      // wait for mutex free
      WaitForSingleObject(this->mutex, INFINITE);
    }

    wchar_t *logRow = this->GetLogMessage(level, format, vl);
    

    if (logRow != NULL)
    {
      this->LogMessage(level, logRow);
      FREE_MEM(logRow);      
    }

    if (this->mutex != NULL)
    {
      // release mutex
      ReleaseMutex(this->mutex);
    }
  }
}

void CLogger::LogMessage(unsigned int logLevel, const wchar_t *message)
{
  if (logLevel <= this->allowedLogVerbosity)
  {
    if (this->mutex != NULL)
    {
      // wait for mutex free
      WaitForSingleObject(this->mutex, INFINITE);
    }

    if (message != NULL)
    {
      // now we have log row
      // get log file
      wchar_t *fileName = GetMediaPortalFilePath(MPURLSOURCESPLITTER_LOG_FILE);

      if (fileName != NULL)
      {
        LARGE_INTEGER size;
        size.QuadPart = 0;

        // open or create file
        HANDLE hLogFile = CreateFile(fileName, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

        if (hLogFile != INVALID_HANDLE_VALUE)
        {
          if (!GetFileSizeEx(hLogFile, &size))
          {
            // error occured while getting file size
            size.QuadPart = 0;
          }

          CloseHandle(hLogFile);
          hLogFile = INVALID_HANDLE_VALUE;
        }

        if ((size.LowPart + wcslen(message)) > this->maxLogSize)
        {
          // log file exceedes maximum log size
          wchar_t *moveFileName = GetMediaPortalFilePath(MPURLSOURCESPLITTER_LOG_FILE_BAK);
          if (moveFileName != NULL)
          {
            // remove previous backup file
            DeleteFile(moveFileName);
            MoveFile(fileName, moveFileName);

            FREE_MEM(moveFileName);
          }
        }

        hLogFile = CreateFile(fileName, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_ALWAYS, FILE_FLAG_WRITE_THROUGH, NULL);
        if (hLogFile != INVALID_HANDLE_VALUE)
        {
          // move to end of log file
          LARGE_INTEGER distanceToMove;
          distanceToMove.QuadPart = 0;
          SetFilePointerEx(hLogFile, distanceToMove, NULL, FILE_END);

          // write data to log file
          DWORD written = 0;
          WriteFile(hLogFile, message, wcslen(message) * sizeof(wchar_t), &written, NULL);

          CloseHandle(hLogFile);
          hLogFile = INVALID_HANDLE_VALUE;
        }

        FREE_MEM(fileName);
      }
    }

    if (this->mutex != NULL)
    {
      // release mutex
      ReleaseMutex(this->mutex);
    }
  }
}

wchar_t *CLogger::GetLogMessage(unsigned int level, const wchar_t *format, va_list vl)
{
  SYSTEMTIME systemTime;
  GetLocalTime(&systemTime);

  int length = _vscwprintf(format, vl) + 1;
  ALLOC_MEM_DEFINE_SET(buffer, wchar_t, length, 0);
  if (buffer != NULL)
  {
    vswprintf_s(buffer, length, format, vl);
  }

  wchar_t *levelBuffer = this->GetLogLevel(level);
  wchar_t *guid = ConvertGuidToString(this->loggerInstance);

  wchar_t *logRow = FormatString(L"%02.2d-%02.2d-%04.4d %02.2d:%02.2d:%02.2d.%03.3d [%4x] [%s] %s %s\r\n",
    systemTime.wDay, systemTime.wMonth, systemTime.wYear,
    systemTime.wHour,systemTime.wMinute,systemTime.wSecond,
    systemTime.wMilliseconds,
    GetCurrentThreadId(),
    guid,
    levelBuffer,
    buffer);

  FREE_MEM(levelBuffer);
  FREE_MEM(guid);
  FREE_MEM(buffer);

  return logRow;
}

void CLogger::SetAllowedLogVerbosity(unsigned int allowedLogVerbosity)
{
  this->allowedLogVerbosity = allowedLogVerbosity;
}
