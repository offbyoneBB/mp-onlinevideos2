using System;
using MediaPortal.Configuration;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using MediaPortal.Util;
using System.Runtime.Serialization;
using System.Xml;
using System.ComponentModel;

namespace OnlineVideos.MediaPortal1
{
    public class PluginConfiguration
    {
        public const string PLUGIN_NAME = "OnlineVideos";

        public const string SitesGroupsFileName = "OnlineVideoGroups.xml";

        public enum SearchHistoryType { Off = 0, Simple = 1, Extended = 2 }
        public enum SiteOrder { AsInFile = 0, Name = 1, Language = 2 }

        public string BasicHomeScreenName = "Online Videos";
        public int wmpbuffer = 5000;  // milliseconds
        public int playbuffer = 2;   // percent        
        public int ThumbsAge = 100; // days
        public bool useQuickSelect = false;
        public string[] FilterArray;
        public string pinAgeConfirmation = "";
        public bool? updateOnStart = null;
        public string email = "";
        public string password = "";
        public SearchHistoryType searchHistoryType = SearchHistoryType.Simple;
        public int searchHistoryNum = 9;
        public BindingList<SitesGroup> SitesGroups = new BindingList<SitesGroup>();
        public BindingList<SitesGroup> CachedAutomaticSitesGroups = new BindingList<SitesGroup>();
        public bool autoGroupByLang = true;
        public DateTime lastFirstRun;
        public uint updatePeriod = 4;
		public bool LatestVideosRandomize = true;
		public uint LatestVideosMaxItems = 3;
		public uint LatestVideosOnlineDataRefresh = 30; // minutes
		public uint LatestVideosGuiDataRefresh = 30; // seconds
        public bool AllowRefreshRateChange = false;
		public bool StoreLayoutPerCategory = true;

        // runtime (while inside MediaPortal OnlineVideos) changeable values
        public Dictionary<string, List<string>> searchHistory;
        public SiteOrder siteOrder = SiteOrder.AsInFile;
        public GUIFacadeControl.Layout currentGroupView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentSiteView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentCategoryView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentVideoView = GUIFacadeControl.Layout.SmallIcons;

        #region MediaPortal.xml attribute names
        public const string CFG_SECTION = "onlinevideos";
        public const string CFG_BASICHOMESCREEN_NAME = "basicHomeScreenName";
        const string CFG_GROUPVIEW_MODE = "groupview";
        const string CFG_SITEVIEW_MODE = "siteview";
        const string CFG_SITEVIEW_ORDER = "siteview_order";
        const string CFG_CATEGORYVIEW_MODE = "categoryview";
        const string CFG_VIDEOVIEW_MODE = "videoview";
        const string CFG_UPDATEONSTART = "updateOnStart";
        const string CFG_THUMBNAIL_DIR = "thumbDir";
        const string CFG_THUMBNAIL_AGE = "thumbAge";
        const string CFG_DOWNLOAD_DIR = "downloadDir";
        const string CFG_FILTER = "filter";
        const string CFG_USE_QUICKSELECT = "useQuickSelect";
        const string CFG_USE_AGECONFIRMATION = "useAgeConfirmation";
        const string CFG_PIN_AGECONFIRMATION = "pinAgeConfirmation";
        const string CFG_CACHE_TIMEOUT = "cacheTimeout";
        const string CFG_UTIL_TIMEOUT = "utilTimeout";
		const string CFG_CATEGORYDISCOVERED_TIMEOUT = "categoryDiscoveryTimeout";
        const string CFG_WMP_BUFFER = "wmpbuffer";
        const string CFG_PLAY_BUFFER = "playbuffer";
        const string CFG_EMAIL = "email";
        const string CFG_PASSWORD = "password";
        const string CFG_HTTP_SOURCE_FILTER = "httpsourcefilter";
        const string CFG_SEARCHHISTORY_ENABLED = "searchHistoryEnabled";
        const string CFG_SEARCHHISTORY_NUM = "searchHistoryNum";
        const string CFG_SEARCHHISTORY = "searchHistory";
        const string CFG_SEARCHHISTORYTYPE = "searchHistoryType";
        const string CFG_USE_RTMP_PROXY = "useRtmpProxy";
        const string CFG_AUTO_LANG_GROUPS = "autoGroupByLang";
        const string CFG_LAST_FIRSTRUN = "lastFirstRun";
        const string CFG_UPDATEPERIOD = "updatePeriod";
        const string CFG_FAVORITES_FIRST = "favoritesFirst";
		const string CFG_LATESTVIDEOS_RANDOMIZE = "latestVideosRandomize";
		const string CFG_LATESTVIDEOS_MAXITEMS = "latestVideosMaxItems";
		const string CFG_LATESTVIDEOS_ONLINEDATA_REFRESH = "latestVideosOnlineDataRefresh";
		const string CFG_LATESTVIDEOS_GUIDATA_REFRESH = "latestVideosGuiDataRefresh";
        const string CFG_ALLOW_REFRESHRATE_CHANGE = "allowRefreshRateChange";
		const string CFG_STORE_LAYOUT_PER_CATEGORY = "storeLayoutPerCategory";

