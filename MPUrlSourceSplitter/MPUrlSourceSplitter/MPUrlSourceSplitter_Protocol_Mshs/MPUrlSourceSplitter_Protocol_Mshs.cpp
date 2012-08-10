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

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Protocol_Mshs::~CMPUrlSourceSplitter_Protocol_Mshs()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  if (this->mainCurlInstance != NULL)
  {
    delete this->mainCurlInstance;
    this->mainCurlInstance = NULL;
  }

  FREE_MEM_CLASS(this->bufferForBoxProcessingCollection);
  FREE_MEM_CLASS(this->bufferForProcessing);
  FREE_MEM_CLASS(this->configurationParameters);
  FREE_MEM_CLASS(this->streamFragments);

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
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded));
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

        while (this->bufferForBoxProcessingCollection->Count() > 1)
        {
          CLinearBuffer *bufferForBoxProcessing = this->bufferForBoxProcessingCollection->GetItem(0);
          do
          {
            continueProcessing = false;
            //CBox *box = new CBox();
            //if (box != NULL)
            {
              unsigned int length = bufferForBoxProcessing->GetBufferOccupiedSpace();
              if (length > 0)
              {
                ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
                if (buffer != NULL)
                {
                  bufferForBoxProcessing->CopyFromBuffer(buffer, length, 0, 0);
                  //if (box->Parse(buffer, length))
                  {
                    //unsigned int boxSize = (unsigned int)box->GetSize();
                    //if (length >= boxSize)
                    {
                      continueProcessing = true;

                      //if (wcscmp(box->GetType(), MEDIA_DATA_BOX_TYPE) == 0)
                      {
                        //CMediaDataBox *mediaBox = new CMediaDataBox();
                        //if (mediaBox != NULL)
                        {
                          //continueProcessing &= mediaBox->Parse(buffer, length);

                          if (continueProcessing)
                          {
                            //unsigned int payloadSize = (unsigned int)mediaBox->GetPayloadSize();
                            //continueProcessing &= (this->bufferForProcessing->AddToBufferWithResize(mediaBox->GetPayload(), payloadSize) == payloadSize);
                            continueProcessing &= (this->bufferForProcessing->AddToBufferWithResize(buffer, length) == length);
                          }
                        }
                        //FREE_MEM_CLASS(mediaBox);
                      }

                      if (continueProcessing)
                      {
                        //bufferForBoxProcessing->RemoveFromBufferAndMove(boxSize);
                        bufferForBoxProcessing->RemoveFromBufferAndMove(length);
                        continueProcessing = true;
                      }
                    }
                  }
                }
                FREE_MEM(buffer);
              }
            }
            //FREE_MEM_CLASS(box);
          } while (continueProcessing);

          if (bufferForBoxProcessing->GetBufferOccupiedSpace() == 0)
          {
            // all data are processed, remove buffer from collection
            this->bufferForBoxProcessingCollection->Remove(0);
          }
        }
      }

      if ((!this->supressData) && (this->bufferForProcessing != NULL))
      {
      //  CFlvPacket *flvPacket = new CFlvPacket();
      //  if (flvPacket != NULL)
      //  {
      //    while (flvPacket->ParsePacket(this->bufferForProcessing))
      //    {
      //      // FLV packet parsed correctly
      //      // push FLV packet to filter

      //      //if ((flvPacket->GetType() != FLV_PACKET_HEADER) && (this->firstTimestamp == (-1)))
      //      //{
      //      //  this->firstTimestamp = flvPacket->GetTimestamp();
      //      //  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: set first timestamp: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->firstTimestamp);
      //      //}

      //      //if ((flvPacket->GetType() == FLV_PACKET_VIDEO) && (this->firstVideoTimestamp == (-1)))
      //      //{
      //      //  this->firstVideoTimestamp = flvPacket->GetTimestamp();
      //      //  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: set first video timestamp: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->firstVideoTimestamp);
      //      //}

      //      //if ((flvPacket->GetType() == FLV_PACKET_VIDEO) && (this->firstVideoTimestamp != (-1)) && (this->firstTimestamp != (-1)))
      //      //{
      //      //  // correction of video timestamps
      //      //  flvPacket->SetTimestamp(flvPacket->GetTimestamp() + this->firstTimestamp - this->firstVideoTimestamp);
      //      //}

      //      if ((flvPacket->GetType() == FLV_PACKET_AUDIO) ||
      //        (flvPacket->GetType() == FLV_PACKET_HEADER) ||
      //        (flvPacket->GetType() == FLV_PACKET_META) ||
      //        (flvPacket->GetType() == FLV_PACKET_VIDEO))
      //      {
      //        // do nothing, known packet types
      //      }
      //      else
      //      {
      //        this->logger->Log(LOGGER_WARNING, L"%s: %s: unknown FLV packet: %d, size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, flvPacket->GetType(), flvPacket->GetSize());
      //      }

      //      if ((flvPacket->GetType() != FLV_PACKET_HEADER) || (!this->seekingActive))
      //      {
      //        // create media packet
      //        // set values of media packet

        unsigned int length = this->bufferForProcessing->GetBufferOccupiedSpace();

        if (length > 0)
        {
        ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
        if (buffer != NULL)
        {
              this->bufferForProcessing->CopyFromBuffer(buffer, length, 0, 0);

              CMediaPacket *mediaPacket = new CMediaPacket();
              //mediaPacket->GetBuffer()->InitializeBuffer(flvPacket->GetSize());
              mediaPacket->GetBuffer()->InitializeBuffer(length);
              //mediaPacket->GetBuffer()->AddToBuffer(flvPacket->GetData(), flvPacket->GetSize());
              mediaPacket->GetBuffer()->AddToBuffer(buffer, length);
              mediaPacket->SetStart(this->bytePosition);
              //mediaPacket->SetEnd(this->bytePosition + flvPacket->GetSize() - 1);
              mediaPacket->SetEnd(this->bytePosition + length - 1);

              HRESULT result = this->filter->PushMediaPacket(mediaPacket);
              if (FAILED(result))
              {
                this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet, error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
              }
              //this->bytePosition += flvPacket->GetSize();
              this->bytePosition += length;

              FREE_MEM_CLASS(mediaPacket);
      //      }
      //      // we are definitely not seeking
            this->seekingActive = false;
            //this->bufferForProcessing->RemoveFromBufferAndMove(flvPacket->GetSize());
            this->bufferForProcessing->RemoveFromBufferAndMove(length);
        }

            FREE_MEM(buffer);
        }
      //      flvPacket->Clear();
      //    }

      //    FREE_MEM_CLASS(flvPacket);
      //  }
      }

      if ((this->mainCurlInstance != NULL) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
      {
        // all data received, we're not receiving data
        if (!this->streamFragments->GetStreamFragment(this->mainCurlInstance->GetUrl(), true)->GetDownloaded())
        {
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: received all data for url '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->mainCurlInstance->GetUrl());
          this->streamFragments->GetStreamFragment(this->mainCurlInstance->GetUrl(), true)->SetDownloaded(true);
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
  //this->firstTimestamp = -1;
  //this->firstVideoTimestamp = -1;
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
        CMSHSSmoothStreamingMedia *smoothStreamingMedia = new CMSHSSmoothStreamingMedia();
        CHECK_POINTER_HRESULT(result, smoothStreamingMedia, result, E_OUTOFMEMORY);

        if (SUCCEEDED(result))
        {
          result = (smoothStreamingMedia->Deserialize(decompressedManifest)) ? S_OK : HRESULT_FROM_WIN32(ERROR_INVALID_DATA);

          if (SUCCEEDED(result))
          {
            FREE_MEM(this->streamFragments);
            this->streamFragments = this->GetStreamFragmentsFromManifest(
              this->logger,
              METHOD_START_RECEIVING_DATA_NAME,
              this->configurationParameters,
              smoothStreamingMedia,
              true);
            CHECK_POINTER_HRESULT(result, this->streamFragments, result, E_POINTER);
          }
          else
          {
            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, L"cannot deserialize manifest");
          }
        }

        FREE_MEM_CLASS(smoothStreamingMedia);
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

  //    if ((SUCCEEDED(result)) && (!this->seekingActive))
  //    {
  //      this->bufferForProcessing->AddToBuffer(FLV_FILE_HEADER, FLV_FILE_HEADER_LENGTH);

  //      char *mediaMetadataBase64Encoded = ConvertToMultiByteW(this->configurationParameters->GetValue(PARAMETER_NAME_AFHS_MEDIA_METADATA, true, NULL));
  //      if (mediaMetadataBase64Encoded != NULL)
  //      {
  //        // metadata can be in connection parameters, but it is optional
  //        // metadata is BASE64 encoded
  //        unsigned char *metadata = NULL;
  //        unsigned int metadataLength = 0;
  //        result = base64_decode(mediaMetadataBase64Encoded, &metadata, &metadataLength);

  //        if (SUCCEEDED(result))
  //        {
  //          // create FLV packet from metadata and add its content to buffer for processing
  //          CFlvPacket *metadataFlvPacket = new CFlvPacket();
  //          CHECK_POINTER_HRESULT(result, metadataFlvPacket, result, E_OUTOFMEMORY);

  //          if (SUCCEEDED(result))
  //          {
  //            result = metadataFlvPacket->CreatePacket(FLV_PACKET_META, metadata, metadataLength, (unsigned int)this->segmentsFragments->GetItem(0)->GetFragmentTimestamp()) ? result : E_FAIL;

  //            if (SUCCEEDED(result))
  //            {
  //              result = (this->bufferForProcessing->AddToBufferWithResize(metadataFlvPacket->GetData(), metadataFlvPacket->GetSize()) == metadataFlvPacket->GetSize()) ? result : E_FAIL;

  //              if (FAILED(result))
  //              {
  //                this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot add FLV metadata packet to buffer");
  //              }
  //            }
  //            else
  //            {
  //              this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot create FLV metadata packet");
  //            }
  //          }
  //          else
  //          {
  //            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"not enough memory for FLV metadata packet");
  //          }
  //          FREE_MEM_CLASS(metadataFlvPacket);
  //        }
  //        else
  //        {
  //          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot decode metadata");
  //        }
  //        FREE_MEM(metadata);
  //      }
  //      FREE_MEM(mediaMetadataBase64Encoded);
  //    }
    }
  }

  if (SUCCEEDED(result))
  {
    CStreamFragment *streamFragmentToDownload = this->GetFirstNotDownloadedStreamFragment();
    this->mainCurlInstance = new CHttpCurlInstance(this->logger, this->lockMutex, streamFragmentToDownload->GetUrl(), PROTOCOL_IMPLEMENTATION_NAME);
    CHECK_POINTER_HRESULT(result, this->mainCurlInstance, result, E_POINTER);
  }

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
 
  this->streamLength = 0;
  this->setLength = false;
  this->streamTime = 0;
  this->wholeStreamDownloaded = false;
  this->receiveDataTimeout = MSHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = MSHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->bytePosition = 0;
  this->shouldExit = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

