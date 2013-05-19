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
 *
 *  Initial design and concept by Gabest and the MPC-HC Team, copyright under GPLv2
 *  Contributions by Ti-BEN from the XBMC DSPlayer Project, also under GPLv2
 */

#include "stdafx.h"
#include "LAVSplitter.h"
#include "OutputPin.h"
#include "InputPin.h"
#include "VersionInfo.h"
#include "ErrorCodes.h"

#include "BaseDemuxer.h"
#include "LAVFDemuxer.h"
//#include "BDDemuxer.h"

#include <Shlwapi.h>
#include <string>
#include <regex>
#include <algorithm>

#include "registry.h"

#include "IGraphRebuildDelegate.h"

extern "C"
{
#include "config.h"
#include "..\..\ffmpeg\version.h"
#if CONFIG_AVFILTER
#include "libavfilter\version.h"
#include "libavfilter\avfilter.h"
#endif
#if CONFIG_SWSCALE
#include "libswscale\swscale.h"
#endif
#if CONFIG_AVDEVICE
#include "libavdevice\avdevice.h"
#endif
#if CONFIG_SWRESAMPLE
#include "libswresample\swresample.h"
#endif
#if CONFIG_POSTPROC
#include "libpostproc\postprocess.h"
#endif
}

#ifdef _DEBUG
#define MODULE_NAME                                               L"LAVSplitterd"
#else
#define MODULE_NAME                                               L"LAVSplitter"
#endif

#define METHOD_LOAD_NAME                                          L"Load()"
#define METHOD_CREATE_DEMUXER_NAME                                L"CreateDemuxer()"
#define METHOD_SET_POSITIONS_INTERNAL_NAME                        L"SetPositionsInternal()"

#define METHOD_STREAM_OPEN_NAME                                   L"stream_open()"
#define METHOD_STREAM_READ_NAME                                   L"stream_read()"
#define METHOD_STREAM_SEEK_NAME                                   L"stream_seek()"
#define METHOD_STREAM_CLOSE_NAME                                  L"stream_close()"
#define METHOD_STREAM_GET_FILE_HANDLE_NAME                        L"stream_get_file_handle()"
#define METHOD_STREAM_READ_PAUSE_NAME                             L"stream_read_pause()"
#define METHOD_STREAM_READ_SEEK_NAME                              L"stream_read_seek()"
#define METHOD_THREAD_PROC_NAME                                   L"ThreadProc()"

#define METHOD_STOP_NAME                                          L"Stop()"
#define METHOD_CLOSE_NAME                                         L"Close()"
#define METHOD_PAUSE_NAME                                         L"Pause()"
#define METHOD_RUN_NAME                                           L"Run()"

// if ffmpeg_log_callback_set is true than ffmpeg log callback will not be set
// in that case we don't receive messages from ffmpeg
static volatile bool ffmpeg_log_callback_set = false;
static CLogger ffmpeg_logger_instance(NULL);

#define SHOW_VERSION  2
#define SHOW_CONFIG   4

static int warned_cfg = 0;

#define GET_LIB_INFO(libInfo, libname, LIBNAME, flags)                                        \
{                                                                                             \
    libInfo = NULL;                                                                           \
    if (CONFIG_##LIBNAME)                                                                     \
    {                                                                                         \
        char *versionStringA = NULL;                                                          \
        char *configurationStringA = NULL;                                                    \
        if (flags & SHOW_VERSION)                                                             \
        {                                                                                     \
            unsigned int version = libname##_version();                                       \
            versionStringA = FormatStringA("lib%-11s %2d.%3d.%3d / %2d.%3d.%3d",              \
                   #libname,                                                                  \
                   LIB##LIBNAME##_VERSION_MAJOR,                                              \
                   LIB##LIBNAME##_VERSION_MINOR,                                              \
                   LIB##LIBNAME##_VERSION_MICRO,                                              \
                   version >> 16, version >> 8 & 0xff, version & 0xff);                       \
        }                                                                                     \
        if (flags & SHOW_CONFIG)                                                              \
        {                                                                                     \
            const char *cfg = libname##_configuration();                                      \
            if (strcmp(FFMPEG_CONFIGURATION, cfg))                                            \
            {                                                                                 \
                configurationStringA = FormatStringA("%-11s configuration: %s",               \
                        #libname, cfg);                                                       \
            }                                                                                 \
        }                                                                                     \
        wchar_t *versionStringW = ConvertToUnicodeA(versionStringA);                          \
        wchar_t *configurationStringW = ConvertToUnicodeA(configurationStringA);              \
        FREE_MEM(versionStringA);                                                             \
        FREE_MEM(configurationStringA);                                                       \
        if ((versionStringW == NULL) && (configurationStringW == NULL))                       \
        {                                                                                     \
            libInfo = NULL;                                                                   \
        }                                                                                     \
        else if (versionStringW == NULL)                                                      \
        {                                                                                     \
            libInfo = Duplicate(configurationStringW);                                        \
        }                                                                                     \
        else if (configurationStringW == NULL)                                                \
        {                                                                                     \
            libInfo = Duplicate(versionStringW);                                              \
        }                                                                                     \
        else                                                                                  \
        {                                                                                     \
            libInfo = FormatStringW(L"%s %s", versionStringW, configurationStringW);          \
        }                                                                                     \
        FREE_MEM(versionStringW);                                                             \
        FREE_MEM(configurationStringW);                                                       \
    }                                                                                         \
}                                                                                             \

CLAVSplitter::CLAVSplitter(LPUNKNOWN pUnk, HRESULT* phr) 
  : CBaseFilter(NAME("lavf dshow source filter"), pUnk, this,  __uuidof(this), phr)
  //, m_rtStart(0)
  //, m_rtStop(0)
  //, m_dRate(1.0)
  //, m_rtLastStart(_I64_MIN)
  //, m_rtLastStop(_I64_MIN)
  //, m_rtCurrent(0)
  , m_bPlaybackStarted(FALSE)
  , m_pDemuxer(NULL)
  , m_bRuntimeConfig(FALSE)
  , m_pSite(NULL)
  , m_bFakeASFReader(FALSE)
  //, m_bStopValid(FALSE)
  , m_rtOffset(0)
  , lastCommand(-1)
  , pauseSeekStopRequest(false)
{
  this->logger = new CLogger(NULL);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER, COMPILE_INFO_MPURLSOURCESPLITTER);
  this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, version);
  FREE_MEM(version);

  wchar_t *ffmpegVersion = ConvertToUnicodeA(FFMPEG_VERSION);
  this->logger->Log(LOGGER_INFO, L"%s: %s: FFMPEG version: %s", MODULE_NAME, METHOD_CONSTRUCTOR_NAME, ffmpegVersion);
  FREE_MEM(ffmpegVersion);
  
  wchar_t *result = NULL;

#if CONFIG_AVUTIL
  GET_LIB_INFO(result, avutil,   AVUTIL,   SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_AVCODEC
  GET_LIB_INFO(result, avcodec,  AVCODEC,  SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_AVFORMAT
  GET_LIB_INFO(result, avformat, AVFORMAT, SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_AVDEVICE
  GET_LIB_INFO(result, avdevice, AVDEVICE, SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_AVFILTER
  GET_LIB_INFO(result, avfilter, AVFILTER, SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_SWSCALE
  GET_LIB_INFO(result, swscale,  SWSCALE,  SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_SWRESAMPLE
  GET_LIB_INFO(result, swresample,SWRESAMPLE,  SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif
#if CONFIG_POSTPROC
  GET_LIB_INFO(result, postproc, POSTPROC, SHOW_VERSION);
  if (result != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME, result);
  }
  FREE_MEM(result);
#endif

  WCHAR fileName[1024];
  GetModuleFileName(NULL, fileName, 1024);
  m_processName = PathFindFileName (fileName);

  m_pInput = new CLAVInputPin(this->logger, NAME("LAV Input Pin"), this, this, phr);

  CLAVFDemuxer::ffmpeg_init();
  
  if (!ffmpeg_log_callback_set)
  {
    // callback for ffmpeg log is not set
    av_log_set_callback(ffmpeg_log_callback);
    av_log_set_level(AV_LOG_DEBUG);

    ffmpeg_log_callback_set = true;
  }

  m_InputFormats.clear();

  std::set<FormatInfo> lavf_formats = CLAVFDemuxer::GetFormatList();
  m_InputFormats.insert(lavf_formats.begin(), lavf_formats.end());

  LoadSettings();

#ifdef DEBUG
  DbgSetModuleLevel (LOG_TRACE, DWORD_MAX);
  DbgSetModuleLevel (LOG_ERROR, DWORD_MAX);

#if ENABLE_DEBUG_LOGFILE
  DbgSetLogFileDesktop(LAVF_LOG_FILE);
#endif
#endif

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CONSTRUCTOR_NAME);
}

CLAVSplitter::~CLAVSplitter()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);

  SAFE_DELETE(m_pInput);
  Close();

  // delete old pins
  std::vector<CLAVOutputPin *>::iterator it;
  for(it = m_pRetiredPins.begin(); it != m_pRetiredPins.end(); ++it) {
    delete (*it);
  }
  m_pRetiredPins.clear();

  SafeRelease(&m_pSite);

#if defined(DEBUG) && ENABLE_DEBUG_LOGFILE
  DbgCloseLogFile();
#endif

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_DESTRUCTOR_NAME);
  FREE_MEM_CLASS(this->logger);
}

STDMETHODIMP CLAVSplitter::Close()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CLOSE_NAME);

  CAutoLock cAutoLock(this);

  this->pauseSeekStopRequest = true;
  AbortOperation();
  CAMThread::CallWorker(CMD_EXIT);
  CAMThread::Close();
  this->pauseSeekStopRequest = false;

  m_State = State_Stopped;
  DeleteOutputs();

  SafeRelease(&m_pDemuxer);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_CLOSE_NAME);
  return S_OK;
}

