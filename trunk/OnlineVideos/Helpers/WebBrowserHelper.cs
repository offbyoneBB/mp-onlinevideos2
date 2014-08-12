using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;

namespace OnlineVideos.Helpers
{
        /// <summary>
        /// Class for clearing the browser cache - required because of some quirks with dynamically added swf files and the webbrowser control
        /// Modified from code originally found here: http://support.microsoft.com/kb/326201
        /// </summary>
        public static class WebBrowserHelper
        {
            private const string SILVERLIGHT_SUB_PATH = @"Microsoft\Silverlight\is";

            #region Definitions/DLL Imports
            [StructLayout(LayoutKind.Sequential)]
            public struct INTERNET_CACHE_ENTRY_INFO
            {
                public UInt32 dwStructSize;
                public string lpszSourceUrlName;
                public string lpszLocalFileName;
                public UInt32 CacheEntryType;
                public UInt32 dwUseCount;
                public UInt32 dwHitRate;
                public UInt32 dwSizeLow;
                public UInt32 dwSizeHigh;
                public FILETIME LastModifiedTime;
                public FILETIME ExpireTime;
                public FILETIME LastAccessTime;
                public FILETIME LastSyncTime;
                public IntPtr lpHeaderInfo;
                public UInt32 dwHeaderInfoSize;
                public string lpszFileExtension;
                public UInt32 dwExemptDelta;
            };
            
            /// <summary>
            /// For PInvoke: Contains information about an entry in the Internet cache
            /// </summary>
            [StructLayout(LayoutKind.Explicit, Size = 80)]
            public struct INTERNET_CACHE_ENTRY_INFOA
            {
                [FieldOffset(0)]
                public uint dwStructSize;
                [FieldOffset(4)]
                public IntPtr lpszSourceUrlName;
                [FieldOffset(8)]
                public IntPtr lpszLocalFileName;
                [FieldOffset(12)]
                public uint CacheEntryType;
                [FieldOffset(16)]
                public uint dwUseCount;
                [FieldOffset(20)]
                public uint dwHitRate;
                [FieldOffset(24)]
                public uint dwSizeLow;
                [FieldOffset(28)]
                public uint dwSizeHigh;
                [FieldOffset(32)]
                public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
                [FieldOffset(40)]
                public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
                [FieldOffset(48)]
                public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
                [FieldOffset(56)]
                public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
                [FieldOffset(64)]
                public IntPtr lpHeaderInfo;
                [FieldOffset(68)]
                public uint dwHeaderInfoSize;
                [FieldOffset(72)]
                public IntPtr lpszFileExtension;
                [FieldOffset(76)]
                public uint dwReserved;
                [FieldOffset(76)]
                public uint dwExemptDelta;
            }

            // For PInvoke: Initiates the enumeration of the cache groups in the Internet cache
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "FindFirstUrlCacheGroup",
                CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr FindFirstUrlCacheGroup(
                int dwFlags,
                int dwFilter,
                IntPtr lpSearchCondition,
                int dwSearchCondition,
                ref long lpGroupId,
                IntPtr lpReserved);

