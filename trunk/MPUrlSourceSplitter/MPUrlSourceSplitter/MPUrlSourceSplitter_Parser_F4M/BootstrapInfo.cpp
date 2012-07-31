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

#include "StdAfx.h"

#include "BootstrapInfo.h"

#include "base64.h"
#include "HttpCurlInstance.h"
#include "formatUrl.h"

CBootstrapInfo::CBootstrapInfo(const wchar_t *id, const wchar_t *profile, const wchar_t *url, const wchar_t *value)
{
  this->id = Duplicate((id == NULL) ? L"" : id);
  this->profile = Duplicate(profile);
  this->url = Duplicate(url);
  this->value = Duplicate(value);

  this->decodedLength = UINT_MAX;
  this->decodedValue = NULL;
  this->decodeResult = E_NOT_VALID_STATE;
  this->baseUrl = NULL;
}

CBootstrapInfo::~CBootstrapInfo(void)
{
  FREE_MEM(this->url);
  FREE_MEM(this->profile);
  FREE_MEM(this->url);
  FREE_MEM(this->value);

  FREE_MEM(this->decodedValue);
  FREE_MEM(this->baseUrl);
}

bool CBootstrapInfo::IsValid(void)
{
  return ((this->id != NULL) && (this->profile != NULL) && (((this->url != NULL) && (this->value == NULL)) || ((this->url == NULL) && (this->value != NULL))));
}

bool CBootstrapInfo::HasUrl(void)
{
  return (this->url != NULL);
}

bool CBootstrapInfo::HasValue(void)
{
  return (this->value != NULL);
}

const wchar_t *CBootstrapInfo::GetId(void)
{
  return this->id;
}

const wchar_t *CBootstrapInfo::GetProfile(void)
{
  return this->profile;
}

const wchar_t *CBootstrapInfo::GetUrl(void)
{
  return this->url;
}

const wchar_t *CBootstrapInfo::GetValue(void)
{
  return this->value;
}

HRESULT CBootstrapInfo::GetDecodeResult(void)
{
  FREE_MEM(this->decodedValue);
  HRESULT result = this->decodeResult;

  if ((this->value != NULL) && (result == E_NOT_VALID_STATE))
  {
    // no conversion occured until now
    result = S_OK;

    char *val = ConvertToMultiByteW(this->value);
    CHECK_POINTER_HRESULT(result, val, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      result = base64_decode(val, &this->decodedValue, &this->decodedLength);
    }

    FREE_MEM(val);
    if (FAILED(result))
    {
      this->decodedLength = UINT_MAX;
    }
  }

  return result;
}

const unsigned char *CBootstrapInfo::GetDecodedValue(void)
{
  return this->decodedValue;
}

unsigned int CBootstrapInfo::GetDecodedValueLength(void)
{
  return this->decodedLength;
}

bool CBootstrapInfo::SetBaseUrl(const wchar_t *baseUrl)
{
  FREE_MEM(this->baseUrl);
  this->baseUrl = Duplicate(baseUrl);

  return (this->baseUrl != NULL);
}

const wchar_t *CBootstrapInfo::GetBaseUrl(void)
{
  return this->baseUrl;
}

HRESULT CBootstrapInfo::DownloadBootstrapInfo(CLogger *logger, const wchar_t *protocolName, unsigned int receiveDataTimeout, const wchar_t *referer, const wchar_t *userAgent, const wchar_t *cookie)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, logger);
  CHECK_POINTER_DEFAULT_HRESULT(result, protocolName);

  if (SUCCEEDED(result))
  {
    wchar_t *bootstrapInfoUrl = FormatAbsoluteUrl(this->baseUrl, this->url);
    CHECK_POINTER_HRESULT(result, bootstrapInfoUrl, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      CHttpCurlInstance *curlInstance = new CHttpCurlInstance(logger, bootstrapInfoUrl, protocolName);
      CHECK_POINTER_HRESULT(result, curlInstance, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        curlInstance->SetReceivedDataTimeout(receiveDataTimeout);
        curlInstance->SetReferer(referer);
        curlInstance->SetUserAgent(userAgent);
        curlInstance->SetCookie(cookie);

        result = (curlInstance->Initialize()) ? S_OK : E_FAIL;

        if (SUCCEEDED(result))
        {
          // all parameters set
          // start receiving data

          result = (curlInstance->StartReceivingData()) ? S_OK : E_FAIL;
        }

        if (SUCCEEDED(result))
        {
          // wait for HTTP status code

          long responseCode = 0;
          while (responseCode == 0)
          {
            CURLcode errorCode = curlInstance->GetResponseCode(&responseCode);
            if (errorCode == CURLE_OK)
            {
              if ((responseCode != 0) && ((responseCode < 200) || (responseCode >= 400)))
              {
                // response code 200 - 299 = OK
                // response code 300 - 399 = redirect (OK)
                result = E_FAIL;
              }
            }
            else
            {
              result = E_FAIL;
              break;
            }

            if ((responseCode == 0) && (curlInstance->GetCurlState() == CURL_STATE_RECEIVED_ALL_DATA))
            {
              // we received data too fast
              result = E_FAIL;
              break;
            }

            // wait some time
            Sleep(1);
          }

          if (SUCCEEDED(result))
          {
            // wait until all data are received
            while (curlInstance->GetCurlState() != CURL_STATE_RECEIVED_ALL_DATA)
            {
              // sleep some time
              Sleep(10);
            }

            result = (curlInstance->GetErrorCode() != CURLE_OK) ? HRESULT_FROM_WIN32(ERROR_INVALID_DATA) : result;
          }

          if (SUCCEEDED(result))
          {
            // copy received data for parsing
            FREE_MEM(this->decodedValue);
            this->decodedLength = curlInstance->GetReceiveDataBuffer()->GetBufferOccupiedSpace();
            this->decodedValue = ALLOC_MEM_SET(this->decodedValue, unsigned char, this->decodedLength, 0);
            CHECK_POINTER_HRESULT(result, this->decodedValue, result, E_OUTOFMEMORY);

            if (SUCCEEDED(result))
            {
              curlInstance->GetReceiveDataBuffer()->CopyFromBuffer(this->decodedValue, this->decodedLength, 0, 0);

              char *base64EncodedValue = NULL;
              result = base64_encode(this->decodedValue, this->decodedLength, &base64EncodedValue);
              if (SUCCEEDED(result))
              {
                FREE_MEM(this->value);
                this->value = ConvertToUnicodeA(base64EncodedValue);
                CHECK_POINTER_HRESULT(result, this->value, result, E_OUTOFMEMORY);
              }
              FREE_MEM(base64EncodedValue);
            }

            this->decodeResult = E_NOT_VALID_STATE;
            this->decodedLength = 0;
            FREE_MEM(this->decodedValue);
          }
        }
      }

      FREE_MEM_CLASS(curlInstance);
    }

    FREE_MEM(bootstrapInfoUrl);
  }

  return result;
}