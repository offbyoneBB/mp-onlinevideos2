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

#include "FragmentRunEntryCollection.h"

CFragmentRunEntryCollection::CFragmentRunEntryCollection(void)
  : CCollection(CCollection::Delete)
{
}

CFragmentRunEntryCollection::~CFragmentRunEntryCollection(void)
{
}

int CFragmentRunEntryCollection::CompareItemKeys(wchar_t *firstKey, wchar_t *secondKey, void *context)
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

wchar_t *CFragmentRunEntryCollection::GetKey(CFragmentRunEntry *item)
{
  return Duplicate(L"");
}

void CFragmentRunEntryCollection::FreeKey(wchar_t *key)
{
  FREE_MEM(key);
}

CFragmentRunEntry *CFragmentRunEntryCollection::Clone(CFragmentRunEntry *item)
{
  return NULL;
}