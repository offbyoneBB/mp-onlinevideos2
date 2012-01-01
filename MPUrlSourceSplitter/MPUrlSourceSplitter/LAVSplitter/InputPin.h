/*
 *      Copyright (C) 2011 Hendrik Leppkes
 *      http://www.1f0.de
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

#pragma once

#include "ProtocolInterface.h"
#include "Logger.h"
#include "Download.h"

#include "AsyncRequest.h"
#include "AsyncRequestCollection.h"
#include "MediaPacketCollection.h"
#include "RangesSupported.h"
#include "StreamAvailableLength.h"

class CLAVSplitter;
class CAsyncRequest;
class CAsyncRequestCollection;
class CMediaPacketCollection;

struct ProtocolImplementation
{
  wchar_t *protocol;
  HINSTANCE hLibrary;
  PIProtocol pImplementation;
  bool supported;
  DESTROYPROTOCOLINSTANCE destroyProtocolInstance;
};

class CLAVInputPin 
  : public CUnknown
  , public CCritSec
  , public IFileSourceFilter
  , public IOutputStream
  , public IDownload
  , public IDownloadCallback
{
public:
  CLAVInputPin(TCHAR* pName, CLAVSplitter *pFilter, CCritSec* pLock, HRESULT* phr);
  ~CLAVInputPin(void);

  HRESULT GetAVIOContext(AVIOContext** ppContext);

  // IUnknown
  DECLARE_IUNKNOWN
  STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void** ppv);

  // CBasePin
  /*HRESULT CheckMediaType(const CMediaType* pmt);
  HRESULT CheckConnect(IPin* pPin);
  HRESULT BreakConnect();
  HRESULT CompleteConnect(IPin* pPin);*/

  STDMETHODIMP BeginFlush();
  STDMETHODIMP EndFlush();

  // IFileSourceFilter
  STDMETHODIMP Load(LPCOLESTR pszFileName, const AM_MEDIA_TYPE * pmt);
  STDMETHODIMP GetCurFile(LPOLESTR *ppszFileName, AM_MEDIA_TYPE *pmt);

  // IAMOpenProgress interface
  STDMETHODIMP QueryProgress(LONGLONG *pllTotal, LONGLONG *pllCurrent);
  STDMETHODIMP AbortOperation(void);

  // IDownload interface
  STDMETHODIMP Download(LPCOLESTR uri, LPCOLESTR fileName);
  STDMETHODIMP DownloadAsync(LPCOLESTR uri, LPCOLESTR fileName, IDownloadCallback *downloadCallback);

  // IDownloadCallback interface
  void STDMETHODCALLTYPE OnDownloadCallback(HRESULT downloadResult);

  // IOutputStream interface

  // sets total length of stream to output pin
  // @param total : total length of stream in bytes
  // @param estimate : specifies if length is estimate
  // @return : S_OK if successful
  HRESULT SetTotalLength(LONGLONG total, bool estimate);

  // pushes media packet to output pin
  // @param mediaPacket : reference to media packet to push to output pin
  // @return : S_OK if successful
  HRESULT PushMediaPacket(CMediaPacket *mediaPacket);

  // notifies output stream that end of stream was reached
  // @param streamPosition : the last valid stream position
  // @return : S_OK if successful
  HRESULT EndOfStreamReached(LONGLONG streamPosition);

  // request protocol implementation to cancel the stream reading operation
  // @return : S_OK if successful
  HRESULT AbortStreamReceive(void);

  // retrieves the progress of the stream reading operation
  // @param total : reference to a variable that receives the length of the entire stream, in bytes
  // @param current : reference to a variable that receives the length of the downloaded portion of the stream, in bytes
  // @return : S_OK if successful, VFW_S_ESTIMATED if returned values are estimates, E_UNEXPECTED if unexpected error
  HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current);

protected:
  static int Read(void *opaque, uint8_t *buf, int buf_size);
  static int64_t Seek(void *opaque, int64_t offset, int whence);

  LONGLONG m_llPos;

