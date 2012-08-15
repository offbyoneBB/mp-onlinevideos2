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

#include "MPUrlSourceSplitter_Protocol_Mshs.h"
#include "MPUrlSourceSplitter_Protocol_Mshs_Parameters.h"
#include "Utilities.h"
#include "LockMutex.h"
#include "VersionInfo.h"

#include "base64.h"
#include "compress_zlib.h"
#include "formatUrl.h"
#include "conversions.h"

#include "MovieFragmentBox.h"
#include "TrackFragmentBox.h"
#include "TrackFragmentHeaderBox.h"
#include "BoxCollection.h"
#include "MovieHeaderBox.h"

// protocol implementation name
#ifdef _DEBUG
#define PROTOCOL_IMPLEMENTATION_NAME                                          L"MPUrlSourceSplitter_Protocol_Mshsd"
#else
#define PROTOCOL_IMPLEMENTATION_NAME                                          L"MPUrlSourceSplitter_Protocol_Mshs"
#endif

PIPlugin CreatePluginInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Protocol_Mshs(configuration);
}

void DestroyPluginInstance(PIPlugin pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Protocol_Mshs *pClass = (CMPUrlSourceSplitter_Protocol_Mshs *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Protocol_Mshs::CMPUrlSourceSplitter_Protocol_Mshs(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_PROTOCOL_MSHS, COMPILE_INFO_MPURLSOURCESPLITTER_PROTOCOL_MSHS);
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
  
  this->receiveDataTimeout = MSHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->filter = NULL;
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->wholeStreamDownloaded = false;
  this->mainCurlInstance = NULL;
  this->bytePosition = 0;
  this->seekingActive = false;
  this->supressData = false;
  this->bufferForBoxProcessingCollection = NULL;
  this->bufferForProcessing = NULL;
  this->shouldExit = false;
  this->streamFragments = NULL;
  this->videoCurlInstance = NULL;
  this->audioCurlInstance = NULL;
  this->streamingMedia = NULL;
  this->lastFragmentDownloaded = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Protocol_Mshs::~CMPUrlSourceSplitter_Protocol_Mshs()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  FREE_MEM_CLASS(this->videoCurlInstance);
  FREE_MEM_CLASS(this->audioCurlInstance);
  FREE_MEM_CLASS(this->mainCurlInstance);
  FREE_MEM_CLASS(this->bufferForBoxProcessingCollection);
  FREE_MEM_CLASS(this->bufferForProcessing);
  FREE_MEM_CLASS(this->configurationParameters);
  FREE_MEM_CLASS(this->streamFragments);
  FREE_MEM_CLASS(this->streamingMedia);

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

bool CMPUrlSourceSplitter_Protocol_Mshs::IsConnected(void)
{
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded) || ((this->videoCurlInstance != NULL) && (this->audioCurlInstance != NULL)));
}

