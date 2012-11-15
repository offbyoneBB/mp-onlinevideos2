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

#ifndef __RTMP_STREAM_FRAGMENT_COLLECTION_DEFINED
#define __RTMP_STREAM_FRAGMENT_COLLECTION_DEFINED

#include "Collection.h"
#include "RtmpStreamFragment.h"

class CRtmpStreamFragmentCollection : public CCollection<CRtmpStreamFragment, const wchar_t *>
{
public:
  CRtmpStreamFragmentCollection(void);
  ~CRtmpStreamFragmentCollection(void);

  // gets first not downloaded stream fragment
  // @param requested : start index for searching
  // @return : index of first not downloaded stream fragment or UINT_MAX if not exists
  unsigned int GetFirstNotDownloadedStreamFragment(unsigned int start);

  // gets fragment with specified timestamp, starts searching from specified position
  // @param timestamp : the timestamp to find
  // @param position : index where seraching starts
  // @return : index of found fragment or UINT_MAX if not found
  unsigned int GetFragmentWithTimestamp(uint64_t timestamp, unsigned int position);

protected:

  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(const wchar_t *firstKey, const wchar_t *secondKey, void *context);

  // gets key for item
  // @param item : the item to get key
  // @return : the key of item
  const wchar_t *GetKey(CRtmpStreamFragment *item);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item or NULL if not implemented
  CRtmpStreamFragment *Clone(CRtmpStreamFragment *item);
};

#endif