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

// MPUrlSource.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "MPUrlSource.h"
#include "Utilities.h"

#include <Shlwapi.h>

class CAsyncSourceStream;

#define MODULE_NAME                                               _T("MPUrlSource")

#define METHOD_SET_CONNECT_INFO_NAME                              _T("SetConnectInfo()")
#define METHOD_LOAD_PLUGINS_NAME                                  _T("LoadPlugins()")
#define METHOD_LOAD_NAME                                          _T("Load()")
#define METHOD_RECEIVE_DATA_WORKER_NAME                           _T("ReceiveDataWorker()")
#define METHOD_CREATE_RECEIVE_DATA_WORKER_NAME                    _T("CreateReceiveDataWorker()")
#define METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME                   _T("DestroyReceiveDataWorker()")
#define METHOD_PUSH_DATA_NAME                                     _T("PushData()")
#define METHOD_SET_TOTAL_LENGTH_NAME                              _T("SetTotalLength()")

CMPUrlSourceFilter::CMPUrlSourceFilter(IUnknown *pUnk, HRESULT *phr)
  : CAsyncSource(NAME(_T("MediaPortal Url Source Filter")), pUnk, CLSID_MPUrlSourceFilter)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
  
  this->receiveDataWorkerShouldExit = false;
  this->parameters = new CParameterCollection();
  this->configuration = GetConfiguration(CONFIGURATION_SECTION_MPURLSOURCEFILTER);
  this->activeProtocol = NULL;
  this->protocolImplementationsCount = 0;
  this->protocolImplementations = NULL;
  this->m_url = NULL;
  this->hReceiveDataWorkerThread = NULL;
  this->status = STATUS_NONE;
  this->sourceStreamCollection = new CAsyncSourceStreamCollection();
  this->mainModuleHandle = GetModuleHandle(MODULE_FILE_NAME);

  if (this->mainModuleHandle == NULL)
  {
    this->logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, _T("main module handle not found"));
  }

  // load plugins from directory
  this->LoadPlugins();

  if (phr)
  {
    *phr = S_OK;
  }

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceFilter::~CMPUrlSourceFilter()
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);

  this->DestroyReceiveDataWorker();

  // close active protocol connection
  if (this->activeProtocol != NULL)
  {
    if (this->activeProtocol->IsConnected())
    {
      this->activeProtocol->CloseConnection();
    }

    this->activeProtocol = NULL;
  }

  this->receiveDataWorkerShouldExit = false;

  delete this->sourceStreamCollection;

  // release all protocol implementations
  if (this->protocolImplementations != NULL)
  {
    for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
    {
      this->logger.Log(LOGGER_INFO, _T("%s: %s: destroying protocol: %s"), MODULE_NAME, METHOD_DESTRUCTOR_NAME, protocolImplementations[i].protocol);

      if (protocolImplementations[i].pImplementation != NULL)
      {
        protocolImplementations[i].destroyProtocolInstance(protocolImplementations[i].pImplementation);
        protocolImplementations[i].pImplementation = NULL;
        protocolImplementations[i].destroyProtocolInstance = NULL;
      }
      if (protocolImplementations[i].protocol != NULL)
      {
        CoTaskMemFree(protocolImplementations[i].protocol);
        protocolImplementations[i].protocol = NULL;
      }
      if (protocolImplementations[i].hLibrary != NULL)
      {
        FreeLibrary(protocolImplementations[i].hLibrary);
        protocolImplementations[i].hLibrary = NULL;
      }
    }
    this->protocolImplementationsCount = 0;
  }
  FREE_MEM(this->protocolImplementations);

  delete this->parameters;
  delete this->configuration;
  FREE_MEM(this->m_url);

  this->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);
}

STDMETHODIMP CMPUrlSourceFilter::NonDelegatingQueryInterface(REFIID riid, void** ppv)
{
  CheckPointer(ppv, E_POINTER);

  if (riid == _uuidof(IFileSourceFilter))
  {
    return GetInterface((IFileSourceFilter *)this, ppv);
  }
  else if (riid == _uuidof(IAMOpenProgress))
  {
    return GetInterface((IAMOpenProgress *)this, ppv);
  }
  /*else if (riid == _uuidof(IMPIPTVConnectInfo))
  {
    return GetInterface((IMPIPTVConnectInfo *)this, ppv);
  }*/
  else
  {
    return __super::NonDelegatingQueryInterface(riid, ppv);
  }
}

