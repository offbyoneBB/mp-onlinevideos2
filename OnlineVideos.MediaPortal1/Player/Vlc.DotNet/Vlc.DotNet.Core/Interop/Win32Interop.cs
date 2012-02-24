using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// Win32 Interops
    /// </summary>
    internal static class Win32Interop
    {
        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable.</param>
        /// <param name="lpProcName">he function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>If the function succeeds, the return value is the address of the exported function or variable. If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count. When the reference count reaches zero, the module is unloaded from the address space of the calling process and the handle is no longer valid.
        /// </summary>
        /// <param name="hModule">A handle to the loaded library module.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call the GetLastError function.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.
        /// </summary>
        /// <param name="lpFileName">The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file). The name specified is the file name of the module and is not related to the name stored in the library module itself, as specified by the LIBRARY keyword in the module-definition (.def) file. If the string specifies a full path, the function searches only that path for the module. If the string specifies a relative path or a module name without a path, the function uses a standard search strategy to find the module; for more information, see the Remarks. If the function cannot find the module, the function fails. When specifying a path, be sure to use backslashes (\), not forward slashes (/). For more information about paths, see Naming a File or Directory. If the string specifies a module name without a path and the file name extension is omitted, the function appends the default library extension .dll to the module name. To prevent the function from appending .dll to the module name, include a trailing point character (.) in the module name string.</param>
        /// <returns>If the function succeeds, the return value is a handle to the module. If the function fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        /// Creates or opens a named or unnamed file mapping object for a specified file.
        /// </summary>
        /// <param name="hFile">A handle to the file from which to create a file mapping object.</param>
        /// <param name="lpAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether a returned handle can be inherited by child processes. The lpSecurityDescriptor member of the SECURITY_ATTRIBUTES structure specifies a security descriptor for a new file mapping object.</param>
        /// <param name="flProtect">Specifies the page protection of the file mapping object. All mapped views of the object must be compatible with this protection.</param>
        /// <param name="dwMaximumSizeLow">The high-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="dwMaximumSizeHigh">The low-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="lpName">The name of the file mapping object.</param>
        /// <returns>The value is a handle to the newly created file mapping object.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, PageAccess flProtect, int dwMaximumSizeLow, int dwMaximumSizeHigh, string lpName);

        /// <summary>
        /// Maps a view of a file mapping into the address space of a calling process.
        /// </summary>
        /// <param name="hFileMappingObject">A handle to a file mapping object. The CreateFileMapping and OpenFileMapping functions return this handle.</param>
        /// <param name="dwDesiredAccess">The type of access to a file mapping object, which determines the protection of the pages. This parameter can be one of the following values.</param>
        /// <param name="dwFileOffsetHigh">A high-order DWORD of the file offset where the view begins.</param>
        /// <param name="dwFileOffsetLow">A low-order DWORD of the file offset where the view is to begin. The combination of the high and low offsets must specify an offset within the file mapping.</param>
        /// <param name="dwNumberOfBytesToMap">The number of bytes of a file mapping to map to the view. All bytes must be within the maximum size specified by CreateFileMapping. If this parameter is 0 (zero), the mapping extends from the specified offset to the end of the file mapping.</param>
        /// <returns>The value is the starting address of the mapped view.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        /// <summary>
        /// Unmaps a mapped view of a file from the calling process's address space.
        /// </summary>
        /// <param name="lpBaseAddress">A pointer to the base address of the mapped view of a file that is to be unmapped. This value must be identical to the value returned by a previous call to the MapViewOfFile or MapViewOfFileEx function.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public enum PageAccess
        {
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400
        }

        public enum FileMapAccess : uint
        {
            Write = 0x00000002,
            Read = 0x00000004,
            AllAccess = 0x000f001f,
            Copy = 0x00000001,
            Execute = 0x00000020
        }
#if SILVERLIGHT
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr handle);

        [DllImport("kernel32.dll", EntryPoint = "LocalAlloc")]
        public static extern IntPtr LocalAlloc(int uFlags, IntPtr sizetdwBytes);
#endif
    }
}
