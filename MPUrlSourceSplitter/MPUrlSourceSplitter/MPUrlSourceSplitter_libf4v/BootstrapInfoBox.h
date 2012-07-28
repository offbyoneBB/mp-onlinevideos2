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

#ifndef __BOOTSTRAP_INFO_BOX_DEFINED
#define __BOOTSTRAP_INFO_BOX_DEFINED

#include "box.h"

#include "BootstrapInfoServerEntryCollection.h"
#include "BootstrapInfoQualityEntryCollection.h"
#include "SegmentRunTableBoxCollection.h"
#include "FragmentRunTableBoxCollection.h"

#define BOOTSTRAP_INFO_BOX_TYPE                                               L"abst"

#define PROFILE_NAMED_ACCESS                                                  0
#define PROFILE_RANGE_ACCESS                                                  1

class CBootstrapInfoBox :
  public CBox
{
public:
  // initializes a new instance of CBootstrapInfoBox class
  CBootstrapInfoBox(void);

  // destructor
  virtual ~CBootstrapInfoBox(void);

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const unsigned char *buffer, unsigned int length);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(wchar_t *indent);

  // gets movie identifier
  // @return : movie identifier
  virtual const wchar_t *GetMovieIdentifier(void);

  // gets quality entry table
  // @return : quality entry table
  virtual CBootstrapInfoQualityEntryCollection *GetQualityEntryTable(void);

  // gets segment run table
  // @return : segment run table
  virtual CSegmentRunTableBoxCollection *GetSegmentRunTable(void);

protected:

  unsigned int version;

  unsigned int flags;

  unsigned int bootstrapInfoVersion;

  unsigned int profile;

  bool live;

  bool update;

  unsigned int timeScale;

  uint64_t currentMediaTime;

  uint64_t smpteTimeCodeOffset;

  wchar_t *movieIdentifier;

  CBootstrapInfoServerEntryCollection *serverEntryTable;

  CBootstrapInfoQualityEntryCollection *qualityEntryTable;

  wchar_t *drmData;

  wchar_t *metaData;

  CSegmentRunTableBoxCollection *segmentRunTable;

  CFragmentRunTableBoxCollection *fragmentRunTable;
};

#endif