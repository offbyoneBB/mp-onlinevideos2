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

#include "StreamFragment.h"

CStreamFragment::CStreamFragment(const wchar_t *url, uint64_t fragmentDuration, uint64_t fragmentTime, unsigned int fragmentType)
{
  this->url = Duplicate(url);
  this->fragmentDuration = fragmentDuration;
  this->fragmentTime = fragmentTime;
  this->downloaded = false;
  this->fragmentType = fragmentType;
}

CStreamFragment::~CStreamFragment(void)
{
  FREE_MEM(this->url);
}

/* get methods */

uint64_t CStreamFragment::GetFragmentDuration(void)
{
  return this->fragmentDuration;
}

uint64_t CStreamFragment::GetFragmentTime(void)
{
  return this->fragmentTime;
}

const wchar_t *CStreamFragment::GetUrl(void)
{
  return this->url;
}

bool CStreamFragment::GetDownloaded(void)
{
  return this->downloaded;
}

unsigned int CStreamFragment::GetFragmentType(void)
{
  return this->fragmentType;
}

/* set methods */

void CStreamFragment::SetDownloaded(bool downloaded)
{
  this->downloaded = downloaded;
}

/* other methods */

CStreamFragment *CStreamFragment::Clone(void)
{
  CStreamFragment *fragment = new CStreamFragment(this->url, this->fragmentDuration, this->fragmentTime, this->fragmentType);

  return fragment;
}