unsigned int CMPUrlSourceSplitter_Protocol_Mshs::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::ParseUrl(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);
  CHECK_POINTER_DEFAULT_HRESULT(result, parameters);

  this->ClearSession();
  if (SUCCEEDED(result))
  {
    this->configurationParameters->Clear();
    ALLOC_MEM_DEFINE_SET(protocolConfiguration, ProtocolPluginConfiguration, 1, 0);
    if (protocolConfiguration != NULL)
    {
      protocolConfiguration->outputStream = this->filter;
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

void CMPUrlSourceSplitter_Protocol_Mshs::ReceiveData(bool *shouldExit)
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
        if ((this->videoCurlInstance != NULL) && (this->audioCurlInstance != NULL) &&
            (this->videoCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA) &&
            (this->audioCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
        {
          // we need to reconstruct header
          CTrackFragmentHeaderBox *videoTrackFragmentHeaderBox = this->GetTrackFragmentHeaderBox(this->videoCurlInstance->GetReceiveDataBuffer());
          CTrackFragmentHeaderBox *audioTrackFragmentHeaderBox = this->GetTrackFragmentHeaderBox(this->audioCurlInstance->GetReceiveDataBuffer());

          if ((videoTrackFragmentHeaderBox != NULL) && (audioTrackFragmentHeaderBox != NULL))
          {
            wchar_t *videoData = videoTrackFragmentHeaderBox->GetParsedHumanReadable(L"");
            wchar_t *audioData = audioTrackFragmentHeaderBox->GetParsedHumanReadable(L"");

            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: video track fragment header box:\n%s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, videoData);
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: audio track fragment header box:\n%s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, audioData);

            FREE_MEM(videoData);
            FREE_MEM(audioData);

            // create file type box
            CFileTypeBox *fileTypeBox = this->CreateFileTypeBox();

            if (fileTypeBox != NULL)
            {
              // copy file type box to processing
              if (this->bufferForBoxProcessingCollection != NULL)
              {
                CLinearBuffer *linearBuffer = this->bufferForBoxProcessingCollection->GetItem(this->bufferForBoxProcessingCollection->Count() - 1);
                if (linearBuffer != NULL)
                {
                  this->PutBoxIntoBuffer(fileTypeBox, linearBuffer);
                }
              }
            }
            FREE_MEM_CLASS(fileTypeBox);

            // create movie box
            CMovieBox *movieBox = this->GetMovieBox(this->streamingMedia, videoTrackFragmentHeaderBox, audioTrackFragmentHeaderBox);

            if (movieBox != NULL)
            {
              // copy movie box to processing
              if (this->bufferForBoxProcessingCollection != NULL)
              {
                CLinearBuffer *linearBuffer = this->bufferForBoxProcessingCollection->GetItem(this->bufferForBoxProcessingCollection->Count() - 1);
                if (linearBuffer != NULL)
                {
                  this->PutBoxIntoBuffer(movieBox, linearBuffer);
                }
              }
            }
            FREE_MEM_CLASS(movieBox);

            // copy data from video buffer and from audio buffer to processing
            if (this->bufferForBoxProcessingCollection != NULL)
            {
              CLinearBuffer *linearBuffer = this->bufferForBoxProcessingCollection->GetItem(this->bufferForBoxProcessingCollection->Count() - 1);
              if (linearBuffer != NULL)
              {
                {
                  unsigned int length = this->videoCurlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace();
                  ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
                  if (buffer != NULL)
                  {
                    this->videoCurlInstance->GetReceiveDataBuffer()->CopyFromBuffer(buffer, length, 0, 0);
                    linearBuffer->AddToBufferWithResize(buffer, length);
                    this->videoCurlInstance->GetReceiveDataBuffer()->RemoveFromBufferAndMove(length);
                  }
                  FREE_MEM(buffer);
                }

                {
                  unsigned int length = this->audioCurlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace();
                  ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
                  if (buffer != NULL)
                  {
                    this->audioCurlInstance->GetReceiveDataBuffer()->CopyFromBuffer(buffer, length, 0, 0);
                    linearBuffer->AddToBufferWithResize(buffer, length);
                  }
                  FREE_MEM(buffer);
                }
              }
            }
          }

          FREE_MEM_CLASS(videoTrackFragmentHeaderBox);
          FREE_MEM_CLASS(audioTrackFragmentHeaderBox);
        }

        if (this->mainCurlInstance != NULL)
        {
          unsigned int bytesRead = this->mainCurlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace();
          if (bytesRead != 0)
          {
            if (this->bufferForBoxProcessingCollection != NULL)
            {
              CLinearBuffer *linearBuffer = this->bufferForBoxProcessingCollection->GetItem(this->bufferForBoxProcessingCollection->Count() - 1);
              if (linearBuffer != NULL)
              {
                unsigned int bufferSize = linearBuffer->GetBufferSize();
                unsigned int freeSpace = linearBuffer->GetBufferFreeSpace();

                if (freeSpace < bytesRead)
                {
                  unsigned int bufferNewSize = max(bufferSize * 2, bufferSize + bytesRead);
                  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: buffer to small, buffer size: %d, new size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, bufferSize, bufferNewSize);
                  if (!linearBuffer->ResizeBuffer(bufferNewSize))
                  {
                    this->logger->Log(LOGGER_WARNING, L"%s: %s: resizing buffer unsuccessful, dropping received data", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
                    // error
                    bytesRead = 0;
                    // in case of error don't report end of stream
                    this->lastFragmentDownloaded = false;
                  }
                }

                if (bytesRead != 0)
                {
                  ALLOC_MEM_DEFINE_SET(buffer, unsigned char, bytesRead, 0);
                  if (buffer != NULL)
                  {
                    this->mainCurlInstance->GetReceiveDataBuffer()->CopyFromBuffer(buffer, bytesRead, 0, 0);
                    linearBuffer->AddToBuffer(buffer, bytesRead);
                    this->mainCurlInstance->GetReceiveDataBuffer()->RemoveFromBufferAndMove(bytesRead);
                  }
                  FREE_MEM(buffer);
                }
              }
            }
          }
        }
      }

      if ((!this->setLength) && (this->bytePosition != 0))
      {
        // adjust total length if not already set
        if (this->streamLength == 0)
        {
          // error occured or stream duration is not set
          // just make guess
          this->streamLength = LONGLONG(MINIMUM_RECEIVED_DATA_FOR_SPLITTER);
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting quess total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
          this->filter->SetTotalLength(this->streamLength, true);
        }
        else if ((this->bytePosition > (this->streamLength * 3 / 4)))
        {
          // it is time to adjust stream length, we are approaching to end but still we don't know total length
          this->streamLength = this->bytePosition * 2;
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: adjusting quess total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
          this->filter->SetTotalLength(this->streamLength, true);
        }
      }

      if ((!this->supressData) && (this->bufferForBoxProcessingCollection != NULL))
      {
        bool continueProcessing = false;
        unsigned int limit = this->lastFragmentDownloaded ? 0 : 1;

        while (this->bufferForBoxProcessingCollection->Count() > limit)
        {
          CLinearBuffer *bufferForBoxProcessing = this->bufferForBoxProcessingCollection->GetItem(0);
          do
          {
            continueProcessing = false;
            unsigned int length = bufferForBoxProcessing->GetBufferOccupiedSpace();
            if (length > 0)
            {
              ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
              if (buffer != NULL)
              {
                bufferForBoxProcessing->CopyFromBuffer(buffer, length, 0, 0);
                continueProcessing = true;

                if (continueProcessing)
                {
                  continueProcessing &= (this->bufferForProcessing->AddToBufferWithResize(buffer, length) == length);
                }

                if (continueProcessing)
                {
                  bufferForBoxProcessing->RemoveFromBufferAndMove(length);
                  continueProcessing = true;
                }
              }
              FREE_MEM(buffer);
            }
          } while (continueProcessing);

          if (bufferForBoxProcessing->GetBufferOccupiedSpace() == 0)
          {
            // all data are processed, remove buffer from collection
            this->bufferForBoxProcessingCollection->Remove(0);
            continueProcessing = true;
          }
        }

        // in case of error don't report end of stream
        this->lastFragmentDownloaded &= continueProcessing;
      }

      if ((!this->supressData) && (this->bufferForProcessing != NULL))
      {
        unsigned int length = this->bufferForProcessing->GetBufferOccupiedSpace();

        if (length > 0)
        {
          ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
          if (buffer != NULL)
          {
            this->bufferForProcessing->CopyFromBuffer(buffer, length, 0, 0);

            CMediaPacket *mediaPacket = new CMediaPacket();
            mediaPacket->GetBuffer()->InitializeBuffer(length);
            mediaPacket->GetBuffer()->AddToBuffer(buffer, length);
            mediaPacket->SetStart(this->bytePosition);
            mediaPacket->SetEnd(this->bytePosition + length - 1);

            HRESULT result = this->filter->PushMediaPacket(mediaPacket);
            if (FAILED(result))
            {
              this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet, error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);

              // in case of error don't report end of stream
              this->lastFragmentDownloaded = false;
            }
            this->bytePosition += length;

            FREE_MEM_CLASS(mediaPacket);
            this->seekingActive = false;
            this->bufferForProcessing->RemoveFromBufferAndMove(length);
          }

          FREE_MEM(buffer);
        }
      }

      if ((this->videoCurlInstance != NULL) && (this->audioCurlInstance != NULL) &&
        (this->videoCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA) &&
        (this->audioCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
      {
        // mark video and audio fragments as downloaded
        if (!this->streamFragments->GetStreamFragment(this->videoCurlInstance->GetUrl(), true)->GetDownloaded())
        {
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: received all data for url '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->videoCurlInstance->GetUrl());
          this->streamFragments->GetStreamFragment(this->videoCurlInstance->GetUrl(), true)->SetDownloaded(true);
        }
        if (!this->streamFragments->GetStreamFragment(this->audioCurlInstance->GetUrl(), true)->GetDownloaded())
        {
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: received all data for url '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->audioCurlInstance->GetUrl());
          this->streamFragments->GetStreamFragment(this->audioCurlInstance->GetUrl(), true)->SetDownloaded(true);
        }

        // all data for video and audio are received
        // remove audio CURL instance
        FREE_MEM_CLASS(this->audioCurlInstance);
        // move video CURL instance into main CURL instance and continue as in common case
        this->mainCurlInstance = this->videoCurlInstance;
        this->videoCurlInstance = NULL;
      }

      if (this->lastFragmentDownloaded)
      {
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
            this->filter->SetTotalLength(this->streamLength, false);
            this->setLength = true;
          }

          // notify filter the we reached end of stream
          // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
          this->filter->EndOfStreamReached(max(0, this->bytePosition - 1));
        }
      }

      if ((this->mainCurlInstance != NULL) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
      {
        // all data received, we're not receiving data
        if (!this->streamFragments->GetStreamFragment(this->mainCurlInstance->GetUrl(), true)->GetDownloaded())
        {
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: received all data for url '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->mainCurlInstance->GetUrl());
          this->streamFragments->GetStreamFragment(this->mainCurlInstance->GetUrl(), true)->SetDownloaded(true);
        }

        if (this->mainCurlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace() != 0)
        {
          // this should not happen, just for sure
          this->logger->Log(LOGGER_ERROR, L"%s: %s: still some data in CURL: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->mainCurlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace());
        }
        
        CStreamFragment *streamFragmentToDownload = this->GetFirstNotDownloadedStreamFragment();
        if (streamFragmentToDownload != NULL)
        {
          FREE_MEM_CLASS(this->mainCurlInstance);
          HRESULT result = S_OK;

          CLinearBuffer *buffer = new CLinearBuffer();
          CHECK_POINTER_HRESULT(result, buffer, result, E_OUTOFMEMORY);

          if (SUCCEEDED(result))
          {
            buffer->InitializeBuffer(MINIMUM_RECEIVED_DATA_FOR_SPLITTER);
            result = (this->bufferForBoxProcessingCollection->Add(buffer)) ? result : E_OUTOFMEMORY;
          }

          if (FAILED(result))
          {
            FREE_MEM_CLASS(buffer);
          }

          if (SUCCEEDED(result))
          {
            // we need to download for another url
            this->mainCurlInstance = new CHttpCurlInstance(this->logger, this->lockMutex, streamFragmentToDownload->GetUrl(), PROTOCOL_IMPLEMENTATION_NAME);
            CHECK_POINTER_HRESULT(result, this->mainCurlInstance, result, E_POINTER);

            if (SUCCEEDED(result))
            {
              this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
              this->mainCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_REFERER, true, NULL));
              this->mainCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_USER_AGENT, true, NULL));
              this->mainCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_COOKIE, true, NULL));
              this->mainCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_VERSION, true, HTTP_VERSION_DEFAULT));
              this->mainCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

              result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;

              if (SUCCEEDED(result))
              {
                // all parameters set
                // start receiving data

                result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
              }
            }
          }
        }
        else
        {
          // we are on last stream fragment, we received all data
          this->lastFragmentDownloaded = true;
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
        this->filter->SetTotalLength(this->streamLength, false);
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
}

// ISimpleProtocol interface

