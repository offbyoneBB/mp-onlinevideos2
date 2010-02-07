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
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class MyVideoUtil : SiteUtilBase
    {
        static Regex videoUrlRegEx = new Regex(@"V=(http[^&]+\.flv)");
        static Regex infoRegEx = new Regex(@"\</a\>(?<desc>.*)Stichwörter\:.*Länge\:\s(?<duration>[0-9\:]+)\<br/\>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            List<VideoInfo> loVideoList = new List<VideoInfo>();            
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {                
                Match mInfo = infoRegEx.Match(rssItem.Description);
                if (mInfo.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Description = mInfo.Groups["desc"].Value.Replace("<br />", "\n").Trim();
                    video.Length = mInfo.Groups["duration"].Value;                    
                    video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                    video.Title = rssItem.MediaTitle;
                    video.VideoUrl = Regex.Match(rssItem.Link, "watch/([\\d]*)").Groups[1].Value;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }
    }
}
