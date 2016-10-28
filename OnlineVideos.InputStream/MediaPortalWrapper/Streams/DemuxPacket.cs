using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Streams
{
  // From https://github.com/xbmc/xbmc/blob/master/xbmc/cores/VideoPlayer/DVDDemuxers/DVDDemuxPacket.h
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct DemuxPacket
  {
    public IntPtr Data;   // data
    public int Size;     // data size
    public int StreamId; // integer representing the stream index
    public long DemuxerId; // id of the demuxer that created the packet
    public int GroupId;  // the group this data belongs to, used to group data from different streams together

    public double Pts; // pts in DVD_TIME_BASE
    public double Dts; // dts in DVD_TIME_BASE
    public double Duration; // duration in DVD_TIME_BASE if available

    public int DispTime;
  }

  public class DemuxPacketHelper
  {
    static readonly object _syncObj = new object();
    static readonly Dictionary<IntPtr, GCHandle> PacketHandles = new Dictionary<IntPtr, GCHandle>();

    public static IntPtr AllocateDemuxPacket(int dataSize, bool noPadding = false)
    {
      var packet = CreateDemuxPacket(dataSize, noPadding);
      var gch = GCHandle.Alloc(packet, GCHandleType.Pinned);
      var ptr = gch.AddrOfPinnedObject();
      lock (_syncObj)
        PacketHandles[ptr] = gch;
      return ptr;
    }

    public static DemuxPacket CreateDemuxPacket(int dataSize, bool noPadding = false)
    {
      DemuxPacket packet = new DemuxPacket();
      /**
        * Required number of additionally allocated bytes at the end of the input bitstream for decoding.
        * this is mainly needed because some optimized bitstream readers read
        * 32 or 64 bit at once and could read over the end<br>
        * Note, if the first 23 bits of the additional bytes are not 0 then damaged
        * MPEG bitstreams could cause overread and segfault
        */
      if (dataSize > 0)
      {
        int adjustedSize = dataSize;
        if (!noPadding)
        {
          adjustedSize = dataSize + 32 /*FF_INPUT_BUFFER_PADDING_SIZE*/;
          var padding = adjustedSize%16;
          if (padding != 0)
            adjustedSize += 16 - padding; /* Padding to mod 16 */
        }
        packet.Data = Marshal.AllocCoTaskMem(adjustedSize);
        packet.Size = adjustedSize;
      }
      return packet;
    }

    public static void FreeDemuxPacket(IntPtr packet)
    {
      lock (_syncObj)
      {
        GCHandle gch;
        if (PacketHandles.TryGetValue(packet, out gch))
        {
          var p = (DemuxPacket) gch.Target;
          PtrExtension.FreeCO(ref p.Data);
          gch.Free();
          PacketHandles.Remove(packet);
        }
      }
    }
  }
}