// Default overrides for input formats
static BOOL get_iformat_default(std::string name)
{
  // Raw video formats lack timestamps..
  if (name == "rawvideo") {
    return FALSE;
  }

  return TRUE;
}

STDMETHODIMP CLAVSplitter::LoadDefaults()
{
  m_settings.prefAudioLangs   = L"";
  m_settings.prefSubLangs     = L"";
  m_settings.subtitleAdvanced = L"";

  m_settings.subtitleMode     = LAVSubtitleMode_Default;
  m_settings.PGSForcedStream  = TRUE;
  m_settings.PGSOnlyForced    = FALSE;

  m_settings.vc1Mode          = 2;
  m_settings.substreams       = TRUE;
  m_settings.videoParsing     = TRUE;

  m_settings.StreamSwitchRemoveAudio = FALSE;
  m_settings.ImpairedAudio    = FALSE;

  std::set<FormatInfo>::iterator it;
  for (it = m_InputFormats.begin(); it != m_InputFormats.end(); ++it) {
    m_settings.formats[std::string(it->strName)] = get_iformat_default(it->strName);
  }

  return S_OK;
}

STDMETHODIMP CLAVSplitter::LoadSettings()
{
  LoadDefaults();
  if (m_bRuntimeConfig)
    return S_FALSE;

  //HRESULT hr;
  //DWORD dwVal;
  //BOOL bFlag;

  //CreateRegistryKey(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY);
  //CRegistry reg = CRegistry(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY, hr);
  //// We don't check if opening succeeded, because the read functions will set their hr accordingly anyway,
  //// and we need to fill the settings with defaults.
  //// ReadString returns an empty string in case of failure, so thats fine!

  //// Language preferences
  //m_settings.prefAudioLangs = reg.ReadString(L"prefAudioLangs", hr);
  //m_settings.prefSubLangs = reg.ReadString(L"prefSubLangs", hr);
  //m_settings.subtitleAdvanced = reg.ReadString(L"subtitleAdvanced", hr);

  //// Subtitle mode, defaults to all subtitles
  //dwVal = reg.ReadDWORD(L"subtitleMode", hr);
  //if (SUCCEEDED(hr)) m_settings.subtitleMode = (LAVSubtitleMode)dwVal;

  //bFlag = reg.ReadBOOL(L"PGSForcedStream", hr);
  //if (SUCCEEDED(hr)) m_settings.PGSForcedStream = bFlag;

  //bFlag = reg.ReadBOOL(L"PGSOnlyForced", hr);
  //if (SUCCEEDED(hr)) m_settings.PGSOnlyForced = bFlag;

  //dwVal = reg.ReadDWORD(L"vc1TimestampMode", hr);
  //if (SUCCEEDED(hr)) m_settings.vc1Mode = dwVal;

  //bFlag = reg.ReadDWORD(L"substreams", hr);
  //if (SUCCEEDED(hr)) m_settings.substreams = bFlag;

  //bFlag = reg.ReadDWORD(L"videoParsing", hr);
  //if (SUCCEEDED(hr)) m_settings.videoParsing = bFlag;

  //bFlag = reg.ReadDWORD(L"StreamSwitchRemoveAudio", hr);
  //if (SUCCEEDED(hr)) m_settings.StreamSwitchRemoveAudio = bFlag;

  //CreateRegistryKey(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY_FORMATS);
  //CRegistry regF = CRegistry(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY_FORMATS, hr);

  //WCHAR wBuffer[80];
  //std::set<FormatInfo>::iterator it;
  //for (it = m_InputFormats.begin(); it != m_InputFormats.end(); ++it) {
  //  MultiByteToWideChar(CP_UTF8, 0, it->strName, -1, wBuffer, 80);
  //  bFlag = regF.ReadBOOL(wBuffer, hr);
  //  if (SUCCEEDED(hr)) m_settings.formats[std::string(it->strName)] = bFlag;
  //}

  return S_OK;
}

STDMETHODIMP CLAVSplitter::SaveSettings()
{
  if (m_bRuntimeConfig) {
    if (m_pDemuxer)
      m_pDemuxer->SettingsChanged(static_cast<ILAVFSettingsInternal *>(this));
    return S_FALSE;
  }

  /*HRESULT hr;
  CRegistry reg = CRegistry(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY, hr);
  if (SUCCEEDED(hr)) {
    reg.WriteString(L"prefAudioLangs", m_settings.prefAudioLangs.c_str());
    reg.WriteString(L"prefSubLangs", m_settings.prefSubLangs.c_str());
    reg.WriteString(L"subtitleAdvanced", m_settings.subtitleAdvanced.c_str());
    reg.WriteDWORD(L"subtitleMode", m_settings.subtitleMode);
    reg.WriteBOOL(L"PGSForcedStream", m_settings.PGSForcedStream);
    reg.WriteBOOL(L"PGSOnlyForced", m_settings.PGSOnlyForced);
    reg.WriteDWORD(L"vc1TimestampMode", m_settings.vc1Mode);
    reg.WriteBOOL(L"substreams", m_settings.substreams);
    reg.WriteBOOL(L"videoParsing", m_settings.videoParsing);
    reg.WriteBOOL(L"StreamSwitchRemoveAudio", m_settings.StreamSwitchRemoveAudio);
  }

  CRegistry regF = CRegistry(HKEY_CURRENT_USER, LAVF_REGISTRY_KEY_FORMATS, hr);
  if (SUCCEEDED(hr)) {
    WCHAR wBuffer[80];
    std::set<FormatInfo>::iterator it;
    for (it = m_InputFormats.begin(); it != m_InputFormats.end(); ++it) {
      MultiByteToWideChar(CP_UTF8, 0, it->strName, -1, wBuffer, 80);
      regF.WriteBOOL(wBuffer, m_settings.formats[std::string(it->strName)]);
    }
  }*/

  if (m_pDemuxer) {
    m_pDemuxer->SettingsChanged(static_cast<ILAVFSettingsInternal *>(this));
  }
  return S_OK;
}

STDMETHODIMP CLAVSplitter::NonDelegatingQueryInterface(REFIID riid, void** ppv)
{
  CheckPointer(ppv, E_POINTER);

  *ppv = NULL;

  if (m_pDemuxer && (riid == __uuidof(IKeyFrameInfo) || riid == __uuidof(ITrackInfo) || riid == IID_IAMExtendedSeeking)) {
    return m_pDemuxer->QueryInterface(riid, ppv);
  }

  return
    QI(IFileSourceFilter)
    QI(IMediaSeeking)
    QI(IAMStreamSelect)
    QI(IAMOpenProgress)
    QI(IDownload)
    //QI2(ISpecifyPropertyPages)
    //QI2(ILAVFSettings)
    //QI2(ILAVFSettingsInternal)
    QI(IObjectWithSite)
    QI(IBufferInfo)
    QI(IFilterState)
    __super::NonDelegatingQueryInterface(riid, ppv);
}

// ISpecifyPropertyPages
//STDMETHODIMP CLAVSplitter::GetPages(CAUUID *pPages)
//{
//  CheckPointer(pPages, E_POINTER);
//  pPages->cElems = 2;
//  pPages->pElems = (GUID *)CoTaskMemAlloc(sizeof(GUID) * pPages->cElems);
//  if (pPages->pElems == NULL) {
//    return E_OUTOFMEMORY;
//  }
//  pPages->pElems[0] = CLSID_LAVSplitterSettingsProp;
//  pPages->pElems[1] = CLSID_LAVSplitterFormatsProp;
//  return S_OK;
//}

// IObjectWithSite
STDMETHODIMP CLAVSplitter::SetSite(IUnknown *pUnkSite)
{
  // AddRef to store it for later
  pUnkSite->AddRef();

  // Release the old one
  SafeRelease(&m_pSite);

  // Store the new one
  m_pSite = pUnkSite;

  return S_OK;
}

STDMETHODIMP CLAVSplitter::GetSite(REFIID riid, void **ppvSite)
{
  CheckPointer(ppvSite, E_POINTER);
  *ppvSite = NULL;
  if (!m_pSite) {
    return E_FAIL;
  }

  IUnknown *pSite = NULL;
  HRESULT hr = m_pSite->QueryInterface(riid, (void **)&pSite);
  if (SUCCEEDED(hr) && pSite) {
    pSite->AddRef();
    *ppvSite = pSite;
    return S_OK;
  }
  return E_NOINTERFACE;
}

