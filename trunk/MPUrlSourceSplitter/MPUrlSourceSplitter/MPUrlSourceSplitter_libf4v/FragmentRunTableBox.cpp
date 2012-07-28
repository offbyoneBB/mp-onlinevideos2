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

#include "FragmentRunTableBox.h"

CFragmentRunTableBox::CFragmentRunTableBox(void)
  :CBox()
{
  this->version = 0;
  this->flags = 0;
  this->timeScale = 0;
  this->qualitySegmentUrlModifiers = new CQualitySegmentUrlModifierCollection();
  this->fragmentRunEntryTable = new CFragmentRunEntryCollection();
}

CFragmentRunTableBox::~CFragmentRunTableBox(void)
{
  FREE_MEM_CLASS(this->qualitySegmentUrlModifiers);
  FREE_MEM_CLASS(this->fragmentRunEntryTable);
}

bool CFragmentRunTableBox::Parse(const unsigned char *buffer, unsigned int length)
{
  this->version = 0;
  this->flags = 0;

  bool result = (this->qualitySegmentUrlModifiers != NULL) && (this->fragmentRunEntryTable != NULL);
  // in bad case we don't have tables, but still it can be valid box
  result &= __super::Parse(buffer, length);

  if (result)
  {
    if (wcscmp(this->type, FRAGMENT_RUN_TABLE_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
      result = false;
    }
    else
    {
      // box is bootstrap info box, parse all values
      unsigned int position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;

      // until time scale end is 8 bytes
      bool continueParsing = ((position + 8) <= length);
      
      if (continueParsing)
      {
        this->version = BE8(buffer + position);
        position++;

        this->flags = BE24(buffer + position);
        position += 3;

        this->timeScale = BE32(buffer + position);
        position += 4;
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // quality entry count and quality segment url modifiers
        unsigned int qualityEntryCount = BE8(buffer + position);
        position++;
        continueParsing &= (position < length);

        for(unsigned int i = 0; continueParsing && (i < qualityEntryCount); i++)
        {
          unsigned int positionAfter = position;
          wchar_t *qualitySegmentUrlModifier = NULL;
          continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &qualitySegmentUrlModifier, &positionAfter));

          if (continueParsing)
          {
            position = positionAfter;

            // create quality segment url modifier item in quality segment url modifier collection
            CQualitySegmentUrlModifier *qualitySegmentUrlModifierEntry = new CQualitySegmentUrlModifier(qualitySegmentUrlModifier);
            continueParsing &= this->qualitySegmentUrlModifiers->Add(qualitySegmentUrlModifierEntry);
          }

          FREE_MEM(qualitySegmentUrlModifier);

          continueParsing &= (position < length);
        }
      }

      continueParsing &= ((position + 4) < length);
      if (continueParsing)
      {
        // fragment run entry count and fragment run entry table
        unsigned int fragmentRunEntryCount = BE32(buffer + position);
        position += 4;

        for(unsigned int i = 0; continueParsing && (i < fragmentRunEntryCount); i++)
        {
          // minimum fragment size is 16 bytes
          // but fragment can be last in buffer
          continueParsing &= ((position + 15 ) < length);

          if (continueParsing)
          {
            unsigned int firstFragment = BE32(buffer + position);
            position += 4;

            uint64_t firstFragmentTimestamp = BE64(buffer + position);
            position += 8;

            unsigned int fragmentDuration = BE32(buffer + position);
            position += 4;

            unsigned int discontinuityIndicator = DISCONTINUITY_INDICATOR_NOT_AVAILABLE;
            if (fragmentDuration == 0)
            {
              continueParsing &= (position < length);

              if (continueParsing)
              {
                discontinuityIndicator = BE8(buffer + position);
                position++;
              }
            }

            CFragmentRunEntry *fragment = new CFragmentRunEntry(firstFragment, firstFragmentTimestamp, fragmentDuration, discontinuityIndicator);
            continueParsing &= this->fragmentRunEntryTable->Add(fragment);
          }
        }
      }

      if (!continueParsing)
      {
        // not correctly parsed
        this->parsed = false;
      }
    }
  }

  result = this->parsed;

  return result;
}

wchar_t *CFragmentRunTableBox::GetParsedHumanReadable(wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare quality segment url modifier collection
    wchar_t *qualitySegmentUrlModifier = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->qualitySegmentUrlModifiers->Count(); i++)
    {
      CQualitySegmentUrlModifier *qualitySegmentUrlModifierEntry = this->qualitySegmentUrlModifiers->GetItem(i);
      wchar_t *tempqualitySegmentUrlModifierEntry = FormatString(
        L"%s%s%s'%s'",
        (i == 0) ? L"" : qualitySegmentUrlModifier,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        qualitySegmentUrlModifierEntry->GetQualitySegmentUrlModifier()
        );
      FREE_MEM(qualitySegmentUrlModifier);

      qualitySegmentUrlModifier = tempqualitySegmentUrlModifierEntry;
    }

    // prepare fragment run entry table
    wchar_t *fragmentRunEntry = NULL;
    for (unsigned int i = 0; i < this->fragmentRunEntryTable->Count(); i++)
    {
      CFragmentRunEntry *fragmentRunEntryEntry = this->fragmentRunEntryTable->GetItem(i);

      wchar_t *tempFragmentRunEntry = FormatString(
        L"%s%s%sFirst fragment: %d, first fragment timestamp: %lld, fragment duration: %d, discontinuity indicator: %d",
        (i == 0) ? L"" : fragmentRunEntry,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        fragmentRunEntryEntry->GetFirstFragment(),
        fragmentRunEntryEntry->GetFirstFragmentTimestamp(),
        fragmentRunEntryEntry->GetFragmentDuration(),
        fragmentRunEntryEntry->GetDiscontinuityIndicator()
        );
      FREE_MEM(fragmentRunEntry);

      fragmentRunEntry = tempFragmentRunEntry;
    }
    FREE_MEM(tempIndent);

    // prepare finally human readable representation
    result = FormatString( \
      L"%s\n" \
      L"%sVersion: %d\n" \
      L"%sFlags: 0x%06X\n"  \
      L"%sTime scale: %d\n" \
      L"%sQuality entry count: %d\n" \
      L"%s%s" \
      L"%sFragment run entry count: %d" \
      L"%s%s",

      previousResult,
      indent, this->version,
      indent, this->flags,
      indent, this->timeScale,
      indent, this->qualitySegmentUrlModifiers->Count(),
      (qualitySegmentUrlModifier == NULL) ? L"" : qualitySegmentUrlModifier, (qualitySegmentUrlModifier == NULL) ? L"" : L"\n",
      indent, this->fragmentRunEntryTable->Count(),
      (fragmentRunEntry == NULL) ? L"" : L"\n", (fragmentRunEntry == NULL) ? L"" : fragmentRunEntry

      );
  }

  FREE_MEM(previousResult);

  return result;
}