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

#ifndef __MEDIAPACKETCOLLECTION_DEFINED
#define __MEDIAPACKETCOLLECTION_DEFINED

#include "Collection.h"
#include "MediaPacket.h"

class CMediaPacketCollection : public CCollection<CMediaPacket, REFERENCE_TIME>
{
public:
  CMediaPacketCollection(void);
  ~CMediaPacketCollection(void);

  // gets index of media packet where time is between start time and end time
  // @param time : the time between start time and end time
  // @return : index of media packet or UINT_MAX if not exists
  unsigned int GetMediaPacketIndexBetweenTimes(REFERENCE_TIME time);

  // gets index of media packet where time is between start time and end time searching from specified packet index
  // @param startIndex : the index of packet where to start searching
  // @param time : the time between start time and end time
  // @return : index of media packet or UINT_MAX if not exists
  unsigned int GetMediaPacketIndexBetweenTimes(unsigned int startIndex, REFERENCE_TIME time);

  // gets index of media packet which overlap time range between start and end time
  // @param startTime : start of time range
  // @param endTime : end of time range
  // @return : index of media packet or UINT_MAX if not exists
  unsigned int GetMediaPacketIndexOverlappingTimes(REFERENCE_TIME startTime, REFERENCE_TIME endTime);

protected:
  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(REFERENCE_TIME firstKey, REFERENCE_TIME secondKey, void *context);

  // gets key for item
  // caller is responsible of deleting item key using FreeKey() method
  // @param item : the item to get key
  // @return : the key of item
  REFERENCE_TIME GetKey(CMediaPacket *item);

  // frees item key
  // @param key : the item to free
  void FreeKey(REFERENCE_TIME key);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item or NULL if not implemented
  CMediaPacket *Clone(CMediaPacket *item);
};

#endif