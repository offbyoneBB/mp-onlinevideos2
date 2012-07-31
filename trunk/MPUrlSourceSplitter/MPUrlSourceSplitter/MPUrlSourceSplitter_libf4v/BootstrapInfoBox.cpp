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

#include "BootstrapInfoBox.h"

CBootstrapInfoBox::CBootstrapInfoBox(void)
  : CBox()
{
  this->version = 0;
  this->flags = 0;
  this->bootstrapInfoVersion = 0;
  this->profile = 0;
  this->live = false;
  this->update = false;
  this->timeScale = 0;
  this->currentMediaTime = 0;
  this->smpteTimeCodeOffset;
  this->movieIdentifier = NULL;
  this->serverEntryTable = new CBootstrapInfoServerEntryCollection();
  this->qualityEntryTable = new CBootstrapInfoQualityEntryCollection();
  this->drmData = NULL;
  this->metaData = NULL;
  this->segmentRunTable = new CSegmentRunTableBoxCollection();
  this->fragmentRunTable = new CFragmentRunTableBoxCollection();
}

CBootstrapInfoBox::~CBootstrapInfoBox(void)
{
  FREE_MEM(this->movieIdentifier);
  FREE_MEM(this->drmData);
  FREE_MEM(this->metaData);
  FREE_MEM_CLASS(this->serverEntryTable);
  FREE_MEM_CLASS(this->qualityEntryTable);
  FREE_MEM_CLASS(this->segmentRunTable);
  FREE_MEM_CLASS(this->fragmentRunTable);
}

