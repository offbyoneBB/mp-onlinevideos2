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

#ifndef __SERVER_SOCKET_CONTEXT_COLLECTION_DEFINED
#define __SERVER_SOCKET_CONTEXT_COLLECTION_DEFINED

#include "Collection.h"
#include "ServerSocketContext.h"

class CServerSocketContextCollection : public CCollection<CServerSocketContext, WORD>
{
public:
  CServerSocketContextCollection(void);
  ~CServerSocketContextCollection(void);

protected:

  // compare two item keys
  // @param firstKey : the first item key to compare
  // @param secondKey : the second item key to compare
  // @param context : the reference to user defined context
  // @return : 0 if keys are equal, lower than zero if firstKey is lower than secondKey, greater than zero if firstKey is greater than secondKey
  int CompareItemKeys(WORD firstKey, WORD secondKey, void *context);

  // gets key for item
  // @param item : the item to get key
  // @return : the key of item
  WORD GetKey(CServerSocketContext *item);

  // clones specified item
  // @param item : the item to clone
  // @return : deep clone of item or NULL if not implemented
  CServerSocketContext *Clone(CServerSocketContext *item);
};

#endif