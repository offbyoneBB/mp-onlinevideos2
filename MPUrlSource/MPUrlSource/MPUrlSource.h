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

#ifndef __MPURLSOURCE_DEFINED
#define __MPURLSOURCE_DEFINED

#include "ProtocolInterface.h"
#include "AsyncSource.h"
#include "AsyncSourceStream.h"
#include "AsyncSourceStreamCollection.h"

#include <initguid.h>
#include <cguid.h>

#define MODULE_FILE_NAME                                            _T("MPUrlSource.ax")

// {87DD67C7-5D13-4CD5-819B-586FFCE8650F}
DEFINE_GUID(CLSID_MPUrlSourceFilter, 
  0x87DD67C7, 0x5D13, 0x4CD5, 0x81, 0x9B, 0x58, 0x6F, 0xFC, 0xE8, 0x65, 0x0F);

#define STATUS_NONE                                                 0
#define STATUS_NO_DATA_ERROR                                        -1
#define STATUS_RECEIVING_DATA                                       1

struct ProtocolImplementation
{
  TCHAR *protocol;
  HINSTANCE hLibrary;
  PIProtocol pImplementation;
  bool supported;
  DESTROYPROTOCOLINSTANCE destroyProtocolInstance;
};

// This class is exported from the MPUrlSource.ax
class CMPUrlSourceFilter : public CAsyncSource, public IFileSourceFilter, public IOutputStream, public IAMOpenProgress
{
private:
  // Constructor is private because you have to use CreateInstance
  CMPUrlSourceFilter(IUnknown *pUnk, HRESULT *phr);
  ~CMPUrlSourceFilter();

  TCHAR* m_url;
  CParameterCollection *parameters;
  CParameterCollection *configuration;
  CLogger logger;

  // handle to MPUrlSource.ax
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

  // collection of async sources (output pins)
  CAsyncSourceStreamCollection *sourceStreamCollection;

public:
  // loads specified url
  // @param url : the url to load
  // @param parameters : the parameters used for connection
  // @return : true if url is loaded, false otherwise
  bool Load(const TCHAR *url, const CParameterCollection *parameters);

  // IFileSourceFilter
  DECLARE_IUNKNOWN
  STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void** ppv);
  STDMETHODIMP Load(LPCOLESTR pszFileName, const AM_MEDIA_TYPE* pmt);
  STDMETHODIMP GetCurFile(LPOLESTR* ppszFileName, AM_MEDIA_TYPE* pmt);
  static CUnknown * WINAPI CreateInstance(IUnknown *pUnk, HRESULT *phr);

  // IAMOpenProgress interface
  STDMETHODIMP QueryProgress(LONGLONG *pllTotal, LONGLONG *pllCurrent);
  STDMETHODIMP AbortOperation(void);

  // CBaseFilter GetState() method
  STDMETHODIMP GetState(DWORD dwMSecs, FILTER_STATE *State);

  // IOutputStream interface

  // sets total length of stream to output pin
  // caller is responsible for deleting output pin name
  // @param outputPinName : the name of output pin (the output pin name must be value from values returned from GetStreamNames() method of IProtocol interface
  // @param total : total length of stream in bytes
  // @param estimate : specifies if length is estimate
  // @return : STATUS_OK if successful
  int SetTotalLength(const TCHAR *outputPinName, LONGLONG total, bool estimate);

  // pushes media packet to output pin
  // @param outputPinName : the name of output pin (the output pin name must be value from values returned from GetStreamNames() method of IProtocol interface
  // @param mediaPacket : reference to media packet to push to output pin
  // @return : STATUS_OK if successful
  int PushMediaPacket(const TCHAR *outputPinName, CMediaPacket *mediaPacket);

  // IBaseProtocol interface

  // return reference to null-terminated string which represents protocol name
  // function have to allocate enough memory for protocol name string
  // errors should be logged to log file and returned NULL
  // @return : reference to null-terminated string
  TCHAR *GetProtocolName(void);

  // test if connection is opened
  // @return : true if connected, false otherwise
  bool IsConnected(void);

  // get timeout (in ms) for receiving data
  // @return : timeout (in ms) for receiving data
  unsigned int GetReceiveDataTimeout(void);

  // get protocol instance ID
  // @return : GUID, which represents instance identifier or GUID_NULL if error
  GUID GetInstanceId(void);

  // get protocol maximum open connection attempts
  // @return : maximum attempts of opening connections or UINT_MAX if error
  unsigned int GetOpenConnectionMaximumAttempts(void);

  // gets stream name provided by protocol
  // this method is called after successfully opened connection
  // caller is responsible for deleting collection after use
  // @return : reference to CStringCollection or NULL if error
  CStringCollection *GetStreamNames(void);

  // request protocol implementation to receive data from specified time
  // @param time : the requested start time (zero is start of stream)
  // @return : S_OK if successful, error code otherwise
  HRESULT ReceiveDataFromTimestamp(REFERENCE_TIME time);

  // request protocol implementation to cancel the stream reading operation
  // @return : S_OK if successful
  HRESULT AbortStreamReceive();

  // retrieves the progress of the stream reading operation
  // @param total : reference to a variable that receives the length of the entire stream, in bytes
  // @param current : reference to a variable that receives the length of the downloaded portion of the stream, in bytes
  // @return : S_OK if successful, VFW_S_ESTIMATED if returned values are estimates, E_UNEXPECTED if unexpected error
  HRESULT QueryStreamProgress(LONGLONG *total, LONGLONG *current);
};


#endif