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

#include "RtmpCurlInstance.h"

#include <librtmp/log.h>

CRtmpCurlInstance::CRtmpCurlInstance(CLogger *logger, const wchar_t *url, const wchar_t *protocolName)
  : CCurlInstance(logger, url, protocolName)
{
  this->rtmpApp = RTMP_APP_DEFAULT;
  this->rtmpArbitraryData = RTMP_ARBITRARY_DATA_DEFAULT;
  this->rtmpBuffer = RTMP_BUFFER_DEFAULT;
  this->rtmpFlashVersion = RTMP_FLASH_VER_DEFAULT;
  this->rtmpAuth = RTMP_AUTH_DEFAULT;
  this->rtmpJtv = RTMP_JTV_DEFAULT;
  this->rtmpLive = RTMP_LIVE_DEFAULT;
  this->rtmpPageUrl = RTMP_PAGE_URL_DEFAULT;
  this->rtmpPlaylist = RTMP_PLAYLIST_DEFAULT;
  this->rtmpPlayPath = RTMP_PLAY_PATH_DEFAULT;
  this->rtmpReceiveDataTimeout = RTMP_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->rtmpStart = RTMP_START_DEFAULT;
  this->rtmpStop = RTMP_STOP_DEFAULT;
  this->rtmpSubscribe = RTMP_SUBSCRIBE_DEFAULT;
  this->rtmpSwfAge = RTMP_SWF_AGE_DEFAULT;
  this->rtmpSwfUrl = RTMP_SWF_URL_DEFAULT;
  this->rtmpSwfVerify = RTMP_SWF_VERIFY_DEFAULT;
  this->rtmpTcUrl = RTMP_TC_URL_DEFAULT;
  this->rtmpToken = RTMP_TOKEN_DEFAULT;
}


CRtmpCurlInstance::~CRtmpCurlInstance(void)
{
  FREE_MEM(this->rtmpApp);
  FREE_MEM(this->rtmpArbitraryData);
  FREE_MEM(this->rtmpFlashVersion);
  FREE_MEM(this->rtmpAuth);
  FREE_MEM(this->rtmpJtv);
  FREE_MEM(this->rtmpPageUrl);
  FREE_MEM(this->rtmpPlayPath);
  FREE_MEM(this->rtmpSubscribe);
  FREE_MEM(this->rtmpSwfUrl);
  FREE_MEM(this->rtmpTcUrl);
  FREE_MEM(this->rtmpToken);
}

bool CRtmpCurlInstance::Initialize(void)
{
  bool result = __super::Initialize();

  if (result)
  {
    CURLcode errorCode = CURLE_OK;
    if (errorCode == CURLE_OK)
    {
      // librtmp needs url in specific format

      wchar_t *connectionString = Duplicate(this->url);

      if (this->rtmpApp != RTMP_APP_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_APP, this->rtmpApp, true);
      }
      if (this->rtmpApp != RTMP_ARBITRARY_DATA_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, this->rtmpArbitraryData);
      }
      if (this->rtmpBuffer != RTMP_BUFFER_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_BUFFER, this->rtmpBuffer);
      }
      if (this->rtmpFlashVersion != RTMP_FLASH_VER_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_FLASHVER, this->rtmpFlashVersion, true);
      }
      if (this->rtmpAuth != RTMP_AUTH_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_AUTH, this->rtmpAuth, true);
      }
      if (this->rtmpJtv != RTMP_JTV_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_JTV, this->rtmpJtv, true);
      }
      if (this->rtmpLive != RTMP_LIVE_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_LIVE, this->rtmpLive ? L"1" : L"0", true);
      }
      if (this->rtmpPageUrl != RTMP_PAGE_URL_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_PAGE_URL, this->rtmpPageUrl, true);
      }
      if (this->rtmpPlaylist != RTMP_PLAYLIST_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_PLAYLIST, this->rtmpPlaylist ? L"1" : L"0", true);
      }
      if (this->rtmpPlayPath != RTMP_PLAY_PATH_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_PLAY_PATH, this->rtmpPlayPath, true);
      }
      // timeout for RTMP protocol is set through libcurl options
      if (this->rtmpStart != RTMP_START_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_START, this->rtmpStart);
      }
      if (this->rtmpStop != RTMP_STOP_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_STOP, this->rtmpStop);
      }
      if (this->rtmpSubscribe != RTMP_SUBSCRIBE_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_SUBSCRIBE, this->rtmpSubscribe, true);
      }
      if (this->rtmpSwfAge != RTMP_SWF_AGE_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_SWF_AGE, this->rtmpSwfAge);
      }
      if (this->rtmpSwfUrl != RTMP_SWF_URL_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_SWF_URL, this->rtmpSwfUrl, true);
      }
      if (this->rtmpSwfVerify != RTMP_SWF_VERIFY_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_SWF_VERIFY, this->rtmpSwfVerify ? L"1" : L"0", true);
      }
      if (this->rtmpTcUrl != RTMP_TC_URL_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_TC_URL, this->rtmpTcUrl, true);
      }
      if (this->rtmpToken != RTMP_TOKEN_DEFAULT)
      {
        this->AddToRtmpConnectionString(&connectionString, RTMP_TOKEN_TOKEN, this->rtmpToken, true);
      }

      this->logger->Log(LOGGER_VERBOSE, L"%s: %s: librtmp connection string: %s", this->protocolName, METHOD_INITIALIZE_NAME, connectionString);
      
      char *curlUrl = ConvertToMultiByte(connectionString);
      errorCode = curl_easy_setopt(this->curl, CURLOPT_URL, curlUrl);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting url", errorCode);
        result = false;
      }
      FREE_MEM(curlUrl);
      FREE_MEM(connectionString);
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_RTMP_LOG_CALLBACK, &CRtmpCurlInstance::RtmpLogCallback);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting RTMP protocol log callback", errorCode);
        result = false;
      }
    }

    if (errorCode == CURLE_OK)
    {
      errorCode = curl_easy_setopt(this->curl, CURLOPT_RTMP_LOG_USERDATA, this);
      if (errorCode != CURLE_OK)
      {
        this->ReportCurlErrorMessage(LOGGER_ERROR, this->protocolName, METHOD_INITIALIZE_NAME, L"error while setting RTMP protocol log callback user data", errorCode);
        result = false;
      }
    }
  }

  this->state = (result) ? CURL_STATE_INITIALIZED : CURL_STATE_CREATED;
  return result;
}

