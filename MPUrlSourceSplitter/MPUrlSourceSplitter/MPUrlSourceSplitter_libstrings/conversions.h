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

#ifndef __CONVERSIONS_DEFINED
#define __CONVERSIONS_DEFINED

#include <stdint.h>

// converts string to unsigned int
// @param input : string to convert
// @param defaultValue : default value
// @return : converted string value or default value if error
unsigned int GetValueUnsignedIntA(const char *input, unsigned int defaultValue);

// converts string to unsigned int
// @param input : string to convert
// @param defaultValue : default value
// @return : converted string value or default value if error
unsigned int GetValueUnsignedIntW(const wchar_t *input, unsigned int defaultValue);

#ifdef _MBCS
#define GetValueUnsignedInt GetValueUnsignedIntA
#else
#define GetValueUnsignedInt GetValueUnsignedIntW
#endif

// converts string to unsigned int64
// @param input : string to convert
// @param defaultValue : default value
// @return : converted string value or default value if error
uint64_t GetValueUnsignedInt64A(const char *input, uint64_t defaultValue);

// converts string to unsigned int64
// @param input : string to convert
// @param defaultValue : default value
// @return : converted string value or default value if error
uint64_t GetValueUnsignedInt64W(const wchar_t *input, uint64_t defaultValue);

#ifdef _MBCS
#define GetValueUnsignedInt64 GetValueUnsignedInt64A
#else
#define GetValueUnsignedInt64 GetValueUnsignedInt64W
#endif


#endif