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

CFlvPacket::CFlvPacket(void)
{
  this->packet = NULL;
  this->size = 0;
  this->type = FLV_PACKET_NONE;
  this->encrypted = false;
}

CFlvPacket::~CFlvPacket(void)
{
  FREE_MEM(this->packet);
}

bool CFlvPacket::IsValid(void)
{
  return ((this->packet != NULL) && (this->size != 0) && (this->type != FLV_PACKET_NONE));
}

const unsigned char *CFlvPacket::GetData(void)
{
  return this->packet;
}

unsigned int CFlvPacket::GetSize(void)
{
  return this->size;
}

unsigned int CFlvPacket::GetType(void)
{
  return this->type;
}

bool CFlvPacket::ParsePacket(const unsigned char *buffer, unsigned int length)
{
  bool result = false;
  this->Clear();

  if ((buffer != NULL) && (length >= FLV_PACKET_HEADER_LENGTH))
  {
    // at least size for FLV header
    this->packet = ALLOC_MEM_SET(this->packet, unsigned char, FLV_PACKET_HEADER_LENGTH, 0);
    if (this->packet != NULL)
    {
      memcpy(this->packet, buffer, FLV_PACKET_HEADER_LENGTH);

      // copied 13 bytes, check first 3 bytes
      if (strncmp("FLV", (char *)this->packet, 3) == 0)
      {
        this->size = FLV_PACKET_HEADER_LENGTH;
        this->type = FLV_PACKET_HEADER;
        result = true;
      }
      else
      {
        // we got first 13 bytes to analyze
        this->type = (*this->packet) & FLV_PACKET_TYPE_MASK;
        this->encrypted = ((*this->packet) & FLV_PACKET_ENCRYPTED_MASK) != 0;

        this->size = ((unsigned char)this->packet[1]) << 8;
        this->size += ((unsigned char)this->packet[2]);
        this->size <<= 8;
        this->size += ((unsigned char)this->packet[3]) + 0x0F;

        if (length >= this->size)
        {
          FREE_MEM(this->packet);
          this->packet = ALLOC_MEM_SET(this->packet, unsigned char, this->size, 0);
          if (this->packet != NULL)
          {
            memcpy(this->packet, buffer, this->size);

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

  if (!result)
  {
    this->Clear();
  }

  return result;
}

unsigned int CFlvPacket::GetPossiblePacketSize(const unsigned char *buffer, unsigned int length)
{
  unsigned int result = UINT_MAX;

  if ((buffer != NULL) && (length >= FLV_PACKET_HEADER_LENGTH))
  {
    // enough data for FLV header packet, check first 3 bytes
    if (strncmp("FLV", (char *)buffer, 3) == 0)
    {
      result = FLV_PACKET_HEADER_LENGTH;
    }
    else
    {
      // we got at least FLV_PACKET_HEADER_LENGTH bytes to analyze

      result = ((unsigned char)buffer[1]) << 8;
      result += ((unsigned char)buffer[2]);
      result <<= 8;
      result += ((unsigned char)buffer[3]) + 0x0F;
    }
  }

  return result;
}

bool CFlvPacket::ParsePacket(CLinearBuffer *buffer)
{
  bool result = false;
  this->Clear();

  if ((buffer != NULL) && (buffer->GetBufferOccupiedSpace() >= FLV_PACKET_HEADER_LENGTH))
  {
    unsigned int possibleSize = UINT_MAX;
    // first get possible FLV packet size
    ALLOC_MEM_DEFINE_SET(sizeBuffer, unsigned char, FLV_PACKET_HEADER_LENGTH, 0);
    if (sizeBuffer != NULL)
    {
      buffer->CopyFromBuffer(sizeBuffer, FLV_PACKET_HEADER_LENGTH, 0, 0);
      possibleSize = this->GetPossiblePacketSize(sizeBuffer, FLV_PACKET_HEADER_LENGTH);
    }
    FREE_MEM(sizeBuffer);

    if (possibleSize != UINT_MAX)
    {
      // at least size for FLV header
      ALLOC_MEM_DEFINE_SET(buf, unsigned char, possibleSize, 0);
      if (buf != NULL)
      {
        buffer->CopyFromBuffer(buf, possibleSize, 0, 0);
        result = this->ParsePacket(buf, possibleSize);
      }
      FREE_MEM(buf);
    }
  }

  if (!result)
  {
    this->Clear();
  }

  return result;
}

bool CFlvPacket::CreatePacket(unsigned int packetType, const unsigned char *buffer, unsigned int length, unsigned int timestamp, bool encrypted)
{
  bool result = false;
  this->Clear();

  if ((buffer != NULL) && ((packetType == FLV_PACKET_AUDIO) || (packetType == FLV_PACKET_VIDEO) || (packetType == FLV_PACKET_META)))
  {
    this->type = packetType;
    this->encrypted = encrypted;
    this->size = length + 0x0F;
    this->packet = ALLOC_MEM_SET(this->packet, unsigned char, this->size, 0);
    result = (this->packet != NULL);

    if (result)
    {
      this->packet[0] = (unsigned char)packetType;
      this->packet[0] |= (this->encrypted) ? (unsigned char)FLV_PACKET_ENCRYPTED_MASK : (unsigned char)0;
      
      this->packet[1] = (unsigned char)((length & 0x00FF0000) >> 16);
      this->packet[2] = (unsigned char)((length & 0x0000FF00) >> 8);
      this->packet[3] = (unsigned char)(length & 0x000000FF);

      this->packet[4] = (unsigned char)((timestamp & 0x00FF0000) >> 16);
      this->packet[5] = (unsigned char)((timestamp & 0x0000FF00) >> 8);
      this->packet[6] = (unsigned char)(timestamp & 0x000000FF);
      this->packet[7] = (unsigned char)((timestamp & 0xFF000000) >> 24);

      memcpy(this->packet + 11, buffer, length);

      unsigned int checkSize = this->size - 0x04;
      this->packet[this->size - 4] = (unsigned char)((checkSize & 0xFF000000) >> 24);
      this->packet[this->size - 3] = (unsigned char)((checkSize & 0x00FF0000) >> 16);
      this->packet[this->size - 2] = (unsigned char)((checkSize & 0x0000FF00) >> 8);
      this->packet[this->size - 1] = (unsigned char)((checkSize & 0x000000FF));
    }
  }

  if (!result)
  {
    this->Clear();
  }

  return result;
}

bool CFlvPacket::IsEncrypted(void)
{
  return this->encrypted;
}

unsigned int CFlvPacket::GetTimestamp(void)
{
  unsigned int result = 0;

  if (this->IsValid() && (this->type != FLV_PACKET_HEADER))
  {
    result = ((unsigned char)this->packet[7]) << 8;
    result += ((unsigned char)this->packet[4]);
    result <<= 8;
    result += ((unsigned char)this->packet[5]);
    result <<= 8;
    result += ((unsigned char)this->packet[6]);
  }

  return result;
}

void CFlvPacket::SetTimestamp(unsigned int timestamp)
{
  if (this->IsValid() && (this->type != FLV_PACKET_HEADER))
  {
    this->packet[6] = (unsigned char)(timestamp & 0xFF);
    timestamp >>= 8;
    this->packet[5] = (unsigned char)(timestamp & 0xFF);
    timestamp >>= 8;
    this->packet[4] = (unsigned char)(timestamp & 0xFF);
    timestamp >>= 8;
    this->packet[7] = (unsigned char)(timestamp & 0xFF);
  }
}

unsigned int CFlvPacket::GetCodecId(void)
{
  unsigned int codecId = UINT_MAX;

  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    codecId =  this->packet[11] & FLV_VIDEO_CODECID_MASK;
  }

  return codecId;
}

unsigned int CFlvPacket::GetFrameType(void)
{
  unsigned int frameType = UINT_MAX;

  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    frameType =  this->packet[11] & FLV_VIDEO_FRAMETYPE_MASK;
  }

  return frameType;
}

void CFlvPacket::SetCodecId(unsigned int codecId)
{
  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    this->packet[11] = this->packet[11] & (~FLV_VIDEO_CODECID_MASK) | codecId;
  }
}

void CFlvPacket::SetFrameType(unsigned int frameType)
{
  if (this->IsValid() && (this->type == FLV_PACKET_VIDEO))
  {
    this->packet[11] = this->packet[11] & (~FLV_VIDEO_FRAMETYPE_MASK) | frameType;
  }
}

void CFlvPacket::Clear(void)
{
  FREE_MEM(this->packet);
  this->size = 0;
  this->type = FLV_PACKET_NONE;
  this->encrypted = false;
}

bool CFlvPacket::IsKeyFrame(void)
{
  return ((this->type == FLV_PACKET_VIDEO) && (this->GetFrameType() == FLV_FRAME_KEY));
}