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

#include <stdio.h>

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

  FILE *stream = fopen("D:\\svnroot\\HttpStreaming\\autosalon_Manifest.xml", "r");
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
        printf("parsed");
      }
    }
    FREE_MEM(buffer);
    fclose(stream);
  }

	return 0;
}

