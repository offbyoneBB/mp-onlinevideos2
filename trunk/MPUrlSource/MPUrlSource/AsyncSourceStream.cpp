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
#include "AsyncSourceStream.h"
#include "ProtocolInterface.h"
#include "Utilities.h"
#include "LockMutex.h"

#include <stdio.h>
#include <ShlObj.h>

#define MODULE_NAME                                               _T("AsyncSourceStream")

#define METHOD_CHECK_MEDIA_TYPE_NAME                              _T("CheckMediaType()")
#define METHOD_GET_MEDIA_TYPE_NAME                                _T("GetMediaType()")
#define METHOD_CHECK_CONNECT_NAME                                 _T("CheckConnect()")
#define METHOD_COMPLETE_CONNECT_NAME                              _T("CompleteConnect()")
#define METHOD_BREAK_CONNECT_NAME                                 _T("BreakConnect()")
#define METHOD_CONNECT_NAME                                       _T("Connect()")

#define METHOD_INIT_ALLOCATOR_NAME                                _T("InitAllocator()")
#define METHOD_REQUEST_ALLOCATOR_NAME                             _T("RequestAllocator()")
#define METHOD_REQUEST_NAME                                       _T("Request()")
#define METHOD_SYNC_READ_ALIGNED_NAME                             _T("SyncReadAligned()")
#define METHOD_WAIT_FOR_NEXT_NAME                                 _T("WaitForNext()")
#define METHOD_SYNC_READ_NAME                                     _T("SyncRead()")
#define METHOD_LENGTH_NAME                                        _T("Length()")
#define METHOD_BEGIN_FLUSH_NAME                                   _T("BeginFlush()")
#define METHOD_END_FLUSH_NAME                                     _T("EndFlush()")
#define METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME           _T("CreateAsyncRequestProcessWorker()")
#define METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME          _T("DestroyAsyncRequestProcessWorker()")
#define METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME                  _T("AsyncRequestProcessWorker()")


CAsyncSourceStream::CAsyncSourceStream(__in_opt LPCTSTR pObjectName, __inout HRESULT *phr, __inout CAsyncSource *ps, __in_opt LPCWSTR pPinName, CParameterCollection *configuration)
  : CBasePin(pObjectName, ps, ps->GetStateLock(), phr, pPinName, PINDIR_OUTPUT)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);

  this->filter = ps;
  this->requestsCollection = new CAsyncRequestCollection();
  *phr = this->filter->AddPin(this);
  this->queriedForAsyncReader = false;
  this->flushing = false;
  this->mediaPacketCollection = new CMediaPacketCollection();
  this->totalLength = 0;
  this->estimate = false;
  this->asyncRequestProcessingShouldExit = false;
  this->requestId = 0;
  this->requestMutex = CreateMutex(NULL, FALSE, NULL);
  this->mediaPacketMutex = CreateMutex(NULL, FALSE, NULL);
  this->configuration = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configuration->Append(configuration);
  }

  this->storeFilePath = NULL;
  this->connectedToAnotherPin = false;

  this->CreateAsyncRequestProcessWorker();

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
}


#ifdef UNICODE
CAsyncSourceStream::CAsyncSourceStream(__in_opt LPCSTR pObjectName, __inout HRESULT *phr, __inout CAsyncSource *ps, __in_opt LPCWSTR pPinName, CParameterCollection *configuration)
  : CBasePin(pObjectName, ps, ps->GetStateLock(), phr, pPinName, PINDIR_OUTPUT)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);

  this->filter = ps;
  this->requestsCollection = new CAsyncRequestCollection();
  *phr = this->filter->AddPin(this);
  this->queriedForAsyncReader = false;
  this->flushing = false;
  this->mediaPacketCollection = new CMediaPacketCollection();
  this->totalLength = 0;
  this->estimate = false;
  this->asyncRequestProcessingShouldExit = false;
  this->requestId = 0;
  this->requestMutex = CreateMutex(NULL, FALSE, NULL);
  this->mediaPacketMutex = CreateMutex(NULL, FALSE, NULL);
  this->configuration = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configuration->Append(configuration);
  }

  this->storeFilePath = NULL;
  this->connectedToAnotherPin = false;

  this->CreateAsyncRequestProcessWorker();

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
}
#endif

CAsyncSourceStream::~CAsyncSourceStream(void)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);

  this->DestroyAsyncRequestProcessWorker();

  // decrements the number of pins on this filter
  this->filter->RemovePin(this);
  delete this->requestsCollection;
  delete this->mediaPacketCollection;
  delete this->configuration;

  if (this->requestMutex != NULL)
  {
    CloseHandle(this->requestMutex);
    this->requestMutex = NULL;
  }
  if (this->mediaPacketMutex != NULL)
  {
    CloseHandle(this->mediaPacketMutex);
    this->mediaPacketMutex = NULL;
  }
  if (this->storeFilePath != NULL)
  {
    DeleteFile(this->storeFilePath);
  }

  FREE_MEM(this->storeFilePath);
  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);
}

STDMETHODIMP CAsyncSourceStream::NonDelegatingQueryInterface(REFIID riid, void** ppv)
{
  CheckPointer(ppv, E_POINTER);

  if(riid == IID_IAsyncReader)
  {
    this->queriedForAsyncReader = true;
    return GetInterface((IAsyncReader *)this, ppv);
  }
  else
  {
    return CBasePin::NonDelegatingQueryInterface(riid, ppv);
  }
}

// CBasePin methods

