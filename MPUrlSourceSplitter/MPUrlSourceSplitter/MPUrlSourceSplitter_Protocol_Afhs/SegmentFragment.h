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

#ifndef __SEGMENT_FRAGMENT_DEFINED
#define __SEGMENT_FRAGMENT_DEFINED

#include <stdint.h>

class CSegmentFragment
{
public:
  // initializes a new instance of CSegmentFragment class
  // @param segment : segment ID
  // @param fragment : fragment ID
  // @param url : common url for segment and fragment
  CSegmentFragment(unsigned int segment, unsigned int fragment, const wchar_t *url, uint64_t fragmentTimestamp);

  // destructor
  ~CSegmentFragment(void);

  /* get methods */

  // gets segment ID
  // @return : segment ID
  unsigned int GetSegment(void);

  // gets fragment ID
  // @return : fragment ID
  unsigned int GetFragment(void);

  // gets segment and fragment url
  // @return : segment and fragment url or NULL if error
  const wchar_t *GetUrl(void);

  // gets fragment timestamp
  // @return : fragment timestamp
  uint64_t GetFragmentTimestamp(void);

  // gets if segment and fragment is downloaded
  // @return : true if downloaded, false otherwise
  bool GetDownloaded(void);

  /* set methods */

  // sets if segment and fragment is downloaded
  // @param downloaded : true if segment and fragment is downloaded
  void SetDownloaded(bool downloaded);

  // deep clone of current instance
  // @return : reference to clone of segment and fragment
  CSegmentFragment *Clone(void);

private:
  // stores segment ID
  unsigned int segment;
  // stores fragment ID
  unsigned int fragment;
  // stores common url for segment and fragment
  wchar_t *url;
  // stores fragment timestamp
  uint64_t fragmentTimestamp;
  // stores if segment and fragment is downloaded
  bool downloaded;
};

#endif