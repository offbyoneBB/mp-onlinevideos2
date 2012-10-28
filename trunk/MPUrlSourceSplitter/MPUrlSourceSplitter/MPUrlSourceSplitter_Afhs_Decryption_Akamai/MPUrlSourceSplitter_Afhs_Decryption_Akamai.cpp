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

#include "MPUrlSourceSplitter_Afhs_Decryption_akamai.h"
#include "VersionInfo.h"

// AFHS decryption plugin implementation name
#ifdef _DEBUG
#define PLUGIN_IMPLEMENTATION_NAME                                            L"MPUrlSourceSplitter_Afhs_Decryption_akamaid"
#else
#define PLUGIN_IMPLEMENTATION_NAME                                            L"MPUrlSourceSplitter_Afhs_Decryption_akamai"
#endif

PIPlugin CreatePluginInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Afhs_Decryption_Akamai(configuration);
}

void DestroyPluginInstance(PIPlugin pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Afhs_Decryption_Akamai *pClass = (CMPUrlSourceSplitter_Afhs_Decryption_Akamai *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Afhs_Decryption_Akamai::CMPUrlSourceSplitter_Afhs_Decryption_Akamai(CParameterCollection *configuration)
{
  this->configurationParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->configurationParameters->Append(configuration);
  }

  this->logger = new CLogger(this->configurationParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_AFHS_DECRYPTION_AKAMAI, COMPILE_INFO_MPURLSOURCESPLITTER_AFHS_DECRYPTION_AKAMAI);
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);

  /*version = CCurlInstance::GetCurlVersion();
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);
  
  this->receiveDataTimeout = AFHS_RECEIVE_DATA_TIMEOUT_DEFAULT;
  this->openConnetionMaximumAttempts = AFHS_OPEN_CONNECTION_MAXIMUM_ATTEMPTS_DEFAULT;
  this->streamLength = 0;
  this->setLength = false;
  this->setEndOfStream = false;
  this->streamTime = 0;
  this->lockMutex = CreateMutex(NULL, FALSE, NULL);
  this->wholeStreamDownloaded = false;
  this->mainCurlInstance = NULL;
  this->bootstrapInfoCurlInstance = NULL;
  this->bytePosition = 0;
  this->seekingActive = false;
  this->supressData = false;
  this->bufferForProcessing = NULL;
  this->shouldExit = false;
  this->bootstrapInfoBox = NULL;
  this->segmentsFragments = NULL;
  this->live = false;
  this->lastBootstrapInfoRequestTime = 0;
  this->storeFilePath = NULL;
  this->lastStoreTime = 0;
  this->isConnected = false;
  this->segmentFragmentDownloading = UINT_MAX;
  this->segmentFragmentProcessing = 0;
  this->segmentFragmentToDownload = UINT_MAX;

  this->decryptionHoster = new CAfhsDecryptionHoster(this->logger, this->configurationParameters);
  if (this->decryptionHoster != NULL)
  {
    this->decryptionHoster->LoadPlugins();
  }*/

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Afhs_Decryption_Akamai::~CMPUrlSourceSplitter_Afhs_Decryption_Akamai()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  /*if (this->IsConnected())
  {
    this->StopReceivingData();
  }

  FREE_MEM_CLASS(this->mainCurlInstance);
  FREE_MEM_CLASS(this->bootstrapInfoCurlInstance);

  if (this->decryptionHoster != NULL)
  {
    this->decryptionHoster->RemoveAllPlugins();
    FREE_MEM_CLASS(this->decryptionHoster);
  }

  if (this->storeFilePath != NULL)
  {
    DeleteFile(this->storeFilePath);
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
  FREE_MEM(this->storeFilePath);*/

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PLUGIN_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  FREE_MEM_CLASS(this->logger);
}

// IPlugin interface

const wchar_t *CMPUrlSourceSplitter_Afhs_Decryption_Akamai::GetName(void)
{
  return DECRYPTION_NAME;
}

GUID CMPUrlSourceSplitter_Afhs_Decryption_Akamai::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

HRESULT CMPUrlSourceSplitter_Afhs_Decryption_Akamai::Initialize(PluginConfiguration *configuration)
{
  if (configuration == NULL)
  {
    return E_POINTER;
  }
  
  AfhsDecryptionPluginConfiguration *pluginConfiguration = (AfhsDecryptionPluginConfiguration *)configuration;
  this->logger->SetParameters(pluginConfiguration->configuration);

  this->configurationParameters->Clear();
  if (pluginConfiguration->configuration != NULL)
  {
    this->configurationParameters->Append(pluginConfiguration->configuration);
  }
  this->configurationParameters->LogCollection(this->logger, LOGGER_VERBOSE, PLUGIN_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  return S_OK;
}

// IAfhsSimpleDecryptionPlugin interface

HRESULT CMPUrlSourceSplitter_Afhs_Decryption_Akamai::ClearSession(void)
{
  return S_OK;
}

HRESULT CMPUrlSourceSplitter_Afhs_Decryption_Akamai::ProcessSegmentsAndFragments(CSegmentFragmentCollection *segmentsFragments)
{
  return S_OK;
}

// IAfhsDecryptionPlugin interface

DecryptionResult CMPUrlSourceSplitter_Afhs_Decryption_Akamai::Supported(CSegmentFragmentCollection *segmentsFragments)
{
  return DecryptionResult_NotKnown;
}
