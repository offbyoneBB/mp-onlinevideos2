using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct libvlc_log_message_t
    {
        public uint sizeof_msg;
        public int i_severity;
        [MarshalAs(UnmanagedType.LPStr)] public string psz_type;
        [MarshalAs(UnmanagedType.LPStr)] public string psz_name;
        [MarshalAs(UnmanagedType.LPStr)] public string psz_header;
        [MarshalAs(UnmanagedType.LPStr)] public string psz_message;
    }
}