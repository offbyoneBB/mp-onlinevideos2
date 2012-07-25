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

#ifndef __BOOTSTRAP_INFO_DEFINED
#define __BOOTSTRAP_INFO_DEFINED

#include "MPUrlSourceSplitter_Parser_F4M_Exports.h"

class MPURLSOURCESPLITTER_PARSER_F4M_API CBootstrapInfo
{
public:
  // constructor
  // create instance of CBootstrapInfo class
  // @param id : the ID of bootstrap info
  // @param profile : the profile name of bootstrap info
  // @param url : the URL of bootstrap info (have to be NULL if value specified)
  // @param value : the BASE64 encoded value (have to be NULL if url specified)
  CBootstrapInfo(wchar_t *id, wchar_t *profile, wchar_t *url, wchar_t *value);

  // destructor
  ~CBootstrapInfo(void);

  // tests if instance is valid
  // @return : true if instance is valid
  bool IsValid(void);

  // tests if for bootstrap info is specified URL
  // @return : true if URL is specified
  bool HasUrl(void);

  // tests if for bootstrap info is specified value
  // @return : true if value is specified
  bool HasValue(void);

  // gets bootstrap info ID
  // @return : bootstrap info ID or NULL if error
  const wchar_t *GetId(void);

  // gets bootstrap info profile name
  // @return : bootstrap info profile name or NULL if error
  const wchar_t *GetProfile(void);

  // gets bootstrap info url
  // @return : bootstrap info url or NULL if value is specified
  const wchar_t *GetUrl(void);

  // gets bootstrap info BASE64 encoded value
  // @return : bootstrap info BASE64 encoded value or NULL if url is specified
  const wchar_t *GetValue(void);

  // gets decoding result of BASE64 encoded value
  // @return : E_NOT_VALID_STATE if value is NULL or result from base64_decode() method
  HRESULT GetDecodeResult(void);

  // gets decoded value
  // @return : decoded value of NULL if error
  const unsigned char *GetDecodedValue(void);

  // gets decoded value length
  // @return : decoded value length, UINT_MAX if error
  unsigned int GetDecodedValueLength(void);

private:
  // stores bootstrap info ID
  wchar_t *id;
  // stores profile name
  wchar_t *profile;
  // stores bootstrap info URL (NULL if value specified)
  wchar_t *url;
  // stores bootstrap raw value (NULL if url specified)
  wchar_t *value;
  // stores result of BASE64 decoding
  HRESULT decodeResult;
  // stores decoded value
  unsigned char *decodedValue;
  // stores length of decoded value
  unsigned int decodedLength;
};

#endif

