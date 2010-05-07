using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Web;
using System.Xml;
using OnlineVideos.Database;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// The abstract base class for all utilities.
    /// </summary>
    public abstract class SiteUtilBase : ICustomTypeDescriptor
    {
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
                            Log.Error("Could not set Configuration Value: {0}. Error: {1}", field.Name, ex.Message);
                        }
                    }
                    else if (((CategoryAttribute)attrs[0]).Category == "OnlineVideosUserConfiguration")
                    {
                        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Config, "MediaPortal.xml")))
                        {
                            string value = xmlreader.GetValueAsString(OnlineVideoSettings.CFG_SECTION, string.Format("{0}.{1}", ImageDownloader.GetSaveFilename(siteSettings.Name).Replace(' ', '_'), field.Name), "NO_VALUE_FOUND");
                            if (value != "NO_VALUE_FOUND")
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
                                    Log.Error("Could not set Configuration Value: {0}. Error: {1}", field.Name, ex.Message);
                                }
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
        public virtual List<VideoInfo> getPreviousPageVideos()
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// Returns true, if the site supports querying for related videos (e.g. <see cref="YouTubeUtil"/>).
        /// If true, a conext menu entry "show related videos" is added on a video.<br/>
        /// default: false
        /// </summary>
        public virtual bool HasRelatedVideos
        {
            get { return false; }
        }

        /// <summary>
        /// This function should return a list if videos that are related to the given video (e.g. <see cref="YouTubeUtil"/>).
        /// It will only be called when <see cref="HasRelatedVideos"/> returns true.<br/>
        /// default: empty list
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of related of videos.</param>
        /// <returns>a list of <see cref="VideoInfo"/> objects that are related to the input video</returns>
        public virtual List<VideoInfo> getRelatedVideos(VideoInfo video)
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// Returns true, if the site has multiple choices for a video (e.g. <see cref="AppleTrailersUtil"/>).
        /// The GUI will show a details view with a selection of videos, taken from <see cref="getOtherVideoList"/>.<br/>
        /// default: false
        /// </summary>
        public virtual bool HasMultipleVideos
        {
            get { return false; }
        }

        /// <summary>
        /// This function will  be called to retreive a list of videos, that will be displayed in the details view, 
        /// as choices for a given video (e.g. <see cref="AppleTrailersUtil"/>). 
        /// It will only be called when <see cref="HasMultipleVideos"/> returns true.<br/>
        /// default: empty list
        /// </summary>
        /// <param name="video">The base <see cref="VideoInfo"/> object, for which to get a choice of videos.</param>
        /// <returns>a list of <see cref="VideoInfo"/> objects</returns>
        public virtual List<VideoInfo> getOtherVideoList(VideoInfo video)
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// This function will be called to get the urls for playback of a video.<br/>
        /// By default: returns a list with the result from <see cref="getUrl"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of urls.</param>
        /// <returns></returns>
        public virtual List<String> getMultipleVideoUrls(VideoInfo video)
        {
            List<String> urls = new List<String>();
            urls.Add(getUrl(video));
            return  urls;
        }

        /// <summary>
        /// This function will be called when the user selects a video for playback. It should return the absolute url to the video file.<br/>
        /// By default, the <see cref="VideoInfo.VideoUrl"/> fields value will be returned.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> from the list of displayed videos that were returned by this instance previously.</param>
        /// <returns>A valid url or filename.</returns>
        [Obsolete]
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
        public virtual List<VideoInfo> Search(string query)
        {
            return new List<VideoInfo>();
        }

        /// <summary>
        /// Should return a list of <see cref="VideoInfo"/> for the given query limited to the given category.<br/>
        /// default: calls the Search overload without a category parameter
        /// </summary>        
        /// <param name="category">The category to search in.</param>
        /// <param name="query">The user entered query.</param>
        /// <returns>the list of videos matching that search query.</returns>
        public virtual List<VideoInfo> Search(string query, string category)
        {
            return Search(query);
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

        public virtual string GetFileNameForDownload(VideoInfo video, string url)
        {
            string extension = System.IO.Path.GetExtension(new System.Uri(url).LocalPath.Trim(new char[] { '/' }));
            if (extension == string.Empty) extension = System.IO.Path.GetExtension(url);
            if (extension == ".f4v" || extension == ".fid") extension = ".flv";
            string safeName = ImageDownloader.GetSaveFilename(video.Title);
            return safeName + extension;
        }

        public virtual bool isPossibleVideo(string fsUrl)
        {
            if (string.IsNullOrEmpty(fsUrl)) return false;
            string extensionFile = System.IO.Path.GetExtension(fsUrl).ToLower();
            bool isVideo = OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extensionFile);
            if (!isVideo)
            {
                foreach (string anExt in OnlineVideoSettings.Instance.VideoExtensions.Keys) if (fsUrl.Contains(anExt)) { isVideo = true; break; }
            }
            return isVideo;
        }

        # region static helper functions

        public static string GetRedirectedUrl(string url)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.USERAGENT;
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
                Log.Error(ex);
            }
            finally
            {
                if (httpWebresponse != null) httpWebresponse.Close();
            }
            return url;
        }

        public static string GetWebData(string url)
        {
            return GetWebData(url, null, null, null, false);
        }

        public static string GetWebData(string url, CookieContainer cc)
        {
            return GetWebData(url, cc, null, null, false);
        }

        public static string GetWebData(string url, CookieContainer cc, string referer)
        {
            return GetWebData(url, cc, referer, null, false);
        }

        public static string GetWebData(string url, CookieContainer cc, string referer, IWebProxy proxy)
        {
            return GetWebData(url, cc, referer, proxy, false);
        }

        public static string GetWebData(string url, CookieContainer cc, string referer, IWebProxy proxy, bool forceUTF8)
        {
            Log.Debug("get webdata from {0}", url);
            // try cache first
            string cachedData = WebCache.Instance[url];
            if (cachedData != null) return cachedData;

            // request the data
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return "";
            request.UserAgent = OnlineVideoSettings.USERAGENT;
            request.Accept = "*/*";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set refere if give
            if (cc != null) request.CookieContainer = cc; // set cookies if given
            if (proxy != null) request.Proxy = proxy;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream;
            if (response.ContentEncoding.ToLower().Contains("gzip"))
                responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
                responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
            else
                responseStream = response.GetResponseStream();
            Encoding encoding = Encoding.UTF8;
            if (!forceUTF8 && !String.IsNullOrEmpty(response.CharacterSet)) encoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
            using (StreamReader reader = new StreamReader(responseStream, encoding, true))
            {
                string str = reader.ReadToEnd().Trim();
                // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[url] = str;
                return str;
            }
        }

        public static string GetWebDataFromPost(string url, string postData)
        {
            byte[] data = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = OnlineVideoSettings.USERAGENT;
            request.Timeout = 15000;
            request.ContentLength = data.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            using (WebResponse response = request.GetResponse())
            {
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                string str = reader.ReadToEnd();
                return str.Trim();
            }
        }

        protected static object GetWebDataAsJson(string url)
        {
            string WebData = GetWebData(url);
            try
            {
                // attempts to convert the returned string into a Json object
                object data = Jayrock.Json.Conversion.JsonConvert.Import(WebData);
                return data;
            }
            catch (Exception e)
            {
                Log.Error("Error parsing results from {0} as JSON: {1}", url, e.Message);
            }
            return null;
        }

        protected static RssToolkit.Rss.RssDocument GetWebDataAsRss(string url)
        {
            try
            {
                return RssToolkit.Rss.RssDocument.Load(GetWebData(url));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new RssToolkit.Rss.RssDocument();
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
                FieldPropertyDescriptor fieldDesc =
                    new FieldPropertyDescriptor(field);
                if (!filtering ||
                    fieldDesc.Attributes.Contains(attributes))
                    props.Add(fieldDesc);
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

        private class FieldPropertyDescriptor : PropertyDescriptor
        {
            private FieldInfo _field;

            public FieldPropertyDescriptor(FieldInfo field)
                : base(field.Name,
                    (Attribute[])field.GetCustomAttributes(typeof(Attribute), true))
            {
                _field = field;
            }

            public FieldInfo Field { get { return _field; } }

            public override bool Equals(object obj)
            {
                FieldPropertyDescriptor other = obj as FieldPropertyDescriptor;
                return other != null && other._field.Equals(_field);
            }

            public override int GetHashCode() { return _field.GetHashCode(); }

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
                        using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(MediaPortal.Configuration.Config.GetFile(MediaPortal.Configuration.Config.Dir.Config, "MediaPortal.xml")))
                        {
                            string siteName = (component as Sites.SiteUtilBase).Settings.Name;
                            xmlreader.SetValue(OnlineVideoSettings.CFG_SECTION, string.Format("{0}.{1}", ImageDownloader.GetSaveFilename(siteName).Replace(' ', '_'), _field.Name), value.ToString());
                        }
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
