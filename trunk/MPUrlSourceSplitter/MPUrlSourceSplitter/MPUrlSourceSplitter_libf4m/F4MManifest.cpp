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

#include "F4MManifest.h"
#include "F4M_Elements.h"

#include "conversions.h"
#include "tinyxml2.h"

CF4MManifest::CF4MManifest(void)
{
  this->isXml = false;
  this->parseError = XML_NO_ERROR;

  this->bootstrapInfo = new CF4MBootstrapInfo();
  this->mediaCollection = new CF4MMediaCollection();
  this->deliveryType = new CF4MDeliveryType();
  this->baseUrl = new CF4MBaseUrl();
}

/* get methods */

CF4MManifest::~CF4MManifest(void)
{
  FREE_MEM_CLASS(this->bootstrapInfo);
  FREE_MEM_CLASS(this->mediaCollection);
  FREE_MEM_CLASS(this->deliveryType);
  FREE_MEM_CLASS(this->baseUrl);
}

CF4MBootstrapInfo *CF4MManifest::GetBootstrapInfo(void)
{
  return this->bootstrapInfo;
}

CF4MMediaCollection *CF4MManifest::GetMediaCollection(void)
{
  return this->mediaCollection;
}

CF4MDeliveryType *CF4MManifest::GetDeliveryType(void)
{
  return this->deliveryType;
}

CF4MBaseUrl *CF4MManifest::GetBaseUrl(void)
{
  return this->baseUrl;
}

int CF4MManifest::GetParseError(void)
{
  return this->parseError;
}

/* set methods */

/* other methods */

bool CF4MManifest::IsXml(void)
{
  return this->isXml;
}

