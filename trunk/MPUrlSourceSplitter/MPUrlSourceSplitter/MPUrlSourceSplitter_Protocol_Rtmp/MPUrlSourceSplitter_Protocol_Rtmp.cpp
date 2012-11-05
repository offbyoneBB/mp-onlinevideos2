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

#include "stdafx.h"

#include "MPUrlSourceSplitter_Protocol_Rtmp.h"
#include "Utilities.h"
#include "LockMutex.h"
#include "FlvPacket.h"
#include "VersionInfo.h"
#include "MPUrlSourceSplitter_Protocol_Rtmp_Parameters.h"
#include "RtmpCurlInstance.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#ifdef _DEBUG
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_Protocol_Rtmpd"
#else
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_Protocol_Rtmp"
#endif

PIPlugin CreatePluginInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Protocol_Rtmp(configuration);
}

void DestroyPluginInstance(PIPlugin pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Protocol_Rtmp *pClass = (CMPUrlSourceSplitter_Protocol_Rtmp *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Protocol_Rtmp::CMPUrlSourceSplitter_Protocol_Rtmp(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_PROTOCOL_RTMP, COMPILE_INFO_MPURLSOURCESPLITTER_PROTOCOL_RTMP);
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);

  version = CCurlInstance::GetCurlVersion();
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);
  
  this->receiveDataTimeout = RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->wholeStreamDownloaded = false;
  this->mainCurlInstance = NULL;
  this->streamDuration = 0;
  this->bytePosition = 0;
  this->seekingActive = false;
  this->supressData = false;
  this->bufferForProcessing = NULL;
  this->shouldExit = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Protocol_Rtmp::~CMPUrlSourceSplitter_Protocol_Rtmp()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  FREE_MEM_CLASS(this->mainCurlInstance);
  FREE_MEM_CLASS(this->bufferForProcessing);
  FREE_MEM_CLASS(this->configurationParameters);

  if (this->lockMutex != NULL)
  {
    CloseHandle(this->lockMutex);
    this->lockMutex = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

// IProtocol interface

bool CMPUrlSourceSplitter_Protocol_Rtmp::IsConnected(void)
{
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded));
}

