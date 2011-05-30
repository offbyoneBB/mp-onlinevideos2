using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LogMessage
        {
            public int i_severity;
            public IntPtr psz_type;
            public IntPtr psz_name;
            public IntPtr psz_header;
            public IntPtr psz_message;
        }
	}
}