HRESULT CAsyncSourceStream::GetMediaType(int position, CMediaType *mediaType)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_GET_MEDIA_TYPE_NAME);
  HRESULT result = S_OK;

  if (position < 0)
  {
    this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: bad position request: %i"), MODULE_NAME, METHOD_GET_MEDIA_TYPE_NAME, position);
    result = E_INVALIDARG;
  }

  if ((result == S_OK) && (position > 0))
  {
    this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: bad position request: %i"), MODULE_NAME, METHOD_GET_MEDIA_TYPE_NAME, position);
    result = VFW_S_NO_MORE_ITEMS;
  }

  if (result == S_OK)
  {    
    if (mediaType == NULL)
    {
      this->logger.Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_GET_MEDIA_TYPE_NAME, _T("bad pointer to media type"));
      result = E_POINTER;
    }

    if (result == S_OK)
    {
      mediaType->majortype = MEDIATYPE_Stream;
      mediaType->subtype = MEDIASUBTYPE_NULL;
      mediaType->bTemporalCompression = FALSE;
      mediaType->lSampleSize = 1;
    }
  }

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_GET_MEDIA_TYPE_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::CheckMediaType(const CMediaType* mediaType)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_CHECK_MEDIA_TYPE_NAME);
  HRESULT result = S_FALSE;

  TCHAR *majorType = (mediaType != NULL) ? ConvertGuidToString(mediaType->majortype) : NULL;
  TCHAR *subType = (mediaType != NULL) ? ConvertGuidToString(mediaType->subtype) : NULL;

  this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: major type: '%s', subtype: '%s'"), MODULE_NAME, METHOD_CHECK_MEDIA_TYPE_NAME, majorType, subType);

  FREE_MEM(majorType);
  FREE_MEM(subType);

  if ((mediaType->majortype == MEDIATYPE_Stream) &&
      (mediaType->subtype == MEDIASUBTYPE_NULL))
  {
    result = S_OK;
  }

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CHECK_MEDIA_TYPE_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::CheckConnect(IPin *pin)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_CHECK_CONNECT_NAME);

  this->queriedForAsyncReader = false;
  HRESULT result = this->CBasePin::CheckConnect(pin);

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CHECK_CONNECT_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::CompleteConnect(IPin *receivePin)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_COMPLETE_CONNECT_NAME);

  HRESULT result = S_OK;
  if (this->queriedForAsyncReader)
  {
    result = this->CBasePin::CompleteConnect(receivePin);
  }
  else
  {
#ifdef VFW_E_NO_TRANSPORT
    result = VFW_E_NO_TRANSPORT;
#else
    result = E_FAIL;
#endif
  }

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_COMPLETE_CONNECT_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::BreakConnect()
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_BREAK_CONNECT_NAME);

  this->queriedForAsyncReader = false;
  HRESULT result = this->CBasePin::BreakConnect();
  this->connectedToAnotherPin = false;

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_BREAK_CONNECT_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::Connect(IPin * receivePin, const AM_MEDIA_TYPE *mediaType)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONNECT_NAME);

  HRESULT result = this->CBasePin::Connect(receivePin, mediaType);

  this->connectedToAnotherPin = (result == S_OK);

  this->logger.Log(LOGGER_VERBOSE, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CONNECT_NAME, result);
  return result;
}

// IAsyncReader methods