STDMETHODIMP CMPUrlSourceFilter::Load(LPCOLESTR pszFileName, const AM_MEDIA_TYPE* pmt) 
{
  HRESULT result = S_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_LOAD_NAME);

#ifdef _MBCS
  this->m_url = ConvertToMultiByteW(pszFileName);
#else
  this->m_url = ConvertToUnicodeW(pszFileName);
#endif

  //this->m_url = Duplicate(_T("http://vid1.markiza.sk/a510/video/part/file/2/0032/2011_10_06_00_00_0_adela_show_1_1.mp4"));

  //this->m_url = Duplicate(_T("http://localhost/test.flv"));

  //this->m_url = Duplicate(_T("http://www.sme.sk/pokus.html"));

  if (this->m_url == NULL)
  {
    result = E_FAIL;
  }

  //// temporary section for specifying network interface
  //if (this->SetConnectInfo(pszFileName))
  //{
  //  return E_FAIL;
  //}

  //if (this->m_parameters != NULL)
  //{
  //  // we have set some parameters
  //  // get url parameter
  //  PCParameter urlParameter = this->m_parameters->GetParameter(URL_PARAMETER_NAME, true);
  //  if (urlParameter != NULL)
  //  {
  //    // free current url
  //    FREE_MEM(this->m_url);
  //    // make duplicate of parameter url
  //    this->m_url = Duplicate(urlParameter->GetValue());
  //  }
  //}

  if (result == S_OK)
  {
    if(!this->Load(this->m_url, this->parameters))
    {
      result = E_FAIL;
    }
  }

  if (result == S_OK)
  {
    // now we have active protocol with loaded url, but still not working
    // we need to know how many output pins have to be created

    CStringCollection *streamNames = this->activeProtocol->GetStreamNames();

    // create stream implementations
    HRESULT asyncStreamResult = S_OK;
    for (unsigned int i = 0; i < streamNames->Count(); i++)
    {
      asyncStreamResult = S_OK;
#ifdef _MBCS
      wchar_t *streamName = ConvertToUnicodeA(streamNames->GetItem(i));
#else
      wchar_t *streamName = ConvertToUnicodeW(streamNames->GetItem(i));
#endif
      CAsyncSourceStream *asyncStream = new CAsyncSourceStream(NAME(_T("Asynchronous Source Filter Output Pin")), &asyncStreamResult, this, streamName, this->configuration);

      if (asyncStreamResult != S_OK)
      {
        result = asyncStreamResult;
        break;
      }

      this->sourceStreamCollection->Add(asyncStream);
    }

    // delete returned stream names
    delete streamNames;
  }

  if (result == S_OK)
  {
    // now we have active protocol with loaded url, but still not working
    // create thread for receiving data

    result = this->CreateReceiveDataWorker();
  }

  if (result == S_OK)
  {
    DWORD ticks = GetTickCount();
    DWORD timeout = 0;

    if (this->activeProtocol != NULL)
    {
      // get receive data timeout for active protocol
      timeout = this->activeProtocol->GetReceiveDataTimeout();
      TCHAR *protocolName = this->activeProtocol->GetProtocolName();
      this->logger.Log(LOGGER_INFO, _T("%s: %s: active protocol '%s' timeout: %d (ms)"), MODULE_NAME, METHOD_LOAD_NAME, protocolName, timeout);
      FREE_MEM(protocolName);
    }
    else
    {
      this->logger.Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_NAME, _T("no active protocol"));
      result = E_FAIL;
    }

    if (result == S_OK)
    {
      // wait for receiving data, timeout or exit
      while ((this->status != STATUS_RECEIVING_DATA) && (this->status != STATUS_NO_DATA_ERROR) && ((GetTickCount() - ticks) <= timeout) && (!this->receiveDataWorkerShouldExit))
      {
        Sleep(1);
      }

      switch(this->status)
      {
      case STATUS_NONE:
        result = E_FAIL;
        break;
      case STATUS_NO_DATA_ERROR:
        result = E_FAIL;
        break;
      case STATUS_RECEIVING_DATA:
        result = S_OK;
        break;
      default:
        result = E_FAIL;
        break;
      }

      if (result != S_OK)
      {
        this->DestroyReceiveDataWorker();        
      }
    }
  }

  this->logger.Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_LOAD_NAME, result);

  return result;
}

