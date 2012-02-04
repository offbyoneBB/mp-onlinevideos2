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

#include "stdafx.h"
#include "InputPin.h"

#include "LAVSplitter.h"
#include "LockMutex.h"

#include <Shlwapi.h>
#include <Shlobj.h>

#define READ_BUFFER_SIZE 32768

#ifdef _DEBUG
#define MODULE_NAME                                               L"InputPind"
#else
#define MODULE_NAME                                               L"InputPin"
#endif
#define MODULE_FILE_NAME                                          L"MPUrlSourceSplitter.ax"

#define METHOD_PARSE_PARAMETERS_NAME                              L"ParseParameters()"
#define METHOD_LOAD_PLUGINS_NAME                                  L"LoadPlugins()"
#define METHOD_LOAD_NAME                                          L"Load()"
#define METHOD_RECEIVE_DATA_WORKER_NAME                           L"ReceiveDataWorker()"
#define METHOD_CREATE_RECEIVE_DATA_WORKER_NAME                    L"CreateReceiveDataWorker()"
#define METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME                   L"DestroyReceiveDataWorker()"
#define METHOD_PUSH_DATA_NAME                                     L"PushData()"
#define METHOD_SET_TOTAL_LENGTH_NAME                              L"SetTotalLength()"
#define METHOD_DOWNLOAD_NAME                                      L"Download()"
#define METHOD_DOWNLOAD_ASYNC_NAME                                L"DownloadAsync()"
#define METHOD_DOWNLOAD_CALLBACK_NAME                             L"OnDownloadCallback()"

#define METHOD_SYNC_READ_NAME                                     L"SyncRead()"
#define METHOD_LENGTH_NAME                                        L"Length()"
#define METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME           L"CreateAsyncRequestProcessWorker()"
#define METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME          L"DestroyAsyncRequestProcessWorker()"
#define METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME                  L"AsyncRequestProcessWorker()"
#define METHOD_CREATE_DEMUXER_WORKER_NAME                         L"CreateDemuxerWorker()"
#define METHOD_DESTROY_DEMUXER_WORKER_NAME                        L"DestroyDemuxerWorker()"
#define METHOD_DEMUXER_WORKER_NAME                                L"DemuxerWorker()"

#define METHOD_GET_CAPABILITIES_NAME                              L"GetCapabilities()"
#define METHOD_CHECK_CAPABILITIES_NAME                            L"CheckCapabilities()"
#define METHOD_IS_FORMAT_SUPPORTED_NAME                           L"IsFormatSupported()"
#define METHOD_QUERY_PREFFERED_FORMAT_NAME                        L"QueryPreferredFormat()"
#define METHOD_GET_TIME_FORMAT_NAME                               L"GetTimeFormat()"
#define METHOD_IS_USING_TIME_FORMAT_NAME                          L"IsUsingTimeFormat()"
#define METHOD_SET_TIME_FORMAT_NAME                               L"SetTimeFormat()"
#define METHOD_GET_DURATION_NAME                                  L"GetDuration()"
#define METHOD_GET_STOP_POSITION_NAME                             L"GetStopPosition()"
#define METHOD_GET_CURRENT_POSITION_NAME                          L"GetCurrentPosition()"
#define METHOD_CONVERT_TIME_FORMAT_NAME                           L"ConvertTimeFormat()"
#define METHOD_SET_POSITIONS_NAME                                 L"SetPositions()"
#define METHOD_GET_POSITIONS_NAME                                 L"GetPositions()"
#define METHOD_GET_AVAILABLE_NAME                                 L"GetAvailable()"
#define METHOD_SET_RATE_NAME                                      L"SetRate()"
#define METHOD_GET_RATE_NAME                                      L"GetRate()"
#define METHOD_GET_PREROLL_NAME                                   L"GetPreroll()"
#define METHOD_SET_POSITIONS_INTERNAL_NAME                        L"SetPositionsInternal()"
#define METHOD_SEEK_NAME                                          L"Seek()"
#define METHOD_READ_SEEK_NAME                                     L"ReadSeek()"
#define METHOD_READ_NAME                                          L"Read()"

#define PARAMETER_SEPARATOR                                       L"&"
#define PARAMETER_IDENTIFIER                                      L"####"
#define PARAMETER_ASSIGN                                          L"="

extern "C" char *curl_easy_unescape(void *handle, const char *string, int length, int *olen);
extern "C" void curl_free(void *p);

CLAVInputPin::CLAVInputPin(CLogger *logger, TCHAR *pName, CLAVSplitter *pFilter, CCritSec *pLock, HRESULT *phr)
  : CUnknown(pName, NULL)
  , m_rtStart(0)
  , m_rtStop(0)
  , m_dRate(1.0)
  , m_rtLastStart(_I64_MIN)
  , m_rtLastStop(_I64_MIN)
  , m_rtCurrent(0)
  , m_bStopValid(FALSE)
{
  this->configuration = new CParameterCollection();

  this->logger = logger;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
  
  this->createdDemuxer = false;
  this->m_pAVIOContext = NULL;
  this->m_llBufferPosition = 0;
  this->filter = pFilter;
  this->receiveDataWorkerShouldExit = false;
  this->activeProtocol = NULL;
  this->protocolImplementationsCount = 0;
  this->protocolImplementations = NULL;
  this->url = NULL;
  this->downloadFileName = NULL;
  this->asyncDownloadFinished = false;
  this->downloadFinished = false;
  this->downloadCallbackCalled = false;
  this->asyncDownloadResult = S_OK;
  this->asyncDownloadCallback = NULL;
  this->hReceiveDataWorkerThread = NULL;
  this->status = STATUS_NONE;
  this->mainModuleHandle = GetModuleHandle(MODULE_FILE_NAME);
  this->requestsCollection = new CAsyncRequestCollection();
  this->mediaPacketCollection = new CMediaPacketCollection();
  this->totalLength = 0;
  this->estimate = true;
  this->asyncRequestProcessingShouldExit = false;
  this->requestId = 0;
  this->requestMutex = CreateMutex(NULL, FALSE, NULL);
  this->mediaPacketMutex = CreateMutex(NULL, FALSE, NULL);
  
  this->storeFilePath = Duplicate(this->configuration->GetValue(PARAMETER_NAME_DOWNLOAD_FILE_NAME, true, NULL));
  this->downloadingFile = (this->storeFilePath != NULL);

  this->hCreateDemuxerWorkerThread = NULL;

  if (this->mainModuleHandle == NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, L"main module handle not found");
  }

  // load plugins from directory
  this->LoadPlugins();

  HRESULT result = S_OK;
  if (SUCCEEDED(result))
  {
    result = this->CreateAsyncRequestProcessWorker();
  }

  if (phr)
  {
    *phr = result;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
}

CLAVInputPin::~CLAVInputPin(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);

  this->DestroyDemuxerWorker();
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
  this->DestroyAsyncRequestProcessWorker();
  this->ReleaseAVIOContext();

  // decrements the number of pins on this filter
  delete this->requestsCollection;
  delete this->mediaPacketCollection;

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
  if ((!this->downloadingFile) && (this->storeFilePath != NULL))
  {
    DeleteFile(this->storeFilePath);
  }

  FREE_MEM(this->storeFilePath);

  // release all protocol implementations
  if (this->protocolImplementations != NULL)
  {
    for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
    {
      this->logger->Log(LOGGER_INFO, L"%s: %s: destroying protocol: %s", MODULE_NAME, METHOD_DESTRUCTOR_NAME, protocolImplementations[i].protocol);

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

  delete this->configuration;
  FREE_MEM(this->url);
  FREE_MEM(this->downloadFileName);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);
}

STDMETHODIMP CLAVInputPin::NonDelegatingQueryInterface(REFIID riid, void** ppv)
{
  CheckPointer(ppv, E_POINTER);

  return
    __super::NonDelegatingQueryInterface(riid, ppv);
}

int CLAVInputPin::Read(void *opaque, uint8_t *buf, int buf_size)
{
  CLAVInputPin *pin = static_cast<CLAVInputPin *>(opaque);
  CAutoLock lock(pin);

  //pin->logger->Log(LOGGER_VERBOSE, L"%s: %s: position: %llu, size: %d", MODULE_NAME, METHOD_READ_NAME, pin->m_llBufferPosition, buf_size);
  HRESULT hr = pin->SyncRead(pin->m_llBufferPosition, buf_size, buf);
  if (FAILED(hr)) {
    return -1;
  }
  if (hr == S_FALSE) {
    // read single bytes, its internally buffered..
    int count = 0;
    do {
      hr = pin->SyncRead(pin->m_llBufferPosition, 1, buf+count);
      pin->m_llBufferPosition++;
    } while(hr == S_OK && (++count) < buf_size);

    return count;
  }
  pin->m_llBufferPosition += buf_size;
  return buf_size;
}

int64_t CLAVInputPin::Seek(void *opaque,  int64_t offset, int whence)
{
  CLAVInputPin *pin = static_cast<CLAVInputPin *>(opaque);
  CAutoLock lock(pin);

  pin->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SEEK_NAME);

  int64_t pos = 0;
  LONGLONG total = 0;
  LONGLONG available = 0;
  pin->Length(&total, &available);

  int64_t result = 0;
  bool resultSet = false;

  if (whence == SEEK_SET)
  {
    pin->m_llBufferPosition = offset;
    pin->logger->Log(LOGGER_INFO, L"%s: %s: offset: %lld, SEEK_SET", MODULE_NAME, METHOD_SEEK_NAME, offset);
  }
  else if (whence == SEEK_CUR)
  {
    pin->m_llBufferPosition += offset;
    pin->logger->Log(LOGGER_INFO, L"%s: %s: offset: %lld, SEEK_CUR", MODULE_NAME, METHOD_SEEK_NAME, offset);
  }
  else if (whence == SEEK_END)
  {
    pin->m_llBufferPosition = total - offset;
    pin->logger->Log(LOGGER_INFO, L"%s: %s: offset: %lld, SEEK_END", MODULE_NAME, METHOD_SEEK_NAME, offset);
  }
  else if (whence == AVSEEK_SIZE)
  {
    result = total;
    resultSet = true;
    pin->logger->Log(LOGGER_INFO, L"%s: %s: offset: %lld, AVSEEK_SIZE", MODULE_NAME, METHOD_SEEK_NAME, offset);
  }
  else
  {
    result = -1;
    resultSet = true;
    pin->logger->Log(LOGGER_ERROR, L"%s: %s: offset: %lld, unknown seek value", MODULE_NAME, METHOD_SEEK_NAME, offset);
  }

  if (!resultSet)
  {
    if (pin->m_llBufferPosition > available)
    {
      pin->m_llBufferPosition = available;
    }

    result = pin->m_llBufferPosition;
    resultSet = true;
  }

  pin->logger->Log(LOGGER_INFO, L"%s: %s: End, result: %lld", MODULE_NAME, METHOD_SEEK_NAME, result);
  return result;
}

