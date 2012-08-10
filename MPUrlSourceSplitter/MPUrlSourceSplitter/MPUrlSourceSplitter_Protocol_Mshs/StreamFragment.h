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

#ifndef __STREAM_FRAGMENT_DEFINED
#define __STREAM_FRAGMENT_DEFINED

#include <stdint.h>

class CStreamFragment
{
public:
  // creats new instance of CStreamFragment class
  CStreamFragment(const wchar_t *url, uint64_t fragmentDuration, uint64_t fragmentTime);

  // desctructor
  ~CStreamFragment(void);

  /* get methods */

  // gets fragment duration
  // @return : fragment duration
  uint64_t GetFragmentDuration(void);

  // gets fragment time
  // @return : fragment time
  uint64_t GetFragmentTime(void);

  // gets stream fragment url
  // @return : stream fragment url or NULL if error
  const wchar_t *GetUrl(void);

  // gets if stream fragment is downloaded
  // @return : true if downloaded, false otherwise
  bool GetDownloaded(void);

  /* set methods */

  // sets if stream fragment is downloaded
  // @param downloaded : true if stream fragment is downloaded
  void SetDownloaded(bool downloaded);

  /* other methods */

  // deep clone of current instance
  // @return : reference to clone of stream fragment
  CStreamFragment *Clone(void);

private:

  // stores stream fragment duration
  uint64_t fragmentDuration;
  // stores stream fragment start time
  uint64_t fragmentTime;
  // stores url for stream fragment
  wchar_t *url;
  // stores if stream fragment is downloaded
  bool downloaded;
};

#endif