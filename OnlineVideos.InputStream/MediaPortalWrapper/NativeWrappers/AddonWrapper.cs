using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MediaPortalWrapper.NativeWrappers
{
  static class NativeMethods
  {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDllDirectory(string lpPathName);
  }

  public enum AddonStatus
  {
    Ok,
    LostConnection,
    NeedRestart,
    NeedSettings,
    Unknown,
    NeedSavedSettings,
    PermanentFailure   /**< permanent failure, like failing to resolve methods */
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct AddonStructSetting
  {
    public int Type;
    [MarshalAs(UnmanagedType.LPStr)]
    public string Id;
    [MarshalAs(UnmanagedType.LPStr)]
    public string Label;
    public int Current;
    public IntPtr Entry;
    public uint NumberOfEntries;
  }

  /*!
   * @brief Handle used to return data from the PVR add-on to CPVRClient
   */
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct AddonHandleStruct
  {
    public IntPtr CallerAddress;  /*!< address of the caller */
    public IntPtr DataAddress;    /*!< address to store data in */
    public int DataIdentifier; /*!< parameter to pass back when calling the callback */
  }

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void GetAddonDlg(IntPtr pAddon);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate AddonStatus CreateDlg(ref AddonCB addonCb, IntPtr info);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void StopDlg();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void DestroyDlg();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate AddonStatus GetStatusDlg();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  public delegate bool HasSettingsDlg();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate uint GetSettingsDlg(IntPtr settings);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void FreeSettingsDlg();

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate AddonStatus SetSettingDlg([MarshalAs(UnmanagedType.LPStr)]string settingName, IntPtr settingValue);


  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct AddonCB
  {
    [MarshalAs(UnmanagedType.LPStr)]
    public string LibPath;
    public IntPtr addonData;
    public Delegate AddOnLib_RegisterMe;
    public Delegate AddOnLib_UnRegisterMe;
    public Delegate AudioEngineLib_RegisterMe;
    public Delegate AudioEngineLib_UnRegisterMe;
    public Delegate CodecLib_RegisterMe;
    public Delegate CodecLib_UnRegisterMe;
    public Delegate GUILib_RegisterMe;
    public Delegate GUILib_UnRegisterMe;
    public Delegate PVRLib_RegisterMe;
    public Delegate PVRLib_UnRegisterMe;
    public Delegate ADSPLib_RegisterMe;
    public Delegate ADSPLib_UnRegisterMe;
    public Delegate INPUTSTREAMLib_RegisterMe;
    public Delegate INPUTSTREAMLib_UnRegisterMe;
    public Delegate PeripheralLib_RegisterMe;
    public Delegate PeripheralLib_UnRegisterMe;
  }

  //<TStruct, TProps> : IDllAddon<TStruct, TProps>, 
  public class DllAddonWrapper<TFunc> : IDisposable where TFunc : new()
  {
    private UnmanagedLibrary _pDll;
    private IntPtr _ptrStruct;

    public TFunc Addon
    {
      get
      {
        int sizeOfStruct = Marshal.SizeOf(typeof(TFunc));
        _ptrStruct = Marshal.AllocCoTaskMem(sizeOfStruct);
        GetAddon(_ptrStruct);
        var res = (TFunc)Marshal.PtrToStructure(_ptrStruct, typeof(TFunc));
        return res;
      }
    }

    public IntPtr StructPtr
    {
      get { return _ptrStruct; }
    }

    public GetAddonDlg GetAddon { get; private set; }
    public CreateDlg Create { get; private set; }
    public StopDlg Stop { get; private set; }
    public DestroyDlg Destroy { get; private set; }
    public GetStatusDlg GetStatus { get; private set; }
    public HasSettingsDlg HasSettings { get; private set; }
    public GetSettingsDlg GetSettings { get; private set; }
    public FreeSettingsDlg FreeSettings { get; private set; }
    public SetSettingDlg SetSetting { get; private set; }

    public void Init(string addonDllPath)
    {
      _pDll = new UnmanagedLibrary(addonDllPath);

      if (_pDll == null)
      {
        var lasterror = Marshal.GetLastWin32Error();
        var innerEx = new Win32Exception(lasterror);
        throw innerEx;
      }

      GetAddon = _pDll.GetUnmanagedFunction<GetAddonDlg>("get_addon");
      Create = _pDll.GetUnmanagedFunction<CreateDlg>("ADDON_Create");
      Stop = _pDll.GetUnmanagedFunction<StopDlg>("ADDON_Stop");
      Destroy = _pDll.GetUnmanagedFunction<DestroyDlg>("ADDON_Destroy");
      GetStatus = _pDll.GetUnmanagedFunction<GetStatusDlg>("ADDON_GetStatus");
      HasSettings = _pDll.GetUnmanagedFunction<HasSettingsDlg>("ADDON_HasSettings");
      GetSettings = _pDll.GetUnmanagedFunction<GetSettingsDlg>("ADDON_GetSettings");
      FreeSettings = _pDll.GetUnmanagedFunction<FreeSettingsDlg>("ADDON_FreeSettings");
      SetSetting = _pDll.GetUnmanagedFunction<SetSettingDlg>("ADDON_SetSetting");
    }

    public void Dispose()
    {
      Marshal.FreeCoTaskMem(_ptrStruct);
      Stop();
      Destroy();
      if (_pDll != null)
        _pDll.Dispose();
    }
  }
}

