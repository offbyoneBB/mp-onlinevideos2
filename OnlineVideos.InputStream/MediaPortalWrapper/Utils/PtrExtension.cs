using System;
using System.Runtime.InteropServices;

namespace MediaPortalWrapper.Utils
{
  public static class PtrExtension
  {
    public static void SetStringHG(this string value, ref IntPtr ptr)
    {
      ptr.FreeHG();
      ptr = Marshal.StringToHGlobalAnsi(value ?? string.Empty);
    }

    public static void FreeHG(this IntPtr ptr)
    {
      if (ptr != IntPtr.Zero)
        Marshal.FreeHGlobal(ptr);
    }

    public static void FreeHG(ref IntPtr ptr)
    {
      ptr.FreeHG();
      ptr = IntPtr.Zero;
    }

    public static void SetStringCO(this string value, ref IntPtr ptr)
    {
      ptr.FreeCO();
      ptr = Marshal.StringToCoTaskMemAnsi(value ?? string.Empty);
    }

    public static void FreeCO(this IntPtr ptr)
    {
      if (ptr != IntPtr.Zero)
        Marshal.FreeCoTaskMem(ptr);
    }

    public static void FreeCO(ref IntPtr ptr)
    {
      ptr.FreeCO();
      ptr = IntPtr.Zero;
    }

    public static string ToManaged(this IntPtr ptr)
    {
      return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : string.Empty;
    }
  }
}
