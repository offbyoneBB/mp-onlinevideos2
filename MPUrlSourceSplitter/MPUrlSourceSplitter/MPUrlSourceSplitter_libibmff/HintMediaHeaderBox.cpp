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

#include "HintMediaHeaderBox.h"

CHintMediaHeaderBox::CHintMediaHeaderBox(void)
  : CFullBox()
{
  this->type = Duplicate(HINT_MEDIA_HEADER_BOX_TYPE);
  this->averageBitrate = 0;
  this->averagePDUSize = 0;
  this->maxBitrate = 0;
  this->maxPDUSize = 0;
}

CHintMediaHeaderBox::~CHintMediaHeaderBox(void)
{
}

/* get methods */

bool CHintMediaHeaderBox::GetBox(uint8_t **buffer, uint32_t *length)
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

uint16_t CHintMediaHeaderBox::GetMaxPDUSize(void)
{
  return this->maxPDUSize;
}

uint16_t CHintMediaHeaderBox::GetAveragePDUSize(void)
{
  return this->averagePDUSize;
}

uint32_t CHintMediaHeaderBox::GetMaxBitrate(void)
{
  return this->maxBitrate;
}

uint32_t CHintMediaHeaderBox::GetAverageBitrate(void)
{
  return this->averageBitrate;
}

/* set methods */

/* other methods */

bool CHintMediaHeaderBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CHintMediaHeaderBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sMax PDU size: %u\n" \
      L"%sAverage PDU size: %u\n" \
      L"%sMax bitrate: %u\n" \
      L"%sAverage bitrate: %u"
      ,
      
      previousResult,
      indent, this->GetMaxPDUSize(),
      indent, this->GetAveragePDUSize(),
      indent, this->GetMaxBitrate(),
      indent, this->GetAverageBitrate()

      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CHintMediaHeaderBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}

bool CHintMediaHeaderBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->averageBitrate = 0;
  this->averagePDUSize = 0;
  this->maxBitrate = 0;
  this->maxPDUSize = 0;

  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, HINT_MEDIA_HEADER_BOX_TYPE) != 0)
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
        RBE16INC(buffer, position, this->maxPDUSize);
        RBE16INC(buffer, position, this->averagePDUSize);
        RBE32INC(buffer, position, this->maxBitrate);
        RBE32INC(buffer, position, this->averageBitrate);
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