// IBufferInfo
STDMETHODIMP_(int) CLAVSplitter::GetCount()
{
  CAutoLock pinLock(&m_csPins);
  return (int)m_pPins.size();
}

STDMETHODIMP CLAVSplitter::GetStatus(int i, int& samples, int& size)
{
  CAutoLock pinLock(&m_csPins);
  if ((size_t)i >= m_pPins.size())
    return E_FAIL;

  CLAVOutputPin *pPin = m_pPins.at(i);
  if (!pPin)
    return E_FAIL;
  return pPin->GetQueueSize(samples, size);
}

STDMETHODIMP_(DWORD) CLAVSplitter::GetPriority()
{
  return 0;
}

// CBaseSplitter
int CLAVSplitter::GetPinCount()
{
  CAutoLock lock(&m_csPins);

  int count = (int)m_pPins.size();
  /*if (m_pInput)
    count++;*/

  return count;
}

CBasePin *CLAVSplitter::GetPin(int n)
{
  CAutoLock lock(&m_csPins);

  if (n < 0 ||n >= GetPinCount()) return NULL;

  /*if (m_pInput) {
    if(n == 0)
      return m_pInput;
    else
      n--;
  }*/

  return m_pPins[n];
}

STDMETHODIMP CLAVSplitter::GetClassID(CLSID* pClsID)
{
  CheckPointer (pClsID, E_POINTER);

  if (m_bFakeASFReader) {
    *pClsID = CLSID_WMAsfReader;
    return S_OK;
  } else {
    return __super::GetClassID(pClsID);
  }
}

STDMETHODIMP CLAVSplitter::GetState(DWORD dwMSecs, __out FILTER_STATE *State)
{
  CheckPointer (State, E_POINTER);

  HRESULT result = __super::GetState(dwMSecs, State);
  result = (SUCCEEDED(result) && (m_State == State_Paused)) ? VFW_S_CANT_CUE : result;

  return result;
}

CLAVOutputPin *CLAVSplitter::GetOutputPin(DWORD streamId, BOOL bActiveOnly)
{
  CAutoLock lock(&m_csPins);

  std::vector<CLAVOutputPin *> &vec = bActiveOnly ? m_pActivePins : m_pPins;

  std::vector<CLAVOutputPin *>::iterator it;
  for(it = vec.begin(); it != vec.end(); ++it) {
    if ((*it)->GetStreamId() == streamId) {
      return *it;
    }
  }
  return NULL;
}

STDMETHODIMP CLAVSplitter::CreateDemuxer(const wchar_t *pszFileName)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_CREATE_DEMUXER_NAME);
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, pszFileName);

  if (SUCCEEDED(result))
  {
    m_bPlaybackStarted = FALSE;

    SAFE_DELETE(m_pDemuxer);
    CLAVFDemuxer *pDemux = new CLAVFDemuxer(this, this, this);

    AVIOContext *pContext = m_pInput->GetAVIOContext();
    result = (pContext != NULL) ? S_OK : E_OUTOFMEMORY;

    if (FAILED(result))
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_CREATE_DEMUXER_NAME, L"GetAVIOContext() returned error");
    }

    if (SUCCEEDED(result))
    {
      result = pDemux->OpenInputStream(pContext, pszFileName);

      if (SUCCEEDED(result))
      {
        m_pDemuxer = pDemux;
        m_pDemuxer->AddRef();

        result = InitDemuxer();
      }
      else
      {
        this->logger->Log(LOGGER_ERROR, L"%s: %s; OpenInputStream() returned error: 0x%08X", MODULE_NAME, METHOD_CREATE_DEMUXER_NAME, result);
        SAFE_DELETE(pDemux);
      }
    }
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_CREATE_DEMUXER_NAME, result);
  return result;
}

// IFileSourceFilter
STDMETHODIMP CLAVSplitter::Load(LPCOLESTR pszFileName, const AM_MEDIA_TYPE * pmt)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_LOAD_NAME);
  HRESULT result = S_OK;

  CHECK_POINTER_DEFAULT_HRESULT(result, pszFileName);
  m_bPlaybackStarted = FALSE;

  if (SUCCEEDED(result))
  {
    result = m_pInput->Load(pszFileName, pmt);
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_LOAD_NAME, result);
  return result;
}

// Get the currently loaded file
STDMETHODIMP CLAVSplitter::GetCurFile(LPOLESTR *ppszFileName, AM_MEDIA_TYPE *pmt)
{
  return (this->m_pInput == NULL) ? E_NOT_VALID_STATE : m_pInput->GetCurFile(ppszFileName, pmt);
}

STDMETHODIMP CLAVSplitter::InitDemuxer()
{
  HRESULT hr = S_OK;

  // Disable subtitles in applications known to fail with them (explorer thumbnail generator, power point, basically all applications using MCI)
  bool bNoSubtitles = _wcsicmp(m_processName.c_str(), L"dllhost.exe") == 0 || _wcsicmp(m_processName.c_str(), L"explorer.exe") == 0 || _wcsicmp(m_processName.c_str(), L"powerpnt.exe") == 0 || _wcsicmp(m_processName.c_str(), L"pptview.exe") == 0;

  //m_rtStart = m_rtNewStart = m_rtCurrent = 0;
  this->m_pInput->SetCurrent(0);
  this->m_pInput->SetNewStart(0);
  this->m_pInput->SetStart(0);
  //m_rtStop = m_rtNewStop = m_pDemuxer->GetDuration();
  this->m_pInput->SetNewStop(m_pDemuxer->GetDuration());
  this->m_pInput->SetStop(this->m_pInput->GetNewStop());

  m_bMPEGTS = strcmp(m_pDemuxer->GetContainerFormat(), "mpegts") == 0;
  m_bMPEGPS = strcmp(m_pDemuxer->GetContainerFormat(), "mpeg") == 0;

  const CBaseDemuxer::stream *videoStream = m_pDemuxer->SelectVideoStream();
  if (videoStream) {
    CLAVOutputPin* pPin = new CLAVOutputPin(videoStream->streamInfo->mtypes, CBaseDemuxer::CStreamList::ToStringW(CBaseDemuxer::video), this, this, &hr, CBaseDemuxer::video, m_pDemuxer->GetContainerFormat());
    if(SUCCEEDED(hr)) {
      pPin->SetStreamId(videoStream->pid);
      m_pPins.push_back(pPin);
      m_pDemuxer->SetActiveStream(CBaseDemuxer::video, videoStream->pid);
    } else {
      delete pPin;
    }
  }

  std::list<std::string> audioLangs = GetPreferredAudioLanguageList();
  const CBaseDemuxer::stream *audioStream = m_pDemuxer->SelectAudioStream(audioLangs);
  if (audioStream) {
    CLAVOutputPin* pPin = new CLAVOutputPin(audioStream->streamInfo->mtypes, CBaseDemuxer::CStreamList::ToStringW(CBaseDemuxer::audio), this, this, &hr, CBaseDemuxer::audio, m_pDemuxer->GetContainerFormat());
    if(SUCCEEDED(hr)) {
      pPin->SetStreamId(audioStream->pid);
      m_pPins.push_back(pPin);
      m_pDemuxer->SetActiveStream(CBaseDemuxer::audio, audioStream->pid);
    } else {
      delete pPin;
    }
  }

  std::string audioLanguage = audioStream ? audioStream->language : std::string();

  std::list<CSubtitleSelector> subtitleSelectors = GetSubtitleSelectors();
  const CBaseDemuxer::stream *subtitleStream = m_pDemuxer->SelectSubtitleStream(subtitleSelectors, audioLanguage);
  if (subtitleStream && !bNoSubtitles) {
    CLAVOutputPin* pPin = new CLAVOutputPin(subtitleStream->streamInfo->mtypes, CBaseDemuxer::CStreamList::ToStringW(CBaseDemuxer::subpic), this, this, &hr, CBaseDemuxer::subpic, m_pDemuxer->GetContainerFormat());
    if(SUCCEEDED(hr)) {
      pPin->SetStreamId(subtitleStream->pid);
      m_pPins.push_back(pPin);
      m_pDemuxer->SetActiveStream(CBaseDemuxer::subpic, subtitleStream->pid);
    } else {
      delete pPin;
    }
  }

  if(SUCCEEDED(hr)) {
    // If there are no pins, what good are we?
    return !m_pPins.empty() ? S_OK : E_FAIL;
  } else {
    return hr;
  }
}

STDMETHODIMP CLAVSplitter::DeleteOutputs()
{
  CAutoLock lock(this);
  if(m_State != State_Stopped) return VFW_E_NOT_STOPPED;

  CAutoLock pinLock(&m_csPins);
  // Release pins
  std::vector<CLAVOutputPin *>::iterator it;
  for(it = m_pPins.begin(); it != m_pPins.end(); ++it) {
    if(IPin* pPinTo = (*it)->GetConnected()) pPinTo->Disconnect();
    (*it)->Disconnect();
    m_pRetiredPins.push_back(*it);
  }
  m_pPins.clear();

  return S_OK;
}