unsigned int CMPUrlSourceSplitter_Protocol_Mshs::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::StartReceivingData(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME);

  CHECK_POINTER_DEFAULT_HRESULT(result, this->configurationParameters);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  this->wholeStreamDownloaded = false;
  this->bytePosition = 0;
  this->streamLength = 0;
  this->setLength = false;

  if (this->streamFragments == NULL)
  {
    char *encoded = ConvertToMultiByteW(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_MANIFEST, true, NULL));
    unsigned char *compressedManifestDecoded = NULL;
    uint32_t compressedManifestDecodedLength = 0;
    result = base64_decode(encoded, &compressedManifestDecoded, &compressedManifestDecodedLength);

    if (SUCCEEDED(result))
    {
      // decompress manifest
      uint8_t *decompressedManifest = NULL;
      uint32_t decompressedLength = 0;

      result = decompress_zlib(compressedManifestDecoded, compressedManifestDecodedLength, &decompressedManifest, &decompressedLength);

      if (SUCCEEDED(result))
      {
        this->streamingMedia = new CMSHSSmoothStreamingMedia();
        CHECK_POINTER_HRESULT(result, this->streamingMedia, result, E_OUTOFMEMORY);

        if (SUCCEEDED(result))
        {
          result = (this->streamingMedia->Deserialize(decompressedManifest)) ? S_OK : HRESULT_FROM_WIN32(ERROR_INVALID_DATA);

          if (SUCCEEDED(result))
          {
            FREE_MEM(this->streamFragments);
            this->streamFragments = this->GetStreamFragmentsFromManifest(
              this->logger,
              METHOD_START_RECEIVING_DATA_NAME,
              this->configurationParameters,
              this->streamingMedia,
              true);
            CHECK_POINTER_HRESULT(result, this->streamFragments, result, E_POINTER);
          }
          else
          {
            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, L"cannot deserialize manifest");
          }
        }

        if (FAILED(result))
        {
          FREE_MEM_CLASS(this->streamingMedia);
        }
      }
      else
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, L"cannot decompress manifest");
      }
    }
    else
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, L"cannot decode manifest");
    }
  }

  if (SUCCEEDED(result))
  {
    if (this->bufferForBoxProcessingCollection == NULL)
    {
      this->bufferForBoxProcessingCollection = new CLinearBufferCollection();
    }
    CHECK_POINTER_HRESULT(result, this->bufferForBoxProcessingCollection, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      CLinearBuffer *buffer = new CLinearBuffer();
      CHECK_POINTER_HRESULT(result, buffer, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        buffer->InitializeBuffer(MINIMUM_RECEIVED_DATA_FOR_SPLITTER);
        result = (this->bufferForBoxProcessingCollection->Add(buffer)) ? result : E_OUTOFMEMORY;
      }

      if (FAILED(result))
      {
        FREE_MEM_CLASS(buffer);
      }
    }
  }

  if (SUCCEEDED(result))
  {
    this->bufferForProcessing = new CLinearBuffer();
    CHECK_POINTER_HRESULT(result, this->bufferForProcessing, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      result = (this->bufferForProcessing->InitializeBuffer(MINIMUM_RECEIVED_DATA_FOR_SPLITTER)) ? result : E_FAIL;
    }
  }

  if (SUCCEEDED(result) && (!this->seekingActive))
  {
    // start downloading first video and first audio stream fragments
    // both are needed for header reconstruction

    CStreamFragment *videoStreamFragment = NULL;
    CStreamFragment *audioStreamFragment = NULL;

    // find first not downloaded video and audio streams
    unsigned int i = 0;
    while (((videoStreamFragment == NULL) || (audioStreamFragment == NULL)) && (i < this->streamFragments->Count()))
    {
      CStreamFragment *fragment = this->streamFragments->GetItem(i);

      if ((videoStreamFragment == NULL) && (fragment->GetFragmentType() == FRAGMENT_TYPE_VIDEO))
      {
        videoStreamFragment = fragment;
      }

      if ((audioStreamFragment == NULL) && (fragment->GetFragmentType() == FRAGMENT_TYPE_AUDIO))
      {
        audioStreamFragment = fragment;
      }

      i++;
    }

    CHECK_POINTER_HRESULT(result, videoStreamFragment, result, E_POINTER);
    CHECK_POINTER_HRESULT(result, audioStreamFragment, result, E_POINTER);

    if (SUCCEEDED(result))
    {
      this->videoCurlInstance = new CHttpCurlInstance(this->logger, this->lockMutex, videoStreamFragment->GetUrl(), PROTOCOL_IMPLEMENTATION_NAME);
      this->audioCurlInstance = new CHttpCurlInstance(this->logger, this->lockMutex, audioStreamFragment->GetUrl(), PROTOCOL_IMPLEMENTATION_NAME);

      CHECK_POINTER_HRESULT(result, this->videoCurlInstance, result, E_POINTER);
      CHECK_POINTER_HRESULT(result, this->audioCurlInstance, result, E_POINTER);
    }

    if (SUCCEEDED(result))
    {
      this->videoCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
      this->videoCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_REFERER, true, NULL));
      this->videoCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_USER_AGENT, true, NULL));
      this->videoCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_COOKIE, true, NULL));
      this->videoCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_VERSION, true, HTTP_VERSION_DEFAULT));
      this->videoCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

      this->audioCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
      this->audioCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_REFERER, true, NULL));
      this->audioCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_USER_AGENT, true, NULL));
      this->audioCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_COOKIE, true, NULL));
      this->audioCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_VERSION, true, HTTP_VERSION_DEFAULT));
      this->audioCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

      result = (this->videoCurlInstance->Initialize()) ? result : E_FAIL;
      result = (this->audioCurlInstance->Initialize()) ? result : E_FAIL;
    }

    if (SUCCEEDED(result))
    {
      // all parameters set
      // start receiving data

      result = (this->videoCurlInstance->StartReceivingData()) ? result : E_FAIL;
      result = (this->audioCurlInstance->StartReceivingData()) ? result : E_FAIL;
    }

    if (SUCCEEDED(result))
    {
      // wait for HTTP status code

      long responseCode = 0;
      while ((responseCode == 0) && SUCCEEDED(result))
      {
        // check state of video CURL instance
        CURLcode errorCode = this->videoCurlInstance->GetResponseCode(&responseCode);
        if (errorCode == CURLE_OK)
        {
          if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
          {
            // response code 200 - 299 = OK
            // response code 300 - 399 = redirect (OK)
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: video fragment HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
            result = E_FAIL;
          }
        }
        else
        {
          this->videoCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
          result = E_FAIL;
          break;
        }

        if ((responseCode == 0) && (this->videoCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
        {
          // we received data too fast
          result = E_FAIL;
          break;
        }

        // wait some time
        Sleep(1);
      }

      responseCode = 0;
      while ((responseCode == 0) && (SUCCEEDED(result)))
      {
        // check state of audio CURL instance
        CURLcode errorCode = this->audioCurlInstance->GetResponseCode(&responseCode);
        if (errorCode == CURLE_OK)
        {
          if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
          {
            // response code 200 - 299 = OK
            // response code 300 - 399 = redirect (OK)
            this->logger->Log(LOGGER_VERBOSE, L"%s: %s: audio fragment HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
            result = E_FAIL;
          }
        }
        else
        {
          this->audioCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
          result = E_FAIL;
          break;
        }

        if ((responseCode == 0) && (this->audioCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
        {
          // we received data too fast
          result = E_FAIL;
          break;
        }

        // wait some time
        Sleep(1);
      }
    }

    if (FAILED(result))
    {
      // free video and audio CURL instances - not needed because of error
      FREE_MEM_CLASS(this->videoCurlInstance);
      FREE_MEM_CLASS(this->audioCurlInstance);
    }
  }
  else if (SUCCEEDED(result))
  {
    // common situation, just download first not downloaded stream fragment
    CStreamFragment *streamFragmentToDownload = this->GetFirstNotDownloadedStreamFragment();
    this->mainCurlInstance = new CHttpCurlInstance(this->logger, this->lockMutex, streamFragmentToDownload->GetUrl(), PROTOCOL_IMPLEMENTATION_NAME);
    CHECK_POINTER_HRESULT(result, this->mainCurlInstance, result, E_POINTER);

    if (SUCCEEDED(result))
    {
      this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
      this->mainCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_REFERER, true, NULL));
      this->mainCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_USER_AGENT, true, NULL));
      this->mainCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_MSHS_COOKIE, true, NULL));
      this->mainCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_VERSION, true, HTTP_VERSION_DEFAULT));
      this->mainCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

      result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;

      if (SUCCEEDED(result))
      {
        // all parameters set
        // start receiving data

        result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
      }

      if (SUCCEEDED(result))
      {
        // wait for HTTP status code

        long responseCode = 0;
        while (responseCode == 0)
        {
          CURLcode errorCode = this->mainCurlInstance->GetResponseCode(&responseCode);
          if (errorCode == CURLE_OK)
          {
            if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
            {
              // response code 200 - 299 = OK
              // response code 300 - 399 = redirect (OK)
              this->logger->Log(LOGGER_VERBOSE, L"%s: %s: HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
              result = E_FAIL;
            }
          }
          else
          {
            this->mainCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
            result = E_FAIL;
            break;
          }

          if ((responseCode == 0) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
          {
            // we received data too fast
            result = E_FAIL;
            break;
          }

          // wait some time
          Sleep(1);
        }
      }
    }
  }

  if (FAILED(result))
  {
    this->StopReceivingData();
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, result);
  return result;
}

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::StopReceivingData(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  FREE_MEM_CLASS(this->videoCurlInstance);
  FREE_MEM_CLASS(this->audioCurlInstance);

  if (this->mainCurlInstance != NULL)
  {
    this->mainCurlInstance->SetCloseWithoutWaiting(this->seekingActive);
    FREE_MEM_CLASS(this->mainCurlInstance);
  }

  FREE_MEM_CLASS(this->bufferForBoxProcessingCollection);
  FREE_MEM_CLASS(this->bufferForProcessing);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
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
  
HRESULT CMPUrlSourceSplitter_Protocol_Mshs::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
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

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  FREE_MEM_CLASS(this->bufferForBoxProcessingCollection);
  FREE_MEM_CLASS(this->bufferForProcessing);
  FREE_MEM_CLASS(this->streamingMedia);
 
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->wholeStreamDownloaded = false;
  this->receiveDataTimeout = MSHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->bytePosition = 0;
  this->shouldExit = false;
  this->lastFragmentDownloaded = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

// ISeeking interface

unsigned int CMPUrlSourceSplitter_Protocol_Mshs::GetSeekingCapabilities(void)
{
  return SEEKING_METHOD_TIME;
}

int64_t CMPUrlSourceSplitter_Protocol_Mshs::SeekToTime(int64_t time)
{
  CLockMutex lock(this->lockMutex, INFINITE);

  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, time);

  int64_t result = -1;

  this->seekingActive = true;

  // close connection
  this->StopReceivingData();

  // MSHS protocol can seek to ms
  // time is in ms

  this->lastFragmentDownloaded = false;
  unsigned int fragmentToDownload = UINT_MAX;
  // find fragment to download
  if (this->streamFragments != NULL)
  {
    for (unsigned int i = 0; i < this->streamFragments->Count(); i++)
    {
      CStreamFragment *fragment = this->streamFragments->GetItem(i);

      if ((fragment->GetFragmentType() == FRAGMENT_TYPE_VIDEO) && (fragment->GetFragmentTime() <= (uint64_t)time))
      {
        fragmentToDownload = i;
        result = fragment->GetFragmentTime();
      }
    }

    for (unsigned int i = 0; i < fragmentToDownload; i++)
    {
      // mark all previous fragments as downloaded
      this->streamFragments->GetItem(i)->SetDownloaded(true);
    }
    for (unsigned int i = fragmentToDownload; i < this->streamFragments->Count(); i++)
    {
      // mark all other fragments as not downloaded
      this->streamFragments->GetItem(i)->SetDownloaded(false);
    }
  }

  // in fragmentToDownload is id of fragment to download
  CStreamFragment *fragment = this->streamFragments->GetItem(fragmentToDownload);

  if (fragment != NULL)
  {
    this->logger->Log(LOGGER_VERBOSE, L"%s: %s: url '%s', timestamp %lld", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME,
      fragment->GetUrl(), fragment->GetFragmentTime());

    // reopen connection
    // StartReceivingData() reset wholeStreamDownloaded
    this->StartReceivingData(NULL);

    if (!this->IsConnected())
    {
      result = -1;
    }
    else
    {
      this->streamTime = result;
    }
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, result);
  return result;
}