HRESULT CMPUrlSourceFilter::CreateReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME);

  this->hReceiveDataWorkerThread = CreateThread( 
    NULL,                                   // default security attributes
    0,                                      // use default stack size  
    &CMPUrlSourceFilter::ReceiveDataWorker, // thread function name
    this,                                   // argument to thread function 
    0,                                      // use default creation flags 
    &dwReceiveDataWorkerThreadId);          // returns the thread identifier

  if (this->hReceiveDataWorkerThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger.Log(LOGGER_ERROR, _T("%s: %s: CreateThread() error: 0x%08X"), MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  }

  if (result == S_OK)
  {
    if (!SetThreadPriority(::GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger.Log(LOGGER_WARNING, _T("%s: %s: cannot set thread priority for main thread, error: %u"), MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
    if (!SetThreadPriority(this->hReceiveDataWorkerThread, THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger.Log(LOGGER_WARNING, _T("%s: %s: cannot set thread priority for receive data thread, error: %u"), MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
  }

  this->logger.Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

HRESULT CMPUrlSourceFilter::DestroyReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME);

  this->receiveDataWorkerShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hReceiveDataWorkerThread != NULL)
  {
    if (WaitForSingleObject(this->hReceiveDataWorkerThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger.Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, _T("thread didn't exit, terminating thread"));
      TerminateThread(this->hReceiveDataWorkerThread, 0);
    }
  }

  this->hReceiveDataWorkerThread = NULL;

  this->logger.Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

STDMETHODIMP CMPUrlSourceFilter::GetCurFile(LPOLESTR* ppszFileName, AM_MEDIA_TYPE* pmt)
{
  if (!ppszFileName)
  {
    return E_POINTER;
  }

  *ppszFileName = ConvertToUnicode(this->m_url);
  if ((*ppszFileName) == NULL)
  {
    return E_FAIL;
  }

  return S_OK;
}

CUnknown * WINAPI CMPUrlSourceFilter::CreateInstance(IUnknown *pUnk, HRESULT *phr)
{
  CMPUrlSourceFilter *pNewFilter = new CMPUrlSourceFilter(pUnk, phr);

  if (phr)
  {
    if (pNewFilter == NULL) 
    {
      *phr = E_OUTOFMEMORY;
    }
    else
    {
      *phr = S_OK;
    }
  }

  return pNewFilter;
}

STDMETHODIMP CMPUrlSourceFilter::QueryProgress(LONGLONG *pllTotal, LONGLONG *pllCurrent)
{
  return this->QueryStreamProgress(pllTotal, pllCurrent);
}

STDMETHODIMP CMPUrlSourceFilter::AbortOperation(void)
{
  return this->AbortStreamReceive();
}

STDMETHODIMP CMPUrlSourceFilter::GetState(DWORD dwMSecs, FILTER_STATE *State)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, State);

  if (result == S_OK)
  {
    *State = this->m_State;

    // return always S_OK, because we can pull data even if in pause state
    result = S_OK;
  }

  return result;
}

void CMPUrlSourceFilter::LoadPlugins()
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME);

  unsigned int maxPlugins = this->configuration->GetValueLong(CONFIGURATION_MAX_PLUGINS, true, MAX_PLUGINS_DEFAULT);

  if (maxPlugins > 0)
  {
    this->protocolImplementations = ALLOC_MEM(ProtocolImplementation, maxPlugins);
    if (this->protocolImplementations != NULL)
    {
      WIN32_FIND_DATA info;
      HANDLE h;

      ALLOC_MEM_DEFINE_SET(strDllPath, TCHAR, _MAX_PATH, 0);
      ALLOC_MEM_DEFINE_SET(strDllSearch, TCHAR, _MAX_PATH, 0);

      GetModuleFileName(this->mainModuleHandle, strDllPath, _MAX_PATH);
      PathRemoveFileSpec(strDllPath);

      _tcscat_s(strDllPath, _MAX_PATH, _T("\\"));
      _tcscpy_s(strDllSearch, _MAX_PATH, strDllPath);
      _tcscat_s(strDllSearch, _MAX_PATH, _T("mpurlsource_*.dll"));

      logger.Log(LOGGER_VERBOSE, _T("%s: %s: search path: %s"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllPath);
      // add plugins directory to search path
      SetDllDirectory(strDllPath);

      h = FindFirstFile(strDllSearch, &info);
      if (h != INVALID_HANDLE_VALUE) 
      {
        do 
        {
          BOOL result = TRUE;
          ALLOC_MEM_DEFINE_SET(strDllName, TCHAR, _MAX_PATH, 0);

          _tcscpy_s(strDllName, _MAX_PATH, strDllPath);
          _tcscat_s(strDllName, _MAX_PATH, info.cFileName);

          // load library
          logger.Log(LOGGER_INFO, _T("%s: %s: loading library: %s"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllName);
          HINSTANCE hLibrary = LoadLibrary(strDllName);        
          if (hLibrary == NULL)
          {
            logger.Log(LOGGER_ERROR, _T("%s: %s: library '%s' not loaded"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllName);
            result = FALSE;
          }

          if (result)
          {
            // find CreateProtocolInstance() function
            // find DestroyProtocolInstance() function
            PIProtocol pIProtocol = NULL;
            CREATEPROTOCOLINSTANCE createProtocolInstance;
            DESTROYPROTOCOLINSTANCE destroyProtocolInstance;

            createProtocolInstance = (CREATEPROTOCOLINSTANCE)GetProcAddress(hLibrary, "CreateProtocolInstance");
            destroyProtocolInstance = (DESTROYPROTOCOLINSTANCE)GetProcAddress(hLibrary, "DestroyProtocolInstance");

            if (createProtocolInstance == NULL)
            {
              logger.Log(LOGGER_ERROR, _T("%s: %s: cannot find CreateProtocolInstance() function, error: %d"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, GetLastError());
              result = FALSE;
            }
            if (destroyProtocolInstance == NULL)
            {
              logger.Log(LOGGER_ERROR, _T("%s: %s: cannot find DestroyProtocolInstance() function, error: %d"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, GetLastError());
              result = FALSE;
            }

            if (result)
            {
              // create protocol instance
              pIProtocol = (PIProtocol)createProtocolInstance();
              if (pIProtocol == NULL)
              {
                logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, _T("cannot create protocol implementation instance"));
                result = FALSE;
              }

              if (result)
              {
                // library is loaded and protocol implementation is instanced
                protocolImplementations[this->protocolImplementationsCount].hLibrary = hLibrary;
                protocolImplementations[this->protocolImplementationsCount].pImplementation = pIProtocol;
                protocolImplementations[this->protocolImplementationsCount].protocol = pIProtocol->GetProtocolName();
                protocolImplementations[this->protocolImplementationsCount].supported = false;
                protocolImplementations[this->protocolImplementationsCount].destroyProtocolInstance = destroyProtocolInstance;

                if (protocolImplementations[this->protocolImplementationsCount].protocol == NULL)
                {
                  // error occured while getting protocol name
                  logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, _T("cannot get protocol name"));
                  protocolImplementations[this->protocolImplementationsCount].destroyProtocolInstance(protocolImplementations[this->protocolImplementationsCount].pImplementation);

                  protocolImplementations[this->protocolImplementationsCount].hLibrary = NULL;
                  protocolImplementations[this->protocolImplementationsCount].pImplementation = NULL;
                  protocolImplementations[this->protocolImplementationsCount].protocol = NULL;
                  protocolImplementations[this->protocolImplementationsCount].supported = false;
                  protocolImplementations[this->protocolImplementationsCount].destroyProtocolInstance = NULL;

                  result = FALSE;
                }
              }

              if (result)
              {
                // initialize protocol implementation
                CParameterCollection *parameters = new CParameterCollection();
                // add global configuration parameters
                parameters->Append(this->configuration);
                // add protocol specific parameters
                CParameterCollection *protocolSpecific = GetConfiguration(protocolImplementations[this->protocolImplementationsCount].protocol);
                parameters->Append(protocolSpecific);

                delete protocolSpecific;

                // initialize protocol
                int initialized = protocolImplementations[this->protocolImplementationsCount].pImplementation->Initialize(this, parameters);
                // delete collection of parameters
                delete parameters;

                if (initialized == STATUS_OK)
                {
                  TCHAR *guid = ConvertGuidToString(protocolImplementations[this->protocolImplementationsCount].pImplementation->GetInstanceId());
                  logger.Log(LOGGER_INFO, _T("%s: %s: protocol '%s' successfully instanced, id: %s"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, protocolImplementations[this->protocolImplementationsCount].protocol, guid);
                  FREE_MEM(guid);
                  this->protocolImplementationsCount++;
                }
                else
                {
                  logger.Log(LOGGER_INFO, _T("%s: %s: protocol '%s' not initialized"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, protocolImplementations[this->protocolImplementationsCount].protocol);
                  protocolImplementations[this->protocolImplementationsCount].destroyProtocolInstance(protocolImplementations[this->protocolImplementationsCount].pImplementation);
                }
              }
            }

            if (!result)
            {
              // any error occured while loading protocol
              // free library and continue with another
              FreeLibrary(hLibrary);
            }
          }

          FREE_MEM(strDllName);
          if (this->protocolImplementationsCount == maxPlugins)
          {
            break;
          }
        } while (FindNextFile(h, &info));
        FindClose(h);
      } 

      logger.Log(LOGGER_INFO, _T("%s: %s: found protocols: %u"), MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, this->protocolImplementationsCount);

      FREE_MEM(strDllPath);
      FREE_MEM(strDllSearch);
    }
    else
    {
      logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, _T("cannot allocate memory for protocol implementations"));
    }
  }

  logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME);
}

int CMPUrlSourceFilter::PushMediaPacket(const TCHAR *outputPinName, CMediaPacket *mediaPacket)
{
  this->logger.Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  this->status = STATUS_RECEIVING_DATA;

  CAsyncSourceStream *stream = this->sourceStreamCollection->GetStream((TCHAR *)outputPinName, false);
  int result = (stream != NULL) ? STATUS_OK : STATUS_ERROR;

  if (result == STATUS_OK)
  {
    result = stream->PushMediaPacket(outputPinName, mediaPacket);
  }
  else
  {
    this->logger.Log(LOGGER_WARNING, _T("%s: %s: unknown stream: %s"), MODULE_NAME, METHOD_PUSH_DATA_NAME, outputPinName);
  }

  if ((result != STATUS_OK) && (mediaPacket != NULL))
  {
    // if result if not STATUS_OK than release media packet
    // because receiver is responsible of deleting media packet
    delete mediaPacket;
  }

  this->logger.Log(LOGGER_DATA, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  return result;
}

int CMPUrlSourceFilter::EndOfStreamReached(const TCHAR *outputPinName)
{
  this->logger.Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME);

  CAsyncSourceStream *stream = this->sourceStreamCollection->GetStream((TCHAR *)outputPinName, false);
  int result = (stream != NULL) ? STATUS_OK : STATUS_ERROR;

  if (result == STATUS_OK)
  {
    result = stream->EndOfStreamReached(outputPinName);
  }
  else
  {
    this->logger.Log(LOGGER_WARNING, _T("%s: %s: unknown stream: %s"), MODULE_NAME, METHOD_PUSH_DATA_NAME, outputPinName);
  }

  this->logger.Log(LOGGER_DATA, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  return result;
}

int CMPUrlSourceFilter::SetTotalLength(const TCHAR *outputPinName, LONGLONG total, bool estimate)
{
  this->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SET_TOTAL_LENGTH_NAME);

  CAsyncSourceStream *stream = this->sourceStreamCollection->GetStream((TCHAR *)outputPinName, false);
  int result = (stream != NULL) ? STATUS_OK : STATUS_ERROR;

  if (result == STATUS_OK)
  {
    result = stream->SetTotalLength(outputPinName, total, estimate);
  }
  else
  {
    this->logger.Log(LOGGER_WARNING, _T("%s: %s: unknown stream: %s"), MODULE_NAME, METHOD_SET_TOTAL_LENGTH_NAME, outputPinName);
  }

  this->logger.Log(LOGGER_INFO, (result == STATUS_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_SET_TOTAL_LENGTH_NAME);
  return result;
}

bool CMPUrlSourceFilter::Load(const TCHAR *url, const CParameterCollection *parameters)
{
  // for each protocol run ParseUrl() method
  // those which return STATUS_OK supports protocol
  // set active protocol to first implementation
  bool retval = false;
  for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
  {
    if (protocolImplementations[i].pImplementation != NULL)
    {
      protocolImplementations[i].supported = (protocolImplementations[i].pImplementation->ParseUrl(url, parameters) == STATUS_OK);
      if ((protocolImplementations[i].supported) && (!retval))
      {
        // active protocol wasn't set yet
        this->activeProtocol = protocolImplementations[i].pImplementation;
      }

      retval |= protocolImplementations[i].supported;
    }
  }

  return retval;
}

DWORD WINAPI CMPUrlSourceFilter::ReceiveDataWorker(LPVOID lpParam)
{
  CMPUrlSourceFilter *caller = (CMPUrlSourceFilter *)lpParam;
  caller->logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME);

  unsigned int attempts = 0;
  bool stopReceivingData = false;

  while ((!caller->receiveDataWorkerShouldExit) && (!stopReceivingData))
  {
    Sleep(1);

    if (caller->activeProtocol != NULL)
    {
      unsigned int maximumAttempts = caller->activeProtocol->GetOpenConnectionMaximumAttempts();

      // if in active protocol is opened connection than receive data
      // if not than open connection
      if (caller->activeProtocol->IsConnected())
      {
        caller->activeProtocol->ReceiveData(&caller->receiveDataWorkerShouldExit);
      }
      else
      {
        if (attempts < maximumAttempts)
        {
          int result = caller->activeProtocol->OpenConnection();
          switch (result)
          {
          case STATUS_OK:
            // set attempts to zero
            attempts = 0;
            break;
          case STATUS_ERROR_NO_RETRY:
            caller->logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME, _T("cannot open connection"));
            caller->status = STATUS_NO_DATA_ERROR;
            stopReceivingData = true;
            break;
          default:
            // increase attempts
            attempts++;
            break;
          }
        }
        else
        {
          caller->logger.Log(LOGGER_ERROR, _T("%s: %s: maximum attempts of opening connection reached, attempts: %u, maximum attempts: %u"), MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME, attempts, maximumAttempts);
          caller->status = STATUS_NO_DATA_ERROR;
          stopReceivingData = true;
        }
      }
    }
  }

  caller->logger.Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME);
  return S_OK;
}

TCHAR *CMPUrlSourceFilter::GetProtocolName(void)
{
  TCHAR *result = NULL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetProtocolName();
  }

  return result;
}

bool CMPUrlSourceFilter::IsConnected(void)
{
  bool result = false;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->IsConnected();
  }

  return result;
}

