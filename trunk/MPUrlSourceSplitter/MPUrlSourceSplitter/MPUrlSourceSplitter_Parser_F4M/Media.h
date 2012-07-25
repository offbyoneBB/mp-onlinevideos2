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

#pragma once

#ifndef __MEDIA_DEFINED
#define __MEDIA_DEFINED

#include "MPUrlSourceSplitter_Parser_F4M_Exports.h"

class MPURLSOURCESPLITTER_PARSER_F4M_API CMedia
{
public:
  // constructor
  // create instance of CBootstrapInfo class
  CMedia(wchar_t *url, unsigned int bitrate, unsigned int width, unsigned int height,
    wchar_t *drmAdditionalHeaderId, wchar_t *bootstrapInfoId, wchar_t *dvrInfoId,
    wchar_t *groupspec, wchar_t *multicastStreamName);

  // destructor
  ~CMedia(void);

  // gets url associated with piece of media
  // @return : the url or NULL if error
  const wchar_t *GetUrl(void);

private:
  // stores media url
  wchar_t *url;
  // stores media bitrate
  unsigned int bitrate;
  // stores media width
  unsigned int width;
  // stores media height
  unsigned int height;
  // stores DRM additional header
  wchar_t *drmAdditionalHeaderId;
  // stores the ID of <bootstrapInfo> element
  wchar_t *bootstrapInfoId;
  // stores the ID of <dvrInfo> element
  wchar_t *dvrInfoId;
  // stores group specifier for multicast media
  wchar_t *groupspec;
  // stores stream name for multicast media
  wchar_t *multicastStreamName;
};

#endif