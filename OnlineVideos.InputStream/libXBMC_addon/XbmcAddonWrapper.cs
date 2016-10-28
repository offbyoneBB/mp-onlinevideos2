using System;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortalWrapper.Filesystem;
using MediaPortalWrapper.Settings;
using MediaPortalWrapper.Utils;
using RGiesecke.DllExport;

namespace libXBMC_addon
{
  public class XbmcAddonWrapper
  {
    // https://github.com/xbmc/xbmc/blob/master/xbmc/addons/kodi-addon-dev-kit/include/kodi/libXBMC_addon.h
    public enum QueueMsg
    {
      Info,
      Warning,
      Error
    }

    public enum AddonLogLevel
    {
      Debug,
      Info,
      Notice,
      Error
    }

    #region CUrl methods

    [DllExport("XBMC_curl_create", CallingConvention.Cdecl)]
    public static IntPtr XBMC_curl_create(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string url)
    {
      Logger.Log("XBMC_curl_create: {0}", url);
      UrlSource urlSource = new UrlSource();
      if (!urlSource.UrlCreate(url))
      {
        urlSource.Dispose();
        return IntPtr.Zero;
      }
      return FileFactory.AddFile(urlSource);
    }

    [DllExport("XBMC_curl_add_option", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_curl_add_option(IntPtr handle, IntPtr callback, IntPtr file, CURLOPTIONTYPE type, [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string value)
    {
      Logger.Log("XBMC_curl_add_option {0}|{1}|{2}|{3}", file, type, name, value);
      AbstractFile f;
      if (FileFactory.TryGetValue(file, out f))
        return f.AddOption(type, name, value);
      return false;
    }

    [DllExport("XBMC_curl_open", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_curl_open(IntPtr handle, IntPtr callback, IntPtr file, uint flags)
    {
      Logger.Log("XBMC_curl_open: {0}|{1}", file, flags);

      AbstractFile f;
      if (FileFactory.TryGetValue(file, out f))
        return f.Open(flags);
      return false;
    }

    #endregion

    #region Directory

    [DllExport("XBMC_can_open_directory", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_can_open_directory(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string url)
    {
      Logger.Log("XBMC_can_open_directory: {0}", url);
      return true;
    }

    [DllExport("XBMC_directory_exists", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_directory_exists(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string path)
    {
      Logger.Log("XBMC_directory_exists: {0}", path);
      return true;
    }

    [DllExport("XBMC_create_directory", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_create_directory(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string path)
    {
      Logger.Log("XBMC_create_directory: {0}", path);
      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);
      return true;
    }

    [DllExport("XBMC_get_directory", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_get_directory(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string strPath, [MarshalAs(UnmanagedType.LPStr)] string mask, IntPtr items, IntPtr numItems)
    {
      Logger.Log("XBMC_get_directory {0}|{1}", strPath, mask);
      return DirectoryHelper.GetDirectory(strPath, mask, items, numItems);
    }

    [DllExport("XBMC_remove_directory", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_remove_directory(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string path)
    {
      Logger.Log("XBMC_remove_directory: {0}", path);
      return true;
    }

    [DllExport("XBMC_free_directory", CallingConvention.Cdecl)]
    public static void XBMC_free_directory(IntPtr handle, IntPtr callback, IntPtr items, uint numItems)
    {
      DirectoryHelper.FreeDirectory(items, numItems);
      Logger.Log("XBMC_free_directory: {0}|{1} susccessful", items, numItems);
    }

    #endregion

    #region File

    [DllExport("XBMC_file_exists", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_file_exists(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.I1)] bool useCache)
    {
      Logger.Log("XBMC_file_exists: {0}", filename);
      return false;
    }

    [DllExport("XBMC_open_file", CallingConvention.Cdecl)]
    public static IntPtr XBMC_open_file(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string filename, int flags)
    {
      Logger.Log("XBMC_open_file {0}", filename);
      return IntPtr.Zero;
    }

    [DllExport("XBMC_open_file_for_write", CallingConvention.Cdecl)]
    public static IntPtr XBMC_open_file_for_write(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.I1)] bool overwrite)
    {
      Logger.Log("XBMC_open_file_for_write {0}", filename);
      return IntPtr.Zero;
    }

    [DllExport("XBMC_get_file_chunk_size", CallingConvention.Cdecl)]
    public static int XBMC_get_file_chunk_size(IntPtr handle, IntPtr callback, IntPtr file)
    {
      Logger.Log("XBMC_get_file_chunk_size");
      return 0;
    }

    [DllExport("XBMC_get_file_download_speed", CallingConvention.Cdecl)]
    public static double XBMC_get_file_download_speed(IntPtr handle, IntPtr callback, IntPtr file)
    {
      AbstractFile f;
      if (file == IntPtr.Zero || !FileFactory.TryGetValue(file, out f))
        return 0;
      var result = f.DownloadSpeed;
      Logger.Log("XBMC_get_file_download_speed: {0}", result);
      return result;
    }

    [DllExport("XBMC_get_file_length", CallingConvention.Cdecl)]
    public static long XBMC_get_file_length(IntPtr handle, IntPtr callback, IntPtr file)
    {
      Logger.Log("XBMC_get_file_length");
      return 0;
    }

    [DllExport("XBMC_get_file_position", CallingConvention.Cdecl)]
    public static long XBMC_get_file_position(IntPtr handle, IntPtr callback, IntPtr file)
    {
      Logger.Log("XBMC_get_file_position");
      return 0;
    }

    [DllExport("XBMC_close_file", CallingConvention.Cdecl)]
    public static void XBMC_close_file(IntPtr handle, IntPtr callback, IntPtr file)
    {
      Logger.Log("XBMC_close_file: {0}", file);
      FileFactory.Remove(file);
    }

    [DllExport("XBMC_read_file", CallingConvention.Cdecl)]
    public static int XBMC_read_file(IntPtr handle, IntPtr callback, IntPtr file, IntPtr buffer, int bufferSize)
    {
      AbstractFile f;
      if (buffer == IntPtr.Zero || !FileFactory.TryGetValue(file, out f))
        return -1;
      int numRead = f.Read(buffer, bufferSize);
      if (numRead == 0)
        Logger.Log(" - total read: {0}", f.TotalBytesRead);
      return numRead;
    }

    [DllExport("XBMC_write_file", CallingConvention.Cdecl)]
    public static IntPtr XBMC_write_file(IntPtr handle, IntPtr callback, IntPtr file, IntPtr buffer, IntPtr bufferSize)
    {
      Logger.Log("XBMC_write_file");
      return IntPtr.Zero;
    }

    [DllExport("XBMC_read_file_string", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_read_file_string(IntPtr handle, IntPtr callback, IntPtr file, [MarshalAs(UnmanagedType.LPStr)] string line, int lineSize)
    {
      Logger.Log("XBMC_read_file_string");
      return true;
    }

    [DllExport("XBMC_seek_file", CallingConvention.Cdecl)]
    public static long XBMC_seek_file(IntPtr handle, IntPtr callback, IntPtr file, long offset, int origin)
    {
      Logger.Log("XBMC_seek_file");
      return 0;
    }

    [DllExport("XBMC_stat_file", CallingConvention.Cdecl)]
    public static int XBMC_stat_file(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string filename, IntPtr buffer)
    {
      Logger.Log("XBMC_stat_file");
      return 0;
    }

    [DllExport("XBMC_delete_file", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_delete_file(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string filename)
    {
      Logger.Log("XBMC_delete_file {0}", filename);
      return true;
    }

    [DllExport("XBMC_truncate_file", CallingConvention.Cdecl)]
    public static int XBMC_truncate_file(IntPtr handle, IntPtr callback, IntPtr file, long size)
    {
      Logger.Log("XBMC_truncate_file");
      return 0;
    }

    [DllExport("XBMC_flush_file", CallingConvention.Cdecl)]
    public static void XBMC_flush_file(IntPtr handle, IntPtr callback, IntPtr file)
    {
      Logger.Log("XBMC_flush_file: {0}", file);
    }

    #endregion

    #region Settings

    [DllExport("XBMC_get_setting", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_get_setting(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string settingName, IntPtr settingValue)
    {
      // Note: target buffer is allocated by the caller, but no length argument is available. So we need to trust that the caller allocated enough space
      Logger.Log("XBMC_get_setting {0}", settingName);
      return SettingsWrapper.GetSetting(settingName, settingValue);
    }

    [DllExport("XBMC_translate_special", CallingConvention.Cdecl)]
    public static IntPtr XBMC_translate_special(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string source)
    {
      Logger.Log("XBMC_translate_special {0}", source);
      object path;
      if (SettingsWrapper.Settings.TryGetValue("DECRYPTERPATH", out path) && path is String)
        return Marshal.StringToCoTaskMemAnsi((String)path);
      return IntPtr.Zero;
    }

    #endregion

    #region Strings

    [DllExport("XBMC_free_string", CallingConvention.Cdecl)]
    public static void XBMC_free_string(IntPtr handle, IntPtr callback, IntPtr str)
    {
      Logger.Log("XBMC_free_string");
      Marshal.FreeCoTaskMem(str);
      Logger.Log("XBMC_free_string: {0} successful", str);
    }

    [DllExport("XBMC_unknown_to_utf8", CallingConvention.Cdecl)]
    public static IntPtr XBMC_unknown_to_utf8(IntPtr handle, IntPtr callback, [MarshalAs(UnmanagedType.LPStr)] string str)
    {
      Logger.Log("XBMC_unknown_to_utf8");
      return IntPtr.Zero;
    }

    [DllExport("XBMC_get_localized_string", CallingConvention.Cdecl)]
    public static IntPtr XBMC_get_localized_string(IntPtr handle, IntPtr callback, int dwCode)
    {
      Logger.Log("XBMC_get_localized_string");
      return IntPtr.Zero;
    }

    [DllExport("XBMC_get_dvd_menu_language", CallingConvention.Cdecl)]
    public static IntPtr XBMC_get_dvd_menu_language(IntPtr handle, IntPtr callback)
    {
      Logger.Log("XBMC_get_dvd_menu_language");
      return IntPtr.Zero;
    }

    #endregion

    #region Log, notifications, register

    [DllExport("XBMC_log", CallingConvention.Cdecl)]
    public static void XBMC_log(IntPtr handle, IntPtr callback, AddonLogLevel logLevel, [MarshalAs(UnmanagedType.LPStr)] string message)
    {
      Logger.Log((int)logLevel, "{0}", message);
    }

    [DllExport("XBMC_queue_notification", CallingConvention.Cdecl)]
    public static void XBMC_queue_notification(IntPtr handle, IntPtr callback, QueueMsg level, [MarshalAs(UnmanagedType.LPStr)] string message)
    {
      Logger.Log("XBMC_queue_notification");
    }

    [DllExport("XBMC_register_me", CallingConvention.Cdecl)]
    public static IntPtr XBMC_register_me(IntPtr handle)
    {
      Logger.Log("XBMC_register_me");
      return (IntPtr)1;
    }

    [DllExport("XBMC_unregister_me", CallingConvention.Cdecl)]
    public static void XBMC_unregister_me(IntPtr handle, IntPtr callback)
    {
      Logger.Log("XBMC_unregister_me");
    }

    [DllExport("XBMC_wake_on_lan", CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static bool XBMC_wake_on_lan(IntPtr handle, IntPtr callback, IntPtr macAddress)
    {
      Logger.Log("XBMC_wake_on_lan");
      return true;
    }

    #endregion
  }
}
