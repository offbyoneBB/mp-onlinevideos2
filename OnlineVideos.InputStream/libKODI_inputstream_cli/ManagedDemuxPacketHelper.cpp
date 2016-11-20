#include "stdafx.h"
#include "ManagedDemuxPacketHelper.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CManagedDemuxPacketHelper::CManagedDemuxPacketHelper()
{
}

void CManagedDemuxPacketHelper::FreeDemuxPacket2(void* pPacket)
{
  FreeDemuxPacket((DemuxPacket*)pPacket);
}
void CManagedDemuxPacketHelper::FreeDemuxPacket(DemuxPacket* pPacket)
{
  if (pPacket)
  {
    try {
      if (pPacket->pData) _aligned_free(pPacket->pData);
      delete pPacket;
    }
    catch (...) {
      //Utils::Logger::Log("{0} - Exception thrown while freeing packet", __FUNCTION__);
    }
  }
}

DemuxPacket* CManagedDemuxPacketHelper::AllocateDemuxPacket(int iDataSize)
{
  DemuxPacket* pPacket = new DemuxPacket;
  if (!pPacket) return NULL;

  try
  {
    memset(pPacket, 0, sizeof(DemuxPacket));

    if (iDataSize > 0)
    {
      // need to allocate a few bytes more.
      // From avcodec.h (ffmpeg)
      /**
      * Required number of additionally allocated bytes at the end of the input bitstream for decoding.
      * this is mainly needed because some optimized bitstream readers read
      * 32 or 64 bit at once and could read over the end<br>
      * Note, if the first 23 bits of the additional bytes are not 0 then damaged
      * MPEG bitstreams could cause overread and segfault
      */
      pPacket->pData = (uint8_t*)_aligned_malloc(iDataSize + FF_INPUT_BUFFER_PADDING_SIZE, 16);
      if (!pPacket->pData)
      {
        FreeDemuxPacket(pPacket);
        return NULL;
      }

      // reset the last 8 bytes to 0;
      memset(pPacket->pData + iDataSize, 0, FF_INPUT_BUFFER_PADDING_SIZE);
    }

    // setup defaults
    pPacket->dts = DVD_NOPTS_VALUE;
    pPacket->pts = DVD_NOPTS_VALUE;
    pPacket->iStreamId = -1;
    pPacket->dispTime = 0;
  }
  catch (...)
  {
    //Utils::Logger::Log("{0} - Exception thrown", __FUNCTION__);
    FreeDemuxPacket(pPacket);
    pPacket = NULL;
  }
  return pPacket;
}