bool CLAVSplitter::IsAnyPinDrying()
{
  // MPC changes thread priority here
  // TODO: Investigate if that is needed
  std::vector<CLAVOutputPin *>::iterator it;
  for(it = m_pActivePins.begin(); it != m_pActivePins.end(); ++it) {
    if((*it)->IsConnected() && !(*it)->IsDiscontinuous() && (*it)->QueueCount() < (*it)->GetQueueLowLimit()) {
      return true;
    }
  }
  return false;
}

// Worker Thread
DWORD CLAVSplitter::ThreadProc()
{
  std::vector<CLAVOutputPin *>::iterator pinIter;

  CheckPointer(m_pDemuxer, 0);

  SetThreadName(-1, "CLAVSplitter Demux");

  m_fFlushing = false;
  m_eEndFlush.Set();
  // last command is no command
  this->lastCommand = -1;

  for(DWORD cmd = (DWORD)-1; ; cmd = GetRequest())
  {
    this->lastCommand = cmd;
    switch (cmd)
    {
    case CMD_EXIT:
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_THREAD_PROC_NAME, L"CMD_EXIT");
      break;
    case CMD_PAUSE:
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_THREAD_PROC_NAME, L"CMD_PAUSE");
      break;
    case CMD_SEEK:
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_THREAD_PROC_NAME, L"CMD_SEEK");
      break;
    case CMD_PLAY:
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_THREAD_PROC_NAME, L"CMD_PLAY");
      break;
    case (DWORD)-1:
      // ignore, it means no command
      this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, MODULE_NAME, METHOD_THREAD_PROC_NAME, L"no command");
      break;
    default:
      this->logger->Log(LOGGER_INFO, L"%s: %s: unknown command: %d", MODULE_NAME, METHOD_THREAD_PROC_NAME, cmd);
      break;
    }

    if(cmd == CMD_EXIT)
    {
      Reply(S_OK);
      break;
    }

    if ((cmd != CMD_PAUSE) && (cmd != CMD_PLAY))
    {
      SetThreadPriority(m_hThread, THREAD_PRIORITY_BELOW_NORMAL);

      //m_rtStart = m_rtNewStart;
      this->m_pInput->SetStart(this->m_pInput->GetNewStart());
      //m_rtStop = m_rtNewStop;
      this->m_pInput->SetStop(this->m_pInput->GetNewStop());

      //if(m_bPlaybackStarted || m_rtStart != 0 || cmd == CMD_SEEK)
      if(m_bPlaybackStarted || this->m_pInput->GetStart() != 0 || cmd == CMD_SEEK)
        //DemuxSeek(m_rtStart);
        DemuxSeek(this->m_pInput->GetStart());

      if(cmd != (DWORD)-1)
      {
        Reply(S_OK);
      }

      // Wait for the end of any flush
      m_eEndFlush.Wait();

      m_pActivePins.clear();

      for(pinIter = m_pPins.begin(); pinIter != m_pPins.end() && !m_fFlushing; ++pinIter) {
        if ((*pinIter)->IsConnected()) {
          //(*pinIter)->DeliverNewSegment(m_rtStart, m_rtStop, m_dRate);
          (*pinIter)->DeliverNewSegment(this->m_pInput->GetStart(), this->m_pInput->GetStop(), this->m_pInput->GetPlayRate());
          m_pActivePins.push_back(*pinIter);
        }
      }
      m_rtOffset = 0;

      m_bDiscontinuitySent.clear();

      m_bPlaybackStarted = TRUE;
    }
    else
    {
      if(cmd != (DWORD)-1)
      {
        Reply(S_OK);
      }
    }

    HRESULT hr = S_OK;
    while(SUCCEEDED(hr) && !CheckRequest(&cmd))
    {
      if ((cmd == CMD_PAUSE) || (cmd == CMD_SEEK) || (this->pauseSeekStopRequest))
      {
        hr = S_OK;
        Sleep(1);
      }
      else
      {
        hr = DemuxNextPacket();
      }
    }

    // If we didnt exit by request, deliver end-of-stream
    if(!CheckRequest(&cmd)) {
      for(pinIter = m_pActivePins.begin(); pinIter != m_pActivePins.end(); ++pinIter) {
        (*pinIter)->QueueEndOfStream();
      }
    }
  }

  return 0;
}

// Seek to the specified time stamp
// Based on DVDDemuxFFMPEG
HRESULT CLAVSplitter::DemuxSeek(REFERENCE_TIME rtStart)
{
  if(rtStart < 0) { rtStart = 0; }
  
  return m_pDemuxer->Seek(rtStart);
}

// Demux the next packet and deliver it to the output pins
// Based on DVDDemuxFFMPEG
HRESULT CLAVSplitter::DemuxNextPacket()
{
  Packet *pPacket;
  HRESULT hr = S_OK;
  hr = m_pDemuxer->GetNextPacket(&pPacket);

  // Only S_OK indicates we have a proper packet
  // S_FALSE is a "soft error", don't deliver the packet
  if (hr != S_OK) {
    return hr;
  }

  return DeliverPacket(pPacket);
}

HRESULT CLAVSplitter::DeliverPacket(Packet *pPacket)
{
  HRESULT hr = S_FALSE;

  if (pPacket->dwFlags & LAV_PACKET_FORCED_SUBTITLE)
    pPacket->StreamId = FORCED_SUBTITLE_PID;

  CLAVOutputPin* pPin = GetOutputPin(pPacket->StreamId, TRUE);

  if(!pPin || !pPin->IsConnected()) {
    delete pPacket;
    return S_FALSE;
  }

  if(pPacket->rtStart != Packet::INVALID_TIME) {
    //m_rtCurrent = pPacket->rtStart;
    this->m_pInput->SetCurrent(pPacket->rtStart);

    //if (m_bStopValid && m_rtStop && pPacket->rtStart > m_rtStop) {
    if (this->m_pInput->GetStopValid() && this->m_pInput->GetStop() && pPacket->rtStart > this->m_pInput->GetStop()) {
      //DbgLog((LOG_TRACE, 10, L"::DeliverPacket(): Reached the designated stop time of %I64d at %I64d", m_rtStop, pPacket->rtStart));
      //DbgLog((LOG_TRACE, 10, L"::DeliverPacket(): Reached the designated stop time of %I64d at %I64d", this->m_pInput->GetStop(), pPacket->rtStart));
      delete pPacket;
      return E_FAIL;
    }

    //pPacket->rtStart -= m_rtStart;
    pPacket->rtStart -= this->m_pInput->GetStart();
    //pPacket->rtStop -= m_rtStart;
    pPacket->rtStop -= this->m_pInput->GetStart();

    ASSERT(pPacket->rtStart <= pPacket->rtStop);

    // Filter PTS values
    // This will try to compensate for timestamp discontinuities in the stream
    if (m_bMPEGTS || m_bMPEGPS) {
      if (pPin->m_rtPrev != Packet::INVALID_TIME && !pPin->IsSubtitlePin()) {
        REFERENCE_TIME rt = pPacket->rtStart + m_rtOffset;
        if(_abs64(rt - pPin->m_rtPrev) > MAX_PTS_SHIFT) {
          m_rtOffset += pPin->m_rtPrev - rt;
          DbgLog((LOG_TRACE, 10, L"::DeliverPacket(): MPEG-TS/PS discontinuity detected, adjusting offset to %I64d", m_rtOffset));
        }
      }
      pPacket->rtStart += m_rtOffset;
      pPacket->rtStop += m_rtOffset;

      pPin->m_rtPrev = pPacket->rtStart;
    }

    pPacket->rtStart = (REFERENCE_TIME)(pPacket->rtStart / this->m_pInput->GetPlayRate());
    pPacket->rtStop = (REFERENCE_TIME)(pPacket->rtStop / this->m_pInput->GetPlayRate());
  }

  if(m_bDiscontinuitySent.find(pPacket->StreamId) == m_bDiscontinuitySent.end()) {
    pPacket->bDiscontinuity = TRUE;
  }

  BOOL bDiscontinuity = pPacket->bDiscontinuity; 
  DWORD streamId = pPacket->StreamId;

  hr = pPin->QueuePacket(pPacket);
  if (hr != S_OK) {
    // Find a iterator pointing to the pin
    std::vector<CLAVOutputPin *>::iterator it = std::find(m_pActivePins.begin(), m_pActivePins.end(), pPin);
    // Remove it from the vector
    m_pActivePins.erase(it);

    // Fail if no active pins remain, otherwise resume demuxing
    return m_pActivePins.empty() ? E_FAIL : S_OK;
  }

  if(bDiscontinuity) {
    m_bDiscontinuitySent.insert(streamId);
  }

  return hr;
}

