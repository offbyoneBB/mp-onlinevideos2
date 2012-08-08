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

#pragma once

#ifndef __MSHS_TRACK_DEFINED
#define __MSHS_TRACK_DEFINED

#include <stdint.h>

#define NAL_UNIT_LENGTH_DEFAULT                                               4

class CMSHSTrack
{
public:
  // creats new instance of CMSHSTrack class
  CMSHSTrack(void);

  // desctructor
  ~CMSHSTrack(void);

  /* get methods */

  /* set methods */

  /* other methods */

private:

  // ordinal that identifies the Track and MUST be unique for each Track in the Stream
  // the Index SHOULD start at 0 and increment by 1 for each subsequent Track in the Stream
  uint32_t index;

  // average bandwidth consumed by the track, in bits-per-second (bps)
  // the value 0 MAY be used for Tracks whose Bit Rate is negligible relative to other Tracks in the Presentation
  uint32_t bitrate;

  // maximum width of a video Sample, in pixels
  uint32_t maxWidth;

  // maximum height of a video Sample, in pixels
  uint32_t maxHeight;

  // data that specifies parameters specific to the Media Format and common to all Samples in the Track,
  // represented as a string of hex-coded bytes
  // the format and semantic meaning of byte sequence varies with the value of the FourCC field as follows:
  // FourCC field equals "H264": The CodecPrivateData field contains a hex-coded string representation of the following byte sequence:
  //  %x00 %x00 %x00 %x01 SPSField %x00 %x00 %x00 %x01 PPSField
  //  SPSField contains the Sequence Parameter Set (SPS)
  //  PPSField contains the Slice Parameter Set (PPS)
  // FourCC field equals "WVC1": The CodecPrivateData field contains a hex-coded string representation of the VIDEOINFOHEADER structure
  // FourCC field equals "AACL": The CodecPrivateData field SHOULD be empty
  // FourCC field equals "WMAP": The CodecPrivateData field contains the WAVEFORMATEX structure, if the AudioTag field equals "65534" equals, and SHOULD be empty otherwise
  // FourCC is a vendor extension value: The format of the CodecPrivateData field is also vendor-extensible
  //  registration of the FourCC field value with MPEG4-RA, to can be used to avoid collision between extensions
  wchar_t *codecPrivateData;

  // sampling Rate of an audio Track
  uint32_t samplingRate;

  // channel Count of an audio Track
  uint16_t channels;

  // sample Size of an audio Track
  uint16_t bitsPerSample;

  // size of each audio Packet, in bytes
  uint32_t packetSize;

  // numeric code that identifies which Media Format and variant of the Media Format is used for each Sample in an audio Track
  // the following range of values is reserved with the following semantic meanings:
  // "1": The sample Media Format is Linear 8 or 16 bit Pulse Code Modulation
  // "353": Microsoft Windows Media Audio v7, v8 and v9.x Standard (WMA Standard)
  // "353": Microsoft Windows Media Audio v9.x and v10 Professional (WMA Professional)
  // "85": ISO MPEG-1 Layer III (MP3)
  // "255": ISO Advanced Audio Coding (AAC)
  // "65534": Vendor-extensible format, if specified, the codec private data field SHOULD contain a hex-encoded version of
  //    the WAVE_FORMAT_EXTENSIBLE structure
  uint32_t audioTag;

  // four-character code that identifies which Media Format is used for each Sample
  // the following range of values is reserved with the following semantic meanings:
  // "H264": Video Samples for this Track use Advanced Video Coding, as specified in [AVCFF]
  // "WVC1": Video Samples for this Track use VC-1, as specified in [VC-1]
  // "AACL": Audio Samples for this Track use AAC (Low Complexity), as specified in [AAC]
  // "WMAP": Audio Samples for this Track use WMA Professional
  // other : a vendor extension value containing a registered with MPEG4-RA
  wchar_t *fourCC;

  // number of bytes that specify the length of each Network Abstraction Layer (NAL) unit
  // this field SHOULD be omitted unless the value of the FourCC field is "H264"
  // the default value is NAL_UNIT_LENGTH_DEFAULT
  uint16_t nalUnitLegthField;
};

#endif