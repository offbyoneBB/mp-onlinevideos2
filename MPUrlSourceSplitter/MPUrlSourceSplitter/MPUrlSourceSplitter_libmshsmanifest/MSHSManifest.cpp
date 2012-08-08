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

#include "MSHSManifest.h"
#include "MSHS_Elements.h"

#include "conversions.h"
#include "tinyxml2.h"

CMSHSManifest::CMSHSManifest(void)
{
  this->isXml = false;
  this->parseError = XML_NO_ERROR;
  this->smoothStreamingMedia = NULL;
}

/* get methods */

CMSHSManifest::~CMSHSManifest(void)
{
  FREE_MEM_CLASS(this->smoothStreamingMedia);
}

int CMSHSManifest::GetParseError(void)
{
  return this->parseError;
}

CMSHSSmoothStreamingMedia *CMSHSManifest::GetSmoothStreamingMedia(void)
{
  return this->smoothStreamingMedia;
}

/* set methods */

/* other methods */

bool CMSHSManifest::IsXml(void)
{
  return this->isXml;
}

bool CMSHSManifest::Parse(const char *buffer)
{
  this->isXml = false;
  this->parseError = XML_NO_ERROR;

  FREE_MEM_CLASS(this->smoothStreamingMedia);
  this->smoothStreamingMedia = new CMSHSSmoothStreamingMedia();

  bool result = false;
  bool continueParsing = ((this->smoothStreamingMedia != NULL) && (this->smoothStreamingMedia->GetProtections() != NULL));

  if (continueParsing && (buffer != NULL))
  {
    this->smoothStreamingMedia->GetProtections()->Clear();

    XMLDocument *document = new XMLDocument();

    if (document != NULL)
    {
      // parse buffer, if no error, continue in parsing
      this->parseError = document->Parse(buffer);
      if (this->parseError == XML_NO_ERROR)
      {
        this->isXml = true;

        XMLElement *manifest = document->FirstChildElement(MSHS_ELEMENT_MANIFEST);
        if (manifest != NULL)
        {
          // correct MSHS manifest, continue in parsing

          // check manifest attributes
          const char *value = manifest->Attribute(MSHS_ELEMENT_MANIFEST_ATTRIBUTE_MAJOR_VERSION);
          if (value != NULL)
          {
            this->smoothStreamingMedia->SetMajorVersion(GetValueUnsignedIntA(value, MANIFEST_MAJOR_VERSION));
          }
          value = manifest->Attribute(MSHS_ELEMENT_MANIFEST_ATTRIBUTE_MINOR_VERSION);
          if (value != NULL)
          {
            this->smoothStreamingMedia->SetMinorVersion(GetValueUnsignedIntA(value, MANIFEST_MINOR_VERSION));
          }
          value = manifest->Attribute(MSHS_ELEMENT_MANIFEST_ATTRIBUTE_TIMESCALE);
          if (value != NULL)
          {
            this->smoothStreamingMedia->SetTimeScale(GetValueUnsignedInt64A(value, MANIFEST_TIMESCALE_DEFAULT));
          }
          value = manifest->Attribute(MSHS_ELEMENT_MANIFEST_ATTRIBUTE_DURATION);
          if (value != NULL)
          {
            this->smoothStreamingMedia->SetDuration(GetValueUnsignedInt64A(value, 0));
          }

          continueParsing &= ((this->smoothStreamingMedia->GetMajorVersion() == MANIFEST_MAJOR_VERSION) && (this->smoothStreamingMedia->GetMinorVersion() == MANIFEST_MINOR_VERSION));

          if (continueParsing)
          {
            XMLElement *child = manifest->FirstChildElement();
            if (child != NULL)
            {
              do
              {
                if (strcmp(child->Name(), MSHS_ELEMENT_PROTECTION) == 0)
                {
                  // protection element, parse it and add to protection collection
                  XMLElement *protectionHeader = child->FirstChildElement(MSHS_ELEMENT_PROTECTION_ELEMENT_PROTECTION_HEADER);
                  if (protectionHeader != NULL)
                  {
                    wchar_t *systemId = ConvertUtf8ToUnicode(protectionHeader->Attribute(MSHS_ELEMENT_PROTECTION_ELEMENT_PROTECTION_HEADER_ATTRIBUTE_SYSTEMID));
                    wchar_t *content = ConvertUtf8ToUnicode(protectionHeader->GetText());

                    CMSHSProtection *protection = new CMSHSProtection();
                    continueParsing &= (protection != NULL);

                    if (continueParsing)
                    {
                      protection->SetSystemId(ConvertStringToGuid(systemId));
                      continueParsing &= protection->SetContent(content);

                      if (continueParsing)
                      {
                        continueParsing &= this->smoothStreamingMedia->GetProtections()->Add(protection);
                      }
                    }

                    FREE_MEM(content);
                    if (!continueParsing)
                    {
                      FREE_MEM_CLASS(protection);
                    }
                  }
                }

                if (strcmp(child->Name(), MSHS_ELEMENT_STREAM) == 0)
                {
                  // stream element

                  wchar_t *type = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_TYPE));
                  wchar_t *subType = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_SUBTYPE));;
                  wchar_t *url = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_URL));
                  wchar_t *timeScale = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_STREAM_TIMESCALE));
                  wchar_t *name = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_NAME));
                  wchar_t *numberOfFragments = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_NUMBER_OF_FRAGMENTS));
                  wchar_t *numberOfTracks = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_NUMBER_OF_TRACKS));
                  wchar_t *maxWidth = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_STREAM_MAX_WIDTH));
                  wchar_t *maxHeight = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_STREAM_MAX_HEIGHT));
                  wchar_t *displayWidth = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_DISPLAY_WIDTH));
                  wchar_t *displayHeight = ConvertUtf8ToUnicode(child->Attribute(MSHS_ELEMENT_STREAM_ATTRIBUTE_DISPLAY_HEIGHT));

                  CMSHSStream *stream = new CMSHSStream();
                  continueParsing &= (stream != NULL);

                  if (continueParsing)
                  {
                    continueParsing &= stream->SetType(type);
                    continueParsing &= stream->SetSubType(subType);
                    continueParsing &= stream->SetUrl(url);
                    stream->SetTimeScale(GetValueUnsignedInt64(timeScale, this->smoothStreamingMedia->GetTimeScale()));
                    continueParsing &= stream->SetName(name);
                    stream->SetNumberOfFragments(GetValueUnsignedInt(numberOfFragments, 0));
                    stream->SetNumberOfTracks(GetValueUnsignedInt(numberOfTracks, 0));
                    stream->SetMaxWidth(GetValueUnsignedInt(maxWidth, 0));
                    stream->SetMaxHeight(GetValueUnsignedInt(maxHeight, 0));
                    stream->SetDisplayWidth(GetValueUnsignedInt(displayWidth, 0));
                    stream->SetDisplayHeight(GetValueUnsignedInt(displayHeight, 0));

                    if (continueParsing)
                    {
                      continueParsing &= this->smoothStreamingMedia->GetStreams()->Add(stream);
                    }
                  }

                  if (!continueParsing)
                  {
                    FREE_MEM_CLASS(stream);
                  }

                  /*
  wchar_t *type;
  wchar_t *subType;
  wchar_t *url;
  uint64_t timeScale;
  wchar_t *name;
  uint32_t numberOfFragments;
  uint32_t numberOfTracks;
  uint32_t maxWidth;
  uint32_t maxHeight;
  uint32_t displayWidth;
  uint32_t displayHeight;
                  */
                }

              //  // bootstrap info
              //  if (strcmp(child->Name(), F4M_ELEMENT_BOOTSTRAPINFO) == 0)
              //  {
              //    // we found bootstrap info element
              //    wchar_t *id = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_ID));
              //    wchar_t *profile = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE));
              //    wchar_t *url = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_URL));
              //    wchar_t *convertedValue = ConvertUtf8ToUnicode(child->GetText());
              //    wchar_t *value = Trim(convertedValue);
              //    FREE_MEM(convertedValue);

              //    continueParsing &= this->bootstrapInfo->SetId((id == NULL) ? L"" : id);
              //    continueParsing &= this->bootstrapInfo->SetProfile(profile);
              //    continueParsing &= this->bootstrapInfo->SetUrl(url);
              //    continueParsing &= this->bootstrapInfo->SetValue(value);

              //    FREE_MEM(id);
              //    FREE_MEM(profile);
              //    FREE_MEM(url);
              //    FREE_MEM(value);
              //  }

              //  // piece of media
              //  if (strcmp(child->Name(), F4M_ELEMENT_MEDIA) == 0)
              //  {
              //    // we found piece of media
              //    wchar_t *url = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_URL));
              //    wchar_t *bitrate = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BITRATE));
              //    wchar_t *width = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_WIDTH));
              //    wchar_t *height = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_HEIGHT));
              //    wchar_t *drmAdditionalHeaderId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DRMADDITTIONALHEADERID));
              //    wchar_t *bootstrapInfoId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BOOTSTRAPINFOID));
              //    wchar_t *dvrInfoId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DVRINFOID));
              //    wchar_t *groupspec = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_GROUPSPEC));
              //    wchar_t *multicastStreamName = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_MULTICASTSTREAMNAME));
              //    wchar_t *metadataValue = NULL;

              //    XMLElement *metadata = child->FirstChildElement(F4M_ELEMENT_MEDIA_ELEMENT_METADATA);
              //    if (metadata != NULL)
              //    {
              //      wchar_t *convertedMetadata = ConvertUtf8ToUnicode(metadata->GetText());
              //      metadataValue = Trim(convertedMetadata);
              //      FREE_MEM(convertedMetadata);
              //    }

              //    unsigned int bitrateValue = GetValueUnsignedInt(bitrate, UINT_MAX);
              //    unsigned int widthValue = GetValueUnsignedInt(width, UINT_MAX);
              //    unsigned int heightValue = GetValueUnsignedInt(height, UINT_MAX);

              //    CF4MMedia *media = new CF4MMedia();
              //    continueParsing &= (media != NULL);

              //    if (continueParsing)
              //    {
              //      continueParsing &= media->SetBitrate(bitrateValue);
              //      continueParsing &= media->SetWidth(widthValue);
              //      continueParsing &= media->SetHeight(heightValue);

              //      continueParsing &= media->SetUrl(url);
              //      continueParsing &= media->SetDrmAdditionalHeaderId(drmAdditionalHeaderId);
              //      continueParsing &= media->SetBootstrapInfoId(bootstrapInfoId);
              //      continueParsing &= media->SetDvrInfoId(dvrInfoId);
              //      continueParsing &= media->SetGroupSpecifier(groupspec);
              //      continueParsing &= media->SetMulticastStreamName(multicastStreamName);
              //      continueParsing &= media->SetMetadata(metadataValue);

              //      continueParsing &= this->mediaCollection->Add(media);
              //    }

              //    if (!continueParsing)
              //    {
              //      FREE_MEM_CLASS(media);
              //    }

              //    FREE_MEM(url);
              //    FREE_MEM(bitrate);
              //    FREE_MEM(width);
              //    FREE_MEM(height);
              //    FREE_MEM(drmAdditionalHeaderId);
              //    FREE_MEM(bootstrapInfoId);
              //    FREE_MEM(dvrInfoId);
              //    FREE_MEM(groupspec);
              //    FREE_MEM(multicastStreamName);
              //    FREE_MEM(metadataValue);
              //  }

              //  // delivery type
              //  if (strcmp(child->Name(), F4M_ELEMENT_DELIVERYTYPE) == 0)
              //  {
              //    wchar_t *deliveryType = ConvertUtf8ToUnicode(child->GetText());
              //    this->deliveryType->SetDeliveryType(deliveryType);
              //    FREE_MEM(deliveryType);
              //  }

              //  // base URL - it's replacing manifest URL
              //  if (strcmp(child->Name(), F4M_ELEMENT_BASEURL) == 0)
              //  {
              //    wchar_t *baseUrl = ConvertUtf8ToUnicode(child->GetText());
              //    continueParsing &= this->baseUrl->SetBaseUrl(baseUrl);
              //    FREE_MEM(baseUrl);
              //  }
              }
              while (continueParsing && ((child = child->NextSiblingElement()) != NULL));
            }
          }
        }

        result = continueParsing;
      }
      else
      {
        XMLNode *child = document->FirstChild();
        if (child != NULL)
        {
          XMLDeclaration *declaration = child->ToDeclaration();
          if (declaration != NULL)
          {
            this->isXml = true;
          }
        }
      }
    }
  }

  return result;
}