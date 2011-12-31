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

#include "StdAfx.h"

#include "Utilities.h"

#include <ObjBase.h>
#include <ShlObj.h>
#include <stdio.h>

wchar_t *GetTvServerFolder(void)
{
  return GetTvServerFilePath(NULL);
}

wchar_t *GetTvServerFilePath(const TCHAR *filePath)
{
  wchar_t *result = NULL;
  ALLOC_MEM_DEFINE_SET(folder, wchar_t, MAX_PATH, 0);
  if (folder != NULL)
  {
    // get common application data folder
    SHGetSpecialFolderPath(NULL, folder, CSIDL_COMMON_APPDATA, FALSE);

    if (filePath == NULL)
    {
      result = FormatString(L"%s\\Team MediaPortal\\MediaPortal TV Server\\", folder);
    }
    else
    {
      result = FormatString(L"%s\\Team MediaPortal\\MediaPortal TV Server\\%s", folder, filePath);
    }
  }

  FREE_MEM(folder);

  return result;
}

wchar_t *GetMediaPortalFolder(void)
{
  return GetMediaPortalFilePath(NULL);
}

wchar_t *GetMediaPortalFilePath(const TCHAR *filePath)
{
  wchar_t *result = NULL;
  ALLOC_MEM_DEFINE_SET(folder, wchar_t, MAX_PATH, 0);
  if (folder != NULL)
  {
    // get common application data folder
    SHGetSpecialFolderPath(NULL, folder, CSIDL_COMMON_APPDATA, FALSE);

    if (filePath == NULL)
    {
      result = FormatString(L"%s\\Team MediaPortal\\MediaPortal\\", folder);
    }
    else
    {
      result = FormatString(L"%s\\Team MediaPortal\\MediaPortal\\%s", folder, filePath);
    }
  }

  FREE_MEM(folder);

  return result;
}

void ZeroURL(URL_COMPONENTS *url) 
{
  url->lpszScheme = NULL;
  url->lpszExtraInfo = NULL;
  url->lpszHostName = NULL;
  url->lpszPassword = NULL;
  url->lpszUrlPath = NULL;
  url->lpszUserName = NULL;

  url->dwSchemeLength = 1;
  url->dwHostNameLength = 1;
  url->dwUrlPathLength = 1;
  url->dwExtraInfoLength = 1;
  url->dwPasswordLength = 1;
  url->dwUrlPathLength = 1;
  url->dwUserNameLength = 1;
}

//TCHAR *GetString(const TCHAR *string, DWORD length, DWORD part)
//{
//  TCHAR *result = NULL;
//  DWORD processed = 0;
//  DWORD count = 0;
//  TCHAR *buffer = (TCHAR *)string;
//  
//  while (processed < length)
//  {
//    if (count == part)
//    {
//      result = buffer;
//      break;
//    }
//
//    processed += _tcslen(buffer) + 1;
//    buffer = (TCHAR *)(string + processed);
//    count++;
//  }
//
//  return result;
//}
//
//DWORD GetStringCount(const TCHAR *string, DWORD length)
//{
//  DWORD result = 0;
//  DWORD processed = 0;
//  TCHAR *buffer = (TCHAR *)string;
//  
//  while (processed < length)
//  {
//    processed += _tcslen(buffer) + 1;
//    buffer = (TCHAR *)(string + processed);
//    result++;
//  }
//
//  return result;
//}