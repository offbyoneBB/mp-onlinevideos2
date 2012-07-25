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

#include "Media.h"

CMedia::CMedia(wchar_t *url, unsigned int bitrate, unsigned int width, unsigned int height,
    wchar_t *drmAdditionalHeaderId, wchar_t *bootstrapInfoId, wchar_t *dvrInfoId,
    wchar_t *groupspec, wchar_t *multicastStreamName)
{
  this->url = Duplicate(url);
  this->bitrate = bitrate;
  this->width = width;
  this->height = height;
  this->drmAdditionalHeaderId = Duplicate(drmAdditionalHeaderId);
  this->bootstrapInfoId = Duplicate(bootstrapInfoId);
  this->dvrInfoId = Duplicate(dvrInfoId);
  this->groupspec = Duplicate(groupspec);
  this->multicastStreamName = Duplicate(multicastStreamName);
}

CMedia::~CMedia(void)
{
  FREE_MEM(this->url);
  FREE_MEM(this->drmAdditionalHeaderId);
  FREE_MEM(this->bootstrapInfoId);
  FREE_MEM(this->dvrInfoId);
  FREE_MEM(this->groupspec);
  FREE_MEM(this->multicastStreamName);
}

const wchar_t *CMedia::GetUrl(void)
{
  return this->url;
}