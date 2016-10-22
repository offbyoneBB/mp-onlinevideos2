using System;
using System.Runtime.InteropServices;
using MediaPortalWrapper.Streams;
using MediaPortalWrapper.Utils;
using RGiesecke.DllExport;

namespace libKODI_inputstream
{
  public class InputStreamWrapper
  {
    [DllExport("INPUTSTREAM_register_me", CallingConvention.Cdecl)]
    public static IntPtr INPUTSTREAM_register_me(IntPtr handle)
    {
      Logger.Log("INPUTSTREAM_register_me");
      return (IntPtr)1;
    }

    [DllExport("INPUTSTREAM_unregister_me", CallingConvention.Cdecl)]
    public static void INPUTSTREAM_unregister_me(IntPtr handle, IntPtr callback)
    {
      Logger.Log("INPUTSTREAM_unregister_me");
    }

    [DllExport("INPUTSTREAM_allocate_demux_packet", CallingConvention.Cdecl)]
    public static IntPtr INPUTSTREAM_allocate_demux_packet(IntPtr handle, IntPtr callback, int dataSize)
    {
      //Logger.Log("INPUTSTREAM_allocate_demux_packet");
      return DemuxPacketHelper.AllocateDemuxPacket(dataSize);
    }

    [DllExport("INPUTSTREAM_free_demux_packet", CallingConvention.Cdecl)]
    public static void INPUTSTREAM_free_demux_packet(IntPtr handle, IntPtr callback, IntPtr packet)
    {
      //Logger.Log("INPUTSTREAM_free_demux_packet");
      DemuxPacketHelper.FreeDemuxPacket(packet);
    }
  }
}
