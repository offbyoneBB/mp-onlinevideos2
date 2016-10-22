using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaPortalWrapper.Streams
{
  public class RawDumpStream : AbstractStream
  {
    private readonly FileStream _fileStream;

    public override void Dispose()
    {
      if (_fileStream != null)
        _fileStream.Dispose();
    }

    public RawDumpStream(string filename, bool readOnly = false)
    {
      _fileStream = new FileStream(filename, readOnly ? FileMode.Open : FileMode.Create);
    }

    public override void Write(DemuxPacket packet)
    {
      // Dummy bytes: AF FE ;-)
      _fileStream.Write(new byte[] { 0xAF, 0xFE }, 0, 2);

      // Packet size
      var bytes = BitConverter.GetBytes(packet.Size);
      _fileStream.Write(bytes, 0, bytes.Length);

      // Packet data
      bytes = new byte[packet.Size];
      Marshal.Copy(packet.Data, bytes, 0, bytes.Length);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.StreamId);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.DemuxerId);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.DispTime);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.Dts);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.Pts);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.Duration);
      _fileStream.Write(bytes, 0, bytes.Length);

      bytes = BitConverter.GetBytes(packet.GroupId);
      _fileStream.Write(bytes, 0, bytes.Length);
    }

    public override DemuxPacket Read()
    {
      DemuxPacket EOS = new DemuxPacket();

      byte[] bytes = new byte[1000000];
      if (_fileStream.Read(bytes, 0, 2) != 2) // 0xAF, 0xFE
        return EOS;

      if (bytes[0] != 0xAF || bytes[1] != 0xFE)
        throw new InvalidDataException("No valid header bytes found!");

      // Packet size
      if (_fileStream.Read(bytes, 0, sizeof(int)) != sizeof(int))
        return EOS; // int Size
      int size = BitConverter.ToInt32(bytes, 0);

      // noPadding = true, because padding was originally applied from source filter, avoid doing it again
      var packet = DemuxPacketHelper.CreateDemuxPacket(size, true);

      // Packet data
      if (_fileStream.Read(bytes, 0, size) != size)
        return EOS;
      Marshal.Copy(bytes, 0, packet.Data, size);

      if (_fileStream.Read(bytes, 0, sizeof(int)) != sizeof(int))
        return EOS;
      packet.StreamId = BitConverter.ToInt32(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(long)) != sizeof(long))
        return EOS;
      packet.DemuxerId = BitConverter.ToInt32(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(int)) != sizeof(int))
        return EOS;
      packet.DispTime = BitConverter.ToInt32(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(double)) != sizeof(double))
        return EOS;
      packet.Dts = BitConverter.ToDouble(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(double)) != sizeof(double))
        return EOS;
      packet.Pts = BitConverter.ToDouble(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(double)) != sizeof(double))
        return EOS;
      packet.Duration = BitConverter.ToDouble(bytes, 0);

      if (_fileStream.Read(bytes, 0, sizeof(int)) != sizeof(int))
        return EOS;
      packet.GroupId = BitConverter.ToInt32(bytes, 0);

      return packet;
    }
  }
}