AVIOContext *CLAVInputPin::GetAVIOContext(void)
{
  if (!m_pAVIOContext)
  {
    uint8_t *buffer = (uint8_t *)av_mallocz(READ_BUFFER_SIZE + FF_INPUT_BUFFER_PADDING_SIZE);
    m_pAVIOContext = avio_alloc_context(buffer, READ_BUFFER_SIZE, 0, this, Read, NULL, Seek);
  }

  return m_pAVIOContext;
}

void CLAVInputPin::ReleaseAVIOContext(void)
{
  if (this->m_pAVIOContext != NULL)
  {
    av_free(this->m_pAVIOContext->buffer);
    av_free(this->m_pAVIOContext);
    this->m_pAVIOContext = NULL;
    this->m_llBufferPosition = 0;
  }
}

STDMETHODIMP CLAVInputPin::BeginFlush()
{
  return E_UNEXPECTED;
}

STDMETHODIMP CLAVInputPin::EndFlush()
{
  return E_UNEXPECTED;
}

// IFileSourceFilter
STDMETHODIMP CLAVInputPin::Load(LPCOLESTR pszFileName, const AM_MEDIA_TYPE * pmt)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_LOAD_NAME);

  this->DestroyDemuxerWorker();
  // stop receiving data
  this->DestroyReceiveDataWorker();

  // reset all protocol implementations
  if (this->protocolImplementations != NULL)
  {
    for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
    {
      this->logger->Log(LOGGER_INFO, L"%s: %s: reseting protocol: %s", MODULE_NAME, METHOD_LOAD_NAME, protocolImplementations[i].protocol);
      if (protocolImplementations[i].pImplementation != NULL)
      {
        protocolImplementations[i].pImplementation->ClearSession();
      }
    }
  }

  this->url = ConvertToUnicodeW(pszFileName);
  //this->url = ConvertToUnicodeW(L"http://o-o.preferred.orangesk-bts1.v19.lscache6.c.youtube.com/videoplayback?sparams=id%2Cexpire%2Cip%2Cipbits%2Citag%2Csource%2Cratebypass%2Ccp&fexp=919700%2C913405%2C910104&itag=22&ip=109.0.0.0&signature=724FDDF479063C65CEAC53F79B02A1A36CBF23CB.21AA87A3A967CFA03FB13A2B722D1F8A5395FCB4&sver=3&ratebypass=yes&source=youtube&expire=1328403712&key=yt1&ipbits=8&cp=U0hRTVRLUV9GTENOMV9MRVVIOmlETGNTVUcyRG1l&id=290eb3afa9023e3f&ext=.mp4");
  //this->url = ConvertToUnicodeW(L"http://o-o.preferred.orangesk-bts1.v11.lscache3.c.youtube.com/videoplayback?sparams=id%2Cexpire%2Cip%2Cipbits%2Citag%2Csource%2Calgorithm%2Cburst%2Cfactor%2Ccp&fexp=906929%2C912300%2C906426%2C913105&algorithm=throttle-factor&itag=35&ip=109.0.0.0&burst=40&sver=3&signature=2459761FDA98D14248BEC0C4E69889EBF1BDB6A0.046670E1231F74F753B61D8B36705230E5CE466C&source=youtube&expire=1328403712&key=yt1&ipbits=8&factor=1.25&cp=U0hRTVRLUV9GTENOMV9MRVVIOmk0V09mTDlOUC1L&id=290eb3afa9023e3f&ext=.flv");
  //this->url = ConvertToUnicodeW(L"rtmp://wcdn102.nacevi.cz/ct-vod?id=MTQxNjY5MTEyNXw2MzQ2Mzk4MTMyMTgyNjYyNTA=&type=wpl/mp4:iVysilani/2011/06/05/3Plus1SMiroslaveCT1-050611-MP4_576p.mp4####Url=rtmp%3a%2f%2fwcdn102.nacevi.cz%2fct-vod%3fid%3dMTQxNjY5MTEyNXw2MzQ2Mzk4MTMyMTgyNjYyNTA%3d%26type%3dwpl%2fmp4%3aiVysilani%2f2011%2f06%2f05%2f3Plus1SMiroslaveCT1-050611-MP4_576p.mp4&RtmpApp=ct-vod%3fid%3dMTQxNjY5MTEyNXw2MzQ2Mzk4MTMyMTgyNjYyNTA%3d%26type%3dwpl&RtmpPageUrl=http%3a%2f%2fwww.ceskatelevize.cz%2fivysilani%2f1148961737-3-plus-1-s-miroslavem-donutilem%2f211512120790001-3-plus-1-s-miroslavem-donutilem-smolari%2f&RtmpPlayPath=mp4%3aiVysilani%2f2011%2f06%2f05%2f3Plus1SMiroslaveCT1-050611-MP4_576p.mp4&RtmpSwfUrl=http%3a%2f%2fimg2.ceskatelevize.cz%2flibraries%2fplayer%2fflashPlayer.swf%3fversion%3d1.44.6&RtmpTcUrl=rtmp%3a%2f%2fwcdn102.nacevi.cz%2fct-vod%3fid%3dMTQxNjY5MTEyNXw2MzQ2Mzk4MTMyMTgyNjYyNTA%3d%26type%3dwpl");

  if (this->url == NULL)
  {
    result = E_OUTOFMEMORY;
  }

  if (result == S_OK)
  {
    CParameterCollection *suppliedParameters = this->ParseParameters(this->url);
    if (suppliedParameters != NULL)
    {
      // we have set some parameters
      // set them as configuration parameters
      this->configuration->Clear();
      this->configuration->Append(suppliedParameters);
      if (!this->configuration->Contains(PARAMETER_NAME_URL, true))
      {
        this->configuration->Add(new CParameter(PARAMETER_NAME_URL, this->url));
      }

      delete suppliedParameters;
      suppliedParameters = NULL;
    }
    else
    {
      // parameters are not supplied, just set current url as only one parameter in configuration
      this->configuration->Clear();
      this->configuration->Add(new CParameter(PARAMETER_NAME_URL, this->url));
    }
  }

  if (result == S_OK)
  {
    // loads protocol based on current configuration parameters
    result = this->Load();
  }

  if (SUCCEEDED(result) && (!this->downloadingFile))
  {
    // splitter is not needed when downloading file
    result = this->CreateDemuxerWorker();
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_LOAD_NAME, result);
  return result;
}

STDMETHODIMP CLAVInputPin::GetCurFile(LPOLESTR *ppszFileName, AM_MEDIA_TYPE *pmt)
{
  if (!ppszFileName)
  {
    return E_POINTER;
  }

  *ppszFileName = ConvertToUnicode(this->url);
  if ((*ppszFileName) == NULL)
  {
    return E_FAIL;
  }

  return S_OK;

}

// IAMOpenProgress
STDMETHODIMP CLAVInputPin::QueryProgress(LONGLONG *pllTotal, LONGLONG *pllCurrent)
{
  return this->QueryStreamProgress(pllTotal, pllCurrent);
}

// IAMOpenProgress
STDMETHODIMP CLAVInputPin::AbortOperation(void)
{
  return this->AbortStreamReceive();
}

// IDownloadCallback
void STDMETHODCALLTYPE CLAVInputPin::OnDownloadCallback(HRESULT downloadResult)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_CALLBACK_NAME);

  this->asyncDownloadResult = downloadResult;
  this->asyncDownloadFinished = true;

  if ((this->asyncDownloadCallback != NULL) && (this->asyncDownloadCallback != this))
  {
    // if download callback is set and it is not current instance (avoid recursion)
    this->asyncDownloadCallback->OnDownloadCallback(downloadResult);
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_CALLBACK_NAME);
}

// IDownload
STDMETHODIMP CLAVInputPin::Download(LPCOLESTR uri, LPCOLESTR fileName)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_NAME);

  result = this->DownloadAsync(uri, fileName, this);

  if (result == S_OK)
  {
    // downloading process is successfully started
    // just wait for callback and return to caller
    while (!this->asyncDownloadFinished)
    {
      // just sleep
      Sleep(100);
    }

    result = this->asyncDownloadResult;
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_NAME, result);
  return result;
}

STDMETHODIMP CLAVInputPin::DownloadAsync(LPCOLESTR uri, LPCOLESTR fileName, IDownloadCallback *downloadCallback)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_ASYNC_NAME);

  CHECK_POINTER_DEFAULT_HRESULT(result, uri);
  CHECK_POINTER_DEFAULT_HRESULT(result, fileName);
  CHECK_POINTER_DEFAULT_HRESULT(result, downloadCallback);

  if (result == S_OK)
  {
    // stop receiving data
    this->DestroyReceiveDataWorker();

    // reset all protocol implementations
    if (this->protocolImplementations != NULL)
    {
      for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
      {
        this->logger->Log(LOGGER_INFO, L"%s: %s: reseting protocol: %s", MODULE_NAME, METHOD_DOWNLOAD_ASYNC_NAME, protocolImplementations[i].protocol);
        if (protocolImplementations[i].pImplementation != NULL)
        {
          protocolImplementations[i].pImplementation->ClearSession();
        }
      }
    }

    this->asyncDownloadResult = S_OK;
    this->asyncDownloadFinished = false;
    this->asyncDownloadCallback = downloadCallback;
  }

  if (result == S_OK)
  {
    this->url = ConvertToUnicodeW(uri);
    this->downloadFileName = ConvertToUnicodeW(fileName);

    result = ((this->url == NULL) || (this->downloadFileName == NULL)) ? E_OUTOFMEMORY : S_OK;
  }

  if (result == S_OK)
  {
    CParameterCollection *suppliedParameters = this->ParseParameters(this->url);
    if (suppliedParameters != NULL)
    {
      // we have set some parameters
      // set them as configuration parameters
      this->configuration->Clear();
      this->configuration->Append(suppliedParameters);
      if (!this->configuration->Contains(PARAMETER_NAME_URL, true))
      {
        this->configuration->Add(new CParameter(PARAMETER_NAME_URL, this->url));
      }
      this->configuration->Add(new CParameter(PARAMETER_NAME_DOWNLOAD_FILE_NAME, this->downloadFileName));

      delete suppliedParameters;
      suppliedParameters = NULL;
    }
    else
    {
      // parameters are not supplied, just set current url and download file name as only parameters in configuration
      this->configuration->Clear();
      this->configuration->Add(new CParameter(PARAMETER_NAME_URL, this->url));
      this->configuration->Add(new CParameter(PARAMETER_NAME_DOWNLOAD_FILE_NAME, this->downloadFileName));
    }
  }

  if (result == S_OK)
  {
    // loads protocol based on current configuration parameters
    result = this->Load();
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DOWNLOAD_ASYNC_NAME, result);
  return result;
}

