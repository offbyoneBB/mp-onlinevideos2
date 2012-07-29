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

#include "StdAfx.h"

#include "FlvPacket.h"

FlvPacket::FlvPacket(void)
{
  this->packet = NULL;
  this->size = 0;
  this->type = FLV_PACKET_NONE;
}

FlvPacket::~FlvPacket(void)
{
  FREE_MEM(this->packet);
}

bool FlvPacket::IsValid(void)
{
  return ((this->packet != NULL) && (this->size != 0) && (this->type != FLV_PACKET_NONE));
}

char *FlvPacket::GetData(void)
{
  return this->packet;
}

unsigned int FlvPacket::GetSize(void)
{
  return this->size;
}

unsigned int FlvPacket::GetType(void)
{
  return this->type;
}

bool FlvPacket::ParsePacket(LinearBuffer *buffer)
{
  bool result = false;

  if ((buffer != NULL) && (buffer->GetBufferOccupiedSpace() >= 13))
  {
    // at least size for FLV header
    this->packet = ALLOC_MEM_SET(this->packet, char, 13, 0);
    if (this->packet != NULL)
    {
      if (buffer->CopyFromBuffer(this->packet, 13, 0, 0) == 13)
      {
        // copied 13 bytes, check first 3 bytes
        if (strncmp("FLV", this->packet, 3) == 0)
        {
          this->size = 13;
          this->type = FLV_PACKET_HEADER;
          result = true;
        }
        else
        {
          // we got first 13 bytes to analyze
          this->type = (*this->packet);

          this->size = ((unsigned char)this->packet[1]) << 8;
          this->size += ((unsigned char)this->packet[2]);
          this->size <<= 8;
          this->size += ((unsigned char)this->packet[3]) + 0x0F;

          if (buffer->GetBufferOccupiedSpace() >= this->size)
          {
            FREE_MEM(this->packet);
            this->packet = ALLOC_MEM_SET(this->packet, char, this->size, 0);
            if (this->packet != NULL)
            {
              if (buffer->CopyFromBuffer(this->packet, this->size, 0, 0) == this->size)
              {
                unsigned int checkSize = ((unsigned char)this->packet[this->size - 4]) << 8;
                checkSize += ((unsigned char)this->packet[this->size - 3]);
                checkSize <<= 8;
                checkSize += ((unsigned char)this->packet[this->size - 2]);
                checkSize <<= 8;
                checkSize += ((unsigned char)this->packet[this->size - 1]) + 4;

                if (this->size == checkSize)
                {
                  // FLV packet has correct size
                  result = true;
                }
              }
            }
          }
        }
      }
    }

    if (!result)
    {
      FREE_MEM(this->packet);
      this->type = FLV_PACKET_NONE;
      this->size = 0;
    }
  }

  return result;
}

unsigned int FlvPacket::GetTimestamp(void)
{
  unsigned int result = 0;

  if (this->IsValid() && (this->type != FLV_PACKET_HEADER))
  {
    result = ((unsigned char)this->packet[4]) << 8;
    result += ((unsigned char)this->packet[5]);
    result <<= 8;
    result += ((unsigned char)this->packet[6]);
  }

  return result;
}

void FlvPacket::SetTimestamp(unsigned int timestamp)
{
  if (this->IsValid() && (this->type != FLV_PACKET_HEADER))
  {
    this->packet[6] = (unsigned char)(timestamp & 0xFF);
    timestamp >>= 8;
    this->packet[5] = (unsigned char)(timestamp & 0xFF);
    timestamp >>= 8;
    this->packet[4] = (unsigned char)(timestamp & 0xFF);
  }
}


unsigned int FlvPacket::GetCodecId(void)
{
  unsigned int codecId = UINT_MAX;

  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    codecId =  this->packet[11] & FLV_VIDEO_CODECID_MASK;
  }

  return codecId;
}

unsigned int FlvPacket::GetFrameType(void)
{
  unsigned int frameType = UINT_MAX;

  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    frameType =  this->packet[11] & FLV_VIDEO_FRAMETYPE_MASK;
  }

  return frameType;
}

void FlvPacket::SetCodecId(unsigned int codecId)
{
  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    this->packet[11] = this->packet[11] & (~FLV_VIDEO_CODECID_MASK) | codecId;
  }
}

void FlvPacket::SetFrameType(unsigned int frameType)
{
  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    this->packet[11] = this->packet[11] & (~FLV_VIDEO_FRAMETYPE_MASK) | frameType;
  }
}

void FlvPacket::Clear(void)
{
  FREE_MEM(this->packet);
  this->size = 0;
  this->type = FLV_PACKET_NONE;
}