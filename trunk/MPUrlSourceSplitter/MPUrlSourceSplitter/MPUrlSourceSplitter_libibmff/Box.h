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

#include <stdint.h>

#define BOX_HEADER_LENGTH                                   8
#define BOX_HEADER_LENGTH_SIZE64                            16

#ifndef RBE8
#define RBE8(buffer, position)                              ((uint8_t)((const uint8_t*)(buffer + position))[0])
#endif

#ifndef RBE8INC
#define RBE8INC(buffer, position, result)                   result = RBE8(buffer, position); \
                                                            position++;
#endif

#ifndef RBE8INC_DEFINE
#define RBE8INC_DEFINE(buffer, position, result, type)      type result = 0; \
                                                            RBE8INC(buffer, position, result);
#endif

#ifndef RBE16
#define RBE16(buffer, position)                             (((uint16_t)((const uint8_t*)(buffer + position))[0] << 8) | \
                                                                       (((const uint8_t*)(buffer + position))[1]))
#endif

#ifndef RBE16INC
#define RBE16INC(buffer, position, result)                  result = RBE16(buffer, position); \
                                                            position += 2;
#endif

#ifndef RBE16INC_DEFINE
#define RBE16INC_DEFINE(buffer, position, result, type)     type result = 0; \
                                                            RBE16INC(buffer, position, result);
#endif


#ifndef RBE24
#define RBE24(buffer, position)                             (((uint32_t)((const uint8_t*)(buffer + position))[0] << 16) | \
                                                                       (((const uint8_t*)(buffer + position))[1] << 8)  | \
                                                                       (((const uint8_t*)(buffer + position))[2]))
#endif

#ifndef RBE24INC
#define RBE24INC(buffer, position, result)                  result = RBE24(buffer, position); \
                                                            position += 3;
#endif

#ifndef RBE24INC_DEFINE
#define RBE24INC_DEFINE(buffer, position, result, type)     type result = 0; \
                                                            RBE24INC(buffer, position, result);
#endif

#ifndef RBE32
#define RBE32(buffer, position)                            (((uint32_t)((const uint8_t*)(buffer + position))[0] << 24) | \
                                                                       (((const uint8_t*)(buffer + position))[1] << 16) | \
                                                                       (((const uint8_t*)(buffer + position))[2] <<  8) | \
                                                                       (((const uint8_t*)(buffer + position))[3]))
#endif

#ifndef RBE32INC
#define RBE32INC(buffer, position, result)                  result = RBE32(buffer, position); \
                                                            position += 4;
#endif

#ifndef RBE32INC_DEFINE
#define RBE32INC_DEFINE(buffer, position, result, type)     type result = 0; \
                                                            RBE32INC(buffer, position, result);
#endif


#ifndef RBE64
#define RBE64(buffer, position)                             (((uint64_t)((const uint8_t*)(buffer + position))[0] << 56) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[1] << 48) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[2] << 40) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[3] << 32) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[4] << 24) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[5] << 16) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[6] <<  8) | \
                                                             ((uint64_t)((const uint8_t*)(buffer + position))[7]))
#endif

#ifndef RBE64INC
#define RBE64INC(buffer, position, result)                  result = RBE64(buffer, position); \
                                                            position += 8;
#endif

#ifndef RBE64INC_DEFINE
#define RBE64INC_DEFINE(buffer, position, result, type)     type result = 0; \
                                                            RBE64INC(buffer, position, result);
#endif

class CBox
{
public:
  // initializes a new instance of CBox class
  CBox(void);

  // destructor
  virtual ~CBox(void);

  /* get methods */

  // gets box size
  // @return : box size
  virtual uint64_t GetSize(void);

  // gets box type
  // @return : box type or NULL if error
  virtual const wchar_t *GetType(void);

  /* set methods */

  /* other methods */

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

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const unsigned char *buffer, unsigned int length);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(const wchar_t *indent);

protected:
  // stores the length of buffer
  uint64_t length;
  // stores if data were successfully parsed
  bool parsed;
  // stores box type
  wchar_t *type;
  // stores if box has extended header
  bool hasExtendedHeader;
  // stores if box has unspecified size
  bool hasUnspecifiedSize;

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