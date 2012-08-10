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

#include "DataEntryBox.h"

CDataEntryBox::CDataEntryBox(void)
  : CFullBox()
{
}

CDataEntryBox::~CDataEntryBox(void)
{
}

/* get methods */

bool CDataEntryBox::GetBox(uint8_t *buffer, uint32_t length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
  }

  return result;
}

bool CDataEntryBox::IsSelfContained(void)
{
  return ((this->flags & FLAGS_SELF_CONTAINED) != 0);
}

/* set methods */

/* other methods */

bool CDataEntryBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CDataEntryBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sSelf contained: %s"
      ,
      
      previousResult,
      indent, this->IsSelfContained() ? L"true" : L"false"

      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CDataEntryBox::GetBoxSize(void)
{
  return __super::GetBoxSize();
}

bool CDataEntryBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  return __super::ParseInternal(buffer, length, processAdditionalBoxes);
}