int64_t CMPUrlSourceSplitter_Protocol_Mshs::SeekToPosition(int64_t start, int64_t end)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from position: %llu, to position: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  return result;
}

void CMPUrlSourceSplitter_Protocol_Mshs::SetSupressData(bool supressData)
{
  this->supressData = supressData;
}

// IPlugin interface

const wchar_t *CMPUrlSourceSplitter_Protocol_Mshs::GetName(void)
{
  return PROTOCOL_NAME;
}

GUID CMPUrlSourceSplitter_Protocol_Mshs::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

HRESULT CMPUrlSourceSplitter_Protocol_Mshs::Initialize(PluginConfiguration *configuration)
{
  if (configuration == NULL)
  {
    return E_POINTER;
  }

  ProtocolPluginConfiguration *protocolConfiguration = (ProtocolPluginConfiguration *)configuration;
  this->logger->SetParameters(protocolConfiguration->configuration);
  this->filter = protocolConfiguration->outputStream;
  if (this->filter == NULL)
  {
    return E_POINTER;
  }

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

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_RECEIVE_DATA_TIMEOUT, true, MSHS_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? MSHS_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return S_OK;
}

// other methods

void CMPUrlSourceSplitter_Protocol_Mshs::RemoveAllDownloadedStreamFragments(void)
{
  if (this->streamFragments->Count() > 1)
  {
    unsigned int i = 0;
    while (i < (this->streamFragments->Count() - 1))
    {
      if (this->streamFragments->GetItem(i)->GetDownloaded())
      {
        this->streamFragments->Remove(i);
      }
      else
      {
        i++;
      }
    }
  }
}

CStreamFragment *CMPUrlSourceSplitter_Protocol_Mshs::GetFirstNotDownloadedStreamFragment(void)
{
  CStreamFragment *result = NULL;

  for (unsigned int i = 0; i < this->streamFragments->Count(); i++)
  {
    if (!this->streamFragments->GetItem(i)->GetDownloaded())
    {
      result = this->streamFragments->GetItem(i);
      break;
    }
  }

  return result;
}

CStreamFragmentCollection *CMPUrlSourceSplitter_Protocol_Mshs::GetStreamFragmentsFromManifest(CLogger *logger, const wchar_t *methodName, CParameterCollection *configurationParameters, CMSHSSmoothStreamingMedia *manifest, bool logCollection)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, logger);
  CHECK_POINTER_DEFAULT_HRESULT(result, methodName);
  CHECK_POINTER_DEFAULT_HRESULT(result, configurationParameters);
  CHECK_POINTER_DEFAULT_HRESULT(result, manifest);

  CStreamFragmentCollection *streamFragments = NULL;
  if (SUCCEEDED(result))
  {
    streamFragments = new CStreamFragmentCollection();
    CHECK_POINTER_HRESULT(result, streamFragments, result, E_OUTOFMEMORY);
  }

  if (SUCCEEDED(result))
  {
    uint32_t videoIndex = 0;
    uint32_t audioIndex = 0;
    uint64_t lastTimestamp = 0;
    uint32_t maxVideoIndex = 0;
    uint32_t maxAudioIndex = 0;

    // get maximum video and audio indexes
    for (unsigned int i = 0; i < manifest->GetStreams()->Count(); i++)
    {
      CMSHSStream *stream = manifest->GetStreams()->GetItem(i);

      if (stream->IsVideo())
      {
        maxVideoIndex = stream->GetStreamFragments()->Count();
      }
      else if (stream->IsAudio())
      {
        maxAudioIndex = stream->GetStreamFragments()->Count();
      }
    }

    const wchar_t *videoUrlPattern = NULL;
    const wchar_t *audioUrlPattern = NULL;
    CMSHSTrack *videoTrack = NULL;
    CMSHSTrack *audioTrack = NULL;
    const wchar_t *baseUrl = configurationParameters->GetValue(PARAMETER_NAME_MSHS_BASE_URL, true, NULL);

    while ((videoIndex < maxVideoIndex) || (audioIndex < maxAudioIndex))
    {
      // there is still some fragment to add to stream fragments
      // choose fragment which is nearest to last timestamp

      CMSHSStreamFragment *videoFragment = NULL;
      CMSHSStreamFragment *audioFragment = NULL;

      for (unsigned int i = 0; i < manifest->GetStreams()->Count(); i++)
      {
        CMSHSStream *stream = manifest->GetStreams()->GetItem(i);

        if (stream->IsVideo() && (videoIndex < maxVideoIndex))
        {
          videoTrack = stream->GetTracks()->GetItem(0);
          videoUrlPattern = stream->GetUrl();
          videoFragment = stream->GetStreamFragments()->GetItem(videoIndex);
        }
        else if (stream->IsAudio() && (audioIndex < maxAudioIndex))
        {
          audioTrack = stream->GetTracks()->GetItem(0);
          audioUrlPattern = stream->GetUrl();
          audioFragment = stream->GetStreamFragments()->GetItem(audioIndex);
        }
      }

      wchar_t *url = NULL;
      uint64_t fragmentTime = 0;
      uint64_t fragmentDuration = 0;
      unsigned int fragmentType = FRAGMENT_TYPE_UNSPECIFIED;

      if (SUCCEEDED(result))
      {
        if ((videoFragment != NULL) && (audioFragment != NULL))
        {
          uint64_t videoDiff = videoFragment->GetFragmentTime() - lastTimestamp;
          uint64_t audioDiff = audioFragment->GetFragmentTime() - lastTimestamp;

          if (videoDiff <= audioDiff)
          {
            fragmentTime = videoFragment->GetFragmentTime();
            fragmentDuration = videoFragment->GetFragmentDuration();
            fragmentType = FRAGMENT_TYPE_VIDEO;
            url = this->FormatUrl(baseUrl, videoUrlPattern, videoTrack, videoFragment);
            videoIndex++;
          }
          else if (audioDiff < videoDiff)
          {
            fragmentTime = audioFragment->GetFragmentTime();
            fragmentDuration = audioFragment->GetFragmentDuration();
            fragmentType = FRAGMENT_TYPE_AUDIO;
            url = this->FormatUrl(baseUrl, audioUrlPattern, audioTrack, audioFragment);
            audioIndex++;
          }
        }
        else if (videoFragment != NULL)
        {
          fragmentTime = videoFragment->GetFragmentTime();
          fragmentDuration = videoFragment->GetFragmentDuration();
          fragmentType = FRAGMENT_TYPE_VIDEO;
          url = this->FormatUrl(baseUrl, videoUrlPattern, videoTrack, videoFragment);
          videoIndex++;
        }
        else if (audioFragment != NULL)
        {
          fragmentTime = audioFragment->GetFragmentTime();
          fragmentDuration = audioFragment->GetFragmentDuration();
          fragmentType = FRAGMENT_TYPE_AUDIO;
          url = this->FormatUrl(baseUrl, audioUrlPattern, audioTrack, audioFragment);
          audioIndex++;
        }
        else
        {
          // bad case, this should not happen
          logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, methodName, L"no audio or video fragment to process");
          result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
      }

      if (SUCCEEDED(result))
      {
        lastTimestamp = fragmentTime;

        CStreamFragment *streamFragment = new CStreamFragment(url, fragmentDuration, fragmentTime, fragmentType);
        CHECK_POINTER_HRESULT(result, streamFragment, result, E_OUTOFMEMORY);

        // add stream fragment to stream fragments
        if (SUCCEEDED(result))
        {
          result = (streamFragments->Add(streamFragment)) ? S_OK : E_FAIL;
        }

        if (FAILED(result))
        {
          FREE_MEM_CLASS(streamFragment);
        }
      }

      FREE_MEM(url);
    }

    result = (streamFragments->Count() > 0) ? result : E_FAIL;

    /*if (SUCCEEDED(result) && (logCollection))
    {
      wchar_t *streamFragmentLog = NULL;
      for (unsigned int i = 0; i < streamFragments->Count(); i++)
      {
        CStreamFragment *streamFragment = streamFragments->GetItem(i);

        wchar_t *temp = FormatString(L"%s%surl '%s', timestamp: %llu", (i == 0) ? L"" : streamFragmentLog, (i == 0) ? L"" : L"\n", streamFragment->GetUrl(), streamFragment->GetFragmentTime());
        FREE_MEM(streamFragmentLog);
        streamFragmentLog = temp;
      }

      if (streamFragmentLog != NULL)
      {
        logger->Log(LOGGER_VERBOSE, L"%s: %s: stream fragments:\n%s", PROTOCOL_IMPLEMENTATION_NAME, methodName, streamFragmentLog);
      }

      FREE_MEM(streamFragmentLog);
    }*/
  }

  if (FAILED(result))
  {
    FREE_MEM_CLASS(streamFragments);
  }

  return streamFragments;
}

