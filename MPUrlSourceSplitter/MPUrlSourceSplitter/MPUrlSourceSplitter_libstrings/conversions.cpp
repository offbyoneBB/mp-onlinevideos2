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

#include "conversions.h"

unsigned int GetValueUnsignedIntA(const char *input, unsigned int defaultValue)
{
  if (!IsNullOrEmptyOrWhitespaceA(input))
  {
    char *end = NULL;
    long valueLong = strtol(input, &end, 10);
    if ((valueLong == 0) && (input == end))
    {
      // error while converting
      valueLong = defaultValue;
    }

    return (unsigned int)valueLong;
  }
  else
  {
    return defaultValue;
  }
}

unsigned int GetValueUnsignedIntW(const wchar_t *input, unsigned int defaultValue)
{
  if (!IsNullOrEmptyOrWhitespaceW(input))
  {
    wchar_t *end = NULL;
    long valueLong = wcstol(input, &end, 10);
    if ((valueLong == 0) && (input == end))
    {
      // error while converting
      valueLong = defaultValue;
    }

    return (unsigned int)valueLong;
  }
  else
  {
    return defaultValue;
  }
}

uint64_t GetValueUnsignedInt64A(const char *input, uint64_t defaultValue)
{
  if (!IsNullOrEmptyOrWhitespaceA(input))
  {
    char *end = NULL;
    uint64_t valueLong = _strtoui64(input, &end, 10);
    if ((valueLong == 0) && (input == end))
    {
      // error while converting
      valueLong = defaultValue;
    }

    return valueLong;
  }
  else
  {
    return defaultValue;
  }
}

uint64_t GetValueUnsignedInt64W(const wchar_t *input, uint64_t defaultValue)
{
  if (!IsNullOrEmptyOrWhitespaceW(input))
  {
    wchar_t *end = NULL;
    uint64_t valueLong = _wcstoui64(input, &end, 10);
    if ((valueLong == 0) && (input == end))
    {
      // error while converting
      valueLong = defaultValue;
    }

    return valueLong;
  }
  else
  {
    return defaultValue;
  }
}