STDMETHODIMP CAsyncSourceStream::RequestAllocator(IMemAllocator *preferred, ALLOCATOR_PROPERTIES *props, IMemAllocator **actual)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_REQUEST_ALLOCATOR_NAME);

  CAutoLock lock(this->m_pLock);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, preferred);
  CHECK_POINTER_DEFAULT_HRESULT(result, props);
  CHECK_POINTER_DEFAULT_HRESULT(result, actual);

  if (SUCCEEDED(result))
  {
    // we care about alignment but nothing else
    if (!props->cbAlign || !this->IsAligned(props->cbAlign))
    {
      result = this->Alignment(&props->cbAlign);
    }
  }

  if (SUCCEEDED(result))
  {
    ALLOCATOR_PROPERTIES Actual;

    if (preferred)
    {
      result = preferred->SetProperties(props, &Actual);

      if (SUCCEEDED(result) && this->IsAligned(Actual.cbAlign))
      {
        preferred->AddRef();
        *actual = preferred;
        result = S_OK;
      }
    }
    else
    {
      // create our own allocator
      IMemAllocator* pAlloc;
      result = this->InitAllocator(&pAlloc);

      if (SUCCEEDED(result))
      {
        // ... and see if we can make it suitable
        result = pAlloc->SetProperties(props, &Actual);
        if (SUCCEEDED(result) && this->IsAligned(Actual.cbAlign))
        {
          // we need to release our refcount on pAlloc, and addref
          // it to pass a refcount to the caller - this is a net nothing.
          *actual = pAlloc;
          result = S_OK;
        }
        else
        {
          // failed to find a suitable allocator
          pAlloc->Release();

          // if we failed because of the IsAligned test, the error code will
          // not be failure
          if (SUCCEEDED(result))
          {
            result = VFW_E_BADALIGN;
          }
        }
      }
    }
  }

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_REQUEST_ALLOCATOR_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::Request(IMediaSample *sample, DWORD_PTR user)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_REQUEST_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, sample);

  if (SUCCEEDED(result))
  {
    REFERENCE_TIME tStart, tStop;
    result = sample->GetTime(&tStart, &tStop);

    if (SUCCEEDED(result))
    {
      LONGLONG position = tStart / UNITS;
      LONG length = (LONG) ((tStop - tStart) / UNITS);

      LONGLONG total = 0, available = 0;

      result = this->Length(&total, &available);
      if (position + length > total)
      {
        // the end needs to be aligned, but may have been aligned on a coarser alignment
        LONG alignment;
        this->Alignment(&alignment);

        total = (total + alignment - 1) & ~(alignment - 1);

        if (position + length > total)
        {
          length = (LONG)(total - position);

          // must be reducing this!
          //ASSERT((llTotal * UNITS) <= tStop);
          tStop = total * UNITS;
          sample->SetTime(&tStart, &tStop);
        }
      }

      BYTE* buffer;
      result = sample->GetPointer(&buffer);
      if (SUCCEEDED(result))
      {
        result = this->Request(NULL, position, length, sample, buffer, true, user);
      }
    }
  }

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_REQUEST_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::WaitForNext(DWORD timeout, IMediaSample **sample, DWORD_PTR *user)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_WAIT_FOR_NEXT_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, sample);
  CHECK_POINTER_DEFAULT_HRESULT(result, user);

  if (SUCCEEDED(result))
  {
    result = VFW_E_TIMEOUT;
    *sample = NULL;
    DWORD ticks = GetTickCount();

    // wait until request is completed or cancelled
    while (((GetTickCount() - ticks) <= timeout) && (!this->asyncRequestProcessingShouldExit))
    {
      if (this->flushing)
      {
        // if we are flushing return wrong state
        result = VFW_E_WRONG_STATE;
        break;
      }

      {
        // lock access to async request collection
        CLockMutex lock(this->requestMutex, INFINITE);
        // try to find any request which has completed state
        unsigned int requestCount = this->requestsCollection->Count();
        for (unsigned int i = 0; i < requestCount; i++)
        {
          CAsyncRequest *request = this->requestsCollection->GetItem(i);

          if (request->GetState() == CAsyncRequest::Completed)
          {
            // found async request with completed state
            result = request->GetErrorCode();

            // return IMediaSample provided to request
            (*sample) = request->GetMediaSample();
            // return user data provided to request
            (*user) = request->GetUserData();
            // set actual bytes read
            if ((*sample) != NULL)
            {
              (*sample)->SetActualDataLength(request->GetBufferLength());
            }

            // remove processed request
            this->requestsCollection->Remove(i);

            break;
          }
        }
      }

      // sleep some time
      Sleep(1);
    }
  }

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_WAIT_FOR_NEXT_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::SyncReadAligned(IMediaSample *sample)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SYNC_READ_ALIGNED_NAME);
  //CLockMutex lock(this->lockMutex, INFINITE);

  HRESULT result = E_NOTIMPL;

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SYNC_READ_ALIGNED_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::SyncRead(LONGLONG position, LONG length, BYTE *buffer)
{
  this->logger.Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME);

  HRESULT result = S_OK;
  CHECK_CONDITION(result, length != 0, S_OK, E_INVALIDARG);
  CHECK_POINTER_DEFAULT_HRESULT(result, buffer);

  if (SUCCEEDED(result))
  {
    //if (this->IsAligned(position) && this->IsAligned(length) && IsAligned((LONG_PTR)buffer))
    //{
    //  this->logger.Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, _T("aligned sync read"));
    //  //LONG unused;
    //  //result = this->SyncReadAligned(position, length, buffer, &cbUnused, NULL);
    //}
    //else
    {
      unsigned int requestId = 0;
      result = this->Request(&requestId, position, length, NULL, buffer, false, NULL);

      if (SUCCEEDED(result))
      {
        DWORD ticks = GetTickCount();
        DWORD timeout = this->filter->GetReceiveDataTimeout();

        result = (timeout != UINT_MAX) ? S_OK : E_UNEXPECTED;

        if (SUCCEEDED(result))
        {
          bool rangesSupported = false;
          bool rangesPending = false;
          result = this->filter->QueryRangesSupported(&rangesSupported);

          // if ranges are not supported than we must wait for data

          switch (result)
          {
          case S_OK:
            rangesPending = false;
            break;
          case E_PENDING:
            rangesSupported = false;
            rangesPending = true;
            break;
          default:
            rangesSupported = false;
            rangesPending = false;
            break;
          }

          result = VFW_E_TIMEOUT;
          this->logger.Log(LOGGER_DATA, _T("%s: %s: requesting data from position: %llu, length: %lu"), MODULE_NAME, METHOD_SYNC_READ_NAME, position, length);

          // wait until request is completed or cancelled
          while (!this->asyncRequestProcessingShouldExit)
          {
            if (rangesPending)            
            {
              // protocol implementation doesn't know yet if ranges are supported
              HRESULT pendingResult = this->filter->QueryRangesSupported(&rangesSupported);
              switch (pendingResult)
              {
              case S_OK:
                rangesPending = false;
                break;
              case E_PENDING:
                rangesSupported = false;
                rangesPending = true;
                break;
              default:
                rangesSupported = false;
                rangesPending = false;
                break;
              }
            }

            if ((rangesSupported) && ((GetTickCount() - ticks) > timeout))
            {
              // if ranges are supported and timeout occured then stop waiting for data and exit with VFW_E_TIMEOUT error
              result = VFW_E_TIMEOUT;
              break;
            }

            CAsyncRequest *request = NULL;

            {
              // lock access to collection
              CLockMutex lock(this->requestMutex, INFINITE);
              request = this->requestsCollection->GetRequest(requestId);

              this->logger.Log(LOGGER_DATA, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, _T("checking request"));

              if (request != NULL)
              {

                if ((!this->estimate) && (request->GetStart() >= this->totalLength))
                {
                  // something bad occured
                  // graph requests data that are beyond stream (data doesn't exists)
                  this->logger.Log(LOGGER_WARNING, _T("%s: %s: graph requests data beyond stream, stream total length: %llu, request start: %llu"), MODULE_NAME, METHOD_SYNC_READ_NAME, this->totalLength, request->GetStart());
                  // complete result with error code
                  request->Complete(E_FAIL);
                }

                if (request->GetState() == CAsyncRequest::Completed)
                {
                  // request is completed
                  result = request->GetErrorCode();
                  this->logger.Log(LOGGER_DATA, _T("%s: %s: returned data length: %lu, result: 0x%08X"), MODULE_NAME, METHOD_SYNC_READ_NAME, request->GetBufferLength(), result);
                  break;
                }
                else if (request->GetState() == CAsyncRequest::Cancelled)
                {
                  // request is cancelled
                  result = E_ABORT;
                  break;
                }
              }
              else
              {
                // request should not disappear before is processed
                result = E_FAIL;
                this->logger.Log(LOGGER_WARNING, _T("%s: %s: request '%u' disappeared before processed"), MODULE_NAME, METHOD_SYNC_READ_NAME, request->GetRequestId());
                break;
              }
            }

            // sleep some time
            Sleep(10);
          }
        }

        {
          // lock access to collection
          CLockMutex lock(this->requestMutex, INFINITE);                
          if (!this->requestsCollection->Remove(this->requestsCollection->GetRequestIndex(requestId)))
          {
            this->logger.Log(LOGGER_WARNING, _T("%s: %s: request '%u' cannot be removed"), METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, requestId);
          }
        }

        if (FAILED(result))
        {
          this->logger.Log(LOGGER_WARNING, _T("%s: %s: requesting data from position: %llu, length: %lu, request id: %u, result: 0x%08X"), MODULE_NAME, METHOD_SYNC_READ_NAME, position, length, requestId, result);
        }
      }
    }
  }

  this->logger.Log(SUCCEEDED(result) ? LOGGER_DATA : LOGGER_WARNING, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::Length(LONGLONG *total, LONGLONG *available)
{
  this->logger.Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_LENGTH_NAME);
  CLockMutex lock(this->mediaPacketMutex, INFINITE);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, available);

  if (result == S_OK)
  {
    *total = this->totalLength;    
    *available = (this->connectedToAnotherPin) ? this->totalLength : 0;
    unsigned int mediaPacketCount = this->mediaPacketCollection->Count();
    
    result = (this->connectedToAnotherPin) ? S_OK : this->filter->QueryStreamAvailableLength(available);
    if (result != S_OK)
    {
      this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: cannot query available stream length, result: 0x%08X"), MODULE_NAME, METHOD_LENGTH_NAME, result);
      *available = 0;
      for (unsigned int i = 0; i < mediaPacketCount; i++)
      {
        CMediaPacket *mediaPacket = this->mediaPacketCollection->GetItem(i);
        REFERENCE_TIME mediaPacketStart = 0;
        REFERENCE_TIME mediaPacketEnd = 0;

        if (mediaPacket->GetTime(&mediaPacketStart, &mediaPacketEnd) == S_OK)
        {
          if ((mediaPacketEnd + 1) > (*available))
          {
            *available = mediaPacketEnd + 1;
          }
        }
      }

      result = S_OK;
    }

    result = (this->estimate) ? VFW_S_ESTIMATED : S_OK;
    this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: total length: %llu, available length: %llu, estimate: %u, media packets: %u"), MODULE_NAME, METHOD_LENGTH_NAME, this->totalLength, *available, (this->estimate) ? 1 : 0, mediaPacketCount);
  }

  this->logger.Log(LOGGER_VERBOSE, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_LENGTH_NAME, result);
  return result;
}

// private methods

