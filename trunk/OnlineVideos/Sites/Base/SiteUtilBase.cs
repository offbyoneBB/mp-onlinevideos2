using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// The abstract base class for all sites. 
    /// It might be hosted in a seperate AppDomain than the main application, so it can be unloaded at runtime.
    /// </summary>
    public abstract class SiteUtilBase : MarshalByRefObject, ICustomTypeDescriptor
    {
        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Skip single Category", TranslationFieldName = "SkipSingleCategory"), Description("Enables skipping over category lists that only contain a single category.")]
        protected bool allowDiveDownOrUpIfSingle = true;

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
                    else if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration"
                             && OnlineVideoSettings.Instance.UserStore != null)
                    {
                        string value = OnlineVideoSettings.Instance.UserStore.GetValue(string.Format("{0}.{1}", Utils.GetSaveFilename(siteSettings.Name).Replace(' ', '_'), field.Name));
                        if (value != null)
                        {
                            try
                            {
                                if (field.FieldType.IsEnum)
                                {
                                    field.SetValue(this, Enum.Parse(field.FieldType, value));
                                }
                                else
                                {
                                    field.SetValue(this, Convert.ChangeType(value, field.FieldType));
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warn("{0} - ould not set User Configuration Value: {1}. Error: {2}", siteSettings.Name, field.Name, ex.Message);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is the only function a subclass has to implement. It's called when a user selects a category in the GUI. 
        /// It should return a list of videos for that category, reset the paging indexes, remember this category, whatever is needed to hold state.
        /// </summary>
        /// <param name="category">The <see cref="Category"/> that was selected by the user.</param>
        /// <returns>a list of <see cref="VideoInfo"/> object for display</returns>
        public abstract List<VideoInfo> getVideoList(Category category);

        /// <summary>
        /// If the site's categories can be retrieved dynamically, then it should be done in the implementation of this method.
        /// The categories must be added to the <see cref="SiteSettings"/> retrieved from the <see cref="Settings"/> property of this class.
        /// Once the categories are added you should set <see cref="SiteSettings.DynamicCategoriesDiscovered"/> to true, 
        /// so this method won't be called each time the user enters this site in the GUI (unless youi want that behavior).<br/>
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
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "next page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public virtual bool HasNextPage { get; protected set; }

        /// <summary>
        /// This function should return the videos of the next page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasNextPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "next page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the next page of the last queried category.</returns>
        public virtual List<VideoInfo> getNextPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This will be called to find out if there is a previous page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "previous page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        [Obsolete]
        public virtual bool HasPreviousPage { get; protected set; }

        /// <summary>
        /// This function should return the videos of the previous page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasPreviousPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "previous page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the previous page of the last queried category.</returns>
        [Obsolete]
        public virtual List<VideoInfo> getPreviousPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This function will be called to get the urls for playback of a video.<br/>
        /// By default: returns a list with the result from <see cref="getUrl"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
        /// <returns></returns>
        public virtual List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> urls = new List<String>();
            urls.Add(getUrl(video));
            return urls;
        }

        /// <summary>
        /// Allows the Util to resolve the url of a playlist item to playback options or a new url directly before playback.
        /// </summary>
        /// <param name="clonedVideoInfo">A clone of the original <see cref="VideoInfo"/> object, given in <see cref="getMultipleVideoUrls"/>, with the VideoUrl set to one of the urls returned.</param>
        /// <param name="chosenPlaybackOption">the key from the <see cref="VideoInfo.PlaybackOptions"/> of the first video chosen by the user</param>
        /// <returns>the resolved url (by default just the clonedVideoInfo.VideoUrl that was given in <see cref="getMultipleVideoUrls"/></returns>
        public virtual string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
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

        /// <summary>
        /// This function will be called when the user selects a video for playback. It should return the absolute url to the video file.<br/>
        /// By default, the <see cref="VideoInfo.VideoUrl"/> fields value will be returned.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> from the list of displayed videos that were returned by this instance previously.</param>
        /// <returns>A valid url or filename.</returns>
        public virtual String getUrl(VideoInfo video)
        {
            return video.VideoUrl;
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
        /// This is also used if HasFilterCategories returns true
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, string> GetSearchableCategories()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> for the given query.
        /// </summary>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos matching that search query.</returns>
        [Obsolete]
        public virtual List<VideoInfo> Search(string query)
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> or <see cref="Category"/> for the given query.<br/>
        /// All items in the list must be of the same type!
        /// </summary>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos or categories matching that search query.</returns>
        public virtual List<ISearchResultItem> DoSearch(string query)
        {
            return Search(query).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> for the given query limited to the given category.<br/>
        /// default: calls the Search overload without a category parameter
        /// </summary>        
        /// <param name="category">The category to search in.</param>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos matching that search query.</returns>
        [Obsolete]
        public virtual List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> or <see cref="Category"/> for the given query limited to the given category.<br/>
        /// All items in the list must be of the same type!
        /// default: calls the Search overload without a category parameter
        /// </summary>        
        /// <param name="category">The category to search in.</param>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos or categories matching that search query.</returns>
        public virtual List<ISearchResultItem> DoSearch(string query, string category)
        {
            return Search(query, category).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        /// <summary>
        /// Should return the title of the current page, which will be put in #header.label at state=videos
        /// </summary>
        /// <returns>the title of the current page</returns>
        public virtual string getCurrentVideosTitle()
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
                string safeName = Utils.GetSaveFilename(video.Title);
                return safeName + extension;
            }
        }

        /// <summary>
        /// This function checks a given string, if it points to a file that is a valid video.
        /// It will check for protocol and known and supported video extensions.
        /// </summary>
        /// <param name="fsUrl">the string to check which should be a valid URI.</param>
        /// <returns>true if the url points to a video that can be played with directshow.</returns>
        public virtual bool isPossibleVideo(string fsUrl)
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

        # region static helper functions

        public static string GetRedirectedUrl(string url, string referer = null)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                if (!string.IsNullOrEmpty(referer)) request.Referer = referer;
                request.Timeout = 15000;
                httpWebresponse = request.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;

                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                {
                    if (httpWebresponse.ContentLength > 0 && httpWebresponse.ContentLength < 1024)
                    {
                        string content = new StreamReader(httpWebresponse.GetResponseStream()).ReadToEnd();
                        if (httpWebresponse.ContentType.Contains("video/quicktime"))
                        {
                            return content.Split('\n')[1];
                        }
                        return httpWebresponse.ResponseUri.ToString();
                    }
                    else
                        return url;
                }
                else
                    return httpWebresponse.ResponseUri.OriginalString;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            finally
            {
                if (httpWebresponse != null) httpWebresponse.Close();
            }
            return url;
        }

        /// <summary>
        /// This method should be used whenever requesting data via http get. You can optionally provide some request settings.
        /// It will automatically convert the retrieved data into the type you provided.
        /// Retrieved data is added to a cache if HTTP Status was 200 and more than 500 bytes were retrieved. The cache timeout is user configurable (<see cref="OnlineVideoSettings.CacheTimeout"/>).
        /// </summary>
        /// <typeparam name="T">The type you want the returned data to be. Supported are <see cref="String"/>, <see cref="Newtonsoft.Json.Linq.JToken"/>, <see cref="Newtonsoft.Json.Linq.JObject"/>, <see cref="RssToolkit.Rss.RssDocument"/>, <see cref="XmlDocument"/>, <see cref="System.Xml.Linq.XDocument"/> and <see cref="HtmlAgilityPack.HtmlDocument"/>.</typeparam>
        /// <param name="url">The url to requets data from.</param>
        /// <param name="cc">A <see cref="CookieContainer"/> that will send cookies along with the request and afterwards contains all cookies of the response.</param>
        /// <param name="referer">A referer that will be send with the request.</param>
        /// <param name="proxy">If you want to use a proxy for the request, give a <see cref="IWebProxy"/>.</param>
        /// <param name="forceUTF8">Some server are not returning a valid CharacterSet on the response, set to true to force reading the response content as UTF8.</param>
        /// <param name="allowUnsafeHeader">Some server return headers that are treated as unsafe by .net. In order to retrieve that data set this to true.</param>
        /// <param name="userAgent">You can provide a custom UserAgent for the request, otherwise the default one (<see cref="OnlineVideoSettings.UserAgent"/>) is used.</param>
        /// <param name="encoding">Set an <see cref="Encoding"/> for reading the response data.</param>
        /// <returns>The data returned by a <see cref="HttpWebResponse"/> converted to the specified type.</returns>
        public static T GetWebData<T>(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", userAgent ?? OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            if (referer != null) headers.Add("Referer", referer);
            return GetWebData<T>(url, headers, cc, proxy, forceUTF8, allowUnsafeHeader, encoding, true);
        }

        public static T GetWebData<T>(string url, NameValueCollection headers, CookieContainer cc, IWebProxy proxy, bool forceUTF8, bool allowUnsafeHeader, Encoding encoding, bool cache)
        {
            string webData = GetWebData(url, null, headers, cc, proxy, forceUTF8, allowUnsafeHeader, encoding, cache);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)webData;
            }
            else if (typeof(T) == typeof(Newtonsoft.Json.Linq.JToken))
            {
                return (T)(object)Newtonsoft.Json.Linq.JToken.Parse(webData);
            }
            else if (typeof(T) == typeof(Newtonsoft.Json.Linq.JObject))
            {
                return (T)(object)Newtonsoft.Json.Linq.JObject.Parse(webData);
            }
            else if (typeof(T) == typeof(RssToolkit.Rss.RssDocument))
            {
                return (T)(object)RssToolkit.Rss.RssDocument.Load(webData);
            }
            else if (typeof(T) == typeof(XmlDocument))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(webData);
                return (T)(object)xmlDoc;
            }
            else if (typeof(T) == typeof(System.Xml.Linq.XDocument))
            {
                return (T)(object)System.Xml.Linq.XDocument.Parse(webData);
            }
            else if (typeof(T) == typeof(HtmlAgilityPack.HtmlDocument))
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(webData);
                return (T)(object)htmlDoc;
            }

            return default(T);
        }

        public static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", userAgent ?? OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            if (referer != null) headers.Add("Referer", referer);
            return GetWebData(url, null, headers, cc, proxy, forceUTF8, allowUnsafeHeader, encoding, true);
        }

        public static string GetWebDataFromPost(string url, string postData, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", userAgent ?? OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            if (referer != null) headers.Add("Referer", referer);
            return GetWebData(url, postData, headers, cc, proxy, forceUTF8, allowUnsafeHeader, encoding, false);
        }

        public static string GetWebData(string url, string postData, NameValueCollection headers, CookieContainer cc, IWebProxy proxy, bool forceUTF8, bool allowUnsafeHeader, Encoding encoding, bool cache)
        {
            HttpWebResponse response = null;
            try
            {
                // build a CRC of the url and all headers + proxy + cookies for caching
                string requestCRC = Utils.EncryptLine(
                    string.Format("{0}{1}{2}{3}",
                    url,
                    headers != null ? string.Join("&", (from item in headers.AllKeys select string.Format("{0}={1}", item, headers[item])).ToArray()) : "",
                    proxy != null ? proxy.GetProxy(new Uri(url)).AbsoluteUri : "",
                    cc != null ? cc.GetCookieHeader(new Uri(url)) : ""));

                // try cache first
                string cachedData = cache ? WebCache.Instance[requestCRC] : null;
                Log.Debug("GetWebData-{2}{1}: '{0}'", url, cachedData != null ? " (cached)" : "", postData != null ? "POST" : "GET");
                if (cachedData != null) return cachedData;

                // build the request
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats (adds header "AcceptEncoding: gzip,deflate" to the request)
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                if (headers != null) // set user defined headers
                {
                    foreach (var headerName in headers.AllKeys)
                    {
                        switch (headerName.ToLowerInvariant())
                        {
                            case "accept":
                                request.Accept = headers[headerName];
                                break;
                            case "user-agent":
                                request.UserAgent = headers[headerName];
                                break;
                            case "referer":
                                request.Referer = headers[headerName];
                                break;
                            default:
                                request.Headers.Set(headerName, headers[headerName]);
                                break;
                        }
                    }
                }
                if (postData != null)
                {
                    byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = data.Length;
                    request.ProtocolVersion = HttpVersion.Version10;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                // request the data
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream = response.GetResponseStream();

                // UTF8 is the default encoding as fallback
                Encoding responseEncoding = Encoding.UTF8;
                // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                if (!forceUTF8 && encoding == null && response.CharacterSet != null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                // the caller did specify a forced encoding
                if (encoding != null) responseEncoding = encoding;
                // the caller wants to force UTF8
                if (forceUTF8) responseEncoding = Encoding.UTF8;

                using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (cache && response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        protected static List<String> ParseASX(string url)
        {
            string lsAsxData = GetWebData(url).ToLower();
            MatchCollection videoUrls = Regex.Matches(lsAsxData, @"<ref\s+href\s*=\s*\""(?<url>[^\""]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
        }

        protected static string ParseASX(string url, out string startTime)
        {
            startTime = "";
            string lsAsxData = GetWebData(url).ToLower();
            XmlDocument asxDoc = new XmlDocument();
            asxDoc.LoadXml(lsAsxData);
            XmlElement entryElement = asxDoc.SelectSingleNode("//entry") as XmlElement;
            if (entryElement == null) return "";
            XmlElement refElement = entryElement.SelectSingleNode("ref") as XmlElement;
            if (entryElement == null) return "";
            XmlElement startElement = entryElement.SelectSingleNode("starttime") as XmlElement;
            if (startElement != null) startTime = startElement.GetAttribute("value");
            return refElement.GetAttribute("href");
        }

        #endregion

        #region ICustomTypeDescriptor Members

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(null);
        }

        #endregion

        #region ICustomTypeDescriptor Implementation with Fields as Properties

        private PropertyDescriptorCollection _propCache;
        private FilterCache _filterCache;

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(
            Attribute[] attributes)
        {
            bool filtering = (attributes != null && attributes.Length > 0);
            PropertyDescriptorCollection props = _propCache;
            FilterCache cache = _filterCache;

            // Use a cached version if possible
            if (filtering && cache != null && cache.IsValid(attributes))
                return cache.FilteredProperties;
            else if (!filtering && props != null)
                return props;

            // Create the property collection and filter
            props = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor prop in
                TypeDescriptor.GetProperties(
                this, attributes, true))
            {
                props.Add(prop);
            }
            foreach (FieldInfo field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                FieldPropertyDescriptor fieldDesc = new FieldPropertyDescriptor(field);
                if (!filtering || fieldDesc.Attributes.Contains(attributes))
                {
                    props.Add(fieldDesc);
                }
            }

            // Store the computed properties
            if (filtering)
            {
                cache = new FilterCache();
                cache.Attributes = attributes;
                cache.FilteredProperties = props;
                _filterCache = cache;
            }
            else _propCache = props;

            return props;
        }

        public List<FieldPropertyDescriptorByRef> GetUserConfigurationProperties()
        {
            List<FieldPropertyDescriptorByRef> result = new List<FieldPropertyDescriptorByRef>();
            CategoryAttribute attr = new CategoryAttribute("OnlineVideosUserConfiguration");
            var props = ((ICustomTypeDescriptor)this).GetProperties(new Attribute[] { attr });
            foreach (PropertyDescriptor prop in props) if (prop.Attributes.Contains(attr) && prop is FieldPropertyDescriptor) result.Add(new FieldPropertyDescriptorByRef() { FieldPropertyDescriptor = prop as FieldPropertyDescriptor });
            return result;
        }

        public string GetConfigValueAsString(FieldPropertyDescriptorByRef config)
        {
            object result = config.FieldPropertyDescriptor.GetValue(this);
            return result == null ? string.Empty : result.ToString();
        }

        public void SetConfigValueFromString(FieldPropertyDescriptorByRef config, string value)
        {
            object valueConverted = null;
            if (config.FieldPropertyDescriptor.PropertyType.IsEnum) valueConverted = Enum.Parse(config.FieldPropertyDescriptor.PropertyType, value);
            else valueConverted = Convert.ChangeType(value, config.FieldPropertyDescriptor.PropertyType);
            config.FieldPropertyDescriptor.SetValue(this, valueConverted);
        }

        public class FieldPropertyDescriptorByRef : MarshalByRefObject
        {
            internal FieldPropertyDescriptor FieldPropertyDescriptor { get; set; }

            public string DisplayName
            {
                get
                {
                    var attr = FieldPropertyDescriptor.Attributes[typeof(LocalizableDisplayNameAttribute)];
                    if (attr != null && ((LocalizableDisplayNameAttribute)attr).LocalizedDisplayName != null) return ((LocalizableDisplayNameAttribute)attr).LocalizedDisplayName;
                    else return FieldPropertyDescriptor.DisplayName;
                }
            }

            public string Description
            {
                get
                {

                    var descAttr = FieldPropertyDescriptor.Attributes[typeof(DescriptionAttribute)];
                    return descAttr != null ? ((DescriptionAttribute)descAttr).Description : string.Empty;
                }
            }

            public bool IsPassword
            {
                get
                {
                    return FieldPropertyDescriptor.Attributes.Contains(new System.ComponentModel.PasswordPropertyTextAttribute(true));
                }
            }

            public bool IsBool { get { return FieldPropertyDescriptor.PropertyType.Equals(typeof(bool)); } }

            public bool IsEnum { get { return FieldPropertyDescriptor.PropertyType.IsEnum; } }

            public string[] GetEnumValues()
            {
                return Enum.GetNames(FieldPropertyDescriptor.PropertyType);
            }
        }

        public class FieldPropertyDescriptor : PropertyDescriptor
        {
            private FieldInfo _field;

            public FieldPropertyDescriptor(FieldInfo field)
                : base(field.Name,
                    (Attribute[])field.GetCustomAttributes(typeof(Attribute), true))
            {
                _field = field;
            }

            public override bool Equals(object obj)
            {
                FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
                return other != null && other._field.Equals(_field);
            }

            public override int GetHashCode() { return _field.GetHashCode(); }

            public override string DisplayName
            {
                get
                {
                    var attr = _field.GetCustomAttributes(typeof(LocalizableDisplayNameAttribute), false);
                    if (attr.Length > 0)
                        return ((LocalizableDisplayNameAttribute)attr[0]).LocalizedDisplayName;
                    else
                        return base.DisplayName;
                }
            }

            public override bool IsReadOnly { get { return false; } }

            public override void ResetValue(object component) { }

            public override bool CanResetValue(object component) { return false; }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }

            public override Type ComponentType
            {
                get { return _field.DeclaringType; }
            }

            public override Type PropertyType { get { return _field.FieldType; } }

            public override object GetValue(object component)
            {
                return _field.GetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                // only set if changed
                if (_field.GetValue(component) != value)
                {
                    _field.SetValue(component, value);
                    OnValueChanged(component, EventArgs.Empty);

                    // if this field is a user config, set value also in MediaPortal config file
                    object[] attrs = _field.GetCustomAttributes(typeof(CategoryAttribute), false);
                    if (attrs.Length > 0 && ((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration")
                    {
                        string siteName = (component as Sites.SiteUtilBase).Settings.Name;
                        OnlineVideoSettings.Instance.UserStore.SetValue(string.Format("{0}.{1}", Utils.GetSaveFilename(siteName).Replace(' ', '_'), _field.Name), value.ToString());
                    }
                }
            }
        }

        private class FilterCache
        {
            public Attribute[] Attributes;
            public PropertyDescriptorCollection FilteredProperties;
            public bool IsValid(Attribute[] other)
            {
                if (other == null || Attributes == null) return false;

                if (Attributes.Length != other.Length) return false;

                for (int i = 0; i < other.Length; i++)
                {
                    if (!Attributes[i].Match(other[i])) return false;
                }

                return true;
            }
        }

        #endregion
    }

}
