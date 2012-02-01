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
#include "AsyncRequestCollection.h"

CAsyncRequestCollection::CAsyncRequestCollection(void)
  : CCollection(CCollection::Delete)
{
}

CAsyncRequestCollection::~CAsyncRequestCollection(void)
{
}

int CAsyncRequestCollection::CompareItemKeys(int64_t firstKey, int64_t secondKey, void *context)
{
  if (firstKey < secondKey)
  {
    return -1;
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

int64_t CAsyncRequestCollection::GetKey(CAsyncRequest *item)
{
  return item->GetStart();
}

CAsyncRequest *CAsyncRequestCollection::Clone(CAsyncRequest *item)
{
  return NULL;
}

void CAsyncRequestCollection::FreeKey(int64_t key)
{
}

CAsyncRequest *CAsyncRequestCollection::GetRequest(unsigned int requestId)
{
  return this->GetItem(this->GetRequestIndex(requestId));
}

unsigned int CAsyncRequestCollection::GetRequestIndex(unsigned int requestId)
{
  unsigned int result = UINT_MAX;
  for (unsigned int i = 0; i < this->itemCount; i++)
  {
    CAsyncRequest *request = this->GetItem(i);
    if (request->GetRequestId() == requestId)
    {
      // found request with specified request ID
      result = i;
      break;
    }
  }
  return result;
}