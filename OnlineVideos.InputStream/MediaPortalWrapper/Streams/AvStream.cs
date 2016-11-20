using System;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortalWrapper.NativeWrappers;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Streams
{

  public enum ManagedAVCodecID
  {
    H264 = 28,
    HEVC = 175,
    AAC = 86018
  }

  public abstract class AbstractStream : IDisposable
  {
    protected readonly object _syncObj = new object();
    public abstract void Dispose();
    public abstract void Write(DemuxPacket packet);
    public abstract DemuxPacketWrapper Read();

    public virtual void Free(DemuxPacketWrapper packet)
    {
    
    }
  }

  //public class AvStream : AbstractStream
  //{
  //  private readonly InputstreamInfo _info;
  //  private readonly FileStream _fileStream;
  //  private readonly CManagedBitstreamConverter _conv;

  //  /// <summary>
  //  /// Constructs a new decoder/writer stream which writes the packets into a single elementary stream.
  //  /// </summary>
  //  /// <param name="info">Info</param>
  //  /// <param name="enableConversion"><c>>true</c> to use BitstreamConverter to convert h264/hevc to AnnexB</param>
  //  public AvStream(InputstreamInfo info, bool enableConversion = true)
  //  {
  //    _info = info;
  //    var streamName = string.Format("stream-{0}-{1}.{2}", info.StreamId, info.Language, info.CodecName);
  //    _fileStream = new FileStream(streamName, FileMode.Create);
  //    // Check if there is video specific decoder information
  //    if (_info.ExtraSize == 0 || !enableConversion)
  //      return;

  //    ManagedAVCodecID avcodec;
  //    if (Enum.TryParse(_info.CodecName, true, out avcodec))
  //    {
  //      var conv = new CManagedBitstreamConverter();
  //      if (!conv.Open((int)avcodec, _info.m_ExtraData, (int)_info.ExtraSize, true))
  //      {
  //        Logger.Log("Could not initialize converter for stream {0}", _info.StreamId);
  //        return;
  //      }
  //      _conv = conv;
  //    }
  //  }

  //  public override void Write(DemuxPacket packet)
  //  {
  //    if (_conv == null)
  //    {
  //      byte[] data = new byte[packet.Size];
  //      Marshal.Copy(packet.Data, data, 0, data.Length);
  //      _fileStream.Write(data, 0, data.Length);
  //    }
  //    else
  //    {
  //      if (_conv.Convert(packet.Data, packet.Size))
  //      {
  //        IntPtr convertedPtr;
  //        int convertedSize;
  //        if (_conv.GetData(out convertedPtr, out convertedSize))
  //        {
  //          byte[] converted = new byte[convertedSize];
  //          Marshal.Copy(convertedPtr, converted, 0, convertedSize);
  //          _fileStream.Write(converted, 0, converted.Length);
  //        }
  //      }
  //    }
  //  }
    //public override DemuxPacket Read()
    //{
    //  throw new NotSupportedException("No read implemented");
    //}

    //public override void Dispose()
    //{
    //  _fileStream.Dispose();
    //  if (_conv != null)
    //    _conv.Close();
    //}
  //}
}
