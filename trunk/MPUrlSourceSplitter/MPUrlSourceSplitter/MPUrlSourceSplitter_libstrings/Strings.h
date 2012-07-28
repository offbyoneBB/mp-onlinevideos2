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

// converts GUID to MBCS string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
char *ConvertGuidToStringA(const GUID guid);

// converts GUID to Unicode string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
wchar_t *ConvertGuidToStringW(const GUID guid);

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
char *ConvertToMultiByteA(const char *string);

// converts string to mutli byte string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
char *ConvertToMultiByteW(const wchar_t *string);

// converts string to Unicode string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
wchar_t *ConvertToUnicodeA(const char *string);

// converts string to Unicode string
// @param string : string to convert
// @return : reference to null terminated string or NULL if error occured
wchar_t *ConvertToUnicodeW(const wchar_t *string);

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
char *DuplicateA(const char *string);

// duplicate Unicode string
// @param string : string to duplicate
// @return : reference to null terminated string or NULL if error occured
wchar_t *DuplicateW(const wchar_t *string);

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
bool IsNullOrEmptyA(const char *string);

// tests if Unicode string is null or empty
// @param string : string to test
// @return : true if null or empty, otherwise false
bool IsNullOrEmptyW(const wchar_t *string);

#ifdef _MBCS
#define IsNullOrEmpty IsNullOrEmptyA
#else
#define IsNullOrEmpty IsNullOrEmptyW
#endif

// formats string using format string and parameters
// @param format : format string
// @return : result string or NULL if error
char *FormatStringA(const char *format, ...);

// formats string using format string and parameters
// @param format : format string
// @return : result string or NULL if error
wchar_t *FormatStringW(const wchar_t *format, ...);

#ifdef _MBCS
#define FormatString FormatStringA
#else
#define FormatString FormatStringW
#endif

char *ReplaceStringA(const char *string, const char *searchString, const char *replaceString);

wchar_t *ReplaceStringW(const wchar_t *string, const wchar_t *searchString, const wchar_t *replaceString);

#ifdef _MBCS
#define ReplaceString ReplaceStringA
#else
#define ReplaceString ReplaceStringW
#endif

char *SkipBlanksA(char *str);
wchar_t *SkipBlanksW(wchar_t *str);

#ifdef _MBCS
#define SkipBlanks SkipBlanksA
#else
#define SkipBlanks SkipBlanksW
#endif

char *EscapeA(char *input);
wchar_t *EscapeW(wchar_t *input);

#ifdef _MBCS
#define Escape EscapeA
#else
#define Escape EscapeW
#endif

char *UnescapeA(char *input);
wchar_t *UnescapeW(wchar_t *input);

#ifdef _MBCS
#define Unescape UnescapeA
#else
#define Unescape UnescapeW
#endif

wchar_t *ConvertUtf8ToUnicode(char *utf8String);
char *ConvertUnicodeToUtf8(wchar_t *unicodeString);

bool IsBlankA(char *input);
bool IsBlankW(wchar_t *input);

#ifdef _MBCS
#define IsBlank IsBlankA
#else
#define IsBlank IsBlankW
#endif

char *TrimLeftA(char *input);
wchar_t *TrimLeftW(wchar_t *input);

#ifdef _MBCS
#define TrimLeft TrimLeftA
#else
#define TrimLeft TrimLeftA
#endif

char *TrimRightA(char *input);
wchar_t *TrimRightW(wchar_t *input);

#ifdef _MBCS
#define TrimRight TrimRightA
#else
#define TrimRight TrimRightW
#endif

char *TrimA(char *input);
wchar_t *TrimW(wchar_t *input);

#ifdef _MBCS
#define Trim TrimA
#else
#define Trim TrimW
#endif


char *ReverseA(char *input);
wchar_t *ReverseW(wchar_t *input);

#ifdef _MBCS
#define Reverse ReverseA
#else
#define Reverse ReverseW
#endif

#endif