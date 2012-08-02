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

#include "FullBox.h"

CFullBox::CFullBox(void)
  : CBox()
{
  this->version = 0;
  this->flags = 0;
}

CFullBox::~CFullBox(void)
{
}

/* get methods */

unsigned int CFullBox::GetVersion(void)
{
  return this->version;
}

unsigned int CFullBox::GetFlags(void)
{
  return this->flags;
}

/* set methods */

/* other methods */

bool CFullBox::Parse(const unsigned char *buffer, unsigned int length)
{
  this->version = 0;
  this->flags = 0;

  bool result = __super::Parse(buffer, length);

  if (result)
  {
    // box is file type box, parse all values
    unsigned int position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
    bool continueParsing = ((position + 4) <= min(length, (unsigned int)this->GetSize()));

    if (continueParsing)
    {
      RBE8INC(buffer, position, this->version);
      RBE24INC(buffer, position, this->flags);
    }

    this->parsed = continueParsing;
  }

  result = this->parsed;

  return result;
}

wchar_t *CFullBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sVersion: %u\n" \
      L"%sFlags: 0x%06X",
      
      previousResult,
      indent, this->version,
      indent, this->flags
      );
  }

  FREE_MEM(previousResult);

  return result;
}