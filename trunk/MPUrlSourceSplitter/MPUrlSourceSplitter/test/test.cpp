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
#include <stdint.h>

#include "BoxFactory.h"

#include "FileTypeBox.h"
#include "BoxCollection.h"
#include "AVCConfigurationBox.h"

#include "conversions.h"


int _tmain(int argc, _TCHAR* argv[])
{
  uint32_t length = 10 * 1024 * 1024;

  ALLOC_MEM_DEFINE_SET(buffer, uint8_t, length, 0);
  //FILE *stream = fopen("D:\\svnroot\\HttpStreaming\\lmfao.ismv", "rb");
  //FILE *stream = fopen("D:\\outout_dat.mp4", "rb");
  FILE *stream = fopen("D:\\test.dat", "rb");
  uint32_t read = fread(buffer, 1, length, stream);
  fclose(stream);

  uint32_t processed = 0;
  CBoxFactory *factory = new CBoxFactory();

  while (processed < length)
  {
    CBox *box = factory->CreateBox(buffer + processed, read - processed);

    if (box != NULL)
    {
      wprintf(L"%s\n", box->GetParsedHumanReadable(L""));
      processed += (uint32_t)box->GetSize();
    }
    else
    {
      break;
    }
  }

  FREE_MEM_CLASS(factory);

  /*CBox *box = GetFileTypeBox();
  uint64_t length = box->GetBoxSize();
  ALLOC_MEM_DEFINE_SET(buffer, uint8_t, length, 0);
  if (box->GetBox(buffer, length))
  {
    FILE *stream = fopen("D:\\outout_dat.mp4", "wb");
    fwrite(buffer, 1, length, stream);
    fclose(stream);
  }

  FREE_MEM(buffer);
  FREE_MEM_CLASS(box);*/

  /*CAVCConfigurationBox *box = new CAVCConfigurationBox();

  if (box->Parse(buffer + 0x020B, length))
  {
    printf("parsed");
  }*/

	return 0;
}

