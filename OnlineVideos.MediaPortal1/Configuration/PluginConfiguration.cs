using System;
using MediaPortal.Configuration;
using System.IO;
using System.Globalization;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1
{
    public class PluginConfiguration
    {
        public const string PLUGIN_NAME = "OnlineVideos";

        public string BasicHomeScreenName = "Online Videos";
        public int wmpbuffer = 5000;  // milliseconds
        public int playbuffer = 2;   // percent        
        public int ThumbsAge = 100; // days
        public bool useQuickSelect = false;
        public string[] FilterArray;
        public bool rememberLastSearch = true;
        public string pinAgeConfirmation = "";
        public bool? updateOnStart = null;
        public string email = "";
        public string password = "";        

        #region MediaPortal.xml attribute names
        public const string CFG_SECTION = "onlinevideos";
        public const string CFG_SITEVIEW_MODE = "siteview";
        public const string CFG_SITEVIEW_ORDER = "siteview_order";
        public const string CFG_CATEGORYVIEW_MODE = "categoryview";
        public const string CFG_VIDEOVIEW_MODE = "videoview";
        const string CFG_UPDATEONSTART = "updateOnStart";
        const string CFG_BASICHOMESCREEN_NAME = "basicHomeScreenName";
        const string CFG_THUMBNAIL_DIR = "thumbDir";
        const string CFG_THUMBNAIL_AGE = "thumbAge";
        const string CFG_DOWNLOAD_DIR = "downloadDir";
        const string CFG_FILTER = "filter";
        const string CFG_USE_QUICKSELECT = "useQuickSelect";
        const string CFG_REMEMBER_LAST_SEARCH = "rememberLastSearch";
        const string CFG_USE_AGECONFIRMATION = "useAgeConfirmation";
        const string CFG_PIN_AGECONFIRMATION = "pinAgeConfirmation";
        const string CFG_CACHE_TIMEOUT = "cacheTimeout";
        const string CFG_UTIL_TIMEOUT = "utilTimeout";
        const string CFG_WMP_BUFFER = "wmpbuffer";
        const string CFG_PLAY_BUFFER = "playbuffer";
        const string CFG_EMAIL = "email";
        const string CFG_PASSWORD = "password";
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

        void Load()
        {
            OnlineVideos.OnlineVideoSettings ovsconf = OnlineVideos.OnlineVideoSettings.Instance;

            ovsconf.UserStore = new UserStore();
            ovsconf.FavDB = FavoritesDatabase.Instance;
            ovsconf.Logger = Log.Instance;
            ovsconf.ThumbsDir = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\";
            ovsconf.ConfigDir = Config.GetFolder(Config.Dir.Config);
            ovsconf.DllsDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
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
                    rememberLastSearch = settings.GetValueAsBool(CFG_SECTION, CFG_REMEMBER_LAST_SEARCH, rememberLastSearch);                    
                    wmpbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                    playbuffer = settings.GetValueAsInt(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                    email = settings.GetValueAsString(CFG_SECTION, CFG_EMAIL, "");
                    password = settings.GetValueAsString(CFG_SECTION, CFG_PASSWORD, "");
                    string lsFilter = settings.GetValueAsString(CFG_SECTION, CFG_FILTER, "").Trim();
                    FilterArray = lsFilter != "" ? lsFilter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;

                    // set updateOnStart only when defined, so we have 3 modi: undefined = ask, true = don't ask and update, false = don't ask and don't update
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
                }

                ovsconf.LoadSites();
            }
            catch (Exception e)
            {
                Log.Instance.Error(e);
            }
        }

        public void Save()
        {
            OnlineVideos.OnlineVideoSettings ovsconf = OnlineVideos.OnlineVideoSettings.Instance;
            try
            {
                using (Settings settings = new MPSettings())
                {
                    settings.SetValue(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);
                    settings.SetValue(CFG_SECTION, CFG_THUMBNAIL_DIR, ovsconf.ThumbsDir);
                    settings.SetValue(CFG_SECTION, CFG_THUMBNAIL_AGE, ThumbsAge);
                    settings.SetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, ovsconf.UseAgeConfirmation);
                    settings.SetValue(CFG_SECTION, CFG_PIN_AGECONFIRMATION, pinAgeConfirmation);
                    settings.SetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                    settings.SetValueAsBool(CFG_SECTION, CFG_REMEMBER_LAST_SEARCH, rememberLastSearch);
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
                }

                ovsconf.SaveSites();
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
            }
        }
    }
}
