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

#include "MPUrlSourceSplitter_Parser_F4M.h"
#include "VersionInfo.h"
#include "..\LAVSplitter\VersionInfo.h"
#include "BootstrapInfoCollection.h"
#include "MediaCollection.h"
#include "MPUrlSourceSplitter_Protocol_Afhs_Parameters.h"

#include "tinyxml2.h"

// parser implementation name
#ifdef _DEBUG
#define PARSER_IMPLEMENTATION_NAME                                      L"MPUrlSourceSplitter_Parser_F4Md"
#else
#define PARSER_IMPLEMENTATION_NAME                                      L"MPUrlSourceSplitter_Parser_F4M"
#endif

unsigned int GetValueUnsignedInt(wchar_t *input, unsigned int defaultValue)
{
  wchar_t *end = NULL;
  long valueLong = wcstol((input == NULL) ? L"" : input, &end, 10);
  if ((valueLong == 0) && (input == end))
  {
    // error while converting
    valueLong = defaultValue;
  }

  return (unsigned int)valueLong;
}

PIPlugin CreatePluginInstance(CParameterCollection *configuration)
{
  return new CMPUrlSourceSplitter_Parser_F4M(configuration);
}

void DestroyPluginInstance(PIPlugin pProtocol)
{
  if (pProtocol != NULL)
  {
    CMPUrlSourceSplitter_Parser_F4M *pClass = (CMPUrlSourceSplitter_Parser_F4M *)pProtocol;
    delete pClass;
  }
}

CMPUrlSourceSplitter_Parser_F4M::CMPUrlSourceSplitter_Parser_F4M(CParameterCollection *configuration)
{
  this->connectionParameters = new CParameterCollection();
  if (configuration != NULL)
  {
    this->connectionParameters->Append(configuration);
  }

  this->logger = new CLogger(this->connectionParameters);
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);

  wchar_t *version = GetVersionInfo(VERSION_INFO_MPURLSOURCESPLITTER_PARSER_F4M, COMPILE_INFO_MPURLSOURCESPLITTER_PARSER_F4M);
  if (version != NULL)
  {
    this->logger->Log(LOGGER_INFO, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME, version);
  }
  FREE_MEM(version);

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_CONSTRUCTOR_NAME);
}

CMPUrlSourceSplitter_Parser_F4M::~CMPUrlSourceSplitter_Parser_F4M()
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->connectionParameters;

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_DESTRUCTOR_NAME);

  delete this->logger;
  this->logger = NULL;
}

// IParser interface

HRESULT CMPUrlSourceSplitter_Parser_F4M::ClearSession(void)
{
  this->logger->Log(LOGGER_INFO, METHOD_START_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);

  this->connectionParameters->Clear();

  this->logger->Log(LOGGER_INFO, METHOD_END_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_CLEAR_SESSION_NAME);
  return S_OK;
}

