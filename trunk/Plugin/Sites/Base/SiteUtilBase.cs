using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using MediaPortal.GUI.Library;
using System.ComponentModel;
using System.Web;
using System.Net;
using System.Xml;
using System.Xml.XPath;
using System.Threading;
using OnlineVideos.Database;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
    public abstract class SiteUtilBase
    {
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

        public abstract List<VideoInfo> getVideoList(Category category);

        public virtual List<VideoInfo> getRelatedVideos(String fsTags)
        {
            return new List<VideoInfo>();
        }

        public virtual List<Category> getDynamicCategories()
        {
            return new List<Category>();
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

        public virtual void AddFavorite(VideoInfo foVideo, String fsSiteId)
        {
            FavoritesDatabase db = FavoritesDatabase.getInstance();
            db.addFavoriteVideo(foVideo, fsSiteId);
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

        protected static string GetRedirectedUrl(string url)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
                if (httpWebRequest == null) return url;
                httpWebRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
                httpWebRequest.Timeout = 10000;                
                HttpWebResponse httpWebresponse = httpWebRequest.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;
                if (httpWebRequest.RequestUri.Equals(httpWebresponse.ResponseUri))
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
        
        protected static string GetWebData(string fsUrl)
        {            
            HttpWebRequest request = WebRequest.Create(fsUrl) as HttpWebRequest;
            if (request == null) return "";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            request.Timeout = 20000;            
            WebResponse response = request.GetResponse();
            using (System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                string str = reader.ReadToEnd();
                return str.Trim();
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

        protected List<String> parseASX(String fsAsxUrl)
        {
            String lsAsxData = GetWebData(fsAsxUrl);
            MatchCollection videoUrls = Regex.Matches(lsAsxData, "<Ref\\shref\\s=\\s\"(?<url>[^\"]*)");
            List<String> urlList = new List<String>();
            foreach (Match videoUrl in videoUrls)
            {
                urlList.Add(videoUrl.Groups["url"].Value);
            }
            return urlList;
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
    }
}
