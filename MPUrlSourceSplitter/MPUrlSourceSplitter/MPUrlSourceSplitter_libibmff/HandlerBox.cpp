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

#include "HandlerBox.h"

CHandlerBox::CHandlerBox(void)
  : CFullBox()
{
  this->type = Duplicate(HANDLER_BOX_TYPE);
  this->name = NULL;
  this->handlerType = 0;
}

CHandlerBox::~CHandlerBox(void)
{
  FREE_MEM(this->name);
}

/* get methods */

bool CHandlerBox::GetBox(uint8_t *buffer, uint32_t length)
{
  bool result = __super::GetBox(buffer, length);

  if (result)
  {
    uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
  }

  return result;
}

uint32_t CHandlerBox::GetHandlerType(void)
{
  return this->handlerType;
}

const wchar_t *CHandlerBox::GetName(void)
{
  return this->name;
}

/* set methods */

/* other methods */

bool CHandlerBox::Parse(const uint8_t *buffer, uint32_t length)
{
  return this->ParseInternal(buffer, length, true);
}

wchar_t *CHandlerBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sHandler type: 0x%08X\n" \
      L"%sName: '%s'" 
      ,
      
      previousResult,
      indent, this->GetHandlerType(),
      indent, this->GetName()
      );
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CHandlerBox::GetBoxSize(void)
{
  return __super::GetBoxSize();
}

bool CHandlerBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  FREE_MEM(this->name);
  this->handlerType = 0;

  // in bad case we don't have objects, but still it can be valid box
  bool result = __super::ParseInternal(buffer, length, false);

  if (result)
  {
    if (wcscmp(this->type, HANDLER_BOX_TYPE) != 0)
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
        // pre-defined field uint32_t
        position += 4;

        // handler type uint32_t
        RBE32INC(buffer, position, this->handlerType);
        
        // reserved, 3 x uint32_t
        position += 12;
      }

      if (continueParsing)
      {
        uint32_t positionAfter = position;
        continueParsing &= SUCCEEDED(this->GetString(buffer, length, position, &this->name, &positionAfter));

        if (continueParsing)
        {
          position = positionAfter;
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