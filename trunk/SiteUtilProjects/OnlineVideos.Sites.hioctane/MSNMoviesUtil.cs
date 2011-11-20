using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
	/*public class MSNMoviesUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("XML Path used to parse the videoLicensor for videoList.")]
        protected string videoLicensorXml;

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                XmlNodeList videoItems = doc.SelectNodes(videoItemXml);
                for (int i = 0; i < videoItems.Count; i++)
                {
                    if (!string.IsNullOrEmpty(videoTitleXml) && !string.IsNullOrEmpty(videoUrlXml))
                    {
                        VideoInfo videoInfo = new VideoInfo();
                        videoInfo.Title = HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitleXml).InnerText);
                        if (!String.IsNullOrEmpty(videoTitle2Xml))
                            videoInfo.Title += ' ' + HttpUtility.HtmlDecode(videoItems[i].SelectSingleNode(videoTitle2Xml).InnerText); ;

                        videoInfo.VideoUrl = videoItems[i].SelectSingleNode(videoUrlXml).InnerText;
                        if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                        if (!string.IsNullOrEmpty(videoThumbXml)) videoInfo.ImageUrl = videoItems[i].SelectSingleNode(videoThumbXml).InnerText;
                        if (!string.IsNullOrEmpty(videoThumbFormatString)) videoInfo.ImageUrl = string.Format(videoThumbFormatString, videoInfo.ImageUrl);
                        if (!string.IsNullOrEmpty(videoDurationXml)) videoInfo.Length = Regex.Replace(videoItems[i].SelectSingleNode(videoDurationXml).InnerText, "(<[^>]+>)", "");
                        if (!string.IsNullOrEmpty(videoDescriptionXml)) videoInfo.Description = videoItems[i].SelectSingleNode(videoDescriptionXml).InnerText;
                        if (!string.IsNullOrEmpty(videoLicensorXml)) videoInfo.Title2 = videoItems[i].SelectSingleNode(videoLicensorXml).InnerText;
                        videos.Add(videoInfo);
                    }
                }
            }
            return videos;
        }

        public override String getUrl(VideoInfo video)
        {
            string postData = string.Format(@"<XML totalplaytime=""{0}"" licensor=""{1}"" content_title=""{2}"" content_id=""{3}"" />",video.Length,video.Title2,video.Title,video.VideoUrl);
            string data = GetWebDataFromPost(video.VideoUrl,postData);

            string url = Regex.Match(data, @"<ref\shref=""(?<url>[^""]+)""/>").Groups["url"].Value;
            string id = Regex.Match(data, @"<param\sname=""id""\svalue=""(?<url>[^""]+)""/>").Groups["url"].Value;

            string s = Regex.Match(data, @"<param\sname=""s""\svalue=""(?<url>[^""]+)""/>").Groups["url"].Value;
            string e = Regex.Match(data, @"<param\sname=""e""\svalue=""(?<url>[^""]+)""/>").Groups["url"].Value;
            string h = Regex.Match(data, @"<param\sname=""h""\svalue=""(?<url>[^""]+)""/>").Groups["url"].Value;
            string ip = Regex.Match(data, @"<param\sname=""ip""\svalue=""(?<url>[^""]+)""/>").Groups["url"].Value;

            string rtmpeUrl = url + id + "?s=" + s + "&e=" + e + "&h=" + h + "&ip=" + ip;
            string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
            string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
            string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
            string playpath = url.Substring(url.IndexOf(app) + app.Length + 1);
            playpath = "mp4:" + playpath + id.Replace("mp4:", "") + "?s=" + s + "&e=" + e + "&h=" + h + "&ip=" + ip;

            string resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&playpath={7}",
                    rtmpeUrl, //rtmpUrl
                    host, //host
                    tcUrl, //tcUrl
                    app, //app
                    "http://movies.msn.de/player/msn_avod_player_07_live.swf", //swfurl
                    "657101", //swfsize
                    "155bf8dde22372d502ec792d2d7b6f3e51ecd844b29dd349d8dc43b9351aca08", //swfhash
                    HttpUtility.UrlEncode(playpath) //playpath
                ));
            return resultUrl;

        }
    }*/
}