bool CF4MManifest::Parse(const char *buffer)
{
  this->isXml = false;
  this->parseError = XML_NO_ERROR;
  FREE_MEM_CLASS(this->baseUrl);
  FREE_MEM_CLASS(this->bootstrapInfo);
  FREE_MEM_CLASS(this->deliveryType);
  FREE_MEM_CLASS(this->mediaCollection);

  this->baseUrl = new CF4MBaseUrl();
  this->bootstrapInfo = new CF4MBootstrapInfo();
  this->deliveryType = new CF4MDeliveryType();
  this->mediaCollection = new CF4MMediaCollection();

  bool result = false;
  bool continueParsing = ((this->baseUrl != NULL) && (this->bootstrapInfo != NULL) && (this->deliveryType != NULL) && (this->mediaCollection != NULL));

  if (continueParsing && (buffer != NULL))
  {
    XMLDocument *document = new XMLDocument();

    if (document != NULL)
    {
      // parse buffer, if no error, continue in parsing
      this->parseError = document->Parse(buffer);
      if (this->parseError == XML_NO_ERROR)
      {
        this->isXml = true;

        XMLElement *manifest = document->FirstChildElement(F4M_ELEMENT_MANIFEST);
        if (manifest != NULL)
        {
          // manifest element is in XML document, check xmlns attribute
          const char *xmlnsValue = manifest->Attribute(F4M_ELEMENT_MANIFEST_ATTRIBUTE_XMLNS);
          if (xmlnsValue != NULL)
          {
            if (strcmp(xmlnsValue, F4M_ELEMENT_MANIFEST_ATTRIBUTE_XMLNS_VALUE) == 0)
            {
              // correct F4M manifest, continue in parsing

              XMLElement *child = manifest->FirstChildElement();
              if (child != NULL)
              {
                do
                {
                  // bootstrap info
                  if (strcmp(child->Name(), F4M_ELEMENT_BOOTSTRAPINFO) == 0)
                  {
                    // we found bootstrap info element
                    wchar_t *id = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_ID));
                    wchar_t *profile = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_PROFILE));
                    wchar_t *url = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_BOOTSTRAPINFO_ATTRIBUTE_URL));
                    wchar_t *convertedValue = ConvertUtf8ToUnicode(child->GetText());
                    wchar_t *value = Trim(convertedValue);
                    FREE_MEM(convertedValue);

                    continueParsing &= this->bootstrapInfo->SetId((id == NULL) ? L"" : id);
                    continueParsing &= this->bootstrapInfo->SetProfile(profile);
                    continueParsing &= this->bootstrapInfo->SetUrl(url);
                    continueParsing &= this->bootstrapInfo->SetValue(value);

                    FREE_MEM(id);
                    FREE_MEM(profile);
                    FREE_MEM(url);
                    FREE_MEM(value);
                  }

                  // piece of media
                  if (strcmp(child->Name(), F4M_ELEMENT_MEDIA) == 0)
                  {
                    // we found piece of media
                    wchar_t *url = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_URL));
                    wchar_t *bitrate = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BITRATE));
                    wchar_t *width = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_WIDTH));
                    wchar_t *height = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_HEIGHT));
                    wchar_t *drmAdditionalHeaderId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DRMADDITTIONALHEADERID));
                    wchar_t *bootstrapInfoId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_BOOTSTRAPINFOID));
                    wchar_t *dvrInfoId = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_DVRINFOID));
                    wchar_t *groupspec = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_GROUPSPEC));
                    wchar_t *multicastStreamName = ConvertUtf8ToUnicode(child->Attribute(F4M_ELEMENT_MEDIA_ATTRIBUTE_MULTICASTSTREAMNAME));
                    wchar_t *metadataValue = NULL;

                    XMLElement *metadata = child->FirstChildElement(F4M_ELEMENT_MEDIA_ELEMENT_METADATA);
                    if (metadata != NULL)
                    {
                      wchar_t *convertedMetadata = ConvertUtf8ToUnicode(metadata->GetText());
                      metadataValue = Trim(convertedMetadata);
                      FREE_MEM(convertedMetadata);
                    }

                    unsigned int bitrateValue = GetValueUnsignedInt(bitrate, UINT_MAX);
                    unsigned int widthValue = GetValueUnsignedInt(width, UINT_MAX);
                    unsigned int heightValue = GetValueUnsignedInt(height, UINT_MAX);

                    CF4MMedia *media = new CF4MMedia();
                    continueParsing &= (media != NULL);

                    if (continueParsing)
                    {
                      continueParsing &= media->SetBitrate(bitrateValue);
                      continueParsing &= media->SetWidth(widthValue);
                      continueParsing &= media->SetHeight(heightValue);

                      continueParsing &= media->SetUrl(url);
                      continueParsing &= media->SetDrmAdditionalHeaderId(drmAdditionalHeaderId);
                      continueParsing &= media->SetBootstrapInfoId(bootstrapInfoId);
                      continueParsing &= media->SetDvrInfoId(dvrInfoId);
                      continueParsing &= media->SetGroupSpecifier(groupspec);
                      continueParsing &= media->SetMulticastStreamName(multicastStreamName);
                      continueParsing &= media->SetMetadata(metadataValue);

                      continueParsing &= this->mediaCollection->Add(media);
                    }

                    if (!continueParsing)
                    {
                      FREE_MEM_CLASS(media);
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
                    wchar_t *deliveryType = ConvertUtf8ToUnicode(child->GetText());
                    this->deliveryType->SetDeliveryType(deliveryType);
                    FREE_MEM(deliveryType);
                  }

                  // base URL - it's replacing manifest URL
                  if (strcmp(child->Name(), F4M_ELEMENT_BASEURL) == 0)
                  {
                    wchar_t *baseUrl = ConvertUtf8ToUnicode(child->GetText());
                    continueParsing &= this->baseUrl->SetBaseUrl(baseUrl);
                    FREE_MEM(baseUrl);
                  }
                }
                while (continueParsing && ((child = child->NextSiblingElement()) != NULL));

                result = continueParsing;
              }
            }
          }
        }
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