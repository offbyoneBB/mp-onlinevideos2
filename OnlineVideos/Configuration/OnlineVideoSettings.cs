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
    /// <summary>
    /// Singleton class holding all Settings that are used througout the OnlineVideos system.
    /// Make sure all fields are initialized before using any OnlineVideos functionality.
    /// </summary>
	public class OnlineVideoSettings : CrossDomainSingleton<OnlineVideoSettings>
    {
		protected bool SiteUtilsWereBuilt = false;

        public IUserStore UserStore;
        public IFavoritesDatabase FavDB;
        public ILog Logger;
		public ImageDownloader.ResizeOptions ThumbsResizeOptions { get; set; }
        public string ConfigDir;
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
        public Dictionary<string, Sites.SiteUtilBase> SiteUtilsList { get; protected set; }
		public List<Sites.LatestVideosSiteUtilBase> LatestVideosSiteUtilsList { get; protected set; }
        public SortedList<string, bool> VideoExtensions { get; protected set; }
        public bool FavoritesFirst = false;

        public String HttpPreferredNetworkInterface = OnlineVideoSettings.NetworkInterfaceSystemDefault;
        public int HttpOpenConnectionTimeout = 20000;           // ms
        public int HttpOpenConnectionSleepTime = 0;             // ms
        public int HttpTotalReopenConnectionTimeout = 60000;    // ms

        public String RtmpPreferredNetworkInterface = OnlineVideoSettings.NetworkInterfaceSystemDefault;
        public int RtmpOpenConnectionTimeout = 20000;           // ms
        public int RtmpOpenConnectionSleepTime = 0;             // ms
        public int RtmpTotalReopenConnectionTimeout = 60000;    // ms

        public String RtspPreferredNetworkInterface = OnlineVideoSettings.NetworkInterfaceSystemDefault;
        public int RtspOpenConnectionTimeout = 20000;           // ms
        public int RtspOpenConnectionSleepTime = 0;             // ms
        public int RtspTotalReopenConnectionTimeout = 60000;    // ms

        public int RtspClientPortMin = 50000;
        public int RtspClientPortMax = 65535;

        public String UdpRtpPreferredNetworkInterface = OnlineVideoSettings.NetworkInterfaceSystemDefault;
        public int UdpRtpOpenConnectionTimeout = 2000;          // ms
        public int UdpRtpOpenConnectionSleepTime = 0;           // ms
        public int UdpRtpTotalReopenConnectionTimeout = 60000;  // ms
        public int UdpRtpReceiveDataCheckInterval = 500;        // ms

        public static readonly String NetworkInterfaceSystemDefault = "System default";

		private OnlineVideoSettings()
		{
			// set some defaults
			Locale = CultureInfo.CurrentUICulture;
			SiteSettingsList = new BindingList<SiteSettings>();
			SiteUtilsList = new Dictionary<string, OnlineVideos.Sites.SiteUtilBase>();
			LatestVideosSiteUtilsList = new List<LatestVideosSiteUtilBase>();
			VideoExtensions = new SortedList<string, bool>();
			ThumbsResizeOptions = ImageDownloader.ResizeOptions.Default;
		}

		/// <summary>
		/// Drops the current single instance, creates a new Appdomain and copies all settings to a new instance in the new AppDomain.
		/// SiteUtil (and DLLs) are not loaded.
		/// </summary>
		public static void Reload()
		{
			// remember settings
			IUserStore __UserStore = Instance.UserStore;
			IFavoritesDatabase __FavDB = Instance.FavDB;
			ILog __Logger = Instance.Logger;
			ImageDownloader.ResizeOptions __ThumbsResizeOptions = Instance.ThumbsResizeOptions;
			string __ConfigDir = Instance.ConfigDir;
			string __ThumbsDir = Instance.ThumbsDir;
			string __DownloadDir = Instance.DownloadDir;
			string __DllsDir = Instance.DllsDir;
			string __UserAgent = Instance.UserAgent;
			string __SitesFileName = Instance.SitesFileName;
			bool __UseAgeConfirmation = Instance.UseAgeConfirmation;
			bool __AgeConfirmed = Instance.AgeConfirmed;
			int __CacheTimeout = Instance.CacheTimeout;
			int __UtilTimeout = Instance.UtilTimeout;
			int __DynamicCategoryTimeout = Instance.DynamicCategoryTimeout;
			CultureInfo __Locale = Instance.Locale;
			SortedList<string, bool> __VideoExtensions = Instance.VideoExtensions;
			bool __FavoritesFirst = Instance.FavoritesFirst;
			// reload domain and create new instance
			OnlineVideosAppDomain.Reload();
			var newInstance = Instance;
			// set remembered settings
			newInstance.UserStore = __UserStore;
			newInstance.FavDB = __FavDB;
			newInstance.Logger = __Logger;
			newInstance.ThumbsResizeOptions = __ThumbsResizeOptions;
			newInstance.ConfigDir = __ConfigDir;
			newInstance.ThumbsDir = __ThumbsDir;
			newInstance.DownloadDir = __DownloadDir;
			newInstance.DllsDir = __DllsDir;
			newInstance.UserAgent = __UserAgent;
			newInstance.SitesFileName = __SitesFileName;
			newInstance.UseAgeConfirmation = __UseAgeConfirmation;
			newInstance.AgeConfirmed = __AgeConfirmed;
			newInstance.CacheTimeout = __CacheTimeout;
			newInstance.UtilTimeout = __UtilTimeout;
			newInstance.DynamicCategoryTimeout = __DynamicCategoryTimeout;
			newInstance.Locale = __Locale;
			newInstance.VideoExtensions = __VideoExtensions;
			newInstance.FavoritesFirst = __FavoritesFirst;
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
                    Sites.SiteUtilBase siteutil = SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings);
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
                SiteSettings aSite = new SiteSettings()
                {
					Name = Translation.Instance.Favourites,
                    UtilName = "Favorite",
                    IsEnabled = true
                };
                SiteUtilsList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        void AddDownloadsSite()
        {
            if (!String.IsNullOrEmpty(DownloadDir))
            {
                //add a downloaded videos site
                SiteSettings aSite = new SiteSettings()
                {
					Name = Translation.Instance.DownloadedVideos,
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
                    new SerializableSettings() { Sites = SiteSettingsList }.Serialize(ms);
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
    }
}