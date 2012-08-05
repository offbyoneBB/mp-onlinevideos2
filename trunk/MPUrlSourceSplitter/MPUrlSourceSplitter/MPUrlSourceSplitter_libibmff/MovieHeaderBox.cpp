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

#include "MovieHeaderBox.h"

CMovieHeaderBox::CMovieHeaderBox(void)
  : CFullBox()
{
  this->creationTime = 0;
  this->modificationTime = 0;
  this->timeScale = 0;
  this->duration = 0;
  this->type = Duplicate(MOVIE_HEADER_BOX_TYPE);
  this->rate = new CFixedPointNumber(16, 16);
  this->volume = new CFixedPointNumber(8, 8);
  this->matrix = new CMatrix();
  this->nextTrackId = 0;
}

CMovieHeaderBox::~CMovieHeaderBox(void)
{
  FREE_MEM_CLASS(this->rate);
  FREE_MEM_CLASS(this->volume);
  FREE_MEM_CLASS(this->matrix);
}

/* get methods */

bool CMovieHeaderBox::GetBox(uint8_t **buffer, uint32_t *length)
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

uint64_t CMovieHeaderBox::GetCreationTime(void)
{
  return this->creationTime;
}

uint64_t CMovieHeaderBox::GetModificationTime(void)
{
  return this->modificationTime;
}

uint32_t CMovieHeaderBox::GetTimeScale(void)
{
  return this->timeScale;
}

uint64_t CMovieHeaderBox::GetDuration(void)
{
  return this->duration;
}

CFixedPointNumber *CMovieHeaderBox::GetRate(void)
{
  return this->rate;
}

CFixedPointNumber *CMovieHeaderBox::GetVolume(void)
{
  return this->volume;
}

CMatrix *CMovieHeaderBox::GetMatrix(void)
{
  return this->matrix;
}

uint32_t CMovieHeaderBox::GetNextTrackId(void)
{
  return this->nextTrackId;
}

/* set methods */

/* other methods */

bool CMovieHeaderBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CMovieHeaderBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare matrix
    wchar_t *matrix = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->matrix->Count(); i += 3)
    {
      CFixedPointNumber *num1 = this->matrix->GetItem(i);
      CFixedPointNumber *num2 = this->matrix->GetItem(i + 1);
      CFixedPointNumber *num3 = this->matrix->GetItem(i + 2);

      wchar_t *tempMatrix = FormatString(
        L"%s%s%s( %5u.%u %5u.%u %5u.%u )",
        (i == 0) ? L"" : matrix,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        num1->GetIntegerPart(), num1->GetFractionPart(),
        num2->GetIntegerPart(), num2->GetFractionPart(),
        num3->GetIntegerPart(), num3->GetFractionPart()
        );
      FREE_MEM(matrix);

      matrix = tempMatrix;
    }

    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sCreation time: %I64u\n" \
      L"%sModification time: %I64u\n" \
      L"%sTime scale: %u\n" \
      L"%sDuration: %I64u\n" \
      L"%sRate: %u.%u\n" \
      L"%sVolume: %u.%u\n" \
      L"%sMatrix:\n" \
      L"%s%s" \
      L"%sNext track ID: %u"
      ,
      
      previousResult,
      indent, this->GetCreationTime(),
      indent, this->GetModificationTime(),
      indent, this->GetTimeScale(),
      indent, this->GetDuration(),
      indent, this->GetRate()->GetIntegerPart(), this->GetRate()->GetFractionPart(),
      indent, this->GetVolume()->GetIntegerPart(), this->GetRate()->GetFractionPart(),
      indent,
      (matrix == NULL) ? L"" : matrix, (matrix == NULL) ? L"" : L"\n",
      indent, this->GetNextTrackId()
      );

    FREE_MEM(matrix);
    FREE_MEM(tempIndent);
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CMovieHeaderBox::GetBoxSize(uint64_t size)
{
  uint64_t adjust = 0;
  switch(this->GetVersion())
  {
  case 0:
    adjust = MOVIE_HEADER_DATA_VERSION_0_SIZE;
    break;
  case 1:
    adjust = MOVIE_HEADER_DATA_VERSION_1_SIZE;
    break;
  default:
    break;
  }

  return __super::GetBoxSize(size + adjust);
}

bool CMovieHeaderBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  FREE_MEM_CLASS(this->rate);
  FREE_MEM_CLASS(this->volume);
  FREE_MEM_CLASS(this->matrix);

  this->creationTime = 0;
  this->modificationTime = 0;
  this->timeScale = 0;
  this->duration = 0;
  this->rate = new CFixedPointNumber(16, 16);
  this->volume = new CFixedPointNumber(8, 8);
  this->matrix = new CMatrix();
  this->nextTrackId = 0;

  bool result = ((this->rate != NULL) && (this->volume != NULL) && (this->matrix != NULL));
  result &= __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, MOVIE_HEADER_BOX_TYPE) != 0)
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
          RBE32INC(buffer, position, this->creationTime);
          RBE32INC(buffer, position, this->modificationTime);
          RBE32INC(buffer, position, this->timeScale);
          RBE32INC(buffer, position, this->duration);
          break;
        case 1:
          RBE64INC(buffer, position, this->creationTime);
          RBE64INC(buffer, position, this->modificationTime);
          RBE32INC(buffer, position, this->timeScale);
          RBE64INC(buffer, position, this->duration);
          break;
        default:
          continueParsing = false;
          break;
        }
      }

      if (continueParsing)
      {
        continueParsing &= this->rate->SetNumber(RBE32(buffer, position));
        position += 4;
      }

      if (continueParsing)
      {
        continueParsing &= this->volume->SetNumber(RBE16(buffer, position));
        position += 2;
      }

      // skip 10 reserved bytes
      position += 10;

      if (continueParsing)
      {
        // read matrix
        for (unsigned int i = 0; (continueParsing && (i < this->matrix->Count())); i++)
        {
          continueParsing &= this->matrix->GetItem(i)->SetNumber(RBE32(buffer, position));
          position += 4;
        }
      }

      // skip 6 * bit(32)
      position += 24;

      if (continueParsing)
      {
        RBE32INC(buffer, position, this->nextTrackId);
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