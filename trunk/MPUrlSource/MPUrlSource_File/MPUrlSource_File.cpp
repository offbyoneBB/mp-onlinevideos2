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

#include "MPUrlSource_FILE.h"
#include "Network.h"
#include "Utilities.h"
#include "LockMutex.h"

#include <WinInet.h>
#include <stdio.h>

// protocol implementation name
#define PROTOCOL_IMPLEMENTATION_NAME                                    _T("CMPUrlSource_File")

PIProtocol CreateProtocolInstance(CParameterCollection *configuration)
{
  return new CMPUrlSource_File(configuration);
}

void DestroyProtocolInstance(PIProtocol pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSource_File *pClass = (CMPUrlSource_File *)pProtocol;
    delete pClass;
  }
}

CMPUrlSource_File::CMPUrlSource_File(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  this->filePath = NULL;
  this->fileStream = NULL;

  this->receiveDataTimeout = 0;
  this->openConnetionMaximumAttempts = FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->filter = NULL;
  this->fileLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->wholeStreamDownloaded = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSource_File::~CMPUrlSource_File()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }

  delete this->configurationParameters;

  if (this->lockMutex != NULL)
  {
    CloseHandle(this->lockMutex);
    this->lockMutex = NULL;
  }

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

HRESULT CMPUrlSource_File::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  if (this->IsConnected())
  {
    this->CloseConnection();
  }

  this->fileLength = 0;
  this->setLenght = false;
  this->streamTime = 0;
  this->wholeStreamDownloaded = false;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

HRESULT CMPUrlSource_File::Initialize(IOutputStream *filter, CParameterCollection *configuration)
{
  this->filter = filter;
  if (this->filter == NULL)
  {
    return E_POINTER;
  }

  if (this->lockMutex == NULL)
  {
    return E_FAIL;
  }

  this->configurationParameters->Clear();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }
  this->configurationParameters->LogCollection(this->logger, LOGGER_VERBOSE, PROTOCOL_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  this->receiveDataTimeout = this->configurationParameters->GetValueLong(PARAMETER_NAME_FILE_RECEIVE_DATA_TIMEOUT, true, FILE_RECEIVE_DATA_TIMEOUT_DEFAULT);
  this->openConnetionMaximumAttempts = this->configurationParameters->GetValueLong(PARAMETER_NAME_FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS, true, FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT);

  this->receiveDataTimeout = (this->receiveDataTimeout < 0) ? FILE_RECEIVE_DATA_TIMEOUT_DEFAULT : this->receiveDataTimeout;
  this->openConnetionMaximumAttempts = (this->openConnetionMaximumAttempts < 0) ? FILE_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT : this->openConnetionMaximumAttempts;

  return S_OK;
}

TCHAR *CMPUrlSource_File::GetProtocolName(void)
{
  return Duplicate(PROTOCOL_NAME);
}

