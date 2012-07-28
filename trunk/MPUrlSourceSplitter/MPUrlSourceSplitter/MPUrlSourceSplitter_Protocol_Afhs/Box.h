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

#ifndef __BOX_DEFINED
#define __BOX_DEFINED

#include "MPUrlSourceSplitter_Protocol_Afhs_Exports.h"

#define BOX_HEADER_LENGTH                                   8
#define BOX_HEADER_LENGTH_SIZE64                            16

#ifndef BE8
#   define BE8(x)                                          \
    ((uint8_t)((const uint8_t*)(x))[0])
#endif

#ifndef BE16
#   define BE16(x)                                          \
    (((uint16_t)((const uint8_t*)(x))[0] << 8) |            \
               (((const uint8_t*)(x))[1]))
#endif

#ifndef BE24
#   define BE24(x)                                          \
    (((uint32_t)((const uint8_t*)(x))[0] << 16) |           \
               (((const uint8_t*)(x))[1] << 8)  |           \
               (((const uint8_t*)(x))[2]))
#endif

#ifndef BE32
#   define BE32(x)                                          \
    (((uint32_t)((const uint8_t*)(x))[0] << 24) |           \
               (((const uint8_t*)(x))[1] << 16) |           \
               (((const uint8_t*)(x))[2] <<  8) |           \
               (((const uint8_t*)(x))[3]))
#endif

#ifndef BE64
#   define BE64(x)                                          \
    (((uint64_t)((const uint8_t*)(x))[0] << 56) |           \
     ((uint64_t)((const uint8_t*)(x))[1] << 48) |           \
     ((uint64_t)((const uint8_t*)(x))[2] << 40) |           \
     ((uint64_t)((const uint8_t*)(x))[3] << 32) |           \
     ((uint64_t)((const uint8_t*)(x))[4] << 24) |           \
     ((uint64_t)((const uint8_t*)(x))[5] << 16) |           \
     ((uint64_t)((const uint8_t*)(x))[6] <<  8) |           \
     ((uint64_t)((const uint8_t*)(x))[7]))
#endif


class MPURLSOURCESPLITTER_PROTOCOL_AFHS_API CBox
{
public:
  // initializes a new instance of CBox class
  CBox(void);

  // destructor
  virtual ~CBox(void);

  // tests if instance has valid box
  virtual bool IsBox(void);

  // gets if box buffer is successfully parsed
  // @return : true if successfully parsed, false otherwise
  virtual bool IsParsed(void);

  // tests if box size is bigger than UINT_MAX
  // @return : true if box size is bigger than UINT_MAX, false otherwise
  virtual bool IsBigSize(void);

  // tests if box size if unspecified (box content extends to the end of the file)
  // @return : true if box size is unspecifed, false otherwise
  virtual bool IsSizeUnspecifed(void);

  // tests if box has extended header (extra 16 bytes for int(64) size)
  // @return : true if box has extended header, false otherwise
  virtual bool HasExtendedHeader(void);

  // gets box size
  // @return : box size or -1 if error
  virtual int64_t GetSize(void);

  // gets box type
  // @return : box type or NULL if error
  virtual const wchar_t *GetType(void);

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const unsigned char *buffer, unsigned int length);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(wchar_t *indent);

protected:
  // stores the length of buffer
  int64_t length;
  // stores if data were successfully parsed
  bool parsed;
  // stores box type
  wchar_t *type;
  // stores if box has extended header
  bool hasExtendedHeader;

  // gets Unicode string from buffer from specified position
  // @param buffer : the buffer to read UTF-8 string
  // @param length : the length of buffer
  // @param startPosition : the position within buffer to start reading UTF-8 string
  // @param output : reference to Unicode buffer where result will be stored
  // @param positionAfterString : reference to variable where will be stored position after null terminating character of UTF-8 string
  // @return : S_OK if successful, E_POINTER if buffer, output or positionAfterString is NULL, HRESULT_FROM_WIN32(ERROR_INVALID_DATA) if not enough data in buffer, E_OUTOFMEMORY if not enough memory for results
  HRESULT GetString(const unsigned char *buffer, unsigned int length, unsigned int startPosition, wchar_t **output, unsigned int *positionAfterString);
};

#endif