        // filter V2
        const string CFG_FILTER_V2_VIEW_MODE = "filterv2";

        const string CFG_FILTER_V2_HTTP_PREFERRED_NETWORK_INTERFACE = "filterv2httpinterface";
        const string CFG_FILTER_V2_HTTP_OPEN_CONNECTION_TIMEOUT = "filterv2httpopenconnectiontimeout";
        const string CFG_FILTER_V2_HTTP_OPEN_CONNECTION_SLEEP_TIME = "filterv2httpopenconnectionsleeptime";
        const string CFG_FILTER_V2_HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT = "filterv2httptotalreopenconnectiontimeout";

        const string CFG_FILTER_V2_RTMP_PREFERRED_NETWORK_INTERFACE = "filterv2rtmpinterface";
        const string CFG_FILTER_V2_RTMP_OPEN_CONNECTION_TIMEOUT = "filterv2rtmpopenconnectiontimeout";
        const string CFG_FILTER_V2_RTMP_OPEN_CONNECTION_SLEEP_TIME = "filterv2rtmpopenconnectionsleeptime";
        const string CFG_FILTER_V2_RTMP_TOTAL_REOPEN_CONNECTION_TIMEOUT = "filterv2rtmptotalreopenconnectiontimeout";

        const string CFG_FILTER_V2_RTSP_PREFERRED_NETWORK_INTERFACE = "filterv2rtspinterface";
        const string CFG_FILTER_V2_RTSP_OPEN_CONNECTION_TIMEOUT = "filterv2rtspopenconnectiontimeout";
        const string CFG_FILTER_V2_RTSP_OPEN_CONNECTION_SLEEP_TIME = "filterv2rtspopenconnectionsleeptime";
        const string CFG_FILTER_V2_RTSP_TOTAL_REOPEN_CONNECTION_TIMEOUT = "filterv2rtsptotalreopenconnectiontimeout";

        const string CFG_FILTER_V2_RTSP_CLIENT_PORT_MIN = "filterv2rtspclientportmin";
        const string CFG_FILTER_V2_RTSP_CLIENT_PORT_MAX = "filterv2rtspclientportmax";

        const string CFG_FILTER_V2_UDPRTP_PREFERRED_NETWORK_INTERFACE = "filterv2udprtpinterface";
        const string CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_TIMEOUT = "filterv2udprtpopenconnectiontimeout";
        const string CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_SLEEP_TIME = "filterv2udprtpopenconnectionsleeptime";
        const string CFG_FILTER_V2_UDPRTP_TOTAL_REOPEN_CONNECTION_TIMEOUT = "filterv2udprtptotalreopenconnectiontimeout";
        const string CFG_FILTER_V2_UDPRTP_RECEIVE_DATA_CHECK_INTERVAL = "filterv2udprtpreceivedatacheckinterval";

        #endregion

        #region Singleton
        private static PluginConfiguration _Instance = null;
        public static PluginConfiguration Instance
        {
            get
            {
                if (_Instance == null) _Instance = new PluginConfiguration();
                return _Instance;
            }
        }
		private PluginConfiguration() { Load(); }
        #endregion

