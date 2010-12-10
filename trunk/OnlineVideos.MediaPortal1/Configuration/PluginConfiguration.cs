using System;
using MediaPortal.Configuration;
using System.IO;
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
        public string httpSourceFilterName = "File Source (URL)";
        public SearchHistoryType searchHistoryType = SearchHistoryType.Simple;
        public int searchHistoryNum = 9;
        public BindingList<SitesGroup> SitesGroups = new BindingList<SitesGroup>();

        // runtime (inside MediaPortal) changeable values
        public Dictionary<string, List<string>> searchHistory;
        public SiteOrder siteOrder = SiteOrder.AsInFile;
#if MP11
        public GUIFacadeControl.ViewMode currentGroupView = GUIFacadeControl.ViewMode.List;
        public GUIFacadeControl.ViewMode currentSiteView = GUIFacadeControl.ViewMode.List;
        public GUIFacadeControl.ViewMode currentCategoryView = GUIFacadeControl.ViewMode.List;
        public GUIFacadeControl.ViewMode currentVideoView = GUIFacadeControl.ViewMode.SmallIcons;
#else
        public GUIFacadeControl.Layout currentGroupView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentSiteView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentCategoryView = GUIFacadeControl.Layout.List;
        public GUIFacadeControl.Layout currentVideoView = GUIFacadeControl.Layout.SmallIcons;
