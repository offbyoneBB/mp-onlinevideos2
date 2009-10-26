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
using OnlineVideos.Database;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// The abstract base class for all utilities.
    /// </summary>
    public abstract class SiteUtilBase : ICustomTypeDescriptor
    {
        public virtual SiteSettings Settings { get; protected set; }

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
                            string value = xmlreader.GetValueAsString(OnlineVideoSettings.SECTION, string.Format("{0}.{1}", ImageDownloader.GetSaveFilename(siteSettings.Name).Replace(' ', '_'), field.Name), "NO_VALUE_FOUND");
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

        public abstract List<VideoInfo> getVideoList(Category category);                       

        public virtual List<VideoInfo> getSiteFavorites(String fsUser)
        {
            return new List<VideoInfo>();
        }

        public virtual List<VideoInfo> getRelatedVideos(String fsTags)
        {
            return new List<VideoInfo>();
        }

        public virtual int DiscoverDynamicCategories()
        {
            Settings.DynamicCategoriesDiscovered = true;
            return 0;
        }

        public virtual int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategoriesDiscovered = true;
            return 0;
        }

        public virtual String getUrl(VideoInfo video)
        {
            return video.VideoUrl;
        }

        public virtual bool HasNextPage
        {
            get { return false; }
        }

        public virtual List<VideoInfo> getNextPageVideos()
        {
            return new List<VideoInfo>();
        }

        public virtual bool HasPreviousPage
        {
            get { return false; }
        }

        public virtual List<VideoInfo> getPreviousPageVideos()
        {
            return new List<VideoInfo>();
        }

        public virtual void AddFavorite(VideoInfo foVideo)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            db.addFavoriteVideo(foVideo, Settings.Name);
        }

        public virtual bool RemoveFavorite(VideoInfo foVideo)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            return db.removeFavoriteVideo(foVideo);
        }

        public virtual bool HasMultipleVideos
        {
            get { return false; }
        }

        public virtual List<VideoInfo> getOtherVideoList(VideoInfo foVideo)
        {
            return new List<VideoInfo>();
        }

        public virtual bool MultipleFilePlay
        {
            get { return false; }
        }

        public virtual List<String> getMultipleVideoUrls(VideoInfo video)
        {
            return new List<String>();
        }

        public virtual string GetFileNameForDownload(VideoInfo video, string url)
        {
            string extension = System.IO.Path.GetExtension(url);
            string safeName = ImageDownloader.GetSaveFilename(video.Title);
            return safeName + extension;
        }

        public virtual bool isPossibleVideo(string fsUrl)
        {
            string extensionFile = System.IO.Path.GetExtension(fsUrl).ToLower();
            bool isVideo = OnlineVideoSettings.getInstance().videoExtensions.ContainsKey(extensionFile);
            if (!isVideo)
            {
                foreach (string anExt in OnlineVideoSettings.getInstance().videoExtensions.Keys) if (fsUrl.Contains(anExt)) { isVideo = true; break; }
            }
            return isVideo;
        }

        # region static helper functions

        protected static string GetRedirectedUrl(string url)
        {            
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;                
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.UserAgent;
                request.Timeout = 15000;
                HttpWebResponse httpWebresponse = request.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;
                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                {
                    if (httpWebresponse.ContentLength > 0 && httpWebresponse.ContentLength < 1024)
                        return GetUrlFromResponse(httpWebresponse);
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
            return url;
        }

        protected static string GetUrlFromResponse(HttpWebResponse httpWebresponse)
        {   
            StreamReader sr = new StreamReader(httpWebresponse.GetResponseStream());                        
            string content = sr.ReadToEnd();
            if (httpWebresponse.ContentType.Contains("video/quicktime"))
            {
                return content.Split('\n')[1];
            }
            return httpWebresponse.ResponseUri.ToString();
        }

        protected static string GetWebData(string url, CookieContainer cc)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return "";
            request.UserAgent = OnlineVideoSettings.UserAgent;
            request.Timeout = 15000;
            if (cc != null) request.CookieContainer = cc;
            WebResponse response = request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string str = reader.ReadToEnd();
                return str.Trim();
            }
        }

        protected static string GetWebData(string url)
        {
            return GetWebData(url, null);
        }
        
        protected static string GetWebDataFromPost(string url, string postData)
        {
            byte[] data = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = OnlineVideoSettings.UserAgent;
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
                            xmlreader.SetValue(OnlineVideoSettings.SECTION, string.Format("{0}.{1}", ImageDownloader.GetSaveFilename(siteName).Replace(' ', '_'), _field.Name), value.ToString());
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