ParseResult CMPUrlSourceSplitter_Parser_F4M::ParseMediaPacket(CMediaPacket *mediaPacket)
{
  ParseResult result = ParseResult_NotKnown;
  this->logger->Log(LOGGER_VERBOSE, METHOD_START_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME);

  if (mediaPacket != NULL)
  {
    unsigned int length = mediaPacket->GetBuffer()->GetBufferOccupiedSpace() + 1;
    ALLOC_MEM_DEFINE_SET(buffer, char, length, 0);
    if ((buffer != NULL) && (length > 1))
    {
      mediaPacket->GetBuffer()->CopyFromBuffer(buffer, length - 1, 0, 0);

      XMLDocument *document = new XMLDocument();

      if (document != NULL)
      {
        // parse received data, if no error, continue in parsing
        if (document->Parse(buffer) == XML_NO_ERROR)
        {
          XMLElement *manifest = document->FirstChildElement(F4M_ELEMENT_MANIFEST);
          if (manifest != NULL)
          {
            // manifest element is in XML document, check xmlns attribute
            const char *xmlnsValue = manifest->Attribute(F4M_ELEMENT_MANIFEST_ATTRIBUTE_XMLNS);
            if (xmlnsValue != NULL)
            {
              if (strcmp(xmlnsValue, F4M_ELEMENT_MANIFEST_ATTRIBUTE_XMLNS_VALUE) == 0)
              {
                // correct F4M file, continue in parsing

                this->logger->Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"F4M manifest");
                wchar_t *asxBuffer = ConvertToUnicodeA(buffer);
                if (asxBuffer != NULL)
                {
                  this->logger->Log(LOGGER_VERBOSE, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, asxBuffer);
                }
                FREE_MEM(asxBuffer);

                // parse bootstrap info
                // bootstrap info should have information about segments, fragments and seeking information

                CBootstrapInfoCollection *bootstrapInfoCollection = new CBootstrapInfoCollection();
                CMediaCollection *mediaCollection = new CMediaCollection();

                if ((bootstrapInfoCollection != NULL) && (mediaCollection != NULL))
                {
                  bool continueParsing = true;
                  XMLElement *child = manifest->FirstChildElement();
                  if (child != NULL)
                  {
                    do
                    {
                      // bootstrap info
                      if (strcmp(child->Name(), F4M_ELEMENT_BOOTSTRAPINFO) == 0)
                      {
                        // we found bootstrap info, insert it into collection
                        wchar_t *id = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_ID));
                        wchar_t *profile = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE));
                        wchar_t *url = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_URL));
                        wchar_t *value = ConvertToUnicodeA(child->GetText());

                        // bootstrap info profile have to be 'named' (F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE_VALUE_NAMED)
                        if ((profile != NULL) && (strcmp(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE), F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE_VALUE_NAMED) == 0))
                        {
                          CBootstrapInfo *bootstrapInfo = new CBootstrapInfo(id, profile, url, value);
                          if (bootstrapInfo != NULL)
                          {
                            if (bootstrapInfo->IsValid())
                            {
                              bootstrapInfoCollection->Add(bootstrapInfo);
                            }
                            else
                            {
                              this->logger->Log(LOGGER_WARNING, L"%s: %s: bootstrap info is not valid, id: %s", PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, bootstrapInfo->GetId());
                              delete bootstrapInfo;
                              bootstrapInfo = NULL;
                            }
                          }
                        }
                        else
                        {
                          this->logger->Log(LOGGER_WARNING, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"bootstrap info profile is not 'named'");
                        }

                        FREE_MEM(id);
                        FREE_MEM(profile);
                        FREE_MEM(url);
                        FREE_MEM(value);
                      }

                      // piece of media
                      if (strcmp(child->Name(), F4M_ELEMENT_MEDIA) == 0)
                      {
                        // we found piece of media, insert it into collection
                        wchar_t *url = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_URL));
                        wchar_t *bitrate = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BITRATE));
                        wchar_t *width = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_WIDTH));
                        wchar_t *height = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_HEIGHT));
                        wchar_t *drmAdditionalHeaderId = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DRMADDITTIONALHEADERID));
                        wchar_t *bootstrapInfoId = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BOOTSTRAPINFOID));
                        wchar_t *dvrInfoId = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DVRINFOID));
                        wchar_t *groupspec = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_GROUPSPEC));
                        wchar_t *multicastStreamName = ConvertToUnicodeA(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_MULTICASTSTREAMNAME));
                        wchar_t *metadataValue = NULL;

                        XMLElement *metadata = child->FirstChildElement(F4M_ELEMENT_MEDIA_ELEMENT_METADATA);
                        if (metadata != NULL)
                        {
                          metadataValue = ConvertToUnicodeA(metadata->GetText());
                        }

                        unsigned int bitrateValue = GetValueUnsignedInt(bitrate, UINT_MAX);
                        unsigned int widthValue = GetValueUnsignedInt(width, UINT_MAX);
                        unsigned int heightValue = GetValueUnsignedInt(height, UINT_MAX);
                        
                        // we should have url and bootstrapInfoId
                        // we exclude piece of media with drmAdditionalHeaderId
                        if ((url != NULL) && (bootstrapInfoId != NULL) && (drmAdditionalHeaderId == NULL))
                        {
                          CMedia *media = new CMedia(url, bitrateValue, widthValue, heightValue, drmAdditionalHeaderId, bootstrapInfoId, dvrInfoId, groupspec, multicastStreamName, metadataValue);
                          if (media != NULL)
                          {
                            mediaCollection->Add(media);
                          }
                        }
                        else
                        {
                          this->logger->Log(LOGGER_WARNING, L"%s: %s: piece of media doesn't have url ('%s'), bootstrap info ID ('%s') or has DRM additional header ID ('%s')", PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, url, bootstrapInfoId, drmAdditionalHeaderId);
                        }

                        FREE_MEM(url);
                        FREE_MEM(bitrate);
                        FREE_MEM(width);
                        FREE_MEM(height);
                        FREE_MEM(drmAdditionalHeaderId);
                        FREE_MEM(bootstrapInfoId);
                        FREE_MEM(dvrInfoId);
                        FREE_MEM(groupspec);
                        FREE_MEM(multicastStreamName);
                        FREE_MEM(metadataValue);
                      }

                      // delivery type
                      if (strcmp(child->Name(), F4M_ELEMENT_DELIVERYTYPE) == 0)
                      {
                        if (strcmp(child->GetText(), F4M_ELEMENT_DELIVERYTYPE_VALUE_STREAMING) != 0)
                        {
                          // it's not HTTP streaming
                          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"delivery type is not 'streaming'");
                          continueParsing = false;
                        }
                      }
                    }
                    while (continueParsing && ((child = child->NextSiblingElement()) != NULL));
                  }

                  if (continueParsing && (bootstrapInfoCollection->Count() == 0))
                  {
                    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"no bootstrap info profile");
                    continueParsing = false;
                  }

                  if (continueParsing)
                  {
                    unsigned int i = 0;
                    while (i < mediaCollection->Count())
                    {
                      CMedia *media = mediaCollection->GetItem(i);
                      if (!bootstrapInfoCollection->Contains((wchar_t *)media->GetBootstrapInfoId(), false))
                      {
                        this->logger->Log(LOGGER_ERROR, L"%s: %s: no bootstrap info '%s' for media '%s'", PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, media->GetBootstrapInfoId(), media->GetUrl());
                        mediaCollection->Remove(i);
                      }
                      else
                      {
                        i++;
                      }
                    }
                  }

                  if (continueParsing && (mediaCollection->Count() == 0))
                  {
                    this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"no piece of media");
                    continueParsing = false;
                  }

                  if (continueParsing)
                  {
                    // at least one media with bootstrap info and without DRM
                    // find media with highest bitrate

                    unsigned int bitrate = 0;
                    unsigned int i = 0;
                    CMedia *mediaWithHighestBitstream = NULL;

                    for (unsigned int i = 0; i < mediaCollection->Count(); i++)
                    {
                      CMedia *media = mediaCollection->GetItem(i);
                      if (media->GetBitrate() > bitrate)
                      {
                        mediaWithHighestBitstream = media;
                        bitrate = media->GetBitrate();
                      }
                    }

                    if (mediaWithHighestBitstream != NULL)
                    {
                      wchar_t *manifestUrl = Duplicate(this->connectionParameters->GetValue(PARAMETER_NAME_URL, true, NULL));
                      if (manifestUrl != NULL)
                      {
                        CParameter *manifestUrlParameter = new CParameter(PARAMETER_NAME_AFHS_MANIFEST_URL, manifestUrl);
                        continueParsing &= this->connectionParameters->Add(manifestUrlParameter);
                      }
                      else
                      {
                        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"cannot get manifest url");
                        continueParsing = false;
                      }
                      FREE_MEM(manifestUrl);

                      // add media url into connection parameters
                      CParameter *mediaUrlParameter = new CParameter(PARAMETER_NAME_AFHS_MEDIA_URL, mediaWithHighestBitstream->GetUrl());
                      continueParsing &= this->connectionParameters->Add(mediaUrlParameter);

                      // add media metadata into connection parameters
                      CParameter *mediaMetadataParameter = new CParameter(PARAMETER_NAME_AFHS_MEDIA_METADATA, mediaWithHighestBitstream->GetMetadata());
                      continueParsing &= this->connectionParameters->Add(mediaMetadataParameter);

                      // add bootstrap info into connection parameters
                      CBootstrapInfo *bootstrapInfo = bootstrapInfoCollection->GetBootstrapInfo(mediaWithHighestBitstream->GetBootstrapInfoId(), false);
                      if (bootstrapInfo != NULL)
                      {
                        CParameter *bootstrapInfoParameter = new CParameter(PARAMETER_NAME_AFHS_BOOTSTRAP_INFO, bootstrapInfo->GetValue());
                        continueParsing &= this->connectionParameters->Add(bootstrapInfoParameter);
                      }
                      else
                      {
                        this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"cannot create bootstrap info parameter");
                        continueParsing = false;
                      }

                      if (continueParsing)
                      {
                        wchar_t *replacedUrl = ReplaceString(this->connectionParameters->GetValue(PARAMETER_NAME_URL, true, NULL), L"http://", L"afhs://");
                        if (replacedUrl != NULL)
                        {
                          if (wcsstr(replacedUrl, L"afhs://") != NULL)
                          {
                            CParameter *urlParameter = new CParameter(PARAMETER_NAME_URL, replacedUrl);
                            if (urlParameter != NULL)
                            {
                              bool invariant = true;
                              this->connectionParameters->Remove(PARAMETER_NAME_URL, (void *)&invariant);
                              continueParsing &= this->connectionParameters->Add(urlParameter);

                              if (continueParsing)
                              {
                                result = ParseResult_Known;
                              }
                            }
                            else
                            {
                              this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"cannot create new URL parameter");
                            }
                          }
                          else
                          {
                            this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"only HTTP protocol supported as manifest url");
                          }
                        }
                        else
                        {
                          this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"cannot specify AFHS protocol");
                        }
                        FREE_MEM(replacedUrl);
                      }
                    }
                    else
                    {
                      // this should not happen, just for sure
                      this->logger->Log(LOGGER_ERROR, METHOD_MESSAGE_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, L"no piece of media with highest bitrate");
                    }
                  }
                }

                if (bootstrapInfoCollection != NULL)
                {
                  delete bootstrapInfoCollection;
                  bootstrapInfoCollection = NULL;
                }

                if (mediaCollection != NULL)
                {
                  delete mediaCollection;
                  mediaCollection = NULL;
                }
              }
            }
          }
        }

        // remove document
        delete document;
        document = NULL;
      }
    }
    FREE_MEM(buffer);
  }

  this->logger->Log(LOGGER_VERBOSE, METHOD_END_INT_FORMAT, PARSER_IMPLEMENTATION_NAME, METHOD_PARSE_MEDIA_PACKET_NAME, result);
  return result;
}