private:
  AVIOContext *m_pAVIOContext;

  wchar_t* url;
  wchar_t* downloadFileName;

  bool asyncDownloadFinished;
  HRESULT asyncDownloadResult;
  IDownloadCallback *asyncDownloadCallback;

  // configuration provided by filter
  CParameterCollection *configuration;
  // logger for logging purposes
  CLogger *logger;
  // the parent of this pin
  CLAVSplitter *filter;
  // the collection of asynchronous requests
  CAsyncRequestCollection *requestsCollection;
  // collection of media packets
  CMediaPacketCollection *mediaPacketCollection;
  // mutex for accessing requests
  HANDLE requestMutex;
  // mutex for accessing media packets
  HANDLE mediaPacketMutex;
  // request ID for async requests
  unsigned int requestId;

  LONGLONG totalLength;
  bool estimate;

  // file path for storing received data to file
  wchar_t *storeFilePath;
  // specifies if we are downloading file
  // in that case we don't delete file on end
  bool downloadingFile;
  // specifies if download finished (all data from stream has been received - it doesn't mean that has been stored to file)
  bool downloadFinished;
  // specifies if download callback has been called
  bool downloadCallbackCalled;

  // specifies if pin is successfully connected to another pin
  bool connectedToAnotherPin;

  // handle to MPUrlSourceSplitter.ax
  HMODULE mainModuleHandle;

  // status of processing
  int status;
  HANDLE hReceiveDataWorkerThread;
  DWORD dwReceiveDataWorkerThreadId;
  bool receiveDataWorkerShouldExit;
  static DWORD WINAPI ReceiveDataWorker(LPVOID lpParam);

  // creates receive data worker
  // @return : S_OK if successful
  HRESULT CreateReceiveDataWorker(void);

  // destroys receive data worker
  // @return : S_OK if successful
  HRESULT DestroyReceiveDataWorker(void);

  // array of available protocol implementations
  ProtocolImplementation *protocolImplementations;
  unsigned int protocolImplementationsCount;

  // loads plugins from directory
  void LoadPlugins(void);

  // stores active protocol
  PIProtocol activeProtocol;  

  // initializes input pin and loads protocol implementation based on configuration
  // @result : S_OK if successful
  STDMETHODIMP Load();

  // loads protocol implementation based on specified url and configuration parameters
  // @param url : the url to load
  // @param parameters : the parameters used for connection
  // @return : true if url is loaded, false otherwise
  bool LoadProtocolImplementation(const wchar_t *url, const CParameterCollection *parameters);

  // parses parameters from specified string
  // @param parameters : null-terminated string with specified parameters
  // @return : reference to variable holding collection of parameters or NULL if error
  CParameterCollection *ParseParameters(const wchar_t *parameters);

  HRESULT Request(unsigned int *requestId, LONGLONG position, LONG length, IMediaSample *sample, BYTE *buffer, bool aligned, DWORD_PTR userData);
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

  // retrieves available lenght of stream
  // @param available : reference to instance of class that receives the available length of stream, in bytes
  // @return : S_OK if successful, other error codes if error
  HRESULT QueryStreamAvailableLength(CStreamAvailableLength *availableLength);

  // queries protocol implementation if ranges are supported
  // @param rangesSupported : reference to instance of class that receives if ranges are supported
  // @return : S_OK if successful
  HRESULT QueryRangesSupported(CRangesSupported *rangesSupported);

  // get timeout (in ms) for receiving data
  // @return : timeout (in ms) for receiving data
  unsigned int GetReceiveDataTimeout(void);

  // request protocol implementation to receive data from specified time
  // @param startTime : the requested start time (zero is start of stream)
  // @param endTime : the requested end time, if endTime is lower or equal to startTime than endTime is not specified
  // @return : S_OK if successful, error code otherwise
  HRESULT ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime);
};
