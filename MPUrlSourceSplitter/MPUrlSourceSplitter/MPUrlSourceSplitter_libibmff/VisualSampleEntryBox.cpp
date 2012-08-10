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

#include "VisualSampleEntryBox.h"

CVisualSampleEntryBox::CVisualSampleEntryBox(void)
  : CSampleEntryBox()
{
  this->width = 0;
  this->height = 0;
  this->horizontalResolution = new CFixedPointNumber(16, 16);
  this->verticalResolution = new CFixedPointNumber(16, 16);
  this->frameCount = 0;
  this->compressorName = NULL;
  this->depth = 0;
}

CVisualSampleEntryBox::~CVisualSampleEntryBox(void)
{
  FREE_MEM_CLASS(this->horizontalResolution);
  FREE_MEM_CLASS(this->verticalResolution);
  FREE_MEM(this->compressorName);
}

/* get methods */

bool CVisualSampleEntryBox::GetBox(uint8_t *buffer, uint32_t length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
  }

  return result;
}

const wchar_t *CVisualSampleEntryBox::GetCodingName(void)
{
  return this->GetType();
}

uint16_t CVisualSampleEntryBox::GetWidth(void)
{
  return this->width;
}

uint16_t CVisualSampleEntryBox::GetHeight(void)
{
  return this->height;
}

CFixedPointNumber *CVisualSampleEntryBox::GetHorizontalResolution(void)
{
  return this->horizontalResolution;
}

CFixedPointNumber *CVisualSampleEntryBox::GetVerticalResolution(void)
{
  return this->verticalResolution;
}

uint16_t CVisualSampleEntryBox::GetFrameCount(void)
{
  return this->frameCount;
}

const wchar_t *CVisualSampleEntryBox::GetCompressorName(void)
{
  return this->compressorName;
}

uint16_t CVisualSampleEntryBox::GetDepth(void)
{
  return this->depth;
}

/* set methods */

/* other methods */

bool CVisualSampleEntryBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CVisualSampleEntryBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sCoding name: '%s'\n" \
      L"%sWidth: %u\n" \
      L"%sHeight: %u\n" \
      L"%sHorizontal resolution: %u.%u\n" \
      L"%sVertical resolution: %u.%u\n" \
      L"%sFrame count: %u\n" \
      L"%sCompressor: '%s'\n" \
      L"%sDepth: %u"
      ,
      
      previousResult,
      indent, this->GetCodingName(),
      indent, this->GetWidth(),
      indent, this->GetHeight(),
      indent, this->GetHorizontalResolution()->GetIntegerPart(), this->GetHorizontalResolution()->GetFractionPart(),
      indent, this->GetVerticalResolution()->GetIntegerPart(), this->GetVerticalResolution()->GetFractionPart(),
      indent, this->GetFrameCount(),
      indent, this->GetCompressorName(),
      indent, this->GetDepth()
      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CVisualSampleEntryBox::GetBoxSize(void)
{
  return __super::GetBoxSize();
}

bool CVisualSampleEntryBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  FREE_MEM_CLASS(this->horizontalResolution);
  FREE_MEM_CLASS(this->verticalResolution);
  FREE_MEM(this->compressorName);

  this->width = 0;
  this->height = 0;
  this->horizontalResolution = new CFixedPointNumber(16, 16);
  this->verticalResolution = new CFixedPointNumber(16, 16);
  this->frameCount = 0;
  this->compressorName = NULL;
  this->depth = 0;

  bool result = ((this->horizontalResolution != NULL) && (this->verticalResolution != NULL));
  result &= __super::ParseInternal(buffer, length, false);

  if (result)
  {
    // box is file type box, parse all values
    uint32_t position = this->HasExtendedHeader() ? SAMPLE_ENTRY_BOX_HEADER_LENGTH_SIZE64 : SAMPLE_ENTRY_BOX_HEADER_LENGTH;
    bool continueParsing = (this->GetSize() <= (uint64_t)length);

    if (continueParsing)
    {
      // skip 16 reserved and pre-defined bytes
      position += 16;

      RBE16INC(buffer, position, this->width);
      RBE16INC(buffer, position, this->height);

      continueParsing &= this->horizontalResolution->SetNumber(RBE32(buffer, position));
      position += 4;

      continueParsing &= this->verticalResolution->SetNumber(RBE32(buffer, position));
      position += 4;

      // skip 4 reserved bytes
      position += 4;

      RBE16INC(buffer, position, this->frameCount);

      RBE8INC_DEFINE(buffer, position, compressorNameLength, uint8_t);
      
      uint32_t positionAfterString = 0;
      continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &this->compressorName, &positionAfterString));
      position += 31;

      RBE16INC(buffer, position, this->depth);

      // skip 2 pre-defined bytes
      position += 2;

      // optional clean aperture box

      // optional pixel aspect ratio box

    }

    if (continueParsing && processAdditionalBoxes)
    {
      this->ProcessAdditionalBoxes(buffer, length, position);
    }

    this->parsed = continueParsing;
  }

  result = this->parsed;

  return result;
}