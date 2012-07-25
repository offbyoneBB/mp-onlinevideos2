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

#ifndef __BOOTSTRAP_INFO_COLLECTION_DEFINED
#define __BOOTSTRAP_INFO_COLLECTION_DEFINED

#include "Collection.h"
#include "BootstrapInfo.h"

class CBootstrapInfoCollection : public CCollection<CBootstrapInfo, wchar_t *>
{
public:
  CBootstrapInfoCollection(void);
  ~CBootstrapInfoCollection(void);

  // test if bootstrap info exists in collection
  // @param name : the ID of bootstrap info to find
  // @param invariant : specifies if ID shoud be find with invariant casing
  // @return : true if bootstrap info exists, false otherwise
  bool Contains(wchar_t *name, bool invariant);

protected:

  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(wchar_t *firstKey, wchar_t *secondKey, void *context);

  // gets key for item
  // caller is responsible of deleting item key using FreeKey() method
  // @param item : the item to get key
  // @return : the key of item
  wchar_t *GetKey(CBootstrapInfo *item);

  // frees item key
  // @param key : the item to free
  void FreeKey(wchar_t *key);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item or NULL if not implemented
  CBootstrapInfo *Clone(CBootstrapInfo *item);
};

#endif