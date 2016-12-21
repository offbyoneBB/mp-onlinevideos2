using System;
using System.Collections.Generic;

namespace MediaPortalWrapper.Filesystem
{
  public static class FileFactory
  {
    private static readonly IDictionary<IntPtr, AbstractFile> _files = new Dictionary<IntPtr, AbstractFile>();
    private static readonly object _fileIndexLock = new object();
    private static IntPtr _nextFileIndex = new IntPtr(1);

    public static IntPtr AddFile(AbstractFile file)
    {
      lock (_fileIndexLock)
      {
        IntPtr index = _nextFileIndex;
        _files.Add(index, file);
        _nextFileIndex = IntPtr.Add(_nextFileIndex, 1);
        return index;
      }
    }

    public static bool TryGetValue(IntPtr key, out AbstractFile file)
    {
      lock (_fileIndexLock)
      {
        return _files.TryGetValue(key, out file) && file != null;
      }
    }

    public static bool Remove(IntPtr key)
    {
      lock (_fileIndexLock)
      {
        AbstractFile file;
        if (TryGetValue(key, out file))
        {
          file.Dispose();
          _files.Remove(key);
          return true;
        }
        return false;
      }
    }
  }
}
