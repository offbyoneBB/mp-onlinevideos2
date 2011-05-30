namespace Vlc.DotNet.Core
{
    ///<summary>
    /// CommonString class
    ///</summary>
    public static class CommonStrings
    {
        /// <summary>
        /// "C:\Program Files (x86)\VideoLAN\VLC\"
        /// </summary>
        public const string LIBVLC_DLLS_PATH_DEFAULT_VALUE_AMD64 = @"C:\Program Files (x86)\VideoLAN\VLC\";

        /// <summary>
        /// "C:\Program Files\VideoLAN\VLC\"
        /// </summary>
        public const string LIBVLC_DLLS_PATH_DEFAULT_VALUE_X86 = @"C:\Program Files\VideoLAN\VLC\";

        /// <summary>
        /// "C:\Program Files (x86)\VideoLAN\VLC\plugins\"
        /// </summary>
        public const string PLUGINS_PATH_DEFAULT_VALUE_AMD64 = @"C:\Program Files (x86)\VideoLAN\VLC\plugins\";

        /// <summary>
        /// "C:\Program Files\VideoLAN\VLC\plugins\"
        /// </summary>
        public const string PLUGINS_PATH_DEFAULT_VALUE_X86 = @"C:\Program Files\VideoLAN\VLC\plugins\";

        internal const string VLC_DOTNET_PROPERTIES_CATEGORY = "VideoLan DotNet";
    }
}