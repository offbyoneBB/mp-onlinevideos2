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

#ifndef __ASYNCREQUEST_DEFINED
#define __ASYNCREQUEST_DEFINED

#include "MPUrlSourceExports.h"
#include "AsyncSourceStream.h"

#include <streams.h>

class CAsyncSourceStream;

class MPURLSOURCE_API CAsyncRequest
{
public:
  CAsyncRequest(void);
  ~CAsyncRequest(void);

  enum AsyncState
  {
    Created,
    Waiting,
    Requested,
    Completed,
    Cancelled
  };

private:
  // stream which created this request
  CAsyncSourceStream *stream;

  // starting position of requested data
  LONGLONG position;

  //// flag if data have to be aligned
  //bool aligned;

  // the length of requested data 
  LONG length;

  // media sample
  IMediaSample *sample;

  // the buffer where to write requested data
  BYTE *buffer;

  // request state
  AsyncState requestState;

  // error code when completed async request
  HRESULT errorCode;

  // specifies an arbitrary value that is returned when the request completes
  DWORD_PTR userData;

  // specifies request ID
  unsigned int requestId;
public:
  // init the parameters for this request
  // @param requestId :
  // @param stream : 
  // @param position :
  // @param length :
  // @param sample :
  // @param buffer :
  // @return : S_OK if successful, E_POINTER if stream or buffer is NULL
  HRESULT Request(unsigned int requestId, CAsyncSourceStream *stream, LONGLONG position, LONG length, IMediaSample *sample, BYTE *buffer, DWORD_PTR userData);

  // mark request as completed
  // @param errorCode : the error code of async request
  void Complete(HRESULT errorCode);

  // mark request as cancelled
  void Cancel(void);

  // mark request as requested from filter
  void Request(void);

  // gets current state of async request
  // @return : one of AsyncState values
  AsyncState GetState(void);

  // gets media sample
  // @return : reference to IMediaSample
  IMediaSample *GetMediaSample(void);

  // gets buffer length
  // @return : buffer length
  LONG GetBufferLength(void);

  // sets buffer length
  // @param length : the length to set
  void SetBufferLength(LONG length);

  // gets start position
  // @return : start position
  LONGLONG GetStart(void);

  // returns error code
  // @return : error code
  HRESULT GetErrorCode(void);

  // gets an arbitrary value that is returned when the request completes
  // @return : reference to arbitrary value
  DWORD_PTR GetUserData(void);

  // gets request ID
  // @return : request ID
  unsigned int GetRequestId(void);

  // gets buffer for writing data
  // @return : buffer for writing data
  BYTE *GetBuffer(void);
};

#endif