STDMETHODIMP CLAVInputPin::Load()
{
  HRESULT result = S_OK;

  if (this->configuration == NULL)
  {
    result = E_FAIL;
  }

  if (SUCCEEDED(result))
  {
    FREE_MEM(this->url);
    this->url = Duplicate(this->configuration->GetValue(PARAMETER_NAME_URL, true, NULL));
    result = (this->url == NULL) ? E_OUTOFMEMORY : S_OK;
  }

  if (SUCCEEDED(result))
  {
    FREE_MEM(this->storeFilePath);
    this->storeFilePath = Duplicate(this->configuration->GetValue(PARAMETER_NAME_DOWNLOAD_FILE_NAME, true, NULL));
    this->downloadingFile = (this->storeFilePath != NULL);
  }

  if (SUCCEEDED(result))
  {
    if(!this->LoadProtocolImplementation(this->url, this->configuration))
    {
      result = E_FAIL;
    }
  }

  if (SUCCEEDED(result))
  {
    // now we have active protocol with loaded url, but still not working
    // create thread for receiving data

    result = this->CreateReceiveDataWorker();
  }

  if (SUCCEEDED(result))
  {
    DWORD ticks = GetTickCount();
    DWORD timeout = 0;

    if (this->activeProtocol != NULL)
    {
      // get receive data timeout for active protocol
      timeout = this->activeProtocol->GetReceiveDataTimeout();
      wchar_t *protocolName = this->activeProtocol->GetProtocolName();
      this->logger->Log(LOGGER_INFO, L"%s: %s: active protocol '%s' timeout: %d (ms)", MODULE_NAME, METHOD_LOAD_NAME, protocolName, timeout);
      FREE_MEM(protocolName);
    }
    else
    {
      this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_NAME, L"no active protocol");
      result = E_FAIL;
    }

    if (SUCCEEDED(result))
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
        result = -1;
        break;
      case STATUS_RECEIVING_DATA:
        result = S_OK;
        break;
      default:
        result = E_UNEXPECTED;
        break;
      }

      if (FAILED(result))
      {
        this->DestroyReceiveDataWorker();
      }
    }
  }

  return result;
}