// State Control
STDMETHODIMP CLAVSplitter::Stop()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_STOP_NAME);

  this->pauseSeekStopRequest = true;
  CAMThread::CallWorker(CMD_EXIT);
  this->pauseSeekStopRequest = false;

  CAutoLock cAutoLock(this);

  DeliverBeginFlush();
  CAMThread::Close();
  DeliverEndFlush();

  HRESULT hr;
  if(FAILED(hr = __super::Stop())) {
    this->logger->Log(LOGGER_ERROR, METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_STOP_NAME, hr);
    return hr;
  }

  return S_OK;
}

STDMETHODIMP CLAVSplitter::Pause()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_PAUSE_NAME);

  this->pauseSeekStopRequest = true;
  CAMThread::CallWorker(CMD_PAUSE);
  this->pauseSeekStopRequest = false;

  CAutoLock cAutoLock(this);

  FILTER_STATE fs = m_State;

  HRESULT hr;
  if(FAILED(hr = __super::Pause())) {
    this->logger->Log(LOGGER_ERROR, METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_PAUSE_NAME, hr);
    return hr;
  }

  // The filter graph will set us to pause before running
  // So if we were stopped before, create the thread
  // Note that the splitter will always be running,
  // and even in pause mode fill up the buffers
  if(fs == State_Stopped) {
    // At this point, the graph is hopefully finished, tell the demuxer about all the cool things
    m_pDemuxer->SettingsChanged(static_cast<ILAVFSettingsInternal *>(this));

    // Create demuxing thread
    Create();
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_PAUSE_NAME);
  return S_OK;
}

STDMETHODIMP CLAVSplitter::Run(REFERENCE_TIME tStart)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_RUN_NAME);

  //this->Pause();

  CAutoLock cAutoLock(this);

  HRESULT hr;
  if(FAILED(hr = __super::Run(tStart))) {
    this->logger->Log(LOGGER_ERROR, METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_RUN_NAME, hr);
    return hr;
  }

  CAMThread::CallWorker(CMD_PLAY);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, MODULE_NAME, METHOD_RUN_NAME);
  return S_OK;
}

// Flushing
void CLAVSplitter::DeliverBeginFlush()
{
  m_fFlushing = true;

  // flush all pins
  std::vector<CLAVOutputPin *>::iterator it;
  for(it = m_pPins.begin(); it != m_pPins.end(); ++it) {
    (*it)->DeliverBeginFlush();
  }
}

void CLAVSplitter::DeliverEndFlush()
{
  // flush all pins
  std::vector<CLAVOutputPin *>::iterator it;
  for(it = m_pPins.begin(); it != m_pPins.end(); ++it) {
    (*it)->DeliverEndFlush();
  }

  m_fFlushing = false;
  m_eEndFlush.Set();
}

// IMediaSeeking
STDMETHODIMP CLAVSplitter::GetCapabilities(DWORD* pCapabilities)
{
  /*CheckPointer(pCapabilities, E_POINTER);

  *pCapabilities =
    AM_SEEKING_CanGetStopPos   |
    AM_SEEKING_CanGetDuration  |
    AM_SEEKING_CanSeekAbsolute |
    AM_SEEKING_CanSeekForwards |
    AM_SEEKING_CanSeekBackwards;

  return S_OK;*/

  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetCapabilities(pCapabilities);
}

STDMETHODIMP CLAVSplitter::CheckCapabilities(DWORD* pCapabilities)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->CheckCapabilities(pCapabilities);
}

STDMETHODIMP CLAVSplitter::IsFormatSupported(const GUID* pFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->IsFormatSupported(pFormat);
}

STDMETHODIMP CLAVSplitter::QueryPreferredFormat(GUID* pFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->QueryPreferredFormat(pFormat);
}

STDMETHODIMP CLAVSplitter::GetTimeFormat(GUID* pFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetTimeFormat(pFormat);
}

STDMETHODIMP CLAVSplitter::IsUsingTimeFormat(const GUID* pFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->IsUsingTimeFormat(pFormat);
}

STDMETHODIMP CLAVSplitter::SetTimeFormat(const GUID* pFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->SetTimeFormat(pFormat);
}

STDMETHODIMP CLAVSplitter::GetDuration(LONGLONG* pDuration)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetDuration(pDuration);
}

STDMETHODIMP CLAVSplitter::GetStopPosition(LONGLONG* pStop)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetStopPosition(pStop);
}

STDMETHODIMP CLAVSplitter::GetCurrentPosition(LONGLONG* pCurrent)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetCurrentPosition(pCurrent);
}

STDMETHODIMP CLAVSplitter::ConvertTimeFormat(LONGLONG* pTarget, const GUID* pTargetFormat, LONGLONG Source, const GUID* pSourceFormat)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->ConvertTimeFormat(pTarget, pTargetFormat, Source, pSourceFormat);
}

STDMETHODIMP CLAVSplitter::SetPositions(LONGLONG* pCurrent, DWORD dwCurrentFlags, LONGLONG* pStop, DWORD dwStopFlags)
{
  return SetPositionsInternal(this, pCurrent, dwCurrentFlags, pStop, dwStopFlags);
}

STDMETHODIMP CLAVSplitter::SetPositionsInternal(void *caller, LONGLONG* pCurrent, DWORD dwCurrentFlags, LONGLONG* pStop, DWORD dwStopFlags)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: seek request; this: %p; caller: %p, start: %I64d; flags: 0x%08X, stop: %I64d; flags: 0x%08X", MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, this, caller, pCurrent ? *pCurrent : -1, dwCurrentFlags, pStop ? *pStop : -1, dwStopFlags);

  HRESULT result = this->m_pInput->SetPositionsInternal(caller, pCurrent, dwCurrentFlags, pStop, dwStopFlags);

  if (result == S_FALSE)
  {
    result = S_OK;
    this->logger->Log(LOGGER_VERBOSE, L"%s: %s: performing seek to %I64d", MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, this->m_pInput->GetNewStart());

    if (ThreadExists())
    {
      DeliverBeginFlush();
      this->pauseSeekStopRequest = true;
      CallWorker(CMD_SEEK);
      this->pauseSeekStopRequest = false;
      DeliverEndFlush();
    }

    this->logger->Log(LOGGER_VERBOSE, L"%s: %s: seek to %I64d finished", MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, this->m_pInput->GetNewStart());
  }

  this->logger->Log(LOGGER_INFO, (SUCCEEDED(result)) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, MODULE_NAME, METHOD_SET_POSITIONS_INTERNAL_NAME, result);
  return result;
}

STDMETHODIMP CLAVSplitter::GetPositions(LONGLONG* pCurrent, LONGLONG* pStop)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetPositions(pCurrent, pStop);
}

STDMETHODIMP CLAVSplitter::GetAvailable(LONGLONG* pEarliest, LONGLONG* pLatest)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetAvailable(pEarliest, pLatest);
}

STDMETHODIMP CLAVSplitter::SetRate(double dRate)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->SetRate(dRate);
}

STDMETHODIMP CLAVSplitter::GetRate(double* pdRate)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetRate(pdRate);
}

STDMETHODIMP CLAVSplitter::GetPreroll(LONGLONG* pllPreroll)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetPreroll(pllPreroll);
}

STDMETHODIMP CLAVSplitter::UpdateForcedSubtitleMediaType()
{
  CheckPointer(m_pDemuxer, E_UNEXPECTED);

  CLAVOutputPin* pPin = GetOutputPin(FORCED_SUBTITLE_PID);
  if (pPin) {
    CBaseDemuxer::CStreamList *streams = m_pDemuxer->GetStreams(CBaseDemuxer::subpic);
    const CBaseDemuxer::stream *s = streams->FindStream(FORCED_SUBTITLE_PID);
    CMediaType *mt = new CMediaType(s->streamInfo->mtypes.back());
    pPin->SendMediaType(mt);
  }

  return S_OK;
}

static int QueryAcceptMediaTypes(IPin *pPin, std::vector<CMediaType> pmts)
{
  for(unsigned int i = 0; i < pmts.size(); i++) {
    if (S_OK == pPin->QueryAccept(&pmts[i])) {
      DbgLog((LOG_TRACE, 20, L"QueryAcceptMediaTypes() - IPin:QueryAccept succeeded on index %d", i));
      return i;
    }
  }
  return -1;
}

