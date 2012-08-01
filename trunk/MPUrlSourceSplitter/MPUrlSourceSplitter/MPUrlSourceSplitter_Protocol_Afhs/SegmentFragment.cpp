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

CSegmentFragment::CSegmentFragment(unsigned int segment, unsigned int fragment, const wchar_t *url, uint64_t fragmentTimestamp)
{
  this->segment = segment;
  this->fragment = fragment;
  this->url = Duplicate(url);
  this->fragmentTimestamp = fragmentTimestamp;
  this->downloaded = false;
}

CSegmentFragment::~CSegmentFragment(void)
{
  FREE_MEM(this->url);
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

bool CSegmentFragment::GetDownloaded(void)
{
  return this->downloaded;
}

void CSegmentFragment::SetDownloaded(bool downloaded)
{
  this->downloaded = downloaded;
}

CSegmentFragment *CSegmentFragment::Clone(void)
{
  CSegmentFragment *clone = new CSegmentFragment(this->segment, this->fragment, this->url, this->fragmentTimestamp);

  return clone;
}