unsigned int CMPUrlSourceSplitter_Protocol_Rtmp::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::ParseUrl(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  this->ClearSession();
  if (parameters != NULL)
  {
    this->configurationParameters->Clear();
    ALLOC_MEM_DEFINE_SET(protocolConfiguration, ProtocolPluginConfiguration, 1, 0);
    if (protocolConfiguration != NULL)
    {
      protocolConfiguration->configuration = (CParameterCollection *)parameters;
    }
    this->Initialize(protocolConfiguration);
    FREE_MEM(protocolConfiguration);
  }

  const wchar_t *url = this->configurationParameters->GetValue(PARAMETER_NAME_URL, true, NULL);
  if (SUCCEEDED(result))
  {
    result = (url == NULL) ? E_OUTOFMEMORY : S_OK;
  }

  if (SUCCEEDED(result))
  {
    ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
    if (urlComponents == NULL)
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'url components'");
      result = E_OUTOFMEMORY;
    }

    if (SUCCEEDED(result))
    {
      ZeroURL(urlComponents);
      urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

      this->logger->Log(LOGGER_INFO, L"%s: %s: url: %s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, url);

      if (!InternetCrackUrl(url, 0, 0, urlComponents))
      {
        this->logger->Log(LOGGER_ERROR, L"%s: %s: InternetCrackUrl() error: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, GetLastError());
        result = E_FAIL;
      }
    }

    if (SUCCEEDED(result))
    {
      int length = urlComponents->dwSchemeLength + 1;
      ALLOC_MEM_DEFINE_SET(protocol, wchar_t, length, 0);
      if (protocol == NULL) 
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'protocol'");
        result = E_OUTOFMEMORY;
      }

      if (SUCCEEDED(result))
      {
        wcsncat_s(protocol, length, urlComponents->lpszScheme, urlComponents->dwSchemeLength);

        bool supportedProtocol = false;
        for (int i = 0; i < TOTAL_SUPPORTED_PROTOCOLS; i++)
        {
          if (_wcsnicmp(urlComponents->lpszScheme, SUPPORTED_PROTOCOLS[i], urlComponents->dwSchemeLength) == 0)
          {
            supportedProtocol = true;
            break;
          }
        }

        if (!supportedProtocol)
        {
          // not supported protocol
          this->logger->Log(LOGGER_INFO, L"%s: %s: unsupported protocol '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, protocol);
          result = E_FAIL;
        }
      }
      FREE_MEM(protocol);
    }

    FREE_MEM(urlComponents);
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);
  return result;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::ReceiveData(bool *shouldExit, CReceiveData *receiveData)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  this->shouldExit = *shouldExit;

  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->IsConnected())
  {
    if (!this->wholeStreamDownloaded)
    {
      if (!(this->shouldExit))
      {
        if (!this->setLength)
        {
          double streamSize = 0;
          CURLcode errorCode = curl_easy_getinfo(this->mainCurlInstance->GetCurlHandle(), CURLINFO_CONTENT_LENGTH_DOWNLOAD, &streamSize);
          if ((errorCode == CURLE_OK) && (streamSize > 0))
          {
            LONGLONG total = LONGLONG(streamSize);
            this->streamLength = total;
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, total);
            receiveData->GetTotalLength()->SetTotalLength(total, false);
            this->setLength = true;
          }
        }

        if (this->streamDuration == 0)
        {
          double streamDuration = 0;
          CURLcode errorCode = curl_easy_getinfo(this->mainCurlInstance->GetCurlHandle(), CURLINFO_RTMP_TOTAL_DURATION, &streamDuration);
          if ((errorCode == CURLE_OK) && (streamDuration > 0))
          {
            this->streamDuration = streamDuration;
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total duration: %lf", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, streamDuration);
          }
        }

        if (!this->setLength)
        {
          if ((this->streamLength == 0) || (this->bytePosition > (this->streamLength * 3 / 4)))
          {
            double currentTime = 0;
            CURLcode errorCode = curl_easy_getinfo(this->mainCurlInstance->GetCurlHandle(), CURLINFO_RTMP_CURRENT_TIME, &currentTime);
            if ((errorCode == CURLE_OK) && (currentTime > 0) && (this->streamDuration != 0))
            {
              LONGLONG tempLength = static_cast<LONGLONG>(this->bytePosition * this->streamDuration / currentTime);
              if (tempLength > this->streamLength)
              {
                this->streamLength = tempLength;
                this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting quess by stream duration total length: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
                receiveData->GetTotalLength()->SetTotalLength(this->streamLength, true);
              }
            }
            else if (this->bytePosition != 0)
            {
              if (this->streamLength == 0)
              {
                // error occured or stream duration is not set
                // just make guess
                this->streamLength = LONGLONG(MINIMUM_RECEIVED_DATA_FOR_SPLITTER);
                this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting quess total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
                receiveData->GetTotalLength()->SetTotalLength(this->streamLength, true);
              }
              else if ((this->bytePosition > (this->streamLength * 3 / 4)))
              {
                // it is time to adjust stream length, we are approaching to end but still we don't know total length
                this->streamLength = this->bytePosition * 2;
                this->logger->Log(LOGGER_VERBOSE, L"%s: %s: adjusting quess total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
                receiveData->GetTotalLength()->SetTotalLength(this->streamLength, true);
              }
            }
          }
        }

        unsigned int bytesRead = this->mainCurlInstance->GetRtmpDownloadResponse()->GetReceivedData()->GetBufferOccupiedSpace();
        if (bytesRead != 0)
        {
          if (this->bufferForProcessing != NULL)
          {
            unsigned int bufferSize = this->bufferForProcessing->GetBufferSize();
            unsigned int freeSpace = this->bufferForProcessing->GetBufferFreeSpace();

            if (freeSpace < bytesRead)
            {
              unsigned int bufferNewSize = max(bufferSize * 2, bufferSize + bytesRead);
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: buffer to small, buffer size: %d, new size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, bufferSize, bufferNewSize);
              if (!this->bufferForProcessing->ResizeBuffer(bufferNewSize))
              {
                this->logger->Log(LOGGER_WARNING, L"%s: %s: resizing buffer unsuccessful, dropping received data", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
                // error
                bytesRead = 0;
              }
            }

            if (bytesRead != 0)
            {
              ALLOC_MEM_DEFINE_SET(buffer, unsigned char, bytesRead, 0);
              if (buffer != NULL)
              {
                unsigned int copyFromBuffer = this->mainCurlInstance->GetRtmpDownloadResponse()->GetReceivedData()->CopyFromBuffer(buffer, bytesRead, 0, 0);
                unsigned int addedToBuffer = this->bufferForProcessing->AddToBuffer(buffer, bytesRead);
                this->mainCurlInstance->GetRtmpDownloadResponse()->GetReceivedData()->RemoveFromBufferAndMove(bytesRead);
              }
              FREE_MEM(buffer); 
            }
          }
        }
      }

      if ((!this->supressData) && (this->bufferForProcessing != NULL))
      {
        CFlvPacket *flvPacket = new CFlvPacket();
        if (flvPacket != NULL)
        {
          while (flvPacket->ParsePacket(this->bufferForProcessing))
          {
            // FLV packet parsed correctly
            // push FLV packet to filter
            if ((flvPacket->GetType() != FLV_PACKET_HEADER) && (this->firstTimestamp == (-1)))
            {
              this->firstTimestamp = flvPacket->GetTimestamp();
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: set first timestamp: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->firstTimestamp);
            }

            if ((flvPacket->GetType() == FLV_PACKET_VIDEO) && (this->firstVideoTimestamp == (-1)))
            {
              this->firstVideoTimestamp = flvPacket->GetTimestamp();
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: set first video timestamp: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->firstVideoTimestamp);
            }

            if ((flvPacket->GetType() == FLV_PACKET_VIDEO) && (this->firstVideoTimestamp != (-1)) && (this->firstTimestamp != (-1)))
            {
              // correction of video timestamps
              flvPacket->SetTimestamp(flvPacket->GetTimestamp() + this->firstTimestamp - this->firstVideoTimestamp);
            }

            if ((flvPacket->GetType() == FLV_PACKET_AUDIO) ||
              (flvPacket->GetType() == FLV_PACKET_HEADER) ||
              (flvPacket->GetType() == FLV_PACKET_META) ||
              (flvPacket->GetType() == FLV_PACKET_VIDEO))
            {
              // do nothing, known packet types
            }
            else
            {
              this->logger->Log(LOGGER_WARNING, L"%s: %s: unknown FLV packet: %d, size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, flvPacket->GetType(), flvPacket->GetSize());
            }

            if ((flvPacket->GetType() != FLV_PACKET_HEADER) || (!this->seekingActive))
            {
              // create media packet
              // set values of media packet
              CMediaPacket *mediaPacket = new CMediaPacket();
              mediaPacket->GetBuffer()->InitializeBuffer(flvPacket->GetSize());
              mediaPacket->GetBuffer()->AddToBuffer(flvPacket->GetData(), flvPacket->GetSize());
              mediaPacket->SetStart(this->bytePosition);
              mediaPacket->SetEnd(this->bytePosition + flvPacket->GetSize() - 1);

              if (!receiveData->GetMediaPacketCollection()->Add(mediaPacket))
              {
                FREE_MEM_CLASS(mediaPacket);
              }
              
              this->bytePosition += flvPacket->GetSize();
            }
            // we are definitely not seeking
            this->seekingActive = false;
            this->bufferForProcessing->RemoveFromBufferAndMove(flvPacket->GetSize());

            flvPacket->Clear();
          }

          delete flvPacket;
          flvPacket = NULL;
        }
      }

      if (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA)
      {
        // all data received, we're not receiving data
        this->logger->Log(LOGGER_VERBOSE, L"%s: %s: received all data", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);

        // whole stream downloaded
        this->wholeStreamDownloaded = true;
        FREE_MEM_CLASS(this->mainCurlInstance);

        if (!this->seekingActive)
        {
          // we are not seeking, so we can set total length
          if (!this->setLength)
          {
            this->streamLength = this->bytePosition;
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
            receiveData->GetTotalLength()->SetTotalLength(this->streamLength, false);
            this->setLength = true;
          }

          // notify filter the we reached end of stream
          receiveData->GetEndOfStreamReached()->SetStreamPosition(max(0, this->bytePosition - 1));
        }
      }
    }
    else
    {
      // set total length (if not set earlier)
      if (!this->setLength)
      {
        this->streamLength = this->bytePosition;
        this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
        receiveData->GetTotalLength()->SetTotalLength(this->streamLength, false);
        this->setLength = true;
      }
    }
  }
  else
  {
    this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"connection closed, opening new one");
    // re-open connection if previous is lost
    if (this->StartReceivingData(NULL) != S_OK)
    {
      this->StopReceivingData();
    }
  }

  this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  return S_OK;
}

