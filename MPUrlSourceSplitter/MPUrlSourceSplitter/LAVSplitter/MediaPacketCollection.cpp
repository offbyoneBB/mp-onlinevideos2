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

bool CMediaPacketCollection::Add(CMediaPacket *item)
{
  if (item == NULL)
  {
    return false;
  }

  if (!this->EnsureEnoughSpace(this->Count() + 1))
  {
    return false;
  }

  unsigned int startIndex = 0;
  unsigned int endIndex = 0;

  if (this->GetItemInsertPosition(this->GetKey(item), NULL, &startIndex, &endIndex))
  {
    if (startIndex == endIndex)
    {
      // media packet exists in collection
      return false;
    }

    // everything after endIndex must be moved
    if (this->itemCount > 0)
    {
      for (unsigned int i = (this->itemCount - 1); i >= endIndex; i--)
      {
        *(this->items + i + 1) = *(this->items + i);
      }
    }

    if (endIndex == UINT_MAX)
    {
      // the media packet have to be added after all media packets
      endIndex = this->itemCount;
    }

    // add new item to collection and increase item count
    *(this->items + endIndex) = item;
    this->itemCount++;
  }

  return true;
}

bool CMediaPacketCollection::GetItemInsertPosition(int64_t key, void *context, unsigned int *startIndex, unsigned int *endIndex)
{
  bool result = ((startIndex != NULL) && (endIndex != NULL));

  if (result)
  {
    result = (this->Count() > 0);

    if (result)
    {
      unsigned int first = 0;
      unsigned int last = this->Count() - 1;
      result = false;

      while ((first <= last) && (first != UINT_MAX) && (last != UINT_MAX))
      {
        // compute middle index
        unsigned int middle = (first + last) / 2;

        // get media packet at middle index
        CMediaPacket *mediaPacket = this->GetItem(middle);

        // media packet key is start time
        // it is not reference type so there is no need to free item key

        // tests media packet to key value
        int comparison = this->CompareItemKeys(key, this->GetKey(mediaPacket), context);
        if (comparison > 0)
        {
          // key is bigger than media packet start time
          // search in top half
          first = middle + 1;
        }
        else if (comparison < 0) 
        {
          // key is lower than media packet start time
          // search in bottom half
          last = middle - 1;
        }
        else
        {
          // we found media packet with same starting time as key
          *startIndex = middle;
          *endIndex = middle;
          result = true;
          break;
        }
      }

      if (!result)
      {
        // we don't found media packet
        // it means that media packet with 'key' belongs between first and last
        *startIndex = last;
        *endIndex = (first >= this->Count()) ? UINT_MAX : first;
        result = true;
      }
    }
    else
    {
      *startIndex = UINT_MAX;
      *endIndex = 0;
      result = true;
    }
  }

  return result;
}

unsigned int CMediaPacketCollection::GetItemIndex(int64_t key, void *context)
{
  unsigned int result = UINT_MAX;

  unsigned int startIndex = 0;
  unsigned int endIndex = 0;
  if (this->GetItemInsertPosition(key, context, &startIndex, &endIndex))
  {
    if (startIndex == endIndex)
    {
      result = startIndex;
    }
  }

  return result;
}

int CMediaPacketCollection::CompareItemKeys(int64_t firstKey, int64_t secondKey, void *context)
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

int64_t CMediaPacketCollection::GetKey(CMediaPacket *item)
{
  return item->GetStart();
}

void CMediaPacketCollection::FreeKey(int64_t key)
{
  // no need for deleting key (not reference type)
}

CMediaPacket *CMediaPacketCollection::Clone(CMediaPacket *item)
{
  // not implemented media packet cloning
  return NULL;
}

unsigned int CMediaPacketCollection::GetMediaPacketIndexBetweenPositions(int64_t time)
{
  unsigned int index = UINT_MAX;

  unsigned int startIndex = 0;
  unsigned int endIndex = 0;

  if (this->GetItemInsertPosition(time, NULL, &startIndex, &endIndex))
  {
    if (startIndex != UINT_MAX)
    {
      // if requested position is somewhere after start of media packets
      CMediaPacket *mediaPacket = this->GetItem(startIndex);
      int64_t positionStart = mediaPacket->GetStart();
      int64_t positionEnd = mediaPacket->GetEnd();

      if ((time >= positionStart) && (time <= positionEnd))
      {
        // we found media packet
        index = startIndex;
      }
    }
  }

  return index;

}
unsigned int CMediaPacketCollection::GetMediaPacketIndexOverlappingPositions(int64_t start, int64_t end)
{
  unsigned int index = UINT_MAX;

  for (unsigned int i = 0; i < this->itemCount; i++)
  {
    CMediaPacket *mediaPacket = this->GetItem(i);
    int64_t mediaPacketStart = mediaPacket->GetStart();
    int64_t mediaPacketEnd = mediaPacket->GetEnd();

    if ((start <= mediaPacketEnd) && (end >= mediaPacketStart))
    {
      // we found media packet which overlaps position range
      index = i;
      break;
    }
  }

  return index;
}