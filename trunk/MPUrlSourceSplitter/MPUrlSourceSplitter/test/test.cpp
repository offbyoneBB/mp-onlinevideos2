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

// test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <stdio.h>
#include "base64.h"
#include <stdint.h>
#include "compress_zlib.h"
#include "MSHSSmoothStreamingMedia.h"

int _tmain(int argc, _TCHAR* argv[])
{
  ALLOC_MEM_DEFINE_SET(base64EncodedManifest, char, 30441, 0);
  FILE *stream = fopen("D:\\input.dat", "r");
  fread(base64EncodedManifest, 1, 30440, stream);
  fclose(stream);
  uint8_t *output = NULL;
  uint32_t length = 0;

  if (SUCCEEDED(base64_decode(base64EncodedManifest, &output, &length)))
  {
    uint8_t *output2 = NULL;
    uint32_t length2 = 0;

    if (SUCCEEDED(decompress_zlib(output, length, &output2, &length2)))
    {
      CMSHSSmoothStreamingMedia *media = new CMSHSSmoothStreamingMedia();
      if (media->Deserialize(output2))
      {
        for(int i=0; i < media->GetStreams()->Count();i++)
        {
          CMSHSStream *stream = media->GetStreams()->GetItem(i);

          for(int j = 0; j < stream->GetTracks()->Count(); j++)
          {
            CMSHSTrack *track = stream->GetTracks()->GetItem(j);

            printf("A");
          }
        }
        printf("des");
      }
    }
  }

	return 0;
}

