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

#include "MSHSStreamFragment.h"

CMSHSStreamFragment::CMSHSStreamFragment(void)
{
  this->fragmentDuration = 0;
  this->fragmentNumber = 0;
  this->fragmentTime = 0;
}

CMSHSStreamFragment::~CMSHSStreamFragment(void)
{
}

/* get methods */

uint32_t CMSHSStreamFragment::GetFragmentNumber(void)
{
  return this->fragmentNumber;
}

uint64_t CMSHSStreamFragment::GetFragmentDuration(void)
{
  return this->fragmentDuration;
}

uint64_t CMSHSStreamFragment::GetFragmentTime(void)
{
  return this->fragmentTime;
}

/* set methods */

void CMSHSStreamFragment::SetFragmentNumber(uint32_t fragmentNumber)
{
  this->fragmentNumber = fragmentNumber;
}

void CMSHSStreamFragment::SetFragmentDuration(uint64_t fragmentDuration)
{
  this->fragmentDuration = fragmentDuration;
}

void CMSHSStreamFragment::SetFragmentTime(uint64_t fragmentTime)
{
  this->fragmentTime = fragmentTime;
}

/* other methods */