#endif

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
        const string CFG_WMP_BUFFER = "wmpbuffer";
        const string CFG_PLAY_BUFFER = "playbuffer";
        const string CFG_EMAIL = "email";
        const string CFG_PASSWORD = "password";
        const string CFG_HTTP_SOURCE_FILTER = "httpsourcefilter";
        const string CFG_SEARCHHISTORY_ENABLED = "searchHistoryEnabled";
        const string CFG_SEARCHHISTORY_NUM = "searchHistoryNum";
        const string CFG_SEARCHHISTORY = "searchHistory";
        const string CFG_SEARCHHISTORYTYPE = "searchHistoryType";
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
                wmpbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                playbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
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
            ovsconf.ThumbsResizeOptions = new OnlineVideos.ImageDownloader.ResizeOptions()
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
#if MP11
                    currentGroupView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(CFG_SECTION, CFG_GROUPVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                    currentSiteView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(CFG_SECTION, CFG_SITEVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                    currentCategoryView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(CFG_SECTION, CFG_CATEGORYVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                    currentVideoView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(CFG_SECTION, CFG_VIDEOVIEW_MODE, (int)GUIFacadeControl.ViewMode.SmallIcons);
#else
                    currentGroupView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_GROUPVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentSiteView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_SITEVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentCategoryView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_CATEGORYVIEW_MODE, (int)GUIFacadeControl.Layout.List);
                    currentVideoView = (GUIFacadeControl.Layout)settings.GetValueAsInt(CFG_SECTION, CFG_VIDEOVIEW_MODE, (int)GUIFacadeControl.Layout.SmallIcons);
#endif
                    ovsconf.ThumbsDir = settings.GetValueAsString(CFG_SECTION, CFG_THUMBNAIL_DIR, ovsconf.ThumbsDir).Replace("/", @"\");
                    if (!ovsconf.ThumbsDir.EndsWith(@"\")) ovsconf.ThumbsDir = ovsconf.ThumbsDir + @"\"; // fix thumbnail dir to include the trailing slash
                    try { if (!Directory.Exists(ovsconf.ThumbsDir)) Directory.CreateDirectory(ovsconf.ThumbsDir); }
                    catch (Exception e) { Log.Instance.Error("Failed to create thumb dir: {0}", e.ToString()); }
                    ThumbsAge = settings.GetValueAsInt(CFG_SECTION, CFG_THUMBNAIL_AGE, ThumbsAge);
                    Log.Instance.Info("Thumbnails will be stored in {0} with a maximum age of {1} days.", ovsconf.ThumbsDir, ThumbsAge);

                    ovsconf.DownloadDir = settings.GetValueAsString(CFG_SECTION, CFG_DOWNLOAD_DIR, "");
                    try { if (Directory.Exists(ovsconf.DownloadDir)) Directory.CreateDirectory(ovsconf.DownloadDir); }
                    catch (Exception e) { Log.Instance.Error("Failed to create download dir: {0}", e.ToString()); }

                    ovsconf.CacheTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_CACHE_TIMEOUT, ovsconf.CacheTimeout);                    
                    ovsconf.UseAgeConfirmation = settings.GetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, ovsconf.UseAgeConfirmation);
                    ovsconf.UtilTimeout = settings.GetValueAsInt(CFG_SECTION, CFG_UTIL_TIMEOUT, ovsconf.UtilTimeout);

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
                            byte[] searchHistoryBytes = System.Text.Encoding.GetEncoding(0).GetBytes(searchHistoryXML);
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

                    // read the video extensions configured in MediaPortal                    
                    string[] mediaportal_user_configured_video_extensions;
                    string strTmp = settings.GetValueAsString("movies", "extensions", ".avi,.mpg,.ogm,.mpeg,.mkv,.wmv,.ifo,.qt,.rm,.mov,.sbe,.dvr-ms,.ts");
                    mediaportal_user_configured_video_extensions = strTmp.Split(',');
                    foreach (string anExt in mediaportal_user_configured_video_extensions)
                    {
                        if (!ovsconf.VideoExtensions.ContainsKey(anExt.ToLower().Trim())) ovsconf.VideoExtensions.Add(anExt.ToLower().Trim(), true);
                    }

                    if (!ovsconf.VideoExtensions.ContainsKey(".asf")) ovsconf.VideoExtensions.Add(".asf", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".asx")) ovsconf.VideoExtensions.Add(".asx", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".flv")) ovsconf.VideoExtensions.Add(".flv", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".m4v")) ovsconf.VideoExtensions.Add(".m4v", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".mov")) ovsconf.VideoExtensions.Add(".mov", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".mp4")) ovsconf.VideoExtensions.Add(".mp4", false);
                    if (!ovsconf.VideoExtensions.ContainsKey(".wmv")) ovsconf.VideoExtensions.Add(".wmv", false);

                    httpSourceFilterName = settings.GetValueAsString(CFG_SECTION, CFG_HTTP_SOURCE_FILTER, httpSourceFilterName);
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
                        settings.SetValue(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                        settings.SetValue(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                        if (FilterArray != null && FilterArray.Length > 0) settings.SetValue(CFG_SECTION, CFG_FILTER, string.Join(",", FilterArray));
                        if (!string.IsNullOrEmpty(ovsconf.DownloadDir)) settings.SetValue(CFG_SECTION, CFG_DOWNLOAD_DIR, ovsconf.DownloadDir);
                        if (!string.IsNullOrEmpty(email)) settings.SetValue(CFG_SECTION, CFG_EMAIL, email);
                        if (!string.IsNullOrEmpty(password)) settings.SetValue(CFG_SECTION, CFG_PASSWORD, password);
                        if (updateOnStart == null) settings.RemoveEntry(CFG_SECTION, CFG_UPDATEONSTART);
                        else settings.SetValueAsBool(CFG_SECTION, CFG_UPDATEONSTART, updateOnStart.Value);
                        if (httpSourceFilterName == "File Source (URL)") settings.RemoveEntry(CFG_SECTION, CFG_HTTP_SOURCE_FILTER);
                        else settings.SetValue(CFG_SECTION, CFG_HTTP_SOURCE_FILTER, httpSourceFilterName);
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORY_NUM, searchHistoryNum);
                        settings.SetValue(CFG_SECTION, CFG_SEARCHHISTORYTYPE, (int)searchHistoryType);
                        SaveSitesGroups();

                        ovsconf.SaveSites();
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
