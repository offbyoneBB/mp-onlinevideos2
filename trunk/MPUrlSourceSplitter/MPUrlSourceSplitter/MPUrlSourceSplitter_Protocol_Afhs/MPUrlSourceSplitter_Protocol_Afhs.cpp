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

#include "MPUrlSourceSplitter_Protocol_Afhs.h"
#include "MPUrlSourceSplitter_Protocol_Afhs_Parameters.h"
#include "Utilities.h"
#include "LockMutex.h"
#include "VersionInfo.h"
#include "formatUrl.h"

#include "base64.h"

#include "Box.h"
#include "BootstrapInfoBox.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#ifdef _DEBUG
#define PROTOCOL_IMPLEMENTATION_NAME                                          L"MPUrlSourceSplitter_Protocol_Afhsd"
#else
#define PROTOCOL_IMPLEMENTATION_NAME                                          L"MPUrlSourceSplitter_Protocol_Afhs"
#endif

PIPlugin CreatePluginInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Protocol_Afhs(configuration);
}

void DestroyPluginInstance(PIPlugin pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Protocol_Afhs *pClass = (CMPUrlSourceSplitter_Protocol_Afhs *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Protocol_Afhs::CMPUrlSourceSplitter_Protocol_Afhs(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_PROTOCOL_AFHS, COMPILE_INFO_MPURLSOURCESPLITTER_PROTOCOL_AFHS);
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
  
  this->receiveDataTimeout = AFHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
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
  this->bufferForProcessing = NULL;
  this->shouldExit = false;
  this->bootstrapInfoBox = NULL;
  this->segmentsFragments = NULL;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Protocol_Afhs::~CMPUrlSourceSplitter_Protocol_Afhs()
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

  FREE_MEM_CLASS(this->bufferForProcessing);
  FREE_MEM_CLASS(this->configurationParameters);

  if (this->lockMutex != NULL)
  {
    CloseHandle(this->lockMutex);
    this->lockMutex = NULL;
  }

  FREE_MEM_CLASS(this->bootstrapInfoBox);
  FREE_MEM_CLASS(this->segmentsFragments);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

// IProtocol interface

bool CMPUrlSourceSplitter_Protocol_Afhs::IsConnected(void)
{
  return ((this->mainCurlInstance != NULL) || (this->wholeStreamDownloaded));
}

unsigned int CMPUrlSourceSplitter_Protocol_Afhs::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::ParseUrl(const CParameterCollection *parameters)
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

  wchar_t *url = this->configurationParameters->GetValue(PARAMETER_NAME_URL, true, NULL);
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

void CMPUrlSourceSplitter_Protocol_Afhs::ReceiveData(bool *shouldExit)
{
  //this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  //this->shouldExit = *shouldExit;

  //CLockMutex lock(this->lockMutex, INFINITE);

  //if (this->internalExitRequest)
  //{
  //  // there is internal exit request pending == changed timestamp

  //  if (this->mainCurlInstance != NULL)
  //  {
  //    this->mainCurlInstance->SetCloseWithoutWaiting(true);
  //  }
  //  
  //  // close connection
  //  this->StopReceivingData();

  //  // reopen connection
  //  // OpenConnection() reset wholeStreamDownloaded
  //  this->StartReceivingData(NULL);

  //  this->internalExitRequest = false;
  //}

  //if (this->IsConnected())
  //{
  //  if (!this->wholeStreamDownloaded)
  //  {
  //    unsigned int bufferOccupiedSpace = this->receivedData->GetBufferOccupiedSpace();
  //    if (bufferOccupiedSpace > 0)
  //    {
  //      ALLOC_MEM_DEFINE_SET(buffer, char, bufferOccupiedSpace, 0);
  //      if (buffer != NULL)
  //      {
  //        this->receivedData->CopyFromBuffer(buffer, bufferOccupiedSpace, 0, 0);
  //        // create media packet
  //        // set values of media packet
  //        CMediaPacket *mediaPacket = new CMediaPacket();
  //        mediaPacket->GetBuffer()->InitializeBuffer(bufferOccupiedSpace);
  //        mediaPacket->GetBuffer()->AddToBuffer(buffer, bufferOccupiedSpace);
  //        mediaPacket->SetStart(this->streamTime);
  //        mediaPacket->SetEnd(this->streamTime + bufferOccupiedSpace - 1);

  //        HRESULT result = this->filter->PushMediaPacket(mediaPacket);
  //        if (FAILED(result))
  //        {
  //          this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet, error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
  //        }

  //        this->streamTime += bufferOccupiedSpace;
  //        this->receivedData->RemoveFromBufferAndMove(bufferOccupiedSpace);

  //        delete mediaPacket;
  //      }
  //      FREE_MEM(buffer);
  //    }

  //    if (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA)
  //    {
  //      // all data received, we're not receiving data

  //      if (this->mainCurlInstance->GetErrorCode() == CURLE_OK)
  //      {
  //        // whole stream downloaded
  //        this->wholeStreamDownloaded = true;

  //        if (!this->setLength)
  //        {
  //          this->streamLength = this->streamTime;
  //          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: setting total length: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, this->streamLength);
  //          this->filter->SetTotalLength(this->streamLength, false);
  //          this->setLength = true;
  //        }

  //        // notify filter the we reached end of stream
  //        // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
  //        int64_t streamTime = this->streamTime;
  //        this->streamTime = this->streamLength;
  //        this->filter->EndOfStreamReached(max(0, streamTime - 1));
  //      }
  //    }
  //  }
  //}
  //else
  //{
  //  this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"connection closed, opening new one");
  //  // re-open connection if previous is lost
  //  if (this->StartReceivingData(NULL) != S_OK)
  //  {
  //    this->StopReceivingData();
  //  }
  //}

  //this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);

  /*
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
  this->shouldExit = *shouldExit;

  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->IsConnected())
  {
    if (!this->wholeStreamDownloaded)
    {
      if ((!this->supressData) && (this->bufferForProcessing != NULL))
      {
        FlvPacket *flvPacket = new FlvPacket();
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

              HRESULT result = this->filter->PushMediaPacket(mediaPacket);
              if (FAILED(result))
              {
                this->logger->Log(LOGGER_WARNING, L"%s: %s: error occured while adding media packet, error: 0x%08X", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
              }
              this->bytePosition += flvPacket->GetSize();

              delete mediaPacket;
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

        // only in case that we started video and received some data
        // in other case close and open connection again
        if ((this->bytePosition != 0) && (this->streamTime == 0))
        {
          // whole stream downloaded
          this->wholeStreamDownloaded = true;

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

            if (this->streamTime == 0)
            {
              // if stream time is zero than we receive data from beginning
              // in that case we call EndOfStreamReached() method (required for ending download)

              // notify filter the we reached end of stream
              // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
              this->filter->EndOfStreamReached(max(0, this->bytePosition - 1));
            }
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
  */

  if (!this->setLength)
  {
    // if not set length, just quess or adjust length
    if ((this->streamLength == 0) || (this->bytePosition > (this->streamLength * 3 / 4)))
    {
      if (this->bytePosition != 0)
      {
        if (this->streamLength == 0)
        {
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
    }
  }
}

// ISimpleProtocol interface

unsigned int CMPUrlSourceSplitter_Protocol_Afhs::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::StartReceivingData(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME);

  CHECK_POINTER_DEFAULT_HRESULT(result, this->configurationParameters);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  this->wholeStreamDownloaded = false;

  char *bootstrapInfoBase64Encoded = ConvertToMultiByteW(this->configurationParameters->GetValue(PARAMETER_NAME_AFHS_BOOTSTRAP_INFO, true, NULL));
  CHECK_POINTER_HRESULT(result, bootstrapInfoBase64Encoded, result, E_POINTER);

  if (SUCCEEDED(result))
  {
    // bootstrap info is BASE64 encoded
    unsigned char *bootstrapInfo = NULL;
    unsigned int bootstrapInfoLength = 0;

    result = base64_decode(bootstrapInfoBase64Encoded, &bootstrapInfo, &bootstrapInfoLength);

    if (SUCCEEDED(result))
    {
      FREE_MEM_CLASS(this->bootstrapInfoBox);
      this->bootstrapInfoBox = new CBootstrapInfoBox();
      CHECK_POINTER_HRESULT(result, this->bootstrapInfoBox, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        result = (this->bootstrapInfoBox->Parse(bootstrapInfo, bootstrapInfoLength)) ? result : HRESULT_FROM_WIN32(ERROR_INVALID_DATA);

        if (SUCCEEDED(result))
        {
          wchar_t *parsedBootstrapInfoBox = this->bootstrapInfoBox->GetParsedHumanReadable(L"");
          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: parsed bootstrap info:\n%s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, parsedBootstrapInfoBox);
          FREE_MEM(parsedBootstrapInfoBox);
        }
        else
        {
          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot parse bootstrap info box");
        }
      }
    }
    else
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot decode bootstrap info");
    }
  }

  if (SUCCEEDED(result))
  {
    // we have bootstrap info box successfully parsed
    // now choose from bootstrap info -> QualityEntryTable highest quality (if exists)
    wchar_t *quality = NULL;
    if (this->bootstrapInfoBox->GetQualityEntryTable()->Count() != 0)
    {
      CBootstrapInfoQualityEntry *bootstrapInfoQualityEntry = this->bootstrapInfoBox->GetQualityEntryTable()->GetItem(0);
      quality = Duplicate(bootstrapInfoQualityEntry->GetQualityEntry());
    }

    // from segment run table choose segment with specifed quality (if exists) or segment with QualityEntryCount equal to zero
    CSegmentRunEntryCollection *segmentRunEntryTable = NULL;
    for (unsigned int i = 0; i < this->bootstrapInfoBox->GetSegmentRunTable()->Count(); i++)
    {
      CSegmentRunTableBox *segmentRunTableBox = this->bootstrapInfoBox->GetSegmentRunTable()->GetItem(i);

      if (quality != NULL)
      {
        if (segmentRunTableBox->GetQualitySegmentUrlModifiers()->Contains(quality, false))
        {
          segmentRunEntryTable = segmentRunTableBox->GetSegmentRunEntryTable();
        }
      }
      else
      {
        if (segmentRunTableBox->GetQualitySegmentUrlModifiers()->Count() == 0)
        {
          segmentRunEntryTable = segmentRunTableBox->GetSegmentRunEntryTable();
        }
      }
    }

    if (segmentRunEntryTable == NULL)
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"cannot find any segment run table");
      result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    if (SUCCEEDED(result))
    {
      wchar_t *movieIdentifierUrl = FormatAbsoluteUrl(L"http://BASEURL", (wchar_t *)this->bootstrapInfoBox->GetMovieIdentifier());
      CHECK_POINTER_HRESULT(result, movieIdentifierUrl, result, E_OUTOFMEMORY);

      if (SUCCEEDED(movieIdentifierUrl))
      {
        wchar_t *qualityUrl = FormatAbsoluteUrl(movieIdentifierUrl, (quality == NULL) ? L"" : quality);
        CHECK_POINTER_HRESULT(result, qualityUrl, result, E_OUTOFMEMORY);

        if (SUCCEEDED(result))
        {
          // convert segment run entry table to simplier collection
          FREE_MEM_CLASS(this->segmentsFragments);
          this->segmentsFragments = new CSegmentFragmentCollection();
          CHECK_POINTER_HRESULT(result, this->segmentsFragments, result, E_OUTOFMEMORY);

          if (SUCCEEDED(result))
          {
            unsigned int lastFragment = 1;
            //unsigned int lastSegment = segmentRunEntryTable->GetItem(0)->GetFirstSegment();

            for (unsigned int i = 0; i < (segmentRunEntryTable->Count() - 1); i++)
            {
              CSegmentRunEntry *segmentRunEntry = segmentRunEntryTable->GetItem(i);
              CSegmentRunEntry *segmentRunEntryNext = segmentRunEntryTable->GetItem(i + 1);

              for (unsigned int j = segmentRunEntry->GetFirstSegment(); j < segmentRunEntryNext->GetFirstSegment(); j++)
              {
                for (unsigned int k = 0; k < segmentRunEntry->GetFragmentsPerSegment(); k++)
                {
                  wchar_t *segmentFragmentPartUrl = FormatString(L"Seg%d-Frag%d", segmentRunEntry->GetFirstSegment() + j, lastFragment++);
                  CHECK_POINTER_HRESULT(result, segmentFragmentPartUrl, result, E_OUTOFMEMORY);

                  if (SUCCEEDED(result))
                  {
                    wchar_t *url = FormatAbsoluteUrl(qualityUrl, segmentFragmentPartUrl);
                    CHECK_POINTER_HRESULT(result, url, result, E_OUTOFMEMORY);

                    if (SUCCEEDED(result))
                    {
                      CSegmentFragment *segmentFragment = new CSegmentFragment(segmentRunEntry->GetFirstSegment() + j, lastFragment, url);
                      CHECK_POINTER_HRESULT(result, segmentFragment, result, E_OUTOFMEMORY);

                      if (SUCCEEDED(result))
                      {
                        result = (this->segmentsFragments->Add(segmentFragment)) ? result : E_FAIL;
                      }

                      if (FAILED(result))
                      {
                        FREE_MEM_CLASS(segmentFragment);
                      }
                    }
                    FREE_MEM(url);
                  }
                  FREE_MEM(segmentFragmentPartUrl);
                }
              }
            }

            // do same for last segment run entry
            CSegmentRunEntry *segmentRunEntry = segmentRunEntryTable->GetItem(segmentRunEntryTable->Count() - 1);
            for (unsigned int i = 0; i < segmentRunEntry->GetFragmentsPerSegment(); i++)
            {
              wchar_t *segmentFragmentPartUrl = FormatString(L"Seg%d-Frag%d", segmentRunEntry->GetFirstSegment(), lastFragment++);
              CHECK_POINTER_HRESULT(result, segmentFragmentPartUrl, result, E_OUTOFMEMORY);

              if (SUCCEEDED(result))
              {
                wchar_t *url = FormatAbsoluteUrl(qualityUrl, segmentFragmentPartUrl);
                CHECK_POINTER_HRESULT(result, url, result, E_OUTOFMEMORY);

                if (SUCCEEDED(result))
                {
                  CSegmentFragment *segmentFragment = new CSegmentFragment(segmentRunEntry->GetFirstSegment(), lastFragment, url);
                  CHECK_POINTER_HRESULT(result, segmentFragment, result, E_OUTOFMEMORY);

                  if (SUCCEEDED(result))
                  {
                    result = (this->segmentsFragments->Add(segmentFragment)) ? result : E_FAIL;
                  }

                  if (FAILED(result))
                  {
                    FREE_MEM_CLASS(segmentFragment);
                  }
                }
                FREE_MEM(url);
              }
              FREE_MEM(segmentFragmentPartUrl);
            }
          }
        }
        FREE_MEM(qualityUrl);
      }
      FREE_MEM(movieIdentifierUrl);
    }

    FREE_MEM(quality);

    if (SUCCEEDED(result))
    {
      for (unsigned int i = 0; i < this->segmentsFragments->Count(); i++)
      {
        CSegmentFragment *segmentFragment = this->segmentsFragments->GetItem(i);
        this->logger->Log(LOGGER_VERBOSE, L"%s: %s: segment %d, fragment %d, url '%s'", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, segmentFragment->GetSegment(), segmentFragment->GetFragment(), segmentFragment->GetUrl());
      }
    }

    result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
  }

  //if (SUCCEEDED(result))
  //{
  //  this->mainCurlInstance = new CCurlInstance(this->logger, this->configurationParameters->GetValue(PARAMETER_NAME_URL, true, NULL), PROTOCOL_IMPLEMENTATION_NAME);
  //  result = (this->mainCurlInstance != NULL) ? S_OK : E_POINTER;
  //}

  //if (SUCCEEDED(result))
  //{
  //  this->receivedData = new LinearBuffer();
  //  result = (this->receivedData == NULL) ? E_POINTER : result;

  //  if (SUCCEEDED(result))
  //  {
  //    result = (this->receivedData->InitializeBuffer(MINIMUM_RECEIVED_DATA_FOR_SPLITTER)) ? result : E_FAIL;
  //  }
  //}

  //if (SUCCEEDED(result))
  //{
  //  this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
  //  this->mainCurlInstance->SetWriteCallback(CMPUrlSourceSplitter_Protocol_Afhs::CurlReceiveData, this);
  //  this->mainCurlInstance->SetStartStreamTime(this->streamTime);
  //  this->mainCurlInstance->SetEndStreamTime(this->endStreamTime);
  //  this->mainCurlInstance->SetReferer(this->configurationParameters->GetValue(PARAMETER_NAME_AFHS_REFERER, true, NULL));
  //  this->mainCurlInstance->SetUserAgent(this->configurationParameters->GetValue(PARAMETER_NAME_AFHS_USER_AGENT, true, NULL));
  //  this->mainCurlInstance->SetCookie(this->configurationParameters->GetValue(PARAMETER_NAME_AFHS_COOKIE, true, NULL));
  //  this->mainCurlInstance->SetHttpVersion(this->configurationParameters->GetValueLong(PARAMETER_NAME_AFHS_VERSION, true, HTTP_VERSION_DEFAULT));
  //  this->mainCurlInstance->SetIgnoreContentLength((this->configurationParameters->GetValueLong(PARAMETER_NAME_AFHS_IGNORE_CONTENT_LENGTH, true, HTTP_IGNORE_CONTENT_LENGTH_DEFAULT) == 1L));

  //  result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;

  //  if (SUCCEEDED(result))
  //  {
  //    // all parameters set
  //    // start receiving data

  //    result = (this->mainCurlInstance->StartReceivingData()) ? S_OK : E_FAIL;
  //  }

  //  if (SUCCEEDED(result))
  //  {
  //    // wait for HTTP status code

  //    long responseCode = 0;
  //    while (responseCode == 0)
  //    {
  //      CURLcode errorCode = this->mainCurlInstance->GetResponseCode(&responseCode);
  //      if (errorCode == CURLE_OK)
  //      {
  //        if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
  //        {
  //          // response code 200 - 299 = OK
  //          // response code 300 - 399 = redirect (OK)
  //          this->logger->Log(LOGGER_VERBOSE, L"%s: %s: HTTP status code: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, responseCode);
  //          result = E_FAIL;
  //        }
  //      }
  //      else
  //      {
  //        this->mainCurlInstance->ReportCurlErrorMessage(LOGGER_WARNING, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"error while requesting HTTP status code", errorCode);
  //        result = E_FAIL;
  //        break;
  //      }

  //      if ((responseCode == 0) && (this->mainCurlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
  //      {
  //        // we received data too fast
  //        result = E_FAIL;
  //        break;
  //      }

  //      // wait some time
  //      Sleep(1);
  //    }
  //  }
  //}

  if (FAILED(result))
  {
    this->StopReceivingData();
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME, result);
  return result;

  /*
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

  if (result == S_OK)
  {
    this->mainCurlInstance = new CCurlInstance(this->logger, this->configurationParameters->GetValue(PARAMETER_NAME_URL, true, NULL), PROTOCOL_IMPLEMENTATION_NAME);
    result = (this->mainCurlInstance != NULL) ? S_OK : E_POINTER;
  }

  if (SUCCEEDED(result) && (this->bufferForProcessing == NULL))
  {
    this->bufferForProcessing = new LinearBuffer();
    result = (this->bufferForProcessing != NULL) ? S_OK : E_OUTOFMEMORY;

    if (SUCCEEDED(result))
    {
      result = this->bufferForProcessing->InitializeBuffer(BUFFER_FOR_PROCESSING_SIZE_DEFAULT, 0) ? S_OK : E_OUTOFMEMORY;
    }
  }

  if (SUCCEEDED(result))
  {
    this->mainCurlInstance->SetReceivedDataTimeout(this->receiveDataTimeout);
    this->mainCurlInstance->SetWriteCallback(CMPUrlSourceSplitter_Protocol_Rtmp::CurlReceiveData, this);

    this->mainCurlInstance->SetRtmpApp(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_APP, true, RTMP_APP_DEFAULT));
    this->mainCurlInstance->SetRtmpArbitraryData(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_ARBITRARY_DATA, true, NULL));
    this->mainCurlInstance->SetRtmpBuffer(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_BUFFER, true, RTMP_BUFFER_DEFAULT));
    this->mainCurlInstance->SetRtmpFlashVersion(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_FLASHVER, true, RTMP_FLASH_VER_DEFAULT));
    this->mainCurlInstance->SetRtmpAuth(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_AUTH, true, RTMP_AUTH_DEFAULT));
    this->mainCurlInstance->SetRtmpJtv(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_JTV, true, RTMP_JTV_DEFAULT));
    this->mainCurlInstance->SetRtmpLive(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_LIVE, true, RTMP_LIVE_DEFAULT));
    this->mainCurlInstance->SetRtmpPageUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PAGE_URL, true, RTMP_PAGE_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpPlaylist(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_PLAYLIST, true, RTMP_PLAYLIST_DEFAULT));
    this->mainCurlInstance->SetRtmpPlayPath(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_PLAY_PATH, true, RTMP_PLAY_PATH_DEFAULT));
    this->mainCurlInstance->SetRtmpStart((this->streamTime >= 0) ? this->streamTime : this->configurationParameters->GetValueInt64(PARAMETER_NAME_RTMP_START, true, RTMP_START_DEFAULT));
    this->mainCurlInstance->SetRtmpStop(this->configurationParameters->GetValueInt64(PARAMETER_NAME_RTMP_STOP, true, RTMP_STOP_DEFAULT));
    this->mainCurlInstance->SetRtmpSubscribe(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SUBSCRIBE, true, RTMP_SUBSCRIBE_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfAge(this->configurationParameters->GetValueUnsignedInt(PARAMETER_NAME_RTMP_SWF_AGE, true, RTMP_SWF_AGE_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_SWF_URL, true, RTMP_SWF_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpSwfVerify(this->configurationParameters->GetValueBool(PARAMETER_NAME_RTMP_SWF_VERIFY, true, RTMP_SWF_VERIFY_DEFAULT));
    this->mainCurlInstance->SetRtmpTcUrl(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TC_URL, true, RTMP_TC_URL_DEFAULT));
    this->mainCurlInstance->SetRtmpToken(this->configurationParameters->GetValue(PARAMETER_NAME_RTMP_TOKEN, true, RTMP_TOKEN_DEFAULT));

    result = (this->mainCurlInstance->Initialize()) ? S_OK : E_FAIL;

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
        Sleep(1);
      }

      if (state == CURL_STATE_RECEIVED_ALL_DATA)
      {
        // we received data too fast
        result = E_FAIL;
      }
    }
  }

  if (FAILED(result))
  {
    this->StopReceivingData();
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_START_RECEIVING_DATA_NAME);
  return result;
  */

  return E_FAIL;
}

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::StopReceivingData(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);

  // lock access to stream
  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->mainCurlInstance != NULL)
  {
    this->mainCurlInstance->SetCloseWithoutWaiting(this->seekingActive);
    delete this->mainCurlInstance;
    this->mainCurlInstance = NULL;
  }

  FREE_MEM_CLASS(this->bufferForProcessing);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_STOP_RECEIVING_DATA_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
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
  