void CRtmpCurlInstance::SetRtmpApp(const wchar_t *rtmpApp)
{
  FREE_MEM(this->rtmpApp);
  this->rtmpApp = Duplicate(rtmpApp);
}

void CRtmpCurlInstance::SetRtmpTcUrl(const wchar_t *rtmpTcUrl)
{
  FREE_MEM(this->rtmpTcUrl);
  this->rtmpTcUrl = Duplicate(rtmpTcUrl);
}

void CRtmpCurlInstance::SetRtmpPageUrl(const wchar_t *rtmpPageUrl)
{
  FREE_MEM(this->rtmpPageUrl);
  this->rtmpPageUrl = Duplicate(rtmpPageUrl);
}

void CRtmpCurlInstance::SetRtmpSwfUrl(const wchar_t *rtmpSwfUrl)
{
  FREE_MEM(this->rtmpSwfUrl);
  this->rtmpSwfUrl = Duplicate(rtmpSwfUrl);
}

void CRtmpCurlInstance::SetRtmpFlashVersion(const wchar_t *rtmpFlashVersion)
{
  FREE_MEM(this->rtmpFlashVersion);
  this->rtmpFlashVersion = Duplicate(rtmpFlashVersion);
}

void CRtmpCurlInstance::SetRtmpAuth(const wchar_t *rtmpAuth)
{
  FREE_MEM(this->rtmpAuth);
  this->rtmpAuth = Duplicate(rtmpAuth);
}

void CRtmpCurlInstance::SetRtmpArbitraryData(const wchar_t *rtmpArbitraryData)
{
  FREE_MEM(this->rtmpArbitraryData);
  this->rtmpArbitraryData = Duplicate(rtmpArbitraryData);
}

void CRtmpCurlInstance::SetRtmpPlayPath(const wchar_t *rtmpPlayPath)
{
  FREE_MEM(this->rtmpPlayPath);
  this->rtmpPlayPath = Duplicate(rtmpPlayPath);
}

void CRtmpCurlInstance::SetRtmpPlaylist(bool rtmpPlaylist)
{
  this->rtmpPlaylist = rtmpPlaylist;
}

void CRtmpCurlInstance::SetRtmpLive(bool rtmpLive)
{
  this->rtmpLive = rtmpLive;
}

void CRtmpCurlInstance::SetRtmpSubscribe(const wchar_t *rtmpSubscribe)
{
  FREE_MEM(this->rtmpSubscribe);
  this->rtmpSubscribe = Duplicate(rtmpSubscribe);
}

void CRtmpCurlInstance::SetRtmpStart(int64_t rtmpStart)
{
  this->rtmpStart = rtmpStart;
}

void CRtmpCurlInstance::SetRtmpStop(int64_t rtmpStop)
{
  this->rtmpStop = this->rtmpStop;
}

void CRtmpCurlInstance::SetRtmpBuffer(unsigned int rtmpBuffer)
{
  this->rtmpBuffer = rtmpBuffer;
}

void CRtmpCurlInstance::SetRtmpToken(const wchar_t *rtmpToken)
{
  FREE_MEM(this->rtmpToken);
  this->rtmpToken = Duplicate(rtmpToken);
}

