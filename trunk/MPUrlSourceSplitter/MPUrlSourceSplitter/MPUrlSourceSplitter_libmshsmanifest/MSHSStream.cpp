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

#include "MSHSStream.h"

CMSHSStream::CMSHSStream(void)
{
  this->displayHeight = 0;
  this->displayWidth = 0;
  this->maxHeight = 0;
  this->maxWidth = 0;
  this->name = NULL;
  this->numberOfFragments = 0;
  this->numberOfTracks = 0;
  this->subType = NULL;
  this->timeScale = 0;
  this->type = NULL;
  this->url = NULL;
}

CMSHSStream::~CMSHSStream(void)
{
  FREE_MEM(this->name);
  FREE_MEM(this->subType);
  FREE_MEM(this->type);
  FREE_MEM(this->url);
}

/* get methods */

const wchar_t *CMSHSStream::GetType(void)
{
  return this->type;
}

const wchar_t *CMSHSStream::GetSubType(void)
{
  return this->subType;
}

const wchar_t *CMSHSStream::GetUrl(void)
{
  return this->url;
}

uint64_t CMSHSStream::GetTimeScale(void)
{
  return this->timeScale;
}

const wchar_t *CMSHSStream::GetName(void)
{
  return this->name;
}

uint32_t CMSHSStream::GetNumberOfFragments(void)
{
  return this->numberOfFragments;
}

uint32_t CMSHSStream::GetNumberOfTracks(void)
{
  return this->numberOfTracks;
}

uint32_t CMSHSStream::GetMaxWidth(void)
{
  return this->maxWidth;
}

uint32_t CMSHSStream::GetMaxHeight(void)
{
  return this->maxHeight;
}

uint32_t CMSHSStream::GetDisplayWidth(void)
{
  return this->displayWidth;
}

uint32_t CMSHSStream::GetDisplayHeight(void)
{
  return this->displayHeight;
}

/* set methods */

bool CMSHSStream::SetType(const wchar_t *type)
{
  SET_STRING_RETURN_WITH_NULL(this->type, type);
}

bool CMSHSStream::SetSubType(const wchar_t *subType)
{
  SET_STRING_RETURN_WITH_NULL(this->subType, subType);
}

bool CMSHSStream::SetUrl(const wchar_t *url)
{
  SET_STRING_RETURN_WITH_NULL(this->url, url);
}

void CMSHSStream::SetTimeScale(uint64_t timeScale)
{
  this->timeScale = timeScale;
}

bool CMSHSStream::SetName(const wchar_t *name)
{
  SET_STRING_RETURN_WITH_NULL(this->name, name);
}

void CMSHSStream::SetNumberOfFragments(uint32_t numberOfFragments)
{
  this->numberOfFragments = numberOfFragments;
}

void CMSHSStream::SetNumberOfTracks(uint32_t numberOfTracks)
{
  this->numberOfTracks = numberOfTracks;
}

void CMSHSStream::SetMaxWidth(uint32_t maxWidth)
{
  this->maxWidth = maxWidth;
}

void CMSHSStream::SetMaxHeight(uint32_t maxHeight)
{
  this->maxHeight = maxHeight;
}

void CMSHSStream::SetDisplayWidth(uint32_t displayWidth)
{
  this->displayWidth = displayWidth;
}

void CMSHSStream::SetDisplayHeight(uint32_t displayHeight)
{
  this->displayHeight = displayHeight;
}

/* other methods */