bool CBootstrapInfoBox::Parse(const unsigned char *buffer, unsigned int length)
{
  this->version = 0;
  this->flags = 0;
  this->bootstrapInfoVersion = 0;
  this->profile = 0;
  this->live = false;
  this->update = false;
  this->timeScale = 0;
  this->currentMediaTime = 0;
  this->smpteTimeCodeOffset;
  FREE_MEM(this->movieIdentifier);
  if (this->serverEntryTable != NULL)
  {
    this->serverEntryTable->Clear();
  }
  if (this->qualityEntryTable != NULL)
  {
    this->qualityEntryTable->Clear();
  }
  FREE_MEM(this->drmData);
  FREE_MEM(this->metaData);
  if (this->segmentRunTable != NULL)
  {
    this->segmentRunTable->Clear();
  }
  if (this->fragmentRunTable != NULL)
  {
    this->fragmentRunTable->Clear();
  }

  bool result = (this->serverEntryTable != NULL) && (this->qualityEntryTable != NULL) && (this->segmentRunTable != NULL) && (this->fragmentRunTable != NULL);
  // in bad case we don't have tables, but still it can be valid box
  result &= __super::Parse(buffer, length);

  if (result)
  {
    if (wcscmp(this->type, BOOTSTRAP_INFO_BOX_TYPE) != 0)
    {
      // incorect box type
      this->parsed = false;
    }
    else
    {
      // box is bootstrap info box, parse all values
      unsigned int position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;

      // until smpteTimeCodeOffset end is 29 bytes
      bool continueParsing = ((position + 29) <= length);
      
      if (continueParsing)
      {
        this->version = BE8(buffer + position);
        position++;

        this->flags = BE24(buffer + position);
        position += 3;

        this->bootstrapInfoVersion = BE32(buffer + position);
        position += 4;

        unsigned int profileLiveUpdate = BE8(buffer + position);
        position++;

        this->profile = (profileLiveUpdate >> 6);
        this->live = ((profileLiveUpdate & 0x20) != 0);
        this->update = ((profileLiveUpdate & 0x10) != 0);

        this->timeScale = BE32(buffer + position);
        position += 4;

        this->currentMediaTime = BE64(buffer + position);
        position += 8;

        this->smpteTimeCodeOffset = BE64(buffer + position);
        position += 8;
      }

      if (continueParsing)
      {
        unsigned int positionAfter = position;
        continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &this->movieIdentifier, &positionAfter));

        if (continueParsing)
        {
          position = positionAfter;
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // server entry count and server entry table
        unsigned int serverEntryCount = BE8(buffer + position);
        position++;
        continueParsing &= (position < length);

        for(unsigned int i = 0; continueParsing && (i < serverEntryCount); i++)
        {
          unsigned int positionAfter = position;
          wchar_t *serverEntry = NULL;
          continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &serverEntry, &positionAfter));

          if (continueParsing)
          {
            position = positionAfter;

            // create server entry item in server entry table
            CBootstrapInfoServerEntry *bootstrapInfoServerEntry = new CBootstrapInfoServerEntry(serverEntry);
            continueParsing &= (bootstrapInfoServerEntry != NULL);

            if (continueParsing)
            {
              continueParsing &= this->serverEntryTable->Add(bootstrapInfoServerEntry);
            }

            if ((!continueParsing) && (bootstrapInfoServerEntry != NULL))
            {
              // cleanup
              FREE_MEM_CLASS(bootstrapInfoServerEntry);
            }
          }

          FREE_MEM(serverEntry);

          continueParsing &= (position < length);
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // quality entry count and quality entry table
        unsigned int qualityEntryCount = BE8(buffer + position);
        position++;
        continueParsing &= (position < length);

        for(unsigned int i = 0; continueParsing && (i < qualityEntryCount); i++)
        {
          unsigned int positionAfter = position;
          wchar_t *qualityEntry = NULL;
          continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &qualityEntry, &positionAfter));

          if (continueParsing)
          {
            position = positionAfter;

            // create quality entry item in quality entry table
            CBootstrapInfoQualityEntry *bootstrapInfoQualityEntry = new CBootstrapInfoQualityEntry(qualityEntry);
            continueParsing &= (bootstrapInfoQualityEntry != NULL);

            if (continueParsing)
            {
              continueParsing &= this->qualityEntryTable->Add(bootstrapInfoQualityEntry);
            }

            if ((!continueParsing) && (bootstrapInfoQualityEntry != NULL))
            {
              // cleanup
              FREE_MEM_CLASS(bootstrapInfoQualityEntry);
            }
          }

          FREE_MEM(qualityEntry);

          continueParsing &= (position < length);
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // read DRM data string
        unsigned int positionAfter = position;
        continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &this->drmData, &positionAfter));

        if (continueParsing)
        {
          position = positionAfter;
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // read metadata string
        unsigned int positionAfter = position;
        continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &this->metaData, &positionAfter));

        if (continueParsing)
        {
          position = positionAfter;
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // read segment run table count and segment run table
        unsigned int segmentRunTableCount = BE8(buffer + position);
        position++;
        continueParsing &= (position < length);

        for (unsigned int i = 0; continueParsing && (i < segmentRunTableCount); i++)
        {
          CSegmentRunTableBox *segmentRunTableBox = new CSegmentRunTableBox();
          continueParsing &= (segmentRunTableBox != NULL);

          if (continueParsing)
          {
            continueParsing &= segmentRunTableBox->Parse(buffer + position, length - position);

            if (continueParsing)
            {
              continueParsing &= this->segmentRunTable->Add(segmentRunTableBox);

              if (continueParsing)
              {
                position += (unsigned int)segmentRunTableBox->GetSize();
              }
            }
          }

          if ((!continueParsing) && (segmentRunTableBox != NULL))
          {
            // cleanup
            FREE_MEM_CLASS(segmentRunTableBox);
          }
        }
      }

      continueParsing &= (position < length);
      if (continueParsing)
      {
        // read fragment run table count and fragment run table
        unsigned int fragmentRunTableCount = BE8(buffer + position);
        position++;
        continueParsing &= (position < length);

        for (unsigned int i = 0; continueParsing && (i < fragmentRunTableCount); i++)
        {
          CFragmentRunTableBox *fragmentRunTableBox = new CFragmentRunTableBox();
          continueParsing &= (fragmentRunTableBox != NULL);

          if (continueParsing)
          {
            continueParsing &= fragmentRunTableBox->Parse(buffer + position, length - position);

            if (continueParsing)
            {
              continueParsing &= this->fragmentRunTable->Add(fragmentRunTableBox);

              if (continueParsing)
              {
                position += (unsigned int)fragmentRunTableBox->GetSize();
              }
            }
          }

          if ((!continueParsing) && (fragmentRunTableBox != NULL))
          {
            // cleanup
            FREE_MEM_CLASS(fragmentRunTableBox);
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

wchar_t *CBootstrapInfoBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare server entry table
    wchar_t *serverEntry = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->serverEntryTable->Count(); i++)
    {
      CBootstrapInfoServerEntry *bootstrapInfoServerEntry = this->serverEntryTable->GetItem(i);
      wchar_t *tempServerEntry = FormatString(
        L"%s%s%s'%s'",
        (i == 0) ? L"" : serverEntry,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        bootstrapInfoServerEntry->GetServerEntry()
        );
      FREE_MEM(serverEntry);

      serverEntry = tempServerEntry;
    }

    // prepare quality entry table
    wchar_t *qualityEntry = NULL;
    for (unsigned int i = 0; i < this->qualityEntryTable->Count(); i++)
    {
      CBootstrapInfoQualityEntry *bootstrapInfoQualityEntry = this->qualityEntryTable->GetItem(i);
      wchar_t *tempQualityEntry = FormatString(
        L"%s%s%s'%s'",
        (i == 0) ? L"" : qualityEntry,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        bootstrapInfoQualityEntry->GetQualityEntry()
        );
      FREE_MEM(qualityEntry);

      qualityEntry = tempQualityEntry;
    }

    // prepare segment run table
    wchar_t *segmentRunEntry = NULL;
    for (unsigned int i = 0; i < this->segmentRunTable->Count(); i++)
    {
      CSegmentRunTableBox *bootstrapInfoSegmentRunTableBox = this->segmentRunTable->GetItem(i);
      wchar_t *tempSegmentRunEntry = FormatString(
        L"%s%s%s--- segment run table box %d start ---\n%s\n%s--- segment run table box %d end ---",
        (i == 0) ? L"" : segmentRunEntry,
        (i == 0) ? L"" : L"\n",
        tempIndent, i + 1,
        bootstrapInfoSegmentRunTableBox->GetParsedHumanReadable(tempIndent),
        tempIndent, i + 1);
      FREE_MEM(segmentRunEntry);
      segmentRunEntry = tempSegmentRunEntry;
    }

    // prepare fragment run table
    wchar_t *fragmentRunEntry = NULL;
    for (unsigned int i = 0; i < this->fragmentRunTable->Count(); i++)
    {
      CFragmentRunTableBox *bootstrapInfoFragmentRunTableBox = this->fragmentRunTable->GetItem(i);
      wchar_t *tempFragmentRunEntry = FormatString(
        L"%s%s%s--- fragment run table box %d start ---\n%s\n%s--- fragment run table box %d end ---",
        (i == 0) ? L"" : fragmentRunEntry,
        (i == 0) ? L"" : L"\n",
        tempIndent, i + 1,
        bootstrapInfoFragmentRunTableBox->GetParsedHumanReadable(tempIndent),
        tempIndent, i + 1);
      FREE_MEM(fragmentRunEntry);
      fragmentRunEntry = tempFragmentRunEntry;
    }

    FREE_MEM(tempIndent);

    // prepare finally human readable representation
    result = FormatString(L"%s\n" \
      L"%sVersion: %d\n" \
      L"%sFlags: 0x%06X\n" \
      L"%sBootstrap info version: %d\n" \
      L"%sProfile: %d\n" \
      L"%sLive: %s\n" \
      L"%sUpdate: %s\n" \
      L"%sTime scale: %d\n" \
      L"%sCurrent media time: %lld\n" \
      L"%sSMPTE time code offset: %lld\n" \
      L"%sMovie identifier: '%s'\n" \
      L"%sServer entry count: %d\n" \
      L"%s%s" \
      L"%sQuality entry count: %d\n" \
      L"%s%s" \
      L"%sDRM data: '%s'\n" \
      L"%sMetadata: '%s'\n" \
      L"%sSegment run table count: %d\n" \
      L"%s%s" \
      L"%sFragment run table count: %d" \
      L"%s%s",
      
      previousResult,
      indent, this->version,
      indent, this->flags,
      indent, this->bootstrapInfoVersion,
      indent, this->profile,
      indent, this->live ? L"true" : L"false",
      indent, this->update ? L"true" : L"false",
      indent, this->timeScale,
      indent, this->currentMediaTime,
      indent, this->smpteTimeCodeOffset,
      indent, this->movieIdentifier,
      indent, this->serverEntryTable->Count(),
      (serverEntry == NULL) ? L"" : serverEntry, (serverEntry == NULL) ? L"" : L"\n",
      indent, this->qualityEntryTable->Count(),
      (qualityEntry == NULL) ? L"" : qualityEntry, (qualityEntry == NULL) ? L"" : L"\n",
      indent, this->drmData,
      indent, this->metaData,
      indent, this->segmentRunTable->Count(),
      (segmentRunEntry == NULL) ? L"" : segmentRunEntry, (segmentRunEntry == NULL) ? L"" : L"\n",
      indent, this->fragmentRunTable->Count(),
      (fragmentRunEntry == NULL) ? L"" : L"\n", (fragmentRunEntry == NULL) ? L"" : fragmentRunEntry

      );

    FREE_MEM(serverEntry);
    FREE_MEM(qualityEntry);
    FREE_MEM(segmentRunEntry);
    FREE_MEM(fragmentRunEntry);
  }

  FREE_MEM(previousResult);

  return result;
}

