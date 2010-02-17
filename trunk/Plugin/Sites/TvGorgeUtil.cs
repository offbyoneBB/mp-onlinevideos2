using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TvGorgeUtil : SiteUtilBase
    {
        private string baseUrl = "http://tvgorge.com";

        private string categoryRegex = @"<li><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<";
        private string seasonRegex = @"<h3[^<]+<a\shref=""(?<url>[^""]+)""[^>]+>(?<title>[^<]+)<";
        private string videoListRegex = @"<div\sclass=""episode"">\s*<h4>\s*<a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<.*?<img\ssrc=""(?<thumb>[^""]+)"".*?Aired On:</span>\s*(?<date>[^<]+)<.*?<p>\s*(?<descr>[^<]+)<.*?Episode\s*(?<episode>[^\s]+)";
        //;
        private string urlRegex = @",file:""(?<url>[^""]+)""";

        public TvGorgeUtil()
        {
        }

        private Regex regEx_Category;
        private Regex regEx_Season;
        private Regex regEx_VideoList;
        private Regex regEx_GetUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Season = new Regex(seasonRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_GetUrl = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl + @"/watch-tv-online");
            data = GetSubString(data, "content-box-bg", "bottom-content-box");
            if (!string.IsNullOrEmpty(data))
            {

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = baseUrl + m.Groups["url"].Value;
                    cat.HasSubCategories = true;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }

                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;

            SortedDictionary<int, Category> categories = new SortedDictionary<int, Category>();
            string webData = GetWebData(url);
            webData = GetSubString(webData, "<ul>", "</ul>");


            if (!string.IsNullOrEmpty(webData))
            {

                Match m = regEx_Season.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = baseUrl + m.Groups["url"].Value;
                    cat.HasSubCategories = false;
                    int p = cat.Name.LastIndexOf(' ');
                    if (!int.TryParse(cat.Name.Substring(p + 1), out p))
                        p = 0;

                    categories.Add(p, cat);
                    cat.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }

            parentCategory.SubCategories = new List<Category>(categories.Values);
            return parentCategory.SubCategories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            string id = GetSubString(data, @"var aiv = """, @":");
            data = GetWebData(String.Format(@"http://tvgorge.com/includes/ajax/ad1s.php?ai={0}", id));
            string url = GetSubString(data, @"streamer=", "&");
            return String.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfurl={2}",
                    OnlineVideoSettings.RTMP_PROXY_PORT,
                    System.Web.HttpUtility.UrlEncode(url + "/" + id),
                    @"http://tvgorge.com/ad1zs/adplayer.swf"
                    );
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string webData = GetWebData(((RssLink)category).Url);
            SortedDictionary<int, VideoInfo> videos = new SortedDictionary<int, VideoInfo>();

            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);
                    int nr;

                    if (!int.TryParse(m.Groups["episode"].Value, out nr))
                        nr = 0;
                    if (!videos.ContainsKey(nr))
                        videos.Add(nr, video);
                    m = m.NextMatch();
                }

            }
            return new List<VideoInfo>(videos.Values);

        }


        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }
}