wchar_t *CMPUrlSourceSplitter_Protocol_Mshs::FormatUrl(const wchar_t *baseUrl, const wchar_t *urlPattern, CMSHSTrack *track, CMSHSStreamFragment *fragment)
{
  wchar_t *result = NULL;

  if ((baseUrl != NULL) && (urlPattern != NULL) && (track != NULL) && (fragment != NULL))
  {
    // in url pattern replace {bitrate} or {Bitrate} with track bitrate bitrate
    // in url pattern replace {start time} or {Start time} with fragment time

    wchar_t *bitrate = FormatString(L"%u", track->GetBitrate());
    wchar_t *startTime = FormatString(L"%llu", fragment->GetFragmentTime());

    if ((bitrate != NULL) && (startTime != NULL))
    {
      wchar_t *replaced1 = ReplaceString(urlPattern, L"{bitrate}", bitrate);
      wchar_t *replaced2 = ReplaceString(replaced1, L"{Bitrate}", bitrate);

      wchar_t *replaced3 = ReplaceString(replaced2, L"{start time}", startTime);
      wchar_t *replaced4 = ReplaceString(replaced3, L"{Start time}", startTime);

      result = FormatAbsoluteUrl(baseUrl, replaced4);

      FREE_MEM(replaced1);
      FREE_MEM(replaced2);
      FREE_MEM(replaced3);
      FREE_MEM(replaced4);
    }

    FREE_MEM(bitrate);
    FREE_MEM(startTime);
  }

  return result;
}

