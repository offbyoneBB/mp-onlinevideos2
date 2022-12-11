using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using OnlineVideos.CrossDomain;
using OnlineVideos.Downloading;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    public delegate string GetConfigDirDelegate();
    public sealed class DelegateWrapper : MarshalByRefObject
    {
        public string Invoke()
        {
            return _delegate();
        }

        private readonly GetConfigDirDelegate _delegate;

        public DelegateWrapper(GetConfigDirDelegate dlgt)
        {
            _delegate = dlgt;
        }
    }
    /// <summary>
    /// Singleton class holding all Settings that are used througout the OnlineVideos system.
    /// Make sure all fields are initialized before using any OnlineVideos functionality.
    /// </summary>
    public class OnlineVideoSettings : CrossDomainSingleton<OnlineVideoSettings>
    {
        private string _configDir = null;
        protected bool SiteUtilsWereBuilt = false;

        public IUserStore UserStore;
        public IFavoritesDatabase FavDB;
        public ILog Logger;
        public ImageDownloader.ResizeOptions ThumbsResizeOptions { get; set; }
        // Gets or sets a delegate which can return a dynamic config dir.
        // This is required for MP2.2+ user management, where the user profiles can be changed at runtime.
        public DelegateWrapper GetConfigDir { get; set; }

        public string ConfigDir
        {
            get { return GetConfigDir != null ? GetConfigDir.Invoke() : _configDir; }
            set { _configDir = value; }
        }

        public string ThumbsDir;
        public string DownloadDir;
        public string DllsDir;
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.1)";
        public string SitesFileName = "OnlineVideoSites.xml";
        public bool UseAgeConfirmation = true; // enable pin by default -> child protection
        public bool AgeConfirmed = false;
        public int CacheTimeout = 30; // minutes
        public int UtilTimeout = 15;  // seconds
        public int DynamicCategoryTimeout = 300; // minutes
        public CultureInfo Locale;
        public BindingList<SiteSettings> SiteSettingsList { get; protected set; }
        public Dictionary<string, SiteUtilBase> SiteUtilsList { get; protected set; }
        public List<LatestVideosSiteUtilBase> LatestVideosSiteUtilsList { get; protected set; }
        public SortedList<string, bool> VideoExtensions { get; protected set; }
        public bool FavoritesFirst = false;

        public String HttpPreferredNetworkInterface = NetworkInterfaceSystemDefault;
        public int HttpOpenConnectionTimeout = MPUrlSourceFilter.HttpUrl.DefaultHttpOpenConnectionTimeout;                     // ms
        public int HttpOpenConnectionSleepTime = MPUrlSourceFilter.HttpUrl.DefaultHttpOpenConnectionSleepTime;                 // ms
        public int HttpTotalReopenConnectionTimeout = MPUrlSourceFilter.HttpUrl.DefaultHttpTotalReopenConnectionTimeout;       // ms

        public Boolean HttpServerAuthenticate = MPUrlSourceFilter.HttpUrl.DefaultHttpServerAuthenticate;
        public String HttpServerUserName = MPUrlSourceFilter.HttpUrl.DefaultHttpServerUserName;
        public String HttpServerPassword = MPUrlSourceFilter.HttpUrl.DefaultHttpServerPassword;

        public Boolean HttpProxyServerAuthenticate = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServerAuthenticate;
        public String HttpProxyServer = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServer;
        public int HttpProxyServerPort = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServerPort;
        public String HttpProxyServerUserName = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServerUserName;
        public String HttpProxyServerPassword = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServerPassword;
        public MPUrlSourceFilter.ProxyServerType HttpProxyServerType = MPUrlSourceFilter.HttpUrl.DefaultHttpProxyServerType;

        public String RtmpPreferredNetworkInterface = NetworkInterfaceSystemDefault;
        public int RtmpOpenConnectionTimeout = MPUrlSourceFilter.RtmpUrl.DefaultRtmpOpenConnectionTimeout;                     // ms
        public int RtmpOpenConnectionSleepTime = MPUrlSourceFilter.RtmpUrl.DefaultRtmpOpenConnectionSleepTime;                 // ms
        public int RtmpTotalReopenConnectionTimeout = MPUrlSourceFilter.RtmpUrl.DefaultRtmpTotalReopenConnectionTimeout;       // ms

        public String RtspPreferredNetworkInterface = NetworkInterfaceSystemDefault;
        public int RtspOpenConnectionTimeout = MPUrlSourceFilter.RtspUrl.DefaultRtspOpenConnectionTimeout;                     // ms
        public int RtspOpenConnectionSleepTime = MPUrlSourceFilter.RtspUrl.DefaultRtspOpenConnectionSleepTime;                 // ms
        public int RtspTotalReopenConnectionTimeout = MPUrlSourceFilter.RtspUrl.DefaultRtspTotalReopenConnectionTimeout;       // ms

        public int RtspClientPortMin = MPUrlSourceFilter.RtspUrl.DefaultRtspClientPortMin;
        public int RtspClientPortMax = MPUrlSourceFilter.RtspUrl.DefaultRtspClientPortMax;

        public String UdpRtpPreferredNetworkInterface = NetworkInterfaceSystemDefault;
        public int UdpRtpOpenConnectionTimeout = MPUrlSourceFilter.UdpRtpUrl.DefaultUdpOpenConnectionTimeout;                  // ms
        public int UdpRtpOpenConnectionSleepTime = MPUrlSourceFilter.UdpRtpUrl.DefaultUdpOpenConnectionSleepTime;              // ms
        public int UdpRtpTotalReopenConnectionTimeout = MPUrlSourceFilter.UdpRtpUrl.DefaultUdpTotalReopenConnectionTimeout;    // ms
        public int UdpRtpReceiveDataCheckInterval = MPUrlSourceFilter.UdpRtpUrl.DefaultUdpReceiveDataCheckInterval;            // ms

        public static readonly String NetworkInterfaceSystemDefault = "System default";

        private OnlineVideoSettings()
        {
            // set some defaults
            Locale = CultureInfo.CurrentUICulture;
            SiteSettingsList = new BindingList<SiteSettings>();
            SiteUtilsList = new Dictionary<string, SiteUtilBase>();
            LatestVideosSiteUtilsList = new List<LatestVideosSiteUtilBase>();
            VideoExtensions = new SortedList<string, bool>();
            ThumbsResizeOptions = ImageDownloader.ResizeOptions.Default;
        }

        /// <summary>
        /// Unloads the OnlineVideos app domain and frees all resources.
        /// </summary>
        public static void Unload()
        {
            OnlineVideosAssemblyContext.Unload();
        }
        /// <summary>
        /// Drops the current single instance, creates a new Appdomain and copies all settings to a new instance in the new AppDomain.
        /// SiteUtil (and DLLs) are not loaded.
        /// </summary>
        public static void Reload()
        {
            // remember settings
            IUserStore userStore = Instance.UserStore;
            IFavoritesDatabase favDb = Instance.FavDB;
            ILog logger = Instance.Logger;
            ImageDownloader.ResizeOptions thumbsResizeOptions = Instance.ThumbsResizeOptions;
            DelegateWrapper getConfigDirDelegateconfigDir = Instance.GetConfigDir;
            string configDir = Instance.ConfigDir;
            string thumbsDir = Instance.ThumbsDir;
            string downloadDir = Instance.DownloadDir;
            string dllsDir = Instance.DllsDir;
            string userAgent = Instance.UserAgent;
            string sitesFileName = Instance.SitesFileName;
            bool useAgeConfirmation = Instance.UseAgeConfirmation;
            bool ageConfirmed = Instance.AgeConfirmed;
            int cacheTimeout = Instance.CacheTimeout;
            int utilTimeout = Instance.UtilTimeout;
            int dynamicCategoryTimeout = Instance.DynamicCategoryTimeout;
            CultureInfo locale = Instance.Locale;
            SortedList<string, bool> videoExtensions = Instance.VideoExtensions;
            bool favoritesFirst = Instance.FavoritesFirst;
            // reload domain and create new instance
            OnlineVideosAssemblyContext.Reload();
            var newInstance = Instance;
            // set remembered settings
            newInstance.UserStore = userStore;
            newInstance.FavDB = favDb;
            newInstance.Logger = logger;
            newInstance.ThumbsResizeOptions = thumbsResizeOptions;
            newInstance.GetConfigDir = getConfigDirDelegateconfigDir;
            newInstance.ConfigDir = configDir;
            newInstance.ThumbsDir = thumbsDir;
            newInstance.DownloadDir = downloadDir;
            newInstance.DllsDir = dllsDir;
            newInstance.UserAgent = userAgent;
            newInstance.SitesFileName = sitesFileName;
            newInstance.UseAgeConfirmation = useAgeConfirmation;
            newInstance.AgeConfirmed = ageConfirmed;
            newInstance.CacheTimeout = cacheTimeout;
            newInstance.UtilTimeout = utilTimeout;
            newInstance.DynamicCategoryTimeout = dynamicCategoryTimeout;
            newInstance.Locale = locale;
            newInstance.VideoExtensions = videoExtensions;
            newInstance.FavoritesFirst = favoritesFirst;
            // load Sites Xml
            newInstance.LoadSites();
        }

        public void LoadSites()
        {
            // create the configured directories
            string iconDir = string.IsNullOrEmpty(ThumbsDir) ? string.Empty : Path.Combine(ThumbsDir, @"Icons\");
            if (!string.IsNullOrEmpty(iconDir) && !Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
            string bannerDir = string.IsNullOrEmpty(ThumbsDir) ? string.Empty : Path.Combine(ThumbsDir, @"Banners\");
            if (!string.IsNullOrEmpty(bannerDir) && !Directory.Exists(bannerDir)) Directory.CreateDirectory(bannerDir);
            string cacheDir = string.IsNullOrEmpty(ThumbsDir) ? string.Empty : Path.Combine(ThumbsDir, @"Cache\");
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
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
                SiteSettingsList = (BindingList<SiteSettings>)SerializableSettings.Deserialize(new StreamReader(sitesStream));
            }
            Log.Info("Loaded {0} sites from {1}", SiteSettingsList.Count, SitesFileName);
        }

        public void BuildSiteUtilsList()
        {
            SiteUtilsList.Clear();
            LatestVideosSiteUtilsList.Clear();

            if (FavoritesFirst)
            {
                AddFavoritesSite();
                AddDownloadsSite();
            }

            foreach (SiteSettings siteSettings in SiteSettingsList)
            {
                // only need enabled sites
                if (siteSettings.IsEnabled)
                {
                    SiteUtilBase siteutil = SiteUtilFactory.Instance.CreateFromShortName(siteSettings.UtilName, siteSettings);
                    if (siteutil != null && !SiteUtilsList.ContainsKey(siteSettings.Name)) SiteUtilsList.Add(siteSettings.Name, siteutil);
                    if (siteutil is LatestVideosSiteUtilBase) LatestVideosSiteUtilsList.Add(siteutil as LatestVideosSiteUtilBase);
                }
            }

            if (!FavoritesFirst)
            {
                AddFavoritesSite();
                AddDownloadsSite();
            }

            SiteUtilsWereBuilt = true;
            Log.Info("Created {0} SiteUtils", SiteUtilsList.Count);
        }

        public bool IsSiteUtilsListBuilt()
        {
            return SiteUtilsWereBuilt;
        }

        void AddFavoritesSite()
        {
            if (FavDB != null)
            {
                //create a favorites site
                SiteSettings aSite = new SiteSettings
                {
                    Name = Translation.Instance.Favourites,
                    UtilName = "Favorite",
                    IsEnabled = true
                };
                SiteUtilsList.Add(aSite.Name, SiteUtilFactory.Instance.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        void AddDownloadsSite()
        {
            if (!String.IsNullOrEmpty(DownloadDir))
            {
                //add a downloaded videos site
                SiteSettings aSite = new SiteSettings
                {
                    Name = Translation.Instance.DownloadedVideos,
                    UtilName = "DownloadedVideo",
                    IsEnabled = true
                };
                SiteUtilsList.Add(aSite.Name, SiteUtilFactory.Instance.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        public void SaveSites()
        {
            // only save if there are sites - otherwise an error might have occured on LoadSites
            if (SiteSettingsList != null && SiteSettingsList.Count > 0 && !string.IsNullOrEmpty(ConfigDir))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    new SerializableSettings { Sites = SiteSettingsList }.Serialize(ms);
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

        public void AddSupportedVideoExtensions(IList<string> extensions)
        {
            foreach (string anExt in extensions)
            {
                if (!VideoExtensions.ContainsKey(anExt.ToLower().Trim())) VideoExtensions.Add(anExt.ToLower().Trim(), true);
            }
        }

        public void AddSite(SiteSettings settings)
        {
            SiteSettingsList.Add(settings);
        }

        public bool RemoveSite(string name)
        {
            for (int i = 0; i < SiteSettingsList.Count; i++)
            {
                if (SiteSettingsList[i].Name == name)
                {
                    SiteSettingsList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void RemoveSiteAt(int index)
        {
            SiteSettingsList.RemoveAt(index);
        }

        public int GetSiteByName(string name, out SiteSettings site)
        {
            site = null;
            for (int i = 0; i < SiteSettingsList.Count; i++)
            {
                if (SiteSettingsList[i].Name == name)
                {
                    site = SiteSettingsList[i];
                    return i;
                }
            }
            return -1;
        }

        public void SetSiteAt(int index, SiteSettings settings)
        {
            SiteSettingsList[index] = settings;
        }

        public void SetSite(string name, SiteUtilBase site)
        {
            SiteUtilsList[name] = site;
        }
    }
}
