using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputStreamSourceFilter.H264
{
  class BitReader
  {
    byte[] bytes;
    int bitOffset;

    public BitReader(byte[] bytes, int bitOffset)
    {
      this.bytes = bytes;
      this.bitOffset = bitOffset;
    }

    public uint ReadBit()
    {
      int nIndex = bitOffset / 8;
      int nOffset = bitOffset % 8 + 1;

      bitOffset++;
      return (uint)(bytes[nIndex] >> (8 - nOffset)) & 0x01;
    }

    public uint ReadBits(int n)
    {
      uint r = 0;
      int i;
      for (i = 0; i < n; i++)
      {
        r |= (ReadBit() << (n - i - 1));
      }
      return r;
    }

    public uint ReadExponentialGolombCode()
    {
      uint r = 0;
      int i = 0;

      while ((ReadBit() == 0) && (i < 32))
      {
        i++;
      }
      r = ReadBits(i);
      r += (uint)(1 << i) - 1;
      return r;
    }


    public int ReadSE()
    {
      int r = (int)ReadExponentialGolombCode();
      if ((r & 0x01) != 0)
      {
        r = (r + 1) / 2;
      }
      else
      {
        r = -(r / 2);
      }
      return r;
    }
  }
}