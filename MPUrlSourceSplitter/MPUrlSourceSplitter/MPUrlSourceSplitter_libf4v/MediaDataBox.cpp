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
  this->playloadSize = -1;
}

CMediaDataBox::~CMediaDataBox(void)
{
  FREE_MEM(this->payload);
}

bool CMediaDataBox::Parse(const unsigned char *buffer, unsigned int length)
{
  FREE_MEM(this->payload);
  this->playloadSize = -1;

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
      unsigned int position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      this->playloadSize = this->GetSize() - position;

      if ((position + this->playloadSize) <= length)
      {
        this->payload = ALLOC_MEM_SET(this->payload, unsigned char, (unsigned int)this->playloadSize, 0);
        if (this->payload != NULL)
        {
          memcpy(this->payload, buffer + position, (unsigned int)this->playloadSize);
        }
        else
        {
          this->parsed = false;
        }
      }
      else
      {
        this->parsed = false;
      }
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
    result = FormatString(L"%s\nPayload size: %lld", previousResult, this->playloadSize);
  }

  FREE_MEM(previousResult);

  return result;
}

const unsigned char *CMediaDataBox::GetPayload(void)
{
  return this->payload;
}

int64_t CMediaDataBox::GetPayloadSize(void)
{
  return this->playloadSize;
}