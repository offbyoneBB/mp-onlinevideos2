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

// test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <stdio.h>
#include <stdint.h>

#include <curl/curl.h>

#include "BufferHelper.h"

static void CurlDebug(curl_infotype type, const wchar_t *data)
{
  if (type == CURLINFO_HEADER_OUT)
  {
    wprintf(data);
    wprintf(L"\n");
  }

  if (type == CURLINFO_HEADER_IN)
  {
    wchar_t *trimmed = Trim(data);
    // we are just interested in headers comming in from peer
    wprintf(trimmed);
    wprintf(L"\n");
    FREE_MEM(trimmed);
  }
}

static int CurlDebugCallback(CURL *handle, curl_infotype type, char *data, size_t size, void *userptr)
{
  // warning: data ARE NOT terminated with null character !!
  if (size > 0)
  {
    size_t length = size + 1;
    ALLOC_MEM_DEFINE_SET(tempData, char, length, 0);
    if (tempData != NULL)
    {
      memcpy(tempData, data, size);

      // now convert data to used character set
      wchar_t *curlData = ConvertToUnicodeA(tempData);

      if (curlData != NULL)
      {
        // we have converted and null terminated data
        CurlDebug(type, curlData);
      }

      FREE_MEM(curlData);
    }
    FREE_MEM(tempData);
  }

  return 0;
}

uint8_t *data = NULL;
unsigned int length = 0;

size_t CurlReceiveDataCallback(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  unsigned int total = size * nmemb;
  memcpy(data + length, buffer, total);
  length += total;

  return total;
}

int _tmain(int argc, _TCHAR* argv[])
{
  data = ALLOC_MEM_SET(data, uint8_t, (10 * 1024 * 1024), 0);
  CURL *curl = curl_easy_init();
  CURLcode errorCode = CURLE_OK;
  if (curl != NULL)
  {
    errorCode = curl_easy_setopt(curl, CURLOPT_URL, "http://svtplay2p-f.akamaihd.net/z/se/secure/20121008/1123072-052A/ADVENTURES_OF_B-052A-f22e5dd766c8c310_,900,320,420,620,1660,2760,.mp4.csmil/manifest.f4m?hdcore=2.10.3&g=SSSIYUEGUPSG");
    errorCode = curl_easy_setopt(curl, CURLOPT_COOKIEFILE, "");
    errorCode = curl_easy_setopt(curl, CURLOPT_DEBUGFUNCTION, CurlDebugCallback);
    errorCode = curl_easy_setopt(curl, CURLOPT_DEBUGDATA, NULL);
    errorCode = curl_easy_setopt(curl, CURLOPT_VERBOSE, 1L);
    //errorCode = curl_easy_setopt(curl, CURLOPT_REFERER, "http://www.svtplay.se/public/swf/video/svtplayer-2012.47.swf");
    //errorCode = curl_easy_setopt(curl, CURLOPT_USERAGENT, "Mozilla/5.0 (Windows NT 6.1; rv:16.0) Gecko/20100101 Firefox/16.0");

    errorCode = curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, CurlReceiveDataCallback);
    errorCode = curl_easy_setopt(curl, CURLOPT_WRITEDATA, NULL);

    //errorCode = curl_easy_perform(curl);

    /*memset(data, 0, (10 * 1024 * 1024));
    length = 0;
    errorCode = curl_easy_setopt(curl, CURLOPT_URL, "http://svtplay2p-f.akamaihd.net/serverip");
    errorCode = curl_easy_perform(curl);*/

    memset(data, 0, (10 * 1024 * 1024));
    length = 0;
    errorCode = curl_easy_setopt(curl, CURLOPT_URL, "http://svtplay2p-f.akamaihd.net/z/se/secure/20121008/1123072-052A/ADVENTURES_OF_B-052A-f22e5dd766c8c310_,900,320,420,620,1660,2760,.mp4.csmil/0_c1c56edfe62a70c4_Seg1-Frag1?hdcore=2.10.3&g=SSSIYUEGUPSG");
    errorCode = curl_easy_setopt(curl, CURLOPT_COOKIEFILE, "");
    errorCode = curl_easy_perform(curl);

    unsigned int position = 0;
    unsigned int size = 0;

    while (true)
    {
      size = RBE32(data, position);

      if ((size < 1000) && (size != 0))
      {
        position += size;
      }
      else
      {
        break;
      }
    }

    char *keyUrl = FormatStringA("http://svtplay2p-f.akamaihd.net%s?guid=SSSIYUEGUPSG", data + position + 48);

    memset(data, 0, (10 * 1024 * 1024));
    length = 0;
    errorCode = curl_easy_setopt(curl, CURLOPT_URL, keyUrl);
    errorCode = curl_easy_setopt(curl, CURLOPT_COOKIEFILE, "");
    errorCode = curl_easy_perform(curl);

    FREE_MEM(keyUrl);

    curl_easy_cleanup(curl);
    curl = NULL;
  }

  FREE_MEM(data);
	return 0;
}
