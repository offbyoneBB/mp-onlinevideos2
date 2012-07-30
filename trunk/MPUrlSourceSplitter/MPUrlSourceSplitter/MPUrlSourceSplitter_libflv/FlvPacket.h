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

#ifndef __FLV_PACKET_DEFINED
#define __FLV_PACKET_DEFINED

#include "LinearBuffer.h"

#define FLV_PACKET_NONE                                                       0x00
#define FLV_PACKET_AUDIO                                                      0x08
#define FLV_PACKET_VIDEO                                                      0x09
#define FLV_PACKET_META                                                       0x12
#define FLV_PACKET_HEADER                                                     0xFF

#define FLV_VIDEO_CODECID_MASK                                                0x0F
#define FLV_VIDEO_FRAMETYPE_MASK                                              0xF0

#define FLV_VIDEO_FRAMETYPE_OFFSET                                            4

#define FLV_FRAME_KEY                                                         1 << FLV_VIDEO_FRAMETYPE_OFFSET
#define FLV_FRAME_INTER                                                       2 << FLV_VIDEO_FRAMETYPE_OFFSET
#define FLV_FRAME_DISP_INTER                                                  3 << FLV_VIDEO_FRAMETYPE_OFFSET

#define FLV_CODECID_H263                                                      2
#define FLV_CODECID_SCREEN                                                    3
#define FLV_CODECID_VP6                                                       4
#define FLV_CODECID_VP6A                                                      5
#define FLV_CODECID_SCREEN2                                                   6
#define FLV_CODECID_H264                                                      7
#define FLV_CODECID_REALH263                                                  8
#define FLV_CODECID_MPEG4                                                     9

class CFlvPacket
{
public:
  // initializes a new instance of CFlvPacket class
  CFlvPacket(void);
  ~CFlvPacket(void);

  // tests if current instance of CFlvPacket is valid
  // @return : true if valid, false otherwise
  bool IsValid();

  // gets FLV packet type
  // @return : FLV packet type
  unsigned int GetType(void);

  // gets FLV packet size
  // @return : FLV packet size
  unsigned int GetSize(void);

  // gets FLV packet data
  // @return : FLV packet data
  const unsigned char *GetData(void);

  // parses buffer for FLV packet
  // @param buffer : linear buffer to parse
  // @return : true if FLV packet found, false otherwise
  bool ParsePacket(LinearBuffer *buffer);

  // parses buffer for FLV packet
  // @param buffer : buffer to parse
  // @param length : length of buffer
  // @return : true if FLV packet found, false otherwise
  bool ParsePacket(const unsigned char *buffer, unsigned int length);

  // creates FLV packet and fill it with data from buffer
  // @param packetType : type of FLV packet (FLV_PACKET_AUDIO, FLV_PACKET_VIDEO, FLV_PACKET_META)
  // @param buffer : data to fill FLV packet
  // @param length : the length of data buffer
  // @param timestamp : the timestamp of FLV packet
  // @return : true if FLV packet successfully created, false otherwise
  bool CreatePacket(unsigned int packetType, const unsigned char *buffer, unsigned int length, unsigned int timestamp);

  // gets FLV packet timestamp
  unsigned int GetTimestamp(void);

  // sets FLV packet timestamp
  void SetTimestamp(unsigned int timestamp);

  // gets codec ID for video packet
  // @return : codec ID or UINT_MAX if not valid
  unsigned int GetCodecId(void);

  // sets codec ID for video packet
  // @param codecId : video packet codec ID to set
  void SetCodecId(unsigned int codecId);

  // gets frame type for video packet
  // @return : frame type or UINT_MAX if not valid
  unsigned int GetFrameType(void);

  // sets video packet frame type
  // @param frameType : video packet frame type to set
  void SetFrameType(unsigned int frameType);

  // clears current instance
  void Clear(void);

protected:
  // holds packet type (AUDIO, VIDEO, META or HEADER)
  unsigned int type;

  // holds FLV packet size
  unsigned int size;

  // holds FLV packet data
  unsigned char *packet;
};

#endif