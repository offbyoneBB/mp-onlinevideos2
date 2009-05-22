using System;
using System.Xml;
using System.Collections.Generic;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using System.Xml.Serialization;

namespace OnlineVideos
{
    /// <summary>
    /// Description of OnlineVideoSettings.
    /// </summary>
    public class OnlineVideoSettings
    {
        [Serializable]
        [XmlRoot("OnlineVideoSites")]
        public class SerializableSettings
        {
            SiteSettings[] sites;
            [XmlArray("Sites")]
            [XmlArrayItem("Site")]
            public SiteSettings[] Sites
            {
                get { return sites; }
                set { sites = value; }
            }
        }

        const string SETTINGS_FILE = "OnlineVideoSites.xml";
        public const string SECTION = "onlinevideos";
        public const string SITEVIEW_MODE = "siteview";
        public const string VIDEOVIEW_MODE = "videoview";
        const string BASICHOMESCREEN_NAME = "basicHomeScreenName";        
        const string THUMBNAIL_DIR = "thumbDir";
        const string TRAILER_SIZE = "trailerSize";
        const string YOUTUBEQUALITY = "youtubequality";
        const string DOWNLOAD_DIR = "downloadDir";        
        const string FILTER = "filter";
        const string USE_AGECONFIRMATION = "useAgeConfirmation";
        const string PIN_AGECONFIRMATION = "pinAgeConfirmation";

        private static OnlineVideoSettings instance = new OnlineVideoSettings();
        public string BasicHomeScreenName = "Online Videos";
        public String msThumbLocation;
        public String msDownloadDir;
        public String[] msFilterArray;
        public bool useAgeConfirmation = false;
        public string pinAgeConfirmation = "";
        public Dictionary<String, SiteSettings> moSiteList = new Dictionary<String, SiteSettings>();
        public Sites.AppleTrailersUtil.VideoQuality AppleTrailerSize = Sites.AppleTrailersUtil.VideoQuality.HD720;
        public Sites.YouTubeUtil.YoutubeVideoQuality YouTubeQuality = OnlineVideos.Sites.YouTubeUtil.YoutubeVideoQuality.High;

        public SortedList<string, bool> videoExtensions = new SortedList<string, bool>();
        public CodecConfiguration CodecConfiguration;

        public static OnlineVideoSettings getInstance()
        {
            if (instance == null) instance = new OnlineVideoSettings();
            return instance;
        }

        private OnlineVideoSettings()
        {
            Load();
            CodecConfiguration = new CodecConfiguration();
        }