CFileTypeBox *CMPUrlSourceSplitter_Protocol_Mshs::CreateFileTypeBox(void)
{
  bool continueCreating = true;
  CFileTypeBox *fileTypeBox = new CFileTypeBox();
  continueCreating &= (fileTypeBox != NULL);

  if (continueCreating)
  {
    continueCreating &= fileTypeBox->GetMajorBrand()->SetBrandString(L"isml");
  }

  if (continueCreating)
  {
    fileTypeBox->SetMinorVersion(512);

    CBrand *brand = new CBrand();
    continueCreating &= (brand != NULL);

    if (continueCreating)
    {
      continueCreating &= brand->SetBrandString(L"piff");
      if (continueCreating)
      {
        continueCreating &= fileTypeBox->GetCompatibleBrands()->Add(brand);
      }
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(brand);
    }

    if (continueCreating)
    {
      brand = new CBrand();
      continueCreating &= (brand != NULL);

      if (continueCreating)
      {
        continueCreating &= brand->SetBrandString(L"iso2");
        if (continueCreating)
        {
          continueCreating &= fileTypeBox->GetCompatibleBrands()->Add(brand);
        }
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(brand);
      }
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(fileTypeBox);
  }

  return fileTypeBox;
}

CTrackFragmentHeaderBox *CMPUrlSourceSplitter_Protocol_Mshs::GetTrackFragmentHeaderBox(CLinearBuffer *buffer)
{
  CTrackFragmentHeaderBox *result = NULL;
  unsigned int bytesRead = buffer->GetBufferOccupiedSpace();

  if (bytesRead != 0)
  {
    ALLOC_MEM_DEFINE_SET(tempBuffer, unsigned char, bytesRead, 0);

    if (tempBuffer != NULL)
    {
      buffer->CopyFromBuffer(tempBuffer, bytesRead, 0, 0);

      CMovieFragmentBox *movieFragmentBox = new CMovieFragmentBox();

      if (movieFragmentBox != NULL)
      {
        if (movieFragmentBox->Parse(tempBuffer, bytesRead))
        {
          for (unsigned int i = 0; ((result == NULL) && (i < movieFragmentBox->GetBoxes()->Count())); i++)
          {
            CBox *trackFragmentBox = movieFragmentBox->GetBoxes()->GetItem(i);

            if (trackFragmentBox->IsType(TRACK_FRAGMENT_BOX_TYPE))
            {
              for (unsigned int j = 0; ((result == NULL) && (j < trackFragmentBox->GetBoxes()->Count())); j++)
              {
                CBox *trackFragmentHeaderBox = trackFragmentBox->GetBoxes()->GetItem(j);

                if (trackFragmentHeaderBox->IsType(TRACK_FRAGMENT_HEADER_BOX_TYPE))
                {
                  // we found video track fragment header box
                  // we can't return reference because movie fragment box is container and will be destroyed
                  // we can save track fragment header box into buffer and then create track fragment header box from buffer

                  uint32_t trackFragmentHeaderBoxSize = (uint32_t)trackFragmentHeaderBox->GetBoxSize();
                  if (trackFragmentHeaderBoxSize != 0)
                  {
                    ALLOC_MEM_DEFINE_SET(trackFragmentHeaderBoxBuffer, uint8_t, trackFragmentHeaderBoxSize, 0);
                    if (trackFragmentHeaderBoxBuffer != NULL)
                    {
                      if (trackFragmentHeaderBox->GetBox(trackFragmentHeaderBoxBuffer, trackFragmentHeaderBoxSize))
                      {
                        result = new CTrackFragmentHeaderBox();
                        if (result != NULL)
                        {
                          if (!result->Parse(trackFragmentHeaderBoxBuffer, trackFragmentHeaderBoxSize))
                          {
                            FREE_MEM_CLASS(result);
                          }
                        }
                      }
                    }
                    FREE_MEM(trackFragmentHeaderBoxBuffer);
                  }
                }
              }
            }
          }
        }
      }

      FREE_MEM_CLASS(movieFragmentBox);
    }

    FREE_MEM(tempBuffer);
  }

  return result;
}

bool CMPUrlSourceSplitter_Protocol_Mshs::PutBoxIntoBuffer(CBox *box, CLinearBuffer *buffer)
{
  bool result = false;

  if ((box != NULL) && (buffer != NULL))
  {
    // copy box to buffer
    uint32_t boxBufferLength = (uint32_t)box->GetBoxSize();
    if (boxBufferLength != 0)
    {
      ALLOC_MEM_DEFINE_SET(boxBuffer, unsigned char, boxBufferLength, 0);

      if (boxBuffer != NULL)
      {
        if (box->GetBox(boxBuffer, boxBufferLength))
        {
          result = (buffer->AddToBufferWithResize(boxBuffer, boxBufferLength) == boxBufferLength);
        }
      }
      FREE_MEM(boxBuffer);
    }
  }

  return result;
}

CMovieBox *CMPUrlSourceSplitter_Protocol_Mshs::GetMovieBox(CMSHSSmoothStreamingMedia *media, CTrackFragmentHeaderBox *videoFragmentHeaderBox, CTrackFragmentHeaderBox *audioFragmentHeaderBox)
{
  CMovieBox *movieBox = NULL;
  bool continueCreating = ((media != NULL) && (videoFragmentHeaderBox != NULL) && (audioFragmentHeaderBox != NULL));

  if (continueCreating)
  {
    movieBox = new CMovieBox();
    continueCreating &= (movieBox != NULL);

    // add movie header box
    if (continueCreating)
    {
      CMovieHeaderBox *movieHeaderBox = new CMovieHeaderBox();
      continueCreating &= (movieHeaderBox != NULL);

      if (continueCreating)
      {
        // set time scale by manifest
        movieHeaderBox->SetTimeScale((uint32_t)media->GetTimeScale());

        // set next track ID to higher value from video and audio
        movieHeaderBox->SetNextTrackId(
          (videoFragmentHeaderBox->GetTrackId() < audioFragmentHeaderBox->GetTrackId()) ?
          audioFragmentHeaderBox->GetTrackId() : videoFragmentHeaderBox->GetTrackId());

        movieHeaderBox->GetRate()->SetIntegerPart(1);
        movieHeaderBox->GetVolume()->SetIntegerPart(1);

        if (continueCreating)
        {
          continueCreating &= movieBox->GetBoxes()->Add(movieHeaderBox);
        }
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(movieHeaderBox);
      }
    }

    unsigned int videoStreamIndex = 0;
    unsigned int audioStreamIndex = 0;
    unsigned int trackIndex = 0;

    for (unsigned int i = 0; i < media->GetStreams()->Count(); i++)
    {
      CMSHSStream *stream = media->GetStreams()->GetItem(i);

      if (stream->IsVideo())
      {
        videoStreamIndex = i;
        break;
      }
    }

    for (unsigned int i = 0; i < media->GetStreams()->Count(); i++)
    {
      CMSHSStream *stream = media->GetStreams()->GetItem(i);

      if (stream->IsAudio())
      {
        audioStreamIndex = i;
        break;
      }
    }

    // add track box (video or audio - depends on track ID)
    if (continueCreating)
    {
      CTrackBox *trackBox = 
        (videoFragmentHeaderBox->GetTrackId() < audioFragmentHeaderBox->GetTrackId()) ? 
        this->GetVideoTrackBox(media, videoStreamIndex, trackIndex, videoFragmentHeaderBox) : this->GetAudioTrackBox(media, audioStreamIndex, trackIndex, audioFragmentHeaderBox);
      continueCreating &= (trackBox != NULL);

      if (continueCreating)
      {
        continueCreating &= movieBox->GetBoxes()->Add(trackBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(trackBox);
      }
    }

    // add track box (video or audio - depends on track ID)
    if (continueCreating)
    {
      CTrackBox *trackBox = 
        (videoFragmentHeaderBox->GetTrackId() < audioFragmentHeaderBox->GetTrackId()) ? 
        this->GetAudioTrackBox(media, audioStreamIndex, trackIndex, audioFragmentHeaderBox) : this->GetVideoTrackBox(media, videoStreamIndex, trackIndex, videoFragmentHeaderBox);
      continueCreating &= (trackBox != NULL);

      if (continueCreating)
      {
        continueCreating &= movieBox->GetBoxes()->Add(trackBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(trackBox);
      }
    }

    // add movie extends box
    if (continueCreating)
    {
      CMovieExtendsBox *movieExtendsBox = new CMovieExtendsBox();
      continueCreating &= (movieExtendsBox != NULL);

      // add track extends box (video or audio - depends on tack ID)
      if (continueCreating)
      {
        CTrackExtendsBox *trackExtendsBox = new CTrackExtendsBox();
        continueCreating &= (trackExtendsBox != NULL);

        if (continueCreating)
        {
          trackExtendsBox->SetTrackId(
            (videoFragmentHeaderBox->GetTrackId() < audioFragmentHeaderBox->GetTrackId()) ? 
            videoFragmentHeaderBox->GetTrackId() : audioFragmentHeaderBox->GetTrackId());

          trackExtendsBox->SetDefaultSampleDescriptionIndex(1);
        }

        if (continueCreating)
        {
          continueCreating &= movieExtendsBox->GetBoxes()->Add(trackExtendsBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(trackExtendsBox);
        }
      }

      // add track extends box (video or audio - depends on tack ID)
      if (continueCreating)
      {
        CTrackExtendsBox *trackExtendsBox = new CTrackExtendsBox();
        continueCreating &= (trackExtendsBox != NULL);

        if (continueCreating)
        {
          trackExtendsBox->SetTrackId(
            (videoFragmentHeaderBox->GetTrackId() < audioFragmentHeaderBox->GetTrackId()) ? 
            audioFragmentHeaderBox->GetTrackId() : videoFragmentHeaderBox->GetTrackId());

          trackExtendsBox->SetDefaultSampleDescriptionIndex(1);
        }

        if (continueCreating)
        {
          continueCreating &= movieExtendsBox->GetBoxes()->Add(trackExtendsBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(trackExtendsBox);
        }
      }

      if (continueCreating)
      {
        continueCreating &= movieBox->GetBoxes()->Add(movieExtendsBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(movieExtendsBox);
      }
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(movieBox);
    }
  }

  return movieBox;
}

CTrackBox *CMPUrlSourceSplitter_Protocol_Mshs::GetVideoTrackBox(CMSHSSmoothStreamingMedia *media, unsigned int streamIndex, unsigned int trackIndex, CTrackFragmentHeaderBox *fragmentHeaderBox)
{
  CTrackBox *trackBox = NULL;
  bool continueCreating = ((media != NULL) && (fragmentHeaderBox != NULL));

  if (continueCreating)
  {
    trackBox = new CTrackBox();
    continueCreating &= (trackBox != NULL);
  }

  CMSHSStream *stream = media->GetStreams()->GetItem(streamIndex);
  continueCreating &= (stream != NULL);

  CMSHSTrack *track = NULL;
  if (continueCreating)
  {
    track = stream->GetTracks()->GetItem(trackIndex);
  }
  continueCreating &= (track != NULL);

  // add track header box
  if (continueCreating)
  {
    CTrackHeaderBox *trackHeaderBox = new CTrackHeaderBox();
    continueCreating &= (trackHeaderBox != NULL);

    if (continueCreating)
    {
      // set flags, track ID, duration, width and height
      // set version to 1 (uint(64))
      trackHeaderBox->SetFlags(0x0000000F);
      trackHeaderBox->SetTrackId(fragmentHeaderBox->GetTrackId());
      trackHeaderBox->SetDuration(media->GetDuration());
      trackHeaderBox->SetVersion(1);

      trackHeaderBox->GetWidth()->SetIntegerPart(track->GetMaxWidth());
      trackHeaderBox->GetHeight()->SetIntegerPart(track->GetMaxHeight());
    }

    if (continueCreating)
    {
      continueCreating &= trackBox->GetBoxes()->Add(trackHeaderBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(trackHeaderBox);
    }
  }

  // add media box
  if (continueCreating)
  {
    CMediaBox *mediaBox = new CMediaBox();
    continueCreating &= (mediaBox != NULL);

    // add media header box
    if (continueCreating)
    {
      CMediaHeaderBox *mediaHeaderBox = new CMediaHeaderBox();
      continueCreating &= (mediaHeaderBox != NULL);

      if (continueCreating)
      {
        // set version (1 = uint(64)), time scale from manifest, duration
        mediaHeaderBox->SetVersion(1);
        mediaHeaderBox->SetTimeScale((uint32_t)media->GetTimeScale());
        mediaHeaderBox->SetDuration(media->GetDuration());
      }

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(mediaHeaderBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(mediaHeaderBox);
      }
    }

    // add handler box
    if (continueCreating)
    {
      CHandlerBox *handlerBox = this->GetHandlerBox(HANDLER_TYPE_VIDEO, L"VideoHandler");
      continueCreating &= (handlerBox != NULL);

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(handlerBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(handlerBox);
      }
    }

    // add media information box
    if (continueCreating)
    {
      CMediaInformationBox *mediaInformationBox = new CMediaInformationBox(HANDLER_TYPE_VIDEO);
      continueCreating &= (mediaInformationBox != NULL);

      // add video media header box
      if (continueCreating)
      {
        CVideoMediaHeaderBox *videoMediaHeaderBox = new CVideoMediaHeaderBox();
        continueCreating &= (videoMediaHeaderBox != NULL);

        if (continueCreating)
        {
          videoMediaHeaderBox->SetFlags(0x00000001);
        }

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(videoMediaHeaderBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(videoMediaHeaderBox);
        }
      }

      // add data information box
      if (continueCreating)
      {
        CDataInformationBox *dataInformationBox = this->GetDataInformationBox();
        continueCreating &= (dataInformationBox != NULL);

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(dataInformationBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(dataInformationBox);
        }
      }

      // add samle table box
      if (continueCreating)
      {
        CSampleTableBox *sampleTableBox = this->GetVideoSampleTableBox(media, streamIndex, trackIndex, fragmentHeaderBox);
        continueCreating &= (sampleTableBox != NULL);

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(sampleTableBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(sampleTableBox);
        }
      }

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(mediaInformationBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(mediaInformationBox);
      }
    }

    if (continueCreating)
    {
      continueCreating &= trackBox->GetBoxes()->Add(mediaBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(mediaBox);
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(trackBox);
  }

  return trackBox;
}

CTrackBox *CMPUrlSourceSplitter_Protocol_Mshs::GetAudioTrackBox(CMSHSSmoothStreamingMedia *media, unsigned int streamIndex, unsigned int trackIndex, CTrackFragmentHeaderBox *fragmentHeaderBox)
{
  CTrackBox *trackBox = NULL;
  bool continueCreating = ((media != NULL) && (fragmentHeaderBox != NULL));

  if (continueCreating)
  {
    trackBox = new CTrackBox();
    continueCreating &= (trackBox != NULL);
  }

  // add track header box
  if (continueCreating)
  {
    CTrackHeaderBox *trackHeaderBox = new CTrackHeaderBox();
    continueCreating &= (trackHeaderBox != NULL);

    if (continueCreating)
    {
      // set flags, track ID, duration, width and height
      // set version to 1 (uint(64))
      trackHeaderBox->SetFlags(0x0000000F);
      trackHeaderBox->SetTrackId(fragmentHeaderBox->GetTrackId());
      trackHeaderBox->SetDuration(media->GetDuration());
      trackHeaderBox->SetVersion(1);
      trackHeaderBox->SetAlternateGroup(1);
      trackHeaderBox->GetVolume()->SetIntegerPart(1);
    }

    if (continueCreating)
    {
      continueCreating &= trackBox->GetBoxes()->Add(trackHeaderBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(trackHeaderBox);
    }
  }

  // add media box
  if (continueCreating)
  {
    CMediaBox *mediaBox = new CMediaBox();
    continueCreating &= (mediaBox != NULL);

    // add media header box
    if (continueCreating)
    {
      CMediaHeaderBox *mediaHeaderBox = new CMediaHeaderBox();
      continueCreating &= (mediaHeaderBox != NULL);

      if (continueCreating)
      {
        // set version (1 = uint(64)), time scale from manifest, duration
        mediaHeaderBox->SetVersion(1);
        mediaHeaderBox->SetTimeScale((uint32_t)media->GetTimeScale());
        mediaHeaderBox->SetDuration(media->GetDuration());
      }

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(mediaHeaderBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(mediaHeaderBox);
      }
    }

    // add handler box
    if (continueCreating)
    {
      CHandlerBox *handlerBox = this->GetHandlerBox(HANDLER_TYPE_AUDIO, L"SoundHandler");
      continueCreating &= (handlerBox != NULL);

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(handlerBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(handlerBox);
      }
    }

    // add media information box
    if (continueCreating)
    {
      CMediaInformationBox *mediaInformationBox = new CMediaInformationBox(HANDLER_TYPE_AUDIO);
      continueCreating &= (mediaInformationBox != NULL);

      // add sound media header box
      if (continueCreating)
      {
        CSoundMediaHeaderBox *soundMediaHeaderBox = new CSoundMediaHeaderBox();
        continueCreating &= (soundMediaHeaderBox != NULL);

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(soundMediaHeaderBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(soundMediaHeaderBox);
        }
      }

      // add data information box
      if (continueCreating)
      {
        CDataInformationBox *dataInformationBox = this->GetDataInformationBox();
        continueCreating &= (dataInformationBox != NULL);

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(dataInformationBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(dataInformationBox);
        }
      }

      // add samle table box
      if (continueCreating)
      {
        CSampleTableBox *sampleTableBox = this->GetAudioSampleTableBox(media, streamIndex, trackIndex, fragmentHeaderBox);
        continueCreating &= (sampleTableBox != NULL);

        if (continueCreating)
        {
          continueCreating &= mediaInformationBox->GetBoxes()->Add(sampleTableBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(sampleTableBox);
        }
      }

      if (continueCreating)
      {
        continueCreating &= mediaBox->GetBoxes()->Add(mediaInformationBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(mediaInformationBox);
      }
    }

    if (continueCreating)
    {
      continueCreating &= trackBox->GetBoxes()->Add(mediaBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(mediaBox);
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(trackBox);
  }

  return trackBox;
}

CDataInformationBox *CMPUrlSourceSplitter_Protocol_Mshs::GetDataInformationBox(void)
{
  CDataInformationBox *dataInformationBox = new CDataInformationBox();
  bool continueCreating = (dataInformationBox != NULL);

  if (continueCreating)
  {
    // add data reference box
    if (continueCreating)
    {
      CDataReferenceBox *dataReferenceBox = new CDataReferenceBox();
      continueCreating &= (dataReferenceBox != NULL);

      if (continueCreating)
      {
        // add data entry url box
        if (continueCreating)
        {
          CDataEntryUrlBox *dataEntryUrlBox = new CDataEntryUrlBox();
          continueCreating &= (dataEntryUrlBox != NULL);

          if (continueCreating)
          {
            dataEntryUrlBox->SetSelfContained(true);
          }

          if (continueCreating)
          {
            continueCreating &= dataReferenceBox->GetDataEntryBoxCollection()->Add(dataEntryUrlBox);
          }

          if (!continueCreating)
          {
            FREE_MEM_CLASS(dataEntryUrlBox);
          }
        }
      }

      if (continueCreating)
      {
        continueCreating &= dataInformationBox->GetBoxes()->Add(dataReferenceBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(dataReferenceBox);
      }
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(dataInformationBox);
  }

  return dataInformationBox;
}

CHandlerBox *CMPUrlSourceSplitter_Protocol_Mshs::GetHandlerBox(uint32_t handlerType, const wchar_t *handlerName)
{
  CHandlerBox *handlerBox = new CHandlerBox();
  bool continueCreating = (handlerBox != NULL);

  if (continueCreating)
  {
    handlerBox->SetHandlerType(handlerType);
    continueCreating &= handlerBox->SetName(handlerName);
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(handlerBox);
  }

  return handlerBox;
}

CSampleTableBox *CMPUrlSourceSplitter_Protocol_Mshs::GetVideoSampleTableBox(CMSHSSmoothStreamingMedia *media, unsigned int streamIndex, unsigned int trackIndex, CTrackFragmentHeaderBox *fragmentHeaderBox)
{
  CSampleTableBox *sampleTableBox = new CSampleTableBox(HANDLER_TYPE_VIDEO);
  bool continueCreating = ((sampleTableBox != NULL) && (media != NULL));

  CMSHSStream *stream = media->GetStreams()->GetItem(streamIndex);
  continueCreating &= (stream != NULL);

  CMSHSTrack *track = NULL;
  if (continueCreating)
  {
    track = stream->GetTracks()->GetItem(trackIndex);
  }
  continueCreating &= (track != NULL);

  // add sample description box
  if (continueCreating)
  {
    CSampleDescriptionBox *sampleDescriptionBox = new CSampleDescriptionBox(HANDLER_TYPE_VIDEO);
    continueCreating &= (sampleDescriptionBox != NULL);

    // add visual sample entry
    if (continueCreating)
    {
      CVisualSampleEntryBox *visualSampleEntryBox = new CVisualSampleEntryBox();
      continueCreating &= (visualSampleEntryBox != NULL);

      if (continueCreating)
      {
        continueCreating &= visualSampleEntryBox->SetCodingName(L"avc1");
        visualSampleEntryBox->SetDataReferenceIndex(1);
        visualSampleEntryBox->GetHorizontalResolution()->SetIntegerPart(72);
        visualSampleEntryBox->GetVerticalResolution()->SetIntegerPart(72);
        visualSampleEntryBox->SetFrameCount(1);
        continueCreating &= visualSampleEntryBox->SetCompressorName(L"");
        visualSampleEntryBox->SetDepth(24);
        visualSampleEntryBox->SetWidth((uint16_t)track->GetMaxWidth());
        visualSampleEntryBox->SetHeight((uint16_t)track->GetMaxHeight());
      }

      // add AVC configuration box
      if (continueCreating)
      {
        CAVCConfigurationBox *avcConfigurationBox = new CAVCConfigurationBox();
        continueCreating &= (avcConfigurationBox != NULL);

        if (continueCreating)
        {
          char *codecPrivateData = ConvertToMultiByte(track->GetCodecPrivateData());
          continueCreating &= (codecPrivateData != NULL);

          if (continueCreating)
          {
            const char *spsStart = strstr(codecPrivateData, "00000001");
            continueCreating &= (spsStart != NULL);

            if (continueCreating)
            {
              spsStart += 8;

              const char *ppsStart = strstr(spsStart, "00000001");
              continueCreating &= (ppsStart != NULL);

              if (continueCreating)
              {
                ppsStart += 8;
                unsigned int ppsLength = strlen(ppsStart);
                unsigned int spsLength = strlen(spsStart) - ppsLength - 8;

                // we have SPS start and PPS start
                // parse data to AVC configuration box
                ALLOC_MEM_DEFINE_SET(sps, char, (spsLength + 1), 0);
                continueCreating &= (sps != NULL);

                if (continueCreating)
                {
                  memcpy(sps, spsStart, spsLength);

                  uint8_t *convertedSps = HexToDecA(sps);
                  uint8_t *convertedPps = HexToDecA(ppsStart);
                  continueCreating &= ((convertedSps != NULL) && (convertedPps != NULL));

                  if (continueCreating)
                  {
                    avcConfigurationBox->GetAVCDecoderConfiguration()->SetConfigurationVersion(1);
                    avcConfigurationBox->GetAVCDecoderConfiguration()->SetAvcProfileIndication(convertedSps[1]);
                    avcConfigurationBox->GetAVCDecoderConfiguration()->SetProfileCompatibility(convertedSps[2]);
                    avcConfigurationBox->GetAVCDecoderConfiguration()->SetAvcLevelIndication(convertedSps[3]);
                    avcConfigurationBox->GetAVCDecoderConfiguration()->SetLengthSizeMinusOne(3);
                  }

                  if (continueCreating)
                  {
                    CSequenceParameterSetNALUnit *spsUnit = new CSequenceParameterSetNALUnit();
                    continueCreating &= (spsUnit != NULL);

                    if (continueCreating)
                    {
                      continueCreating &= spsUnit->SetBuffer(convertedSps, spsLength / 2);
                    }

                    if (continueCreating)
                    {
                      continueCreating &= avcConfigurationBox->GetAVCDecoderConfiguration()->GetSequenceParameterSetNALUnits()->Add(spsUnit);
                    }

                    if (!continueCreating)
                    {
                      FREE_MEM_CLASS(spsUnit);
                    }
                  }

                  if (continueCreating)
                  {
                    CPictureParameterSetNALUnit *ppsUnit = new CPictureParameterSetNALUnit();
                    continueCreating &= (ppsUnit != NULL);

                    if (continueCreating)
                    {
                      continueCreating &= ppsUnit->SetBuffer(convertedPps, ppsLength / 2);
                    }

                    if (continueCreating)
                    {
                      continueCreating &= avcConfigurationBox->GetAVCDecoderConfiguration()->GetPictureParameterSetNALUnits()->Add(ppsUnit);
                    }

                    if (!continueCreating)
                    {
                      FREE_MEM_CLASS(ppsUnit);
                    }
                  }

                  FREE_MEM(convertedSps);
                  FREE_MEM(convertedPps);
                }
                FREE_MEM(sps);
              }
            }
          }
          FREE_MEM(codecPrivateData);
        }

        if (continueCreating)
        {
          continueCreating &= visualSampleEntryBox->GetBoxes()->Add(avcConfigurationBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(avcConfigurationBox);
        }
      }

      if (continueCreating)
      {
        continueCreating &= sampleDescriptionBox->GetSampleEntries()->Add(visualSampleEntryBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(visualSampleEntryBox);
      }
    }

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(sampleDescriptionBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(sampleDescriptionBox);
    }
  }

  // add time to sample box
  if (continueCreating)
  {
    CTimeToSampleBox *timeToSampleBox = new CTimeToSampleBox();
    continueCreating &= (timeToSampleBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(timeToSampleBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(timeToSampleBox);
    }
  }

  // add sample to chunk box
  if (continueCreating)
  {
    CSampleToChunkBox *sampleToChunkBox = new CSampleToChunkBox();
    continueCreating &= (sampleToChunkBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(sampleToChunkBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(sampleToChunkBox);
    }
  }

  // add chunk offset box
  if (continueCreating)
  {
    CChunkOffsetBox *chunkOffsetBox = new CChunkOffsetBox();
    continueCreating &= (chunkOffsetBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(chunkOffsetBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(chunkOffsetBox);
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(sampleTableBox);
  }

  return sampleTableBox;
}

CSampleTableBox *CMPUrlSourceSplitter_Protocol_Mshs::GetAudioSampleTableBox(CMSHSSmoothStreamingMedia *media, unsigned int streamIndex, unsigned int trackIndex, CTrackFragmentHeaderBox *fragmentHeaderBox)
{
  CSampleTableBox *sampleTableBox = new CSampleTableBox(HANDLER_TYPE_AUDIO);
  bool continueCreating = ((sampleTableBox != NULL) && (media != NULL));

  CMSHSStream *stream = media->GetStreams()->GetItem(streamIndex);
  continueCreating &= (stream != NULL);

  CMSHSTrack *track = NULL;
  if (continueCreating)
  {
    track = stream->GetTracks()->GetItem(trackIndex);
  }
  continueCreating &= (track != NULL);

  // add sample description box
  if (continueCreating)
  {
    CSampleDescriptionBox *sampleDescriptionBox = new CSampleDescriptionBox(HANDLER_TYPE_AUDIO);
    continueCreating &= (sampleDescriptionBox != NULL);

    // add audio sample entry
    if (continueCreating)
    {
      CAudioSampleEntryBox *audioSampleEntryBox = new CAudioSampleEntryBox();
      continueCreating &= (audioSampleEntryBox != NULL);

      if (continueCreating)
      {
        continueCreating &= audioSampleEntryBox->SetCodingName(L"mp4a");
        audioSampleEntryBox->SetChannelCount(track->GetChannels());
        audioSampleEntryBox->SetSampleSize(track->GetBitsPerSample());
        audioSampleEntryBox->GetSampleRate()->SetIntegerPart(track->GetSamplingRate());
      }

      // add ESD box
      if (continueCreating)
      {
        CESDBox *esdBox = new CESDBox();
        continueCreating &= (esdBox != NULL);

        if (continueCreating)
        {
          uint32_t length = (track->GetCodecPrivateData() != NULL) ? wcslen(track->GetCodecPrivateData()) : 0;

          if (continueCreating)
          {
            esdBox->SetTrackId(fragmentHeaderBox->GetTrackId());
            esdBox->SetCodecTag(CODEC_TAG_AAC);
            esdBox->SetMaxBitrate(128000);

            if (length > 0)
            {
              uint8_t *convertedCodecPrivateData = HexToDecW(track->GetCodecPrivateData());
              continueCreating &= (convertedCodecPrivateData != NULL);

              if (continueCreating)
              {
                continueCreating &= esdBox->SetCodecPrivateData(convertedCodecPrivateData, length);
              }

              FREE_MEM(convertedCodecPrivateData);
            }
          }
        }

        if (continueCreating)
        {
          continueCreating &= audioSampleEntryBox->GetBoxes()->Add(esdBox);
        }

        if (!continueCreating)
        {
          FREE_MEM_CLASS(esdBox);
        }
      }

      if (continueCreating)
      {
        continueCreating &= sampleDescriptionBox->GetSampleEntries()->Add(audioSampleEntryBox);
      }

      if (!continueCreating)
      {
        FREE_MEM_CLASS(audioSampleEntryBox);
      }
    }

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(sampleDescriptionBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(sampleDescriptionBox);
    }
  }

  // add time to sample box
  if (continueCreating)
  {
    CTimeToSampleBox *timeToSampleBox = new CTimeToSampleBox();
    continueCreating &= (timeToSampleBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(timeToSampleBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(timeToSampleBox);
    }
  }

  // add sample to chunk box
  if (continueCreating)
  {
    CSampleToChunkBox *sampleToChunkBox = new CSampleToChunkBox();
    continueCreating &= (sampleToChunkBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(sampleToChunkBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(sampleToChunkBox);
    }
  }

  // add chunk offset box
  if (continueCreating)
  {
    CChunkOffsetBox *chunkOffsetBox = new CChunkOffsetBox();
    continueCreating &= (chunkOffsetBox != NULL);

    if (continueCreating)
    {
      continueCreating &= sampleTableBox->GetBoxes()->Add(chunkOffsetBox);
    }

    if (!continueCreating)
    {
      FREE_MEM_CLASS(chunkOffsetBox);
    }
  }

  if (!continueCreating)
  {
    FREE_MEM_CLASS(sampleTableBox);
  }

  return sampleTableBox;
}