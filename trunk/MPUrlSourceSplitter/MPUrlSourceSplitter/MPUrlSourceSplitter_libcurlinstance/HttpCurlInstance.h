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

#ifndef __HTTP_CURL_INSTANCE_DEFINED
#define __HTTP_CURL_INSTANCE_DEFINED

#include "CurlInstance.h"

#include <DShow.h>

#define HTTP_VERSION_NONE                                                     0
#define HTTP_VERSION_FORCE_HTTP10                                             1
#define HTTP_VERSION_FORCE_HTTP11                                             2

#define HTTP_VERSION_DEFAULT                                                  HTTP_VERSION_NONE
#define HTTP_IGNORE_CONTENT_LENGTH_DEFAULT                                    false

class CHttpCurlInstance :
  public CCurlInstance
{
public:
  // initializes a new instance of CHttpCurlInstance class
  // @param logger : logger for logging purposes
  // @param mutex : mutex for locking access to receive data buffer
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CHttpCurlInstance(CLogger *logger, HANDLE mutex, const wchar_t *url, const wchar_t *protocolName);
  ~CHttpCurlInstance(void);

  // initializes CURL instance
  // @return : true if successful, false otherwise
  virtual bool Initialize(void);

  // gets start stream time
  // @return : start stream time
  virtual REFERENCE_TIME GetStartStreamTime(void);

  // sets start stream time
  // @param startStreamTime : the start stream time to set
  virtual void SetStartStreamTime(REFERENCE_TIME startStreamTime);

  // gets end stream time
  // @return : end stream time
  REFERENCE_TIME GetEndStreamTime(void);

  // sets end stream time
  // @param endStreamTime : the end stream time to set
  void SetEndStreamTime(REFERENCE_TIME endStreamTime);

  // sets referer
  // @param referer : the referer to set
  void SetReferer(const wchar_t *referer);

  // sets user agent
  // @param user agent : the user agent to set
  void SetUserAgent(const wchar_t *userAgent);

  // sets cookie
  // @param cookie : the cookie to set
  void SetCookie(const wchar_t *cookie);

  // sets HTTP version
  // @param version : the HTTP version to set
  void SetHttpVersion(int version);

  // sets ignore content length
  // @param ignoreContentLength : the ignore content length to set
  void SetIgnoreContentLength(bool ignoreContentLength);

  // gets if ranges are supported
  // @return : true if ranges are supported, false otherwise
  bool GetRangesSupported(void);

  // appends header to HTTP headers
  // @return : true if successful, false otherwise
  bool AppendToHeaders(const wchar_t *header);

  // clears headers
  void ClearHeaders(void);

protected:
  // start stream time and end stream time
  REFERENCE_TIME startStreamTime;
  REFERENCE_TIME endStreamTime;

  // referer header in HTTP request
  wchar_t *referer;

  // user agent header in HTTP request
  wchar_t *userAgent;

  // cookie header in HTTP request
  wchar_t *cookie;

  // the HTTP protocol version
  int version;

  // specifies if CURL have to ignore content length
  bool ignoreContentLength;

  // specifies if ranges are supported
  bool rangesSupported;

  curl_slist *httpHeaders;

  // called when CURL debug message arives
  // @param type : CURL message type
  // @param data : received CURL message data
  virtual void CurlDebug(curl_infotype type, const wchar_t *data);

  // process received data
  // @param buffer : buffer with received data
  // @param length : the length of buffer
  // @return : the length of processed data (lower value than length means error)
  virtual size_t CurlReceiveData(const unsigned char *buffer, size_t length);
};

#endif