unsigned int CBootstrapInfoBox::GetVersion(void)
{
  return this->version;
}

unsigned int CBootstrapInfoBox::GetFlags(void)
{
  return this->flags;
}

unsigned int CBootstrapInfoBox::GetBootstrapInfoVersion(void)
{
  return this->bootstrapInfoVersion;
}

unsigned int CBootstrapInfoBox::GetProfile(void)
{
  return this->profile;
}

bool CBootstrapInfoBox::IsLive(void)
{
  return this->live;
}

bool CBootstrapInfoBox::IsUpdate(void)
{
  return this->update;
}

unsigned int CBootstrapInfoBox::GetTimeScale(void)
{
  return this->timeScale;
}

uint64_t CBootstrapInfoBox::GetCurrentMediaTime(void)
{
  return this->currentMediaTime;
}

uint64_t CBootstrapInfoBox::GetSmpteTimeCodeOffset(void)
{
  return this->smpteTimeCodeOffset;
}

const wchar_t *CBootstrapInfoBox::GetMovieIdentifier(void)
{
  return this->movieIdentifier;
}

CBootstrapInfoServerEntryCollection *CBootstrapInfoBox::GetServerEntryTable(void)
{
  return this->serverEntryTable;
}

CBootstrapInfoQualityEntryCollection *CBootstrapInfoBox::GetQualityEntryTable(void)
{
  return this->qualityEntryTable;
}

const wchar_t *CBootstrapInfoBox::GetDrmData(void)
{
  return this->drmData;
}

const wchar_t *CBootstrapInfoBox::GetMetaData(void)
{
  return this->metaData;
}

CSegmentRunTableBoxCollection *CBootstrapInfoBox::GetSegmentRunTable(void)
{
  return this->segmentRunTable;
}

CFragmentRunTableBoxCollection *CBootstrapInfoBox::GetFragmentRunTable(void)
{
  return this->fragmentRunTable;
}