        void Load()
        {
            try
            {
                String lsTrailerSize;
                using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    BasicHomeScreenName = xmlreader.GetValueAsString(SECTION, BASICHOMESCREEN_NAME, BasicHomeScreenName);
                    msThumbLocation = xmlreader.GetValueAsString(SECTION, THUMBNAIL_DIR, "");
                    msDownloadDir = xmlreader.GetValueAsString(SECTION, DOWNLOAD_DIR, "");
                    useAgeConfirmation = xmlreader.GetValueAsBool(SECTION, USE_AGECONFIRMATION, false);
                    pinAgeConfirmation = xmlreader.GetValueAsString(SECTION, PIN_AGECONFIRMATION, "");                    
                    String lsFilter = xmlreader.GetValueAsString(SECTION, FILTER, "");
                    lsTrailerSize = xmlreader.GetValueAsString(SECTION, TRAILER_SIZE, "h640");
                    this.YouTubeQuality = (Sites.YouTubeUtil.YoutubeVideoQuality)xmlreader.GetValueAsInt(SECTION, YOUTUBEQUALITY, 1);
                    msFilterArray = lsFilter.Split(new char[] { ',' });
                    if (msFilterArray.Length == 1 && msFilterArray[0] == "")
                    {
                        msFilterArray = null;
                    }

                    // read the video extensions configured in MediaPortal
                    string[] mediaportal_user_configured_video_extensions;
                    string strTmp = xmlreader.GetValueAsString("movies", "extensions", ".avi,.mpg,.ogm,.mpeg,.mkv,.wmv,.ifo,.qt,.rm,.mov,.sbe,.dvr-ms,.ts");
                    mediaportal_user_configured_video_extensions = strTmp.Split(',');
                    foreach (string anExt in mediaportal_user_configured_video_extensions)
                    {
                        if (!videoExtensions.ContainsKey(anExt.ToLower().Trim())) videoExtensions.Add(anExt.ToLower().Trim(), true);
                    }

                    if (!videoExtensions.ContainsKey(".flv")) videoExtensions.Add(".flv", false);
                    if (!videoExtensions.ContainsKey(".m4v")) videoExtensions.Add(".m4v", false);
                    if (!videoExtensions.ContainsKey(".mp4")) videoExtensions.Add(".mp4", false);
                }

                if (Enum.IsDefined(typeof(Sites.AppleTrailersUtil.VideoQuality), lsTrailerSize))
                {
                    AppleTrailerSize = (Sites.AppleTrailersUtil.VideoQuality)Enum.Parse(typeof(Sites.AppleTrailersUtil.VideoQuality), lsTrailerSize, true);
                }

                if (String.IsNullOrEmpty(msThumbLocation))
                {
                    msThumbLocation = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\";
                }

                string filename = Config.GetFile(Config.Dir.Config, SETTINGS_FILE);
                if (!System.IO.File.Exists(filename))
                {
                    Log.Error("ConfigFile {0} was not found!", filename);
                }
                else
                {
                    using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                        System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(SerializableSettings));
                        SerializableSettings s = (SerializableSettings)ser.Deserialize(fs);
                        fs.Close();
                        AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                        moSiteList.Clear();
                        foreach (SiteSettings site in s.Sites) moSiteList.Add(site.Name, site);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void Save()
        {
            try
            {
                Log.Info("using MP config file:" + Config.GetFile(Config.Dir.Config, "MediaPortal.xml"));
                using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                {
                    String lsFilterList = "";
                    String[] lsFilterArray = msFilterArray;
                    if (lsFilterArray != null)
                    {
                        foreach (String lsFilter in lsFilterArray)
                        {
                            lsFilterList += lsFilter + ",";
                        }
                    }
                    if ((lsFilterList == ",") == false)
                    {
                        lsFilterList.Remove(lsFilterList.Length - 1);
                        xmlwriter.SetValue(SECTION, FILTER, lsFilterList);
                    }
                    else
                    {
                        msFilterArray = null;
                    }
                    xmlwriter.SetValue(SECTION, BASICHOMESCREEN_NAME, BasicHomeScreenName);
                    xmlwriter.SetValue(SECTION, THUMBNAIL_DIR, msThumbLocation);
                    Log.Info("OnlineVideoSettings - download Dir:" + msDownloadDir);
                    xmlwriter.SetValue(SECTION, DOWNLOAD_DIR, msDownloadDir);
                    xmlwriter.SetValue(SECTION, TRAILER_SIZE, AppleTrailerSize.ToString());
                    xmlwriter.SetValue(SECTION, YOUTUBEQUALITY, (int)YouTubeQuality);
                    xmlwriter.SetValueAsBool(SECTION, USE_AGECONFIRMATION, useAgeConfirmation);
                    xmlwriter.SetValue(SECTION, PIN_AGECONFIRMATION, pinAgeConfirmation);
                }

                // only save if there are sites - otherwise an error might have occured on load
                if (moSiteList != null && moSiteList.Count > 0)
                {
                    string filename = Config.GetFile(Config.Dir.Config, SETTINGS_FILE);
                    if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);

                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    SiteSettings[] sites = new SiteSettings[moSiteList.Count];
                    moSiteList.Values.CopyTo(sites, 0);
                    SerializableSettings s = new SerializableSettings();
                    s.Sites = sites;
                    System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(s.GetType());
                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Encoding = System.Text.Encoding.UTF8;
                    xmlSettings.Indent = true;

                    using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
                    {
                        XmlWriter writer = XmlWriter.Create(fs, xmlSettings);
                        ser.Serialize(writer, s);
                        fs.Close();
                    }

                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// This Method was needed for the XmlSerializer to find the serialization assembly when the Configuration tool is used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName == args.Name)
                    return asm;
            }
            return null;
        }
    }
}
