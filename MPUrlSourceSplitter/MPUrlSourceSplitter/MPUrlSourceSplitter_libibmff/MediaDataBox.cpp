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

#include "MediaDataBox.h"

CMediaDataBox::CMediaDataBox(void)
  : CBox()
{
  this->payload = NULL;
  this->payloadSize = 0;
  this->type = Duplicate(MEDIA_DATA_BOX_TYPE);
}

CMediaDataBox::~CMediaDataBox(void)
{
  FREE_MEM(this->payload);
}

/* get methods */

const uint8_t *CMediaDataBox::GetPayload(void)
{
  return this->payload;
}

uint64_t CMediaDataBox::GetPayloadSize(void)
{
  return this->payloadSize;
}

bool CMediaDataBox::GetBox(uint8_t **buffer, uint32_t *length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;

    if (this->GetPayloadSize() > 0)
    {
      memcpy((*buffer) + position, this->GetPayload(), (uint32_t)this->GetPayloadSize());
    }

    if (!result)
    {
      FREE_MEM(*buffer);
      *length = 0;
    }
  }

  return result;
}

/* set methods */

bool CMediaDataBox::SetPayload(const uint8_t *buffer, uint32_t length)
{
  bool result = (buffer != NULL) || (length == 0);
  FREE_MEM(this->payload);
  this->payloadSize = 0;

  if (result)
  {
    if (length > 0)
    {
      this->payload = ALLOC_MEM_SET(this->payload, uint8_t, length, 0);
      result &= (this->payload != NULL);

      if (result)
      {
        memcpy(this->payload, buffer, length);
        this->payloadSize = length;
      }
    }
  }

  return result;
}

/* other methods */

bool CMediaDataBox::Parse(const uint8_t *buffer, uint32_t length)
{
  FREE_MEM(this->payload);
  this->payloadSize = 0;

  bool result = __super::Parse(buffer, length);

  if (result)
  {
    if (wcscmp(this->type, MEDIA_DATA_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is bootstrap info box, parse all values
      uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      this->payloadSize = this->GetSize() - position;
      bool continueParsing = ((position + this->payloadSize) <= length);

      if (continueParsing)
      {
        this->payload = ALLOC_MEM_SET(this->payload, uint8_t, (uint32_t)this->payloadSize, 0);
        continueParsing &= (this->payload != NULL);

        if (continueParsing)
        {
          memcpy(this->payload, buffer + position, (uint32_t)this->payloadSize);
        }
      }
      
      this->parsed = continueParsing;
    }
  }

  result = this->parsed;

  return result;
}

wchar_t *CMediaDataBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sPayload size: %llu",
      
      previousResult,
      indent, this->payloadSize
      
      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CMediaDataBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size + this->payloadSize);
}