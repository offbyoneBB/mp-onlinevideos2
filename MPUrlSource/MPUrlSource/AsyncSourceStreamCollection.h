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

#ifndef __ASYNCSOURCESTREAMCOLLECTION_DEFINED
#define __ASYNCSOURCESTREAMCOLLECTION_DEFINED

#include "Collection.h"
#include "AsyncSourceStream.h"

class CAsyncSourceStreamCollection : public CCollection<CAsyncSourceStream, TCHAR *>
{
public:
  CAsyncSourceStreamCollection(void);
  ~CAsyncSourceStreamCollection(void);

  // test if asynchronous source stream exists in collection
  // @param name : the name of asynchronous source stream to find
  // @param invariant : specifies if asynchronous source stream name shoud be find with invariant casing
  // @return : true if asynchronous source stream exists, false otherwise
  bool Contains(TCHAR *name, bool invariant);

  // get the asynchronous source stream from collection with specified name
  // @param name : the name of asynchronous source stream to find
  // @param invariant : specifies if asynchronous source stream name shoud be find with invariant casing
  // @return : the reference to asynchronous source stream or NULL if not find
  CAsyncSourceStream *GetStream(TCHAR *name, bool invariant);
protected:
  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(TCHAR *firstKey, TCHAR *secondKey, void *context);

  // gets key for item
  // @param item : the item to get key
  // @return : the key of item
  TCHAR *GetKey(CAsyncSourceStream *item);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item
  CAsyncSourceStream *Clone(CAsyncSourceStream *item);

  // frees item key
  // @param key : the item to free
  void FreeKey(TCHAR *key);
};

#endif

