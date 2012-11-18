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

#include <curl/curl.h>

#include "BufferHelper.h"
#include "SegmentFragmentCollection.h"
#include "ParameterCollection.h"
#include "BootstrapInfoBox.h"
#include "MPUrlSourceSplitter_Protocol_Afhs_Parameters.h"
#include "formatUrl.h"

CSegmentFragmentCollection *GetSegmentsFragmentsFromBootstrapInfoBox(CParameterCollection *configurationParameters, CBootstrapInfoBox *bootstrapInfoBox)
{
  HRESULT result = S_OK;
  CSegmentFragmentCollection *segmentsFragments = NULL;

  if (SUCCEEDED(result))
  {
    // now choose from bootstrap info -> QualityEntryTable highest quality (if exists) with segment run
    wchar_t *quality = NULL;
    CSegmentRunEntryCollection *segmentRunEntryTable = NULL;

    for (unsigned int i = 0; ((i <= bootstrapInfoBox->GetQualityEntryTable()->Count()) && (segmentRunEntryTable == NULL)); i++)
    {
      FREE_MEM(quality);

      // choose quality only for valid indexes, in another case is quality NULL
      if (i != bootstrapInfoBox->GetQualityEntryTable()->Count())
      {
        CBootstrapInfoQualityEntry *bootstrapInfoQualityEntry = bootstrapInfoBox->GetQualityEntryTable()->GetItem(0);
        quality = Duplicate(bootstrapInfoQualityEntry->GetQualityEntry());
      }

      // from segment run table choose segment with specifed quality (if exists) or segment with QualityEntryCount equal to zero
      for (unsigned int i = 0; i < bootstrapInfoBox->GetSegmentRunTable()->Count(); i++)
      {
        CSegmentRunTableBox *segmentRunTableBox = bootstrapInfoBox->GetSegmentRunTable()->GetItem(i);

        if (quality != NULL)
        {
          if (segmentRunTableBox->GetQualitySegmentUrlModifiers()->Contains(quality, false))
          {
            segmentRunEntryTable = segmentRunTableBox->GetSegmentRunEntryTable();
          }
        }
        else
        {
          if ((segmentRunTableBox->GetQualitySegmentUrlModifiers()->Count() == 0) || (segmentRunTableBox->GetQualitySegmentUrlModifiers()->Contains(L"", false)))
          {
            segmentRunEntryTable = segmentRunTableBox->GetSegmentRunEntryTable();
          }
        }
      }
    }

    if (segmentRunEntryTable == NULL)
    {
      result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    if (SUCCEEDED(result))
    {
      if (segmentRunEntryTable->Count() == 0)
      {
        result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
      }
    }

    // from fragment run table choose fragment with specifed quality (if exists) or fragment with QualityEntryCount equal to zero
    CFragmentRunEntryCollection *fragmentRunEntryTableTemp = NULL;
    unsigned int timeScale = 0;
    for (unsigned int i = 0; i < bootstrapInfoBox->GetFragmentRunTable()->Count(); i++)
    {
      CFragmentRunTableBox *fragmentRunTableBox = bootstrapInfoBox->GetFragmentRunTable()->GetItem(i);

      if (quality != NULL)
      {
        if (fragmentRunTableBox->GetQualitySegmentUrlModifiers()->Contains(quality, false))
        {
          fragmentRunEntryTableTemp = fragmentRunTableBox->GetFragmentRunEntryTable();
          timeScale = fragmentRunTableBox->GetTimeScale();
        }
      }
      else
      {
        if ((fragmentRunTableBox->GetQualitySegmentUrlModifiers()->Count() == 0) || (fragmentRunTableBox->GetQualitySegmentUrlModifiers()->Contains(L"", false)))
        {
          fragmentRunEntryTableTemp = fragmentRunTableBox->GetFragmentRunEntryTable();
          timeScale = fragmentRunTableBox->GetTimeScale();
        }
      }
    }

    if (fragmentRunEntryTableTemp == NULL)
    {
      result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    CFragmentRunEntryCollection *fragmentRunEntryTable = new CFragmentRunEntryCollection();
    CHECK_POINTER_HRESULT(result, fragmentRunEntryTable, result, E_OUTOFMEMORY);

    if (SUCCEEDED(result))
    {
      // convert temporary fragment run table to simplier collection
      for (unsigned int i = 0; i < fragmentRunEntryTableTemp->Count(); i++)
      {
        CFragmentRunEntry *fragmentRunEntryTemp = fragmentRunEntryTableTemp->GetItem(i);
        unsigned int nextItemIndex = i + 1;
        CFragmentRunEntry *fragmentRunEntryTempNext = NULL;

        for (unsigned int j = nextItemIndex; j < fragmentRunEntryTableTemp->Count(); j++)
        {
          CFragmentRunEntry *temp = fragmentRunEntryTableTemp->GetItem(nextItemIndex);
          if (temp->GetFirstFragment() != 0)
          {
            fragmentRunEntryTempNext = temp;
            break;
          }
          else
          {
            nextItemIndex++;
          }
        }

        if (((fragmentRunEntryTemp->GetFirstFragmentTimestamp() == 0) && (i == 0)) ||
          (fragmentRunEntryTemp->GetFirstFragmentTimestamp() != 0))
        {
          uint64_t fragmentTimestamp = fragmentRunEntryTemp->GetFirstFragmentTimestamp();
          unsigned int lastFragment = (fragmentRunEntryTempNext == NULL) ? (fragmentRunEntryTemp->GetFirstFragment() + 1) : fragmentRunEntryTempNext->GetFirstFragment();

          for (unsigned int j = fragmentRunEntryTemp->GetFirstFragment(); j < lastFragment; j++)
          {
            unsigned int diff = j - fragmentRunEntryTemp->GetFirstFragment();
            CFragmentRunEntry *fragmentRunEntry = new CFragmentRunEntry(
              fragmentRunEntryTemp->GetFirstFragment() + diff,
              fragmentTimestamp,
              fragmentRunEntryTemp->GetFragmentDuration(),
              fragmentRunEntryTemp->GetDiscontinuityIndicator());
            fragmentTimestamp += fragmentRunEntryTemp->GetFragmentDuration();

            CHECK_POINTER_HRESULT(result, fragmentRunEntry, result, E_OUTOFMEMORY);

            if (SUCCEEDED(result))
            {
              result = (fragmentRunEntryTable->Add(fragmentRunEntry)) ? result : E_FAIL;
            }

            if (FAILED(result))
            {
              FREE_MEM_CLASS(fragmentRunEntry);
            }
          }
        }
      }
    }

    if (SUCCEEDED(result))
    {
      if (fragmentRunEntryTable->Count() == 0)
      {
        result = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
      }
    }

    if (SUCCEEDED(result))
    {
      wchar_t *serverBaseUrl = Duplicate(configurationParameters->GetValue(PARAMETER_NAME_AFHS_BASE_URL, true, L""));
      for (unsigned int i = 0; i < bootstrapInfoBox->GetServerEntryTable()->Count(); i++)
      {
        CBootstrapInfoServerEntry *serverEntry = bootstrapInfoBox->GetServerEntryTable()->GetItem(i);
        if (!IsNullOrEmptyOrWhitespace(serverEntry->GetServerEntry()))
        {
          FREE_MEM(serverBaseUrl);
          serverBaseUrl = Duplicate(serverEntry->GetServerEntry());
        }
      }

      CHECK_POINTER_HRESULT(result, serverBaseUrl, result, E_OUTOFMEMORY);

      if (SUCCEEDED(result))
      {
        wchar_t *mediaPartUrl = Duplicate(configurationParameters->GetValue(PARAMETER_NAME_AFHS_MEDIA_PART_URL, true, L""));
        CHECK_POINTER_HRESULT(result, mediaPartUrl, result, E_OUTOFMEMORY);

        if (SUCCEEDED(result))
        {
          wchar_t *baseUrl = FormatAbsoluteUrl(serverBaseUrl, mediaPartUrl);
          CHECK_POINTER_HRESULT(result, baseUrl, result, E_OUTOFMEMORY);

          if (SUCCEEDED(result))
          {
            //wchar_t *movieIdentifierUrl = FormatAbsoluteUrl(baseUrl, this->bootstrapInfoBox->GetMovieIdentifier());
            //CHECK_POINTER_HRESULT(result, movieIdentifierUrl, result, E_OUTOFMEMORY);

            if (SUCCEEDED(result))
            {
              //wchar_t *qualityUrl = FormatString(L"%s%s", movieIdentifierUrl, (quality == NULL) ? L"" : quality);
              wchar_t *qualityUrl = FormatAbsoluteUrl(baseUrl, (quality == NULL) ? L"" : quality);
              CHECK_POINTER_HRESULT(result, qualityUrl, result, E_OUTOFMEMORY);

              if (SUCCEEDED(result))
              {
                // convert segment run entry table to simplier collection
                segmentsFragments = new CSegmentFragmentCollection();
                CHECK_POINTER_HRESULT(result, segmentsFragments, result, E_OUTOFMEMORY);

                if (SUCCEEDED(result))
                {
                  result = (segmentsFragments->SetBaseUrl(qualityUrl)) ? result : E_OUTOFMEMORY;
                }

                if (SUCCEEDED(result))
                {
                  unsigned int fragmentRunEntryTableIndex  = 0;

                  for (unsigned int i = 0; (SUCCEEDED(result) && (i < segmentRunEntryTable->Count())); i++)
                  {
                    CSegmentRunEntry *segmentRunEntry = segmentRunEntryTable->GetItem(i);
                    unsigned int lastSegment = (i == (segmentRunEntryTable->Count() - 1)) ? (segmentRunEntry->GetFirstSegment() + 1) : segmentRunEntryTable->GetItem(i + 1)->GetFirstSegment();

                    for (unsigned int j = segmentRunEntry->GetFirstSegment(); (SUCCEEDED(result) && (j < lastSegment)); j++)
                    {
                      unsigned int totalFragmentsPerSegment = ((segmentRunEntry->GetFragmentsPerSegment() == UINT_MAX) ? fragmentRunEntryTable->Count() : segmentRunEntry->GetFragmentsPerSegment());

                      result = (segmentsFragments->EnsureEnoughSpace(segmentsFragments->Count() + totalFragmentsPerSegment)) ? result : E_OUTOFMEMORY;

                      for (unsigned int k = 0; (SUCCEEDED(result) && (k < totalFragmentsPerSegment)); k++)
                      {
                        // choose fragment and get its timestamp
                        uint64_t timestamp = fragmentRunEntryTable->GetItem(min(fragmentRunEntryTableIndex, fragmentRunEntryTable->Count() - 1))->GetFirstFragmentTimestamp();
                        unsigned int firstFragment = fragmentRunEntryTable->GetItem(min(fragmentRunEntryTableIndex, fragmentRunEntryTable->Count() - 1))->GetFirstFragment();

                        if (fragmentRunEntryTableIndex >= fragmentRunEntryTable->Count())
                        {
                          // adjust fragment timestamp
                          timestamp += (fragmentRunEntryTableIndex - fragmentRunEntryTable->Count() + 1) * fragmentRunEntryTable->GetItem(fragmentRunEntryTable->Count() - 1)->GetFragmentDuration();
                          firstFragment += (fragmentRunEntryTableIndex - fragmentRunEntryTable->Count() + 1);
                        }
                        fragmentRunEntryTableIndex++;

                        CSegmentFragment *segmentFragment = new CSegmentFragment(j, firstFragment, timestamp * 1000 / timeScale);
                        CHECK_POINTER_HRESULT(result, segmentFragment, result, E_OUTOFMEMORY);

                        if (SUCCEEDED(result))
                        {
                          result = (segmentsFragments->Add(segmentFragment)) ? result : E_FAIL;
                        }

                        if (FAILED(result))
                        {
                          FREE_MEM_CLASS(segmentFragment);
                        }
                      }
                    }
                  }
                }
              }
              FREE_MEM(qualityUrl);
            }
            //FREE_MEM(movieIdentifierUrl);
          }
          FREE_MEM(baseUrl);
        }
        FREE_MEM(mediaPartUrl);
      }
      FREE_MEM(serverBaseUrl);
    }

    FREE_MEM(quality);
    FREE_MEM_CLASS(fragmentRunEntryTable);

    if (SUCCEEDED(result))
    {
      result = (segmentsFragments->Count() > 0) ? result : E_FAIL;
    }
  }

  if (FAILED(result))
  {
    FREE_MEM_CLASS(segmentsFragments);
  }

  return segmentsFragments;
}

int _tmain(int argc, _TCHAR* argv[])
{
  unsigned int size = 8624;
  ALLOC_MEM_DEFINE_SET(buffer, uint8_t, size, 0);
  FILE *stream = fopen("D:\\bootstrap.dat", "rb");

  fread(buffer, sizeof(uint8_t), size, stream);

  CParameterCollection *params = new CParameterCollection();

  params->Add(new CParameter(PARAMETER_NAME_AFHS_BASE_URL, L""));
  params->Add(new CParameter(PARAMETER_NAME_AFHS_MEDIA_PART_URL, L""));

  CBootstrapInfoBox *box = new CBootstrapInfoBox();
  if (box->Parse(buffer, size))
  {
    CSegmentFragmentCollection *sf = GetSegmentsFragmentsFromBootstrapInfoBox(params, box);

    for (unsigned int i = 0; i < sf->Count(); i++)
    {
      CSegmentFragment *sfi = sf->GetItem(i);

      if (sfi->GetFragmentTimestamp() >= box->GetCurrentMediaTime())
      {
        printf("found\n");
      }
    }

    FREE_MEM_CLASS(sf);
  }

  FREE_MEM_CLASS(box);
  FREE_MEM_CLASS(params);

  fclose(stream);
  FREE_MEM(buffer);
	return 0;
}