HRESULT CMPUrlSource_File::ParseUrl(const TCHAR *url, const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  this->ClearSession();
  if (parameters != NULL)
  {
    this->configurationParameters->Clear();
    this->Initialize(this->filter, (CParameterCollection *)parameters);
  }

  ALLOC_MEM_DEFINE_SET(urlComponents, URL_COMPONENTS, 1, 0);
  if (urlComponents == NULL)
  {
    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'url components'"));
    result = E_OUTOFMEMORY;
  }

  if (result == S_OK)
  {
    ZeroURL(urlComponents);
    urlComponents->dwStructSize = sizeof(URL_COMPONENTS);

    this->logger->Log(LOGGER_INFO, _T("%s: %s: url: %s"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, url);

    if (!InternetCrackUrl(url, 0, 0, urlComponents))
    {
      this->logger->Log(LOGGER_ERROR, _T("%s: %s: InternetCrackUrl() error: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, GetLastError());
      result = E_FAIL;
    }
  }

  if (result == S_OK)
  {
    int length = urlComponents->dwSchemeLength + 1;
    ALLOC_MEM_DEFINE_SET(protocol, TCHAR, length, 0);
    if (protocol == NULL) 
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for 'protocol'"));
      result = E_OUTOFMEMORY;
    }

    if (result == S_OK)
    {
      _tcsncat_s(protocol, length, urlComponents->lpszScheme, urlComponents->dwSchemeLength);

      bool supportedProtocol = false;
      for (int i = 0; i < TOTAL_SUPPORTED_PROTOCOLS; i++)
      {
        if (_tcsncicmp(urlComponents->lpszScheme, SUPPORTED_PROTOCOLS[i], urlComponents->dwSchemeLength) == 0)
        {
          supportedProtocol = true;
          break;
        }
      }

      if (!supportedProtocol)
      {
        // not supported protocol
        this->logger->Log(LOGGER_INFO, _T("%s: %s: unsupported protocol '%s'"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, protocol);
        result = E_FAIL;
      }
    }

    if (result == S_OK)
    {
      // convert url to Unicode (if needed), because CoInternetParseUrl() works in Unicode

      size_t urlLength = _tcslen(url) + 1;
      wchar_t *parseUrl = ConvertToUnicode(url);
      if (parseUrl == NULL)
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot convert url to wide character url"));
        result = E_OUTOFMEMORY;
      }

      if (result == S_OK)
      {
        // parsed file path should be shorter than wide character url
        ALLOC_MEM_DEFINE_SET(parsedFilePath, wchar_t, urlLength, 0);
        if (parsedFilePath == NULL)
        {
          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for parsed file path"));
          result = E_OUTOFMEMORY;
        }

        if (result == S_OK)
        {
          DWORD stored = 0;
          HRESULT error = CoInternetParseUrl(parseUrl, PARSE_PATH_FROM_URL, 0, parsedFilePath, urlLength, &stored, 0);
          if (error == E_POINTER)
          {
            // not enough memory in buffer, in stored is required buffer size
            FREE_MEM(parsedFilePath);
            parsedFilePath = ALLOC_MEM_SET(parsedFilePath, wchar_t, stored, 0);
            if (parsedFilePath == NULL)
            {
              this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot allocate memory for parsed file path"));
              result = E_OUTOFMEMORY;
            }

            if (result == S_OK)
            {
              stored = 0;
              error = CoInternetParseUrl(parseUrl, PARSE_PATH_FROM_URL, 0, parsedFilePath, stored, &stored, 0);
              if (error != S_OK)
              {
                this->logger->Log(LOGGER_ERROR, _T("%s: %s: error occured while parsing file url, error: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, error);
                result = error;
              }
            }
          }
          else if (error != S_OK)
          {
            // error occured
            this->logger->Log(LOGGER_ERROR, _T("%s: %s: error occured while parsing file url, error: %u"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, error);
            result = error;
          }
        }

        if (result == S_OK)
        {
          // if we are here, then file url was successfully parsed
          // now store parsed url into this->filePath

#ifdef _MBCS
          this->filePath = ConvertToMultiByteW(parsedFilePath);
#else
          this->filePath = ConvertToUnicodeW(parsedFilePath);
#endif
          if (this->filePath == NULL)
          {
            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, _T("cannot convert from Unicode file path to file path"));
            result = E_OUTOFMEMORY;
          }
        }

        if (result == S_OK)
        {
          this->logger->Log(LOGGER_INFO, _T("%s: %s: file path: %s"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, this->filePath);
        }
        FREE_MEM(parsedFilePath);
      }
      FREE_MEM(parseUrl);
    }
    FREE_MEM(protocol);
  }

  FREE_MEM(urlComponents);
  
  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME);

  return result;
}

HRESULT CMPUrlSource_File::OpenConnection(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, this->filePath);

  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);

  this->wholeStreamDownloaded = false;

  if ((result == S_OK) && (this->fileStream == NULL))
  {
    if (_tfopen_s(&this->fileStream, this->filePath, _T("rb")) != 0)
    {
      this->logger->Log(LOGGER_ERROR, _T("%s: %s: error occured while opening file, error: %i"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, errno);
      result = E_FAIL;
    }

    if (result == S_OK)
    {
      LARGE_INTEGER size;
      size.QuadPart = 0;

      // open or create file
      HANDLE hLogFile = CreateFile(this->filePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

      if (hLogFile != INVALID_HANDLE_VALUE)
      {
        if (!GetFileSizeEx(hLogFile, &size))
        {
          // error occured while getting file size
          size.QuadPart = 0;
        }

        CloseHandle(hLogFile);
        hLogFile = INVALID_HANDLE_VALUE;

        this->fileLength = size.QuadPart;
      }
    }
  }

  this->logger->Log(LOGGER_INFO, SUCCEEDED(result) ? METHOD_END_FORMAT : METHOD_END_FAIL_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);
  return result;
}

bool CMPUrlSource_File::IsConnected(void)
{
  return ((this->fileStream != NULL) || (this->wholeStreamDownloaded));
}

void CMPUrlSource_File::CloseConnection(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);

  if (this->fileStream != NULL)
  {
    fclose(this->fileStream);
  }
  this->fileStream = NULL;

  FREE_MEM(this->filePath);
  
  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_CLOSE_CONNECTION_NAME);
}

void CMPUrlSource_File::ReceiveData(bool *shouldExit)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);

  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->IsConnected())
  {
    if (!this->wholeStreamDownloaded)
    {
      if (!this->setLenght)
      {
        this->filter->SetTotalLength(OUTPUT_PIN_NAME, this->fileLength, false);
        this->setLenght = true;
      }

      if (!feof(this->fileStream))
      {
        unsigned int bytesToRead = DEFAULT_BUFFER_SIZE; // 32 kB

        ALLOC_MEM_DEFINE_SET(receiveBuffer, char, bytesToRead, 0);    
        unsigned int bytesRead = fread_s(receiveBuffer, bytesToRead, sizeof(char), bytesToRead, this->fileStream);
        if (bytesRead != 0)
        {
          // create media packet
          // set values of media packet
          CMediaPacket *mediaPacket = new CMediaPacket();
          mediaPacket->GetBuffer()->InitializeBuffer(bytesRead);
          mediaPacket->GetBuffer()->AddToBuffer(receiveBuffer, bytesRead);

          REFERENCE_TIME timeEnd = this->streamTime + bytesRead - 1;
          HRESULT result = mediaPacket->SetTime(&this->streamTime, &timeEnd);
          if (result != S_OK)
          {
            this->logger->Log(LOGGER_WARNING, _T("%s: %s: stream time not set, error: 0x%08X"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, result);
          }

          this->filter->PushMediaPacket(OUTPUT_PIN_NAME, mediaPacket);
          this->streamTime += bytesRead;
        }
        FREE_MEM(receiveBuffer);
      }
      else
      {
        this->wholeStreamDownloaded = true;

        // notify filter the we reached end of stream
        // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
        REFERENCE_TIME streamTime = this->streamTime;
        this->streamTime = this->fileLength;
        this->filter->EndOfStreamReached(OUTPUT_PIN_NAME, max(0, streamTime - 1));
      }
    }
  }
  else
  {
    this->logger->Log(LOGGER_WARNING,  METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, _T("file not opened, opening file"));
    // re-open connection if previous is lost
    if (this->OpenConnection() != S_OK)
    {
      this->CloseConnection();
    }
  }

  this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
}