void CRtmpCurlInstance::SetRtmpJtv(const wchar_t *rtmpJtv)
{
  FREE_MEM(this->rtmpJtv);
  this->rtmpJtv = Duplicate(rtmpJtv);
}

void CRtmpCurlInstance::SetRtmpSwfVerify(bool rtmpSwfVerify)
{
  this->rtmpSwfVerify = rtmpSwfVerify;
}

void CRtmpCurlInstance::SetRtmpSwfAge(unsigned int rtmpSwfAge)
{
  this->rtmpSwfAge = rtmpSwfAge;
}

void CRtmpCurlInstance::RtmpLogCallback(RTMP *r, int level, const char *format, va_list vl)
{
  CRtmpCurlInstance *caller = (CRtmpCurlInstance *)r->m_logUserData;

  int length = _vscprintf(format, vl) + 1;
  ALLOC_MEM_DEFINE_SET(buffer, char, length, 0);
  if (buffer != NULL)
  {
    vsprintf_s(buffer, length, format, vl);
  }

  // convert buffer to wchar_t
  wchar_t *convertedBuffer = ConvertToUnicodeA(buffer);

  int loggerLevel = LOGGER_NONE;

  switch (level)
  {
  case RTMP_LOGCRIT:
  case RTMP_LOGERROR:
    loggerLevel = LOGGER_ERROR;
    break;
  case RTMP_LOGWARNING:
    loggerLevel = LOGGER_WARNING;
    break;
  case RTMP_LOGINFO:
    loggerLevel = LOGGER_INFO;
    break;
  case RTMP_LOGDEBUG:
    loggerLevel = (caller->state == CURL_STATE_RECEIVING_DATA) ? LOGGER_DATA : LOGGER_VERBOSE;
    break;
  case RTMP_LOGDEBUG2:
    loggerLevel = LOGGER_DATA;
    break;
  default:
    loggerLevel = LOGGER_NONE;
    break;
  }

  caller->logger->Log(loggerLevel, L"%s: %s: %s", caller->protocolName, L"RtmpLogCallback()", convertedBuffer);

  FREE_MEM(convertedBuffer);
  FREE_MEM(buffer);
}

wchar_t *CRtmpCurlInstance::EncodeString(const wchar_t *string)
{
  wchar_t *replacedString = ReplaceString(string, L"\\", L"\\5c");
  wchar_t *replacedString2 = ReplaceString(replacedString, L" ", L"\\20");
  FREE_MEM(replacedString);
  return replacedString2;
}

wchar_t *CRtmpCurlInstance::CreateRtmpParameter(const wchar_t *name, const wchar_t *value)
{
  if ((name == NULL) || (value == NULL))
  {
    return NULL;
  }
  else
  {
    return FormatString(L"%s=%s", name, value);
  }
}

wchar_t *CRtmpCurlInstance::CreateRtmpEncodedParameter(const wchar_t *name, const wchar_t *value)
{
  wchar_t *result = NULL;
  wchar_t *encodedValue = this->EncodeString(value);
  if (encodedValue != NULL)
  {
    result = this->CreateRtmpParameter(name, encodedValue);
  }
  FREE_MEM(encodedValue);

  return result;
}

bool CRtmpCurlInstance::AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, unsigned int value)
{
  wchar_t *formattedValue = FormatString(L"%u", value);
  bool result = this->AddToRtmpConnectionString(connectionString, name, formattedValue, false);
  FREE_MEM(formattedValue);
  return result;
}

bool CRtmpCurlInstance::AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, int64_t value)
{
  wchar_t *formattedValue = FormatString(L"%lld", value);
  bool result = this->AddToRtmpConnectionString(connectionString, name, formattedValue, false);
  FREE_MEM(formattedValue);
  return result;
}

bool CRtmpCurlInstance::AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *name, const wchar_t *value, bool encode)
{
  if ((connectionString == NULL) || (*connectionString == NULL) || (name == NULL) || (value == NULL))
  {
    return false;
  }

  wchar_t *temp = (encode) ? this->CreateRtmpEncodedParameter(name, value) : this->CreateRtmpParameter(name, value);
  bool result = this->AddToRtmpConnectionString(connectionString, temp);
  FREE_MEM(temp);

  return result;
}

bool CRtmpCurlInstance::AddToRtmpConnectionString(wchar_t **connectionString, const wchar_t *string)
{
  if ((connectionString == NULL) || (*connectionString == NULL) || (string == NULL))
  {
    return false;
  }

  wchar_t *temp = FormatString(L"%s %s", *connectionString, string);
  FREE_MEM(*connectionString);

  *connectionString = temp;

  return (*connectionString != NULL);
}

void CRtmpCurlInstance::CurlDebug(curl_infotype type, const wchar_t *data)
{
}