HRESULT CMPUrlSourceSplitter_Protocol_Afhs::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
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

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::ClearSession(void)
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
  this->receiveDataTimeout = AFHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->bytePosition = 0;
  this->shouldExit = false;
  FREE_MEM_CLASS(this->bootstrapInfoBox);
  FREE_MEM_CLASS(this->segmentsFragments);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

// ISeeking interface

unsigned int CMPUrlSourceSplitter_Protocol_Afhs::GetSeekingCapabilities(void)
{
  return SEEKING_METHOD_NONE;
}

int64_t CMPUrlSourceSplitter_Protocol_Afhs::SeekToTime(int64_t time)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu, to time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, time);

  int64_t result = -1;

  //this->seekingActive = true;

  //// close connection
  //this->StopReceivingData();

  //// RTMP protocol can seek to ms
  //// time is in ms

  //// 1 second back
  //this->streamTime = max(0, time - 1000);

  //// reopen connection
  //// StartReceivingData() reset wholeStreamDownloaded
  //this->StartReceivingData(NULL);

  //if (this->IsConnected())
  //{
  //  result = max(0, time - 1000);
  //}

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, result);
  return result;
}

int64_t CMPUrlSourceSplitter_Protocol_Afhs::SeekToPosition(int64_t start, int64_t end)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from position: %llu, to position: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT64_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  return result;
}

