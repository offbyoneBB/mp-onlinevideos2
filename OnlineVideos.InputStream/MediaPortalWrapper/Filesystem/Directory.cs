using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using MediaPortalWrapper.Utils;

namespace MediaPortalWrapper.Filesystem
{
  public class ManagedDirEntry
  {
    public string Label; //!< item label
    public string Path; //!< item path
    public bool IsFolder; //!< Item is a folder
    public long Size;

    public ManagedDirEntry(string label, string path)
    {
      Label = label;
      Path = path;
    }
  }

  // https://github.com/xbmc/xbmc/blob/master/xbmc/addons/kodi-addon-dev-kit/include/kodi/kodi_vfs_types.h
  [StructLayout(LayoutKind.Sequential)]//, Pack = 1 causes crash
  public struct NativeVFSDirEntry
  {
    public IntPtr PtrLabel;           //!< item label
    public IntPtr PtrPath;            //!< item path
    [MarshalAs(UnmanagedType.I1)]
    public bool IsFolder;             //!< Item is a folder
    public long Size;                 //!< Size of file represented by item

    public NativeVFSDirEntry(string labelStr, string pathStr)
      : this()
    {
      Set(labelStr, pathStr);
    }

    public void Set(string labelStr, string pathStr)
    {
      labelStr.SetStringCO(ref PtrLabel);
      pathStr.SetStringCO(ref PtrPath);
    }

    public string Label { get { return PtrLabel.ToManaged(); } }
    public string Path { get { return PtrPath.ToManaged(); } }

    public override string ToString()
    {
      return Label;
    }

    public void Dispose()
    {
      PtrExtension.FreeCO(ref PtrLabel);
      PtrExtension.FreeCO(ref PtrPath);
    }
  }

  public static class DirectoryHelper
  {
    public static bool GetDirectory(string strPath, string mask, IntPtr target, IntPtr numItemsPtr)
    {
      var list = GetDirectory(strPath, mask);
      List<NativeVFSDirEntry> itemsArray = new List<NativeVFSDirEntry>();
      foreach (ManagedDirEntry managedDirEntry in list)
      {
        itemsArray.Add(new NativeVFSDirEntry(managedDirEntry.Label, managedDirEntry.Path) { IsFolder = managedDirEntry.IsFolder, Size = managedDirEntry.Size });
      }

      int numItems = itemsArray.Count;
      var sizeOf = Marshal.SizeOf(typeof(NativeVFSDirEntry));
      IntPtr res = Marshal.AllocCoTaskMem(numItems * sizeOf);
      for (int i = 0; i < numItems; i++)
        Marshal.StructureToPtr(itemsArray[i], res + (i * sizeOf), true);

      Marshal.WriteIntPtr(target, res);
      Marshal.WriteInt32(numItemsPtr, numItems);
      return true;
    }

    public static void FreeDirectory(IntPtr items, uint numItems)
    {
      var sizeOf = Marshal.SizeOf(typeof(NativeVFSDirEntry));
      for (int i = 0; i < numItems; i++)
      {
        NativeVFSDirEntry entry = Marshal.PtrToStructure<NativeVFSDirEntry>(IntPtr.Add(items, i * sizeOf));
        entry.Dispose();
      }
      Marshal.FreeCoTaskMem(items);
    }

    public static List<ManagedDirEntry> GetDirectory(string strPath, string mask)
    {
      List<ManagedDirEntry> vsEntries = new List<ManagedDirEntry>();
      if (string.IsNullOrEmpty(mask))
        mask = "*.*";
      foreach (var entry in Directory.GetDirectories(strPath, mask, SearchOption.AllDirectories))
      {
        var dirName = new DirectoryInfo(entry).Name;
        var vsEntry = new ManagedDirEntry(dirName, entry) { IsFolder = true };
        vsEntries.Add(vsEntry);
      }
      foreach (var entry in Directory.GetFiles(strPath, mask, SearchOption.AllDirectories))
      {
        ManagedDirEntry vsEntry = new ManagedDirEntry(Path.GetFileName(entry), entry) { IsFolder = false, Size = new FileInfo(entry).Length };
        vsEntries.Add(vsEntry);
      }
      Logger.Log("-  Total: {0}", vsEntries.Count);
      return vsEntries;
    }
  }
}