STDMETHODIMP CLAVSplitter::RenameOutputPin(DWORD TrackNumSrc, DWORD TrackNumDst, std::vector<CMediaType> pmts)
{
  CheckPointer(m_pDemuxer, E_UNEXPECTED);

#ifndef DEBUG
  if (TrackNumSrc == TrackNumDst) return S_OK;
#endif

  CLAVOutputPin* pPin = GetOutputPin(TrackNumSrc);

  DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - Switching %s Stream %d to %d", CBaseDemuxer::CStreamList::ToStringW(pPin->GetPinType()), TrackNumSrc, TrackNumDst));
  // Output Pin was found
  // Stop the Graph, remove the old filter, render the graph again, start it up again
  // This only works on pins that were connected before, or the filter graph could .. well, break
  if (pPin && pPin->IsConnected()) {
    HRESULT hr = S_OK;

    IMediaControl *pControl = NULL;
    hr = m_pGraph->QueryInterface(IID_IMediaControl, (void **)&pControl);

    FILTER_STATE oldState;
    // Get the graph state
    // If the graph is in transition, we'll get the next state, not the previous
    hr = pControl->GetState(10, (OAFilterState *)&oldState);
    DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IMediaControl::GetState returned %d (hr %x)", oldState, hr));

    // Stop the filter graph
    hr = pControl->Stop();
    DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IMediaControl::Stop (hr %x)", hr));

    Lock();

    // Update Output Pin
    pPin->SetStreamId(TrackNumDst);
    m_pDemuxer->SetActiveStream(pPin->GetPinType(), TrackNumDst);
    pPin->SetNewMediaTypes(pmts);

    // IGraphRebuildDelegate support
    // Query our Site for the appropriate interface, and if its present, delegate graph building there
    IGraphRebuildDelegate *pDelegate = NULL;
    if (SUCCEEDED(GetSite(IID_IGraphRebuildDelegate, (void **)&pDelegate)) && pDelegate) {
      hr = pDelegate->RebuildPin(m_pGraph, pPin);
      if (hr == S_FALSE) {
        int mtIdx = QueryAcceptMediaTypes(pPin->GetConnected(), pmts);
        if (mtIdx == -1) {
          DbgLog((LOG_ERROR, 10, L"::RenameOutputPin(): No matching media type after rebuild delegation"));
          mtIdx = 0;
        }
        CMediaType *mt = new CMediaType(pmts[mtIdx]);
        pPin->SendMediaType(mt);
      }
      SafeRelease(&pDelegate);

      if (SUCCEEDED(hr)) {
        goto resumegraph;
      }
      DbgLog((LOG_TRACE, 10, L"::RenameOutputPin(): IGraphRebuildDelegate::RebuildPin failed"));
    }

    // Audio Filters get their connected filter removed
    // This way we make sure we reconnect to the proper filter
    // Other filters just disconnect and try to reconnect later on
    PIN_INFO pInfo;
    hr = pPin->GetConnected()->QueryPinInfo(&pInfo);
    if (FAILED(hr)) {
      DbgLog((LOG_ERROR, 10, L"::RenameOutputPin(): QueryPinInfo failed (hr %x)", hr));
    }

    int mtIdx = QueryAcceptMediaTypes(pPin->GetConnected(), pmts);
    BOOL bMediaTypeFound = (mtIdx >= 0);

    if (!bMediaTypeFound) {
      DbgLog((LOG_TRACE, 10, L"::RenameOutputPin() - Filter does not accept our media types!"));
      mtIdx = 0; // Fallback type
    }
    CMediaType *pmt = &pmts[mtIdx];

    if(!pPin->IsVideoPin() && SUCCEEDED(hr) && pInfo.pFilter) {
      BOOL bRemoveFilter = m_settings.StreamSwitchRemoveAudio || !bMediaTypeFound;
      if (bRemoveFilter && pPin->IsAudioPin()) {
        hr = m_pGraph->RemoveFilter(pInfo.pFilter);
  #ifdef DEBUG
        CLSID guidFilter;
        pInfo.pFilter->GetClassID(&guidFilter);
        DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IFilterGraph::RemoveFilter - %s (hr %x)", WStringFromGUID(guidFilter).c_str(), hr));
  #endif
        // Use IGraphBuilder to rebuild the graph
        IGraphBuilder *pGraphBuilder = NULL;
        if(SUCCEEDED(hr = m_pGraph->QueryInterface(__uuidof(IGraphBuilder), (void **)&pGraphBuilder))) {
          // Instruct the GraphBuilder to connect us again
          hr = pGraphBuilder->Render(pPin);
          DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IGraphBuilder::Render (hr %x)", hr));
          pGraphBuilder->Release();
        }
      } else {
        hr = ReconnectPin(pPin, pmt);
        DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - ReconnectPin (hr %x)", hr));
      }

      if (pPin->IsAudioPin() && m_settings.PGSForcedStream)
        UpdateForcedSubtitleMediaType();
    } else {
      CMediaType *mt = new CMediaType(*pmt);
      pPin->SendMediaType(mt);
      DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - Sending new Media Type"));
    }
    if(SUCCEEDED(hr) && pInfo.pFilter) { pInfo.pFilter->Release(); }

resumegraph:
    Unlock();

    // Re-start the graph
    if(oldState == State_Paused) {
      hr = pControl->Pause();
      DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IMediaControl::Pause (hr %x)", hr));
    } else if (oldState == State_Running) {
      hr = pControl->Run();
      DbgLog((LOG_TRACE, 20, L"::RenameOutputPin() - IMediaControl::Run (hr %x)", hr));
    }
    pControl->Release();

    return hr;
  } else if (pPin) {
    CAutoLock lock(this);
    // In normal operations, this won't make much sense
    // However, in graphstudio it is now possible to change the stream before connecting
    pPin->SetStreamId(TrackNumDst);
    m_pDemuxer->SetActiveStream(pPin->GetPinType(), TrackNumDst);
    pPin->SetNewMediaTypes(pmts);

    return S_OK;
  }

  return E_FAIL;
}

// IAMStreamSelect
STDMETHODIMP CLAVSplitter::Count(DWORD *pcStreams)
{
  CheckPointer(pcStreams, E_POINTER);
  CheckPointer(m_pDemuxer, E_UNEXPECTED);

  *pcStreams = 0;
  for(int i = 0; i < CBaseDemuxer::unknown; i++) {
    *pcStreams += (DWORD)m_pDemuxer->GetStreams((CBaseDemuxer::StreamType)i)->size();
  }

  return S_OK;
}

STDMETHODIMP CLAVSplitter::Enable(long lIndex, DWORD dwFlags)
{
  CheckPointer(m_pDemuxer, E_UNEXPECTED);
  if(!(dwFlags & AMSTREAMSELECTENABLE_ENABLE)) {
    return E_NOTIMPL;
  }

  for(int i = 0, j = 0; i < CBaseDemuxer::unknown; i++) {
    CBaseDemuxer::CStreamList *streams = m_pDemuxer->GetStreams((CBaseDemuxer::StreamType)i);
    int cnt = (int)streams->size();

    if(lIndex >= j && lIndex < j+cnt) {
      long idx = (lIndex - j);

      CBaseDemuxer::stream& to = streams->at(idx);

      std::deque<CBaseDemuxer::stream>::iterator it;
      for(it = streams->begin(); it != streams->end(); ++it) {
        if(!GetOutputPin(it->pid)) {
          continue;
        }

        HRESULT hr;
        if(FAILED(hr = RenameOutputPin(*it, to, to.streamInfo->mtypes))) {
          return hr;
        }
        return S_OK;
      }
      break;
    }
    j += cnt;
  }
  return S_FALSE;
}

STDMETHODIMP CLAVSplitter::Info(long lIndex, AM_MEDIA_TYPE **ppmt, DWORD *pdwFlags, LCID *plcid, DWORD *pdwGroup, WCHAR **ppszName, IUnknown **ppObject, IUnknown **ppUnk)
{
  CheckPointer(m_pDemuxer, E_UNEXPECTED);
  HRESULT hr = S_FALSE;
  for(int i = 0, j = 0; i < CBaseDemuxer::unknown; i++) {
    CBaseDemuxer::CStreamList *streams = m_pDemuxer->GetStreams((CBaseDemuxer::StreamType)i);
    int cnt = (int)streams->size();

    if(lIndex >= j && lIndex < j+cnt) {
      long idx = (lIndex - j);

      CBaseDemuxer::stream& s = streams->at(idx);

      if(ppmt) *ppmt = CreateMediaType(&s.streamInfo->mtypes[0]);
      if(pdwFlags) *pdwFlags = GetOutputPin(s) ? (AMSTREAMSELECTINFO_ENABLED|AMSTREAMSELECTINFO_EXCLUSIVE) : 0;
      if(pdwGroup) *pdwGroup = i;
      if(ppObject) *ppObject = NULL;
      if(ppUnk) *ppUnk = NULL;

      // Special case for the "no subtitles" pin
      if(s.pid == NO_SUBTITLE_PID) {
        if (plcid) *plcid = LCID_NOSUBTITLES;
        if (ppszName) {
          WCHAR str[] = L"S: No subtitles";
          size_t len = wcslen(str) + 1;
          *ppszName = (WCHAR*)CoTaskMemAlloc(len * sizeof(WCHAR));
          wcsncpy_s(*ppszName, len, str, _TRUNCATE);
        }
      } else if (s.pid == FORCED_SUBTITLE_PID) {
        if (plcid) {
          SUBTITLEINFO *subinfo = (SUBTITLEINFO *)s.streamInfo->mtypes[0].Format();
          *plcid = ProbeLangForLCID(subinfo->IsoLang);
        }
        if (ppszName) {
          WCHAR str[] = L"S: " FORCED_SUB_STRING;
          size_t len = wcslen(str) + 1;
          *ppszName = (WCHAR*)CoTaskMemAlloc(len * sizeof(WCHAR));
          wcsncpy_s(*ppszName, len, str, _TRUNCATE);
        }
      } else {
        // Populate stream name and language code
        m_pDemuxer->StreamInfo(s, plcid, ppszName);
      }
      hr = S_OK;
      break;
    }
    j += cnt;
  }

  return hr;
}

