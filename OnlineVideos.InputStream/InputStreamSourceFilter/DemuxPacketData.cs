using System.Runtime.InteropServices;
using DirectShow.BaseClasses;
using InputStreamSourceFilter.Extensions;
using MediaPortalWrapper.Streams;

namespace InputStreamSourceFilter
{
  public class DemuxPacketData : PacketData
  {
    public DemuxPacketData(DemuxPacket demuxPacket, bool isVideo)
    {
      byte[] buffer = new byte[demuxPacket.Size];
      Marshal.Copy(demuxPacket.Data, buffer, 0, buffer.Length);
      Buffer = buffer;
      Size = buffer.Length;
      if (isVideo)
      {
        // Set video timestamps
        Start = demuxPacket.Dts.ToDS();
        Stop = demuxPacket.Duration.ToDS();
      }
    }

    public override void Dispose()
    {
      base.Dispose();
    }
  }
}
