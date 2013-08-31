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

#include "FragmentRunEntry.h"

CFragmentRunEntry::CFragmentRunEntry(uint32_t firstFragment, uint64_t firstFragmentTimestamp, uint32_t fragmentDuration, uint32_t discontinuityIndicator)
{
  this->firstFragment = firstFragment;
  this->firstFragmentTimestamp = firstFragmentTimestamp;
  this->fragmentDuration = fragmentDuration;
  this->discontinuityIndicator = discontinuityIndicator;
}

CFragmentRunEntry::~CFragmentRunEntry(void)
{
}

/* get methods */

uint32_t CFragmentRunEntry::GetFirstFragment(void)
{
  return this->firstFragment;
}

uint64_t CFragmentRunEntry::GetFirstFragmentTimestamp(void)
{
  return this->firstFragmentTimestamp;
}

uint32_t CFragmentRunEntry::GetFragmentDuration(void)
{
  return this->fragmentDuration;
}

uint32_t CFragmentRunEntry::GetDiscontinuityIndicator(void)
{
  return this->discontinuityIndicator;
}

/* set methods */

/* other methods */

bool CFragmentRunEntry::IsEndOfPresentation(void)
{
  return ((this->fragmentDuration == 0) && (this->discontinuityIndicator == DISCONTINUITY_INDICATOR_END_OF_PRESENTATION));
}

bool CFragmentRunEntry::IsDiscontinuityInFragmentNumbering(void)
{
  return ((this->fragmentDuration == 0) && (this->IsDiscontinuityInFragmentNumberingAndTimestamps() || (this->discontinuityIndicator == DISCONTINUITY_INDICATOR_FRAGMENT_NUMBERING)));
}

bool CFragmentRunEntry::IsDiscontinuityInFragmentTimestamps(void)
{
  return ((this->fragmentDuration == 0) && (this->IsDiscontinuityInFragmentNumberingAndTimestamps() || (this->discontinuityIndicator == DISCONTINUITY_INDICATOR_TIMESTAMPS)));
}

bool CFragmentRunEntry::IsDiscontinuityInFragmentNumberingAndTimestamps(void)
{
  return ((this->fragmentDuration == 0) && (this->discontinuityIndicator == DISCONTINUITY_INDICATOR_FRAGMENT_NUMBERING_AND_TIMESTAMPS));
}