HRESULT CLAVInputPin::CreateReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME);

  this->hReceiveDataWorkerThread = CreateThread( 
    NULL,                                   // default security attributes
    0,                                      // use default stack size  
    &CLAVInputPin::ReceiveDataWorker,       // thread function name
    this,                                   // argument to thread function 
    0,                                      // use default creation flags 
    &dwReceiveDataWorkerThreadId);          // returns the thread identifier

  if (this->hReceiveDataWorkerThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger->Log(LOGGER_ERROR, L"%s: %s: CreateThread() error: 0x%08X", MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  }

  if (result == S_OK)
  {
    if (!SetThreadPriority(::GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger->Log(LOGGER_WARNING, L"%s: %s: cannot set thread priority for main thread, error: %u", MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
    if (!SetThreadPriority(this->hReceiveDataWorkerThread, THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger->Log(LOGGER_WARNING, L"%s: %s: cannot set thread priority for receive data thread, error: %u", MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, GetLastError());
    }
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

HRESULT CLAVInputPin::DestroyReceiveDataWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME);

  this->receiveDataWorkerShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hReceiveDataWorkerThread != NULL)
  {
    if (WaitForSingleObject(this->hReceiveDataWorkerThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, L"thread didn't exit, terminating thread");
      TerminateThread(this->hReceiveDataWorkerThread, 0);
    }
  }

  this->hReceiveDataWorkerThread = NULL;
  this->receiveDataWorkerShouldExit = false;

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DESTROY_RECEIVE_DATA_WORKER_NAME, result);
  return result;
}

void CLAVInputPin::LoadPlugins()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME);

  unsigned int maxPlugins = this->configuration->GetValueLong(PARAMETER_NAME_MAX_PLUGINS, true, MAX_PLUGINS_DEFAULT);
  maxPlugins = (maxPlugins < 0) ? MAX_PLUGINS_DEFAULT : maxPlugins;

  if (maxPlugins > 0)
  {
    this->protocolImplementations = ALLOC_MEM(ProtocolImplementation, maxPlugins);
    if (this->protocolImplementations != NULL)
    {
      WIN32_FIND_DATA info;
      HANDLE h;

      ALLOC_MEM_DEFINE_SET(strDllPath, wchar_t, _MAX_PATH, 0);
      ALLOC_MEM_DEFINE_SET(strDllSearch, wchar_t, _MAX_PATH, 0);

      GetModuleFileName(this->mainModuleHandle, strDllPath, _MAX_PATH);
      PathRemoveFileSpec(strDllPath);

      wcscat_s(strDllPath, _MAX_PATH, L"\\");
      wcscpy_s(strDllSearch, _MAX_PATH, strDllPath);
      wcscat_s(strDllSearch, _MAX_PATH, L"mpurlsourcesplitter_*.dll");

      this->logger->Log(LOGGER_VERBOSE, L"%s: %s: search path: %s", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllPath);
      // add plugins directory to search path
      SetDllDirectory(strDllPath);

      h = FindFirstFile(strDllSearch, &info);
      if (h != INVALID_HANDLE_VALUE) 
      {
        do 
        {
          BOOL result = TRUE;
          ALLOC_MEM_DEFINE_SET(strDllName, wchar_t, _MAX_PATH, 0);

          wcscpy_s(strDllName, _MAX_PATH, strDllPath);
          wcscat_s(strDllName, _MAX_PATH, info.cFileName);

          // load library
          this->logger->Log(LOGGER_INFO, L"%s: %s: loading library: %s", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllName);
          HINSTANCE hLibrary = LoadLibrary(strDllName);        
          if (hLibrary == NULL)
          {
            this->logger->Log(LOGGER_ERROR, L"%s: %s: library '%s' not loaded", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, strDllName);
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
              this->logger->Log(LOGGER_ERROR, L"%s: %s: cannot find CreateProtocolInstance() function, error: %d", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, GetLastError());
              result = FALSE;
            }
            if (destroyProtocolInstance == NULL)
            {
              this->logger->Log(LOGGER_ERROR, L"%s: %s: cannot find DestroyProtocolInstance() function, error: %d", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, GetLastError());
              result = FALSE;
            }

            if (result)
            {
              // create protocol instance
              pIProtocol = (PIProtocol)createProtocolInstance(this->configuration);
              if (pIProtocol == NULL)
              {
                this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, L"cannot create protocol implementation instance");
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
                  this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, L"cannot get protocol name");
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
                // we don't have protocol specific parameters
                // all parameters are supplied with calling IFileSourceFilter.Load() method

                // initialize protocol
                HRESULT initialized = protocolImplementations[this->protocolImplementationsCount].pImplementation->Initialize(this, this->configuration);

                if (SUCCEEDED(initialized))
                {
                  TCHAR *guid = ConvertGuidToString(protocolImplementations[this->protocolImplementationsCount].pImplementation->GetInstanceId());
                  this->logger->Log(LOGGER_INFO, L"%s: %s: protocol '%s' successfully instanced, id: %s", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, protocolImplementations[this->protocolImplementationsCount].protocol, guid);
                  FREE_MEM(guid);
                  this->protocolImplementationsCount++;
                }
                else
                {
                  this->logger->Log(LOGGER_INFO, L"%s: %s: protocol '%s' not initialized", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, protocolImplementations[this->protocolImplementationsCount].protocol);
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

      this->logger->Log(LOGGER_INFO, L"%s: %s: found protocols: %u", MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, this->protocolImplementationsCount);

      FREE_MEM(strDllPath);
      FREE_MEM(strDllSearch);
    }
    else
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME, L"cannot allocate memory for protocol implementations");
    }
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_LOAD_PLUGINS_NAME);
}

HRESULT CLAVInputPin::PushMediaPacket(CMediaPacket *mediaPacket)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  this->status = STATUS_RECEIVING_DATA;

  HRESULT result = S_OK;

  {
    CLockMutex lock(this->mediaPacketMutex, INFINITE);
    HRESULT result = S_OK;

    CHECK_POINTER_DEFAULT_HRESULT(result, mediaPacket);

    if (result == S_OK)
    {
      CMediaPacketCollection *unprocessedMediaPackets = new CMediaPacketCollection();
      if (unprocessedMediaPackets->Add(mediaPacket->Clone()))
      {
        int64_t start = mediaPacket->GetStart();
        int64_t stop = mediaPacket->GetEnd();
        this->logger->Log(LOGGER_DATA, L"%s: %s media packet start: %016llu, length: %08u", MODULE_NAME, METHOD_PUSH_MEDIA_PACKET_NAME, start, mediaPacket->GetBuffer()->GetBufferOccupiedSpace());

        result = S_OK;
        while ((unprocessedMediaPackets->Count() != 0) && (result == S_OK))
        {
          // there is still some unprocessed media packets
          // get first media packet
          CMediaPacket *unprocessedMediaPacket = unprocessedMediaPackets->GetItem(0);

          int64_t unprocessedMediaPacketStart = unprocessedMediaPacket->GetStart();
          int64_t unprocessedMediaPacketEnd = unprocessedMediaPacket->GetEnd();

          if (result == S_OK)
          {
            // try to find overlapping media packet
            CMediaPacket *overlappingPacket = this->mediaPacketCollection->GetItem(this->mediaPacketCollection->GetMediaPacketIndexOverlappingPositions(unprocessedMediaPacketStart, unprocessedMediaPacketEnd));
            if (overlappingPacket == NULL)
            {
              // there isn't overlapping media packet
              // whole packet can be added to collection
              result = (this->mediaPacketCollection->Add(unprocessedMediaPacket->Clone())) ? S_OK : E_FAIL;
            }
            else
            {
              // current unprocessed media packet is overlapping some media packet in media packet collection
              // it means that this packet has same data (in overlapping range)
              // there is no need to duplicate data in collection

              int64_t overlappingMediaPacketStart = overlappingPacket->GetStart();
              int64_t overlappingMediaPacketEnd = overlappingPacket->GetEnd();

              if (result == S_OK)
              {
                // we get both media packets start and end
                if (unprocessedMediaPacketStart < overlappingMediaPacketStart)
                {
                  // split unprocessed media packet into two parts
                  // insert them into unprocessed media packet collection

                  // initialize first part
                  int64_t start = unprocessedMediaPacketStart;
                  int64_t end = overlappingMediaPacketStart - 1;
                  CMediaPacket *firstPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                  // initialize second part
                  start = overlappingMediaPacketStart;
                  end = unprocessedMediaPacketEnd;
                  CMediaPacket *secondPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                  result = ((firstPart != NULL) && (secondPart != NULL)) ? S_OK : E_POINTER;

                  if (result == S_OK)
                  {
                    // delete first media packet because it is processed
                    if (!unprocessedMediaPackets->Remove(0))
                    {
                      // some error occured
                      result = E_FAIL;
                    }
                  }

                  if (result == S_OK)
                  {
                    // both media packets are created correctly
                    // now add both packets to unprocessed media collection

                    result = (unprocessedMediaPackets->Add(firstPart)) ? S_OK : E_FAIL;

                    if (result == S_OK)
                    {
                      result = (unprocessedMediaPackets->Add(secondPart)) ? S_OK : E_FAIL;

                      if (FAILED(result))
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
                  int64_t start = unprocessedMediaPacketStart;
                  int64_t end = overlappingMediaPacketEnd;
                  CMediaPacket *firstPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                  // initialize second part
                  start = overlappingMediaPacketEnd + 1;
                  end = unprocessedMediaPacketEnd;
                  CMediaPacket *secondPart = unprocessedMediaPacket->CreateMediaPacketBasedOnPacket(start, end);

                  result = ((firstPart != NULL) && (secondPart != NULL)) ? S_OK : E_POINTER;

                  if (result == S_OK)
                  {
                    // delete first media packet because it is processed
                    if (!unprocessedMediaPackets->Remove(0))
                    {
                      // some error occured
                      result = E_FAIL;
                    }
                  }

                  if (result == S_OK)
                  {
                    // both media packets are created correctly
                    // now add both packets to unprocessed media collection

                    result = (unprocessedMediaPackets->Add(firstPart)) ? S_OK : E_FAIL;

                    if (result == S_OK)
                    {
                      result = (unprocessedMediaPackets->Add(secondPart)) ? S_OK : E_FAIL;

                      if (FAILED(result))
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
                  if (result == S_OK)
                  {
                    // delete first media packet because it is processed
                    if (!unprocessedMediaPackets->Remove(0))
                    {
                      // some error occured
                      result = E_FAIL;
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
  }

  if ((result != S_OK) && (mediaPacket != NULL))
  {
    // if result if not STATUS_OK than release media packet
    // because receiver is responsible of deleting media packet
    delete mediaPacket;
  }

  this->logger->Log(LOGGER_DATA, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  return result;
}

HRESULT CLAVInputPin::EndOfStreamReached(int64_t streamPosition)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME);

  HRESULT result = E_FAIL;

  {
    CLockMutex mediaPacketLock(this->mediaPacketMutex, INFINITE);

    if (this->mediaPacketCollection->Count() > 0)
    {
      this->logger->Log(LOGGER_VERBOSE, L"%s: %s: media packet count: %u, stream position: %llu", MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, this->mediaPacketCollection->Count(), streamPosition);

      // check media packets from supplied last valid stream position
      int64_t startPosition = 0;
      int64_t endPosition = 0;
      unsigned int mediaPacketIndex = this->mediaPacketCollection->GetMediaPacketIndexBetweenPositions(streamPosition);

      if (mediaPacketIndex != UINT_MAX)
      {
        CMediaPacket *mediaPacket = this->mediaPacketCollection->GetItem(mediaPacketIndex);
        startPosition = mediaPacket->GetStart();
        endPosition = mediaPacket->GetEnd();
        this->logger->Log(LOGGER_VERBOSE, L"%s: %s: for stream position '%llu' found media packet, start: %llu, end: %llu", MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, streamPosition, startPosition, endPosition);
      }

      for (int i = 0; i < 2; i++)
      {
        // because collection is sorted
        // then simle going through all media packets will reveal if there is some empty place
        while (mediaPacketIndex != UINT_MAX)
        {
          CMediaPacket *mediaPacket = this->mediaPacketCollection->GetItem(mediaPacketIndex);
          int64_t mediaPacketStart = mediaPacket->GetStart();
          int64_t mediaPacketEnd = mediaPacket->GetEnd();;
          if (startPosition == mediaPacketStart)
          {
            // next start time is next to end of current media packet
            startPosition = mediaPacketEnd + 1;
            mediaPacketIndex++;

            if (mediaPacketIndex >= this->mediaPacketCollection->Count())
            {
              // stop checking, all media packets checked
              endPosition = startPosition;
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: all media packets checked, start: %llu, end: %llu", MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, startPosition, endPosition);
              mediaPacketIndex = UINT_MAX;
            }
          }
          else
          {
            // we found gap between media packets
            // set end time and stop checking media packets
            endPosition = mediaPacketStart - 1;
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: found gap between media packets, start: %llu, end: %llu", MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, startPosition, endPosition);
            mediaPacketIndex = UINT_MAX;
          }
        }

        if ((!estimate) && (startPosition >= this->totalLength) && (i == 0))
        {
          // we are after end of stream
          // check media packets from start if we don't have gap
          startPosition = 0;
          endPosition = 0;
          mediaPacketIndex = this->mediaPacketCollection->GetMediaPacketIndexBetweenPositions(startPosition);
          this->logger->Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, L"searching for gap in media packets from beginning");
        }
        else
        {
          // we found some gap
          break;
        }
      }

      if (((!estimate) && (startPosition < this->totalLength)) || (estimate))
      {
        // found part which is not downloaded
        this->logger->Log(LOGGER_VERBOSE, L"%s: %s: requesting stream part from: %llu, to: %llu", MODULE_NAME, METHOD_END_OF_STREAM_REACHED_NAME, startPosition, endPosition);
        this->SeekToPosition(startPosition, endPosition);
      }
      else
      {
        // all data received
        // if downloading file, mark that download callback can be called after storing all data to download file
        if (this->downloadingFile)
        {
          this->downloadFinished = true;
        }
      }
    }

    result = S_OK;
  }
  
  this->logger->Log(LOGGER_DATA, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, MODULE_NAME, METHOD_PUSH_DATA_NAME);
  return result;
}

HRESULT CLAVInputPin::SetTotalLength(int64_t total, bool estimate)
{
  HRESULT result = E_FAIL;

  {
    CLockMutex lock(this->mediaPacketMutex, INFINITE);

    this->totalLength = total;
    this->estimate = estimate;

    result = S_OK;
  }

  return result;
}

bool CLAVInputPin::LoadProtocolImplementation(const wchar_t *url, const CParameterCollection *parameters)
{
  // for each protocol run ParseUrl() method
  // those which return STATUS_OK supports protocol
  // set active protocol to first implementation
  bool retval = false;
  for(unsigned int i = 0; i < this->protocolImplementationsCount; i++)
  {
    if (protocolImplementations[i].pImplementation != NULL)
    {
      protocolImplementations[i].supported = (protocolImplementations[i].pImplementation->ParseUrl(url, parameters) == S_OK);
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

DWORD WINAPI CLAVInputPin::ReceiveDataWorker(LPVOID lpParam)
{
  CLAVInputPin *caller = (CLAVInputPin *)lpParam;
  caller->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME);

  unsigned int attempts = 0;
  bool stopReceivingData = false;

  HRESULT result = S_OK;
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
          result = caller->activeProtocol->OpenConnection();
          if (SUCCEEDED(result))
          {
            // set attempts to zero
            attempts = 0;
          }
          else
          {
            // increase attempts
            attempts++;
          }
        }
        else
        {
          caller->logger->Log(LOGGER_ERROR, L"%s: %s: maximum attempts of opening connection reached, attempts: %u, maximum attempts: %u", MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME, attempts, maximumAttempts);
          caller->status = STATUS_NO_DATA_ERROR;
          stopReceivingData = true;

          if (caller->downloadFileName != NULL)
          {
            caller->OnDownloadCallback(result);
          }
        }
      }
    }
  }

  caller->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_RECEIVE_DATA_WORKER_NAME);
  return S_OK;
}

// split parameters string by separator
// @param parameters : null-terminated string containing parameters
// @param separator : null-terminated separator string
// @param length : length of first token (without separator)
// @param restOfParameters : reference to rest of parameter string without first token and separator, if NULL then there is no rest of parameters and whole parameters string was processed
// @param separatorMustBeFound : specifies if separator must be found
// @return : true if successful, false otherwise
bool SplitBySeparator(const wchar_t *parameters, const wchar_t *separator, unsigned int *length, wchar_t **restOfParameters, bool separatorMustBeFound)
{
  bool result = false;

  if ((parameters != NULL) && (separator != NULL) && (length != NULL) && (restOfParameters))
  {
    unsigned int parameterLength = wcslen(parameters);

    wchar_t *tempSeparator = NULL;
    wchar_t *tempParameters = (wchar_t *)parameters;

    tempSeparator = (wchar_t *)wcsstr(tempParameters, separator);
    if (tempSeparator == NULL)
    {
      // separator not found
      *length = wcslen(parameters);
      *restOfParameters = NULL;
      result = !separatorMustBeFound;
    }
    else
    {
      // separator found
      if (wcslen(tempSeparator) > 1)
      {
        // we are not on the last character of separator
        // move to end of separator
        tempParameters = tempSeparator + wcslen(separator);
      }
    }

    if (tempSeparator != NULL)
    {
      // we found separator
      // everything before separator is token, everything after separator is rest
      *length = parameterLength - wcslen(tempSeparator);
      *restOfParameters = tempSeparator + wcslen(separator);
      result = true;
    }
  }

  return result;
}

CParameterCollection *CLAVInputPin::ParseParameters(const wchar_t *parameters)
{
  HRESULT result = S_OK;
  CParameterCollection *parsedParameters = new CParameterCollection();

  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME);

  result = ((parameters == NULL) || (parsedParameters == NULL)) ? E_FAIL : S_OK;

  if (result == S_OK)
  {
    this->logger->Log(LOGGER_INFO, L"%s: %s: parameters: %s", MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, parameters);

    // now we have unified string
    // let's parse

    parsedParameters->Clear();

    if (result == S_OK)
    {
      bool splitted = false;
      unsigned int tokenLength = 0;
      wchar_t *rest = NULL;

      splitted = SplitBySeparator(parameters, PARAMETER_IDENTIFIER, &tokenLength, &rest, false);
      if (splitted)
      {
        // identifier for parameters for MediaPortal Source Filter is found
        parameters = rest;
        splitted = false;

        do
        {
          splitted = SplitBySeparator(parameters, PARAMETER_SEPARATOR, &tokenLength, &rest, false);
          if (splitted)
          {
            // token length is without terminating null character
            tokenLength++;
            ALLOC_MEM_DEFINE_SET(token, wchar_t, tokenLength, 0);
            if (token == NULL)
            {
              this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, L"not enough memory for token");
              result = E_OUTOFMEMORY;
            }

            if (result == S_OK)
            {
              // copy token from parameters string
              wcsncpy_s(token, tokenLength, parameters, tokenLength - 1);
              parameters = rest;

              unsigned int nameLength = 0;
              wchar_t *value = NULL;
              bool splittedNameAndValue = SplitBySeparator(token, PARAMETER_ASSIGN, &nameLength, &value, true);

              if ((splittedNameAndValue) && (nameLength != 0))
              {
                // if correctly splitted parameter name and value
                nameLength++;
                ALLOC_MEM_DEFINE_SET(name, wchar_t, nameLength, 0);
                if (name == NULL)
                {
                  this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, L"not enough memory for parameter name");
                  result = E_OUTOFMEMORY;
                }

                if (result == S_OK)
                {
                  // copy name from token
                  wcsncpy_s(name, nameLength, token, nameLength - 1);

                  // the value is in url encoding (percent encoding)
                  // so it doesn't have doubled separator

                  // CURL library cannot handle wchar_t characters
                  // convert to mutli-byte character set

                  char *curlValue = ConvertToMultiByte(value);
                  if (curlValue == NULL)
                  {
                    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, L"not enough memory for value for CURL library");
                    result = E_OUTOFMEMORY;
                  }

                  if (result == S_OK)
                  {
                    char *unescapedCurlValue = curl_easy_unescape(NULL, curlValue, 0, NULL);

                    if (unescapedCurlValue == NULL)
                    {
                      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, "error occured while getting unescaped value from CURL library");
                      result = E_FAIL;
                    }

                    if (result == S_OK)
                    {
                      wchar_t *unescapedValue = ConvertToUnicodeA(unescapedCurlValue);

                      if (unescapedValue == NULL)
                      {
                        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, "not enough memory for unescaped value");
                        result = E_OUTOFMEMORY;
                      }

                      if (result == S_OK)
                      {
                        // we got successfully unescaped parameter value
                        CParameter *parameter = new CParameter(name, unescapedValue);
                        parsedParameters->Add(parameter);
                      }

                      // free unescaped value
                      FREE_MEM(unescapedValue);

                      // free CURL return value
                      curl_free(unescapedCurlValue);
                    }
                  }

                  FREE_MEM(curlValue);
                }

                FREE_MEM(name);
              }
            }

            FREE_MEM(token);
          }
        } while ((splitted) && (rest != NULL) && (result == S_OK));
      }
    }

    if (result == S_OK)
    {
      this->logger->Log(LOGGER_INFO, L"%s: %s: count of parameters: %u", MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, parsedParameters->Count());
      parsedParameters->LogCollection(this->logger, LOGGER_INFO, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME);
    }
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_PARSE_PARAMETERS_NAME, result);

  if ((FAILED(result)) && (parsedParameters != NULL))
  {
    delete parsedParameters;
    parsedParameters = NULL;
  }
  
  return parsedParameters;
}

HRESULT CLAVInputPin::AbortStreamReceive(void)
{
  HRESULT result = E_NOT_VALID_STATE;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->AbortStreamReceive();
  }

  return result;
}

HRESULT CLAVInputPin::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  HRESULT result = E_NOT_VALID_STATE;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->QueryStreamProgress(total, current);
  }

  return result;
}

