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

#include "UuidBox.h"

CUuidBox::CUuidBox(void)
  : CBox()
{
  this->type = Duplicate(UUID_BOX_TYPE);
  this->guid.Data1 = 0;
  this->guid.Data2 = 0;
  this->guid.Data3 = 0;
  this->guid.Data4[0] = 0;
  this->guid.Data4[1] = 0;
  this->guid.Data4[2] = 0;
  this->guid.Data4[3] = 0;
  this->guid.Data4[4] = 0;
  this->guid.Data4[5] = 0;
  this->guid.Data4[6] = 0;
  this->guid.Data4[7] = 0;
}

CUuidBox::~CUuidBox(void)
{
}

/* get methods */

bool CUuidBox::GetBox(uint8_t **buffer, uint32_t *length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;

    if (!result)
    {
      FREE_MEM(*buffer);
      *length = 0;
    }
  }

  return result;
}

GUID CUuidBox::GetGuid(void)
{
  return this->guid;
}

/* set methods */

/* other methods */

bool CUuidBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CUuidBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    wchar_t *guid = ConvertGuidToString(this->GetGuid());
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sGUID: %s"
      ,
      
      previousResult,
      indent, guid
      );

    FREE_MEM(guid);
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CUuidBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}

bool CUuidBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->guid.Data1 = 0;
  this->guid.Data2 = 0;
  this->guid.Data3 = 0;
  this->guid.Data4[0] = 0;
  this->guid.Data4[1] = 0;
  this->guid.Data4[2] = 0;
  this->guid.Data4[3] = 0;
  this->guid.Data4[4] = 0;
  this->guid.Data4[5] = 0;
  this->guid.Data4[6] = 0;
  this->guid.Data4[7] = 0;

  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, UUID_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is file type box, parse all values
      uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      bool continueParsing = (this->GetSize() <= (uint64_t)length);

      if (continueParsing)
      {
        RBE32INC(buffer, position, this->guid.Data1);
        RBE16INC(buffer, position, this->guid.Data2);
        RBE16INC(buffer, position, this->guid.Data3);
        RBE8INC(buffer, position, this->guid.Data4[0]);
        RBE8INC(buffer, position, this->guid.Data4[1]);
        RBE8INC(buffer, position, this->guid.Data4[2]);
        RBE8INC(buffer, position, this->guid.Data4[3]);
        RBE8INC(buffer, position, this->guid.Data4[4]);
        RBE8INC(buffer, position, this->guid.Data4[5]);
        RBE8INC(buffer, position, this->guid.Data4[6]);
        RBE8INC(buffer, position, this->guid.Data4[7]);
      }

      this->parsed = continueParsing;
    }
  }

  result = this->parsed;

  return result;
}