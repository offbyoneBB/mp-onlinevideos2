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
#include "MediaPacket.h"
#include "Utilities.h"

CMediaPacket::CMediaPacket(void)
{
  this->buffer = new LinearBuffer();
  this->buffer->DeleteBuffer();

  this->flags = 0;
  this->start = 0;
  this->end = 0;
  /*this->mediaStart = 0;
  this->mediaEnd = 0;
  this->mediaType = NULL;*/
  this->lastAccessTime = GetTickCount();
}

CMediaPacket::~CMediaPacket(void)
{
  if (this->buffer != NULL)
  {
    delete this->buffer;
  }
  /*if (mediaType)
  {
    DeleteMediaType(mediaType);
  }*/
}

LinearBuffer *CMediaPacket::GetBuffer()
{
  return this->buffer;
}

CMediaPacket *CMediaPacket::Clone(void)
{
  CMediaPacket *clone = new CMediaPacket();
  clone->start = this->start;
  clone->end = this->end;
  clone->flags = this->flags;
  clone->lastAccessTime = this->lastAccessTime;

  // because in clone is created linear buffer we need to delete clone buffer
  delete clone->buffer;
  clone->buffer = this->buffer->Clone();

  if (clone->buffer == NULL)
  {
    // error occured while cloning current instance
    delete clone;
    clone = NULL;
  }

  return clone;
}

CMediaPacket *CMediaPacket::CreateMediaPacketBasedOnPacket(REFERENCE_TIME timeStart, REFERENCE_TIME timeEnd)
{
  CMediaPacket *mediaPacket = new CMediaPacket();
  char *buffer = NULL;
  bool success = ((timeStart >= this->start) && (timeEnd >= timeStart));

  if (success)
  {
    // initialize new media packet start, end and length
    unsigned int length = (unsigned int)(timeEnd - timeStart + 1);

    // initialize new media packet data
    success = (mediaPacket->SetTime(&timeStart, &timeEnd) == S_OK);
    if (success)
    {
      mediaPacket->SetLastAccessTime(this->GetLastAccessTime());
      success = (mediaPacket->GetBuffer()->InitializeBuffer(length));
    }

    if (success)
    {
      // create temporary buffer and copy data from unprocessed media packet
      buffer = ALLOC_MEM_SET(buffer, char, length, 0);
      success = (buffer != NULL);
    }

    if (success)
    {
      success = (this->GetBuffer()->CopyFromBuffer(buffer, length, 0, (unsigned int)(timeStart - this->start)) == length);
    }

    if (success)
    {
      // add data from temporary buffer to first part media packet
      success = (mediaPacket->GetBuffer()->AddToBuffer(buffer, length) == length);

      // remove temporary buffer
      FREE_MEM(buffer);
    }
  }

  if (!success)
  {
    delete mediaPacket;
    mediaPacket = NULL;
  }

  return mediaPacket;
}

HRESULT CMediaPacket::GetTime(REFERENCE_TIME *timeStart, REFERENCE_TIME *timeEnd)
{
  HRESULT result = S_OK;

  CHECK_POINTER_DEFAULT_HRESULT(result, timeStart);
  CHECK_POINTER_DEFAULT_HRESULT(result, timeEnd);

  if (result == S_OK)
  {
    if (!(this->flags & Sample_StopValid))
    {
      if (!(this->flags & Sample_TimeValid))
      {
        result = VFW_E_SAMPLE_TIME_NOT_SET;
      }
      else
      {
        *timeStart = this->start;

        //  make sure old stuff works
        *timeEnd = this->start + 1;
        result = VFW_S_NO_STOP_TIME;
      }
    }
    else
    {
      *timeStart = this->start;
      *timeEnd = this->end;
    }
  }

  return result;
}

HRESULT CMediaPacket::SetTime(REFERENCE_TIME *timeStart, REFERENCE_TIME *timeEnd)
{
  HRESULT result = S_OK;

  if (timeStart == NULL)
  {
    if (timeEnd != NULL)
    {
      result = E_POINTER;
    }
    else
    {
      this->flags &= ~(Sample_TimeValid | Sample_StopValid);
    }
  }
  else
  {
    if (timeEnd == NULL)
    {
      this->start = *timeStart;
      this->flags |= Sample_TimeValid;
      this->flags &= ~Sample_StopValid;
    }
    else
    {
      CHECK_CONDITION(result, *timeEnd >= *timeStart, S_OK, E_INVALIDARG);

      if (result == S_OK)
      {
        this->start = *timeStart;
        this->end = *timeEnd;
        this->flags |= Sample_TimeValid | Sample_StopValid;
      }
    }
  }

  return result;
}

