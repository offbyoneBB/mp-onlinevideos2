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

#include "MovieExtendsHeaderBox.h"

CMovieExtendsHeaderBox::CMovieExtendsHeaderBox(void)
  : CFullBox()
{
  this->type = Duplicate(MOVIE_EXTENDS_HEADER_BOX_TYPE);
  this->fragmentDuration = 0;
}

CMovieExtendsHeaderBox::~CMovieExtendsHeaderBox(void)
{
}

/* get methods */

bool CMovieExtendsHeaderBox::GetBox(uint8_t *buffer, uint32_t length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
  }

  return result;
}

uint64_t CMovieExtendsHeaderBox::GetFragmentDuration(void)
{
  return this->fragmentDuration;
}

/* set methods */

/* other methods */

bool CMovieExtendsHeaderBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CMovieExtendsHeaderBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sFragment duration: %u"
      ,
      
      previousResult,
      indent, this->GetFragmentDuration()

      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CMovieExtendsHeaderBox::GetBoxSize(void)
{
  return __super::GetBoxSize();
}

bool CMovieExtendsHeaderBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->fragmentDuration = 0;

  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, MOVIE_EXTENDS_HEADER_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is file type box, parse all values
      uint32_t position = this->HasExtendedHeader() ? FULL_BOX_HEADER_LENGTH_SIZE64 : FULL_BOX_HEADER_LENGTH;
      bool continueParsing = (this->GetSize() <= (uint64_t)length);

      if (continueParsing)
      {
        switch (this->GetVersion())
        {
        case 0:
          RBE32INC(buffer, position, this->fragmentDuration);
          break;
        case 1:
          RBE64INC(buffer, position, this->fragmentDuration);
          break;
        default:
          continueParsing = false;
          break;
        }
      }

      if (continueParsing && processAdditionalBoxes)
      {
        this->ProcessAdditionalBoxes(buffer, length, position);
      }

      this->parsed = continueParsing;
    }
  }

  result = this->parsed;

  return result;
}