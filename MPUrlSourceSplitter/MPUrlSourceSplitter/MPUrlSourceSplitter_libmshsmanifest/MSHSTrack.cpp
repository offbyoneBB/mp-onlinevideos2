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

#include "MSHSTrack.h"

CMSHSTrack::CMSHSTrack(void)
{
  this->index = 0;
  this->bitrate = 0;
  this->maxWidth = 0;
  this->maxHeight = 0;
  this->codecPrivateData = NULL;
  this->samplingRate = 0;
  this->channels = 0;
  this->bitsPerSample = 0;
  this->packetSize = 0;
  this->audioTag = 0;
  this->fourCC = NULL;
  this->nalUnitLengthField = MSHS_NAL_UNIT_LENGTH_DEFAULT;
  this->customAttributes = new CMSHSCustomAttributeCollection();
}

CMSHSTrack::~CMSHSTrack(void)
{
  FREE_MEM(this->codecPrivateData);
  FREE_MEM_CLASS(this->customAttributes);
}

/* get methods */

uint32_t CMSHSTrack::GetIndex(void)
{
  return this->index;
}

uint32_t CMSHSTrack::GetBitrate(void)
{
  return this->bitrate;
}

uint32_t CMSHSTrack::GetMaxWidth(void)
{
  return this->maxWidth;
}

uint32_t CMSHSTrack::GetMaxHeight(void)
{
  return this->maxHeight;
}

const wchar_t *CMSHSTrack::GetCodecPrivateData(void)
{
  return this->codecPrivateData;
}

uint32_t CMSHSTrack::GetSamplingRate(void)
{
  return this->samplingRate;
}

uint16_t CMSHSTrack::GetChannels(void)
{
  return this->channels;
}

uint16_t CMSHSTrack::GetBitsPerSample(void)
{
  return this->bitsPerSample;
}

uint32_t CMSHSTrack::GetPacketSize(void)
{
  return this->packetSize;
}

uint32_t CMSHSTrack::GetAudioTag(void)
{
  return this->audioTag;
}

const wchar_t *CMSHSTrack::GetFourCC(void)
{
  return this->fourCC;
}

uint16_t CMSHSTrack::GetNalUnitLengthField(void)
{
  return this->nalUnitLengthField;
}

CMSHSCustomAttributeCollection *CMSHSTrack::GetCustomAttributes(void)
{
  return this->customAttributes;
}

/* set methods */

void CMSHSTrack::SetIndex(uint32_t index)
{
  this->index = index;
}

void CMSHSTrack::SetBitrate(uint32_t bitrate)
{
  this->bitrate = bitrate;
}

void CMSHSTrack::SetMaxWidth(uint32_t maxWidth)
{
  this->maxWidth = maxWidth;
}

void CMSHSTrack::SetMaxHeight(uint32_t maxHeight)
{
  this->maxHeight = maxHeight;
}

bool CMSHSTrack::SetCodecPrivateData(const wchar_t *codecPrivateData)
{
  SET_STRING_RETURN_WITH_NULL(this->codecPrivateData, codecPrivateData);
}

void CMSHSTrack::SetSamplingRate(uint32_t samplingRate)
{
  this->samplingRate = samplingRate;
}

void CMSHSTrack::SetChannels(uint16_t channels)
{
  this->channels = channels;
}

void CMSHSTrack::SetBitsPerSample(uint16_t bitsPerSample)
{
  this->bitsPerSample = bitsPerSample;
}

void CMSHSTrack::SetPacketSize(uint32_t packetSize)
{
  this->packetSize = packetSize;
}

void CMSHSTrack::SetAudioTag(uint32_t audioTag)
{
  this->audioTag = audioTag;
}

bool CMSHSTrack::SetFourCC(const wchar_t *fourCC)
{
  SET_STRING_RETURN_WITH_NULL(this->fourCC, fourCC);
}

void CMSHSTrack::SetNalUnitLengthField(uint16_t nalUnitLengthField)
{
  this->nalUnitLengthField = nalUnitLengthField;
}

/* other methods */