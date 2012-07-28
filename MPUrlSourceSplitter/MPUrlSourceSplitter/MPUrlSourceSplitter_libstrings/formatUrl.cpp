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

#include "formatUrl.h"

#include <WinInet.h>
#include <stdio.h>

extern void ZeroURL(URL_COMPONENTS *url);

// gets base URL without last '/'
// @param url : URL to get base url
// @return : base URL or NULL if error
wchar_t *GetBaseUrl(wchar_t *url)
{
  wchar_t *result = NULL;

  ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
  if ((urlComponents != NULL) && (url != NULL))
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    if (InternetCrackUrl(url, 0, 0, urlComponents))
    {
      // if URL path is not specified, than whole URL is base URL
      if (urlComponents->dwUrlPathLength != 0)
      {
        // find last '/'
        // before it is base URL
        const wchar_t *last = wcsrchr(url, L'/');
        unsigned int length = (last - url);

        result = ALLOC_MEM_SET(result, wchar_t, (length + 1), 0);
        if (result != NULL)
        {
          wcsncpy_s(result, length + 1, url, length);
        }
      }
    }
  }
  FREE_MEM(urlComponents);

  return result;
}

// tests if URL is absolute
// @param url : URL to test
// @return : true if URL is absolute, false otherwise or if error
bool IsAbsoluteUrl(wchar_t *url)
{
  bool result = false;

  ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
  if ((urlComponents != NULL) && (url != NULL))
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    if (InternetCrackUrl(url, 0, 0, urlComponents))
    {
      result = true;
    }
  }
  FREE_MEM(urlComponents);

  return result;
}

// gets absolute URL combined from base URL and relative URL
// if relative URL is absolute, then duplicate of relative URL is returned
// @param baseUrl : base URL for combining, URL have to be without last '/'
// @param relativeUrl : relative URL for combinig
// @return : absolute URL or NULL if error
wchar_t *FormatAbsoluteUrl(wchar_t *baseUrl, wchar_t *relativeUrl)
{
  wchar_t *result = NULL;

  if ((baseUrl != NULL) && (relativeUrl != NULL))
  {
    if (IsAbsoluteUrl(relativeUrl))
    {
      result = DuplicateW(relativeUrl);
    }
    else
    {
      // URL is concatenation of base URL and relative URL
      unsigned int baseUrlLength = wcslen(baseUrl);
      unsigned int relativeUrlLength = wcslen(relativeUrl);
      // we need one extra character for '/' between base URL and relative URL
      unsigned int length = baseUrlLength + relativeUrlLength + 1;

      if (wcsncmp(relativeUrl, L"/", 1) == 0)
      {
        // the first character is '/'
        length--;
        relativeUrl++;
      }

      result = ALLOC_MEM_SET(result, wchar_t, (length + 1), 0);
      if (result != NULL)
      {
        wcscat_s(result, length + 1, baseUrl);
        wcscat_s(result, length + 1, L"/");
        wcscat_s(result, length + 1, relativeUrl);
      }
    }
  }

  return result;
}

// gets absolute base URL combined from base URL and relative URL
// @param baseUrl : base URL for combining, URL have to be without last '/'
// @param relativeUrl : relative URL for combinig, URL have to be without start '/'
// @return : absolute base URL or NULL if error
wchar_t *FormatAbsoluteBaseUrl(wchar_t *baseUrl, wchar_t *relativeUrl)
{
  wchar_t *result = NULL;
  wchar_t *absoluteUrl = FormatAbsoluteUrl(baseUrl, relativeUrl);

  result = GetBaseUrl(absoluteUrl);
  FREE_MEM(absoluteUrl);

  return result;
}