using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{
    internal static class MonoInterop
    {
        [DllImport("libdl.so", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr dlopen(string file, RTLD mode);

        [DllImport("libdl.so", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr dlsym(IntPtr handle, string name);

        [DllImport("libdl.so", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern void dlclose(IntPtr handle);

        [DllImport("libdl.so", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern string dlerror();

        [Flags]
        internal enum RTLD
        {
            /// <summary>
            /// Relocations are performed at an implementation-defined time.
            /// </summary>
            LAZY = 0x00000,
            /// <summary>
            /// Relocations are performed when the object is loaded.
            /// </summary>
            NOW = 0x00001,
            /// <summary>
            /// All symbols are available for relocation processing of other modules.
            /// </summary>
            GLOBAL = 0x10000,
            /// <summary>
            /// All symbols are not made available for relocation processing by other modules.
            /// </summary>
            LOCAL = 0x00000
        }
    }
}
