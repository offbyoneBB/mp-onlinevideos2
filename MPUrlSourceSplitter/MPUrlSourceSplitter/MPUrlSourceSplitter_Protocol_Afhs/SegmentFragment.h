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

class CSegmentFragment
{
public:
  // initializes a new instance of CSegmentFragment class
  // @param segment : segment ID
  // @param fragment : fragment ID
  // @param url : common url for segment and fragment
  CSegmentFragment(unsigned int segment, unsigned int fragment, const wchar_t *url);

  // destructor
  ~CSegmentFragment(void);

  // gets segment ID
  // @return : segment ID
  unsigned int GetSegment(void);

  // gets fragment ID
  // @return : fragment ID
  unsigned int GetFragment(void);

  // gets segment and fragment url
  // @return : segment and fragment url or NULL if error
  const wchar_t *GetUrl(void);

private:
  // stores segment ID
  unsigned int segment;
  // stores fragment ID
  unsigned int fragment;
  // stores common url for segment and fragment
  wchar_t *url;
};

#endif