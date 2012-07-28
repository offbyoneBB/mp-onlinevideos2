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

#ifndef __SEGMENT_RUN_ENTRY_DEFINED
#define __SEGMENT_RUN_ENTRY_DEFINED

#include "MPUrlSourceSplitter_Protocol_Afhs_Exports.h"

class MPURLSOURCESPLITTER_PROTOCOL_AFHS_API CSegmentRunEntry
{
public:
  // initializes a new instance of CSegmentRunEntry class
  CSegmentRunEntry(unsigned int firstSegment, unsigned int fragmentsPerSegment);

  ~CSegmentRunEntry(void);

  // gets first segment
  // @return : first segment
  unsigned int GetFirstSegment(void);

  // gets fragments per segment
  // @return : fragments per segment
  unsigned int GetFragmentsPerSegment(void);

private:
  // stores the identifying number of the first segment in the run of segments containing the same number of fragments
  unsigned int firstSegment;
  // stores the number of fragments in each segment in this run
  unsigned int fragmentsPerSegment;

};

#endif