HRESULT CMPUrlSourceSplitter_Parser_F4M::SetConnectionParameters(const CParameterCollection *parameters)
{
  if (parameters != NULL)
  {
    this->connectionParameters->Append((CParameterCollection *)parameters);
  }
  return S_OK;
}

Action CMPUrlSourceSplitter_Parser_F4M::GetAction(void)
{
  return Action_GetNewConnection;
}

HRESULT CMPUrlSourceSplitter_Parser_F4M::GetConnectionParameters(CParameterCollection *parameters)
{
  HRESULT result = (parameters == NULL) ? E_POINTER : S_OK;

  if (SUCCEEDED(result))
  {
    parameters->Append(this->connectionParameters);
  }

  return result;
}

// IPlugin interface

wchar_t *CMPUrlSourceSplitter_Parser_F4M::GetName(void)
{
  return Duplicate(PARSER_NAME);
}

GUID CMPUrlSourceSplitter_Parser_F4M::GetInstanceId(void)
{
  return this->logger->loggerInstance;
}

HRESULT CMPUrlSourceSplitter_Parser_F4M::Initialize(PluginConfiguration *configuration)
{
  if (configuration == NULL)
  {
    return E_POINTER;
  }

  ParserPluginConfiguration *parserPluginConfiguration = (ParserPluginConfiguration *)configuration;

  this->connectionParameters->Clear();
  if (parserPluginConfiguration->configuration != NULL)
  {
    this->logger->SetParameters(configuration->configuration);
    this->connectionParameters->Append(parserPluginConfiguration->configuration);
  }
  this->connectionParameters->LogCollection(this->logger, LOGGER_VERBOSE, PARSER_IMPLEMENTATION_NAME, METHOD_INITIALIZE_NAME);

  return S_OK;
}
