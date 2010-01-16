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
    public class OnsNetNuenenUtil : SiteUtilBase
    {
        private bool asFlv = false;
        private string baseUrl = "http://onsnet.video4all.nl/";
        private string categoryRegex = @"<li\sclass=""[^\s]+\sSubMenuItem[^""]+""><a\shref=""(?<url>[^""]+)""\s+><span>(?<title>[^<]+)</span>";
        private string videoListRegex = @"<div\sclass=""VideoThumbnail""><a\shref=""\#""\s+onclick=""getElementById\('VideoFrame'\)\.src='(?<url>[^']+)';""><img\ssrc=""(?<thumb>[^""]+)""\swidth=""[^""]+""\sheight=""[^""]+""\stitle=""(?<title>[^""]+)""";
        private string subCategoryRegex = @"<option\s+value=""(?<id>[^""]+)""[^>]*>(?<title>[^<]+)</option>";
        private string mainPageRegex = @"<div\sclass=""(New|Search)VideoThumb""><a\shref=""index\.php\?cid=(?<id>[^&]+)&[^<]+<img\swidth=""[^""]+""\ssrc=""(?<thumb>[^""]+)""\stitle=""(?<title>[^""]+)""";

        public OnsNetNuenenUtil()
        {
        }

        private Regex regEx_Category;
        private Regex regEx_SubCategory;
        private Regex regEx_VideoList;
        private Regex regEx_MainPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_MainPage = new Regex(mainPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl + "corp/index.php?page=48&site=4");
            if (!string.IsNullOrEmpty(data))
            {
                RssLink cat2 = new RssLink();
                cat2.Name = "Beste video's";
                cat2.Url = "!http://onsnet.video4all.nl/corp/index.php?ord=hig&page=48&site=4&ln=nl";
                Settings.Categories.Add(cat2);

                cat2 = new RssLink();
                cat2.Name = "Nieuwste video's";
                cat2.Url = "!http://onsnet.video4all.nl/corp/index.php?ord=new&page=48&site=4&ln=nl";
                Settings.Categories.Add(cat2);

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    string url = baseUrl + "corp/" + HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    cat.Url = url;
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

            string webData = GetWebData(url);
            int i = webData.IndexOf("videoalbumsubject");
            if (i >= 0) webData = webData.Substring(i);

            int p = url.IndexOf("itemid=");
            if (p >= 0)
            {
                int q = url.IndexOf('&', p);
                url = url.Substring(0, p + 7) + "{0}" + url.Substring(q);
            }

            parentCategory.SubCategories = new List<Category>();

            Match m = regEx_SubCategory.Match(webData);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                cat.Url = String.Format(url, HttpUtility.HtmlDecode(m.Groups["id"].Value));
                cat.SubCategoriesDiscovered = true;
                cat.HasSubCategories = false;

                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
                m = m.NextMatch();
            }

            return parentCategory.SubCategories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            string url = video.VideoUrl;
            if (asFlv)
            {
                url = url.Replace(@"/win_video.php?tra=yes&tit=no&cid=", @"/getVideo.php?tp=flv&cid=") + "&tid=328";
                string xmlData = GetWebData(url);
                XmlDocument tdoc = new XmlDocument();
                tdoc.Load(XmlReader.Create(new StringReader(xmlData)));
                XmlNode source = tdoc.SelectSingleNode("result/entry");
                return source.Attributes["url"].Value;
            }
            else
            {
                url = url.Replace(@"/win_video.php?tra=yes&tit=no&cid=", @"/getVideo.php?tp=wmv&cid=") + "&lvl=3&tid=328";
                //lvl=quality, 3=highest
                return ParseASX(url)[0];
            }
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            return getPagedVideoList(String.Format("!http://onsnet.video4all.nl/corp/?page=54&site=4&search=1&srch_str={0}&x=0&y=0&submit_frmSearch=1", query));
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            return getPagedVideoList(rssLink.Url);
        }

        private List<VideoInfo> getPagedVideoList(string url)
        {
            Regex regEx = null;
            if (url.StartsWith("!"))
            {
                url = url.Substring(1);
                regEx = regEx_MainPage;
            }
            else
                regEx = regEx_VideoList;

            string webData = GetWebData(url);
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                    if (HttpUtility.HtmlDecode(m.Groups["url"].Value) == String.Empty)
                        video.VideoUrl = baseUrl + "src/getVideo.php?tp=flv&cid=" + m.Groups["id"].Value + "&tid=328";
                    else
                        video.VideoUrl = baseUrl.TrimEnd('/') + HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    video.ImageUrl = baseUrl.TrimEnd('/') + HttpUtility.HtmlDecode(m.Groups["thumb"].Value);
                    videos.Add(video);
                    m = m.NextMatch();
                }

            }
            return videos;

        }

    }
}
