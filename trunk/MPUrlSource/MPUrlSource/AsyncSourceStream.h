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

#ifndef __ASYNCSOURCESTREAM_DEFINED
#define __ASYNCSOURCESTREAM_DEFINED

#include "ProtocolInterface.h"
#include "AsyncSource.h"
#include "AsyncRequest.h"
#include "AsyncRequestCollection.h"
#include "ParameterCollection.h"
#include "MediaPacketCollection.h"

class CAsyncSource;
class CAsyncRequest;
class CAsyncRequestCollection;

class CAsyncSourceStream : public CBasePin, public IAsyncReader, public IOutputStream
{
public:
  CAsyncSourceStream(__in_opt LPCTSTR pObjectName, __inout HRESULT *phr, __inout CAsyncSource *pms, __in_opt LPCWSTR pName, CParameterCollection *configuration);
#ifdef UNICODE
  CAsyncSourceStream(__in_opt LPCSTR pObjectName, __inout HRESULT *phr, __inout CAsyncSource *pms, __in_opt LPCWSTR pName, CParameterCollection *configuration);
#endif
  ~CAsyncSourceStream(void);  // virtual destructor ensures derived class destructors are called too

  GUID GetInstanceId(void);

  // IOutputStream interface

  // pushes media packet to output pin
  // media packet will be destroyed after processing
  // @param outputPinName : the name of output pin (ignored)
  // @param mediaPacket : reference to media packet to push to output pin
  // @return : STATUS_OK if successful
  int PushMediaPacket(const TCHAR *outputPinName, CMediaPacket *mediaPacket);

  // sets total length of stream to output pin
  // @param outputPinName : the name of output pin (ignored)
  // @param total : total length of stream in bytes
  // @param estimate : specifies if length is estimate
  // @return : STATUS_OK if successful
  int SetTotalLength(const TCHAR *outputPinName, LONGLONG total, bool estimate);
protected:
  // logger for logging purposes
  CLogger logger;

  // the parent of this stream
  CAsyncSource *filter;

  // the collection of asynchronous requests
  CAsyncRequestCollection *requestsCollection;

  // collection of media packets
  CMediaPacketCollection *mediaPacketCollection;

  //  this is set every time we're asked to return an IAsyncReader  interface
  //  this allows us to know if the downstream pin can use  this transport, otherwise we can hook up to thinks like the
  //  dump filter and nothing happens
  bool queriedForAsyncReader;

  // specifies if between BeginFlush / EndFlush
  bool flushing;

  // mutex for accessing requests
  HANDLE requestMutex;

  // mutex for accessing media packets
  HANDLE mediaPacketMutex;

  LONGLONG totalLength;
  bool estimate;

  // configuration provided by filter
  CParameterCollection *configuration;

