using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class SouthParkDeUtil : SiteUtilBase
    {
        Regex seasonsRegEx = new Regex(@"href=""(?<url>/episodenguide/staffel/(?<season>\d{1,2})/)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        Regex episodesRegEx = new Regex(@"<li\sclass=""grid_item"">\s*
<div\sclass=""image"">\s*
(?:(?!<img).)*
<img\ssrc=""(?<thumb>[^""]*)""
(?:(?!<a).)*
<a\sclass=""overlay""\shref=""/episodenguide/folge/(?<episode>\d{3,4})/"">\s*
<span\sclass=""epnumber"">[^<]*</span>\s*
<span\sclass=""title\septitle"">(?<title>[^<]*)</span>\s*
<span\sclass=""epdate"">(?<airdate>[^<]*)</span>\s*
<span\sclass=""epdesc"">(?<desc>[^<]*)</span>\s*
</a>", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        Regex episodePlayerRegEx = new Regex(@"swfobject.embedSWF\(""(?<url>[^""]*)""", RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData("http://www.southpark.de/alleepisoden/");
            if (!string.IsNullOrEmpty(data))
            {
                Settings.Categories.Clear();

                Match m = seasonsRegEx.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = "Season " + m.Groups["season"].Value;
                    cat.Url = "http://www.southpark.de" + m.Groups["url"].Value;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }

                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = episodesRegEx.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = string.Format("Episode {0}: {1}", m.Groups["episode"].Value, m.Groups["title"].Value);
                    video.Description = m.Groups["desc"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;
                    video.Length = m.Groups["airdate"].Value;
                    video.VideoUrl = "http://www.southpark.de/alleEpisoden/" + m.Groups["episode"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override bool MultipleFilePlay
        {
            get { return true; }
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            List<string> result = new List<string>();
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = episodePlayerRegEx.Match(data);
                if (m.Success)
                {
                    string playerUrl = m.Groups["url"].Value;
                    playerUrl = GetRedirectedUrl(playerUrl);
                    playerUrl = System.Web.HttpUtility.ParseQueryString(new Uri(playerUrl).Query)["uri"];
                    playerUrl = System.Web.HttpUtility.UrlDecode(playerUrl);
                    playerUrl = @"http://www.southpark.de/feeds/as3player/mrss.php?uri=" + playerUrl;
                    data = GetWebData(playerUrl);
                    if (!string.IsNullOrEmpty(data))
                    {
                        foreach (RssItem item in RssWrapper.GetRssItems(data))
                        {
                            //if (!item.title.ToLower().Contains("vorspann"))
                            //{
                                data = GetWebData(item.contentList[0].url);
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(data);
                                XmlNodeList list = doc.SelectNodes("//src");
                                string url = list[list.Count - 1].InnerText; // todo : quality selection in configuration
                                if (url.StartsWith("rtmpe://")) url = url.Replace("rtmpe://", "rtmp://");
                                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfsize={2}&swfhash={3}",
                                    OnlineVideoSettings.RTMP_PROXY_PORT,
                                    System.Web.HttpUtility.UrlEncode(url)
                                    , "563963",
                                    "1155163cece179766c97fedce8933ccfccb9a553e47c1fabbb5faeacc4e8ad70");
                                result.Add(resultUrl);
                            //}
                        }
                    }
                }
            }
            return result;
        }

        public override String getUrl(VideoInfo video)
        {
            string result = "";
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = episodePlayerRegEx.Match(data);
                if (m.Success)
                {
                    string playerUrl = m.Groups["url"].Value;
                    playerUrl = GetRedirectedUrl(playerUrl);
                    playerUrl = System.Web.HttpUtility.ParseQueryString(new Uri(playerUrl).Query)["uri"];
                    playerUrl = System.Web.HttpUtility.UrlDecode(playerUrl);
                    playerUrl = @"http://www.southpark.de/feeds/as3player/mrss.php?uri=" + playerUrl;
                    data = GetWebData(playerUrl);
                    if (!string.IsNullOrEmpty(data))
                    {
                        List<RssItem> items = RssWrapper.GetRssItems(data);
                        data = GetWebData(items[1].contentList[0].url);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(data);
                        XmlNodeList list = doc.SelectNodes("//src");
                        string url = list[list.Count - 1].InnerText; // todo : quality selection in configuration
                        if (url.StartsWith("rtmpe://")) url = url.Replace("rtmpe://", "rtmp://");
                        result = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfsize={2}&swfhash={3}", 
                            OnlineVideoSettings.RTMP_PROXY_PORT, 
                            System.Web.HttpUtility.UrlEncode(url)
                            , "563963",
                            "1155163cece179766c97fedce8933ccfccb9a553e47c1fabbb5faeacc4e8ad70");
                    }
                }
            }
            return result;
        }
    }
}
