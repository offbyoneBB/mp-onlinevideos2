using DirectShow;
using InputStreamSourceFilter.H264;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using MediaPortalWrapper.NativeWrappers;

namespace InputStreamSourceFilter
{
  public class MediaTypeBuilder
  {
    const int FOURCC_H264 = 0x34363248;
    const int FOURCC_AVC1 = 0x31435641;
    static readonly Guid MEDIASUBTYPE_AVC1 = new Guid(0x31435641, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
    static readonly Guid MEDIASUBTYPE_DOLBY_DDPLUS = new Guid("a7fb87af-2d02-42fb-a4d4-05cd93843bdd");
    static readonly Guid MEDIASUBTYPE_RAW_AAC1 = new Guid("{000000FF-0000-0010-8000-00AA00389B71}");

    private static readonly Dictionary<string, Func<InputstreamInfo, AMMediaType>> TYPE_MAPPINGS = new Dictionary<string, Func<InputstreamInfo, AMMediaType>>
    {
      // Types from https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/StreamingMediaGuide/FrequentlyAskedQuestions/FrequentlyAskedQuestions.html
      { "h264", H264_AVC1 }, //        Common codec name as fallback
      { "avc1.64001F", H264_AVC1 }, // H.264 High Profile level 3.1
      { "avc1.640028", H264_AVC1 }, // H.264 High Profile level 4.0
      { "avc1.640029", H264_AVC1 }, // H.264 High Profile level 4.1
      { "mp4a.40.2", AAC_LC }, //      AAC-LC
      { "mp4a.40.5", HE_AAC }, //      HE-AAC No decoder support yet?
      { "ec-3", E_AC3 }, //            E-AC3
    };

    /// <summary>
    /// Tries to create a matching <see cref="AMMediaType"/> for the given <paramref name="streamInfo"/>.
    /// </summary>
    /// <param name="streamInfo">stream</param>
    /// <param name="mediaType">media type</param>
    /// <returns><c>true</c> if successful</returns>
    public static bool TryGetType(InputstreamInfo streamInfo, out AMMediaType mediaType)
    {
      Func<InputstreamInfo, AMMediaType> mediaTypeFn;
      if (TYPE_MAPPINGS.TryGetValue(streamInfo.CodecInternalName, out mediaTypeFn) || TYPE_MAPPINGS.TryGetValue(streamInfo.CodecName, out mediaTypeFn))
      {
        mediaType = mediaTypeFn(streamInfo);
        return true;
      }
      mediaType = null;
      return false;
    }

    /// <summary>
    /// AnnexB formatted h264 bitstream
    /// </summary>
    /// <param name="streamInfo"></param>
    /// <returns></returns>
    public static AMMediaType H264_AnnexB(InputstreamInfo streamInfo)
    {
      H264CodecData codecData = new H264CodecData(streamInfo.ExtraData);
      SPSUnit spsUnit = new SPSUnit(codecData.SPS);
      int width = spsUnit.Width();
      int height = spsUnit.Height();

      VideoInfoHeader2 vi = new VideoInfoHeader2();
      vi.SrcRect.right = width;
      vi.SrcRect.bottom = height;
      vi.TargetRect.right = width;
      vi.TargetRect.bottom = height;

      int hcf = HCF(width, height);
      vi.PictAspectRatioX = width / hcf;
      vi.PictAspectRatioY = height / hcf;

      vi.BmiHeader.Width = width;
      vi.BmiHeader.Height = height;
      vi.BmiHeader.Planes = 1;
      vi.BmiHeader.Compression = FOURCC_H264;

      AMMediaType amt = new AMMediaType();
      amt.majorType = MediaType.Video;
      amt.subType = MediaSubType.H264;
      amt.temporalCompression = true;
      amt.fixedSizeSamples = false;
      amt.sampleSize = 1;
      amt.SetFormat(vi);
      return amt;
    }

    /// <summary>
    /// AVC1 formatted H264 bitstream
    /// </summary>
    /// <param name="streamInfo"></param>
    /// <returns></returns>
    public static AMMediaType H264_AVC1(InputstreamInfo streamInfo)
    {
      H264CodecData codecData = null;
      Mpeg2VideoInfo vi = new Mpeg2VideoInfo();
      byte[] extraData = new byte[0];
      int width = (int)streamInfo.Width;
      int height = (int)streamInfo.Height;

      if (streamInfo.ExtraData.Length > 0)
      {
        codecData = new H264CodecData(streamInfo.ExtraData);

        SPSUnit spsUnit = new SPSUnit(codecData.SPS);
        width = spsUnit.Width();
        height = spsUnit.Height();
      }

      vi.hdr.SrcRect.right = width;
      vi.hdr.SrcRect.bottom = height;
      vi.hdr.TargetRect.right = width;
      vi.hdr.TargetRect.bottom = height;

      int hcf = HCF(width, height);
      vi.hdr.PictAspectRatioX = width / hcf;
      vi.hdr.PictAspectRatioY = height / hcf;

      vi.hdr.BmiHeader.Width = width;
      vi.hdr.BmiHeader.Height = height;

      vi.hdr.BmiHeader.Planes = 1;
      vi.hdr.BmiHeader.Compression = FOURCC_AVC1;
      vi.hdr.BmiHeader.BitCount = 24;

      if (codecData != null)
      {
        vi.dwProfile = (uint)codecData.Profile;
        vi.dwLevel = (uint)codecData.Level;
        vi.dwFlags = (uint)codecData.NALSizeMinusOne + 1;

        extraData = NaluParser.CreateAVC1ParameterSet(codecData.SPS, codecData.PPS, 2);
      }
      else
      {
        // Example: avc1.4D401F -> Main Level 3.1
        // Profile     Value
        // Baseline    42E0
        // Main        4D40
        // High        6400
        // Extended    58A0

        // Level       Hex Value
        // 3.0         1E
        // 3.1         1F
        // 4.1         29
        // 5.1         33
        string codecInfo = streamInfo.CodecInternalName.Split('.').Last();
        if (codecInfo.Length == 6)
        {
          int codecNum;
          if (int.TryParse(codecInfo.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codecNum))
            vi.dwProfile = (uint)codecNum;
          if (int.TryParse(codecInfo.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codecNum))
            vi.dwLevel = (uint)codecNum;
        }
      }

      vi.cbSequenceHeader = (uint)extraData.Length;

      AMMediaType amt = new AMMediaType();
      amt.majorType = MediaType.Video;
      amt.subType = MEDIASUBTYPE_AVC1;
      amt.temporalCompression = true;
      amt.fixedSizeSamples = false;
      amt.sampleSize = 1;
      SetFormat(vi, extraData, amt);
      return amt;
    }

    private static void AssignStreamInfoFields(InputstreamInfo streamInfo, ref WaveFormatEx wf, ref AMMediaType amt)
    {
      wf.nChannels = (ushort)streamInfo.Channels;
      wf.nSamplesPerSec = (int)streamInfo.SampleRate;
      if (wf.nSamplesPerSec == 0)
        wf.nSamplesPerSec = 44100; // Fallback if missing, otherwise audio decoder filter will not connect
      wf.nAvgBytesPerSec = streamInfo.Bandwidth / 8;
      amt.sampleSize = streamInfo.Bandwidth;
    }

    public static AMMediaType AAC_LC(InputstreamInfo streamInfo)
    {
      WaveFormatEx wf = new WaveFormatEx();
      wf.wFormatTag = 255;
      wf.nBlockAlign = 1;
      wf.wBitsPerSample = 16;
      wf.cbSize = 0;

      AMMediaType amt = new AMMediaType();
      AssignStreamInfoFields(streamInfo, ref wf, ref amt);
      amt.majorType = MediaType.Audio;
      amt.subType = MEDIASUBTYPE_RAW_AAC1;
      amt.temporalCompression = false;
      amt.fixedSizeSamples = true;
      amt.SetFormat(wf);
      return amt;
    }


    // TODO: find a working decoder and get matching properties
    public static AMMediaType HE_AAC(InputstreamInfo streamInfo)
    {
      WaveFormatEx wf = new WaveFormatEx();
      wf.wFormatTag = 255;
      wf.nBlockAlign = 1;
      wf.wBitsPerSample = 16;
      wf.cbSize = 0;

      AMMediaType amt = new AMMediaType();
      AssignStreamInfoFields(streamInfo, ref wf, ref amt);
      amt.majorType = MediaType.Audio;
      amt.subType = MEDIASUBTYPE_RAW_AAC1;
      amt.temporalCompression = false;
      amt.fixedSizeSamples = true;
      amt.SetFormat(wf);
      return amt;
    }

    public static AMMediaType E_AC3(InputstreamInfo streamInfo)
    {
      WaveFormatEx wf = new WaveFormatEx();
      wf.wFormatTag = 8192;
      wf.nBlockAlign = 24;
      wf.wBitsPerSample = 32;
      wf.cbSize = 0;

      AMMediaType amt = new AMMediaType();
      AssignStreamInfoFields(streamInfo, ref wf, ref amt);
      amt.majorType = MediaType.Audio;
      amt.subType = MEDIASUBTYPE_DOLBY_DDPLUS;
      amt.temporalCompression = false;
      amt.fixedSizeSamples = true;
      amt.SetFormat(wf);
      return amt;
    }

    /// <summary>
    /// Sets AMMediaType format data with Mpeg2VideoInfo and optional extra data.
    /// </summary>
    /// <param name="vi"></param>
    /// <param name="extraData"></param>
    /// <param name="amt"></param>
    static void SetFormat(Mpeg2VideoInfo vi, byte[] extraData, AMMediaType amt)
    {
      int cb = Marshal.SizeOf(vi);
      int add = extraData == null || extraData.Length < 4 ? 0 : extraData.Length - 4;
      IntPtr ptr = Marshal.AllocCoTaskMem(cb + add);
      try
      {
        Marshal.StructureToPtr(vi, ptr, false);
        if (extraData != null)
          Marshal.Copy(extraData, 0, ptr + cb - 4, extraData.Length);
        amt.SetFormat(ptr, cb + add);
        amt.formatType = FormatType.Mpeg2Video;
      }
      finally
      {
        Marshal.FreeCoTaskMem(ptr);
      }
    }

    /// <summary>
    /// Finds the Highest Common Factor of 2 ints
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    static int HCF(int x, int y)
    {
      return y == 0 ? x : HCF(y, x % y);
    }
  }
}