        public void ReLoadRuntimeSettings()
        {
            using (Settings settings = new MPSettings())
            {
                BasicHomeScreenName = settings.GetValueAsString(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);
                useQuickSelect = settings.GetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                searchHistoryType = (SearchHistoryType)settings.GetValueAsInt(CFG_SECTION, CFG_SEARCHHISTORYTYPE, (int)searchHistoryType);
                ThumbsAge = settings.GetValueAsInt(CFG_SECTION, CFG_THUMBNAIL_AGE, ThumbsAge);
                OnlineVideos.OnlineVideoSettings.Instance.CacheTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_CACHE_TIMEOUT, OnlineVideos.OnlineVideoSettings.Instance.CacheTimeout);
                OnlineVideos.OnlineVideoSettings.Instance.UtilTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_UTIL_TIMEOUT, OnlineVideos.OnlineVideoSettings.Instance.UtilTimeout);
				OnlineVideos.OnlineVideoSettings.Instance.DynamicCategoryTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_CATEGORYDISCOVERED_TIMEOUT, OnlineVideos.OnlineVideoSettings.Instance.DynamicCategoryTimeout);
                wmpbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                playbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                autoGroupByLang = settings.GetValueAsBool(CFG_SECTION, CFG_AUTO_LANG_GROUPS, autoGroupByLang);
                OnlineVideos.OnlineVideoSettings.Instance.FavoritesFirst = settings.GetValueAsBool(CFG_SECTION, CFG_FAVORITES_FIRST, OnlineVideos.OnlineVideoSettings.Instance.FavoritesFirst);
				LatestVideosRandomize = settings.GetValueAsBool(CFG_SECTION, CFG_LATESTVIDEOS_RANDOMIZE, LatestVideosRandomize);
				LatestVideosOnlineDataRefresh = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_LATESTVIDEOS_ONLINEDATA_REFRESH, (int)LatestVideosOnlineDataRefresh);
				LatestVideosGuiDataRefresh = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_LATESTVIDEOS_GUIDATA_REFRESH, (int)LatestVideosGuiDataRefresh);
				AllowRefreshRateChange = settings.GetValueAsBool(CFG_SECTION, CFG_ALLOW_REFRESHRATE_CHANGE, AllowRefreshRateChange);
				StoreLayoutPerCategory = settings.GetValueAsBool(CFG_SECTION, CFG_STORE_LAYOUT_PER_CATEGORY, StoreLayoutPerCategory);
            }
        }

        void Load()
        {
            OnlineVideos.OnlineVideoSettings ovsconf = OnlineVideos.OnlineVideoSettings.Instance;

            ovsconf.UserStore = new UserStore();
            ovsconf.FavDB = FavoritesDatabase.Instance;
            ovsconf.Logger = Log.Instance;
            ovsconf.ThumbsDir = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\";
            ovsconf.ConfigDir = Config.GetFolder(Config.Dir.Config);
            ovsconf.DllsDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");

            // When run from MPEI we get an invalid plugin directory, we'll try to rectify that here
            try 
            {
                var hasFiles = true;

                if (Directory.Exists(ovsconf.DllsDir))
                {
                    var files = Directory.GetFiles(ovsconf.DllsDir, "OnlineVideos.Sites.*.dll");
                    if (files == null || files.Count() == 0)
                        hasFiles = false;
                }
                else
                    hasFiles = false;

                if (!hasFiles)
                    ovsconf.DllsDir = Path.Combine(MediaPortal.Configuration.Config.GetDirectoryInfo(MediaPortal.Configuration.Config.Dir.Plugins).FullName, "Windows\\OnlineVideos"); 
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
            }
            
            ovsconf.ThumbsResizeOptions = new OnlineVideos.Downloading.ImageDownloader.ResizeOptions()
            {
                MaxSize = (int)Thumbs.ThumbLargeResolution,
                Compositing = Thumbs.Compositing,
                Interpolation = Thumbs.Interpolation,
                Smoothing = Thumbs.Smoothing 
            };
            try
            {
                ovsconf.Locale = CultureInfo.CreateSpecificCulture(GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage()));
            }
            catch (Exception ex)
            {                
                Log.Instance.Error(ex);
            }
            try
            {
                using (Settings settings = new MPSettings())
                {
                    BasicHomeScreenName = settings.GetValueAsString(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);
                    siteOrder = (SiteOrder)settings.GetValueAsInt(CFG_SECTION, CFG_SITEVIEW_ORDER, (int)SiteOrder.AsInFile);
                    currentGroupView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_GROUPVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentSiteView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_SITEVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentCategoryView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_CATEGORYVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentVideoView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_VIDEOVIEW_MODE, (int)GUIFacadeControl.Layout.SmallIcons);

					ovsconf.ThumbsDir = settings.GetValueAsString(CFG_SECTION, CFG_THUMBNAIL_DIR, ovsconf.ThumbsDir).Replace("/", @"\");
                    if (!ovsconf.ThumbsDir.EndsWith(@"\")) ovsconf.ThumbsDir = ovsconf.ThumbsDir + @"\"; // fix thumbnail dir to include the trailing slash
                    try { if (!string.IsNullOrEmpty(ovsconf.ThumbsDir) && !Directory.Exists(ovsconf.ThumbsDir)) Directory.CreateDirectory(ovsconf.ThumbsDir); }
                    catch (Exception e) { Log.Instance.Error("Failed to create thumb dir: {0}", e.ToString()); }
                    ThumbsAge = settings.GetValueAsInt(CFG_SECTION, CFG_THUMBNAIL_AGE, ThumbsAge);
                    Log.Instance.Info("Thumbnails will be stored in {0} with a maximum age of {1} days.", ovsconf.ThumbsDir, ThumbsAge);

                    ovsconf.DownloadDir = settings.GetValueAsString(CFG_SECTION, CFG_DOWNLOAD_DIR, "");
                    try { if (!string.IsNullOrEmpty(ovsconf.DownloadDir) && !Directory.Exists(ovsconf.DownloadDir)) Directory.CreateDirectory(ovsconf.DownloadDir); }
                    catch (Exception e) { Log.Instance.Error("Failed to create download dir: {0}", e.ToString()); }

                    ovsconf.CacheTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_CACHE_TIMEOUT, ovsconf.CacheTimeout);                    
                    ovsconf.UseAgeConfirmation = settings.GetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, ovsconf.UseAgeConfirmation);
                    ovsconf.UtilTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_UTIL_TIMEOUT, ovsconf.UtilTimeout);
					ovsconf.DynamicCategoryTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_CATEGORYDISCOVERED_TIMEOUT, ovsconf.DynamicCategoryTimeout);

                    // set an almost random string by default -> user must enter pin in Configuration before beeing able to watch adult sites
                    pinAgeConfirmation = settings.GetValueAsString(CFG_SECTION, CFG_PIN_AGECONFIRMATION, DateTime.Now.Millisecond.ToString());
                    useQuickSelect = settings.GetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                    wmpbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                    playbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                    email = settings.GetValueAsString(CFG_SECTION, CFG_EMAIL, "");
                    password = settings.GetValueAsString(CFG_SECTION, CFG_PASSWORD, "");
                    string lsFilter = settings.GetValueAsString(CFG_SECTION, CFG_FILTER, "").Trim();
                    FilterArray = lsFilter != "" ? lsFilter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;
                    searchHistoryNum = settings.GetValueAsInt(CFG_SECTION, CFG_SEARCHHISTORY_NUM, searchHistoryNum);
                    searchHistoryType = (SearchHistoryType)settings.GetValueAsInt(CFG_SECTION, CFG_SEARCHHISTORYTYPE, (int)searchHistoryType);

                    string searchHistoryXML = settings.GetValueAsString(CFG_SECTION, CFG_SEARCHHISTORY, "").Trim();
                    if ("" != searchHistoryXML)
                    {
                        try
                        {
                            byte[] searchHistoryBytes = System.Text.Encoding.UTF8.GetBytes(searchHistoryXML);
                            MemoryStream xmlMem = new MemoryStream(searchHistoryBytes);
                            xmlMem.Position = 0;
                            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(Dictionary<string, List<string>>));
                            searchHistory = (Dictionary<string, List<string>>)dcs.ReadObject(xmlMem);
                        }
                        catch (Exception e)
                        {
                            Log.Instance.Warn("Error reading search history from configuration: {0}:{1}! Clearing...", e.GetType(), e.Message);
                            searchHistory = null;
                        }
                    }
                    if (null == searchHistory) searchHistory = new Dictionary<string, List<string>>();

                    // set updateOnStart only when defined, so we have 3 modes: undefined = ask, true = don't ask and update, false = don't ask and don't update
                    string doUpdateString = settings.GetValue(CFG_SECTION, CFG_UPDATEONSTART);
                    if (!string.IsNullOrEmpty(doUpdateString)) updateOnStart = settings.GetValueAsBool(CFG_SECTION, CFG_UPDATEONSTART, true);

                    // last point in time the plugin was run in mediaportal
                    string tempDate = settings.GetValueAsString(CFG_SECTION, CFG_LAST_FIRSTRUN, string.Empty);
                    if (!string.IsNullOrEmpty(tempDate)) DateTime.TryParse(tempDate, out lastFirstRun);

                    updatePeriod = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_UPDATEPERIOD, (int)updatePeriod);

                    // read the video extensions configured in MediaPortal                    
                    string[] mediaportal_user_configured_video_extensions;
                    string strTmp = settings.GetValueAsString("movies", "extensions", ".avi,.mpg,.ogm,.mpeg,.mkv,.wmv,.ifo,.qt,.rm,.mov,.sbe,.dvr-ms,.ts");
                    mediaportal_user_configured_video_extensions = strTmp.Split(',');
					var listOfExtensions = mediaportal_user_configured_video_extensions.ToList();
					listOfExtensions.AddRange(new string[] { ".asf", ".asx", ".flv", ".m4v", ".mov", ".mp4", ".wmv" });
					listOfExtensions = listOfExtensions.Distinct().ToList();
					listOfExtensions.Sort();
					ovsconf.AddSupportedVideoExtensions(listOfExtensions);

                    autoGroupByLang = settings.GetValueAsBool(CFG_SECTION, CFG_AUTO_LANG_GROUPS, autoGroupByLang);
					ovsconf.FavoritesFirst = settings.GetValueAsBool(CFG_SECTION, CFG_FAVORITES_FIRST, ovsconf.FavoritesFirst);

					LatestVideosRandomize = settings.GetValueAsBool(CFG_SECTION, CFG_LATESTVIDEOS_RANDOMIZE, LatestVideosRandomize);
					LatestVideosMaxItems = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_LATESTVIDEOS_MAXITEMS, (int)LatestVideosMaxItems);
					LatestVideosOnlineDataRefresh = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_LATESTVIDEOS_ONLINEDATA_REFRESH, (int)LatestVideosOnlineDataRefresh);
					LatestVideosGuiDataRefresh = (uint)settings.GetValueAsInt(CFG_SECTION, CFG_LATESTVIDEOS_GUIDATA_REFRESH, (int)LatestVideosGuiDataRefresh);

					AllowRefreshRateChange = settings.GetValueAsBool(CFG_SECTION, CFG_ALLOW_REFRESHRATE_CHANGE, AllowRefreshRateChange);
					StoreLayoutPerCategory = settings.GetValueAsBool(CFG_SECTION, CFG_STORE_LAYOUT_PER_CATEGORY, StoreLayoutPerCategory);

                    ovsconf.HttpPreferredNetworkInterface = settings.GetValueAsString(CFG_SECTION, CFG_FILTER_V2_HTTP_PREFERRED_NETWORK_INTERFACE, OnlineVideoSettings.NetworkInterfaceSystemDefault);
                    ovsconf.HttpOpenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_HTTP_OPEN_CONNECTION_TIMEOUT, ovsconf.HttpOpenConnectionTimeout);
                    ovsconf.HttpOpenConnectionSleepTime = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_HTTP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.HttpOpenConnectionSleepTime);
                    ovsconf.HttpTotalReopenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.HttpTotalReopenConnectionTimeout);

                    ovsconf.RtmpPreferredNetworkInterface = settings.GetValueAsString(CFG_SECTION, CFG_FILTER_V2_RTMP_PREFERRED_NETWORK_INTERFACE, OnlineVideoSettings.NetworkInterfaceSystemDefault);
                    ovsconf.RtmpOpenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTMP_OPEN_CONNECTION_TIMEOUT, ovsconf.RtmpOpenConnectionTimeout);
                    ovsconf.RtmpOpenConnectionSleepTime = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTMP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.RtmpOpenConnectionSleepTime);
                    ovsconf.RtmpTotalReopenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTMP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.RtmpTotalReopenConnectionTimeout);

                    ovsconf.RtspPreferredNetworkInterface = settings.GetValueAsString(CFG_SECTION, CFG_FILTER_V2_RTSP_PREFERRED_NETWORK_INTERFACE, OnlineVideoSettings.NetworkInterfaceSystemDefault);
                    ovsconf.RtspOpenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTSP_OPEN_CONNECTION_TIMEOUT, ovsconf.RtspOpenConnectionTimeout);
                    ovsconf.RtspOpenConnectionSleepTime = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTSP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.RtspOpenConnectionSleepTime);
                    ovsconf.RtspTotalReopenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTSP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.RtspTotalReopenConnectionTimeout);

                    ovsconf.RtspClientPortMin = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTSP_CLIENT_PORT_MIN, ovsconf.RtspClientPortMin);
                    ovsconf.RtspClientPortMax = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_RTSP_CLIENT_PORT_MAX, ovsconf.RtspClientPortMax);

                    ovsconf.UdpRtpPreferredNetworkInterface = settings.GetValueAsString(CFG_SECTION, CFG_FILTER_V2_UDPRTP_PREFERRED_NETWORK_INTERFACE, OnlineVideoSettings.NetworkInterfaceSystemDefault);
                    ovsconf.UdpRtpOpenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_TIMEOUT, ovsconf.UdpRtpOpenConnectionTimeout);
                    ovsconf.UdpRtpOpenConnectionSleepTime = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.UdpRtpOpenConnectionSleepTime);
                    ovsconf.UdpRtpTotalReopenConnectionTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_UDPRTP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.UdpRtpTotalReopenConnectionTimeout);
                    ovsconf.UdpRtpReceiveDataCheckInterval = settings.GetValueAsInt(CFG_SECTION, CFG_FILTER_V2_UDPRTP_RECEIVE_DATA_CHECK_INTERVAL, ovsconf.UdpRtpReceiveDataCheckInterval);
                }
                LoadSitesGroups();
                ovsconf.LoadSites();
            }
            catch (Exception e)
            {
                Log.Instance.Error(e);
            }
        }

        public void Save(bool saveOnlyRuntimeModifyable)
        {
            OnlineVideos.OnlineVideoSettings ovsconf = OnlineVideos.OnlineVideoSettings.Instance;
            try
            {
                using (Settings settings = new MPSettings())
                {
                    settings.SetValue(CFG_SECTION, CFG_GROUPVIEW_MODE, (int)currentGroupView);
                    settings.SetValue(CFG_SECTION, CFG_SITEVIEW_MODE, (int)currentSiteView);
                    settings.SetValue(CFG_SECTION, CFG_SITEVIEW_ORDER, (int)siteOrder);
                    settings.SetValue(CFG_SECTION, CFG_VIDEOVIEW_MODE, (int)currentVideoView);
                    settings.SetValue(CFG_SECTION, CFG_CATEGORYVIEW_MODE, (int)currentCategoryView);
                    if (lastFirstRun != default(DateTime)) settings.SetValue(CFG_SECTION, CFG_LAST_FIRSTRUN, lastFirstRun.ToString("s"));
                    try
                    {
                        MemoryStream xmlMem = new MemoryStream();
                        DataContractSerializer dcs = new DataContractSerializer(typeof(Dictionary<string, List<string>>));
                        dcs.WriteObject(xmlMem, searchHistory);
                        xmlMem.Position = 0;
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(xmlMem);
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORY, xmlDoc.InnerXml);
                    }
                    catch (Exception e)
                    {
                        Log.Instance.Warn("Error saving search history to configuration: {0}:{1}! Will be reset on next load...", e.GetType(), e.Message);
                        searchHistory = null;
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORY, "");
                    }

                    if (!saveOnlyRuntimeModifyable)
                    {
                        settings.SetValue(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);
                        settings.SetValue(CFG_SECTION, CFG_THUMBNAIL_DIR, ovsconf.ThumbsDir);
                        settings.SetValue(CFG_SECTION, CFG_THUMBNAIL_AGE, ThumbsAge);
                        settings.SetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, ovsconf.UseAgeConfirmation);
                        settings.SetValue(CFG_SECTION, CFG_PIN_AGECONFIRMATION, pinAgeConfirmation);
                        settings.SetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                        settings.SetValue(CFG_SECTION, CFG_CACHE_TIMEOUT, ovsconf.CacheTimeout);
                        settings.SetValue(CFG_SECTION, CFG_UTIL_TIMEOUT, ovsconf.UtilTimeout);
						settings.SetValue(CFG_SECTION, CFG_CATEGORYDISCOVERED_TIMEOUT, ovsconf.DynamicCategoryTimeout);
                        settings.SetValue(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                        settings.SetValue(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                        settings.SetValue(CFG_SECTION, CFG_UPDATEPERIOD, updatePeriod);
                        if (FilterArray != null && FilterArray.Length > 0) settings.SetValue(CFG_SECTION, CFG_FILTER, string.Join(",", FilterArray));
                        if (!string.IsNullOrEmpty(ovsconf.DownloadDir)) settings.SetValue(CFG_SECTION, CFG_DOWNLOAD_DIR, ovsconf.DownloadDir);
                        if (!string.IsNullOrEmpty(email)) settings.SetValue(CFG_SECTION, CFG_EMAIL, email);
                        if (!string.IsNullOrEmpty(password)) settings.SetValue(CFG_SECTION, CFG_PASSWORD, password);
                        if (updateOnStart == null) settings.RemoveEntry(CFG_SECTION, CFG_UPDATEONSTART);
                        else settings.SetValueAsBool(CFG_SECTION, CFG_UPDATEONSTART, updateOnStart.Value);
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORY_NUM, searchHistoryNum);
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORYTYPE, (int)searchHistoryType);
                        settings.SetValueAsBool(CFG_SECTION, CFG_AUTO_LANG_GROUPS, autoGroupByLang);
                        settings.SetValueAsBool(CFG_SECTION, CFG_FAVORITES_FIRST, ovsconf.FavoritesFirst);
						settings.SetValueAsBool(CFG_SECTION, CFG_LATESTVIDEOS_RANDOMIZE, LatestVideosRandomize);
						settings.SetValue(CFG_SECTION, CFG_LATESTVIDEOS_MAXITEMS, LatestVideosMaxItems);
						settings.SetValue(CFG_SECTION, CFG_LATESTVIDEOS_ONLINEDATA_REFRESH, LatestVideosOnlineDataRefresh);
						settings.SetValue(CFG_SECTION, CFG_LATESTVIDEOS_GUIDATA_REFRESH, LatestVideosGuiDataRefresh);
						settings.SetValueAsBool(CFG_SECTION, CFG_ALLOW_REFRESHRATE_CHANGE, AllowRefreshRateChange);
						settings.SetValueAsBool(CFG_SECTION, CFG_STORE_LAYOUT_PER_CATEGORY, StoreLayoutPerCategory);
                        SaveSitesGroups();
                        ovsconf.SaveSites();

                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_HTTP_PREFERRED_NETWORK_INTERFACE, ovsconf.HttpPreferredNetworkInterface);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_HTTP_OPEN_CONNECTION_TIMEOUT, ovsconf.HttpOpenConnectionTimeout);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_HTTP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.HttpOpenConnectionSleepTime);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.HttpTotalReopenConnectionTimeout);

                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTMP_PREFERRED_NETWORK_INTERFACE, ovsconf.RtmpPreferredNetworkInterface);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTMP_OPEN_CONNECTION_TIMEOUT, ovsconf.RtmpOpenConnectionTimeout);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTMP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.RtmpOpenConnectionSleepTime);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTMP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.RtmpTotalReopenConnectionTimeout);

                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_PREFERRED_NETWORK_INTERFACE, ovsconf.RtspPreferredNetworkInterface);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_OPEN_CONNECTION_TIMEOUT, ovsconf.RtspOpenConnectionTimeout);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.RtspOpenConnectionSleepTime);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.RtspTotalReopenConnectionTimeout);

                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_CLIENT_PORT_MIN, ovsconf.RtspClientPortMin);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_RTSP_CLIENT_PORT_MAX, ovsconf.RtspClientPortMax);

                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_UDPRTP_PREFERRED_NETWORK_INTERFACE, ovsconf.UdpRtpPreferredNetworkInterface);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_TIMEOUT, ovsconf.UdpRtpOpenConnectionTimeout);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_UDPRTP_OPEN_CONNECTION_SLEEP_TIME, ovsconf.UdpRtpOpenConnectionSleepTime);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_UDPRTP_TOTAL_REOPEN_CONNECTION_TIMEOUT, ovsconf.UdpRtpTotalReopenConnectionTimeout);
                        settings.SetValue(CFG_SECTION, CFG_FILTER_V2_UDPRTP_RECEIVE_DATA_CHECK_INTERVAL, ovsconf.UdpRtpReceiveDataCheckInterval);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
            }
        }

        void LoadSitesGroups()
        {
            if (File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Config), SitesGroupsFileName)))
            {
                try
                {
                    using (FileStream fso = new FileStream(Path.Combine(Config.GetFolder(Config.Dir.Config), SitesGroupsFileName), FileMode.Open))
                    {
                        DataContractSerializer dcs = new DataContractSerializer(SitesGroups.GetType());
                        SitesGroups = (BindingList<SitesGroup>)dcs.ReadObject(fso);
                    }
                }
                catch (Exception e)
                {
                    Log.Instance.Warn("Error loading {0}:{1}", SitesGroupsFileName, e.Message);
                }
            }
        }

        public void BuildAutomaticSitesGroups()
        {
            if ((SitesGroups == null || SitesGroups.Count == 0) && autoGroupByLang)
            {
                Dictionary<string, BindingList<string>> sitenames = new Dictionary<string, BindingList<string>>();
                var siteutils = OnlineVideoSettings.Instance.SiteUtilsList;
                foreach (string name in siteutils.Keys)
                {
                    Sites.SiteUtilBase aSite;
                    if (siteutils.TryGetValue(name, out aSite) && !(aSite is Sites.FavoriteUtil) && !(aSite is Sites.DownloadedVideoUtil))
                    {
                        string key = string.IsNullOrEmpty(aSite.Settings.Language) ? "--" : aSite.Settings.Language;
                        BindingList<string> listForLang = null;
                        if (!sitenames.TryGetValue(key, out listForLang)) { listForLang = new BindingList<string>(); sitenames.Add(key, listForLang); }
                        listForLang.Add(aSite.Settings.Name);
                    }
                }
                CachedAutomaticSitesGroups = new BindingList<SitesGroup>();
                foreach (string aLang in sitenames.Keys.ToList().OrderBy(l => l))
                {
                    string name = GetLanguageInUserLocale(aLang);
                    CachedAutomaticSitesGroups.Add(new SitesGroup()
                    {
                        Name = name,
                        Sites = sitenames[aLang],
                        Thumbnail = string.Format(@"{0}Langs\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, aLang)
                    });
                }
            }
        }

        internal static string GetLanguageInUserLocale(string aLang)
        {
            string name = aLang;
            try
            {
                name = aLang != "--" ? System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag(aLang).DisplayName : "Global";
            }
            catch
            {
                var temp = System.Globalization.CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(
                    ci => ci.IetfLanguageTag == aLang || ci.ThreeLetterISOLanguageName == aLang || ci.TwoLetterISOLanguageName == aLang || ci.ThreeLetterWindowsLanguageName == aLang);
                if (temp != null)
                {
                    name = temp.DisplayName;
                }
                else
                {
                    Log.Instance.Warn("Unable to find CultureInfo for language identifier: '{0}'", name);
                }
            }
            return name;
        }

        void SaveSitesGroups()
        {
            if (SitesGroups.Count > 0)
            {
                try
                {
                    using (FileStream fso = new FileStream(Path.Combine(Config.GetFolder(Config.Dir.Config), SitesGroupsFileName), FileMode.Create))
                    {
                        DataContractSerializer dcs = new DataContractSerializer(SitesGroups.GetType());
                        XmlWriter xmlWriter = XmlWriter.Create(fso, new XmlWriterSettings() { Indent = true });
                        dcs.WriteObject(xmlWriter, SitesGroups);
                        xmlWriter.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Instance.Warn("Error saving {0}:{1}", SitesGroupsFileName, e.Message);
                }
            }
            else
            {
                if (File.Exists(Path.Combine(Config.GetFolder(Config.Dir.Config), SitesGroupsFileName)))
                {
                    try
                    {
                        File.Delete(Path.Combine(Config.GetFolder(Config.Dir.Config), SitesGroupsFileName));
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Warn("Error deleting {0}:{1}", SitesGroupsFileName, ex.Message);
                    }
                }
            }
        }
    }
}
