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

#include "VideoMediaHeaderBox.h"

CVideoMediaHeaderBox::CVideoMediaHeaderBox(void)
  : CFullBox()
{
  this->type = Duplicate(VIDEO_MEDIA_HEADER_BOX_TYPE);
  this->graphicsMode = 0;
  this->colorRed = 0;
  this->colorGreen = 0;
  this->colorBlue = 0;
}

CVideoMediaHeaderBox::~CVideoMediaHeaderBox(void)
{
}

/* get methods */

bool CVideoMediaHeaderBox::GetBox(uint8_t **buffer, uint32_t *length)
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

uint16_t CVideoMediaHeaderBox::GetGraphicsMode(void)
{
  return this->graphicsMode;
}

uint16_t CVideoMediaHeaderBox::GetColorRed(void)
{
  return this->colorRed;
}

uint16_t CVideoMediaHeaderBox::GetColorGreen(void)
{
  return this->colorGreen;
}

uint16_t CVideoMediaHeaderBox::GetColorBlue(void)
{
  return this->colorBlue;
}

/* set methods */

/* other methods */

bool CVideoMediaHeaderBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CVideoMediaHeaderBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sGraphics mode: %u\n" \
      L"%sColor red: %u\n" \
      L"%sColor green: %u\n" \
      L"%sColor blue: %u"
      ,
      
      previousResult,
      indent, this->GetGraphicsMode(),
      indent, this->GetColorRed(),
      indent, this->GetColorGreen(),
      indent, this->GetColorBlue()

      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CVideoMediaHeaderBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}

bool CVideoMediaHeaderBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->graphicsMode = 0;
  this->colorRed = 0;
  this->colorGreen = 0;
  this->colorBlue = 0;

  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, VIDEO_MEDIA_HEADER_BOX_TYPE) != 0)
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
        RBE16INC(buffer, position, this->graphicsMode);
        RBE16INC(buffer, position, this->colorRed);
        RBE16INC(buffer, position, this->colorGreen);
        RBE16INC(buffer, position, this->colorBlue);
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