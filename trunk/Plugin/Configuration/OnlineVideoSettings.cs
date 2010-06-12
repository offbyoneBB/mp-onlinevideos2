using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using OnlineVideos.Sites;

namespace OnlineVideos
{
    /// <summary>
    /// Description of OnlineVideoSettings.
    /// </summary>
    public class OnlineVideoSettings
    {
        public const string USERAGENT = "Mozilla/5.0 (Windows; U; Windows NT 6.1; de; rv:1.9.1.3) Gecko/20090824 Firefox/3.5.3";
        public const string PLUGIN_NAME = "Online Videos";
        public const string SETTINGS_FILE = "OnlineVideoSites.xml";

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

        public string BasicHomeScreenName = PLUGIN_NAME;        
        public string ThumbsDir = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\";
        public int thumbAge = 100;
        public string DownloadDir;
        public string[] FilterArray;
        public bool useAgeConfirmation = true;
        public bool useQuickSelect = false;
        public bool rememberLastSearch = true;
        public string pinAgeConfirmation = "";
        public int cacheTimeout = 30; // minutes
        public int utilTimeout = 15;  // seconds
        public int wmpbuffer = 5000;  // milliseconds
        public int playbuffer = 2;   // percent

        public string email = "";
        public string password = "";
        public bool? updateOnStart = null;

        public BindingList<SiteSettings> SiteSettingsList { get; protected set; }
        public Dictionary<string, Sites.SiteUtilBase> SiteList { get; protected set; }
        public SortedList<string, bool> VideoExtensions { get; protected set; }
        public CultureInfo MediaPortalLocale { get; protected set; }

        public bool ageHasBeenConfirmed = false;

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
            try
            {
                MediaPortalLocale = CultureInfo.CreateSpecificCulture(GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage()));
            }
            catch (Exception ex)
            {
                MediaPortalLocale = CultureInfo.CurrentUICulture;
                Log.Error(ex);                
            }
            SiteSettingsList = new BindingList<SiteSettings>();
            SiteList = new Dictionary<string, OnlineVideos.Sites.SiteUtilBase>();
            Load();

