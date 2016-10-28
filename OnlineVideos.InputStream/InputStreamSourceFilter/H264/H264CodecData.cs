using System;

namespace InputStreamSourceFilter.H264
{
  public class H264CodecData
  {
    public H264CodecData(byte[] data)
    {
      int offset = 0;
      Version = data[offset++];
      Profile = data[offset++];
      Compatability = data[offset++];
      Level = data[offset++];
      NALSizeMinusOne = data[offset++] & 0x3;
      SPSCount = data[offset++] & 0x1F;
      SPSLength = IntFromBigEndian(data, offset, 2);
      offset += 2;
      SPS = CopyBytes(data, offset, SPSLength);
      offset += SPSLength;
      PPSCount = data[offset++];
      PPSLength = IntFromBigEndian(data, offset, 2);
      offset += 2;
      PPS = CopyBytes(data, offset, PPSLength);
    }

    public int Version { get; set; }
    public int Profile { get; set; }
    public int Compatability { get; set; }
    public int Level { get; set; }
    public int NALSizeMinusOne { get; set; }
    public int SPSCount { get; set; }
    public int SPSLength { get; set; }
    public byte[] SPS { get; set; }
    public int PPSCount { get; set; }
    public int PPSLength { get; set; }
    public byte[] PPS { get; set; }

    static byte[] CopyBytes(byte[] src, int offset, int count)
    {
      byte[] dst = new byte[count];
      Buffer.BlockCopy(src, offset, dst, 0, count);
      return dst;
    }

    static int IntFromBigEndian(byte[] buffer, int offset, int count)
    {
      if (count > 4)
        count = 4;
      int result = 0;
      for (int x = 0; x < count; x++)
      {
        int shift = 8 * (count - 1 - x);
        result = result | buffer[x + offset] << shift;
      }
      return result;
    }
  }
}
