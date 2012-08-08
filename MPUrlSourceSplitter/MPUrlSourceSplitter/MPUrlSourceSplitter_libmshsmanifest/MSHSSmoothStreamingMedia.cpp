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

#include "MSHSSmoothStreamingMedia.h"

CMSHSSmoothStreamingMedia::CMSHSSmoothStreamingMedia(void)
{
  this->majorVersion = MANIFEST_MAJOR_VERSION;
  this->minorVersion = MANIFEST_MINOR_VERSION;
  this->timeScale = MANIFEST_TIMESCALE_DEFAULT;
  this->duration = 0;
  this->protections = new CMSHSProtectionCollection();
  this->streams = new CMSHSStreamCollection();
}

CMSHSSmoothStreamingMedia::~CMSHSSmoothStreamingMedia(void)
{
  FREE_MEM_CLASS(this->protections);
  FREE_MEM_CLASS(this->streams);
}

/* get methods */

uint32_t CMSHSSmoothStreamingMedia::GetMajorVersion(void)
{
  return this->majorVersion;
}

uint32_t CMSHSSmoothStreamingMedia::GetMinorVersion(void)
{
  return this->minorVersion;
}

uint64_t CMSHSSmoothStreamingMedia::GetTimeScale(void)
{
  return this->timeScale;
}

uint64_t CMSHSSmoothStreamingMedia::GetDuration(void)
{
  return this->duration;
}

CMSHSProtectionCollection *CMSHSSmoothStreamingMedia::GetProtections(void)
{
  return this->protections;
}

CMSHSStreamCollection *CMSHSSmoothStreamingMedia::GetStreams(void)
{
  return this->streams;
}

/* set methods */

void CMSHSSmoothStreamingMedia::SetMajorVersion(uint32_t majorVersion)
{
  this->majorVersion = majorVersion;
}

void CMSHSSmoothStreamingMedia::SetMinorVersion(uint32_t minorVersion)
{
  this->minorVersion = minorVersion;
}

void CMSHSSmoothStreamingMedia::SetTimeScale(uint64_t timeScale)
{
  this->timeScale = timeScale;
}

void CMSHSSmoothStreamingMedia::SetDuration(uint64_t duration)
{
  this->duration = duration;
}

/* other methods */

bool CMSHSSmoothStreamingMedia::IsProtected(void)
{
  return (this->GetProtections()->Count() != 0);
}