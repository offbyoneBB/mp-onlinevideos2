using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interop;

namespace Vlc.DotNet.Forms
{
    //[TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public sealed class VlcManager : Component
    {
        private bool myIsDisposed;
        private IntPtr myVlcClient = IntPtr.Zero;

        public VlcManager()
        {
            PluginsPath = CommonStrings.PLUGINS_PATH_DEFAULT_VALUE;
            if (IsInstalled) PluginsPath = Path.Combine(vlcPath, "plugins");
            ScreenSaverEnabled = false;
            LogOptions = new VlcLogOptions();
            IgnoreConfig = true;
        }

        internal IntPtr VlcClient
        {
            get
            {
                if (myVlcClient == IntPtr.Zero && !myIsDisposed)
                {
                    InitVlcClient();
                }
                return myVlcClient;
            }
        }

        [DefaultValue(CommonStrings.PLUGINS_PATH_DEFAULT_VALUE)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        [Description("Modules search path")]
        public string PluginsPath { get; set; }

        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        [Description("Enable screensaver")]
        public bool ScreenSaverEnabled { get; set; }

        [DefaultValue(false)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public VlcLogOptions LogOptions { get; private set; }

        [DefaultValue(true)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        [Description("No configuration option will be loaded nor saved to config file")]
        public bool IgnoreConfig { get; set; }

        [DefaultValue(null)]
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string UserAgent { get; set; }

        [Browsable(false)]
        public string Version
        {
            get { return LibVlcMethods.libvlc_get_version(); }
        }

        [Browsable(false)]
        public string ChangeSet
        {
            get { return LibVlcMethods.libvlc_get_changeset(); }
        }

        [Browsable(false)]
        public string Compiler
        {
            get { return LibVlcMethods.libvlc_get_compiler(); }
        }


        ~VlcManager()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (myIsDisposed)
                return;
            if (disposing)
            {
            }
            if (myVlcClient != IntPtr.Zero)
            {
                LibVlcMethods.libvlc_release(myVlcClient);
                myVlcClient = IntPtr.Zero;
            }
            myIsDisposed = true;
        }

        static string vlcPath = null;
        public static bool IsInstalled
        {
            get
            {
                if (vlcPath == null)
                {
                    vlcPath = string.Empty;
                    Microsoft.Win32.RegistryKey regkeyVlcInstallPathKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC");
                    if (regkeyVlcInstallPathKey != null)
                    {
                        string sVlcPath = (string)regkeyVlcInstallPathKey.GetValue("InstallDir", "");
                        if (Directory.Exists(sVlcPath)) vlcPath = sVlcPath;
                    }
                }
                return !string.IsNullOrEmpty(vlcPath);
            }
        }

        private void InitVlcClient()
        {
            if (DesignMode || myVlcClient != IntPtr.Zero)
                return;

            if (!IsInstalled) return;

            var args = new StringCollection();
            args.Add("-I");
            args.Add("dummy");
            if (IgnoreConfig)
                args.Add("--ignore-config");
            if (LogOptions != null && LogOptions.Verbosity != VlcLogOptions.Verbosities.None)
            {
                if (LogOptions.ShowLoggerConsole)
                    args.Add("--extraintf=logger");
                args.Add("--verbose=" + (int) LogOptions.Verbosity);
                if (LogOptions.LogInFile)
                {
                    args.Add("--file-logging");
                    args.Add(@"--logfile=" + LogOptions.LogInFilePath);
                }
            }
            if (!string.IsNullOrEmpty(PluginsPath) && Directory.Exists(PluginsPath))
                args.Add("--plugin-path=" + PluginsPath);
            if (!ScreenSaverEnabled)
                args.Add(":disable-screensaver");
            if (!string.IsNullOrEmpty(UserAgent))
                args.Add("--user-agent=" + UserAgent);

            args.Add("--no-video-title-show");

            args.Add("--http-caching=" + OnlineVideos.MediaPortal1.PluginConfiguration.Instance.wmpbuffer);

            var argsArray = new string[args.Count];
            args.CopyTo(argsArray, 0);

            //save the original EnviornmentDirectory and Set the directory to load the COM object libvlc.dll
            string originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = vlcPath;

            myVlcClient = LibVlcMethods.libvlc_new(args.Count, argsArray);

            //restore the original EnviornmentDirectory.
            Environment.CurrentDirectory = originalDir;
        }

        public override string ToString()
        {
            return "(VlcManager)";
        }
    }
}