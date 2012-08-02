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

#ifndef __MEDIA_DATA_BOX_DEFINED
#define __MEDIA_DATA_BOX_DEFINED

#include "box.h"

#define MEDIA_DATA_BOX_TYPE                                                   L"mdat"

class CMediaDataBox :
  public CBox
{
public:
  // initializes a new instance of CMediaDataBox class
  CMediaDataBox(void);

  // destructor
  virtual ~CMediaDataBox(void);

  /* get methods */

  // gets payload data of media data box
  // @return : payload data or NULL if error
  virtual const unsigned char *GetPayload(void);

  // gets payload size
  // @return : payload size or -1 if error
  virtual int64_t GetPayloadSize(void);

  /* set methods */

  /* other methods */

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
  // stores playload
  unsigned char *payload;
  // stores payload size
  int64_t playloadSize;
};

#endif