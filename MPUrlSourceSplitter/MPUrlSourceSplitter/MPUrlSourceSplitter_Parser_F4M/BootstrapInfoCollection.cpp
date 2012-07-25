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

#include "BootstrapInfoCollection.h"

CBootstrapInfoCollection::CBootstrapInfoCollection(void)
  : CCollection(CCollection::Delete)
{
}

CBootstrapInfoCollection::~CBootstrapInfoCollection(void)
{
}

int CBootstrapInfoCollection::CompareItemKeys(wchar_t *firstKey, wchar_t *secondKey, void *context)
{
  bool invariant = (*(bool *)context);

  if (invariant)
  {
    return _wcsicmp(firstKey, secondKey);
  }
  else
  {
    return wcscmp(firstKey, secondKey);
  }
}

wchar_t *CBootstrapInfoCollection::GetKey(CBootstrapInfo *item)
{
  return Duplicate(item->GetId());
}

void CBootstrapInfoCollection::FreeKey(wchar_t *key)
{
  FREE_MEM(key);
}

CBootstrapInfo *CBootstrapInfoCollection::Clone(CBootstrapInfo *item)
{
  return NULL;
}

bool CBootstrapInfoCollection::Contains(wchar_t *name, bool invariant)
{
  return __super::Contains(name, (void *)&invariant);
}