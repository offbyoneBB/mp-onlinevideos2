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

#include "SampleToChunkBox.h"

CSampleToChunkBox::CSampleToChunkBox(void)
  : CFullBox()
{
  this->type = Duplicate(SAMPLE_TO_CHUNK_BOX_TYPE);
  this->samplesToChunks = new CSampleToChunkCollection();
}

CSampleToChunkBox::~CSampleToChunkBox(void)
{
  FREE_MEM_CLASS(this->samplesToChunks);
}

/* get methods */

bool CSampleToChunkBox::GetBox(uint8_t **buffer, uint32_t *length)
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

CSampleToChunkCollection *CSampleToChunkBox::GetSamplesToChunks(void)
{
  return this->samplesToChunks;
}

/* set methods */

/* other methods */

bool CSampleToChunkBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CSampleToChunkBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare sample entries collection
    wchar_t *samplesToChunks = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->GetSamplesToChunks()->Count(); i++)
    {
      CSampleToChunk *sampleToChunk = this->GetSamplesToChunks()->GetItem(i);
      wchar_t *tempSamplesToChunks = FormatString(
        L"%s%s%sFirst chunk: %5u Samples per chunk: %3u Sample description index: %2u",
        (i == 0) ? L"" : samplesToChunks,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        sampleToChunk->GetFirstChunk(),
        sampleToChunk->GetSamplesPerChunk(),
        sampleToChunk->GetSampleDescriptionIndex());
      FREE_MEM(samplesToChunks);

      samplesToChunks = tempSamplesToChunks;
    }

    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sChunk offsets:%s" \
      L"%s"
      ,
      
      previousResult,
      indent, (this->GetSamplesToChunks()->Count() == 0) ? L"" : L"\n",
      (this->GetSamplesToChunks()->Count() == 0) ? L"" : samplesToChunks

      );

    FREE_MEM(samplesToChunks);
    FREE_MEM(tempIndent);
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CSampleToChunkBox::GetBoxSize(uint64_t size)
{
  return __super::GetBoxSize(size);
}

bool CSampleToChunkBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  if (this->samplesToChunks != NULL)
  {
    this->samplesToChunks->Clear();
  }

  bool result (this->samplesToChunks != NULL);
  result &= __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, SAMPLE_TO_CHUNK_BOX_TYPE) != 0)
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
        RBE32INC_DEFINE(buffer, position, entryCount, uint32_t);

        for (uint32_t i = 0; (continueParsing && (i < entryCount)); i++)
        {
          CSampleToChunk *sampleToChunk = new CSampleToChunk();
          continueParsing &= (sampleToChunk != NULL);

          if (continueParsing)
          {
            sampleToChunk->SetFirstChunk(RBE32(buffer, position));
            position += 4;

            sampleToChunk->SetSamplesPerChunk(RBE32(buffer, position));
            position += 4;

            sampleToChunk->SetSampleDescriptionIndex(RBE32(buffer, position));
            position += 4;

            continueParsing &= this->samplesToChunks->Add(sampleToChunk);
          }

          if (!continueParsing)
          {
            FREE_MEM_CLASS(sampleToChunk);
          }
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