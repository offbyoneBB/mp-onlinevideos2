using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
//using RssToolkit.Rss;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TurboNickUtil : SiteUtilBase
    {
        private int pageNr = 1;
        private bool hasNextPage = false;
        private RssLink bareCategory;

        private string baseUrl = "http://www.nickelodeon.nl";
        private string categoryRegex = @"<span\sclass=""list_image"">\s+<a\shref=""(?<url>[^""]+)""\stitle=""(?<title>[^""]+)"">\s+<img\ssrc=""(?<img>[^""]+)""";
        private string videoListRegex = @"<div\sclass=""inner_column"">\s+<span\sclass=""inner_column_image"">\s+<a\shref=""(?<url>[^""]+)""\stitle=""[^""]+"">\s+<img\ssrc=""(?<thumb>[^""]+)""\salt=""(?<title>[^""]+)""";

        public TurboNickUtil()
        {
        }

        private Regex regEx_Category;
        private Regex regEx_VideoList;
        private string popUrl = "@POP";
        private string newUrl = "@NEW";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl + "/turbonick");
            if (!string.IsNullOrEmpty(data))
            {
                RssLink cat2 = new RssLink();
                cat2.Name = "Populaire video's";
                cat2.Url = popUrl;
                Settings.Categories.Add(cat2);

                cat2 = new RssLink();
                cat2.Name = "Nieuwe video's";
                cat2.Url = newUrl;
                Settings.Categories.Add(cat2);

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    string url = baseUrl + m.Groups["url"].Value;
                    cat.Url = url;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }


                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string url = video.VideoUrl;
            int p1 = url.LastIndexOf('/');
            int p2 = url.LastIndexOf('/', p1 - 1);
            string videoId = url.Substring(p2 + 1, p1 - p2 - 1);
            string xmlData = GetWebData(String.Format(@"http://www.nickelodeon.nl/feeds/turbonick/mediaGen.php?id={0}", videoId));
            XmlDocument tdoc = new XmlDocument();
            tdoc.LoadXml(xmlData);
            XmlNodeList sources = tdoc.SelectNodes("//rendition");
            int HighestBitrate = 0;
            string theOne = null;
            foreach (XmlNode source in sources)
            {
                int bitRate = Convert.ToInt32(source.Attributes["bitrate"].Value);
                if (bitRate > HighestBitrate)
                {
                    HighestBitrate = bitRate;
                    theOne = source.SelectSingleNode("src").InnerText;
                }
            }

            if (theOne != null && theOne.StartsWith("rtmp"))
                return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(theOne));
            return theOne;
        }

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(bareCategory);
        }

        public override bool HasPreviousPage
        {
            get { return pageNr > 1; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            bareCategory = rssLink;
            pageNr = 1;
            return getPagedVideoList(rssLink);
        }

        private List<VideoInfo> getPagedVideoList(RssLink category)
        {
            string url = category.Url;
            if (pageNr > 1)
            {
                int p = url.LastIndexOf('/');
                url = url.Insert(p, '/' + pageNr.ToString());
            }

            string webData = null;
            if (url == popUrl || url == newUrl)
            {
                webData = GetWebData(baseUrl + "/turbonick");
                if (!String.IsNullOrEmpty(webData))
                {
                    string[] tt = { "<h4>" };
                    string[] tmp = webData.Split(tt, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in tmp)
                    {
                        if ((s.StartsWith("Populair") && url == popUrl) ||
                            (s.StartsWith("Nieuwe") && url == newUrl))
                        {
                            webData = s;
                            break;
                        }
                    }
                }
            }
            else
                webData = GetWebData(url);
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }

                hasNextPage = webData.IndexOf(@">Volgende<") != -1;
            }
            return videos;

        }

    }
}
