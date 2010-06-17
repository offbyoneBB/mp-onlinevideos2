using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Xml;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class SouthParkUtil : GenericSiteUtil
    {

        public enum VideoQuality { low, high }

        VideoQuality videoQuality = VideoQuality.high;

        Regex episodePlayerRegEx = new Regex(@"swfobject.embedSWF\(""(?<url>[^""]*)""", RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.Name = "Season " + cat.Name;
            return res;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = base.getVideoList(category);
            foreach (VideoInfo video in res)
            {
                string[] tmp = video.Length.Split('|');
                if (tmp.Length == 2)
                {
                    video.Length = tmp[1];
                    video.Title = tmp[0] + ": " + video.Title;
                }
            }
            return res;
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            List<string> result = new List<string>();
            string tmmpUrl;
            if (regEx_FileUrl != null)
            {
                tmmpUrl = base.getUrl(video);
            }
            else
                tmmpUrl = video.VideoUrl;

            string data = GetWebData(tmmpUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = episodePlayerRegEx.Match(data);
                if (m.Success)
                {
                    string playerUrl = m.Groups["url"].Value;
                    playerUrl = GetRedirectedUrl(playerUrl);
                    playerUrl = System.Web.HttpUtility.ParseQueryString(new Uri(playerUrl).Query)["uri"];
                    playerUrl = System.Web.HttpUtility.UrlDecode(playerUrl);
                    playerUrl = new Uri(new Uri(baseUrl), @"/feeds/as3player/mrss.php?uri=" + playerUrl).AbsoluteUri;
                    data = GetWebData(playerUrl);
                    if (!string.IsNullOrEmpty(data))
                    {
                        foreach (RssItem item in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                        {
                            data = GetWebData(item.MediaGroups[0].MediaContents[0].Url);
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(data);

                            XmlNodeList list = doc.SelectNodes("//src");
                            string url = list[0].InnerText;
                            int bitrate = Convert.ToInt32(list[0].ParentNode.Attributes[1].Value);
                            string videoType = list[0].ParentNode.Attributes[4].Value;
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (videoQuality == VideoQuality.high)
                                {
                                    if (bitrate < Convert.ToInt32(list[i].ParentNode.Attributes[1].Value))
                                    {
                                        bitrate = Convert.ToInt32(list[i].ParentNode.Attributes[1].Value);
                                        url = list[i].InnerText;
                                        videoType = list[i].ParentNode.Attributes[4].Value;
                                    }
                                }
                                else
                                {
                                    if (bitrate > Convert.ToInt32(list[i].ParentNode.Attributes[1].Value))
                                    {
                                        bitrate = Convert.ToInt32(list[i].ParentNode.Attributes[1].Value);
                                        url = list[i].InnerText;
                                        videoType = list[i].ParentNode.Attributes[4].Value;
                                    }
                                }
                            }
                            if (url.Contains("intro")) continue;

                            if (url.StartsWith("rtmpe://")) url = url.Replace("rtmpe://", "rtmp://");

                            string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfsize={1}&swfhash={2}",
                                    System.Web.HttpUtility.UrlEncode(url)
                                    , "563963",
                                    "1155163cece179766c97fedce8933ccfccb9a553e47c1fabbb5faeacc4e8ad70"));
                            result.Add(resultUrl);
                        }
                    }
                }
            }
            return result;
        }

    }
}
