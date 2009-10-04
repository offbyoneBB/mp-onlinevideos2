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
        static Regex infoRegEx = new Regex(@"\</a\>(?<desc>.*)Stichwörter\:.*Länge\:\s(?<duration>.*)\<br/\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override String getUrl(VideoInfo video)
        {
            String lsUrl = "";
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(String.Format("http://www.myvideo.de/movie/{0}", video.VideoUrl));
            webrequest.UserAgent = OnlineVideoSettings.UserAgent;
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
                rssItem.description = rssItem.description.Replace("\n", "<br/>");
                Match mInfo = infoRegEx.Match(rssItem.description);

                video = new VideoInfo();
                video.Description = mInfo.Groups["desc"].Value;
                video.Length = mInfo.Groups["duration"].Value;
                video.ImageUrl = rssItem.mediaThumbnail;
                video.Title = rssItem.title;
                video.VideoUrl = Regex.Match(rssItem.link, "watch/([\\d]*)").Groups[1].Value;
                loVideoList.Add(video);
            }
            return loVideoList;
        }
    }
}