HRESULT CLAVInputPin::Request(unsigned int *requestId, int64_t position, LONG length, BYTE *buffer, DWORD_PTR userData)
{
  CAsyncRequest* request = new CAsyncRequest();
  if (!request)
  {
    return E_OUTOFMEMORY;
  }

  HRESULT result = request->Request(this->requestId++, position, length, buffer, userData);

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

HRESULT CLAVInputPin::EnqueueAsyncRequest(CAsyncRequest *request)
{
  CLockMutex lock(this->requestMutex, INFINITE);

  HRESULT result = (request != NULL) ? S_OK : E_POINTER;

  if (this->requestsCollection->Add(request))
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

HRESULT CLAVInputPin::CreateAsyncRequestProcessWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME);

  this->asyncRequestProcessingShouldExit = false;

  this->hAsyncRequestProcessingThread = CreateThread( 
    NULL,                                                 // default security attributes
    0,                                                    // use default stack size  
    &CLAVInputPin::AsyncRequestProcessWorker,             // thread function name
    this,                                                 // argument to thread function 
    0,                                                    // use default creation flags 
    &dwAsyncRequestProcessingThreadId);                   // returns the thread identifier

  if (this->hAsyncRequestProcessingThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger->Log(LOGGER_ERROR, L"%s: %s: CreateThread() error: 0x%08X", MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  }

  if (result == S_OK)
  {
    if (!SetThreadPriority(this->hAsyncRequestProcessingThread, THREAD_PRIORITY_TIME_CRITICAL))
    {
      this->logger->Log(LOGGER_WARNING, L"%s: %s: cannot set thread priority for receive data thread, error: %u", MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, GetLastError());
    }
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  return result;
}

HRESULT CLAVInputPin::DestroyAsyncRequestProcessWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME);

  this->asyncRequestProcessingShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hAsyncRequestProcessingThread != NULL)
  {
    if (WaitForSingleObject(this->hAsyncRequestProcessingThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME, L"thread didn't exit, terminating thread");
      TerminateThread(this->hAsyncRequestProcessingThread, 0);
    }
  }

  this->hAsyncRequestProcessingThread = NULL;
  this->asyncRequestProcessingShouldExit = false;

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DESTROY_ASYNC_REQUEST_PROCESS_WORKER_NAME, result);
  return result;
}

