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

#include "TrackFragmentHeaderBox.h"

CTrackFragmentHeaderBox::CTrackFragmentHeaderBox(void)
  : CFullBox()
{
  this->type = Duplicate(TRACK_FRAGMENT_HEADER_BOX_TYPE);
  this->trackId = 0;
  this->baseDataOffset = 0;
  this->sampleDescriptionIndex = 0;
  this->defaultSampleDuration = 0;
  this->defaultSampleFlags = 0;
  this->defaultSampleSize = 0;
}

CTrackFragmentHeaderBox::~CTrackFragmentHeaderBox(void)
{
}

/* get methods */

bool CTrackFragmentHeaderBox::GetBox(uint8_t **buffer, uint32_t *length)
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

uint32_t CTrackFragmentHeaderBox::GetTrackId(void)
{
  return this->trackId;
}

uint64_t CTrackFragmentHeaderBox::GetBaseDataOffset(void)
{
  return this->baseDataOffset;
}

uint32_t CTrackFragmentHeaderBox::GetSampleDescriptionIndex(void)
{
  return this->sampleDescriptionIndex;
}

uint32_t CTrackFragmentHeaderBox::GetDefaultSampleDuration(void)
{
  return this->defaultSampleDuration;
}

uint32_t CTrackFragmentHeaderBox::GetDefaultSampleSize(void)
{
  return this->defaultSampleSize;
}

uint32_t CTrackFragmentHeaderBox::GetDefaultSampleFlags(void)
{
  return this->defaultSampleFlags;
}

/* set methods */

/* other methods */

bool CTrackFragmentHeaderBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CTrackFragmentHeaderBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sTrack ID: %u\n" \
      L"%sBase data offset: present: %s value: %llu\n" \
      L"%sSample description index: present: %s value: %u\n" \
      L"%sDefault sample duration: present: %s value: %u\n" \
      L"%sDefault sample size: present: %s value: %u\n" \
      L"%sDefault sample flags: present: %s value: 0x%08X"
      ,
      
      previousResult,
      indent, this->GetTrackId(),
      indent, this->IsBaseDataOffsetPresent() ? L"true" : L"false", this->GetBaseDataOffset(),
      indent, this->IsSampleDescriptionIndexPresent() ? L"true" : L"false", this->GetSampleDescriptionIndex(),
      indent, this->IsDefaultSampleDurationPresent() ? L"true" : L"false", this->GetDefaultSampleDuration(),
      indent, this->IsDefaultSampleSizePresent() ? L"true" : L"false", this->GetDefaultSampleSize(),
      indent, this->IsDefaultSampleFlagsPresent() ? L"true" : L"false", this->GetDefaultSampleFlags()
      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CTrackFragmentHeaderBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}

bool CTrackFragmentHeaderBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->trackId = 0;
  this->baseDataOffset = 0;
  this->sampleDescriptionIndex = 0;
  this->defaultSampleDuration = 0;
  this->defaultSampleFlags = 0;
  this->defaultSampleSize = 0;

  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, TRACK_FRAGMENT_HEADER_BOX_TYPE) != 0)
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
        RBE32INC(buffer, position, this->trackId);
        if (this->IsBaseDataOffsetPresent())
        {
          RBE64INC(buffer, position, this->baseDataOffset);
        }
        if (this->IsSampleDescriptionIndexPresent())
        {
          RBE32INC(buffer, position, this->sampleDescriptionIndex);
        }
        if (this->IsDefaultSampleDurationPresent())
        {
          RBE32INC(buffer, position, this->defaultSampleDuration);
        }
        if (this->IsDefaultSampleSizePresent())
        {
          RBE32INC(buffer, position, this->defaultSampleSize);
        }
        if (this->IsDefaultSampleFlagsPresent())
        {
          RBE32INC(buffer, position, this->defaultSampleFlags);
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

bool CTrackFragmentHeaderBox::IsBaseDataOffsetPresent(void)
{
  return ((this->GetFlags() & FLAGS_BASE_DATA_OFFSET_PRESENT) != 0);
}

bool CTrackFragmentHeaderBox::IsSampleDescriptionIndexPresent(void)
{
  return ((this->GetFlags() & FLAGS_SAMPLE_DESCRIPTION_INDEX_PRESENT) != 0);
}

bool CTrackFragmentHeaderBox::IsDefaultSampleDurationPresent(void)
{
  return ((this->GetFlags() & FLAGS_DEFAULT_SAMPLE_DURATION_PRESENT) != 0);
}

bool CTrackFragmentHeaderBox::IsDefaultSampleSizePresent(void)
{
  return ((this->GetFlags() & FLAGS_DEFAULT_SAMPLE_SIZE_PRESENT) != 0);
}

bool CTrackFragmentHeaderBox::IsDefaultSampleFlagsPresent(void)
{
  return ((this->GetFlags() & FLAGS_DEFAULT_SAMPLE_FLAGS_PRESENT) != 0);
}

bool CTrackFragmentHeaderBox::IsDurationIsEmpty(void)
{
  return ((this->GetFlags() & FLAGS_DURATION_IS_EMPTY) != 0);
}