// ISimpleProtocol interface

unsigned int CMPUrlSourceSplitter_Protocol_Rtmp::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::StartReceivingData(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME);

  CHECK_POINTER_DEFAULT_HRESULT(result, this->configurationParameters);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  this->wholeStreamDownloaded = false;
  this->firstTimestamp = -1;
  this->firstVideoTimestamp = -1;
  this->bytePosition = 0;
  this->streamLength = 0;
  this->setLength = false;

  if (SUCCEEDED(result))
  {
    this->mainCurlInstance = new CRtmpCurlInstance(this->logger, this->lockMutex, PROTOCOL_IMPLEMENTATION_NAME, L"Main");
    CHECK_POINTER_HRESULT(result, this->mainCurlInstance, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      CRtmpDownloadRequest *request = new CRtmpDownloadRequest();
      CHECK_POINTER_HRESULT(result, request, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        request->SetUrl(this->configurationParameters->GetValue(PARAMETER_NAME_URL, true, NULL));
        request->SetRtmpApp(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_APP, true, RTMP_APP_DEFAULT));
        request->SetRtmpArbitraryData(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_ARBITRARY_DATA, true, NULL));
        request->SetRtmpBuffer(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_BUFFER, true, RTMP_BUFFER_DEFAULT));
        request->SetRtmpFlashVersion(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_FLASHVER, true, RTMP_FLASH_VER_DEFAULT));
        request->SetRtmpAuth(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_AUTH, true, RTMP_AUTH_DEFAULT));
        request->SetRtmpJtv(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_JTV, true, RTMP_JTV_DEFAULT));
        request->SetRtmpLive(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_LIVE, true, RTMP_LIVE_DEFAULT));
        request->SetRtmpPageUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PAGE_URL, true, RTMP_PAGE_URL_DEFAULT));
        request->SetRtmpPlaylist(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_PLAYLIST, true, RTMP_PLAYLIST_DEFAULT));
        request->SetRtmpPlayPath(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PLAY_PATH, true, RTMP_PLAY_PATH_DEFAULT));
        request->SetRtmpStart((this->streamTime >= 0) ? this->streamTime : this->configurationParameters->GetValueInt64(PARAMETER_NAME_RTMP_START, true, RTMP_START_DEFAULT));
        request->SetRtmpStop(this->configurationParameters->GetValueInt64(PARAMETER_NAME_RTMP_STOP, true, RTMP_STOP_DEFAULT));
        request->SetRtmpSubscribe(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SUBSCRIBE, true, RTMP_SUBSCRIBE_DEFAULT));
        request->SetRtmpSwfAge(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_SWF_AGE, true, RTMP_SWF_AGE_DEFAULT));
        request->SetRtmpSwfUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SWF_URL, true, RTMP_SWF_URL_DEFAULT));
        request->SetRtmpSwfVerify(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_SWF_VERIFY, true, RTMP_SWF_VERIFY_DEFAULT));
        request->SetRtmpTcUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TC_URL, true, RTMP_TC_URL_DEFAULT));
        request->SetRtmpToken(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TOKEN, true, RTMP_TOKEN_DEFAULT));

        this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
        result = (this->mainCurlInstance->Initialize(request)) ? S_OK : E_FAIL;
      }
      FREE_MEM_CLASS(request);
    }
  }

  if (SUCCEEDED(result) && (this->bufferForProcessing == NULL))
  {
    this->bufferForProcessing = new CLinearBuffer();
    result = (this->bufferForProcessing != NULL) ? S_OK : E_OUTOFMEMORY;

    if (SUCCEEDED(result))
    {
      result = this->bufferForProcessing->InitializeBuffer(BUFFER_FOR_PROCESSING_SIZE_DEFAULT, 0) ? S_OK : E_OUTOFMEMORY;
    }
  }

  if (SUCCEEDED(result))
  {
    // all parameters set
    // start receiving data

    result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
  }

  if (SUCCEEDED(result))
  {
    // wait until we receive some data or transfer end (whatever comes first)
    unsigned int state = CURL_STATE_NONE;
    while ((state != CURL_STATE_RECEIVING_DATA) && (state != CURL_STATE_RECEIVED_ALL_DATA))
    {
      state = this->mainCurlInstance->GetCurlState();

      // wait some time
      Sleep(10);
    }

    if (state == CURL_STATE_RECEIVED_ALL_DATA)
    {
      // we received data too fast
      result = E_FAIL;
    }
  }

  if (FAILED(result))
  {
    this->StopReceivingData();
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME);
  return result;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::StopReceivingData(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->mainCurlInstance != NULL)
  {
    this->mainCurlInstance->SetCloseWithoutWaiting(this->seekingActive);
  }

  FREE_MEM_CLASS(this->mainCurlInstance);
  FREE_MEM_CLASS(this->bufferForProcessing);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, current);

  if (SUCCEEDED(result))
  {
    *total = (this->streamLength == 0) ? 1 : this->streamLength;
    *current = (this->streamLength == 0) ? 0 : this->bytePosition;

    if (!this->setLength)
    {
      result = VFW_S_ESTIMATED;
    }
  }

  this->logger->Log(LOGGER_DATA, (SUCCEEDED(result)) ? METHOD_END_HRESULT_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME, result);
  return result;
}
  
HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, availableLength);

  if (SUCCEEDED(result))
  {
    availableLength->SetQueryResult(S_OK);
    if (!this->setLength)
    {
      availableLength->SetAvailableLength(this->bytePosition);
    }
    else
    {
      availableLength->SetAvailableLength(this->streamLength);
    }
  }

  return result;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  FREE_MEM_CLASS(this->bufferForProcessing);
 
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->wholeStreamDownloaded = false;
  this->receiveDataTimeout = RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->streamDuration = 0;
  this->bytePosition = 0;
  this->shouldExit = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

// ISeeking interface

unsigned int CMPUrlSourceSplitter_Protocol_Rtmp::GetSeekingCapabilities(void)
{
  return SEEKING_METHOD_TIME;
}

int64_t CMPUrlSourceSplitter_Protocol_Rtmp::SeekToTime(int64_t time)
{
  CLockMutex lock(this->lockMutex, INFINITE);

  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, time);

  int64_t result = -1;

  this->seekingActive = true;

  // close connection
  this->StopReceivingData();

  // RTMP protocol can seek to ms
  // time is in ms

  // 1 second back
  this->streamTime = max(0, time - 1000);

  // reopen connection
  // StartReceivingData() reset wholeStreamDownloaded
  this->StartReceivingData(NULL);

  if (this->IsConnected())
  {
    result = max(0, time - 1000);
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, result);
  return result;
}