HRESULT CAsyncSourceStream::InitAllocator(IMemAllocator **allocator)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_INIT_ALLOCATOR_NAME);

  HRESULT result = (allocator != NULL) ? S_OK : E_POINTER;
  CMemAllocator *pMemObject = NULL;
  *allocator = NULL;

  // create a default memory allocator
  pMemObject = new CMemAllocator(NAME("Base memory allocator"), NULL, &result);
  if (pMemObject == NULL)
  {
    result = E_OUTOFMEMORY;
  }

  if (FAILED(result))
  {
    delete pMemObject;
  }

  if (SUCCEEDED(result))
  {
    // get a reference counted IID_IMemAllocator interface
    result = pMemObject->QueryInterface(IID_IMemAllocator, (void **)allocator);
    if (FAILED(result))
    {
      delete pMemObject;
      result = E_NOINTERFACE;
    }
  }

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_INIT_ALLOCATOR_NAME, result);
  return result;
}

bool CAsyncSourceStream::IsAligned(LONG_PTR pointer)
{
  // LONG_PTR is long on 32-bit or __int64 on 64-bit.
  if ((static_cast<LONG>(pointer & 0xFFFFFFFF) & (this->Alignment() - 1)) == 0)
  {
    return true;
  } 
  else 
  {
    return false;
  }

}

#ifndef _WIN64
bool CAsyncSourceStream::IsAligned(LONGLONG number)
{
  return this->IsAligned((LONG)(number & 0xFFFFFFFF));
};
#endif

LONG CAsyncSourceStream::Alignment()
{
  return 1;
}

HRESULT CAsyncSourceStream::Alignment(LONG *alignment)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, alignment);

  if (result == S_OK)
  {
    *alignment = this->Alignment();
  }

  return result;
}



HRESULT CAsyncSourceStream::Request(unsigned int *requestId, LONGLONG position, LONG length, IMediaSample *sample, BYTE *buffer, bool aligned, DWORD_PTR userData)
{
  if (aligned)
  {
    if (!this->IsAligned(position) || !IsAligned(length) || !IsAligned((LONG_PTR)buffer))
    {
      return VFW_E_BADALIGN;
    }
  }

  CAsyncRequest* request = new CAsyncRequest();
  if (!request)
  {
    return E_OUTOFMEMORY;
  }

  HRESULT result = request->Request(this->requestId++, this, position, length, sample, buffer, userData);

  if (SUCCEEDED(result))
  {
    // might fail if flushing
    result = EnqueueAsyncRequest(request);
  }

  if (FAILED(result))
  {
    delete request;
  }
  else
  {
    if (requestId != NULL)
    {
      *requestId = request->GetRequestId();
    }
  }

  return result;
}

HRESULT CAsyncSourceStream::EnqueueAsyncRequest(CAsyncRequest *request)
{
  CLockMutex lock(this->requestMutex, INFINITE);

  HRESULT result = (request != NULL) ? S_OK : E_POINTER;

  if (this->flushing)
  {
    result = VFW_E_WRONG_STATE;
  }
  else if (this->requestsCollection->Add(request))
  {
    // request correctly added
    result = S_OK;
  }
  else
  {
    result = E_OUTOFMEMORY;
  }

  return result;
}

//HRESULT CAsyncSourceStream::Request(LONGLONG llPos, LONG lLength, BOOL bAligned, BYTE * pBuffer, LPVOID pContext, DWORD_PTR dwUser)
//{
//  if (bAligned)
//  {
//    if (!IsAligned(llPos) || !IsAligned(lLength) || !IsAligned((LONG_PTR) pBuffer))
//    {
//      return VFW_E_BADALIGN;
//    }
//  }
//
//  CAsyncRequest* pRequest = new CAsyncRequest();
//  if (!pRequest)
//  {
//    return E_OUTOFMEMORY;
//  }
//
//  HRESULT result = pRequest->Request(this, llPos, lLength, bAligned, pBuffer, pContext, dwUser);
//
//  if (SUCCEEDED(result))
//  {
//    // might fail if flushing
//    result = EnqueueAsyncRequest(pRequest);
//  }
//
//  if (FAILED(result))
//  {
//    delete pRequest;
//  }
//
//  return result;
//}

//HRESULT CAsyncSourceStream::EnqueueAsyncRequest(CAsyncRequest *request)
//{
//  CAutoLock lock(this->m_pLock);
//
//  HRESULT result = (request != NULL) ? S_OK : E_POINTER;
//
//  if (this->m_bFlushing)
//  {
//    result = VFW_E_WRONG_STATE;
//  }
//  else if (this->m_pRequests->Add(request))
//  {
//    // request correctly added
//    result = S_OK;
//  }
//  else
//  {
//    result = E_OUTOFMEMORY;
//  }
//
//  return result;
//}

//STDMETHODIMP CAsyncSourceStream::SyncReadAligned(IMediaSample* pSample)
//{
//  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SYNC_READ_ALIGNED_NAME);
//
//  HRESULT result = E_FAIL;
//
//  //CheckPointer(pSample, E_POINTER);
//
//  //REFERENCE_TIME tStart, tStop;
//  //HRESULT hr = pSample->GetTime(&tStart, &tStop);
//  //if (FAILED(hr))
//  //{
//  //  return hr;
//  //}
//
//  //LONGLONG llPos = tStart / UNITS;
//  //LONG lLength = (LONG) ((tStop - tStart) / UNITS);
//
//  //LONGLONG llTotal;
//  //LONGLONG llAvailable;
//
//  //hr = this->Length(&llTotal, &llAvailable);
//  //if (llPos + lLength > llTotal)
//  //{
//  //  // the end needs to be aligned, but may have been aligned
//  //  // on a coarser alignment.
//  //  LONG lAlign;
//  //  this->Alignment(&lAlign);
//
//  //  llTotal = (llTotal + lAlign -1) & ~(lAlign-1);
//
//  //  if (llPos + lLength > llTotal)
//  //  {
//  //    lLength = (LONG) (llTotal - llPos);
//
//  //    // must be reducing this!
//  //    ASSERT((llTotal * UNITS) <= tStop);
//  //    tStop = llTotal * UNITS;
//  //    pSample->SetTime(&tStart, &tStop);
//  //  }
//  //}
//
//  //BYTE* pBuffer;
//  //hr = pSample->GetPointer(&pBuffer);
//  //if (FAILED(hr))
//  //{
//  //  return hr;
//  //}
//
//  //LONG cbActual;
//  //hr = m_pIo->SyncReadAligned(llPos, lLength, pBuffer, &cbActual, pSample);
//
//  //pSample->SetActualDataLength(cbActual);
//  //return hr;
//
//  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SYNC_READ_ALIGNED_NAME, result);
//  return result;
//}

