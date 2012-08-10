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
#include "FileTypeBox.h"
#include "MovieBox.h"
#include "BoxCollection.h"

int _tmain(int argc, _TCHAR* argv[])
{
  CFileTypeBox *box = new CFileTypeBox();

  box->GetMajorBrand()->SetBrandString(L"isml");
  box->SetMinorVersion(0x00000200);
  CBrand *brand = new CBrand();
  brand->SetBrandString(L"piff");
  box->GetCompatibleBrands()->Add(brand);
  brand = new CBrand();
  brand->SetBrandString(L"iso2");
  box->GetCompatibleBrands()->Add(brand);

  uint8_t *buffer = NULL;
  uint32_t length = box->GetBoxSize(0);
  
  if (box->GetBox(&buffer, &length))
  {
    FILE *stream = fopen("D:\\test_dat.mp4", "wb");
    fwrite(buffer, 1, length, stream);
    fclose(stream);
  }

  /*CMovieBox *movieBox = new CMovieBox();

  movieBox->GetBoxes()->Add(box);

  buffer = NULL;
  length = 0;
  
  if (movieBox->GetBox(&buffer, &length))
  {
    FILE *stream = fopen("D:\\test_dat.mp4", "ab");
    fwrite(buffer, 1, length, stream);
    fclose(stream);
  }*/

  
	return 0;
}

