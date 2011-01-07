using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MyVideoUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to get the actual file url from the video link.")]
        string videoUrlRegEx = @"V=(http[^&]+\.flv)";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the rss item description.")]
        string infoRegEx = @"\</a\>(?<desc>.*)Stichwörter\:.*Länge\:\s(?<duration>[0-9\:]+)\<br/\>";

        Regex regEx_VideoUrl, regEx_Info;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(videoUrlRegEx)) regEx_VideoUrl = new Regex(videoUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            if (!string.IsNullOrEmpty(infoRegEx)) regEx_Info = new Regex(infoRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public override String getUrl(VideoInfo video)
        {
            /*
            string data = GetWebData(string.Format("http://88.198.16.200/myvideo/?content_id={0}", Regex.Match(video.VideoUrl, @".+/(?<id>\d+)/.*").Groups["id"].Value));
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(data);
            string url = xDoc.SelectSingleNode("//param[@name='server']/@value").Value;
            string playpath = xDoc.SelectSingleNode("//param[@name='path']/@value").Value;
            int indexQ = url.IndexOf("?token=");
            string token = url.Substring(indexQ + "?token=".Length);
            url = url.Substring(0, indexQ);
            string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&token={2}",
                    System.Web.HttpUtility.UrlEncode(url),
                    System.Web.HttpUtility.UrlEncode(playpath),
                    System.Web.HttpUtility.UrlEncode(token)
                ));
            return resultUrl;
            */
            string videoUrl = video.ImageUrl.Replace("/thumbs", "").Replace(".jpg", ".flv");
            videoUrl = Regex.Replace(videoUrl, @"(?<before>.*\d+)_\d*(?<after>.flv)", "${before}${after}");
            return videoUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {            
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebData<RssDocument>(((RssLink)category).Url).Channel.Items)
            {
                Match mInfo = regEx_Info.Match(rssItem.Description);
                if (mInfo.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Description = mInfo.Groups["desc"].Value.Replace("<br />", "\n").Trim();
                    video.Length = mInfo.Groups["duration"].Value;                    
                    video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                    video.Title = rssItem.MediaTitle;
                    video.VideoUrl = rssItem.Link;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }
    }
}