// setting helpers
std::list<std::string> CLAVSplitter::GetPreferredAudioLanguageList()
{
  // Convert to multi-byte ascii
  int bufSize = (int)(sizeof(WCHAR) * (m_settings.prefAudioLangs.length() + 1));
  char *buffer = (char *)CoTaskMemAlloc(bufSize);
  WideCharToMultiByte(CP_UTF8, 0, m_settings.prefAudioLangs.c_str(), -1, buffer, bufSize, NULL, NULL);

  std::list<std::string> list;

  split(std::string(buffer), std::string(",; "), list);
  CoTaskMemFree(buffer);

  return list;
}

std::list<CSubtitleSelector> CLAVSplitter::GetSubtitleSelectors()
{
  std::list<CSubtitleSelector> selectorList;

  std::string separators = ",; ";
  std::list<std::string> tokenList;

  if (m_settings.subtitleMode == LAVSubtitleMode_NoSubs) {
    // Do nothing
  } else if (m_settings.subtitleMode == LAVSubtitleMode_Default || m_settings.subtitleMode == LAVSubtitleMode_ForcedOnly) {
    // Convert to multi-byte ascii
    size_t bufSize = sizeof(WCHAR) * (m_settings.prefSubLangs.length() + 1);
    char *buffer = (char *)CoTaskMemAlloc(bufSize);
    ZeroMemory(buffer, bufSize);
    WideCharToMultiByte(CP_UTF8, 0, m_settings.prefSubLangs.c_str(), -1, buffer, (int)bufSize, NULL, NULL);

    std::list<std::string> langList;
    split(std::string(buffer), separators, langList);
    SAFE_CO_FREE(buffer);

    // If no languages have been set, prefer the forced/default streams as specified by the audio languages
    bool bNoLanguage = false;
    if (langList.empty()) {
      langList = GetPreferredAudioLanguageList();
      bNoLanguage = true;
    }

    std::list<std::string>::iterator it;
    for (it = langList.begin(); it != langList.end(); it++) {
      std::string token = "*:" + *it;
      if (m_settings.subtitleMode == LAVSubtitleMode_ForcedOnly || bNoLanguage) {
        tokenList.push_back(token + "|f");
        if (m_settings.subtitleMode == LAVSubtitleMode_Default)
          tokenList.push_back(token + "|d");
      } else
          tokenList.push_back(token + "|!h");
    }

    // Add fallbacks (forced/default)
    tokenList.push_back("*:*|f");
    if (m_settings.subtitleMode == LAVSubtitleMode_Default)
      tokenList.push_back("*:*|d");
  } else if (m_settings.subtitleMode == LAVSubtitleMode_Advanced) {
    // Convert to multi-byte ascii
    size_t bufSize = sizeof(WCHAR) * (m_settings.subtitleAdvanced.length() + 1);
    char *buffer = (char *)CoTaskMemAlloc(bufSize);
    ZeroMemory(buffer, bufSize);
    WideCharToMultiByte(CP_UTF8, 0, m_settings.subtitleAdvanced.c_str(), -1, buffer, (int)bufSize, NULL, NULL);

    split(std::string(buffer), separators, tokenList);
    SAFE_CO_FREE(buffer);
  }

  // Add the "off" termination element
  tokenList.push_back("*:off");

  std::tr1::regex advRegex("(?:(\\*|[[:alpha:]]+):)?(\\*|[[:alpha:]]+)(?:\\|(!?)([fdnh]+))?");
  std::list<std::string>::iterator it;
  for (it = tokenList.begin(); it != tokenList.end(); it++) {
    std::tr1::cmatch res;
    bool found = std::tr1::regex_search(it->c_str(), res, advRegex);
    if (found) {
      CSubtitleSelector selector;
      selector.audioLanguage = res[1].str().empty() ? "*" : ProbeForISO6392(res[1].str().c_str());
      selector.subtitleLanguage = ProbeForISO6392(res[2].str().c_str());
      selector.dwFlags = 0;

      // Parse flags
      std::string flags = res[4];
      if (flags.length() > 0) {
        if (flags.find('d') != flags.npos)
          selector.dwFlags |= SUBTITLE_FLAG_DEFAULT;
        if (flags.find('f') != flags.npos)
          selector.dwFlags |= SUBTITLE_FLAG_FORCED;
        if (flags.find('n') != flags.npos)
          selector.dwFlags |= SUBTITLE_FLAG_NORMAL;
        if (flags.find('h') != flags.npos)
          selector.dwFlags |= SUBTITLE_FLAG_IMPAIRED;

        // Check for flag negation
        std::string not = res[3];
        if (not.length() == 1 && not == "!") {
          selector.dwFlags = (~selector.dwFlags) & 0xFF;
        }
      }
      selectorList.push_back(selector);
      DbgLog((LOG_TRACE, 10, L"::GetSubtitleSelectors(): Parsed selector \"%S\" to: %S -> %S (flags: 0x%x)", it->c_str(), selector.audioLanguage.c_str(), selector.subtitleLanguage.c_str(), selector.dwFlags));
    } else {
      DbgLog((LOG_ERROR, 10, L"::GetSubtitleSelectors(): Selector string \"%S\" could not be parsed", it->c_str()));
    }
  }

  return selectorList;
}

// Settings
// ILAVAudioSettings
HRESULT CLAVSplitter::SetRuntimeConfig(BOOL bRuntimeConfig)
{
  m_bRuntimeConfig = bRuntimeConfig;
  LoadSettings();

  return S_OK;
}


STDMETHODIMP CLAVSplitter::GetPreferredLanguages(WCHAR **ppLanguages)
{
  CheckPointer(ppLanguages, E_POINTER);
  size_t len = m_settings.prefAudioLangs.length() + 1;
  if (len > 1) {
    *ppLanguages = (WCHAR *)CoTaskMemAlloc(sizeof(WCHAR) * len);
    wcsncpy_s(*ppLanguages, len,  m_settings.prefAudioLangs.c_str(), _TRUNCATE);
  } else {
    *ppLanguages = NULL;
  }
  return S_OK;
}

STDMETHODIMP CLAVSplitter::SetPreferredLanguages(WCHAR *pLanguages)
{
  m_settings.prefAudioLangs = std::wstring(pLanguages);
  return SaveSettings();
}

STDMETHODIMP CLAVSplitter::GetPreferredSubtitleLanguages(WCHAR **ppLanguages)
{
  CheckPointer(ppLanguages, E_POINTER);
  size_t len = m_settings.prefSubLangs.length() + 1;
  if (len > 1) {
    *ppLanguages = (WCHAR *)CoTaskMemAlloc(sizeof(WCHAR) * len);
    wcsncpy_s(*ppLanguages, len,  m_settings.prefSubLangs.c_str(), _TRUNCATE);
  } else {
    *ppLanguages = NULL;
  }
  return S_OK;
}

STDMETHODIMP CLAVSplitter::SetPreferredSubtitleLanguages(WCHAR *pLanguages)
{
  m_settings.prefSubLangs = std::wstring(pLanguages);
  return SaveSettings();
}

STDMETHODIMP_(LAVSubtitleMode) CLAVSplitter::GetSubtitleMode()
{
  return m_settings.subtitleMode;
}