//bool CMediaPacket::IsSyncPoint(void)
//{
//  return ((this->flags & Sample_SyncPoint) != 0);
//}
//
//void CMediaPacket::SetSyncPoint(bool isSyncPoint)
//{
//  if (isSyncPoint)
//  {
//    this->flags |= Sample_SyncPoint;
//  }
//  else
//  {
//    this->flags &= ~Sample_SyncPoint;
//  }
//}
//
//bool CMediaPacket::IsPreroll(void)
//{
//  return ((this->flags & Sample_Preroll) != 0);
//}
//
//void CMediaPacket::SetPreroll(bool isPreroll)
//{
//  if (isPreroll)
//  {
//    this->flags |= Sample_Preroll;
//  }
//  else
//  {
//    this->flags &= ~Sample_Preroll;
//  }
//}
//
//bool CMediaPacket::IsDiscontinuity(void)
//{
//  return ((this->flags & Sample_Discontinuity) != 0);
//}
//
//void CMediaPacket::SetDiscontinuity(bool discontinuity)
//{
//  if (discontinuity)
//  {
//    this->flags |= Sample_Discontinuity;
//  }
//  else
//  {
//    this->flags &= ~Sample_Discontinuity;
//  }
//}
//
//HRESULT CMediaPacket::GetMediaTime(LONGLONG *timeStart, LONGLONG *timeEnd)
//{
//  HRESULT result = S_OK;
//
//  CHECK_POINTER_DEFAULT_HRESULT(result, timeStart);
//  CHECK_POINTER_DEFAULT_HRESULT(result, timeEnd);
//
//  if (result == S_OK)
//  {
//    if (!(this->flags & Sample_MediaTimeValid))
//    {
//      result = VFW_E_MEDIA_TIME_NOT_SET;
//    }
//  }
//
//  if (result == S_OK)
//  {
//    *timeStart = this->mediaStart;
//    *timeEnd = this->mediaEnd;
//  }
//
//  return result;
//}
//
//HRESULT CMediaPacket::SetMediaTime(LONGLONG *timeStart, LONGLONG *timeEnd)
//{
//  HRESULT result = S_OK;
//
//  if (timeStart == NULL)
//  {
//    if (timeEnd == NULL)
//    {
//      this->flags &= ~Sample_MediaTimeValid;
//    }
//    else
//    {
//      result = E_POINTER;
//    }
//  }
//  else
//  {
//    CHECK_POINTER_DEFAULT_HRESULT(result, timeEnd);
//
//    if (result == S_OK)
//    {
//      CHECK_CONDITION(result, *timeEnd >= *timeStart, S_OK, E_INVALIDARG);
//    }
//
//    if (result == S_OK)
//    {
//      this->mediaStart = *timeStart;
//      this->mediaEnd = *timeEnd;
//      this->flags |= Sample_MediaTimeValid;
//    }
//  }
//
//  return result;
//}
//
//HRESULT CMediaPacket::GetMediaType(AM_MEDIA_TYPE **mediaType)
//{
//  HRESULT result = S_OK;
//
//  CHECK_POINTER_DEFAULT_HRESULT(result, mediaType);
//
//  if (result == S_OK)
//  {
//    if (!(this->flags & Sample_TypeChanged))
//    {
//      *mediaType = NULL;
//      result = S_FALSE;
//    }
//    else
//    {
//      // create copy of our media type
//      *mediaType = CreateMediaType(this->mediaType);
//      CHECK_POINTER(result, *mediaType, S_OK, E_OUTOFMEMORY);
//    }
//  }
//
//  return result;
//}
//
//HRESULT CMediaPacket::SetMediaType(AM_MEDIA_TYPE *mediaType)
//{
//  HRESULT result = S_OK;
//
//  // delete current media type
//  if (this->mediaType != NULL)
//  {
//    DeleteMediaType(this->mediaType);
//    this->mediaType = NULL;
//  }
//
//  // reset format type
//  if (mediaType == NULL)
//  {
//    this->flags &= ~Sample_TypeChanged;
//  }
//  else
//  {
//    this->mediaType = CreateMediaType(mediaType);
//
//    if (this->mediaType == NULL)
//    {
//      // clear flag of type changed and return out of memory
//      this->flags &= ~Sample_TypeChanged;
//      result = E_OUTOFMEMORY;
//    }
//    else
//    {
//      // set flag of type changed
//      this->flags |= Sample_TypeChanged;
//    }
//  }
//
//  return result;
//}

DWORD CMediaPacket::GetLastAccessTime(void)
{
  return this->lastAccessTime;
}

 void CMediaPacket::SetLastAccessTime(DWORD accessTime)
 {
   this->lastAccessTime = accessTime;
 }