int64_t CMPUrlSourceSplitter_Protocol_Rtmp::SeekToPosition(int64_t start, int64_t end)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu, to time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  return result;
}

void CMPUrlSourceSplitter_Protocol_Rtmp::SetSupressData(bool supressData)
{
  this->supressData = supressData;
}

// IPlugin interface

const wchar_t *CMPUrlSourceSplitter_Protocol_Rtmp::GetName(void)
{
  return PROTOCOL_NAME;
}

GUID CMPUrlSourceSplitter_Protocol_Rtmp::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

HRESULT CMPUrlSourceSplitter_Protocol_Rtmp::Initialize(PluginConfiguration *configuration)
{
  if (configuration == NULL)
  {
    return E_POINTER;
  }

  ProtocolPluginConfiguration *protocolConfiguration = (ProtocolPluginConfiguration *)configuration;
  this->logger->SetParameters(protocolConfiguration->configuration);

  if (this->lockMutex == NULL)
  {
    return E_FAIL;
  }

  this->configurationParameters->Clear();
  if (protocolConfiguration->configuration != NULL)
  {
    this->configurationParameters->Append(protocolConfiguration->configuration);
  }
  this->configurationParameters->LogCollection(this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_RTMP_RECEIVE_DATA_TIMEOUT, true, RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? RTMP_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return S_OK;
}

// other methods
