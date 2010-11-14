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
    public class OnsNetNuenenUtil : GenericSiteUtil
    {
        private string mainPageRegex = @"<div\sclass=""NewVideoThumb""><a\shref=""index\.php\?visid=(?<id>[^&]+)&[^<]+<img\swidth=""[^""]+""\ssrc=""(?<thumb>[^""]+)""\stitle=""(?<title>[^""]+)""";
        private string searchRegex = @"<div\sclass=""SearchVideoThumb""><a\shref=""index\.php\?(?:(?!visid).)*visid=(?<id>[^&]+)&[^<]+<img\ssrc=""(?<thumb>[^""]+)""[^>]*>[^>]*>(?<title>[^<]*)<";

        public OnsNetNuenenUtil()
        {
        }

        private Regex regEx_MainPage;
        private Regex regEx_Search;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_MainPage = new Regex(mainPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Search = new Regex(searchRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {

            RssLink cat2 = new RssLink();
            cat2.Name = "Beste video's";
            cat2.Url = "!http://www.onsnet.tv/corp/index.php?ord=hig&page=48&site=4&ln=nl";
            Settings.Categories.Add(cat2);

            cat2 = new RssLink();
            cat2.Name = "Nieuwste video's";
            cat2.Url = "!http://www.onsnet.tv/corp/index.php?ord=new&page=48&site=4&ln=nl";
            Settings.Categories.Add(cat2);

            return base.DiscoverDynamicCategories() + 2;
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            return getVideoList(String.Format("http://www.onsnet.tv/corp/?page=54&site=4&search=1&srch_str={0}&x=0&y=0&submit_frmSearch=1", query),
                regEx_Search);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            if (!url.StartsWith("!")) return base.getVideoList(category);
            else
                return getVideoList(url.Substring(1), regEx_MainPage);
        }

        private List<VideoInfo> getVideoList(string url, Regex regex)
        {
            string webData = GetWebData(url);
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regex.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                    video.VideoUrl = string.Format(videoListRegExFormatString, m.Groups["id"].Value);
                    video.ImageUrl = new Uri(new Uri(baseUrl), HttpUtility.HtmlDecode(m.Groups["thumb"].Value)).AbsoluteUri;
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }
    }
}
