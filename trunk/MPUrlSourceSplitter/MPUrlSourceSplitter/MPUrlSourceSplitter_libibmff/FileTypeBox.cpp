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

#include "FileTypeBox.h"

CFileTypeBox::CFileTypeBox(void)
  : CBox()
{
  this->majorBrand = new CBrand();
  this->compatibleBrands = new CBrandCollection();
}

CFileTypeBox::~CFileTypeBox(void)
{
  FREE_MEM_CLASS(this->majorBrand);
  FREE_MEM_CLASS(this->compatibleBrands);
}

/* get methods */

CBrand *CFileTypeBox::GetMajorBrand(void)
{
  return this->majorBrand;
}

unsigned int CFileTypeBox::GetMinorVersion(void)
{
  return this->minorVersion;
}

CBrandCollection *CFileTypeBox::GetCompatibleBrands(void)
{
  return this->compatibleBrands;
}

/* set methods */

/* other methods */

bool CFileTypeBox::Parse(const unsigned char *buffer, unsigned int length)
{
  FREE_MEM_CLASS(this->majorBrand);
  FREE_MEM_CLASS(this->compatibleBrands);

  this->majorBrand = new CBrand();
  this->compatibleBrands = new CBrandCollection();

  bool result = ((this->majorBrand != NULL) && (this->compatibleBrands != NULL));
  // in bad case we don't have objects, but still it can be valid box
  result &= __super::Parse(buffer, length);

  if (result)
  {
    if (wcscmp(this->type, FILE_TYPE_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is file type box, parse all values
      unsigned int position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      bool continueParsing = (((position + 8) <= length) && (this->GetSize() <= length));

      if (continueParsing)
      {
        // major brand + minor version = 8 bytes
        continueParsing &= this->majorBrand->SetBrand(RBE32(buffer, position));
        position += 4;

        RBE32INC(buffer, position, this->minorVersion);
      }

      if (continueParsing)
      {
        // the last bytes in file type box are compatible brands (each 4 characters)
        while (continueParsing && ((position + 3) < (unsigned int)this->GetSize()))
        {
          CBrand *brand = new CBrand();
          continueParsing &= (brand != NULL);

          if (continueParsing)
          {
            continueParsing &= brand->SetBrand(RBE32(buffer, position));
            if (continueParsing)
            {
              continueParsing &= this->compatibleBrands->Add(brand);
            }
          }

          if (!continueParsing)
          {
            FREE_MEM_CLASS(brand);
          }

          position += 4;
        }
      }
      
      this->parsed = continueParsing;
    }
  }

  result = this->parsed;

  return result;
}

wchar_t *CFileTypeBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare compatible brands collection
    wchar_t *compatibleBrands = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->compatibleBrands->Count(); i++)
    {
      CBrand *brand = this->compatibleBrands->GetItem(i);
      wchar_t *tempCompatibleBrands = FormatString(
        L"%s%s%s'%s'",
        (i == 0) ? L"" : compatibleBrands,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        brand->GetBrandString()
        );
      FREE_MEM(compatibleBrands);

      compatibleBrands = tempCompatibleBrands;
    }

    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"Brand: %s\n" \
      L"Minor version: %u\n" \
      L"Compatible brands:" \
      L"%s%s",
      
      previousResult,
      this->majorBrand->GetBrandString(),
      this->minorVersion,
      (compatibleBrands == NULL) ? L"" : L"\n", (compatibleBrands == NULL) ? L"" : compatibleBrands
      );
  }

  FREE_MEM(previousResult);

  return result;
}