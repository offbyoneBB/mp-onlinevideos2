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

#include "MPUrlSourceSplitter_FILE.h"
#include "Utilities.h"
#include "LockMutex.h"

// protocol implementation name
#ifdef _DEBUG
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_Filed"
#else
#define PROTOCOL_IMPLEMENTATION_NAME                                    L"MPUrlSourceSplitter_File"
#endif

PIProtocol CreateProtocolInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_File(configuration);
}

void DestroyProtocolInstance(PIProtocol pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_File *pClass = (CMPUrlSourceSplitter_File *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_File::CMPUrlSourceSplitter_File(CParameterCollection *configuration)
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

CMPUrlSourceSplitter_File::~CMPUrlSourceSplitter_File()
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

HRESULT CMPUrlSourceSplitter_File::ClearSession(void)
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

HRESULT CMPUrlSourceSplitter_File::Initialize(IOutputStream *filter, CParameterCollection *configuration)
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

TCHAR *CMPUrlSourceSplitter_File::GetProtocolName(void)
{
  return Duplicate(PROTOCOL_NAME);
}

HRESULT CMPUrlSourceSplitter_File::ParseUrl(const TCHAR *url, const CParameterCollection *parameters)
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
    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'url components'");
    result = E_OUTOFMEMORY;
  }

  if (result == S_OK)
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

  if (result == S_OK)
  {
    int length = urlComponents->dwSchemeLength + 1;
    ALLOC_MEM_DEFINE_SET(protocol, wchar_t, length, 0);
    if (protocol == NULL) 
    {
      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for 'protocol'");
      result = E_OUTOFMEMORY;
    }

    if (result == S_OK)
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

    if (result == S_OK)
    {
      // convert url to Unicode (if needed), because CoInternetParseUrl() works in Unicode

      size_t urlLength = wcslen(url) + 1;
      wchar_t *parseUrl = ConvertToUnicode(url);
      if (parseUrl == NULL)
      {
        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot convert url to wide character url");
        result = E_OUTOFMEMORY;
      }

      if (result == S_OK)
      {
        // parsed file path should be shorter than wide character url
        ALLOC_MEM_DEFINE_SET(parsedFilePath, wchar_t, urlLength, 0);
        if (parsedFilePath == NULL)
        {
          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for parsed file path");
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
              this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot allocate memory for parsed file path");
              result = E_OUTOFMEMORY;
            }

            if (result == S_OK)
            {
              stored = 0;
              error = CoInternetParseUrl(parseUrl, PARSE_PATH_FROM_URL, 0, parsedFilePath, stored, &stored, 0);
              if (error != S_OK)
              {
                this->logger->Log(LOGGER_ERROR, L"%s: %s: error occured while parsing file url, error: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, error);
                result = error;
              }
            }
          }
          else if (error != S_OK)
          {
            // error occured
            this->logger->Log(LOGGER_ERROR, L"%s: %s: error occured while parsing file url, error: %u", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, error);
            result = error;
          }
        }

        if (result == S_OK)
        {
          // if we are here, then file url was successfully parsed
          // now store parsed url into this->filePath

          this->filePath = ConvertToUnicodeW(parsedFilePath);
          if (this->filePath == NULL)
          {
            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, L"cannot convert from Unicode file path to file path");
            result = E_OUTOFMEMORY;
          }
        }

        if (result == S_OK)
        {
          this->logger->Log(LOGGER_INFO, L"%s: %s: file path: %s", PROTOCOL_IMPLEMENTATION_NAME, METHOD_PARSE_URL_NAME, this->filePath);
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

HRESULT CMPUrlSourceSplitter_File::OpenConnection(void)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, this->filePath);

  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME);

  this->wholeStreamDownloaded = false;

  if ((result == S_OK) && (this->fileStream == NULL))
  {
    if (_wfopen_s(&this->fileStream, this->filePath, L"rb") != 0)
    {
      this->logger->Log(LOGGER_ERROR, L"%s: %s: error occured while opening file, error: %i", PROTOCOL_IMPLEMENTATION_NAME, METHOD_OPEN_CONNECTION_NAME, errno);
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

bool CMPUrlSourceSplitter_File::IsConnected(void)
{
  return ((this->fileStream != NULL) || (this->wholeStreamDownloaded));
}

void CMPUrlSourceSplitter_File::CloseConnection(void)
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

void CMPUrlSourceSplitter_File::ReceiveData(bool *shouldExit)
{
  this->logger->Log(LOGGER_DATA, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);

  CLockMutex lock(this->lockMutex, INFINITE);

  if (this->IsConnected())
  {
    if (!this->wholeStreamDownloaded)
    {
      if (!this->setLenght)
      {
        this->filter->SetTotalLength(this->fileLength, false);
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

          mediaPacket->SetStart(this->streamTime);
          mediaPacket->SetEnd(this->streamTime + bytesRead - 1);
          this->filter->PushMediaPacket(mediaPacket);
          this->streamTime += bytesRead;
        }
        FREE_MEM(receiveBuffer);
      }
      else
      {
        this->wholeStreamDownloaded = true;

        // notify filter the we reached end of stream
        // EndOfStreamReached() can call ReceiveDataFromTimestamp() which can set this->streamTime
        int64_t streamTime = this->streamTime;
        this->streamTime = this->fileLength;
        this->filter->EndOfStreamReached(max(0, streamTime - 1));
      }
    }
  }
  else
  {
    this->logger->Log(LOGGER_WARNING,  METHOD_MESSAGE_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME, L"file not opened, opening file");
    // re-open connection if previous is lost
    if (this->OpenConnection() != S_OK)
    {
      this->CloseConnection();
    }
  }

  this->logger->Log(LOGGER_DATA, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_RECEIVE_DATA_NAME);
}

unsigned int CMPUrlSourceSplitter_File::GetReceiveDataTimeout(void)
{
  return this->receiveDataTimeout;
}

GUID CMPUrlSourceSplitter_File::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

unsigned int CMPUrlSourceSplitter_File::GetOpenConnectionMaximumAttempts(void)
{
  return this->openConnetionMaximumAttempts;
}

int64_t CMPUrlSourceSplitter_File::SeekToPosition(int64_t start, int64_t end)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu, to time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, start, end);

  int64_t result = -1;

  if (this->IsConnected())
  {
    {
      // lock access to file
      CLockMutex lock(this->lockMutex, INFINITE);

      result = (fseek(this->fileStream, (long)start, SEEK_SET) == 0) ? start : -1;
      if (SUCCEEDED(result))
      {
        this->wholeStreamDownloaded = false;
        this->streamTime = start;
      }
    }
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_POSITION_NAME, result);
  return result;
}

HRESULT CMPUrlSourceSplitter_File::AbortStreamReceive()
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  CLockMutex lock(this->lockMutex, INFINITE);

  // close connection and set that whole stream downloaded
  this->CloseConnection();
  this->wholeStreamDownloaded = true;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_ABORT_STREAM_RECEIVE_NAME);
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_File::QueryStreamProgress(LONGLONG *total, LONGLONG *current)
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

HRESULT CMPUrlSourceSplitter_File::QueryStreamAvailableLength(CStreamAvailableLength *availableLength)
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

unsigned int CMPUrlSourceSplitter_File::GetSeekingCapabilities(void)
{
  return SEEKING_METHOD_POSITION;
}

int64_t CMPUrlSourceSplitter_File::SeekToTime(int64_t time)
{
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME);
  this->logger->Log(LOGGER_VERBOSE, L"%s: %s: from time: %llu, to time: %llu", PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, time);

  int64_t result = -1;

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_HRESULT_FORMAT, PROTOCOL_IMPLEMENTATION_NAME, METHOD_SEEK_TO_TIME_NAME, result);
  return result;
}