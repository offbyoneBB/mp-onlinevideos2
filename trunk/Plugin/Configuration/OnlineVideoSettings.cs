using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    /// <summary>
    /// Description of OnlineVideoSettings.
    /// </summary>
    public class OnlineVideoSettings
    {
        public const string USERAGENT = "Mozilla/5.0 (Windows; U; Windows NT 6.1; de; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3";
        public const string SETTINGS_FILE = "OnlineVideoSites.xml";

        public IUserStore UserStore;
        public IFavoritesDatabase FavDB;
        public ILog Logger;
        public string ConfigDir;
        public string ThumbsDir;        
        public string DownloadDir;
        public string DllsDir;

        public int ThumbsAge = 100; // days
        public bool useAgeConfirmation = true; // enable pin by default -> child protection
        public bool ageHasBeenConfirmed = false;
        public int CacheTimeout = 30; // minutes

        public CultureInfo Locale { get; set; }
        public BindingList<SiteSettings> SiteSettingsList { get; protected set; }
        public Dictionary<string, Sites.SiteUtilBase> SiteList { get; protected set; }
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
            SiteList = new Dictionary<string, OnlineVideos.Sites.SiteUtilBase>();
            VideoExtensions = new SortedList<string, bool>();
        }

        public void LoadSites()
        {
            // create the configured directories
            string iconDir = Path.Combine(ThumbsDir, @"Icons\");
            if (!Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
            string bannerDir = Path.Combine(ThumbsDir, @"Banners\");
            if (!Directory.Exists(bannerDir)) Directory.CreateDirectory(bannerDir);
            try { if (!Directory.Exists(DllsDir)) Directory.CreateDirectory(DllsDir); }
            catch { /* might fail due to UAC */ }

            string filename = Path.Combine(ConfigDir, SETTINGS_FILE);
            Stream sitesStream = null;
            if (!File.Exists(filename))
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
            if (Directory.Exists(Path.Combine(ConfigDir, "scripts\\OnlineVideos")))
            {
                FileInfo[] fileInfos = new DirectoryInfo(Path.Combine(ConfigDir, "scripts\\OnlineVideos")).GetFiles("*.xml");
                foreach (var fileInfo in fileInfos)
                {
                    Log.Info("Script loaded for {0}", fileInfo.FullName);
                    ScriptUtil scriptUtil = new ScriptUtil();
                    scriptUtil.ScriptFile = fileInfo.FullName;
                    scriptUtil.Initialize(new SiteSettings());
                    SiteList.Add(scriptUtil.Settings.Name, scriptUtil);
                }
            }
        }

        public void BuildSiteList()
        {
            SiteList.Clear();
            foreach (SiteSettings siteSettings in SiteSettingsList)
            {
                // only need enabled sites
                if (siteSettings.IsEnabled)
                {
                    Sites.SiteUtilBase siteutil = SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);
                    if (siteutil != null && !SiteList.ContainsKey(siteSettings.Name)) SiteList.Add(siteSettings.Name, siteutil);
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
                SiteList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
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
                SiteList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        public void SaveSites()
        {
            // only save if there are sites - otherwise an error might have occured on LoadSites
            if (SiteSettingsList != null && SiteSettingsList.Count > 0)
            {
                using (FileStream fso = new FileStream(Path.Combine(ConfigDir, SETTINGS_FILE), FileMode.Create))
                {
                    Utils.SiteSettingsToXml(new SerializableSettings() { Sites = SiteSettingsList }, fso);
                }
            }
        }
    }
}