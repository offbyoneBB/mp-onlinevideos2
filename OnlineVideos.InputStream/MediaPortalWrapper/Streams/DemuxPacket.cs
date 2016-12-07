using System;
using System.Runtime.InteropServices;

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

  public class DemuxPacketWrapper : IDisposable
  {
    public DemuxPacketWrapper()
    {
      NativePtr = IntPtr.Zero;
      IsEOS = true;
    }
    public DemuxPacketWrapper(DemuxPacket packet, IntPtr ptr)
    {
      DemuxPacket = packet;
      NativePtr = ptr;
      IsEOS = packet.StreamId == 0;
    }
    public IntPtr NativePtr;
    public DemuxPacket DemuxPacket;
    public bool IsEOS;

    public void Dispose()
    {
      unsafe
      {
        if (NativePtr != IntPtr.Zero)
          CManagedDemuxPacketHelper.FreeDemuxPacket2((void*)NativePtr);
      }
    }
  }
}
