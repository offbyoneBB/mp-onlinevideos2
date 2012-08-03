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
  this->length = 0;
  this->parsed = false;
  this->type = NULL;
  this->hasExtendedHeader = false;
  this->hasUnspecifiedSize = false;
}

CBox::~CBox(void)
{
  FREE_MEM(this->type);
}

/* get methods */

uint64_t CBox::GetSize(void)
{
  return (this->length == 0) ? this->GetBoxSize(0) : this->length;
}

const wchar_t *CBox::GetType(void)
{
  return this->type;
}

bool CBox::GetBox(uint8_t **buffer, uint32_t *length)
{
  bool result = ((buffer != NULL) && (length != NULL) && (!this->IsBigSize()));

  if (result)
  {
    ALLOC_MEM_DEFINE_SET(tempBuffer, uint8_t, (uint32_t)this->GetBoxSize(0), 0);
    result &= (tempBuffer != NULL);

    if (result)
    {
      uint32_t position = 0;
      WBE32INC(tempBuffer, position, (uint32_t)this->GetSize());

      char *type = ConvertToMultiByteW(this->GetType());
      result &= (type != NULL);

      if (result)
      {
        memcpy(tempBuffer + position, type, 4);
      }

      FREE_MEM(type);
    }

    if (result)
    {
      *buffer = tempBuffer;
      *length = (uint32_t)this->GetSize();
    }
    else
    {
      FREE_MEM(tempBuffer);
    }
  }

  return result;
}

/* set methods */

/* other methods */

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
  return (this->GetSize() > ((uint64_t)UINT_MAX));
}

bool CBox::IsSizeUnspecifed(void)
{
  return this->hasUnspecifiedSize;
}

bool CBox::HasExtendedHeader(void)
{
  return this->hasExtendedHeader;
}

bool CBox::Parse(const uint8_t *buffer, uint32_t length)
{
  this->length = 0;
  this->parsed = false;
  FREE_MEM(this->type);
  this->hasExtendedHeader = false;

  if ((buffer != NULL) && (length >= BOX_HEADER_LENGTH))
  {
    uint64_t size = RBE32(buffer, 0);

    if (size == 1)
    {
      // the actual size is in the field largesize (after BOX_HEADER_LENGTH int(64))
      if (length >= BOX_HEADER_LENGTH_SIZE64)
      {
        // enough data for reading int(64) size
        size = RBE64(buffer, BOX_HEADER_LENGTH);
        this->hasExtendedHeader = true;
      }
    }

    // set length of box
    // if size == 0 then box is the last one in and its contents extend to the end of the file
    this->length = (size == 0) ? length : size;
    this->hasUnspecifiedSize = (size == 0);

    // read box type
    uint8_t *type = ALLOC_MEM_SET(type, uint8_t, 5, 0);
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

uint64_t CBox::GetBoxSize(uint64_t size)
{
  uint64_t result = size + BOX_HEADER_LENGTH;

  if (size > (uint64_t)UINT_MAX)
  {
    // size of box doesn't fit into box header
    size = size - BOX_HEADER_LENGTH + BOX_HEADER_LENGTH_SIZE64;
  }

  return result;
}

HRESULT CBox::GetString(const uint8_t *buffer, uint32_t length, uint32_t startPosition, wchar_t **output, uint32_t *positionAfterString)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, buffer);
  CHECK_POINTER_DEFAULT_HRESULT(result, output);
  CHECK_POINTER_DEFAULT_HRESULT(result, positionAfterString);

  if (SUCCEEDED(result))
  {
    bool foundEnd = false;
    uint32_t tempPosition = startPosition;

    while (tempPosition < length)
    {
      if (RBE8(buffer, tempPosition) == 0)
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
      uint32_t length = tempPosition - startPosition;
      uint8_t *utf8string = ALLOC_MEM_SET(utf8string, uint8_t, length + 1, 0);
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
    result = FormatString(L"%sType: '%s'\n%sSize: %llu\n%sExtended header: %s\n%sUnspecified size: %s", indent, this->type, indent, this->length, indent, this->HasExtendedHeader() ? L"true" : L"false", indent, this->IsSizeUnspecifed() ? L"true" : L"false");
  }

  return result;
}