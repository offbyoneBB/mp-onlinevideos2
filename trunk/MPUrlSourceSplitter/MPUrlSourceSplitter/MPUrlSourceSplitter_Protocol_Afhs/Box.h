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

#define BOX_HEADER_LENGTH                                   8

#ifndef BE8
#   define BE8(x)                                          \
    (((uint8_t)((const uint8_t*)(x))[0] << 8)
#endif

#ifndef BE16
#   define BE16(x)                                          \
    (((uint16_t)((const uint8_t*)(x))[0] << 8) |            \
                ((const uint8_t*)(x))[1])
#endif

#ifndef BE24
#   define BE24(x)                                          \
    (((uint32_t)((const uint8_t*)(x))[0] << 16) |           \
               (((const uint8_t*)(x))[1] << 8)  |           \
               (((const uint8_t*)(x))[2])
#endif

#ifndef BE32
#   define BE32(x)                                          \
    (((uint32_t)((const uint8_t*)(x))[0] << 24) |           \
               (((const uint8_t*)(x))[1] << 16) |           \
               (((const uint8_t*)(x))[2] <<  8) |           \
                ((const uint8_t*)(x))[3])
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
      (uint64_t)((const uint8_t*)(x))[7])
#endif


class CBox
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

protected:
  // stores data for parsing in box
  unsigned char *buffer;
  // stores the length of buffer
  int64_t length;
  // stores if data were successfully parsed
  bool parsed;
  // stores box type
  wchar_t *type;
};

#endif