HRESULT CLAVInputPin::CheckValues(CAsyncRequest *request, CMediaPacket *mediaPacket, unsigned int *mediaPacketDataStart, unsigned int *mediaPacketDataLength, int64_t startPosition)
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

    result = ((startPosition >= requestStart) && (startPosition <= requestEnd)) ? S_OK : E_INVALIDARG;

    if (result == S_OK)
    {
      int64_t mediaPacketStart = mediaPacket->GetStart();
      int64_t mediaPacketEnd = mediaPacket->GetEnd();

      this->logger->Log(LOGGER_DATA, L"%s: %s: async request start: %llu, end: %llu, start time: %llu", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, requestStart, requestEnd, startPosition);
      this->logger->Log(LOGGER_DATA, L"%s: %s: media packet start: %llu, end: %llu", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, mediaPacketStart, mediaPacketEnd);

      if (result == S_OK)
      {
        // check if start position is in media packet
        result = ((startPosition >= mediaPacketStart) && (startPosition <= mediaPacketEnd)) ? S_OK : E_INVALIDARG;

        if (result == S_OK)
        {
          // increase position end because position end is stamp of last byte in buffer
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

        int64_t tempMediaPacketDataStart = ((startPosition - mediaPacketStart) > 0) ? startPosition : mediaPacketStart;
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

DWORD WINAPI CLAVInputPin::AsyncRequestProcessWorker(LPVOID lpParam)
{
  CLAVInputPin *caller = (CLAVInputPin *)lpParam;
  caller->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME);

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

        if ((request->GetState() == CAsyncRequest::Waiting) || (request->GetState() == CAsyncRequest::WaitingIgnoreTimeout) || (request->GetState() == CAsyncRequest::Requested))
        {
          // process only waiting requests
          // variable to store found data length
          unsigned int foundDataLength = 0;
          HRESULT result = S_OK;
          // current stream position is get only when media packet for request is not found
          int64_t currentStreamPosition = -1;

          // first try to find starting media packet (packet which have first data)
          unsigned int packetIndex = UINT_MAX;
          {
            // lock access to media packets
            CLockMutex mediaPacketLock(caller->mediaPacketMutex, INFINITE);

            int64_t startPosition = request->GetStart();
            packetIndex = caller->mediaPacketCollection->GetMediaPacketIndexBetweenPositions(startPosition);            
            if (packetIndex != UINT_MAX)
            {
              while (packetIndex != UINT_MAX)
              {
                unsigned int mediaPacketDataStart = 0;
                unsigned int mediaPacketDataLength = 0;

                // get media packet
                CMediaPacket *mediaPacket = caller->mediaPacketCollection->GetItem(packetIndex);
                // check packet values against async request values
                result = caller->CheckValues(request, mediaPacket, &mediaPacketDataStart, &mediaPacketDataLength, startPosition);

                if (result == S_OK)
                {
                  // successfully checked values
                  int64_t positionStart = mediaPacket->GetStart();
                  int64_t positionEnd = mediaPacket->GetEnd();

                  // copy data from media packet to request buffer
                  caller->logger->Log(LOGGER_DATA, L"%s: %s: copy data from media packet '%u' to async request '%u', start: %u, data length: %u, request buffer position: %u, request buffer length: %lu", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, packetIndex, request->GetRequestId(), mediaPacketDataStart, mediaPacketDataLength, foundDataLength, request->GetBufferLength());
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
                          caller->logger->Log(LOGGER_ERROR, L"%s: %s: error occured while setting position: %lu", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, lastError);
                          error = true;
                        }
                      }

                      if (!error)
                      {
                        DWORD read = 0;
                        if (ReadFile(hTempFile, requestBuffer, mediaPacketDataLength, &read, NULL) == 0)
                        {
                          caller->logger->Log(LOGGER_ERROR, L"%s: %s: error occured reading file: %lu", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, GetLastError());
                        }
                        else if (read != mediaPacketDataLength)
                        {
                          caller->logger->Log(LOGGER_WARNING, L"%s: %s: readed data length not same as requested, requested: %u, readed: %u", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, mediaPacketDataLength, read);
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
                    startPosition = positionEnd + 1;
                    packetIndex = caller->mediaPacketCollection->GetMediaPacketIndexBetweenPositions(startPosition);
                    caller->logger->Log(LOGGER_DATA, L"%s: %s: next media packet '%u'", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, packetIndex);
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
                  else if (!caller->estimate)
                  {
                    // we are not receiving more data
                    // finish request
                    caller->logger->Log(LOGGER_DATA, L"%s: %s: request '%u' complete status: 0x%08X", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), S_FALSE);
                    request->SetBufferLength(foundDataLength);
                    // filters doesn't understand S_FALSE return code, so return S_OK
                    request->Complete(S_OK);
                  }
                }
                else if (foundDataLength == request->GetBufferLength())
                {
                  // found data length is equal than requested, return S_OK
                  caller->logger->Log(LOGGER_DATA, L"%s: %s: request '%u' complete status: 0x%08X", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), S_OK);
                  request->SetBufferLength(foundDataLength);
                  request->Complete(S_OK);
                }
                else
                {
                  caller->logger->Log(LOGGER_ERROR, L"%s: %s: request '%u' found data length '%u' bigger than requested '%lu'", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), foundDataLength, request->GetBufferLength());
                  request->Complete(E_OUTOFMEMORY);
                }
              }
              else
              {
                // some error occured
                // complete async request with error
                // set request is completed with result
                caller->logger->Log(LOGGER_WARNING, L"%s: %s: request '%u' complete status: 0x%08X", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), result);
                request->SetBufferLength(foundDataLength);
                request->Complete(result);
              }
            }

            if ((packetIndex == UINT_MAX) && (request->GetState() == CAsyncRequest::Waiting))
            {
              // get current stream position
              LONGLONG total = 0;
              HRESULT queryStreamProgressResult = caller->QueryStreamProgress(&total, &currentStreamPosition);
              if (FAILED(queryStreamProgressResult))
              {
                caller->logger->Log(LOGGER_WARNING, L"%s: %s: failed to get current stream position: 0x%08X", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, queryStreamProgressResult);
                currentStreamPosition = -1;
              }
            }
          }

          if ((packetIndex == UINT_MAX) && (request->GetState() == CAsyncRequest::Waiting))
          {
            // first check current stream position and request start
            // if request start is just next to current stream position then only wait for data and do not issue ranges request
            if (currentStreamPosition != (-1))
            {
              // current stream position has valid value
              if (request->GetStart() > currentStreamPosition)
              {
                // if request start is after current stream position than we have to issue ranges request (if supported)
                caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: request '%u', start '%llu' (size '%lu') after current stream position '%llu'", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), request->GetStart(), request->GetBufferLength(), currentStreamPosition);
              }
              else if ((request->GetStart() <= currentStreamPosition) && ((request->GetStart() + request->GetBufferLength()) > currentStreamPosition))
              {
                // current stream position is within current request
                // we are receiving data, do nothing, just wait for all data
                request->WaitAndIgnoreTimeout();
                caller->logger->Log(LOGGER_DATA, L"%s: %s: request '%u', start '%llu' (size '%lu') waiting for data and ignoring timeout, current stream position '%llu'", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), request->GetStart(), request->GetBufferLength(), currentStreamPosition);
              }
              else
              {
                // if request start is before current stream position than we have to issue ranges request
                caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: request '%u', start '%llu' (size '%lu') before current stream position '%llu'", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), request->GetStart(), request->GetBufferLength(), currentStreamPosition);
              }
            }

            if (request->GetState() == CAsyncRequest::Waiting)
            {
              // there isn't any packet containg some data for request
              // check if ranges are supported

              unsigned int seekingCapabilities = caller->GetSeekingCapabilities();
              if (seekingCapabilities & SEEKING_METHOD_POSITION)
              {
                if (SUCCEEDED(result))
                {
                  // not found start packet and request wasn't requested from filter yet
                  // first found start and end of request

                  int64_t requestStart = request->GetStart();
                  int64_t requestEnd = requestStart;

                  unsigned int startIndex = 0;
                  unsigned int endIndex = 0;
                  {
                    // lock access to media packets
                    CLockMutex mediaPacketLock(caller->mediaPacketMutex, INFINITE);

                    if (caller->mediaPacketCollection->GetItemInsertPosition(request->GetStart(), NULL, &startIndex, &endIndex))
                    {
                      // start and end index found successfully
                      if (startIndex == endIndex)
                      {
                        int64_t endPacketStartPosition = 0;
                        int64_t endPacketStopPosition = 0;
                        unsigned int mediaPacketIndex = caller->mediaPacketCollection->GetMediaPacketIndexBetweenPositions(endPacketStartPosition);

                        // media packet exists in collection
                        while (mediaPacketIndex != UINT_MAX)
                        {
                          CMediaPacket *mediaPacket = caller->mediaPacketCollection->GetItem(mediaPacketIndex);
                          int64_t mediaPacketStart = mediaPacket->GetStart();
                          int64_t mediaPacketEnd = mediaPacket->GetEnd();
                          if (endPacketStartPosition == mediaPacketStart)
                          {
                            // next start time is next to end of current media packet
                            endPacketStartPosition = mediaPacketEnd + 1;
                            mediaPacketIndex++;

                            if (mediaPacketIndex >= caller->mediaPacketCollection->Count())
                            {
                              // stop checking, all media packets checked
                              mediaPacketIndex = UINT_MAX;
                            }
                          }
                          else
                          {
                            endPacketStopPosition = mediaPacketStart - 1;
                            mediaPacketIndex = UINT_MAX;
                          }
                        }

                        requestEnd = endPacketStopPosition;
                      }
                      else if ((startIndex == (caller->mediaPacketCollection->Count() - 1)) && (endIndex == UINT_MAX))
                      {
                        // media packet belongs to end
                        // do nothing, default request is from specific point until end of stream
                      }
                      else if ((startIndex == UINT_MAX) && (endIndex == 0))
                      {
                        // media packet belongs to start
                        CMediaPacket *endMediaPacket = caller->mediaPacketCollection->GetItem(endIndex);
                        if (endMediaPacket != NULL)
                        {
                          // requests data from requestStart until end packet start position
                          requestEnd = endMediaPacket->GetStart() - 1;
                        }
                      }
                      else
                      {
                        // media packet belongs between packets startIndex and endIndex
                        CMediaPacket *endMediaPacket = caller->mediaPacketCollection->GetItem(endIndex);
                        if (endMediaPacket != NULL)
                        {
                          // requests data from requestStart until end packet start position
                          requestEnd = endMediaPacket->GetStart() - 1;
                        }
                      }
                    }
                  }

                  if (requestEnd < requestStart)
                  {
                    caller->logger->Log(LOGGER_WARNING, L"%s: %s: request '%u' has start '%llu' after end '%llu', modifying to equal", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), requestStart, requestEnd);
                    requestEnd = requestStart;
                  }

                  // request filter to receive data from request start to end
                  result = (caller->SeekToPosition(requestStart, requestEnd) >= 0) ? S_OK : E_FAIL;
                }

                if (SUCCEEDED(result))
                {
                  request->Request();
                }
                else
                {
                  // if error occured while requesting filter for data
                  caller->logger->Log(LOGGER_WARNING, L"%s: %s: request '%u' error while requesting data, complete status: 0x%08X", MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, request->GetRequestId(), result);
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
            TCHAR *guid = ConvertGuidToString(caller->logger->loggerInstance);
            ALLOC_MEM_DEFINE_SET(folder, TCHAR, MAX_PATH, 0);
            if ((guid != NULL) && (folder != NULL))
            {
              // get common application data folder
              if (SHGetSpecialFolderPath(NULL, folder, CSIDL_LOCAL_APPDATA, FALSE))
              {
                TCHAR *storeFolder = FormatString(L"%s\\MPUrlSourceSplitter\\", folder);
                wchar_t *unicodeStoreFolder = ConvertToUnicode(storeFolder);
                if ((storeFolder != NULL) && (unicodeStoreFolder != NULL))
                {
                  int error = SHCreateDirectory(NULL, unicodeStoreFolder);
                  if ((error == ERROR_SUCCESS) || (error == ERROR_FILE_EXISTS) || (error == ERROR_ALREADY_EXISTS))
                  {
                    // correct, directory exists
                    caller->storeFilePath = FormatString(L"%smpurlsourcesplitter_%s.temp", storeFolder, guid);
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
                caller->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, L"error while getting size");
                // error occured while getting file size
                size.QuadPart = -1;
              }

              if (size.QuadPart >= 0)
              {
                unsigned int i = 0;
                bool allMediaPacketsStored = true;
                while (i < caller->mediaPacketCollection->Count())
                {
                  CMediaPacket *mediaPacket = caller->mediaPacketCollection->GetItem(i);

                  if (!mediaPacket->IsStoredToFile())
                  {
                    // if media packet is not stored to file
                    // store it to file
                    int64_t mediaPacketStartPosition = mediaPacket->GetStart();
                    int64_t mediaPacketEndPosition = mediaPacket->GetEnd();
                    unsigned int length = (unsigned int)(mediaPacketEndPosition + 1 - mediaPacketStartPosition);

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
                        else
                        {
                          allMediaPacketsStored = false;
                        }
                      }
                      else
                      {
                        allMediaPacketsStored = false;
                        caller->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, L"not written");
                      }
                    }
                    else
                    {
                      allMediaPacketsStored = false;
                    }
                    FREE_MEM(buffer);
                  }

                  i++;
                }

                if (caller->downloadingFile && caller->downloadFinished && allMediaPacketsStored && (!caller->downloadCallbackCalled))
                {
                  // all data received
                  // call download callback method
                  caller->OnDownloadCallback(S_OK);
                  caller->downloadCallbackCalled = true;
                }
              }

              CloseHandle(hTempFile);
              hTempFile = INVALID_HANDLE_VALUE;
            }
            else
            {
              caller->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME, L"invalid file handle");
            }
          }
        }
      }
    }

    Sleep(1);
  }

  caller->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_ASYNC_REQUEST_PROCESS_WORKER_NAME);
  return S_OK;
}