            // For PInvoke: Retrieves the next cache group in a cache group enumeration
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "FindNextUrlCacheGroup",
                CallingConvention = CallingConvention.StdCall)]
            public static extern bool FindNextUrlCacheGroup(
                IntPtr hFind,
                ref long lpGroupId,
                IntPtr lpReserved);

            // For PInvoke: Releases the specified GROUPID and any associated state in the cache index file
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "DeleteUrlCacheGroup",
                CallingConvention = CallingConvention.StdCall)]
            public static extern bool DeleteUrlCacheGroup(
                long GroupId,
                int dwFlags,
                IntPtr lpReserved);

            // For PInvoke: Begins the enumeration of the Internet cache
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "FindFirstUrlCacheEntryA",
                CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr FindFirstUrlCacheEntry(
                [MarshalAs(UnmanagedType.LPTStr)] string lpszUrlSearchPattern,
                IntPtr lpFirstCacheEntryInfo,
                ref int lpdwFirstCacheEntryInfoBufferSize);

            // For PInvoke: Retrieves the next entry in the Internet cache
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "FindNextUrlCacheEntryA",
                CallingConvention = CallingConvention.StdCall)]
            public static extern bool FindNextUrlCacheEntry(
                IntPtr hFind,
                IntPtr lpNextCacheEntryInfo,
                ref int lpdwNextCacheEntryInfoBufferSize);

            // For PInvoke: Removes the file that is associated with the source name from the cache, if the file exists
            [DllImport(@"wininet",
                SetLastError = true,
                CharSet = CharSet.Auto,
                EntryPoint = "DeleteUrlCacheEntryA",
                CallingConvention = CallingConvention.StdCall)]
            public static extern bool DeleteUrlCacheEntry(
                IntPtr lpszUrlName);
            #endregion

            #region Public Static Functions

            /// <summary>
            /// Clears the cache of the web browser
            /// </summary>
            public static void ClearCache(string fileStartsWith, string fileExtension)
            {
                // Indicates that all of the cache groups in the user's system should be enumerated
                const int CACHEGROUP_SEARCH_ALL = 0x0;
                // Indicates that all the cache entries that are associated with the cache group
                // should be deleted, unless the entry belongs to another cache group.
                const int CACHEGROUP_FLAG_FLUSHURL_ONDELETE = 0x2;
                // File not found.
                const int ERROR_FILE_NOT_FOUND = 0x2;
                // No more items have been found.
                const int ERROR_NO_MORE_ITEMS = 259;
                // Pointer to a GROUPID variable
                long groupId = 0;

                // Local variables
                int cacheEntryInfoBufferSizeInitial = 0;
                int cacheEntryInfoBufferSize = 0;
                IntPtr cacheEntryInfoBuffer = IntPtr.Zero;
                INTERNET_CACHE_ENTRY_INFOA internetCacheEntry;
                INTERNET_CACHE_ENTRY_INFO internetCacheEntry2;
                IntPtr enumHandle = IntPtr.Zero;
                bool returnValue = false;

                // Start to delete URLs that do not belong to any group.
                enumHandle = FindFirstUrlCacheEntry(null, IntPtr.Zero, ref cacheEntryInfoBufferSizeInitial);
                 if (enumHandle != IntPtr.Zero && ERROR_NO_MORE_ITEMS == Marshal.GetLastWin32Error())
                   return;
               
                cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
                cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize);
                enumHandle = FindFirstUrlCacheEntry(null, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);

                while (true)
                {
                    internetCacheEntry = (INTERNET_CACHE_ENTRY_INFOA)Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(INTERNET_CACHE_ENTRY_INFOA));
                    internetCacheEntry2 = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(INTERNET_CACHE_ENTRY_INFO));
                    if (ERROR_NO_MORE_ITEMS == Marshal.GetLastWin32Error()) { break; }
                    
                    cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize;

                    returnValue = false;

                    if (!string.IsNullOrEmpty(internetCacheEntry2.lpszLocalFileName) && internetCacheEntry2.lpszLocalFileName.Contains("\\" + fileStartsWith) && internetCacheEntry2.lpszLocalFileName.EndsWith(fileExtension, true, CultureInfo.CurrentCulture))
                        returnValue = DeleteUrlCacheEntry(internetCacheEntry.lpszSourceUrlName);
                   
                    if (!returnValue)
                    {
                        returnValue = FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
                    }
                    if (!returnValue && ERROR_NO_MORE_ITEMS == Marshal.GetLastWin32Error())
                    {
                        break;
                    }
                    if (!returnValue && cacheEntryInfoBufferSizeInitial > cacheEntryInfoBufferSize)
                    {
                        cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
                        cacheEntryInfoBuffer = Marshal.ReAllocHGlobal(cacheEntryInfoBuffer, (IntPtr)cacheEntryInfoBufferSize);
                        returnValue = FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
                    }
                }
                Marshal.FreeHGlobal(cacheEntryInfoBuffer);
            }

            /// <summary>
            /// Is application storage enabled for Silverlight - please note, returns true if it can't determine the setting
            /// </summary>
            /// <returns></returns>
            public static bool IsSilverlightAppStorageEnabled()
            {
                var file = GetSilverlightAppStorageFilePath();

                if (string.IsNullOrEmpty(file)) return true;

                // If the Silverlight file exists, the application storage is disabled
                return !File.Exists(file);
            }

            /// <summary>
            /// Toggle application storage for Silverlight
            /// </summary>
            /// <param name="enabled"></param>
            public static void ToogleSilverlightAppStorage(bool enabled)
            {
                var file = GetSilverlightAppStorageFilePath();

                if (string.IsNullOrEmpty(file)) return;

                if (enabled && File.Exists(file)) File.Delete(file);
                if (!enabled && !File.Exists(file)) File.WriteAllText(file, string.Empty);
            }

            /// <summary>
            /// This will return a string which contains the full path to the "disabled.dat" file.
            /// Basically, Silverlight puts an empty file in C:\Users\{User}\AppData\LocalLow\Microsoft\Silverlight\is\{Random Name}\{Random Name}\1
            /// The file "disabled.dat" is deleted if application storage is subsequently enabled
            /// </summary>
            /// <returns></returns>
            private static string GetSilverlightAppStorageFilePath()
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToString() + "Low";
                var tmpPath = Path.Combine(root, SILVERLIGHT_SUB_PATH);

                if (Directory.Exists(tmpPath))
                {
                    var dirs = Directory.GetDirectories(tmpPath);
                    if (dirs != null && dirs.Count() > 0)
                    {
                        tmpPath = Path.Combine(tmpPath, dirs[0]);

                        dirs = Directory.GetDirectories(tmpPath);
                        if (dirs != null && dirs.Count() > 0)
                        {
                            tmpPath = Path.Combine(tmpPath, dirs[0]);

                            // Look for the "1" sub directory
                            dirs = Directory.GetDirectories(tmpPath, "1");
                            if (dirs != null && dirs.Count() > 0)
                            {
                                tmpPath = Path.Combine(tmpPath, dirs[0]);
                                tmpPath = Path.Combine(tmpPath, "disabled.dat");
                                return tmpPath;
                            }
                        }
                    }
                }

                return string.Empty;

            }

            #endregion
        }

}
