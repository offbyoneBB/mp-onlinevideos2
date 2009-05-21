using System;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using MediaPortal.Player;
using System.Collections.Generic;
using MediaPortal.GUI.View;
using MediaPortal.Dialogs;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class MyVideoUtil : SiteUtilBase
    {
        static Regex videoUrlRegEx = new Regex(@"V=(http[^&]+\.flv)");

        public override String getUrl(VideoInfo video, SiteSettings site)
        {
            String lsUrl = "";
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(String.Format("http://www.myvideo.de/movie/{0}", video.VideoUrl));
            webrequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            webrequest.KeepAlive = false;
            webrequest.Method = "GET";
            webrequest.ContentType = "text/html";
            webrequest.AllowAutoRedirect = true;
            webrequest.MaximumAutomaticRedirections = 2;
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();            
            Match m = videoUrlRegEx.Match(webresponse.ResponseUri.ToString());
            if (m.Success) lsUrl = m.Groups[1].Value;
            return lsUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsUrl = (category as RssLink).Url;
            List<RssItem> loRssItemList = getRssDataItems(fsUrl);
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;
            foreach (RssItem rssItem in loRssItemList)
            {
                video = new VideoInfo();
                video.Description = rssItem.description;
                video.ImageUrl = rssItem.mediaThumbnail;
                video.Title = rssItem.title;
                video.VideoUrl = Regex.Match(rssItem.link, "watch/([\\d]*)").Groups[1].Value;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
    }
}