            // create some needed directories
            string iconDir = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"OnlineVideos\Icons\");
            if (!Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
            string bannerDir = Path.Combine(Config.GetFolder(Config.Dir.Thumbs), @"OnlineVideos\Banners\");
            if (!Directory.Exists(bannerDir)) Directory.CreateDirectory(bannerDir);
            try
            {
                string dllDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
                if (!Directory.Exists(dllDir)) Directory.CreateDirectory(dllDir);
            }
            catch { /* might fail due to UAC */ }
        }

        XmlSerializerImplementation _XmlSerImp;
        public XmlSerializerImplementation XmlSerImp
        {
            get
            {
                if (_XmlSerImp == null)
                {
                    _XmlSerImp = (XmlSerializerImplementation)Activator.CreateInstance(GetType().Assembly.GetType("Microsoft.Xml.Serialization.GeneratedAssembly.XmlSerializerContract", false));
                }
                return _XmlSerImp;
            }
        }

        void Load()
        {
            try
            {                
                using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    BasicHomeScreenName = xmlreader.GetValueAsString(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);

                    ThumbsDir = xmlreader.GetValueAsString(CFG_SECTION, CFG_THUMBNAIL_DIR, ThumbsDir).Replace("/", @"\");
                    if (!ThumbsDir.EndsWith(@"\")) ThumbsDir = ThumbsDir + @"\"; // fix thumbnail dir to include the trailing slash
                    try { if (!Directory.Exists(ThumbsDir)) Directory.CreateDirectory(ThumbsDir); }
                    catch (Exception e) { Log.Error("Failed to create thumb dir: {0}", e.ToString()); }
                    thumbAge = xmlreader.GetValueAsInt(CFG_SECTION, CFG_THUMBNAIL_AGE, thumbAge);
                    Log.Info("Thumbnails will be stored in {0} with a maximum age of {1} days.", ThumbsDir, thumbAge);
                    
                    DownloadDir = xmlreader.GetValueAsString(CFG_SECTION, CFG_DOWNLOAD_DIR, "");
                    try { if (Directory.Exists(DownloadDir)) Directory.CreateDirectory(DownloadDir); }
                    catch (Exception e) { Log.Error("Failed to create download dir: {0}", e.ToString()); }

                    // enable pin by default -> child protection
                    useAgeConfirmation = xmlreader.GetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, useAgeConfirmation);
                    // set an almost random string by default -> user must enter pin in Configuration before beeing able to watch adult sites
                    pinAgeConfirmation = xmlreader.GetValueAsString(CFG_SECTION, CFG_PIN_AGECONFIRMATION, DateTime.Now.Millisecond.ToString());
                    useQuickSelect = xmlreader.GetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                    rememberLastSearch = xmlreader.GetValueAsBool(CFG_SECTION, CFG_REMEMBER_LAST_SEARCH, rememberLastSearch);
                    cacheTimeout = xmlreader.GetValueAsInt(CFG_SECTION, CFG_CACHE_TIMEOUT, cacheTimeout);
                    utilTimeout = xmlreader.GetValueAsInt(CFG_SECTION, CFG_UTIL_TIMEOUT, utilTimeout);
                    wmpbuffer = xmlreader.GetValueAsInt(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                    playbuffer = xmlreader.GetValueAsInt(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                    email = xmlreader.GetValueAsString(CFG_SECTION, CFG_EMAIL, "");
                    password = xmlreader.GetValueAsString(CFG_SECTION, CFG_PASSWORD, "");
                    string lsFilter = xmlreader.GetValueAsString(CFG_SECTION, CFG_FILTER, "").Trim();
                    FilterArray = lsFilter != "" ? lsFilter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;

                    // set updateOnStart only when defined, so we have 3 modi: undefined = ask, true = don't ask and update, false = don't ask and don't update
                    string doUpdateString = xmlreader.GetValue(OnlineVideoSettings.CFG_SECTION, OnlineVideoSettings.CFG_UPDATEONSTART);
                    if (!string.IsNullOrEmpty(doUpdateString)) updateOnStart = xmlreader.GetValueAsBool(OnlineVideoSettings.CFG_SECTION, OnlineVideoSettings.CFG_UPDATEONSTART, true);

                    // read the video extensions configured in MediaPortal
                    VideoExtensions = new SortedList<string, bool>();
                    string[] mediaportal_user_configured_video_extensions;
                    string strTmp = xmlreader.GetValueAsString("movies", "extensions", ".avi,.mpg,.ogm,.mpeg,.mkv,.wmv,.ifo,.qt,.rm,.mov,.sbe,.dvr-ms,.ts");
                    mediaportal_user_configured_video_extensions = strTmp.Split(',');
                    foreach (string anExt in mediaportal_user_configured_video_extensions)
                    {
                        if (!VideoExtensions.ContainsKey(anExt.ToLower().Trim())) VideoExtensions.Add(anExt.ToLower().Trim(), true);
                    }

                    if (!VideoExtensions.ContainsKey(".asf")) VideoExtensions.Add(".asf", false);
                    if (!VideoExtensions.ContainsKey(".asx")) VideoExtensions.Add(".asx", false);
                    if (!VideoExtensions.ContainsKey(".flv")) VideoExtensions.Add(".flv", false);
                    if (!VideoExtensions.ContainsKey(".m4v")) VideoExtensions.Add(".m4v", false);
                    if (!VideoExtensions.ContainsKey(".mov")) VideoExtensions.Add(".mov", false);
                    if (!VideoExtensions.ContainsKey(".mp4")) VideoExtensions.Add(".mp4", false);
                    if (!VideoExtensions.ContainsKey(".wmv")) VideoExtensions.Add(".wmv", false);
                }
                
                string filename = Config.GetFile(Config.Dir.Config, SETTINGS_FILE);
                if (!File.Exists(filename))
                {
                    Log.Info("ConfigFile {0} was not found using embedded resource.", filename);
                    using (Stream fs = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("OnlineVideos.OnlineVideoSites.xml"))
                    {
                        XmlSerializer ser = XmlSerImp.GetSerializer(typeof(SerializableSettings));
                        SerializableSettings s = (SerializableSettings)ser.Deserialize(fs);
                        fs.Close();
                        SiteSettingsList = s.Sites;
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        XmlSerializer ser = XmlSerImp.GetSerializer(typeof(SerializableSettings));
                        SerializableSettings s = (SerializableSettings)ser.Deserialize(fs);
                        fs.Close();
                        SiteSettingsList = s.Sites;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        void LoadScriptSites()
        {
            Log.Info("Loading script files");
            if (Directory.Exists(Config.GetSubFolder(Config.Dir.Config, "scripts\\OnlineVideos")))
            {
                FileInfo[] fileInfos = Config.GetSubDirectoryInfo(Config.Dir.Config, "scripts\\OnlineVideos").GetFiles("*.xml");
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

            //create a favorites site
            SiteSettings aSite = new SiteSettings()
            {
                Name = Translation.Favourites,
                UtilName = "Favorite",
                IsEnabled = true
            };
            SiteList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));

            if (!String.IsNullOrEmpty(DownloadDir))
            {                
                //add a downloaded videos site
                aSite = new SiteSettings()
                {
                    Name = Translation.DownloadedVideos,
                    UtilName = "DownloadedVideo",
                    IsEnabled = true
                };
                SiteList.Add(aSite.Name, SiteUtilFactory.CreateFromShortName(aSite.UtilName, aSite));
            }
        }

        public void Save()
        {
            try
            {
                Log.Info("using MP config file:" + Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));
                using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    xmlwriter.SetValue(CFG_SECTION, CFG_BASICHOMESCREEN_NAME, BasicHomeScreenName);
                    xmlwriter.SetValue(CFG_SECTION, CFG_THUMBNAIL_DIR, ThumbsDir);
                    xmlwriter.SetValue(CFG_SECTION, CFG_THUMBNAIL_AGE, thumbAge);
                    xmlwriter.SetValueAsBool(CFG_SECTION, CFG_USE_AGECONFIRMATION, useAgeConfirmation);
                    xmlwriter.SetValue(CFG_SECTION, CFG_PIN_AGECONFIRMATION, pinAgeConfirmation);
                    xmlwriter.SetValueAsBool(CFG_SECTION, CFG_USE_QUICKSELECT, useQuickSelect);
                    xmlwriter.SetValueAsBool(CFG_SECTION, CFG_REMEMBER_LAST_SEARCH, rememberLastSearch);
                    xmlwriter.SetValue(CFG_SECTION, CFG_CACHE_TIMEOUT, cacheTimeout);
                    xmlwriter.SetValue(CFG_SECTION, CFG_UTIL_TIMEOUT, utilTimeout);
                    xmlwriter.SetValue(CFG_SECTION, CFG_WMP_BUFFER, wmpbuffer);
                    xmlwriter.SetValue(CFG_SECTION, CFG_PLAY_BUFFER, playbuffer);
                    if (FilterArray != null && FilterArray.Length > 0) xmlwriter.SetValue(CFG_SECTION, CFG_FILTER, string.Join(",", FilterArray));
                    if (!string.IsNullOrEmpty(DownloadDir)) xmlwriter.SetValue(CFG_SECTION, CFG_DOWNLOAD_DIR, DownloadDir);
                    if (!string.IsNullOrEmpty(email)) xmlwriter.SetValue(CFG_SECTION, CFG_EMAIL, email);
                    if (!string.IsNullOrEmpty(password)) xmlwriter.SetValue(CFG_SECTION, CFG_PASSWORD, password);
                    if (updateOnStart == null) xmlwriter.RemoveEntry(CFG_SECTION, CFG_UPDATEONSTART);
                    else xmlwriter.SetValueAsBool(CFG_SECTION, CFG_UPDATEONSTART, updateOnStart.Value);
                }

                SaveSites();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void SaveSites()
        {
            // only save if there are sites - otherwise an error might have occured on load
            if (SiteSettingsList != null && SiteSettingsList.Count > 0)
            {
                string filename = Config.GetFile(Config.Dir.Config, SETTINGS_FILE);
                if (File.Exists(filename)) File.Delete(filename);

                SerializableSettings s = new SerializableSettings();
                s.Sites = SiteSettingsList;
                System.Xml.Serialization.XmlSerializer ser = XmlSerImp.GetSerializer(s.GetType());
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Encoding = System.Text.Encoding.UTF8;
                xmlSettings.Indent = true;

                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    XmlWriter writer = XmlWriter.Create(fs, xmlSettings);
                    ser.Serialize(writer, s);
                    fs.Close();
                }
            }
        }
    }
}