STDMETHODIMP CLAVSplitter::SetSubtitleMode(LAVSubtitleMode mode)
{
  m_settings.subtitleMode = mode;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetSubtitleMatchingLanguage()
{
  return FALSE;
}

STDMETHODIMP CLAVSplitter::SetSubtitleMatchingLanguage(BOOL dwMode)
{
  return E_FAIL;
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetPGSForcedStream()
{
  return m_settings.PGSForcedStream;
}

STDMETHODIMP CLAVSplitter::SetPGSForcedStream(BOOL bFlag)
{
  m_settings.PGSForcedStream = bFlag;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetPGSOnlyForced()
{
  return m_settings.PGSOnlyForced;
}

STDMETHODIMP CLAVSplitter::SetPGSOnlyForced(BOOL bForced)
{
  m_settings.PGSOnlyForced = bForced;
  return SaveSettings();
}

STDMETHODIMP_(int) CLAVSplitter::GetVC1TimestampMode()
{
  return m_settings.vc1Mode;
}

STDMETHODIMP CLAVSplitter::SetVC1TimestampMode(int iMode)
{
  m_settings.vc1Mode = iMode;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::IsVC1CorrectionRequired()
{
  return
      FilterInGraph(CLSID_LAVVideo, m_pGraph)
   || (FilterInGraph(CLSID_LAVCUVID, m_pGraph) && (_strnicmp(m_pDemuxer->GetContainerFormat(), "matroska", 8) == 0))
   || FilterInGraph(CLSID_MPCVideoDec, m_pGraph)
   || FilterInGraph(CLSID_ffdshowDXVA, m_pGraph)
   || FilterInGraphWithInputSubtype(CLSID_madVR, m_pGraph, MEDIASUBTYPE_WVC1)
   || FilterInGraphWithInputSubtype(CLSID_DMOWrapperFilter, m_pGraph, MEDIASUBTYPE_WVC1);
}

STDMETHODIMP CLAVSplitter::SetSubstreamsEnabled(BOOL bSubStreams)
{
  m_settings.substreams = bSubStreams;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetSubstreamsEnabled()
{
  return m_settings.substreams;
}

STDMETHODIMP CLAVSplitter::SetVideoParsingEnabled(BOOL bEnabled)
{
  m_settings.videoParsing = bEnabled;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetVideoParsingEnabled()
{
  return m_settings.videoParsing;
}

STDMETHODIMP CLAVSplitter::SetFixBrokenHDPVR(BOOL bEnabled)
{
  return E_FAIL;
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetFixBrokenHDPVR()
{
  return TRUE;
}

STDMETHODIMP_(BOOL) CLAVSplitter::IsFormatEnabled(const char *strFormat)
{
  std::string format(strFormat);
  if (m_settings.formats.find(format) != m_settings.formats.end()) {
    return m_settings.formats[format];
  }
  return FALSE;
}

STDMETHODIMP_(HRESULT) CLAVSplitter::SetFormatEnabled(const char *strFormat, BOOL bEnabled)
{
  std::string format(strFormat);
  if (m_settings.formats.find(format) != m_settings.formats.end()) {
    m_settings.formats[format] = bEnabled;
    return SaveSettings();
  }
  return E_FAIL;
}

STDMETHODIMP CLAVSplitter::SetStreamSwitchRemoveAudio(BOOL bEnabled)
{
  m_settings.StreamSwitchRemoveAudio = bEnabled;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetStreamSwitchRemoveAudio()
{
  return m_settings.StreamSwitchRemoveAudio;
}

STDMETHODIMP CLAVSplitter::GetAdvancedSubtitleConfig(WCHAR **ppAdvancedConfig)
{
  CheckPointer(ppAdvancedConfig, E_POINTER);
  size_t len = m_settings.subtitleAdvanced.length() + 1;
  if (len > 1) {
    *ppAdvancedConfig = (WCHAR *)CoTaskMemAlloc(sizeof(WCHAR) * len);
    wcsncpy_s(*ppAdvancedConfig, len,  m_settings.subtitleAdvanced.c_str(), _TRUNCATE);
  } else {
    *ppAdvancedConfig = NULL;
  }
  return S_OK;
}

STDMETHODIMP CLAVSplitter::SetAdvancedSubtitleConfig(WCHAR *pAdvancedConfig)
{
  m_settings.subtitleAdvanced = std::wstring(pAdvancedConfig);
  return SaveSettings();
}

STDMETHODIMP CLAVSplitter::SetUseAudioForHearingVisuallyImpaired(BOOL bEnabled)
{
  m_settings.ImpairedAudio = bEnabled;
  return SaveSettings();
}

STDMETHODIMP_(BOOL) CLAVSplitter::GetUseAudioForHearingVisuallyImpaired()
{
  return m_settings.ImpairedAudio;
}

STDMETHODIMP_(std::set<FormatInfo>&) CLAVSplitter::GetInputFormats()
{
  return m_InputFormats;
}

// IAMOpenProgress
STDMETHODIMP CLAVSplitter::QueryProgress(LONGLONG *pllTotal, LONGLONG *pllCurrent)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : m_pInput->QueryProgress(pllTotal, pllCurrent);
}

STDMETHODIMP CLAVSplitter::AbortOperation(void)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : m_pInput->AbortOperation();
}

// IDownload interface
STDMETHODIMP CLAVSplitter::Download(LPCOLESTR uri, LPCOLESTR fileName)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : m_pInput->Download(uri, fileName);
}

STDMETHODIMP CLAVSplitter::DownloadAsync(LPCOLESTR uri, LPCOLESTR fileName, IDownloadCallback *downloadCallback)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : m_pInput->DownloadAsync(uri, fileName, downloadCallback);
}

CBaseDemuxer *CLAVSplitter::GetDemuxer(void)
{
  return this->m_pDemuxer;
}

void InvalidParameterHandler(const wchar_t* expression, const wchar_t* function, const wchar_t* file, unsigned int line, uintptr_t pReserved)
{
#ifdef _DEBUG
  if (ffmpeg_log_callback_set)
  {
    ffmpeg_logger_instance.Log(LOGGER_VERBOSE, L"%s: %s: invalid parameter detected in function '%s', file '%s', line %d.\nExpression: %s", MODULE_NAME, L"InvalidParameterHandler()", function, file, line, expression);
  }
#endif
}


void CLAVSplitter::ffmpeg_log_callback(void *ptr, int log_level, const char *format, va_list vl)
{
  // supress error messages while logging messages from ffmpeg
  // error messages are written to log file in Debug

  int warnReportMode = _CrtSetReportMode(_CRT_WARN, 0);
  int errorReportMode = _CrtSetReportMode(_CRT_ERROR, 0);
  int assertReportMode = _CrtSetReportMode(_CRT_ASSERT, 0);

  _invalid_parameter_handler previousHandler = _set_invalid_parameter_handler(InvalidParameterHandler);

  int length = _vscprintf(format, vl) + 1;
  ALLOC_MEM_DEFINE_SET(buffer, char, length, 0);
  if (buffer != NULL)
  {
    if (vsprintf_s(buffer, length, format, vl) != (-1))
    {
      char *trimmed = TrimA(buffer);
      if (trimmed != NULL)
      {
        wchar_t *logLine = ConvertToUnicodeA(trimmed);
        if (logLine != NULL)
        {
          ffmpeg_logger_instance.Log(LOGGER_VERBOSE, L"%s: %s: log level: %d, message: %s", MODULE_NAME, L"ffmpeg_log_callback()", log_level, logLine);
        }

        FREE_MEM(logLine);
      }
      FREE_MEM(trimmed);
    }
  }

  FREE_MEM(buffer);

  // set original values for error messages back
  _set_invalid_parameter_handler(previousHandler);

  _CrtSetReportMode(_CRT_WARN, warnReportMode);
  _CrtSetReportMode(_CRT_ERROR, errorReportMode);
  _CrtSetReportMode(_CRT_ASSERT, assertReportMode);
}

// IFilter interface

CLogger *CLAVSplitter::GetLogger(void)
{
  return m_pInput->GetLogger();
}

HRESULT CLAVSplitter::GetTotalLength(int64_t *totalLength)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetTotalLength(totalLength);
}

HRESULT CLAVSplitter::GetAvailableLength(int64_t *availableLength)
{
  return (m_pInput == NULL) ? E_NOT_VALID_STATE : this->m_pInput->GetAvailableLength(availableLength);
}

// ISeeking interface

unsigned int CLAVSplitter::GetSeekingCapabilities(void)
{
  return m_pInput->GetSeekingCapabilities();
}

int64_t CLAVSplitter::SeekToTime(int64_t time)
{
  return m_pInput->SeekToTime(time);
}

int64_t CLAVSplitter::SeekToPosition(int64_t start, int64_t end)
{
  return m_pInput->SeekToPosition(start, end);
}

void CLAVSplitter::SetSupressData(bool supressData)
{
  if (this->m_pInput != NULL)
  {
    this->m_pInput->SetSupressData(supressData);
  }
}

// IFilterState interface

HRESULT CLAVSplitter::IsFilterReadyToConnectPins(bool *ready)
{
  CheckPointer(ready, E_POINTER);

  *ready = (this->m_pInput->createdDemuxer);

  if (FAILED(this->m_pInput->GetParserHosterStatus()))
  {
    // return parser hoster status, there is error
    return this->m_pInput->GetParserHosterStatus();
  }
  else if ((!(*ready)) && this->m_pInput->allDataReceived && (!this->m_pInput->createdDemuxer) && (this->m_pInput->demuxerWorkerFinished))
  {
    // if demuxer is not created, all data are received and demuxer worker finished its work
    // it throws exception in OV and immediately stops buffering and playback
    return E_DEMUXER_NOT_CREATED_ALL_DATA_RECEIVED_DEMUXER_WORKER_FINISHED;
  }

  return S_OK;
}

HRESULT CLAVSplitter::GetCacheFileName(wchar_t **path)
{
  CheckPointer(path, E_POINTER);

	if (this->m_pInput->storeFilePath != NULL)
	{
    int length = wcslen(this->m_pInput->storeFilePath) + 1;
    *path = ALLOC_MEM_SET(*path, wchar_t, length, 0);

    CheckPointer((*path), E_OUTOFMEMORY);

    if (*path != NULL)
    {
      if (wcscpy_s(*path, length, this->m_pInput->storeFilePath) != 0)
      {
        FREE_MEM(*path);
        return E_CONVERT_STRING_ERROR;
      }
    }
	}

	return S_OK;
}

int CLAVSplitter::GetLastCommand(void)
{
  return this->lastCommand;
}