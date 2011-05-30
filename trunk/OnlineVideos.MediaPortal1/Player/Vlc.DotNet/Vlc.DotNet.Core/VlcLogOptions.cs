using System.ComponentModel;

namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcLogOptions class
    /// </summary>
    public sealed class VlcLogOptions
    {
        internal VlcLogOptions()
        {
            Verbosity = VlcLogVerbosities.None;
            LogInFile = false;
            LogInFilePath = "vlc-log.txt";
            ShowLoggerConsole = false;
        }

        /// <summary>
        /// Get / Set the verbosity of log
        /// </summary>
        [DefaultValue(VlcLogVerbosities.None)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcLogVerbosities Verbosity { get; set; }

        /// <summary>
        /// Get / Set enable logging in file
        /// </summary>
        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool LogInFile { get; set; }

        /// <summary>
        /// Get / Set the file to log in
        /// </summary>
        [DefaultValue("vlc-log.txt")]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string LogInFilePath { get; set; }

        /// <summary>
        /// Get / Set enable the show of console
        /// </summary>
        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool ShowLoggerConsole { get; set; }
    }
}