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

#include "FileTypeBox.h"
#include "MovieBox.h"

#include <stdio.h>

int _tmain(int argc, _TCHAR* argv[])
{
  FILE *stream = fopen("D:\\svnroot\\HttpStreaming\\test.mp4", "rb");
  if (stream != NULL)
  {
    unsigned int length = 256 * 1024;
    ALLOC_MEM_DEFINE_SET(buffer, unsigned char, length, 0);
    if (buffer != NULL)
    {
      fread(buffer, sizeof(unsigned char), length, stream);

      unsigned int position = 0;

      while (length > position)
      {
        {
          CFileTypeBox *box = new CFileTypeBox();
          if (box != NULL)
          {
            if (box->Parse(buffer + position, length - position))
            {
              wprintf(L"%s\n", box->GetParsedHumanReadable(L""));
              position += box->GetSize();
            }
          }
          FREE_MEM_CLASS(box);
        }

        {
          CMovieBox *box = new CMovieBox();
          if (box != NULL)
          {
            if (box->Parse(buffer + position, length - position))
            {
              wprintf(L"%s\n", box->GetParsedHumanReadable(L""));
              position += box->GetSize();
            }
          }
          FREE_MEM_CLASS(box);
        }

        {
          CBox *box = new CBox();
          if (box != NULL)
          {
            if (box->Parse(buffer + position, length - position))
            {
              wprintf(L"%s\n", box->GetParsedHumanReadable(L""));
              position += box->GetSize();
            }
          }
          FREE_MEM_CLASS(box);
        }
      }
    }
    FREE_MEM(buffer);
    fclose(stream);
  }

	return 0;
}

