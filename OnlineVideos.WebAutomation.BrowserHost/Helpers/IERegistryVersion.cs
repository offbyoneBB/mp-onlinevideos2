using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    /// <summary>
    /// To make sure this work in IE 11 we need to set this web browser version to use IE10 rendering and turn off (or configure) ActiveX filtering
    /// This is specifically for loading silverlight for Sky Go
    /// </summary>
    internal static class IERegistryVersion
    {
        private const string BrowserKeyPath = @"\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION";

        /// <summary>
        /// Set a flag in the registry to make this browser run as if it was IE 10
        /// Value reference: http://msdn.microsoft.com/en-us/library/ee330730%28v=VS.85%29.aspx
        /// IDOC Reference:  http://msdn.microsoft.com/en-us/library/ms535242%28v=vs.85%29.aspx
        /// </summary>
        /// <param name="applicationName"></param>
        internal static void SetIEVersion()
        {
            var baseKey = Registry.CurrentUser.ToString();
            var ieVersion = 10000;
            Registry.SetValue(baseKey + BrowserKeyPath,
                                           Process.GetCurrentProcess().ProcessName + ".exe",
                                           ieVersion,
                                           RegistryValueKind.DWord);

        }

        /// <summary>
        /// Clean up (if needs be)
        /// </summary>
        internal static void RemoveIEVersion()
        {
            var key = Registry.CurrentUser.OpenSubKey(BrowserKeyPath.Substring(1), true);
            key.DeleteValue(Process.GetCurrentProcess().ProcessName + ".exe", false);
        }
    }
}