STDMETHODIMP CLAVInputPin::SyncRead(int64_t position, LONG length, BYTE *buffer)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME);

  HRESULT result = S_OK;
  CHECK_CONDITION(result, length >= 0, S_OK, E_INVALIDARG);
  CHECK_POINTER_DEFAULT_HRESULT(result, buffer);

  if ((SUCCEEDED(result)) && (length > 0))
  {
    unsigned int requestId = 0;
    result = this->Request(&requestId, position, length, buffer, NULL);

    if (SUCCEEDED(result))
    {
      DWORD ticks = GetTickCount();
      DWORD timeout = this->GetReceiveDataTimeout();

      result = (timeout != UINT_MAX) ? S_OK : E_UNEXPECTED;

      if (SUCCEEDED(result))
      {
        // if ranges are not supported than we must wait for data

        result = VFW_E_TIMEOUT;
        this->logger->Log(LOGGER_DATA, L"%s: %s: requesting data from position: %llu, length: %lu", MODULE_NAME, METHOD_SYNC_READ_NAME, position, length);

        // wait until request is completed or cancelled
        while (!this->asyncRequestProcessingShouldExit)
        {
          unsigned int seekingCapabilities = this->GetSeekingCapabilities();

          CAsyncRequest *request = NULL;

          {
            // lock access to collection
            CLockMutex lock(this->requestMutex, INFINITE);
            request = this->requestsCollection->GetRequest(requestId);

            if (request != NULL)
            {

              if ((!this->estimate) && (request->GetStart() >= this->totalLength))
              {
                // something bad occured
                // graph requests data that are beyond stream (data doesn't exists)
                this->logger->Log(LOGGER_WARNING, L"%s: %s: graph requests data beyond stream, stream total length: %llu, request start: %llu", MODULE_NAME, METHOD_SYNC_READ_NAME, this->totalLength, request->GetStart());
                // complete result with error code
                request->Complete(E_FAIL);
              }

              if (request->GetState() == CAsyncRequest::Completed)
              {
                // request is completed
                result = request->GetErrorCode();
                this->logger->Log(LOGGER_DATA, L"%s: %s: returned data length: %lu, result: 0x%08X", MODULE_NAME, METHOD_SYNC_READ_NAME, request->GetBufferLength(), result);
                break;
              }
              else if (request->GetState() == CAsyncRequest::Cancelled)
              {
                // request is cancelled
                result = E_ABORT;
                break;
              }
              else if (request->GetState() == CAsyncRequest::WaitingIgnoreTimeout)
              {
                // we are waiting for data and we have to ignore timeout
              }
              else
              {
                // common case
                if ((seekingCapabilities != SEEKING_METHOD_NONE) && ((GetTickCount() - ticks) > timeout))
                {
                  // if ranges are supported and timeout occured then stop waiting for data and exit with VFW_E_TIMEOUT error
                  result = VFW_E_TIMEOUT;
                  break;
                }
              }
            }
            else
            {
              // request should not disappear before is processed
              result = E_FAIL;
              this->logger->Log(LOGGER_WARNING, L"%s: %s: request '%u' disappeared before processed", MODULE_NAME, METHOD_SYNC_READ_NAME, request->GetRequestId());
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
          this->logger->Log(LOGGER_WARNING, L"%s: %s: request '%u' cannot be removed", METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, requestId);
        }
      }

      if (FAILED(result))
      {
        this->logger->Log(LOGGER_WARNING, L"%s: %s: requesting data from position: %llu, length: %lu, request id: %u, result: 0x%08X", MODULE_NAME, METHOD_SYNC_READ_NAME, position, length, requestId, result);
      }
    }
  }

  this->logger->Log(LOGGER_DATA, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SYNC_READ_NAME, result);
  return result;
}

unsigned int CLAVInputPin::GetReceiveDataTimeout(void)
{
  unsigned int result = UINT_MAX;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->GetReceiveDataTimeout();
  }

  return result;
}

STDMETHODIMP CLAVInputPin::Length(LONGLONG *total, LONGLONG *available)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, MODULE_NAME, METHOD_LENGTH_NAME);
  CLockMutex lock(this->mediaPacketMutex, INFINITE);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, available);

  if (result == S_OK)
  {
    *total = this->totalLength;
    *available = this->totalLength;
    unsigned int mediaPacketCount = this->mediaPacketCollection->Count();

    CStreamAvailableLength *availableLength = new CStreamAvailableLength();
    result = this->QueryStreamAvailableLength(availableLength);
    if (result == S_OK)
    {
      result = availableLength->GetQueryResult();
    }

    if (result == S_OK)
    {
      *available = availableLength->GetAvailableLength();
    }
    
    if (result != S_OK)
    {
      // error occured while requesting stream available length
      this->logger->Log(LOGGER_VERBOSE, L"%s: %s: cannot query available stream length, result: 0x%08X", MODULE_NAME, METHOD_LENGTH_NAME, result);

      // return default value = last media packet end
      *available = 0;
      for (unsigned int i = 0; i < mediaPacketCount; i++)
      {
        CMediaPacket *mediaPacket = this->mediaPacketCollection->GetItem(i);
        int64_t mediaPacketStart = mediaPacket->GetStart();
        int64_t mediaPacketEnd = mediaPacket->GetEnd();

        if ((mediaPacketEnd + 1) > (*available))
        {
          *available = mediaPacketEnd + 1;
        }
      }

      result = S_OK;
    }

    result = (this->estimate) ? VFW_S_ESTIMATED : S_OK;
    this->logger->Log(LOGGER_VERBOSE, L"%s: %s: total length: %llu, available length: %llu, estimate: %u, media packets: %u", MODULE_NAME, METHOD_LENGTH_NAME, this->totalLength, *available, (this->estimate) ? 1 : 0, mediaPacketCount);
  }

  this->logger->Log(LOGGER_VERBOSE, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_LENGTH_NAME, result);
  return result;
}

HRESULT CLAVInputPin::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  HRESULT result = E_NOTIMPL;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->QueryStreamAvailableLength(availableLength);
  }

  return result;
}

int64_t CLAVInputPin::SeekToPosition(int64_t start, int64_t end)
{
  int64_t result = -1;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->SeekToPosition(start, end);
  }

  return result;
}

HRESULT CLAVInputPin::CreateDemuxerWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_DEMUXER_WORKER_NAME);

  this->demuxerWorkerShouldExit = false;

  this->hCreateDemuxerWorkerThread = CreateThread( 
    NULL,                                                 // default security attributes
    0,                                                    // use default stack size  
    &CLAVInputPin::DemuxerWorker,                         // thread function name
    this,                                                 // argument to thread function 
    0,                                                    // use default creation flags 
    &dwCreateDemuxerWorkerThreadId);                      // returns the thread identifier

  if (this->hCreateDemuxerWorkerThread == NULL)
  {
    // thread not created
    result = HRESULT_FROM_WIN32(GetLastError());
    this->logger->Log(LOGGER_ERROR, L"%s: %s: CreateThread() error: 0x%08X", MODULE_NAME, METHOD_CREATE_DEMUXER_WORKER_NAME, result);
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_DEMUXER_WORKER_NAME, result);
  return result;
}

HRESULT CLAVInputPin::DestroyDemuxerWorker(void)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTROY_DEMUXER_WORKER_NAME);

  this->demuxerWorkerShouldExit = true;

  // wait for the receive data worker thread to exit      
  if (this->hCreateDemuxerWorkerThread != NULL)
  {
    if (WaitForSingleObject(this->hCreateDemuxerWorkerThread, 1000) == WAIT_TIMEOUT)
    {
      // thread didn't exit, kill it now
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_DESTROY_DEMUXER_WORKER_NAME, L"thread didn't exit, terminating thread");
      TerminateThread(this->hCreateDemuxerWorkerThread, 0);
    }
  }

  this->hCreateDemuxerWorkerThread = NULL;
  this->demuxerWorkerShouldExit = false;

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_DESTROY_DEMUXER_WORKER_NAME, result);
  return result;
}

DWORD WINAPI CLAVInputPin::DemuxerWorker(LPVOID lpParam)
{
  CLAVInputPin *caller = (CLAVInputPin *)lpParam;
  caller->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DEMUXER_WORKER_NAME);

  while (!caller->demuxerWorkerShouldExit)
  {
    if (!caller->createdDemuxer)
    {
      caller->m_llBufferPosition = 0;
      if (SUCCEEDED(caller->filter->CreateDemuxer(caller->url)))
      {
        caller->createdDemuxer = true;
        break;
      }
      else
      {
        caller->ReleaseAVIOContext();
      }
    }

    Sleep(100);
  }

  caller->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DEMUXER_WORKER_NAME);
  return S_OK;
}

