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

#ifndef __MEDIAPACKET_DEFINED
#define __MEDIAPACKET_DEFINED

#include "MPUrlSourceExports.h"
#include "LinearBuffer.h"

#include <streams.h>

// CMediaPacket class is wrapper for IMediaSample interface
// this class doesn't implement all methods of IMediaSample interface
class MPURLSOURCE_API CMediaPacket
{
public:
  CMediaPacket(void);
  virtual ~CMediaPacket();

  // gets linear buffer
  // @return : linear buffer  
  LinearBuffer *GetBuffer();

  // gets the stream time at which this packet should start and finish
  // @param timeStart : reference to time start variable
  // @param timeEnd : reference to time end variable
  // @return : S_OK if the packet has valid start and stop times
  // VFW_S_NO_STOP_TIME if the packet has a valid start time, but no stop time
  // VFW_E_SAMPLE_TIME_NOT_SET if the the packet is not time-stamped
  HRESULT GetTime(REFERENCE_TIME *timeStart, REFERENCE_TIME *timeEnd);

  // sets the stream time at which this packet should start and finish
  // @param timeStart : reference to time start variable or NULL if time is reset
  // @param timeEnd : reference to time end variable or NULL if time is reset
  // @return : S_OK if successful
  HRESULT SetTime(REFERENCE_TIME *timeStart, REFERENCE_TIME *timeEnd);

  // deeply clones current instance of media packet
  // @return : deep clone of current instance or NULL if error
  CMediaPacket *Clone(void);

  // deeply clone current instance of media packet with specified time range to new media packet
  // @param timeStart : time start of new media packet
  // @param timeEnd : time end of new media packet
  // @return : new media packet or NULL if error
  CMediaPacket *CreateMediaPacketBasedOnPacket(REFERENCE_TIME timeStart, REFERENCE_TIME timeEnd);

  //// determines if the beginning of this packet is a synchronization point
  //// @return : true if the packet is a synchronization point, false otherwise
  //bool IsSyncPoint(void);

  //// specifies whether the beginning of this packet is a synchronization point
  //// @param isSyncPoint : true if the packet is a synchronization point, false otherwise
  //void SetSyncPoint(bool isSyncPoint);

  //// determines if this packet is a preroll packet, a preroll packet should not be displayed
  //// @return : true if the packet is a preroll packet, false otherwise
  //bool IsPreroll(void);

  //// specifies whether this packet is a preroll packet
  //// @param isPreroll : true if the packet is a preroll packet, false otherwise
  //void SetPreroll(bool isPreroll);

  //// determines if this packet represents a break in the data stream
  //// @return : true if the packet is a break in the data stream, false otherwise
  //bool IsDiscontinuity(void);

  //// specifies whether this sample represents a break in the data stream
  //// @param discontinuity : true if the packet is a break in the data stream, false otherwise
  //void SetDiscontinuity(bool discontinuity);

  //// get the media times (e.g. bytes) for this packet
  //// @param timeStart : reference to time start variable
  //// @param timeEnd : reference to time end variable
  //// @return : S_OK if successful, VFW_E_MEDIA_TIME_NOT_SET if media times are not set on this packet
  //HRESULT GetMediaTime(LONGLONG *timeStart, LONGLONG *timeEnd);

  //// set the media times for this packet
  //// @param timeStart : reference to time start variable or NULL if time is reset
  //// @param timeEnd : reference to time end variable or NULL if time is reset
  //// @return : S_OK if succsessful
  //HRESULT SetMediaTime(LONGLONG *timeStart, LONGLONG *timeEnd);

  //// retrieves the media type, if the media type differs from the previous packet
  //// @param mediaType : reference to a variable that receives a pointer to an AM_MEDIA_TYPE structure, if the media type has not changed from the previous packet, *mediaType is set to NULL
  //// @return : S_OK if successful, S_FALSE if the media type has not changed from the previous sample, E_OUTOFMEMORY if not enough memory
  //HRESULT GetMediaType(AM_MEDIA_TYPE **mediaType);

  //// sets the media type for the packet
  //// @param mediaType : reference to an AM_MEDIA_TYPE structure that specifies the media type
  //// @return : S_OK if successful, E_OUTOFMEMORY if not enough memory
  //HRESULT SetMediaType(AM_MEDIA_TYPE *mediaType);

  // gets last access time (in ticks) to media packet
  // @return : last access time in ticks
  DWORD GetLastAccessTime(void);

  // sets last access time (in ticks)
  // @param accessTime : the access time (in ticks) to set
  void SetLastAccessTime(DWORD accessTime);

protected:
  // internal linear buffer for media data
  LinearBuffer *buffer;

  // start sample time
  REFERENCE_TIME start;
  // end sample time
  REFERENCE_TIME end;

  //// real media start position
  //LONGLONG mediaStart;
  //// real media end position
  //LONGLONG mediaEnd;
  //// media type change data
  //AM_MEDIA_TYPE *mediaType;

  // values for dwFlags - these are used for backward compatiblity only now - use AM_SAMPLE_xxx
  enum
  {
    Sample_SyncPoint       = 0x01,   /* Is this a sync point */
    Sample_Preroll         = 0x02,   /* Is this a preroll sample */
    Sample_Discontinuity   = 0x04,   /* Set if start of new segment */
    Sample_TypeChanged     = 0x08,   /* Has the type changed */
    Sample_TimeValid       = 0x10,   /* Set if time is valid */
    Sample_MediaTimeValid  = 0x20,   /* Is the media time valid */
    Sample_TimeDiscontinuity = 0x40, /* Time discontinuity */
    Sample_StopValid       = 0x100,  /* Stop time valid */
    Sample_ValidFlags      = 0x1FF
  };

  // flags for this packet
  // type specific flags are packed into the top word
  DWORD flags;

  // holds when was last access to media packet data
  DWORD lastAccessTime;
};

#endif

