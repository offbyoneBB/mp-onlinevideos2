using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using OnlineVideos.Database;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
    public abstract class SiteUtilBase
    {
        public abstract List<VideoInfo> getVideoList(Category category);

        string name = string.Empty;
        public virtual string Name
        {
            get
            {
                if (name == string.Empty)
                {
                    name = GetType().Name;
                    if (name.EndsWith("Util")) name = name.Substring(0, name.Length - 4);
                }
                return name;
            }
        }

        public virtual List<VideoInfo> getSiteFavorites(String fsUser)
        {
            return new List<VideoInfo>();
        }

        public virtual List<VideoInfo> getRelatedVideos(String fsTags)
        {
            return new List<VideoInfo>();
        }

        public virtual int DiscoverDynamicCategories(SiteSettings site)
        {
            site.DynamicCategoriesDiscovered = true;
            return 0;
        }

        public virtual String getUrl(VideoInfo video, SiteSettings foSite)
        {
            return video.VideoUrl;
        }

        public virtual bool hasNextPage()
        {
            return false;
        }

        public virtual List<VideoInfo> getNextPageVideos()
        {
            return new List<VideoInfo>();
        }

        public virtual bool hasPreviousPage()
        {
            return false;
        }

        public virtual List<VideoInfo> getPreviousPageVideos()
        {
            return new List<VideoInfo>();
        }

        public virtual void AddFavorite(VideoInfo foVideo, SiteSettings site)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            db.addFavoriteVideo(foVideo, site.Name);
        }

        public virtual bool RemoveFavorite(VideoInfo foVideo)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            return db.removeFavoriteVideo(foVideo);
        }

        public virtual bool hasMultipleVideos()
        {
            return false;
        }

        public virtual List<VideoInfo> getOtherVideoList(VideoInfo foVideo)
        {
            return new List<VideoInfo>();
        }

        protected virtual List<RssItem> getRssDataItems(string fsUrl)
        {
            try
            {
                return RssWrapper.GetRssItems(GetWebData(fsUrl));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new List<RssItem>();
            }
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

        public virtual bool MultipleFilePlay()
        {
            return false;
        }

        public virtual List<String> getMultipleVideoUrls(VideoInfo video, SiteSettings foSite)
        {
            return new List<String>();
        }

        public virtual bool hasLoginSupport()
        {
            return false;
        }

        protected static string GetRedirectedUrl(string url)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;                
                if (request == null) return url;
                request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
                request.Timeout = 15000;
                HttpWebResponse httpWebresponse = request.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;
                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                    return url;
                else
                    return httpWebresponse.ResponseUri.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return url;
        }

        protected static string GetWebData(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return "";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            request.Timeout = 15000;
            WebResponse response = request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                string str = reader.ReadToEnd();
                return str.Trim();
            }
        }
        
        protected static string GetWebDataFromPost(string url, string postData)
        {
            byte[] data = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
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

        protected static List<String> ParseASX(String fsAsxUrl)
        {
            String lsAsxData = GetWebData(fsAsxUrl).ToLower();
            MatchCollection videoUrls = Regex.Matches(lsAsxData, "<ref\\shref\\s=\\s\"(?<url>[^\"]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
        }
    }
}