unsigned int CMPUrlSourceFilter::GetReceiveDataTimeout(void)
{
  unsigned int result = UINT_MAX;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetReceiveDataTimeout();
  }

  return result;
}

GUID CMPUrlSourceFilter::GetInstanceId(void)
{
  GUID result = GUID_NULL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetInstanceId();
  }

  return result;
}

unsigned int CMPUrlSourceFilter::GetOpenConnectionMaximumAttempts(void)
{
  unsigned int result = UINT_MAX;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetOpenConnectionMaximumAttempts();
  }

  return result;
}

CStringCollection *CMPUrlSourceFilter::GetStreamNames(void)
{
  CStringCollection *result = NULL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetStreamNames();
  }

  return result;
}

HRESULT CMPUrlSourceFilter::ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime)
{
  HRESULT result = E_NOT_VALID_STATE;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->ReceiveDataFromTimestamp(startTime, endTime);
  }

  return result;
}

HRESULT CMPUrlSourceFilter::AbortStreamReceive(void)
{
  HRESULT result = E_NOT_VALID_STATE;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->AbortStreamReceive();
  }

  return result;
}

HRESULT CMPUrlSourceFilter::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  HRESULT result = E_NOT_VALID_STATE;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->QueryStreamProgress(total, current);
  }

  return result;
}

