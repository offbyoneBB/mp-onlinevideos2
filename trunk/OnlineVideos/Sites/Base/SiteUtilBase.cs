using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using OnlineVideos.MPUrlSourceFilter.UserSettings;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// The abstract base class for all sites. 
    /// Instances might be hosted in a seperate AppDomain than the main application, so it can be unloaded at runtime.
    /// </summary>
    public abstract class SiteUtilBase : UserConfigurable
    {
        #region UserConfigurable implementation

        internal override string GetConfigurationKey(string fieldName)
        {
            return string.Format("{0}.{1}", Helpers.FileUtils.GetSaveFilename(Settings.Name).Replace(' ', '_'), fieldName);
        }

        #endregion

        #region User Configurable Settings

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), LocalizableDisplayName("Skip single Category", TranslationFieldName = "SkipSingleCategory"), Description("Enables skipping over category lists that only contain a single category.")]
        protected bool allowDiveDownOrUpIfSingle = true;

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), LocalizableDisplayName("HTTP settings"), Description("Settings for HTTP protocol used by site.")]
        protected HttpUrlSettings httpSettings = new HttpUrlSettings();

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), LocalizableDisplayName("RTMP settings"), Description("Settings for RTMP protocol used by site.")]
        protected RtmpUrlSettings rtmpSettings = new RtmpUrlSettings();

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), LocalizableDisplayName("RTSP settings"), Description("Settings for RTSP protocol used by site.")]
        protected RtspUrlSettings rtspSettings = new RtspUrlSettings();

        [Category(ONLINEVIDEOS_USERCONFIGURATION_CATEGORY), LocalizableDisplayName("UDP/RTP settings"), Description("Settings for UDP or RTP protocol used by site.")]
        protected UdpRtpUrlSettings udpRtpSettings = new UdpRtpUrlSettings();

        public virtual HttpUrlSettings HttpSettings { get { return this.httpSettings; } }

        public virtual RtmpUrlSettings RtmpSettings { get { return this.rtmpSettings; } }

        public virtual RtspUrlSettings RtspSettings { get { return this.rtspSettings; } }

        public virtual UdpRtpUrlSettings UdpRtpSettings { get { return this.udpRtpSettings; } }

        #endregion

        /// <summary>
        /// The <see cref="SiteSettings"/> as configured in the xml will be set after an instance of this class was created 
        /// by the default implementation of the <see cref="Initialize"/> method.
        /// </summary>
        public virtual SiteSettings Settings { get; protected set; }

        /// <summary>
        /// You should always call this implementation, even when overriding it. It is called after the instance has been created
        /// in order to configure settings from the xml for this util.
        /// </summary>
        /// <param name="siteSettings"></param>
        public virtual void Initialize(SiteSettings siteSettings)
        {
            Settings = siteSettings;

            // apply custom settings
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = field.GetCustomAttributes(typeof(CategoryAttribute), false);
                if (attrs.Length > 0)
                {
                    if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosConfiguration"
                        && siteSettings != null
                        && siteSettings.Configuration != null
                        && siteSettings.Configuration.ContainsKey(field.Name))
                    {
                        try
                        {
                            if (field.FieldType.IsEnum)
                            {
                                field.SetValue(this, Enum.Parse(field.FieldType, siteSettings.Configuration[field.Name]));
                            }
                            else
                            {
                                field.SetValue(this, Convert.ChangeType(siteSettings.Configuration[field.Name], field.FieldType));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("{0} - could not set Configuration Value: {1}. Error: {2}", siteSettings.Name, field.Name, ex.Message);
                        }
                    }
                    else 
                        SetUserConfigurationValue(field, attrs[0] as CategoryAttribute); 
                }
            }
        }

        /// <summary>
        /// This is the only function a subclass has to implement. It's called when a user selects a category in the GUI.<br/>
        /// It should return a list of videos for that category, reset the paging indexes, remember this category, whatever is needed to hold state,
        /// because a call to <see cref="HasNextPage"/> and <see cref="GetNextPageVideos"/> will not give any parameter.
        /// </summary>
        /// <param name="category">The <see cref="Category"/> that was selected by the user.</param>
        /// <returns>a list of <see cref="VideoInfo"/> objects for display</returns>
        public abstract List<VideoInfo> GetVideos(Category category);

        /// <summary>
        /// If the site's categories can be retrieved dynamically, then it should be done in the implementation of this method.
        /// The categories must be added to the <see cref="SiteSettings"/> retrieved from the <see cref="Settings"/> property of this class.
        /// Once the categories are added you should set <see cref="SiteSettings.DynamicCategoriesDiscovered"/> to true, 
        /// so this method won't be called each time the user enters this site in the GUI (unless you want that behavior).<br/>
        /// default: sets <see cref="SiteSettings.DynamicCategoriesDiscovered"/> to true
        /// </summary>
        /// <returns>The number of dynamic categories added. 0 means none found / added</returns>
        public virtual int DiscoverDynamicCategories()
        {
            Settings.DynamicCategoriesDiscovered = true;
            return 0;
        }

        /// <summary>
        /// Override this method in your derived Util when you need paging in a list of <see cref="Category"/>s.
        /// It will be called when the last item in that list is a <see cref="NextPageCategory"/>.
        /// </summary>
        /// <param name="category">The category item that you used to store info about how to get the next page categories.</param>
        /// <returns>The number of new categories discovered.</returns>
        public virtual int DiscoverNextPageCategories(NextPageCategory category)
        {
            return 0;
        }

        /// <summary>
        /// If a category has sub-categories this function will be called when the user selects a category in the GUI.
        /// This happens only when <see cref="Category.HasSubCategories"/> is true and <see cref="Category.SubCategoriesDiscovered"/> is false.
        /// </summary>
        /// <param name="parentCategory">the category that was selected by the user</param>
        /// <returns>0 if no sub-categeories where found, otherwise the number of categories that were added to <see cref="Category.SubCategories"/></returns>
        public virtual int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategoriesDiscovered = true;
            return 0;
        }

        /// <summary>
        /// Ths can be used to override the default behavior of diving down or up if only one category is present
        /// </summary>
        /// <returns>if it's allowed to dive up or down</returns>
        public virtual bool AllowDiveDownOrUpIfSingle
        {
            get { return allowDiveDownOrUpIfSingle; }
        }

        /// <summary>
        /// This will be called to find out if there is a next page for the videos that have just been returned 
        /// by a call to <see cref="GetVideos"/>. If returns true, the menu entry for "next page" will be enabled, otherwise disabled.<br/>
        /// default: always false
        /// </summary>
        public virtual bool HasNextPage { get; protected set; }

        /// <summary>
        /// This function should return the videos of the next page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasNextPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "next page".<br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the next page of the last queried category (or search).</returns>
        public virtual List<VideoInfo> GetNextPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This function will be called when the user selects a video for playback. It should return the absolute url to the video file.<br/>
        /// By default, the <see cref="VideoInfo.VideoUrl"/> fields value will be returned.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> from the list of displayed videos that were returned by this instance previously.</param>
        /// <returns>A valid url or filename.</returns>
        public virtual String GetVideoUrl(VideoInfo video)
        {
            return video.VideoUrl;
        }

        /// <summary>
        /// This function will be called to get the urls for playback of a video. 
        /// Use this if your video is split into smaller parts for playback.<br/>
        /// By default: returns a list with the result from <see cref="GetVideoUrl"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
        /// <returns></returns>
        public virtual List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> urls = new List<String>();
            urls.Add(GetVideoUrl(video));
            return urls;
        }

        /// <summary>
        /// Allows the Util to resolve the url of a playlist item to playback options or a new url directly before playback.
        /// </summary>
        /// <param name="clonedVideoInfo">A clone of the original <see cref="VideoInfo"/> object, given in <see cref="GetMultipleVideoUrls"/>, with the VideoUrl set to one of the urls returned.</param>
        /// <param name="chosenPlaybackOption">the key from the <see cref="VideoInfo.PlaybackOptions"/> of the first video chosen by the user</param>
        /// <returns>the resolved url (by default just the clonedVideoInfo.VideoUrl that was given in <see cref="GetMultipleVideoUrls"/></returns>
        public virtual string GetPlaylistItemVideoUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            return clonedVideoInfo.VideoUrl;
        }

        /// <summary>
        /// This method will ask the Util to provide information for a <see cref="VideoInfo"/> that can be used to identify the video for http://trakt.tv/.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> to get info for.</param>
        /// <returns>A filled <see cref="ITrackingInfo"/> with the information required to identify the video uniquely</returns>
        public virtual ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return new TrackingInfo() { VideoKind = VideoKind.Other, Title = video.Title };
        }

        /// <summary>
        /// This method will be called after playback of a video from this site was stopped or has ended.
        /// </summary>
        /// <param name="video"></param>
        /// <param name="url"></param>
        /// <param name="percent"></param>
        /// <param name="stoppedByUser"></param>
        public virtual void OnPlaybackEnded(VideoInfo video, string url, double percent, bool stoppedByUser)
        {
        }

        /// <summary>
        /// This method will be called after download of a video from this site was stopped or has ended.
        /// </summary>
        /// <param name="video"></param>
        /// <param name="url"></param>
        /// <param name="percent"></param>
        /// <param name="stoppedByUser"></param>
        public virtual void OnDownloadEnded(VideoInfo video, string url, double percent, bool stoppedByUser)
        {
        }

        #region Search
        /// <summary>
        /// Returns true, if this site allows searching.<br/>
        /// default: false
        /// </summary>
        public virtual bool CanSearch
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true, if this site has categories to filter on, f.e. newest, highest rating.<br/>
        /// default: false
        /// </summary>
        public virtual bool HasFilterCategories
        {
            get { return false; }
        }

        /// <summary>
        /// Will be called to get the list of categories (names only) that can be chosen to search. 
        /// The keys will be the names and the value will be given to the <see cref="Search"/> as parameter.<br/>
        /// default: returns empty list, so no category specific search can be done
        /// This is also used if <see cref="HasFilterCategories"/> returns true
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> or <see cref="Category"/> for the given query - limited to the given category if not null.<br/>
        /// All items in the list must be of the same type!<br/>
        /// default: returns empty list
        /// </summary>        
        /// <param name="query">The user entered query.</param>
        /// <param name="category">The category to search in, can be null to indicate a global search - not limited to a category.</param>
        /// <returns>the list of videos or categories matching the search query.</returns>
        public virtual List<SearchResultItem> Search(string query, string category = null)
        {
            return new List<SearchResultItem>();
        }

        /// <summary>
        /// Should return the title of the current page, which will be put in #header.label at state=videos when showing videos returned from a search
        /// </summary>
        /// <returns>the title of the current page</returns>
        public virtual string GetCurrentVideosTitle()
        {
            return null;
        }

        #endregion

        /// <summary>
        /// This method will be called before downloading the video from the given url, 
        /// or before adding it to the favorites. (in that case the url param is null.<br/>
        /// By default, the favorite name is the <see cref="VideoInfo.Title"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object that can be used to get some more info.</param>
        /// <param name="category">The <see cref="Category"/> that this video comes from.</param>
        /// <param name="url">The url from which the download will take place. If null, a favorite name should be returned.</param>
        /// <returns>A cleaned pretty name that can be user as filename, or as favorite title.</returns>
        public virtual string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            if (string.IsNullOrEmpty(url)) // called for adding to favorites
                return video.Title;
            else // called for downloading
            {
                Uri uri = new Uri(url);
                string extension = System.IO.Path.GetExtension(uri.LocalPath.Trim(new char[] { '/' }));
                if (extension == string.Empty) extension = System.IO.Path.GetExtension(url);
                if (extension == ".f4v" || extension == ".fid") extension = ".flv";
                // downloading via rtmp always creates a flv file
                if ((string.IsNullOrEmpty(extension) || !OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extension)) && uri.Scheme.StartsWith("rtmp"))
                {
                    extension = ".flv";
                }
                string safeName = Helpers.FileUtils.GetSaveFilename(video.Title);
                return safeName + extension;
            }
        }

        /// <summary>
        /// This function checks a given string, if it points to a file that is a valid video.
        /// It will check for protocol and known and supported video extensions.
        /// </summary>
        /// <param name="fsUrl">the string to check which should be a valid URI.</param>
        /// <returns>true if the url points to a video that can be played with directshow.</returns>
        public virtual bool IsPossibleVideo(string fsUrl)
        {
            if (string.IsNullOrEmpty(fsUrl)) return false; // empty string is not a video
            if (fsUrl.StartsWith("rtsp://")) return false; // rtsp protocol not supported yet
            string extensionFile = System.IO.Path.GetExtension(fsUrl).ToLower();
            bool isVideo = OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extensionFile);
            if (!isVideo)
            {
                foreach (string anExt in OnlineVideoSettings.Instance.VideoExtensions.Keys) if (fsUrl.Contains(anExt)) { isVideo = true; break; }
            }
            return isVideo;
        }

        /// <summary>
        /// This function will be called when a contextmenu for a video or category is to be shown in the GUI. 
        /// Override it to add your own entries (which should be localized).
        /// </summary>
        /// <param name="selectedCategory">either the <see cref="Category"/> to show the context menu for, or the <see cref="Category"/> of the video to show the context menu for</param>
        /// <param name="selectedItem">when this is null the context menu is called on the <see cref="Category"/> otherwise on the <see cref="VideoInfo"/></param>
        /// <returns>A list of <see cref="ContextMenuEntry"/> items to be added to the context menu.</returns>
        public virtual List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            return new List<ContextMenuEntry>();
        }

        /// <summary>
        /// This function is called when one of the custom contextmenu entries was selected by the user.
        /// Override it to handle the entries you added with <see cref="GetContextMenuEntries"/>.
        /// </summary>
        /// <param name="selectedCategory">either the <see cref="Category"/> the context menu was shown for, or the <see cref="Category"/> of the video the context menu was shown for</param>
        /// <param name="selectedItem">when this is null the context menu was called on the <see cref="Category"/> otherwise on the <see cref="VideoInfo"/></param>
        /// <param name="choice">the <see cref="ContextMenuEntry"/> that was chosen by the user</param>
        /// <returns>a <see cref="ContextMenuExecutionResult"/> telling the GUI how to react to the execution of the choice</returns>
        public virtual ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            return null;
        }

        public override string ToString()
        {
            return Settings == null ? base.ToString() : Settings.Name;
        }

        # region helper functions

        /// <summary>
        /// Generic version of <see cref="GetWebData"/> that will automatically convert the retrieved data into the type you provided.
        /// </summary>
        /// <typeparam name="T">The type you want the returned data to be. Supported are:
        /// <list type="bullet">
        /// <item><description><see cref="String"/></description></item>
        /// <item><description><see cref="Newtonsoft.Json.Linq.JToken"/></description></item>
        /// <item><description><see cref="Newtonsoft.Json.Linq.JObject"/></description></item>
        /// <item><description><see cref="RssToolkit.Rss.RssDocument"/></description></item>
        /// <item><description><see cref="XmlDocument"/></description></item>
        /// <item><description><see cref="System.Xml.Linq.XDocument"/></description></item>
        /// <item><description><see cref="HtmlAgilityPack.HtmlDocument"/></description></item>
        /// </list>
        /// </typeparam>
        /// <returns>The data returned by a <see cref="HttpWebResponse"/> converted to the specified type.</returns>
        protected virtual T GetWebData<T>(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            return WebCache.Instance.GetWebData<T>(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
        }

        /// <summary>
        /// This method should be used whenever requesting data via http (GET or POST) in your SiteUtil.
        /// Retrieved data is added to a cache if a GET request with HTTP Status 200 and more than 500 bytes was successful. 
        /// The cache timeout is user configurable (<see cref="OnlineVideoSettings.CacheTimeout"/>).
        /// You can provide some request settings with the optional parameters.
        /// </summary>
        /// <param name="url">The url to request data from - the only mandatory parameter.</param>
        /// <param name="postData">Any data you want to POST with your request.</param>
        /// <param name="cookies">A <see cref="CookieContainer"/> that will send cookies along with the request and afterwards contains all cookies of the response.</param>
        /// <param name="referer">A referer that will be send with the request.</param>
        /// <param name="proxy">If you want to use a proxy for the request, give a <see cref="IWebProxy"/>.</param>
        /// <param name="forceUTF8">Some server are not returning a valid CharacterSet on the response, set to true to force reading the response content as UTF8.</param>
        /// <param name="allowUnsafeHeader">Some server return headers that are treated as unsafe by .net. In order to retrieve that data set this to true.</param>
        /// <param name="userAgent">You can provide a custom UserAgent for the request, otherwise the default one (<see cref="OnlineVideoSettings.UserAgent"/>) is used.</param>
        /// <param name="encoding">Set an <see cref="Encoding"/> for reading the response data.</param>
        /// <param name="headers">Allows to set your own custom headers for the request</param>
        /// <param name="cache">Controls if the result should be cached - default true</param>
        /// <returns>The data returned by a <see cref="HttpWebResponse"/>.</returns>
        protected virtual string GetWebData(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            return WebCache.Instance.GetWebData(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
        }

        #endregion
    }
}
