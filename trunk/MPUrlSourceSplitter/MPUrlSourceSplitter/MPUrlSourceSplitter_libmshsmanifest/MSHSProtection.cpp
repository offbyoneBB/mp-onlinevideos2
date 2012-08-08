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

#include "MSHSProtection.h"

CMSHSProtection::CMSHSProtection(void)
{
  this->systemId = GUID_NULL;
  this->content = NULL;
}

CMSHSProtection::~CMSHSProtection(void)
{
  FREE_MEM(this->content);
}

/* get methods */

GUID CMSHSProtection::GetSystemId(void)
{
  return this->systemId;
}

const wchar_t *CMSHSProtection::GetContent(void)
{
  return this->content;
}

/* set methods */

void CMSHSProtection::SetSystemId(GUID systemId)
{
  this->systemId = systemId;
}

bool CMSHSProtection::SetContent(const wchar_t *content)
{
  SET_STRING_RETURN_WITH_NULL(this->content, content);
}

/* other methods */