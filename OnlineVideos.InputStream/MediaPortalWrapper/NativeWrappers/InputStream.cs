using System;
using System.Runtime.InteropServices;
using System.Text;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.NativeWrappers
{
  // https://github.com/xbmc/xbmc/blob/master/xbmc/addons/kodi-addon-dev-kit/include/kodi/kodi_inputstream_types.h
  public enum StreamType
  {
    None,
    Video,
    Audio,
    Subtitle,
    Teletext
  }

  public enum CodecFeatures
  {
    Decode = 1
  }

  [Flags]
  public enum StreamFlags
  {
    None = 0x0000,
    Default = 0x0001,
    Dub = 0x0002,
    Original = 0x0004,
    Comment = 0x0008,
    Lyrics = 0x0010,
    Karaoke = 0x0020,
    Forced = 0x0040,
    HearingImpaired = 0x0080,
    VisualImpaired = 0x0100
  }

  public enum StreamcodecProfile
  {
    CodecProfileUnknown = 0,
    CodecProfileNotNeeded,
    H264CodecProfileBaseline,
    H264CodecProfileMain,
    H264CodecProfileExtended,
    H264CodecProfileHigh,
    H264CodecProfileHigh10,
    H264CodecProfileHigh422,
    H264CodecProfileHigh444Predictive
  }

  public enum CryptoKeySystem
  {
    None = 0,
    Widevine,
    PlayReady,
    Count
  }

  #region structs

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct CryptoInfo
  {
    public CryptoKeySystem CryptoKeySystem;                 /*!< @brief keysystem for encrypted media, KEY_SYSTEM_NONE for unencrypted media */
    public byte Flags;
    public short CryptoSessionIdSize;      /*!< @brief The size of the crypto session key id */
    [MarshalAs(UnmanagedType.LPStr)]
    public string CryptoSessionId;       /*!< @brief The crypto session key id */
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct ListItemProperty
  {
    public ListItemProperty(string key, string value)
    {
      Key = key;
      Value = value;
    }
    [MarshalAs(UnmanagedType.LPStr)]
    public string Key;
    [MarshalAs(UnmanagedType.LPStr)]
    public string Value;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct InputStreamConfig
  {
    public const uint MAX_INFO_COUNT = 8;

    [MarshalAs(UnmanagedType.LPStr)]
    public string Url;

    public uint CountInfoValues;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public ListItemProperty[] Properties;

    [MarshalAs(UnmanagedType.LPStr)]
    public string LibFolder;
    [MarshalAs(UnmanagedType.LPStr)]
    public string ProfileFolder;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct InputstreamProps
  {
    public int dummy;
  }

  [Flags]
  public enum MaskType
  {
    SupportsIDemux = 1,
    SupportsIPosTime = 2,
    SupportsIDisplayTime = 4,
    SupportsSeek = 8,
    SupportsPause = 16
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct InputstreamCapabilities
  {
    public MaskType Mask;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct InputstreamIds
  {
    public uint StreamCount;
    public fixed uint StreamIds[32];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public unsafe struct InputstreamInfo
  {
    private string FixedToString(byte* value, int len)
    {
      return Marshal.PtrToStringAnsi((IntPtr)value, len).Trim(' ', '\0');
    }
    private byte[] FixedToByteArray(IntPtr value, int len)
    {
      if (value == IntPtr.Zero || len == 0)
        return new byte[0];
      byte[] result = new byte[len];
      Marshal.Copy(value, result, 0, len);
      return result;
    }

    public string CodecName
    {
      get { fixed (byte* c = m_codecName) return FixedToString(c, 32); }
      set
      {
        fixed (byte* c = m_codecName)
        {
          var bytes = Encoding.UTF8.GetBytes(value);
          Marshal.Copy(bytes, 0, (IntPtr)c, Math.Min(bytes.Length, 4));
        }
      }
    }
    public string CodecInternalName
    {
      get { fixed (byte* c = m_codecInternalName) return FixedToString(c, 32); }
      set
      {
        fixed (byte* c = m_codecInternalName)
        {
          var bytes = Encoding.UTF8.GetBytes(value);
          Marshal.Copy(bytes, 0, (IntPtr)c, Math.Min(bytes.Length, 32));
        }
      }
    }
    public string Language
    {
      get { fixed (byte* c = m_language) return FixedToString(c, 4); }
      set
      {
        fixed (byte* c = m_language)
        {
          var bytes = Encoding.UTF8.GetBytes(value);
          Marshal.Copy(bytes, 0, (IntPtr)c, Math.Min(bytes.Length, 4));
        }
      }
    }
    public byte[] ExtraData
    {
      get { return FixedToByteArray(m_ExtraData, (int)ExtraSize); }
      set
      {
        m_ExtraData.FreeCO();
        m_ExtraData = Marshal.AllocCoTaskMem((int)ExtraSize);
        Marshal.Copy(value, 0, m_ExtraData, (int)ExtraSize);
      }
    }

    public StreamType StreamType;
    public CodecFeatures CodecFeatures;
    public StreamFlags Flags;

    public fixed byte m_codecName[32];                /*!< @brief (required) name of codec according to ffmpeg */
    public fixed byte m_codecInternalName[32];        /*!< @brief (optional) internal name of codec (selectionstream info) */

    public StreamcodecProfile CodecProfile;  /*!< @brief (optional) the profile of the codec */

    public uint StreamId;                  /*!< @brief (required) physical index */

    public IntPtr m_ExtraData; // TODO: Attention uint8_t* -> 8 bit vs. 32 bit?!
    public uint ExtraSize;

    public fixed byte m_language[4];                  /*!< @brief ISO 639 3-letter language code (empty string if undefined) */

    public uint FpsScale;             /*!< @brief Scale of 1000 and a rate of 29970 will result in 29.97 fps */
    public uint FpsRate;
    public uint Height;               /*!< @brief height of the stream reported by the demuxer */
    public uint Width;                /*!< @brief width of the stream reported by the demuxer */
    public float Aspect;                      /*!< @brief display aspect of stream */

    public uint Channels;             /*!< @brief (required) amount of channels */
    public uint SampleRate;           /*!< @brief (required) sample rate */
    public uint BitRate;              /*!< @brief (required) bit rate */
    public uint BitsPerSample;        /*!< @brief (required) bits per sample */
    public uint BlockAlign;
    public CryptoInfo CryptoInfo;

    public override string ToString()
    {
      fixed (byte* c = m_codecName)
      fixed (byte* ci = m_codecInternalName)
      fixed (byte* l = m_language)
      {
        return String.Format("ID: {0}; Codec: {6} ({8}); Lang: {7}; FPS: {1}; Res: {2}x{3}; Ch: {4}; Rate: {5}", StreamId,
          FpsRate, Width, Height, Channels, BitRate,
          FixedToString(c, 32),
          FixedToString(l, 4),
          FixedToString(ci, 32)
          );
      }
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct AddonToHostFuncTableInputStream /* internal */
  {
    public IntPtr /*KODI_HANDLE*/ HostInstance;
    public AllocateDemuxPacketDlg AllocateDemuxPacket;
    public AllocateEncryptedDemuxPacketDlg AllocateEncryptedDemuxPacket;
    public FreeDemuxPacketDlg FreeDemuxPacket;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public unsafe struct AddonInstance_InputStream /* internal */
  {
    public InputstreamProps Props;
    public IntPtr /*AddonToHostFuncTableInputStream* */ ToHost;
    public IntPtr /*HostToAddonFuncTableInputStream* */ ToAddon;
  }

  #endregion

  #region delegates

  // Returns "DemuxPacket*" where DemuxPacket is a struct.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr AllocateDemuxPacketDlg(IntPtr hostInstance, int dataSize);

  // Returns "DemuxPacket*" where DemuxPacket is a struct.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr AllocateEncryptedDemuxPacketDlg(IntPtr hostInstance, uint dataSize, uint encryptedSubSampleCount);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void FreeDemuxPacketDlg(IntPtr hostInstance, IntPtr demuxPacket);

  // InputstreamCapabilities (INPUTSTREAM_CAPABILITIES)
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.Struct)]
  public delegate InputstreamCapabilities GetCapabilitiesDlg(IntPtr addonInstance);

  // INPUTSTREAM_IDS
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.Struct)]
  public delegate InputstreamIds GetStreamIdsDlg(IntPtr addonInstance);

  // INPUTSTREAM_INFO
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.Struct)]
  public delegate InputstreamInfo GetStreamDlg(IntPtr addonInstance, int streamIdx);

  // Returns "DemuxPacket*" where DemuxPacket is a struct.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr DemuxDlg(IntPtr addonInstance);

  // The inputStream parameter is actually a reference, not a pointer.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool OpenDlg(IntPtr addonInstance, ref InputStreamConfig inputStreamConfig);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool OpenStreamDlg(IntPtr addonInstance, int streamId);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void VoidDlg(IntPtr addonInstance);

  // Cannot directly return a string, because the marshaller will automatically
  // free the associated memory, which is not always the right thing to do.
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr StringDlg(IntPtr addonInstance);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate int IntDlg(IntPtr addonInstance);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool BoolDlg(IntPtr addonInstance);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate long LongDlg(IntPtr addonInstance);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void EnableStreamDlg(IntPtr addonInstance, int streamIdx, [MarshalAs(UnmanagedType.I1)] bool enable);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool DemuxSeekTimeDlg(IntPtr addonInstance, double time, [MarshalAs(UnmanagedType.I1)] bool backwards, ref double startpts);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void DemuxSetSpeedDlg(IntPtr addonInstance, int speed);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void SetVideoResolutionDlg(IntPtr addonInstance, int width, int height);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool PosTimeDlg(IntPtr addonInstance, int pos);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate int ReadStreamDlg(IntPtr addonInstance, IntPtr buffer, int bufferSize);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate long SeekStreamDlg(IntPtr addonInstance, long post, int iWhence);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void PauseStreamDlg(IntPtr addonInstance, double rate);

  #endregion

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public unsafe struct HostToAddonFuncTableInputStream
  {
    public AddonInstance_InputStream* addonInstance; // TODO

    public OpenDlg Open;
    public VoidDlg Close;
    public StringDlg GetPathList;
    public GetCapabilitiesDlg GetCapabilities;

    // IDemux
    public GetStreamIdsDlg GetStreamIds;
    public GetStreamDlg GetStream;
    public EnableStreamDlg EnableStream;
    public OpenStreamDlg OpenStream;
    public VoidDlg DemuxReset;
    public VoidDlg DemuxAbort;
    public VoidDlg DemuxFlush;
    public DemuxDlg DemuxRead;
    public DemuxSeekTimeDlg DemuxSeekTime;
    public DemuxSetSpeedDlg DemuxSetSpeed;
    public SetVideoResolutionDlg SetVideoResolution;

    // IDisplayTime
    public IntDlg GetTotalTime;
    public IntDlg GetTime;

    // IPosTime
    public PosTimeDlg PosTime;

    // Seekable (mandatory)
    public BoolDlg CanPauseStream;
    public BoolDlg CanSeekStream;

    public ReadStreamDlg ReadStream;
    public SeekStreamDlg SeekStream;
    public LongDlg PositionStream;
    public LongDlg LengthStream;
    public PauseStreamDlg PauseStream;
    public BoolDlg IsRealTimeStream;
  }

  public class Constants
  {
    public const int DMX_SPECIALID_STREAMINFO = -10;
    public const int DMX_SPECIALID_STREAMCHANGE = -11;
    public const byte FLAG_SECURE_DECODER = 1; /*!< @brief is set in flags if decoding has to be done in TEE environment */
  }
}