  DECLARE_IUNKNOWN
  STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void** ppv);

  // IAsyncReader methods

  // pass in your preferred allocator and your preferred properties.
  // method returns the actual allocator to be used. Call GetProperties
  // on returned allocator to learn alignment and prefix etc chosen.
  // this allocator will be not be committed and decommitted by
  // the async reader, only by the consumer.
  STDMETHODIMP RequestAllocator(IMemAllocator *preferred, ALLOCATOR_PROPERTIES *props, IMemAllocator **actual);

  // queue a request for data.
  // media sample start and stop times contain the requested absolute
  // byte position (start inclusive, stop exclusive)
  // may fail if sample not obtained from agreed allocator
  // may fail if start/stop position does not match agreed alignment
  // samples allocated from source pin's allocator may fail
  STDMETHODIMP Request(IMediaSample *sample, DWORD_PTR user);

  // block until the next sample is completed or the timeout occurs.
  // timeout (millisecs) may be 0 or INFINITE. Samples may not
  // be delivered in order. If there is a read error of any sort, a
  // notification will already have been sent by the source filter,
  // and STDMETHODIMP will be an error.
  STDMETHODIMP WaitForNext(DWORD timeout,
    IMediaSample** sample,  // completed sample
    DWORD_PTR * user);     // user context

  // sync read of data. Sample passed in must have been acquired from
  // the agreed allocator. Start and stop position must be aligned.
  // equivalent to a Request/WaitForNext pair, but may avoid the
  // need for a thread on the source filter.
  STDMETHODIMP SyncReadAligned(IMediaSample* sample);

  // performs a synchronous read
  // the method blocks until the request is completed
  // the stream position and the buffer address do not have to be aligned
  // if the request is not aligned, the method performs a buffered read operation
  // @param position : specifies the byte offset at which to begin reading, the method fails if this value is beyond the end of the stream
  // @param length : specifies the number of bytes to read
  // @param buffer : reference to a buffer that receives the data
  // @return : S_OK if successful, S_FALSE if retrieved fewer bytes than requested (probably the end of the stream was reached)
  STDMETHODIMP SyncRead(LONGLONG position, LONG length, BYTE* buffer);

  // retrieves the total length of the stream
  // @param total : pointer to a variable that receives the length of the stream, in bytes
  // @param available : pointer to a variable that receives the portion of the stream that is currently available, in bytes
  // @return : S_OK if success, VFW_S_ESTIMATED if values are estimates, E_UNEXPECTED if error
  STDMETHODIMP Length(LONGLONG *total, LONGLONG *available);

  // IPin methods

  // begins a flush operation
  // @return : S_OK if successful
  STDMETHODIMP BeginFlush(void);

  // ends a flush operation
  // @return : S_OK if successful
  STDMETHODIMP EndFlush(void);

  // CBasePin methods

  // retrieves a preferred media type, by index value
  // @param position : zero-based index value
  // @param mediaType : pointer to a CMediaType object that receives the media type
  // @return : S_OK if successful, VFW_S_NO_MORE_ITEMS if index out of range, E_INVALIDARG if index less than zero, E_UNEXPECTED if error
  HRESULT GetMediaType(int position, CMediaType *mediaType);

  // determines if the pin accepts a specific media type
  // @param mediaType : pointer to a CMediaType object that contains the proposed media type
  // @return : S_OK if the proposed media type is acceptable, S_FALSE otherwise
  HRESULT CheckMediaType(const CMediaType *mediaType);

  // determines whether a pin connection is suitable
  // clear the flag so we see if IAsyncReader is queried for
  // @param pin : pointer to the other pin's IPin interface
  // @return : S_OK if succsessful, VFW_E_INVALID_DIRECTION if pin directions are not compatible
  HRESULT CheckConnect(IPin *pin);

  // completes a connection to another pin
  // @param receivePin : pointer to the other pin's IPin interface
  // @return : S_OK if successful
  HRESULT CompleteConnect(IPin *receivePin);

  // releases the pin from a connection
  // @return : S_OK if successful
  HRESULT BreakConnect();

  // connects the pin to another pin
  // @param receivePin : pointer to the receiving pin's IPin interface
  // @param mediaType : pointer to an AM_MEDIA_TYPE structure that specifies the media type for the connection
  // @return : S_OK if successful
  // @return : VFW_E_ALREADY_CONNECTED if the pin is already connected
  // @return : VFW_E_NO_ACCEPTABLE_TYPES if an acceptable media type could not be find
  // @return : VFW_E_NOT_STOPPED if the filter is active and the pin does not support dynamic reconnection
  // @return : VFW_E_TYPE_NOT_ACCEPTED if the specified media type is not acceptable
  STDMETHODIMP Connect(IPin * receivePin, const AM_MEDIA_TYPE *mediaType);

private:
  // request ID for async requests
  unsigned int requestId;

  HRESULT InitAllocator(IMemAllocator **allocator);

  bool IsAligned(LONG_PTR pointer);
#ifndef _WIN64
  bool IsAligned(LONGLONG number);
#endif

  LONG Alignment();

  // all reader positions, read lengths and memory locations must be aligned to this
  HRESULT Alignment(LONG *alignment);

  HRESULT Request(unsigned int *requestId, LONGLONG position, LONG length, IMediaSample *sample, BYTE *buffer, bool aligned, DWORD_PTR userData);
  //HRESULT Request(LONGLONG position, LONG length, bool aligned, BYTE *buffer, LPVOID context, DWORD_PTR user);
  HRESULT EnqueueAsyncRequest(CAsyncRequest *request);

  // handle for thread which makes relation between CMediaPacket and CAsyncRequest
  HANDLE hAsyncRequestProcessingThread;
  DWORD dwAsyncRequestProcessingThreadId;
  bool asyncRequestProcessingShouldExit;
  static DWORD WINAPI AsyncRequestProcessWorker(LPVOID lpParam);

  // check async request and media packet values agains not valid values
  // @param request : async request
  // @param mediaPacket : media packet
  // @param mediaPacketDataStart : the reference to variable that holds data start within media packet (if successful)
  // @param mediaPacketDataLength : the reference to variable that holds data length within media packet (if successful)
  // @param startTime : start timestamp of data
  // @return : S_OK if successful, error code otherwise
  HRESULT CheckValues(CAsyncRequest *request, CMediaPacket *mediaPacket, unsigned int *mediaPacketDataStart, unsigned int *mediaPacketDataLength, REFERENCE_TIME startTime);

  // creates async request worker
  // @return : S_OK if successful
  HRESULT CreateAsyncRequestProcessWorker(void);

  // destroys async request worker
  // @return : S_OK if successful
  HRESULT DestroyAsyncRequestProcessWorker(void);
};

#endif