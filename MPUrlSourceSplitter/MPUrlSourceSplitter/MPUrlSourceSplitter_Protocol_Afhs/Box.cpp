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

#include "Box.h"

CBox::CBox(void)
{
  this->buffer = NULL;
  this->length = -1;
  this->parsed = false;
  this->type = NULL;
}

CBox::~CBox(void)
{
  FREE_MEM(this->buffer);
  FREE_MEM(this->type);
}

bool CBox::IsBox(void)
{
  return ((this->buffer != NULL) && (this->length >= BOX_HEADER_LENGTH) && (this->type != NULL));
}

bool CBox::IsParsed(void)
{
  return this->parsed;
}

int64_t CBox::GetSize(void)
{
  return this->length;
}

const wchar_t *CBox::GetType(void)
{
  return this->type;
}

bool CBox::Parse(const unsigned char *buffer, unsigned int length)
{
  FREE_MEM(this->buffer);
  this->length = -1;
  this->parsed = false;
  FREE_MEM(this->type);

  if ((buffer != NULL) && (length >= BOX_HEADER_LENGTH))
  {
    int64_t size = BE32(buffer);

    if (size == 0)
    {
      // box is the last one in and its contents extend to the end of the file
    }
    else if (size == 1)
    {
      // the actual size is in the field largesize (after BOX_HEADER_LENGTH int(64))
    }
    else
    {
      // size is int(32)
      if ((size >= 0) && (size <= length))
      {
        this->buffer = ALLOC_MEM_SET(this->buffer, unsigned char, (size_t)size, 0);
        if (this->buffer != NULL)
        {
          this->length = size;
          memcpy(this->buffer, buffer, (size_t)size);
        }

        unsigned char *type = ALLOC_MEM_SET(type, unsigned char, 5, 0);
        if (type != NULL)
        {
          // copy 4 chars after size field
          memcpy(type, buffer + 4, 4);
        }

        this->type = ConvertToUnicodeA((char *)type);
        if (this->type != NULL)
        {
          this->parsed = true;
        }

        FREE_MEM(type);
      }
    }
  }

  return this->parsed;
}