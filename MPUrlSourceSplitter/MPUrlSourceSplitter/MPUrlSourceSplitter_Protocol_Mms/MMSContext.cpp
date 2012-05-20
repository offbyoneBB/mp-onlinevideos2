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

#include "stdafx.h"

#include "MMSContext.h"

MMSContext::MMSContext()
{
  this->buffer = new LinearBuffer();
  this->streams = new MMSStreamCollection();
  this->headerParsed = false;
  this->requestSequenceNumber = 1;
  this->chunkSequence = 0;
  this->timeout = INFINITE;

  if (this->buffer != NULL)
  {
    this->buffer->InitializeBuffer(HEADER_BUFFER_SIZE);
  }

  this->asfHeader = NULL;
  this->asfHeaderLength = 0;
  this->asfPacketLength = 0;
}

MMSContext::~MMSContext()
{
  if (this->buffer != NULL)
  {
    delete this->buffer;
    this->buffer = NULL;
  }

  if (this->streams != NULL)
  {
    delete this->streams;
    this->streams = NULL;
  }

  FREE_MEM(this->asfHeader);
}

LinearBuffer *MMSContext::GetBuffer(void)
{
  return this->buffer;
}

MMSStreamCollection *MMSContext::GetStreams(void)
{
  return this->streams;
}

bool MMSContext::GetHeaderParsed(void)
{
  return this->headerParsed;
}

void MMSContext::SetHeaderParsed(bool headerParsed)
{
  this->headerParsed = headerParsed;
}

bool MMSContext::IsValid(void)
{
  return ((this->buffer != NULL) && (this->streams != NULL) && (this->buffer->GetBufferSize() != 0));
}

void MMSContext::SetChunkSequence(unsigned int chunkSequence)
{
  this->chunkSequence = chunkSequence;
}

unsigned int MMSContext::GetChunkSequence(void)
{
  return this->chunkSequence;
}

void MMSContext::SetTimeout(unsigned int timeout)
{
  this->timeout = timeout;
}

 unsigned int MMSContext::GetTimeout(void)
 {
   return this->timeout;
 }

 bool MMSContext::InitializeAsfHeader(unsigned int asfHeaderLength)
 {
   bool result = false;

   char *header = ALLOC_MEM_SET(header, char, asfHeaderLength, 0);
   if (header != NULL)
   {
     FREE_MEM(this->asfHeader);
     this->asfHeader = header;
     this->asfHeaderLength = asfHeaderLength;
     result = true;
   }

   return result;
 }

 char *MMSContext::GetAsfHeader(void)
 {
   return this->asfHeader;
 }

 unsigned int MMSContext::GetAsfHeaderLength(void)
 {
   return this->asfHeaderLength;
 }

 void MMSContext::ClearAsfHeader(void)
 {
   FREE_MEM(this->asfHeader);
   this->asfHeaderLength = 0;
 }

 unsigned int MMSContext::GetAsfPacketLength(void)
 {
   return this->asfPacketLength;
 }

 void MMSContext::SetAsfPacketLength(unsigned int asfPacketLength)
 {
   this->asfPacketLength = asfPacketLength;
 }