// IMediaSeeking
STDMETHODIMP CLAVInputPin::GetCapabilities(DWORD* pCapabilities)
{
  CheckPointer(pCapabilities, E_POINTER);

  *pCapabilities =
    AM_SEEKING_CanGetStopPos   |
    AM_SEEKING_CanGetDuration  |
    AM_SEEKING_CanSeekAbsolute |
    AM_SEEKING_CanSeekForwards |
    AM_SEEKING_CanSeekBackwards;

  return S_OK;
}

STDMETHODIMP CLAVInputPin::CheckCapabilities(DWORD* pCapabilities)
{
  CheckPointer(pCapabilities, E_POINTER);
  // capabilities is empty, all is good
  if(*pCapabilities == 0) return S_OK;
  // read caps
  DWORD caps;
  GetCapabilities(&caps);

  // Store the caps that we wanted
  DWORD wantCaps = *pCapabilities;
  // Update pCapabilities with what we have
  *pCapabilities = caps & wantCaps;

  // if nothing matches, its a disaster!
  if(*pCapabilities == 0) return E_FAIL;
  // if all matches, its all good
  if(*pCapabilities == wantCaps) return S_OK;
  // otherwise, a partial match
  return S_FALSE;
}

STDMETHODIMP CLAVInputPin::IsFormatSupported(const GUID* pFormat)
{
  return !pFormat ? E_POINTER : *pFormat == TIME_FORMAT_MEDIA_TIME ? S_OK : S_FALSE;
}

STDMETHODIMP CLAVInputPin::QueryPreferredFormat(GUID* pFormat)
{
  return this->GetTimeFormat(pFormat);
}

STDMETHODIMP CLAVInputPin::GetTimeFormat(GUID* pFormat)
{
  return pFormat ? *pFormat = TIME_FORMAT_MEDIA_TIME, S_OK : E_POINTER;
}

STDMETHODIMP CLAVInputPin::IsUsingTimeFormat(const GUID* pFormat)
{
  return this->IsFormatSupported(pFormat);
}

STDMETHODIMP CLAVInputPin::SetTimeFormat(const GUID* pFormat)
{
  return S_OK == this->IsFormatSupported(pFormat) ? S_OK : E_INVALIDARG;
}

STDMETHODIMP CLAVInputPin::GetDuration(LONGLONG* pDuration)
{
  CheckPointer(pDuration, E_POINTER);
  CBaseDemuxer *demuxer = this->filter->GetDemuxer();
  CheckPointer(demuxer, E_UNEXPECTED);
  
  *pDuration = demuxer->GetDuration();

  return (*pDuration < 0) ? E_FAIL : S_OK;
}

STDMETHODIMP CLAVInputPin::GetStopPosition(LONGLONG* pStop)
{
  return this->GetDuration(pStop);
}

STDMETHODIMP CLAVInputPin::GetCurrentPosition(LONGLONG* pCurrent)
{
  return E_NOTIMPL;
}

STDMETHODIMP CLAVInputPin::ConvertTimeFormat(LONGLONG* pTarget, const GUID* pTargetFormat, LONGLONG Source, const GUID* pSourceFormat)
{
  return E_NOTIMPL;
}

STDMETHODIMP CLAVInputPin::SetPositions(LONGLONG* pCurrent, DWORD dwCurrentFlags, LONGLONG* pStop, DWORD dwStopFlags)
{
  return this->SetPositionsInternal(this, pCurrent, dwCurrentFlags, pStop, dwStopFlags);
}

STDMETHODIMP CLAVInputPin::SetPositionsInternal(void *caller, LONGLONG* pCurrent, DWORD dwCurrentFlags, LONGLONG* pStop, DWORD dwStopFlags)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: seek request; this: %p; caller: %p, current: %I64d; start: %I64d; flags: 0x%08X, stop: %I64d; flags: 0x%08X", MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, this, caller, this->m_rtCurrent, pCurrent ? *pCurrent : -1, dwCurrentFlags, pStop ? *pStop : -1, dwStopFlags);

  CAutoLock cAutoLock(this);
  HRESULT result = E_FAIL;

  if ((pCurrent == NULL) && (pStop == NULL)
    || (((dwCurrentFlags & AM_SEEKING_PositioningBitsMask) == AM_SEEKING_NoPositioning)
      && ((dwStopFlags & AM_SEEKING_PositioningBitsMask) == AM_SEEKING_NoPositioning)))
  {
      result = S_OK;
  }
  else
  {
    REFERENCE_TIME rtCurrent = this->m_rtCurrent, rtStop = this->m_rtStop;

    if (pCurrent != NULL)
    {
      switch(dwCurrentFlags & AM_SEEKING_PositioningBitsMask)
      {
      case AM_SEEKING_NoPositioning:
        break;
      case AM_SEEKING_AbsolutePositioning:
        rtCurrent = *pCurrent;
        break;
      case AM_SEEKING_RelativePositioning:
        rtCurrent = rtCurrent + *pCurrent;
        break;
      case AM_SEEKING_IncrementalPositioning:
        rtCurrent = rtCurrent + *pCurrent;
        break;
      }
    }

    if (pStop != NULL)
    {
      switch(dwStopFlags & AM_SEEKING_PositioningBitsMask)
      {
      case AM_SEEKING_NoPositioning:
        break;
      case AM_SEEKING_AbsolutePositioning:
        rtStop = *pStop;
        this->m_bStopValid = TRUE;
        break;
      case AM_SEEKING_RelativePositioning:
        rtStop += *pStop;
        this->m_bStopValid = TRUE;
        break;
      case AM_SEEKING_IncrementalPositioning:
        rtStop = rtCurrent + *pStop;
        this->m_bStopValid = TRUE;
        break;
      }
    }

    if ((this->m_rtCurrent == rtCurrent) && (this->m_rtStop == rtStop))
    {
      result = S_OK;
    }
    else
    {
      if ((this->m_rtLastStart == rtCurrent) && (this->m_rtLastStop == rtStop) && (this->m_LastSeekers.find(caller) == this->m_LastSeekers.end()))
      {
        this->m_LastSeekers.insert(caller);
        result = S_OK;
      }
      else
      {
        this->m_rtLastStart = rtCurrent;
        this->m_rtLastStop = rtStop;
        this->m_LastSeekers.clear();
        this->m_LastSeekers.insert(caller);

        this->m_rtNewStart = this->m_rtCurrent = rtCurrent;
        this->m_rtNewStop = rtStop;

        // perform seek in CLAVSplitter::SetPositionsInternal()
        result = S_FALSE;
      }
    }
  }
  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, result);
  return result;
}

STDMETHODIMP CLAVInputPin::GetPositions(LONGLONG* pCurrent, LONGLONG* pStop)
{
  if (pCurrent)
  {
    *pCurrent = m_rtCurrent;
  }
  if (pStop)
  {
    *pStop = m_rtStop;
  }
  return S_OK;
}

STDMETHODIMP CLAVInputPin::GetAvailable(LONGLONG* pEarliest, LONGLONG* pLatest)
{
  if (pEarliest)
  {
    *pEarliest = 0;
  }
  return this->GetDuration(pLatest);
}

STDMETHODIMP CLAVInputPin::SetRate(double dRate)
{
  return dRate > 0 ? m_dRate = dRate, S_OK : E_INVALIDARG;
}

STDMETHODIMP CLAVInputPin::GetRate(double* pdRate)
{
  return pdRate ? *pdRate = m_dRate, S_OK : E_POINTER;
}

STDMETHODIMP CLAVInputPin::GetPreroll(LONGLONG* pllPreroll)
{
  return pllPreroll ? *pllPreroll = 0, S_OK : E_POINTER;
}

REFERENCE_TIME CLAVInputPin::GetStart(void) { return this->m_rtStart; }
REFERENCE_TIME CLAVInputPin::GetStop(void) { return this->m_rtStop; }
REFERENCE_TIME CLAVInputPin::GetCurrent(void) { return this->m_rtCurrent; }
REFERENCE_TIME CLAVInputPin::GetNewStart(void) { return this->m_rtNewStart; }
REFERENCE_TIME CLAVInputPin::GetNewStop(void) { return this->m_rtNewStop; }
double CLAVInputPin::GetPlayRate(void) { return this->m_dRate; }
BOOL CLAVInputPin::GetStopValid(void) { return this->m_bStopValid; }

void CLAVInputPin::SetStart(REFERENCE_TIME time) { this->m_rtStart = time; }
void CLAVInputPin::SetStop(REFERENCE_TIME time) { this->m_rtStop = time; }
void CLAVInputPin::SetCurrent(REFERENCE_TIME time)  { this->m_rtCurrent = time; }
void CLAVInputPin::SetNewStart(REFERENCE_TIME time)  { this->m_rtNewStart = time; }
void CLAVInputPin::SetNewStop(REFERENCE_TIME time)  { this->m_rtNewStop = time; }
void CLAVInputPin::SetPlayRate(double rate)  { this->m_dRate = rate; }
void CLAVInputPin::SetStopValid(BOOL valid) { this->m_bStopValid = valid; }

// IFilter interface

unsigned int CLAVInputPin::GetSeekingCapabilities(void)
{
  unsigned int capabilities = SEEKING_METHOD_NONE;

  if (this->activeProtocol != NULL)
  {
    capabilities = this->activeProtocol->GetSeekingCapabilities();
  }

  return capabilities;
}

CLogger *CLAVInputPin::GetLogger(void)
{
  return this->logger;
}

int64_t CLAVInputPin::SeekToTime(int64_t time)
{
  int64_t result = -1;

  if (this->activeProtocol != NULL)
  {
    result = this->activeProtocol->SeekToTime(time);
    if (result >= 0)
    {
      // lock access to media packets
      CLockMutex mediaPacketLock(this->mediaPacketMutex, INFINITE);
      // clear media packets, we are starting from beginning
      // delete buffer file and set buffer position to zero
      this->mediaPacketCollection->Clear();
      if (this->storeFilePath != NULL)
      {
        DeleteFile(this->storeFilePath);
      }
      this->m_llBufferPosition = 0;
    }
  }

  return result;
}

HRESULT CLAVInputPin::GetTotalLength(int64_t *totalLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, totalLength);

  if (SUCCEEDED(result))
  {
    int64_t available = 0;
    result = this->Length(totalLength, &available);
  }

  return result;
}