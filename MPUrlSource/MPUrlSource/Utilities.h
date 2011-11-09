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

#ifndef __UTILITIES_DEFINED
#define __UTILITIES_DEFINED

#include "ParameterCollection.h"
#include "Logger.h"
#include "LinearBuffer.h"

#include <streams.h>

#define INI_FILE          _T("MPUrlSource.ini")

// get Tv Server folder
// @return : reference to null terminated string with path of Tv Server ended with '\' or NULL if error occured
MPURLSOURCE_API TCHAR *GetTvServerFolder(void);

// get MediaPortal folder
// @return : reference to null terminated string with path of MediaPortal ended with '\' or NULL if error occured
MPURLSOURCE_API TCHAR *GetMediaPortalFolder(void);

// get path to file in Tv Server folder
// Tv Server folder always ended with '\'
// @param filePath : the file path in Tv Server folder
// @return : reference to null terminated string with path of file or NULL if error occured
MPURLSOURCE_API TCHAR *GetTvServerFilePath(const TCHAR *filePath);

// get path to file in MediaPortal folder
// MediaPortal folder always ended with '\'
// @param filePath : the file path in MediaPortal folder
// @return : reference to null terminated string with path of file or NULL if error occured
MPURLSOURCE_API TCHAR *GetMediaPortalFilePath(const TCHAR *filePath);

#define CHECK_CONDITION(result, condition, case_true, case_false)                 if (result == 0) { result = (condition) ? case_true : case_false; }

#define CHECK_CONDITION_HRESULT(result, condition, case_true, case_false)         if (SUCCEEDED(result)) { result = (condition) ? case_true : case_false; }

#define CHECK_POINTER_HRESULT(result, pointer, case_true, case_false)             CHECK_CONDITION_HRESULT(result, pointer != NULL, case_true, case_false)
#define CHECK_POINTER_DEFAULT_HRESULT(result, pointer)                            CHECK_POINTER_HRESULT(result, pointer, S_OK, E_POINTER)

#define CHECK_POINTER(result, pointer, case_true, case_false)                     CHECK_CONDITION(result, pointer != NULL, case_true, case_false)

#endif