//STDMETHODIMP CAsyncSourceStream::WaitForNext(DWORD dwTimeout, IMediaSample** ppSample, DWORD_PTR * pdwUser)
//{
//  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_WAIT_FOR_NEXT_NAME);
//
//  HRESULT result = E_FAIL;
//
//  /*CheckPointer(ppSample, E_POINTER);
//
//  LONG cbActual;
//  IMediaSample *pSample = NULL;
//
//  HRESULT hr = m_pIo->WaitForNext(dwTimeout, (LPVOID*) &pSample, pdwUser, &cbActual);
//
//  if (SUCCEEDED(hr))
//  {
//    pSample->SetActualDataLength(cbActual);
//  }
//
//  *ppSample = pSample;
//  return hr;*/
//
//  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_WAIT_FOR_NEXT_NAME, result);
//  return result;
//}




STDMETHODIMP CAsyncSourceStream::BeginFlush(void)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_BEGIN_FLUSH_NAME);
  CLockMutex requestLock(this->requestMutex, INFINITE);
  CLockMutex mediaPacketLock(this->mediaPacketMutex, INFINITE);

  // set the flushing begins
  this->flushing = true;

  // mark all requests as cancelled
  unsigned int requestCount = this->requestsCollection->Count();
  for (unsigned int i = 0; i < requestCount; i++)
  {
    CAsyncRequest *request = this->requestsCollection->GetItem(i);
    request->Cancel();
  }

  // remove all media packets
  this->mediaPacketCollection->Clear();

  HRESULT result = S_OK;

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_BEGIN_FLUSH_NAME, result);
  return result;
}

STDMETHODIMP CAsyncSourceStream::EndFlush(void)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_END_FLUSH_NAME);
  CLockMutex requestLock(this->requestMutex, INFINITE);
  CLockMutex mediaPacketLock(this->mediaPacketMutex, INFINITE);

  // set the flushing ends
  this->flushing = false;

  HRESULT result = S_OK;

  this->logger.Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_END_FLUSH_NAME, result);
  return result;
}

GUID CAsyncSourceStream::GetInstanceId(void)
{
  return this->logger.loggerInstance;
}

int CAsyncSourceStream::PushMediaPacket(const TCHAR *outputPinName, CMediaPacket *mediaPacket)
{
  CLockMutex lock(this->mediaPacketMutex, INFINITE);
  int result = STATUS_ERROR;

  if (mediaPacket != NULL)
  {
    CMediaPacketCollection *unprocessedMediaPackets = new CMediaPacketCollection();
    if (unprocessedMediaPackets->Add(mediaPacket->Clone()))
    {
      result = STATUS_OK;
      while ((unprocessedMediaPackets->Count() != 0) && (result == STATUS_OK))
      {
        // there is still some unprocessed media packets
        // get first media packet
        CMediaPacket *unprocessedMediaPacket = unprocessedMediaPackets->GetItem(0);

        REFERENCE_TIME unprocessedMediaPacketStart = 0;
        REFERENCE_TIME unprocessedMediaPacketEnd = 0;
        result = (unprocessedMediaPacket->GetTime(&unprocessedMediaPacketStart, &unprocessedMediaPacketEnd) == S_OK) ? STATUS_OK : STATUS_ERROR;

        if (result == STATUS_OK)
        {
          // try to find overlapping media packet
          CMediaPacket *overlappingPacket = this->mediaPacketCollection->GetItem(this->mediaPacketCollection->GetMediaPacketIndexOverlappingTimes(unprocessedMediaPacketStart, unprocessedMediaPacketEnd));
          if (overlappingPacket == NULL)
          {
            // there isn't overlapping media packet
            // whole packet can be added to collection
            result = (this->mediaPacketCollection->Add(unprocessedMediaPacket->Clone())) ? STATUS_OK : STATUS_ERROR;
          }
          else
          {
            // current unprocessed media packet is overlapping some media packet in media packet collection
            // it means that this packet has same data (in overlapping range)
            // there is no need to duplicate data in collection

            REFERENCE_TIME overlappingMediaPacketStart = 0;
            REFERENCE_TIME overlappingMediaPacketEnd = 0;
            result = (overlappingPacket->GetTime(&overlappingMediaPacketStart, &overlappingMediaPacketEnd) == S_OK) ? STATUS_OK : STATUS_ERROR;
            
            if (result == STATUS_OK)
            {
              // we get both media packets start and end
              if (unprocessedMediaPacketStart < overlappingMediaPacketStart)
              {
                // split unprocessed media packet into two parts
                // insert them into unprocessed media packet collection

                // initialize first part
                REFERENCE_TIME start = unprocessedMediaPacketStart;
                REFERENCE_TIME end = overlappingMediaPacketStart - 1;
                CMediaPacket *firstPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                // initialize second part
                start = overlappingMediaPacketStart;
                end = unprocessedMediaPacketEnd;
                CMediaPacket *secondPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                result = ((firstPart != NULL) && (secondPart != NULL)) ? STATUS_OK : STATUS_ERROR;

                if (result == STATUS_OK)
                {
                  // delete first media packet because it is processed
                  if (!unprocessedMediaPackets->Remove(0))
                  {
                    // some error occured
                    result = STATUS_ERROR;
                  }
                }
                
                if (result == STATUS_OK)
                {
                  // both media packets are created correctly
                  // now add both packets to unprocessed media collection

                  result = (unprocessedMediaPackets->Add(firstPart)) ? STATUS_OK : STATUS_ERROR;

                  if (result == STATUS_OK)
                  {
                    result = (unprocessedMediaPackets->Add(secondPart)) ? STATUS_OK : STATUS_ERROR;

                    if (result == STATUS_ERROR)
                    {
                      // second part wasn't added to media collection
                      delete secondPart;
                    }
                  }
                  else
                  {
                    // first part wasn't added to media collection
                    delete firstPart;
                    delete secondPart;
                  }
                }
                else
                {
                  // some error occured
                  // both media packets must be destroyed

                  if (firstPart != NULL)
                  {
                    delete firstPart;
                  }
                  if (secondPart != NULL)
                  {
                    delete secondPart;
                  }
                }
              }
              else if (unprocessedMediaPacketEnd > overlappingMediaPacketEnd)
              {
                // split unprocessed media packet into two parts
                // insert them into unprocessed media packet collection

                // initialize first part
                REFERENCE_TIME start = unprocessedMediaPacketStart;
                REFERENCE_TIME end = overlappingMediaPacketEnd;
                CMediaPacket *firstPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                // initialize second part
                start = overlappingMediaPacketEnd + 1;
                end = unprocessedMediaPacketEnd;
                CMediaPacket *secondPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                result = ((firstPart != NULL) && (secondPart != NULL)) ? STATUS_OK : STATUS_ERROR;

                if (result == STATUS_OK)
                {
                  // delete first media packet because it is processed
                  if (!unprocessedMediaPackets->Remove(0))
                  {
                    // some error occured
                    result = STATUS_ERROR;
                  }
                }

                if (result == STATUS_OK)
                {
                  // both media packets are created correctly
                  // now add both packets to unprocessed media collection

                  result = (unprocessedMediaPackets->Add(firstPart)) ? STATUS_OK : STATUS_ERROR;

                  if (result == STATUS_OK)
                  {
                    result = (unprocessedMediaPackets->Add(secondPart)) ? STATUS_OK : STATUS_ERROR;

                    if (result == STATUS_ERROR)
                    {
                      // second part wasn't added to media collection
                      delete secondPart;
                    }
                  }
                  else
                  {
                    // first part wasn't added to media collection
                    delete firstPart;
                    delete secondPart;
                  }
                }
                else
                {
                  // some error occured
                  // both media packets must be destroyed

                  if (firstPart != NULL)
                  {
                    delete firstPart;
                  }
                  if (secondPart != NULL)
                  {
                    delete secondPart;
                  }
                }
              }
              else
              {
                // just delete processed media packet
                if (result == STATUS_OK)
                {
                  // delete first media packet because it is processed
                  if (!unprocessedMediaPackets->Remove(0))
                  {
                    // some error occured
                    result = STATUS_ERROR;
                  }
                }
              }
            }
          }
        }
      }
    }

    // media packets collection is not longer needed
    delete unprocessedMediaPackets;

    // in any case there is need to delete media packet
    // because media packet must be destroyed after processing

    delete mediaPacket;
  }

  return result;
}

