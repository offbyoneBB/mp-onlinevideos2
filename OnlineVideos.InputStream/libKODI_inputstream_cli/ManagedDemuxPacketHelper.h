#pragma once
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string>
#include "Stdafx.h"

#define DVD_NOPTS_VALUE 0xFFF0000000000000
#define FF_INPUT_BUFFER_PADDING_SIZE 32

typedef struct DemuxPacket
{
  unsigned char* pData;   // data
  int iSize;     // data size
  int iStreamId; // integer representing the stream index
  int64_t demuxerId; // id of the demuxer that created the packet
  int iGroupId;  // the group this data belongs to, used to group data from different streams together

  double pts; // pts in DVD_TIME_BASE
  double dts; // dts in DVD_TIME_BASE
  double duration; // duration in DVD_TIME_BASE if available

  int dispTime;
} DemuxPacket;

public ref class CManagedDemuxPacketHelper
{
public:
  CManagedDemuxPacketHelper();
  static void FreeDemuxPacket(DemuxPacket* packet);
  static void FreeDemuxPacket2(void* packet);
  static DemuxPacket* AllocateDemuxPacket(int iDataSize);
};