// ISeeking interface

unsigned int CMPUrlSourceSplitter_Protocol_Mshs::GetSeekingCapabilities(void)
{
  //return SEEKING_METHOD_TIME;
  return SEEKING_METHOD_NONE;
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

  //unsigned int segmentFragmentToDownload = UINT_MAX;
  //// find segment and fragment to download
  //if (this->segmentsFragments != NULL)
  //{
  //  for (unsigned int i = 0; i < this->segmentsFragments->Count(); i++)
  //  {
  //    CSegmentFragment *segFrag = this->segmentsFragments->GetItem(i);

  //    if (segFrag->GetFragmentTimestamp() <= (uint64_t)time)
  //    {
  //      segmentFragmentToDownload = i;
  //      result = segFrag->GetFragmentTimestamp();
  //    }
  //  }

  //  for (unsigned int i = 0; i < segmentFragmentToDownload; i++)
  //  {
  //    // mark all previous segments as downloaded
  //    this->segmentsFragments->GetItem(i)->SetDownloaded(true);
  //  }
  //}

  //// in this->lastSegmentFragment is id of segment and fragment to download
  //CSegmentFragment *segFrag = this->segmentsFragments->GetItem(segmentFragmentToDownload);

  //if (segFrag == NULL)
  //{
  //  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: segment %d, fragment %d, url '%s', timestamp %lld", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME,
  //    segFrag->GetSegment(), segFrag->GetFragment(), segFrag->GetUrl(), segFrag->GetFragmentTimestamp());

  //  // reopen connection
  //  // StartReceivingData() reset wholeStreamDownloaded
  //  this->StartReceivingData(NULL);

  //  if (!this->IsConnected())
  //  {
  //    result = -1;
  //  }
  //  else
  //  {
  //    this->streamTime = result;
  //  }
  //}

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

    while ((videoIndex < maxVideoIndex) && (audioIndex < maxAudioIndex))
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
            url = this->FormatUrl(baseUrl, videoUrlPattern, videoTrack, videoFragment);
            videoIndex++;
          }
          else if (audioDiff < videoDiff)
          {
            fragmentTime = audioFragment->GetFragmentTime();
            fragmentDuration = audioFragment->GetFragmentDuration();
            url = this->FormatUrl(baseUrl, audioUrlPattern, audioTrack, audioFragment);
            audioIndex++;
          }
        }
        else if (videoFragment != NULL)
        {
          fragmentTime = videoFragment->GetFragmentTime();
          fragmentDuration = videoFragment->GetFragmentDuration();
          url = this->FormatUrl(baseUrl, videoUrlPattern, videoTrack, videoFragment);
          videoIndex++;
        }
        else if (audioFragment != NULL)
        {
          fragmentTime = audioFragment->GetFragmentTime();
          fragmentDuration = audioFragment->GetFragmentDuration();
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

        CStreamFragment *streamFragment = new CStreamFragment(url, fragmentDuration, fragmentTime);
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