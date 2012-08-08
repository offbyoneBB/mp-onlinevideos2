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

#define SET_STRING(class_variable, variable)                                  FREE_MEM(class_variable); \
                                                                              class_variable = Duplicate(variable);

#define TEST_STRING(class_variable, variable)                                 (class_variable != NULL)
#define TEST_STRING_WITH_NULL(class_variable, variable)                       (TEST_STRING(class_variable, variable)  || (class_variable == variable))

#define SET_STRING_RESULT(class_variable, variable, result)                   SET_STRING(class_variable, variable) \
                                                                              result = TEST_STRING(class_variable, variable);

#define SET_STRING_RESULT_WITH_NULL(class_variable, variable, result)         SET_STRING(class_variable, variable) \
                                                                              result = TEST_STRING_WITH_NULL(class_variable, variable);

#define SET_STRING_RESULT_WITH_NULL_DEFINE(class_variable, variable, result)  bool result = false; \
                                                                              SET_STRING_RESULT_WITH_NULL(class_variable, variable, result)

#define SET_STRING_RETURN(class_variable, variable)                           SET_STRING(class_variable, variable) \
                                                                              return TEST_STRING(class_variable, variable);

#define SET_STRING_RETURN_WITH_NULL(class_variable, variable)                 SET_STRING(class_variable, variable) \
                                                                              return TEST_STRING_WITH_NULL(class_variable, variable);

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

// converts MBCS string to GUID
// @param guid : string GUID to convert
// @return : GUID or GUID_NULL if error
GUID ConvertStringToGuidA(const char *guid);

// converts Unicode string to GUID
// @param guid : string GUID to convert
// @return : GUID or GUID_NULL if error
GUID ConvertStringToGuidW(const wchar_t *guid);

// converts GUID to string
// @param guid : GUID to convert
// @return : reference to null terminated string or NULL if error occured
#ifdef _MBCS
#define ConvertStringToGuid ConvertStringToGuidA
#else
#define ConvertStringToGuid ConvertStringToGuidW
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

// tests if mutli byte string is null or empty or consists of only white-space characters
// @param string : string to test
// @return : true if null or empty or consists of only white-space characters, otherwise false
bool IsNullOrEmptyOrWhitespaceA(const char *string);

// tests if Unicode string is null or empty or consists of only white-space characters
// @param string : string to test
// @return : true if null or empty or consists of only white-space characters, otherwise false
bool IsNullOrEmptyOrWhitespaceW(const wchar_t *string);

#ifdef _MBCS
#define IsNullOrEmptyOrWhitespace IsNullOrEmptyOrWhitespaceA
#else
#define IsNullOrEmptyOrWhitespace IsNullOrEmptyOrWhitespaceW
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

const char *SkipBlanksA(const char *str);
const wchar_t *SkipBlanksW(const wchar_t *str);

#ifdef _MBCS
#define SkipBlanks SkipBlanksA
#else
#define SkipBlanks SkipBlanksW
#endif

char *EscapeA(const char *input);
wchar_t *EscapeW(const wchar_t *input);

#ifdef _MBCS
#define Escape EscapeA
#else
#define Escape EscapeW
#endif

char *UnescapeA(const char *input);
wchar_t *UnescapeW(const wchar_t *input);

#ifdef _MBCS
#define Unescape UnescapeA
#else
#define Unescape UnescapeW
#endif

wchar_t *ConvertUtf8ToUnicode(const char *utf8String);
char *ConvertUnicodeToUtf8(const wchar_t *unicodeString);

bool IsBlankA(const char *input);
bool IsBlankW(const wchar_t *input);

#ifdef _MBCS
#define IsBlank IsBlankA
#else
#define IsBlank IsBlankW
#endif

char *TrimLeftA(const char *input);
wchar_t *TrimLeftW(const wchar_t *input);

#ifdef _MBCS
#define TrimLeft TrimLeftA
#else
#define TrimLeft TrimLeftA
#endif

char *TrimRightA(const char *input);
wchar_t *TrimRightW(const wchar_t *input);

#ifdef _MBCS
#define TrimRight TrimRightA
#else
#define TrimRight TrimRightW
#endif

char *TrimA(const char *input);
wchar_t *TrimW(const wchar_t *input);

#ifdef _MBCS
#define Trim TrimA
#else
#define Trim TrimW
#endif


char *ReverseA(const char *input);
wchar_t *ReverseW(const wchar_t *input);

#ifdef _MBCS
#define Reverse ReverseA
#else
#define Reverse ReverseW
#endif

bool EndsWithA(const char *string, const char c);
bool EndsWithW(const wchar_t *string, const wchar_t c);

#ifdef _MBCS
#define EndsWith EndsWithA
#else
#define EndsWith EndsWithW
#endif

#endif