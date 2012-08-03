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

#include "MovieBox.h"

CMovieBox::CMovieBox(void)
  : CBox()
{
  this->type = Duplicate(MOVIE_BOX_TYPE);
}

CMovieBox::~CMovieBox(void)
{
}

/* get methods */

bool CMovieBox::GetBox(uint8_t **buffer, uint32_t *length)
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

/* set methods */

/* other methods */

bool CMovieBox::Parse(const uint8_t *buffer, uint32_t length)
{
  //bool result = ((this->majorBrand != NULL) && (this->compatibleBrands != NULL));
  bool result = true;
  // in bad case we don't have objects, but still it can be valid box
  result &= __super::Parse(buffer, length);

  if (result)
  {
    if (wcscmp(this->type, MOVIE_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is file type box, parse all values
      uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      bool continueParsing = (((position + 8) <= length) && (this->GetSize() <= length));

      if (continueParsing)
      {
      }

      this->parsed = continueParsing;
    }
  }

  result = this->parsed;

  return result;
}

wchar_t *CMovieBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    //// prepare compatible brands collection
    //wchar_t *compatibleBrands = NULL;
    //wchar_t *tempIndent = FormatString(L"%s\t", indent);
    //for (unsigned int i = 0; i < this->compatibleBrands->Count(); i++)
    //{
    //  CBrand *brand = this->compatibleBrands->GetItem(i);
    //  wchar_t *tempCompatibleBrands = FormatString(
    //    L"%s%s%s'%s'",
    //    (i == 0) ? L"" : compatibleBrands,
    //    (i == 0) ? L"" : L"\n",
    //    tempIndent,
    //    brand->GetBrandString()
    //    );
    //  FREE_MEM(compatibleBrands);

    //  compatibleBrands = tempCompatibleBrands;
    //}

    //// prepare finally human readable representation
    //result = FormatString(
    //  L"%s\n" \
    //  L"Brand: %s\n" \
    //  L"Minor version: %u\n" \
    //  L"Compatible brands:" \
    //  L"%s%s",
    //  
    //  previousResult,
    //  this->majorBrand->GetBrandString(),
    //  this->minorVersion,
    //  (compatibleBrands == NULL) ? L"" : L"\n", (compatibleBrands == NULL) ? L"" : compatibleBrands
    //  );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CMovieBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}