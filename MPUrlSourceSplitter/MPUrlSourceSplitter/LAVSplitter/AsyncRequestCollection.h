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

#ifndef __ASYNCREQUESTCOLLECTION_DEFINED
#define __ASYNCREQUESTCOLLECTION_DEFINED

#include "AsyncRequest.h"
#include "Collection.h"

class CAsyncRequest;

class CAsyncRequestCollection : public CCollection<CAsyncRequest, LONGLONG>
{
public:
  CAsyncRequestCollection(void);
  ~CAsyncRequestCollection(void);

  // gets request index with specified request ID
  // @param requestId : the request ID to find in collection
  // @return : request index or UINT_MAX if not found
  unsigned int GetRequestIndex(unsigned int requestId);

  // gets request with specified request ID
  // @param requestId : the request ID to find in collection
  // @return : reference to request or NULL if not found
  CAsyncRequest *GetRequest(unsigned int requestId);

protected:
  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(LONGLONG firstKey, LONGLONG secondKey, void *context);

  // gets key for item
  // @param item : the item to get key
  // @return : the key of item
  LONGLONG GetKey(CAsyncRequest *item);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item or NULL if not implemented
  CAsyncRequest *Clone(CAsyncRequest *item);

  // frees item key
  // @param key : the item to free
  void FreeKey(LONGLONG key);
};

#endif
