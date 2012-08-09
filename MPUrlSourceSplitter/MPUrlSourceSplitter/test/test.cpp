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

#include "BoxFactory.h"
#include "MSHSManifest.h"
#include "base64.h"
#include "MSHSSmoothStreamingMedia.h"

#include <stdio.h>

#include <zlib.h>

#define CHUNK 16384

/* Compress from file source to file dest until EOF on source.
   def() returns Z_OK on success, Z_MEM_ERROR if memory could not be
   allocated for processing, Z_STREAM_ERROR if an invalid compression
   level is supplied, Z_VERSION_ERROR if the version of zlib.h and the
   version of the library linked do not match, or Z_ERRNO if there is
   an error reading or writing the files. */
int def(FILE *source, FILE *dest, int level)
{
    int ret, flush;
    unsigned have;
    z_stream strm;
    unsigned char in[CHUNK];
    unsigned char out[CHUNK];

    /* allocate deflate state */
    strm.zalloc = Z_NULL;
    strm.zfree = Z_NULL;
    strm.opaque = Z_NULL;
    ret = deflateInit(&strm, level);
    if (ret != Z_OK)
        return ret;

    /* compress until end of file */
    do {
        strm.avail_in = fread(in, 1, CHUNK, source);
        if (ferror(source)) {
            (void)deflateEnd(&strm);
            return Z_ERRNO;
        }
        flush = feof(source) ? Z_FINISH : Z_NO_FLUSH;
        strm.next_in = in;

        /* run deflate() on input until output buffer not full, finish
           compression if all of source has been read in */
        do {
            strm.avail_out = CHUNK;
            strm.next_out = out;

            ret = deflate(&strm, flush);    /* no bad return value */

            have = CHUNK - strm.avail_out;
            if (fwrite(out, 1, have, dest) != have || ferror(dest)) {
                (void)deflateEnd(&strm);
                return Z_ERRNO;
            }

        } while (strm.avail_out == 0);

        /* done when last data in file processed */
    } while (flush != Z_FINISH);

    /* clean up and return */
    (void)deflateEnd(&strm);
    return Z_OK;
}


int _tmain(int argc, _TCHAR* argv[])
{
  /*FILE *stream = fopen("D:\\svnroot\\HttpStreaming\\lmfao.ismv", "rb");
  if (stream != NULL)
  {
    unsigned int length = 256 * 1024;
    ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
    if (buffer != NULL)
    {
      fread(buffer, sizeof(unsigned char), length, stream);

      unsigned int position = 0;

      CBoxFactory *factory = new CBoxFactory();

      while ((length > position) && (factory != NULL))
      {
        CBox *box = factory->CreateBox(buffer + position, length - position);
        if (box != NULL)
        {
          wchar_t *parsed = box->GetParsedHumanReadable(L"");
          position += (unsigned int)box->GetSize();

          wprintf(L"%s\n", parsed);

          FREE_MEM(parsed);
        }
        else
        {
          break;
        }
        FREE_MEM_CLASS(box);
      }

      FREE_MEM_CLASS(factory);
    }
    FREE_MEM(buffer);
    fclose(stream);
  }*/

  FILE *stream = fopen("D:\\svnroot\\HttpStreaming\\test_manifest.xml", "r");
  if (stream != NULL)
  {
    unsigned int length = 256 * 1024;
    ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
    if (buffer != NULL)
    {
      unsigned int readBytes = fread(buffer, sizeof(unsigned char), length, stream);

      if (((buffer[0] == 0xFF) && (buffer[1] == 0xFE)) ||
        ((buffer[1] == 0xFF) && (buffer[0] == 0xFE)))
      {
        // input is probably in UTF-16 (Unicode)
        char *temp = ConvertUnicodeToUtf8((wchar_t *)(buffer + 2));
        FREE_MEM(buffer);
        buffer = (unsigned char *)temp;

        length = (buffer != NULL) ? strlen(temp) : 0;
      }

      CMSHSManifest *manifest = new CMSHSManifest();
      if (manifest->Parse((char *)buffer))
      {
        uint32_t length = manifest->GetSmoothStreamingMedia()->GetSerializeSize();
        ALLOC_MEM_DEFINE_SET(buffer, uint8_t, length, 0);
        

        if (manifest->GetSmoothStreamingMedia()->Serialize(buffer))
        {
          char *output = NULL;
          if (SUCCEEDED(base64_encode(buffer, length, &output)))
          {
            FILE *out = fopen("D:\\mshs_out_encoded.dat", "w");
            fwrite(output, sizeof(char), strlen(output), out);
            fclose(out);
          }

          FILE *out = fopen("D:\\mshs_out.dat", "wb");
          fwrite(buffer, sizeof(uint8_t), length, out);
          fclose(out);

          FILE *inp = fopen("D:\\mshs_out.dat", "rb");
          FILE *ou = fopen("D:\\mshs_out_zlib.dat", "wb");

          int err = def(inp, ou, -1);

          fclose(inp);
          fclose(ou);

          CMSHSSmoothStreamingMedia *media = new CMSHSSmoothStreamingMedia();

          if (media->Deserialize(buffer))
          {
            printf("deserialized");
          }

          FREE_MEM_CLASS(media);
        }

        FREE_MEM(buffer);
      }
    }
    FREE_MEM(buffer);
    fclose(stream);
  }

	return 0;
}