int CAsyncSourceStream::SetTotalLength(const TCHAR *outputPinName, LONGLONG total, bool estimate)
{
  CLockMutex lock(this->mediaPacketMutex, INFINITE);

  this->totalLength = total;
  this->estimate = estimate;

  return S_OK;
}

HRESULT CAsyncSourceStream::CreateAsyncRequestProcessWorker(void)
{
  HRESULT result = S_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME);

  this->asyncRequestProcessingShouldExit = false;

  this->hAsyncRequestProcessingThread = CreateThread( 
    NULL,                                                 // default security attributes
    0,                                                    // use default stack size  
    &CAsyncSourceStream::AsyncRequestProcessWorker,       // thread function name
    this,                                                 // argument to thread function 
    0,                                                    // use default creation flags 
    &dwAsyncRequestProcessingThreadId);                   // returns the thread identifier

  if (this->hAsyncRequestProcessingThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger.Log(LOGGER_ERROR, _T("%s: %s: CreateThread() error: 0x%08X"), MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  }

  if (result == S_OK)
  {
    if (!SetThreadPriority(this->hAsyncRequestProcessingThread, THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger.Log(LOGGER_WARNING, _T("%s: %s: cannot set thread priority for receive data thread, error: %u"), MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, GetLastError());
    }
  }

  this->logger.Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::DestroyAsyncRequestProcessWorker(void)
{
  HRESULT result = S_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME);

  this->asyncRequestProcessingShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hAsyncRequestProcessingThread != NULL)
  {
    if (WaitForSingleObject(this->hAsyncRequestProcessingThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME, _T("thread didn't exit, terminating thread"));
      TerminateThread(this->hAsyncRequestProcessingThread, 0);
    }
  }

  this->hAsyncRequestProcessingThread = NULL;
  this->asyncRequestProcessingShouldExit = false;

  this->logger.Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  return result;
}