HRESULT CMPUrlSourceFilter::QueryStreamAvailableLength(LONGLONG *available)
{
  HRESULT result = E_NOTIMPL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->QueryStreamAvailableLength(available);
  }

  return result;
}

HRESULT CMPUrlSourceFilter::QueryRangesSupported(bool *rangesSupported)
{
  HRESULT result = E_NOTIMPL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->QueryRangesSupported(rangesSupported);
  }

  return result;
}

//// split parameters string by separator
//// @param parameters : null-terminated string containing parameters
//// @param separator : null-terminated separator string
//// @param length : length of first token (without separator)
//// @param restOfParameters : reference to rest of parameter string without first token and separator, if NULL then there is no rest of parameters and whole parameters string was processed
//// @param separatorMustBeFound : specifies if separator must be found
//// @return : true if successful, false otherwise
//bool SplitBySeparator(const TCHAR *parameters, const TCHAR *separator, unsigned int *length, TCHAR **restOfParameters, bool separatorMustBeFound)
//{
//  bool result = false;
//
//  if ((parameters != NULL) && (separator != NULL) && (length != NULL) && (restOfParameters))
//  {
//    unsigned int parameterLength = _tcslen(parameters);
//
//    TCHAR *tempSeparator = NULL;
//    TCHAR *tempParameters = (TCHAR *)parameters;
//    while(true)
//    {
//      tempSeparator = (TCHAR *)_tcsstr(tempParameters, separator);
//      if (tempSeparator == NULL)
//      {
//        // possible separator not found
//        *length = _tcslen(parameters);
//        *restOfParameters = NULL;
//        result = !separatorMustBeFound;
//        break;
//      }
//      else
//      {
//        // possible separator found - if after first separator is second separator, it's not separator (double separator represents separator character)
//        // check next character if is it separator character - if yes, continue in cycle, if not than separator found
//
//        if (_tcslen(tempSeparator) > 1)
//        {
//          // we are not on the last character
//          // check next character if is it separator character
//          tempParameters = tempSeparator + _tcslen(separator);
//          if (_tcsncmp(tempParameters, separator, _tcslen(separator)) != 0)
//          {
//            // next character is not separator character
//            // we found separator
//            break;
//          }
//          else
//          {
//            // next character is separator character, skip
//            tempParameters += _tcslen(separator);
//          }
//        }
//        else
//        {
//          // we found separator
//          break;
//        }
//      }
//    }
//
//    if (tempSeparator != NULL)
//    {
//      // we found separator
//      // everything before separator is token, everything after separator is rest
//      *length = parameterLength - _tcslen(tempSeparator);
//      *restOfParameters = tempSeparator + _tcslen(separator);
//      result = true;
//    }
//  }
//
//  return result;
//}
//
//// replaces double separator character with one separator character
//// returns size of memory block to fit result value, 0 if error
//// [in] lpszValue - value with double separator character
//// [in] lpszSeparator - separator string
//// [out] lpszReplacedValue - pointer to buffer where result will be stored, if NULL result is ignored
//// [in] replacedValueLength - size of lpszReplacedValue
//
//// replace double separator character with one separator character
//// @param value : value with double separator character
//// @param separator : separator string
//// @param replacedValue : reference to buffer where result will be stored, if NULL result is ignored
//// @param replacedValueLength : the length of replaced value buffer
//// @return : size of memory block to fit result value, UINT_MAX if error
//unsigned int ReplaceDoubleSeparator(const TCHAR *value, const TCHAR *separator, TCHAR *replacedValue, unsigned int replacedValueLength)
//{
//  unsigned int requiredLength = UINT_MAX;
//  // first count of replaced value length
//
//  if ((value != NULL) && (separator != NULL))
//  {
//    requiredLength = 0;
//
//    TCHAR *tempSeparator = NULL;
//    TCHAR *tempValue = (TCHAR *)value;
//    while(true)
//    {
//      unsigned int valueLength = _tcslen(tempValue);
//      tempSeparator = (TCHAR *)_tcsstr(tempValue, separator);
//
//      if (tempSeparator != NULL)
//      {
//        // possible separator found - if after first separator is second separator, it's not separator (double separator represents separator character)
//        // check next character if is it separator character - if yes, skip, if not than separator found
//
//        requiredLength += valueLength - _tcslen(tempSeparator);
//        if (replacedValue != NULL)
//        {
//          _tcsncat_s(replacedValue, replacedValueLength, tempValue, valueLength - _tcslen(tempSeparator));
//        }
//
//        if (_tcslen(tempSeparator) > 1)
//        {
//          // we are not on the last character
//          // check next character if is it separator character
//          tempValue = tempSeparator + _tcslen(separator);
//          if (_tcsncmp(tempValue, separator, _tcslen(separator)) == 0)
//          {
//            // next character is separator character, skip
//            tempValue += _tcslen(separator);
//            requiredLength++;
//            if (replacedValue != NULL)
//            {
//              _tcsncat_s(replacedValue, replacedValueLength, separator, _tcslen(separator));
//            }
//          }
//        }
//      }
//      else
//      {
//        requiredLength += valueLength;
//        if (replacedValue != NULL)
//        {
//          _tcsncat_s(replacedValue, replacedValueLength, tempValue, valueLength);
//        }
//        break;
//      }
//    }
//  }
//
//  return requiredLength;
//}
//
//STDMETHODIMP CAsyncFilter::SetConnectInfo(LPCOLESTR pszConnectInfo)
//{
//  HRESULT result = S_OK;
//
//  logger.Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME);
//#ifdef _MBCS
//  TCHAR *sParameters = ConvertToMultiByteW(pszConnectInfo);
//#else
//  TCHAR *sParameters = ConvertToUnicodeW(pszConnectInfo);
//#endif
//
//  result = (sParameters == NULL) ? E_FAIL : S_OK;
//
//  if (result == S_OK)
//  {
//    logger.Log(LOGGER_INFO, _T("%s: %s: additional data: %s"), MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, sParameters);
//
//    // now we have unified string
//    // let's parse
//
//    this->m_parameters->Clear();
//
//    bool splitted = false;
//    unsigned int tokenLength = 0;
//    TCHAR *rest = NULL;
//    do
//    {
//      splitted = SplitBySeparator(sParameters, _T("|"), &tokenLength, &rest, false);
//      if (splitted)
//      {
//        // token length is without terminating null character
//        tokenLength++;
//        ALLOC_MEM_DEFINE_SET(token, TCHAR, tokenLength, 0);
//        if (token == NULL)
//        {
//          logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, _T("not enough memory for token"));
//          result = E_OUTOFMEMORY;
//        }
//
//        if (result == S_OK)
//        {
//          // copy token from parameters string
//          _tcsncpy_s(token, tokenLength, sParameters, tokenLength - 1);
//          sParameters = rest;
//
//          unsigned int nameLength = 0;
//          TCHAR *value = NULL;
//          bool splittedNameAndValue = SplitBySeparator(token, _T("="), &nameLength, &value, true);
//
//          if ((splittedNameAndValue) && (nameLength != 0))
//          {
//            // if correctly splitted parameter name and value
//            nameLength++;
//            ALLOC_MEM_DEFINE_SET(name, TCHAR, nameLength, 0);
//            if (name == NULL)
//            {
//              logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, _T("not enough memory for parameter name"));
//              result = E_OUTOFMEMORY;
//            }
//
//            if (result == S_OK)
//            {
//              // copy name from token
//              _tcsncpy_s(name, nameLength, token, nameLength - 1);
//
//              // get length of value with replaced double separator
//              unsigned int replacedLength = ReplaceDoubleSeparator(value, _T("|"), NULL, 0) + 1;
//
//              ALLOC_MEM_DEFINE_SET(replacedValue, TCHAR, replacedLength, 0);
//              if (replacedValue == NULL)
//              {
//                logger.Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, _T("not enough memory for replaced value"));
//                result = E_OUTOFMEMORY;
//              }
//
//              if (result == S_OK)
//              {
//                ReplaceDoubleSeparator(value, _T("|"), replacedValue, replacedLength);
//
//                CParameter *parameter = new CParameter(name, replacedValue);
//                this->m_parameters->Add(parameter);
//              }
//
//              FREE_MEM(replacedValue);
//            }
//
//            FREE_MEM(name);
//          }
//        }
//
//        FREE_MEM(token);
//      }
//    } while ((splitted) && (rest != NULL) && (result == S_OK));
//
//    if (result == S_OK)
//    {
//      logger.Log(LOGGER_INFO, _T("%s: %s: count of parameters: %u"), MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, this->m_parameters->Count());
//      for(unsigned int i = 0; i < this->m_parameters->Count(); i++)
//      {
//        PCParameter parameter = this->m_parameters->GetParameter(i);
//        logger.Log(LOGGER_INFO, _T("%s: %s: parameter name: %s, value: %s"), MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME, parameter->GetName(), parameter->GetValue());
//      }
//    }
//  }
//
//  FREE_MEM(sParameters);
//
//  logger.Log(LOGGER_INFO, (result == S_OK) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_SET_CONNECT_INFO_NAME);
//  
//  return S_OK;
//}
