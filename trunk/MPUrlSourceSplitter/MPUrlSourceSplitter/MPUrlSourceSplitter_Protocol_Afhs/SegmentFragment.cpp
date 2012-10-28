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

#include "SegmentFragment.h"
#include "MPUrlSourceSplitter_Protocol_Afhs_Parameters.h"

CSegmentFragment::CSegmentFragment(unsigned int segment, unsigned int fragment, const wchar_t *url, uint64_t fragmentTimestamp)
{
  this->segment = segment;
  this->fragment = fragment;
  this->url = Duplicate(url);
  this->fragmentTimestamp = fragmentTimestamp;
  this->downloaded = false;
  this->processed = false;
  this->storeFilePosition = -1;

  this->buffer = new CLinearBuffer();
  this->length = 0;
}

CSegmentFragment::~CSegmentFragment(void)
{
  FREE_MEM(this->url);
  FREE_MEM_CLASS(this->buffer);
}

unsigned int CSegmentFragment::GetSegment(void)
{
  return this->segment;
}

unsigned int CSegmentFragment::GetFragment(void)
{
  return this->fragment;
}

const wchar_t *CSegmentFragment::GetUrl(void)
{
  return this->url;
}

uint64_t CSegmentFragment::GetFragmentTimestamp(void)
{
  return this->fragmentTimestamp;
}

int64_t CSegmentFragment::GetStoreFilePosition(void)
{
  return this->storeFilePosition;
}

CLinearBuffer *CSegmentFragment::GetBuffer()
{
  return this->buffer;
}

unsigned int CSegmentFragment::GetLength(void)
{
  return (this->buffer != NULL) ? this->buffer->GetBufferOccupiedSpace() : this->length;
}

void CSegmentFragment::SetDownloaded(bool downloaded)
{
  this->downloaded = downloaded;
}

void CSegmentFragment::SetProcessed(bool processed)
{
  this->processed = processed;
}

void CSegmentFragment::SetStoredToFile(int64_t position)
{
  this->storeFilePosition = position;
  if (this->storeFilePosition != (-1))
  {
    if (this->buffer != NULL)
    {
      this->length = this->buffer->GetBufferOccupiedSpace();
    }

    FREE_MEM_CLASS(this->buffer);
  }
  else
  {
    if (this->buffer == NULL)
    {
      this->buffer = new CLinearBuffer();
    }
  }
}

bool CSegmentFragment::IsStoredToFile(void)
{
  return (this->storeFilePosition != (-1));
}

bool CSegmentFragment::IsDownloaded(void)
{
  return this->downloaded;
}

bool CSegmentFragment::IsProcessed(void)
{
  return this->processed;
}