unsigned int CMPUrlSource_File::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

GUID CMPUrlSource_File::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

unsigned int CMPUrlSource_File::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

CStringCollection *CMPUrlSource_File::GetStreamNames(void)
{
  CStringCollection *streamNames = new CStringCollection();

  streamNames->Add(Duplicate(OUTPUT_PIN_NAME));

  return streamNames;
}

HRESULT CMPUrlSource_File::ReceiveDataFromTimestamp(REFERENCE_TIME startTime, REFERENCE_TIME endTime)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME);
  this->logger->Log(LOGGER_VERBOSE, _T("%s: %s: from time: %llu, to time: %llu"), PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, startTime, endTime);

  HRESULT result = E_NOT_VALID_STATE;

  if (this->IsConnected())
  {
    {
      // lock access to file
      CLockMutex lock(this->lockMutex, INFINITE);

      result = (fseek(this->fileStream, (long)startTime, SEEK_SET) == 0) ? S_OK : E_FAIL;
      if (SUCCEEDED(result))
      {
        this->wholeStreamDownloaded = false;
        this->streamTime = startTime;
      }
    }
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_FROM_TIMESTAMP_NAME, result);
  return result;
}

HRESULT CMPUrlSource_File::AbortStreamReceive()
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  CLockMutex lock(this->lockMutex, INFINITE);

  // close connection and set that whole stream downloaded
  this->CloseConnection();
  this->wholeStreamDownloaded = true;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  return S_OK;
}

HRESULT CMPUrlSource_File::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME);

  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, total);
  CHECK_POINTER_DEFAULT_HRESULT(result, current);

  if (result == S_OK)
  {
    *total = this->fileLength;
    *current = this->streamTime;
  }

  this->logger->Log(LOGGER_DATA, (SUCCEEDED(result)) ? METHOD_END_HRESULT_FORMAT : METHOD_END_FAIL_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_QUERY_STREAM_PROGRESS_NAME, result);
  return result;
}

HRESULT CMPUrlSource_File::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, availableLength);

  if (result == S_OK)
  {
    availableLength->SetQueryResult(S_OK);
    availableLength->SetAvailableLength(this->fileLength);
  }

  return result;
}

HRESULT CMPUrlSource_File::QueryRangesSupported(CRangesSupported *rangesSupported)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, rangesSupported);

  if (result == S_OK)
  {
    rangesSupported->SetQueryResult(S_OK);
    rangesSupported->SetRangesSupported(true);
  }

  return result;
}