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

#include "BootstrapInfo.h"

#include "base64.h"
#include "Utilities.h"

CBootstrapInfo::CBootstrapInfo(wchar_t *id, wchar_t *profile, wchar_t *url, wchar_t *value)
{
  this->id = Duplicate(id);
  this->profile = Duplicate(profile);
  this->url = Duplicate(url);
  this->value = Duplicate(value);

  this->decodedLength = UINT_MAX;
  this->decodedValue = NULL;
  this->decodeResult = E_NOT_VALID_STATE;
}

CBootstrapInfo::~CBootstrapInfo(void)
{
  FREE_MEM(this->url);
  FREE_MEM(this->profile);
  FREE_MEM(this->url);
  FREE_MEM(this->value);

  FREE_MEM(this->decodedValue);
}

bool CBootstrapInfo::IsValid(void)
{
  return ((this->id != NULL) && (this->profile != NULL) && (((this->url != NULL) && (this->value == NULL)) || ((this->url == NULL) && (this->value != NULL))));
}

bool CBootstrapInfo::HasUrl(void)
{
  return (this->url != NULL);
}

bool CBootstrapInfo::HasValue(void)
{
  return (this->value != NULL);
}

const wchar_t *CBootstrapInfo::GetId(void)
{
  return this->id;
}

const wchar_t *CBootstrapInfo::GetProfile(void)
{
  return this->profile;
}

const wchar_t *CBootstrapInfo::GetUrl(void)
{
  return this->url;
}

const wchar_t *CBootstrapInfo::GetValue(void)
{
  return this->value;
}

HRESULT CBootstrapInfo::GetDecodeResult(void)
{
  HRESULT result = this->decodeResult;

  if ((this->value != NULL) && (result == E_NOT_VALID_STATE))
  {
    // no conversion occured until now
    result = S_OK;

    char *val = ConvertToMultiByteW(this->value);
    CHECK_POINTER_HRESULT(result, val, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      result = base64_decode(val, &this->decodedValue, &this->decodedLength);
    }

    FREE_MEM(val);
    if (FAILED(result))
    {
      this->decodedLength = UINT_MAX;
    }
  }

  return result;
}

const unsigned char *CBootstrapInfo::GetDecodedValue(void)
{
  return this->decodedValue;
}

unsigned int CBootstrapInfo::GetDecodedValueLength(void)
{
  return this->decodedLength;
}