HRESULT CAsyncSourceStream::CheckValues(CAsyncRequest *request, CMediaPacket *mediaPacket, unsigned int *mediaPacketDataStart, unsigned int *mediaPacketDataLength, REFERENCE_TIME startTime)
{
  HRESULT result = S_OK;

  CHECK_POINTER_DEFAULT_HRESULT(result, request);
  CHECK_POINTER_DEFAULT_HRESULT(result, mediaPacket);
  CHECK_POINTER_DEFAULT_HRESULT(result, mediaPacketDataStart);
  CHECK_POINTER_DEFAULT_HRESULT(result, mediaPacketDataLength);

  if (result == S_OK)
  {
    LONGLONG requestStart = request->GetStart();
    LONGLONG requestEnd = request->GetStart() + request->GetBufferLength();

    result = ((startTime >= requestStart) && (startTime <= requestEnd)) ? S_OK : E_INVALIDARG;

    if (result == S_OK)
    {
      REFERENCE_TIME mediaPacketStart = 0;
      REFERENCE_TIME mediaPacketEnd = 0;
      result = mediaPacket->GetTime(&mediaPacketStart, &mediaPacketEnd);

      this->logger.Log(LOGGER_DATA, _T("%s: %s: async request start: %llu, end: %llu, start time: %llu"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, requestStart, requestEnd, startTime);
      this->logger.Log(LOGGER_DATA, _T("%s: %s: media packet start: %llu, end: %llu"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, mediaPacketStart, mediaPacketEnd);

      if (result == S_OK)
      {
        // check if start time is in media packet
        result = ((startTime >= mediaPacketStart) && (startTime <= mediaPacketEnd)) ? S_OK : E_INVALIDARG;

        if (result == S_OK)
        {
          // increase timeEnd because timeEnd is stamp of last byte in buffer
          mediaPacketEnd++;

          // check if async request and media packet are overlapping
          result = ((requestStart <= mediaPacketEnd) && (requestEnd >= mediaPacketStart)) ? S_OK : E_INVALIDARG;
        }
      }

      if (result == S_OK)
      {
        // check problematic values
        // maximum length of data in media packet can be UINT_MAX - 1
        // async request cannot start after UINT_MAX - 1 because then async request and media packet are not overlapping

        REFERENCE_TIME tempMediaPacketDataStart = ((startTime - mediaPacketStart) > 0) ? startTime : mediaPacketStart;
        if ((min(requestEnd, mediaPacketEnd) - tempMediaPacketDataStart) >= UINT_MAX)
        {
          // it's there just for sure
          // problem: length of data is bigger than possible values for copying data
          result = E_OUTOFMEMORY;
        }

        if (SUCCEEDED(result))
        {
          // all values are correct
          *mediaPacketDataStart = (unsigned int)(tempMediaPacketDataStart - mediaPacketStart);
          *mediaPacketDataLength = (unsigned int)(min(requestEnd, mediaPacketEnd) - tempMediaPacketDataStart);
        }
      }
    }
  }

  return result;
}

DWORD WINAPI CAsyncSourceStream::AsyncRequestProcessWorker(LPVOID lpParam)
{
  CAsyncSourceStream *caller = (CAsyncSourceStream *)lpParam;
  caller->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME);

  unsigned int maxCacheTime = caller->configuration->GetValueLong(CONFIGURATION_MAX_CACHE_TIME, true, MAX_CACHE_TIME_DEFAULT);
  DWORD lastCheckTime = GetTickCount();

  while (!caller->asyncRequestProcessingShouldExit)
  {
    {
      // lock access to requests
      CLockMutex requestLock(caller->requestMutex, INFINITE);

      unsigned int requestCount = caller->requestsCollection->Count();
      for (unsigned int i = 0; i < requestCount; i++)
      {
        CAsyncRequest *request = caller->requestsCollection->GetItem(i);

        if ((request->GetState() == CAsyncRequest::Waiting) || (request->GetState() == CAsyncRequest::Requested))
        {
          // process only waiting requests
          // variable to store found data length
          unsigned int foundDataLength = 0;
          HRESULT result = S_OK;

          // first try to find starting media packet (packet which have first data)
          unsigned int packetIndex = UINT_MAX;
          {
            // lock access to media packets
            CLockMutex mediaPacketLock(caller->mediaPacketMutex, INFINITE);

            REFERENCE_TIME startTime = request->GetStart();
            packetIndex = caller->mediaPacketCollection->GetMediaPacketIndexBetweenTimes(startTime);            
            if (packetIndex != UINT_MAX)
            {
              while (packetIndex != UINT_MAX)
              {
                unsigned int mediaPacketDataStart = 0;
                unsigned int mediaPacketDataLength = 0;

                // get media packet
                CMediaPacket *mediaPacket = caller->mediaPacketCollection->GetItem(packetIndex);
                // check packet values against async request values
                result = caller->CheckValues(request, mediaPacket, &mediaPacketDataStart, &mediaPacketDataLength, startTime);

                if (result == S_OK)
                {
                  // successfully checked values
                  REFERENCE_TIME timeStart = 0;
                  REFERENCE_TIME timeEnd = 0;
                  mediaPacket->GetTime(&timeStart, &timeEnd);

                  // copy data from media packet to request buffer
                  caller->logger.Log(LOGGER_DATA, _T("%s: %s: copy data from media packet '%u' to async request '%u', start: %u, data length: %u, request buffer position: %u, request buffer length: %lu"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, packetIndex, request->GetRequestId(), mediaPacketDataStart, mediaPacketDataLength, foundDataLength, request->GetBufferLength());
                  char *requestBuffer = (char *)request->GetBuffer() + foundDataLength;
                  if (mediaPacket->IsStoredToFile())
                  {
                    // if media packet is stored to file
                    // than is need to read 'mediaPacketDataLength' bytes
                    // from 'mediaPacket->GetStoreFilePosition()' + 'mediaPacketDataStart' position of file

                    LARGE_INTEGER size;
                    size.QuadPart = 0;

                    // open or create file
                    HANDLE hTempFile = CreateFile(caller->storeFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

                    if (hTempFile != INVALID_HANDLE_VALUE)
                    {
                      bool error = false;

                      LONG distanceToMoveLow = (LONG)(mediaPacket->GetStoreFilePosition() + mediaPacketDataStart);
                      LONG distanceToMoveHigh = (LONG)((mediaPacket->GetStoreFilePosition() + mediaPacketDataStart) >> 32);
                      LONG distanceToMoveHighResult = distanceToMoveHigh;
                      DWORD result = SetFilePointer(hTempFile, distanceToMoveLow, &distanceToMoveHighResult, FILE_BEGIN);
                      if (result == INVALID_SET_FILE_POINTER)
                      {
                        DWORD lastError = GetLastError();
                        if (lastError != NO_ERROR)
                        {
                          caller->logger.Log(LOGGER_ERROR, _T("%s: %s: error occured while setting position: %lu"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, lastError);
                          error = true;
                        }
                      }

                      if (!error)
                      {
                        DWORD read = 0;
                        if (ReadFile(hTempFile, requestBuffer, mediaPacketDataLength, &read, NULL) == 0)
                        {
                          caller->logger.Log(LOGGER_ERROR, _T("%s: %s: error occured reading file: %lu"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, GetLastError());
                        }
                        else if (read != mediaPacketDataLength)
                        {
                          caller->logger.Log(LOGGER_WARNING, _T("%s: %s: readed data length not same as requested, requested: %u, readed: %u"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, mediaPacketDataLength, read);
                        }
                      }

                      CloseHandle(hTempFile);
                      hTempFile = INVALID_HANDLE_VALUE;
                    }
                  }
                  else
                  {
                    // media packet is stored in memory
                    mediaPacket->GetBuffer()->CopyFromBuffer(requestBuffer, mediaPacketDataLength, 0, mediaPacketDataStart);
                  }

                  // update length of data
                  foundDataLength += mediaPacketDataLength;

                  if (foundDataLength < (unsigned int)request->GetBufferLength())
                  {
                    // find another media packet after end of this media packet
                    startTime = timeEnd + 1;
                    packetIndex = caller->mediaPacketCollection->GetMediaPacketIndexBetweenTimes(startTime);
                    caller->logger.Log(LOGGER_DATA, _T("%s: %s: next media packet '%u'"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, packetIndex);
                  }
                  else
                  {
                    // do not find any more media packets for this request because we have enough data
                    break;
                  }
                }
                else
                {
                  // some error occured
                  // do not find any more media packets for this request because request failed
                  break;
                }
              }

              if (SUCCEEDED(result))
              {
                if (foundDataLength < (unsigned int)request->GetBufferLength())
                {
                  // found data length is lower than requested, return S_FALSE

                  if ((!caller->estimate) && (caller->totalLength > (request->GetStart() + request->GetBufferLength())))
                  {
                    // we are receiving data, wait for all requested data
                  }
                  else
                  {
                    // we are not receiving more data
                    // finish request
                    caller->logger.Log(LOGGER_DATA, _T("%s: %s: request '%u' complete status: 0x%08X"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), S_FALSE);
                    request->SetBufferLength(foundDataLength);
                    // filters doesn't understand S_FALSE return code, so return S_OK
                    request->Complete(S_OK);
                  }
                }
                else if (foundDataLength == request->GetBufferLength())
                {
                  // found data length is equal than requested, return S_OK
                  caller->logger.Log(LOGGER_DATA, _T("%s: %s: request '%u' complete status: 0x%08X"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), S_OK);
                  request->SetBufferLength(foundDataLength);
                  request->Complete(S_OK);
                }
                else
                {
                  caller->logger.Log(LOGGER_ERROR, _T("%s: %s: request '%u' found data length '%u' bigger than requested '%lu'"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), foundDataLength, request->GetBufferLength());
                  request->Complete(E_OUTOFMEMORY);
                }
              }
              else
              {
                // some error occured
                // complete async request with error
                // set request is completed with result
                caller->logger.Log(LOGGER_WARNING, _T("%s: %s: request '%u' complete status: 0x%08X"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), result);
                request->SetBufferLength(foundDataLength);
                request->Complete(result);
              }
            }
          }

          if ((packetIndex == UINT_MAX) && (request->GetState() == CAsyncRequest::Waiting))
          {
            bool rangesSupported = false;
            // check if ranges are supported
            if (caller->filter->QueryRangesSupported(&rangesSupported) == S_OK)
            {
              if (rangesSupported)
              {
                if (SUCCEEDED(result))
                {
                  // not found start packet and request wasn't requested from filter yet
                  // request filter to receive data from request start
                  result = caller->filter->ReceiveDataFromTimestamp(request->GetStart(), request->GetStart());
                }

                if (SUCCEEDED(result))
                {
                  request->Request();
                }
                else
                {
                  // if error occured while requesting filter for data
                  caller->logger.Log(LOGGER_WARNING, _T("%s: %s: request '%u' error while requesting data, complete status: 0x%08X"), MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), result);
                  request->Complete(result);
                }
              }
            }
          }
        }
      }
    }

    {
      if ((GetTickCount() - lastCheckTime) > 1000)
      {
        lastCheckTime = GetTickCount();

        // lock access to media packets
        CLockMutex mediaPacketLock(caller->mediaPacketMutex, INFINITE);

        if (caller->mediaPacketCollection->Count() > 0)
        {
          // store all media packets (which are not stored) to file
          if (caller->storeFilePath == NULL)
          {
            TCHAR *guid = ConvertGuidToString(caller->GetInstanceId());
            ALLOC_MEM_DEFINE_SET(folder, TCHAR, MAX_PATH, 0);
            if ((guid != NULL) && (folder != NULL))
            {
              // get common application data folder
              if (SHGetSpecialFolderPath(NULL, folder, CSIDL_LOCAL_APPDATA, FALSE))
              {
                TCHAR *storeFolder = FormatString(_T("%s\\MPUrlSource\\"), folder);
                wchar_t *unicodeStoreFolder = ConvertToUnicode(storeFolder);
                if ((storeFolder != NULL) && (unicodeStoreFolder != NULL))
                {
                  int error = SHCreateDirectory(NULL, unicodeStoreFolder);
                  if ((error == ERROR_SUCCESS) || (error == ERROR_FILE_EXISTS) || (error == ERROR_ALREADY_EXISTS))
                  {
                    // correct, directory exists
                    caller->storeFilePath = FormatString(_T("%smpurlsource_%s.temp"), storeFolder, guid);
                  }
                }
                FREE_MEM(storeFolder);
                FREE_MEM(unicodeStoreFolder);
              }
            }
            FREE_MEM(guid);
            FREE_MEM(folder);
          }

          if (caller->storeFilePath != NULL)
          {
            LARGE_INTEGER size;
            size.QuadPart = 0;

            // open or create file
            HANDLE hTempFile = CreateFile(caller->storeFilePath, FILE_APPEND_DATA, FILE_SHARE_READ, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

            if (hTempFile != INVALID_HANDLE_VALUE)
            {
              if (!GetFileSizeEx(hTempFile, &size))
              {
                caller->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, _T("error while getting size"));
                // error occured while getting file size
                size.QuadPart = -1;
              }

              if (size.QuadPart >= 0)
              {
                unsigned int i = 0;
                while (i < caller->mediaPacketCollection->Count())
                {
                  bool error = false;
                  CMediaPacket *mediaPacket = caller->mediaPacketCollection->GetItem(i);

                  if (!mediaPacket->IsStoredToFile())
                  {
                    // if media packet is not stored to file
                    // store it to file
                    REFERENCE_TIME mediaPacketStartTime = 0;
                    REFERENCE_TIME mediaPacketEndTime = 0;
                    if (mediaPacket->GetTime(&mediaPacketStartTime, &mediaPacketEndTime) == S_OK)
                    {
                      unsigned int length = (unsigned int)(mediaPacketEndTime + 1 - mediaPacketStartTime);

                      ALLOC_MEM_DEFINE_SET(buffer, char, length, 0);
                      if (mediaPacket->GetBuffer()->CopyFromBuffer(buffer, length, 0, 0) == length)
                      {
                        DWORD written = 0;
                        if (WriteFile(hTempFile, buffer, length, &written, NULL))
                        {
                          if (length == written)
                          {
                            // mark as stored
                            mediaPacket->SetStoredToFile(size.QuadPart);
                            size.QuadPart += length;
                          }
                        }
                        else
                        {
                          caller->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, _T("not written"));
                        }
                      }
                      FREE_MEM(buffer);
                    }
                  }

                  i++;
                }
              }

              CloseHandle(hTempFile);
              hTempFile = INVALID_HANDLE_VALUE;
            }
            else
            {
              caller->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, _T("invalid file handle"));
            }
          }
        }
      }
    }

    Sleep(1);
  }

  caller->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME);
  return S_OK;
}

int CAsyncSourceStream::EndOfStreamReached(const TCHAR *outputPinName)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME);
  CLockMutex mediaPacketLock(this->mediaPacketMutex, INFINITE);

  if (this->mediaPacketCollection->Count() > 0)
  {
    this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: media packet count: %u"), MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, this->mediaPacketCollection->Count());
    REFERENCE_TIME startTime = 0;
    REFERENCE_TIME endTime = 0;
    unsigned int mediaPacketIndex = this->mediaPacketCollection->GetMediaPacketIndexBetweenTimes(startTime);

    // because collection is sorted
    // then simle going through all media packets will reveal if there is some empty place
    while (mediaPacketIndex != UINT_MAX)
    {
      CMediaPacket *mediaPacket = this->mediaPacketCollection->GetItem(mediaPacketIndex);
      REFERENCE_TIME mediaPacketStart = 0;
      REFERENCE_TIME mediaPacketEnd = 0;
      if (mediaPacket->GetTime(&mediaPacketStart, &mediaPacketEnd) == S_OK)
      {
        if (startTime == mediaPacketStart)
        {
          // next start time is next to end of current media packet
          startTime = mediaPacketEnd + 1;
          mediaPacketIndex++;

          if (mediaPacketIndex >= this->mediaPacketCollection->Count())
          {
            // stop checking, all media packets checked
            mediaPacketIndex = UINT_MAX;
          }
        }
        else
        {
          endTime = mediaPacketStart - 1;
          mediaPacketIndex = UINT_MAX;
        }
      }
      else
      {
        mediaPacketIndex = UINT_MAX;
      }
    }

    if (startTime < this->totalLength)
    {
      // found part which is not downloaded
      this->logger.Log(LOGGER_VERBOSE, _T("%s: %s: requesting stream part from: %llu, to: %llu"), MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, startTime, endTime);
      this->filter->ReceiveDataFromTimestamp(startTime, endTime);
    }
  }

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME);
  return STATUS_OK;
}