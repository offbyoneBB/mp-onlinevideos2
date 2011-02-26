using System.ComponentModel;
using Vlc.DotNet.Core;

namespace Vlc.DotNet.Forms
{
    [TypeConverter(typeof (ExpandableObjectConverter))]
    public sealed class VlcLogOptions
    {
        #region Verbosity enum

        public enum Verbosities
        {
            None = -1,
            Standard = 0,
            Warnings = 1,
            Debug = 2
        }

        #endregion

        internal VlcLogOptions()
        {
            Verbosity = Verbosities.None;
            LogInFile = false;
            LogInFilePath = "vlc_dotnet.log";
            ShowLoggerConsole = false;
        }

        [DefaultValue(Verbosities.None)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public Verbosities Verbosity { get; set; }

        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool LogInFile { get; set; }

        [DefaultValue("vlc_dotnet.log")]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string LogInFilePath { get; set; }

        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool ShowLoggerConsole { get; set; }

        public override string ToString()
        {
            return "(VlcLog)";
        }
    }
}