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

#include "MediaPacketCollection.h"

CMediaPacketCollection::CMediaPacketCollection(void)
  : CCollection(CCollection::Delete)
{
}

CMediaPacketCollection::~CMediaPacketCollection(void)
{
}

int CMediaPacketCollection::CompareItemKeys(REFERENCE_TIME firstKey, REFERENCE_TIME secondKey, void *context)
{
  if (firstKey < secondKey)
  {
    return (-1);
  }
  else if (firstKey == secondKey)
  {
    return 0;
  }
  else
  {
    return 1;
  }
}

REFERENCE_TIME CMediaPacketCollection::GetKey(CMediaPacket *item)
{
  REFERENCE_TIME timeStart = 0;
  REFERENCE_TIME timeEnd = 0;

  // ignore return value
  item->GetTime(&timeStart, &timeEnd);

  return timeStart;
}

void CMediaPacketCollection::FreeKey(REFERENCE_TIME key)
{
  // no need for deleting key (not reference type)
}

CMediaPacket *CMediaPacketCollection::Clone(CMediaPacket *item)
{
  // not implemented media packet cloning
  return NULL;
}

unsigned int CMediaPacketCollection::GetMediaPacketIndexBetweenTimes(REFERENCE_TIME time)
{
  return this->GetMediaPacketIndexBetweenTimes(0, time);
}

unsigned int CMediaPacketCollection::GetMediaPacketIndexBetweenTimes(unsigned int startIndex, REFERENCE_TIME time)
{
  unsigned int index = UINT_MAX;

  for (unsigned int i = startIndex; i < this->itemCount; i++)
  {
    CMediaPacket *mediaPacket = this->GetItem(i);
    REFERENCE_TIME timeStart;
    REFERENCE_TIME timeEnd;
    HRESULT result = mediaPacket->GetTime(&timeStart, &timeEnd);

    if (result == S_OK)
    {
      // successfully get sample time

      if ((time >= timeStart) && (time <= timeEnd))
      {
        // we found media packet
        index = i;
        break;
      }
    }
  }

  return index;
}

unsigned int CMediaPacketCollection::GetMediaPacketIndexOverlappingTimes(REFERENCE_TIME startTime, REFERENCE_TIME endTime)
{
  unsigned int index = UINT_MAX;

  for (unsigned int i = 0; i < this->itemCount; i++)
  {
    CMediaPacket *mediaPacket = this->GetItem(i);
    REFERENCE_TIME mediaPacketStart;
    REFERENCE_TIME mediaPacketEnd;
    HRESULT result = mediaPacket->GetTime(&mediaPacketStart, &mediaPacketEnd);

    if (result == S_OK)
    {
      // successfully get sample time

      if ((startTime <= mediaPacketEnd) && (endTime >= mediaPacketStart))
      {
        // we found media packet which overlaps time range
        index = i;
        break;
      }
    }
  }

  return index;
}