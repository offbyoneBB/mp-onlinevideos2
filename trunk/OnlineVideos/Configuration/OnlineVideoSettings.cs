using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    /// <summary>
    /// Singleton class holding all Settings that are used througout the OnlineVideos system.
    /// Make sure all fields are initialized before using any OnlineVideos functionality.
    /// </summary>
    public class OnlineVideoSettings
    {
        public IUserStore UserStore;
        public IFavoritesDatabase FavDB;
        public ILog Logger;
        public ImageDownloader.ResizeOptions ThumbsResizeOptions;
        public string ConfigDir;
        public string ThumbsDir;        
        public string DownloadDir;
        public string DllsDir;
        public string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; de; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3";
        public string SitesFileName = "OnlineVideoSites.xml";
        public bool UseAgeConfirmation = true; // enable pin by default -> child protection
        public bool AgeConfirmed = false;
        public int CacheTimeout = 30; // minutes
        public int UtilTimeout = 15;  // seconds
        public CultureInfo Locale;
        public BindingList<SiteSettings> SiteSettingsList { get; protected set; }
        public Dictionary<string, Sites.SiteUtilBase> SiteUtilsList { get; protected set; }
        public SortedList<string, bool> VideoExtensions { get; protected set; }

        #region Singleton
        private static OnlineVideoSettings _Instance = null;
        public static OnlineVideoSettings Instance
        {
            get
            {
                if (_Instance == null) _Instance = new OnlineVideoSettings();
                return _Instance;
            }
        }
        #endregion

        private OnlineVideoSettings()
        {
            // set some defaults
            Locale = CultureInfo.CurrentUICulture;
            SiteSettingsList = new BindingList<SiteSettings>();
            SiteUtilsList = new Dictionary<string, OnlineVideos.Sites.SiteUtilBase>();
            VideoExtensions = new SortedList<string, bool>();
            ThumbsResizeOptions = ImageDownloader.ResizeOptions.Default;
        }

        public void LoadSites()
        {
            // create the configured directories
            string iconDir = string.IsNullOrEmpty(ThumbsDir) ? string.Empty : Path.Combine(ThumbsDir, @"Icons\");
            if (!string.IsNullOrEmpty(iconDir) && !Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
            string bannerDir = string.IsNullOrEmpty(ThumbsDir) ? string.Empty : Path.Combine(ThumbsDir, @"Banners\");
            if (!string.IsNullOrEmpty(bannerDir) && !Directory.Exists(bannerDir)) Directory.CreateDirectory(bannerDir);
            try { if (!string.IsNullOrEmpty(DllsDir) && !Directory.Exists(DllsDir)) Directory.CreateDirectory(DllsDir); }
            catch { /* might fail due to UAC */ }

            string filename = string.IsNullOrEmpty(ConfigDir) ? string.Empty : Path.Combine(ConfigDir, SitesFileName);
            Stream sitesStream = null;
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            {
                Log.Info("ConfigFile \"{0}\" was not found. Using embedded resource.", filename);
                sitesStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("OnlineVideos.OnlineVideoSites.xml");
            }
            else
            {
                sitesStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            }
            using (sitesStream)
            {
                SiteSettingsList = (BindingList<SiteSettings>)Utils.SiteSettingsFromXml(new StreamReader(sitesStream));
            }
        }

        void LoadScriptSites()
        {
            Log.Info("Loading script files");
            if (!string.IsNullOrEmpty(ConfigDir) && Directory.Exists(Path.Combine(ConfigDir, "scripts\\OnlineVideos")))
            {
                FileInfo[] fileInfos = new DirectoryInfo(Path.Combine(ConfigDir, "scripts\\OnlineVideos")).GetFiles("*.xml");
                foreach (var fileInfo in fileInfos)
                {
                    Log.Info("Script loaded for {0}", fileInfo.FullName);
                    ScriptUtil scriptUtil = new ScriptUtil();
                    scriptUtil.ScriptFile = fileInfo.FullName;
                    scriptUtil.Initialize(new SiteSettings());
                    SiteUtilsList.Add(scriptUtil.Settings.Name, scriptUtil);
                }
            }
        }

        public void BuildSiteUtilsList()
        {
            SiteUtilsList.Clear();
            foreach (SiteSettings siteSettings in SiteSettingsList)
            {
                // only need enabled sites
                if (siteSettings.IsEnabled)
                {
                    Sites.SiteUtilBase siteutil = SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);
                    if (siteutil != null && !SiteUtilsList.ContainsKey(siteSettings.Name)) SiteUtilsList.Add(siteSettings.Name, siteutil);
                }
            }

            LoadScriptSites();

            if (FavDB != null)
            {
                //create a favorites site
                SiteSettings aSite = new SiteSettings()
                {
                    Name = Translation.Favourites,
                    UtilName = "Favorite",
                    IsEnabled = true
                };
                SiteUtilsList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
            }

            if (!String.IsNullOrEmpty(DownloadDir))
            {                
                //add a downloaded videos site
                SiteSettings aSite = new SiteSettings()
                {
                    Name = Translation.DownloadedVideos,
                    UtilName = "DownloadedVideo",
                    IsEnabled = true
                };
                SiteUtilsList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        public void SaveSites()
        {
            // only save if there are sites - otherwise an error might have occured on LoadSites
            if (SiteSettingsList != null && SiteSettingsList.Count > 0 && !string.IsNullOrEmpty(ConfigDir))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Utils.SiteSettingsToXml(new SerializableSettings() { Sites = SiteSettingsList }, ms);
                    if (ms.Length > 0)
                    {
                        ms.Position = 0;
                        using (FileStream fso = new FileStream(Path.Combine(ConfigDir, SitesFileName), FileMode.Create))
                        {
                            ms.WriteTo(fso);
                        }
                    }
                }
            }
        }
    }
}