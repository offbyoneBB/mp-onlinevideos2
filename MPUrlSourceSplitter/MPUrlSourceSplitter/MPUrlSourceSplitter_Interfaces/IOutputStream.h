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

#ifndef __OUTPUTSTREAMINTERFACE_DEFINED
#define __OUTPUTSTREAMINTERFACE_DEFINED

#include "MediaPacket.h"

#include <stdint.h>

#define METHOD_PUSH_MEDIA_PACKET_NAME                                         L"PushMediaPacket()"
#define METHOD_END_OF_STREAM_REACHED_NAME                                     L"EndOfStreamReached()"

// defines interface for stream output
struct IOutputStream
{
  // sets total length of stream to output pin
  // caller is responsible for deleting output pin name
  // @param total : total length of stream in bytes
  // @param estimate : specifies if length is estimate
  // @return : S_OK if successful
  virtual HRESULT SetTotalLength(int64_t total, bool estimate) = 0;

  // pushes media packet to output pin
  // caller is responsible for freeing memory
  // @param mediaPacket : reference to media packet to push to output pin
  // @return : S_OK if successful
  virtual HRESULT PushMediaPacket(CMediaPacket *mediaPacket) = 0;

  // notifies output stream that end of stream was reached
  // this method can be called only when protocol support SEEKING_METHOD_POSITION
  // @param streamPosition : the last valid stream position
  // @return : S_OK if successful
  virtual HRESULT EndOfStreamReached(int64_t streamPosition) = 0;
};

#endif