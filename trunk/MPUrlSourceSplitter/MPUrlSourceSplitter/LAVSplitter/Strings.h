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

#ifndef __STRINGS_DEFINED
#define __STRINGS_DEFINED

#include "MPUrlSourceSplitterExports.h"

// converts GUID to MBCS string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API char *ConvertGuidToStringA(const GUID guid);

// converts GUID to Unicode string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API wchar_t *ConvertGuidToStringW(const GUID guid);

// converts GUID to string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
#ifdef _MBCS
#define ConvertGuidToString ConvertGuidToStringA
#else
#define ConvertGuidToString ConvertGuidToStringW
#endif

// converts string to mutli byte string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API char *ConvertToMultiByteA(const char *string);

// converts string to mutli byte string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API char *ConvertToMultiByteW(const wchar_t *string);

// converts string to Unicode string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API wchar_t *ConvertToUnicodeA(const char *string);

// converts string to Unicode string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API wchar_t *ConvertToUnicodeW(const wchar_t *string);

// converts string to mutli byte string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
#ifdef _MBCS
#define ConvertToMultiByte ConvertToMultiByteA
#else
#define ConvertToMultiByte ConvertToMultiByteW
#endif

// converts string to Unicode string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
#ifdef _MBCS
#define ConvertToUnicode ConvertToUnicodeA
#else
#define ConvertToUnicode ConvertToUnicodeW
#endif

// duplicate mutli byte string
// @param string : string to duplicate
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API char *DuplicateA(const char *string);

// duplicate Unicode string
// @param string : string to duplicate
// @return : reference to null terminated string or NULL if error occured
MPURLSOURCESPLITTER_API wchar_t *DuplicateW(const wchar_t *string);

// duplicate string
// @param string : string to duplicate
// @return : reference to null terminated string or NULL if error occured
#ifdef _MBCS
#define Duplicate DuplicateA
#else
#define Duplicate DuplicateW
#endif

// tests if mutli byte string is null or empty
// @param string : string to test
// @return : true if null or empty, otherwise false
MPURLSOURCESPLITTER_API bool IsNullOrEmptyA(const char *string);

// tests if Unicode string is null or empty
// @param string : string to test
// @return : true if null or empty, otherwise false
MPURLSOURCESPLITTER_API bool IsNullOrEmptyW(const wchar_t *string);

#ifdef _MBCS
#define IsNullOrEmpty IsNullOrEmptyA
#else
#define IsNullOrEmpty IsNullOrEmptyW
#endif

// formats string using format string and parameters
// @param format : format string
// @return : result string or NULL if error
MPURLSOURCESPLITTER_API char *FormatStringA(const char *format, ...);

// formats string using format string and parameters
// @param format : format string
// @return : result string or NULL if error
MPURLSOURCESPLITTER_API wchar_t *FormatStringW(const wchar_t *format, ...);

#ifdef _MBCS
#define FormatString FormatStringA
#else
#define FormatString FormatStringW
#endif

MPURLSOURCESPLITTER_API char *ReplaceStringA(const char *string, const char *searchString, const char *replaceString);

MPURLSOURCESPLITTER_API wchar_t *ReplaceStringW(const wchar_t *string, const wchar_t *searchString, const wchar_t *replaceString);

#ifdef _MBCS
#define ReplaceString ReplaceStringA
#else
#define ReplaceString ReplaceStringW
#endif

MPURLSOURCESPLITTER_API char *SkipBlanksA(char *str);
MPURLSOURCESPLITTER_API wchar_t *SkipBlanksW(wchar_t *str);

#ifdef _MBCS
#define SkipBlanks SkipBlanksA
#else
#define SkipBlanks SkipBlanksW
#endif

MPURLSOURCESPLITTER_API char *EscapeA(char *input);
MPURLSOURCESPLITTER_API wchar_t *EscapeW(wchar_t *input);

#ifdef _MBCS
#define Escape EscapeA
#else
#define Escape EscapeW
#endif

MPURLSOURCESPLITTER_API char *UnescapeA(char *input);
MPURLSOURCESPLITTER_API wchar_t *UnescapeW(wchar_t *input);

#ifdef _MBCS
#define Unescape UnescapeA
#else
#define Unescape UnescapeW
#endif

MPURLSOURCESPLITTER_API wchar_t *ConvertUtf8ToUnicode(char *utf8String);
MPURLSOURCESPLITTER_API char *ConvertUnicodeToUtf8(wchar_t *unicodeString);

MPURLSOURCESPLITTER_API bool IsBlankA(char *input);
MPURLSOURCESPLITTER_API bool IsBlankW(wchar_t *input);

#ifdef _MBCS
#define IsBlank IsBlankA
#else
#define IsBlank IsBlankW
#endif

MPURLSOURCESPLITTER_API char *TrimLeftA(char *input);
MPURLSOURCESPLITTER_API wchar_t *TrimLeftW(wchar_t *input);

#ifdef _MBCS
#define TrimLeft TrimLeftA
#else
#define TrimLeft TrimLeftA
#endif

MPURLSOURCESPLITTER_API char *TrimRightA(char *input);
MPURLSOURCESPLITTER_API wchar_t *TrimRightW(wchar_t *input);

#ifdef _MBCS
#define TrimRight TrimRightA
#else
#define TrimRight TrimRightW
#endif

MPURLSOURCESPLITTER_API char *TrimA(char *input);
MPURLSOURCESPLITTER_API wchar_t *TrimW(wchar_t *input);

#ifdef _MBCS
#define Trim TrimA
#else
#define Trim TrimW
#endif


MPURLSOURCESPLITTER_API char *ReverseA(char *input);
MPURLSOURCESPLITTER_API wchar_t *ReverseW(wchar_t *input);

#ifdef _MBCS
#define Reverse ReverseA
#else
#define Reverse ReverseW
#endif

#endif