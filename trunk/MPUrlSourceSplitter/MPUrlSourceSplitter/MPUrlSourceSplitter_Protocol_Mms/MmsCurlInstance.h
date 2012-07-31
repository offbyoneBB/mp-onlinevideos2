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

#ifndef __MMS_CURL_INSTANCE_DEFINED
#define __MMS_CURL_INSTANCE_DEFINED

#include "HttpCurlInstance.h"

class CMmsCurlInstance :
  public CHttpCurlInstance
{
public:
  // initializes a new instance of CMmsCurlInstance class
  // @param logger : logger for logging purposes
  // @param url : the url to open
  // @param protocolName : the protocol name instantiating
  CMmsCurlInstance(CLogger *logger, const wchar_t *url, const wchar_t *protocolName);

  // destructor
  virtual ~CMmsCurlInstance(void);

  // initializes CURL instance
  // @return : true if successful, false otherwise
  virtual bool Initialize(void);

protected:
  // called when CURL debug message arives
  // @param type : CURL message type
  // @param data : received CURL message data
  virtual void CurlDebug(curl_infotype type, const wchar_t *data);

};

#endif