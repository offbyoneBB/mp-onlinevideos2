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

#define BOOTSTRAP_INFO_BOX_TYPE                                               L"abst"

class CBootstrapInfoBox :
  public CBox
{
public:
  // initializes a new instance of CBox class
  CBootstrapInfoBox(void);

  // destructor
  virtual ~CBootstrapInfoBox(void);

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const unsigned char *buffer, unsigned int length);

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

  unsigned int segmentRunTableCount;

  /*
  SegmentRunTableEntries SegmentRunTable[SegmentRunTableCount]
  */

  unsigned int fragmentRunTableCount;

  /*
  FragmentRunTableEntries FragmentRunTable[FragmentRunTableCount]
  */
};

#endif