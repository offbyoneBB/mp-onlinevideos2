using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Settings
{
  public static class SettingsWrapper
  {
    public static string GetSettingString(string key)
    {
      object value;
      if (Settings.TryGetValue(key, out value) && value is string)
        return (string)value;
      return string.Empty;
    }

    public static object GetSetting(string key)
    {
      object value;
      return Settings.TryGetValue(key, out value) ? value : null;
    }

    public static bool GetSetting(string settingName, IntPtr settingValue)
    {
      object settingValueObject;
      var res = Settings.TryGetValue(settingName, out settingValueObject);
      if (!res || settingValueObject == null)
      {
        Marshal.WriteByte(settingValue, 0);
        return false;
      }

      Logger.Log("XBMC_get_setting {0}-->{1}", settingName, settingValueObject);
      if (settingValueObject is string)
      {
        settingValueObject += "\0"; // Append termination sign
        var bytes = Encoding.ASCII.GetBytes((string)settingValueObject);
        Marshal.Copy(bytes, 0, settingValue, bytes.Length);
      }
      else if (settingValueObject is byte)
      {
        Marshal.WriteByte(settingValue, (byte)settingValueObject);
      }
      else if (settingValueObject is Int16)
      {
        Marshal.WriteInt16(settingValue, (Int16)settingValueObject);
      }
      else if (settingValueObject is Int32)
      {
        Marshal.WriteInt32(settingValue, (Int32)settingValueObject);
      }
      else if (settingValueObject is Int64)
      {
        Marshal.WriteInt64(settingValue, (Int64)settingValueObject);
      }
      return true;
    }

    public static IntPtr TranslatePath(string source)
    {
      Logger.Log("XBMC_translate_special {0}", source);
      string translatedPath = string.Empty;
      object path;
      if (Settings.TryGetValue("DECRYPTERPATH", out path) && path is String)
        translatedPath = (String)path;

      return Marshal.StringToCoTaskMemAnsi(translatedPath);
    }

    public static Dictionary<string, object> Settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      {"DECRYPTERPATH",  Path.GetDirectoryName(typeof(SettingsWrapper).Assembly.Location)},
      {"MAXRESOLUTION",  2 /* 1920x1080 */}
    };
  }
}