void CMPUrlSourceSplitter_Protocol_Afhs::SetSupressData(bool supressData)
{
  this->supressData = supressData;
}

// IPlugin interface

wchar_t *CMPUrlSourceSplitter_Protocol_Afhs::GetName(void)
{
  return Duplicate(PROTOCOL_NAME);
}

GUID CMPUrlSourceSplitter_Protocol_Afhs::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

HRESULT CMPUrlSourceSplitter_Protocol_Afhs::Initialize(PluginConfiguration *configuration)
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

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_AFHS_RECEIVE_DATA_TIMEOUT, true, AFHS_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? AFHS_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return S_OK;
}

// other methods

size_t CMPUrlSourceSplitter_Protocol_Afhs::CurlReceiveData(char *buffer, size_t size, size_t nmemb, void *userdata)
{
  CMPUrlSourceSplitter_Protocol_Afhs *caller = (CMPUrlSourceSplitter_Protocol_Afhs *)userdata;
  CLockMutex lock(caller->lockMutex, INFINITE);
  unsigned int bytesRead = size * nmemb;

  if (!(caller->shouldExit))
  {
    if (bytesRead != 0)
    {
      if (caller->bufferForProcessing != NULL)
      {
        unsigned int bufferSize = caller->bufferForProcessing->GetBufferSize();
        unsigned int freeSpace = caller->bufferForProcessing->GetBufferFreeSpace();

        if (freeSpace < bytesRead)
        {
          unsigned int bufferNewSize = max(bufferSize * 2, bufferSize + bytesRead);
          caller->logger->Log(LOGGER_VERBOSE, L"%s: %s: buffer to small, buffer size: %d, new size: %d", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, bufferSize, bufferNewSize);
          if (!caller->bufferForProcessing->ResizeBuffer(bufferNewSize))
          {
            caller->logger->Log(LOGGER_WARNING, L"%s: %s: resizing buffer unsuccessful, dropping received data", PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
          }

          freeSpace = caller->bufferForProcessing->GetBufferFreeSpace();
        }

        if (freeSpace >= bytesRead)
        {
          caller->bufferForProcessing->AddToBuffer(buffer, bytesRead);
        }
      }
    }
  }

  // if returned 0 (or lower value than bytesRead) it cause transfer interruption
  return (caller->shouldExit) ? 0 : (bytesRead);
}

