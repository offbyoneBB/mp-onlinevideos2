using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using OnlineVideos.CrossDomain;

namespace OnlineVideos.MediaPortal2.Models
{
    static class ConfigurationHelper
    {
        private static bool _isInitialized;
        private static readonly object _syncObj = new object();

        public static void Init()
        {
            InitEnvironment();
            BuildSiteList();
        }

        public static void InitEnvironment()
        {
            lock (_syncObj)
            {
                if (_isInitialized)
                    return;
                _isInitialized = true;
            }
            try
            {
                // The AppDomain might be initialized before, in this case this command will throw
                OnlineVideosAppDomain.UseSeperateDomain = true;
            }
            catch { }

            ServiceRegistration.Get<ISettingsManager>().Load<Configuration.Settings>().SetValuesToApi();
            string ovConfigPath = ServiceRegistration.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}\", Environment.UserName));
            string ovDataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OnlineVideos");

            OnlineVideoSettings.Instance.Logger = new LogDelegator();
            OnlineVideoSettings.Instance.UserStore = new Configuration.UserSiteSettingsStore();

            OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(ovDataPath, "SiteUtils");
            OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(ovDataPath, "Thumbs");
            OnlineVideoSettings.Instance.ConfigDir = ovConfigPath;

            OnlineVideoSettings.Instance.AddSupportedVideoExtensions(new List<string> { ".asf", ".asx", ".flv", ".m4v", ".mov", ".mkv", ".mp4", ".wmv" });

            // clear cache files that might be left over from an application crash
            MPUrlSourceFilter.Downloader.ClearDownloadCache();
            // load translation strings in other AppDomain, so SiteUtils can use localized language strings
            TranslationLoader.LoadTranslations(ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(ConfigurationHelper).Assembly.Location), "Language"), "en", "strings_{0}.xml");
            // The default connection limit is 2 in .Net on most platforms! This means downloading two files will block all other WebRequests.
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            // The default .Net implementation for URI parsing removes trailing dots, which is not correct
            Helpers.DotNetFrameworkHelper.FixUriTrailingDots();

            // load the xml that holds all configured sites
            OnlineVideoSettings.Instance.LoadSites();
        }

        public static void BuildSiteList()
        {
            if (OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
                return;

            // show the busy indicator, because loading site dlls takes some seconds
            ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
            try
            {
                OnlineVideoSettings.Instance.BuildSiteUtilsList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
            }
        }
    }
}
