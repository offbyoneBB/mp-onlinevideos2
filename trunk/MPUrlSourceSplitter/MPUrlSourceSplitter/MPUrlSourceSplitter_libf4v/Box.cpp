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

#include "Box.h"

CBox::CBox(void)
{
  this->length = -1;
  this->parsed = false;
  this->type = NULL;
  this->hasExtendedHeader = false;
}

CBox::~CBox(void)
{
  FREE_MEM(this->type);
}

bool CBox::IsBox(void)
{
  return ((this->length >= BOX_HEADER_LENGTH) && (this->type != NULL));
}

bool CBox::IsParsed(void)
{
  return this->parsed;
}

bool CBox::IsBigSize(void)
{
  return (this->length > ((int64_t)UINT_MAX));
}

bool CBox::IsSizeUnspecifed(void)
{
  return (this->length == 0);
}

bool CBox::HasExtendedHeader(void)
{
  return this->hasExtendedHeader;
}

int64_t CBox::GetSize(void)
{
  return this->length;
}

const wchar_t *CBox::GetType(void)
{
  return this->type;
}

bool CBox::Parse(const unsigned char *buffer, unsigned int length)
{
  this->length = -1;
  this->parsed = false;
  FREE_MEM(this->type);
  this->hasExtendedHeader = false;

  if ((buffer != NULL) && (length >= BOX_HEADER_LENGTH))
  {
    int64_t size = BE32(buffer);

    if (size == 1)
    {
      // the actual size is in the field largesize (after BOX_HEADER_LENGTH int(64))
      if (length >= BOX_HEADER_LENGTH_SIZE64)
      {
        // enough data for reading int(64) size
        size = BE64(buffer + BOX_HEADER_LENGTH);
        this->hasExtendedHeader = true;
      }
    }

    // set length of box
    // if size == 0 then box is the last one in and its contents extend to the end of the file
    this->length = size;

    // read box type
    unsigned char *type = ALLOC_MEM_SET(type, unsigned char, 5, 0);
    if (type != NULL)
    {
      // copy 4 chars after size field
      memcpy(type, buffer + 4, 4);
    }

    this->type = ConvertToUnicodeA((char *)type);
    if (this->type != NULL)
    {
      this->parsed = true;
    }
  }

  return this->parsed;
}

HRESULT CBox::GetString(const unsigned char *buffer, unsigned int length, unsigned int startPosition, wchar_t **output, unsigned int *positionAfterString)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, buffer);
  CHECK_POINTER_DEFAULT_HRESULT(result, output);
  CHECK_POINTER_DEFAULT_HRESULT(result, positionAfterString);

  if (SUCCEEDED(result))
  {
    bool foundEnd = false;
    unsigned int tempPosition = startPosition;

    while (tempPosition < length)
    {
      if (BE8(buffer + tempPosition) == 0)
      {
        // null terminating character
        foundEnd = true;
        break;
      }
      else
      {
        tempPosition++;
      }
    }

    result = (foundEnd) ? S_OK : HRESULT_FROM_WIN32(ERROR_INVALID_DATA);

    if (SUCCEEDED(result))
    {
      // if foundEnd is true then in tempPosition is positon of null terminating character
      unsigned int length = tempPosition - startPosition;
      unsigned char *utf8string = ALLOC_MEM_SET(utf8string, unsigned char, length + 1, 0);
      CHECK_POINTER_HRESULT(result, utf8string, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        // copy value from buffer and convert it into wchar_t (Unicode)
        memcpy(utf8string, buffer + startPosition, length);

        *output = ConvertUtf8ToUnicode((char *)utf8string);
        *positionAfterString = tempPosition + 1;
        CHECK_POINTER_HRESULT(result, *output, result, E_OUTOFMEMORY);
      }
    }
  }

  return result;
}

wchar_t *CBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;

  if (this->IsBox())
  {
    result = FormatString(L"%sType: '%s'\n%sSize: %lld\n%sExtended header: %s", indent, this->type, indent, this->length, indent, this->HasExtendedHeader() ? L"true" : L"false");
  }

  return result;
}