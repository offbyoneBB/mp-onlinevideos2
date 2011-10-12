using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class NOSUtil : SiteUtilBase, IFilter
    {
        private string bareCategoryUrl;
        private string currentCat;

        private string baseUrl = "http://nos.nl";
        private string subCategoryRegex = @"href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<";
        // <a href="/video/131573-zilveren-camera-voor-pim-ras.html">Zilveren Camera voor Pim Ras</a> <span class="img-video"><img src="http://content.nos.nl/data//video/xs/2010/01/24//NO_zo_18_zil--CNO10012403_1.jpg" alt="" /><em>&raquo;</em></span> <span class="cat">Video</span>  Fotograaf Pim Ras heeft de Zilveren Camera 2009 gewonnen; de prijs voor de beste nieuwsfoto van het... <em>24  jan 2010, 715 keer afgespeeld</em> </li><li>  
        private string videoListRegex = @"<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)<.*?img\ssrc=""(?<thumb>[^""]+)"".*?Video</span>(?<descr>[^<]+)<[^>]+>(?<descr2>[^<]+)<";
        private string videoUrlRegex = @"file:[^']'(?<url>[^']+)'";
        private string voetbalListPopRegex = @"<a.*?href=""(?<url>[^""]+)""><img\ssrc=""(?<thumb>[^""]+)"".*?href=.*?>(?<title>[^<]+)<.*?<p>(?<descr>.+?)</b>.*?<p>(?<descr2>.+?)</b>";
        private string voetbalListLatestRegex = @"<a.*?href=""(?<url>[^""]+)""><img\ssrc=""(?<thumb>[^""]+)"".*?href=[^>]+>(?<title>[^<]+)</a><br\s/><br\s/>(?<descr>[^<]+)<";
        private string voetbalSubRegex = @"<a\shref=""(?<url>[^""]+)""[^>]+>(?<title>[^<]+)<";
        private string os2010ListRegex = @"<li\sid="".*?<a\shref=""(?<url>[^""]+)"".*?<span[^>]*>(?<title>[^<]+)<";
        private string laatsteJournaalListRegex = @"<li[^>]*>\s*<a\shref=""(?<url>[^""]*)"">(?<title1>[^<]+)<strong>(?<title2>[^<]*)</strong></a><span>(?<airdate>[^<]*)</span></li>";
        private string baseVoetbalUrl = null;

        private string Pop = "POP";
        private string Latest = "LATEST";
        private string Live = "LIVE";
        private string LaatsteJournaals = "LAATSTEJOURNAALS";
        private string Os2010 = "os2010";

        // Todo: search for voetbal. (should use baseVoetbalUrl)

        public NOSUtil()
        {
        }

        private Regex regEx_SubCategory;
        private Regex regEx_VideoList;
        private Regex regEx_VideoUrl;
        private Regex regEx_VoetbalPop;
        private Regex regEx_VoetbalLatest;
        private Regex regEx_VoetbalSub;
        private Regex regEx_os2010List;
        private Regex regEx_LaatsteJournaal;

        private string currentPageTitle;

        private Dictionary<string, string> categories;
        private Dictionary<string, string> timeFrames;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            DateTime day = DateTime.Today;
            string today = day.ToString("yyyy-MM-dd");
            categories = new Dictionary<string, string>();
            categories.Add("Laatste", String.Empty);
            categories.Add("Populair vandaag", String.Format("video/pagina/1/datum/{0}/populair/dag/", today));
            categories.Add("Populair deze week", String.Format("video/pagina/1/datum/{0}/populair/week/", today));
            categories.Add("Alles van [Datum]", "video/pagina/1/datum/$DATE/");
            categories.Add("Journaals van [Datum]", "journaal/pagina/1/datum/$DATE/");

            timeFrames = new Dictionary<string, string>();
            for (int i = 0; i < 7; i++)
            {
                timeFrames.Add(day.ToString("d"), day.ToString("yyyy-MM-dd"));
                day = day.AddDays(-1);
            }

            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VoetbalPop = new Regex(voetbalListPopRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VoetbalLatest = new Regex(voetbalListLatestRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VoetbalSub = new Regex(voetbalSubRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            regEx_os2010List = new Regex(os2010ListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_LaatsteJournaal = new Regex(laatsteJournaalListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Name = "Nos";
            cat.Url = @"http://nos.nl/video-en-audio/";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Nieuws";
            cat.Url = @"http://nos.nl/nieuws/video-en-audio/";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Sport";
            cat.Url = @"http://nos.nl/sport/video-en-audio/";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Live";
            cat.Url = Live;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Laatste journaals";
            cat.Url = LaatsteJournaals;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;

            parentCategory.SubCategories = new List<Category>();


            if (url.StartsWith("!")) //voetbal main or sub
            {
                if (url.StartsWith("!!")) //voetbal sub
                {
                    RssLink cat = new RssLink();
                    cat.Name = "Laatste video's van " + parentCategory.Name;
                    cat.Url = Latest + url.Substring(1);
                    cat.HasSubCategories = false;
                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;

                    cat = new RssLink();
                    cat.Name = "Meest bekeken video's van " + parentCategory.Name;
                    cat.Url = Pop + url.Substring(1);
                    cat.HasSubCategories = false;
                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                }
                else
                {
                    string webData = GetWebData(url.Substring(1));
                    webData = GetSubString(webData, @"topbar", @"</ul>");

                    Match m = regEx_VoetbalSub.Match(webData);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        cat.Url = m.Groups["url"].Value;
                        cat.HasSubCategories = true;

                        cat.Url = "!!" + cat.Url + @"video/index/";
                        cat.HasSubCategories = true;

                        parentCategory.SubCategories.Add(cat);
                        cat.ParentCategory = parentCategory;
                        m = m.NextMatch();
                    }
                }
            }
            else
            {
                string webData = GetWebData(url, null, null, null, true);
                webData = GetSubString(webData, @"class=""active""", @"</ul>");
                webData = webData.Replace("><", String.Empty);

                Match m = regEx_SubCategory.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;
                    cat.HasSubCategories = false;
                    if (!cat.Url.StartsWith("http:"))
                        cat.Url = baseUrl + cat.Url + @"video-en-audio/";
                    else
                    {
                        cat.Url = '!' + cat.Url + @"video/index/";
                        cat.HasSubCategories = true; //voetbal
                    }

                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            if (Live.Equals(video.Other))
                return ParseASX(video.VideoUrl)[0];
            string webData = GetWebData(video.VideoUrl);
            string url = video.VideoUrl;
            if (!Os2010.Equals(video.Other))
            {
                Match m = regEx_VideoUrl.Match(webData);
                if (m.Success)
                    url = m.Groups["url"].Value;
                else
                {
                    Match m2 = Regex.Match(webData, @"class=""wmp""\s*href=""(?<url>[^""]*)""");
                    if (m2.Success)
                    {
                        webData = GetWebData(video.VideoUrl + m2.Groups["url"]);
                        url = GetSubString(webData, @"name=""URL"" value=""", @"""");
                        url = GetRedirectedUrl(url);
                        return url;
                    }
                }

            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(url));
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://xspf.org/ns/0/");

            XmlNode node = doc.SelectSingleNode(@"//a:location", nsmRequest);
            if (node != null)
                url = node.InnerText;
            if (!String.IsNullOrEmpty(url))
                return url;
            else
                return video.VideoUrl;
        }

        public override string getCurrentVideosTitle()
        {
            return currentPageTitle;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            bareCategoryUrl = rssLink.Url;
            currentCat = String.Empty;
            return getPagedVideoList(rssLink.Url, rssLink.Name, String.Empty);
        }

        private List<VideoInfo> getVoetbalVideos(string url)
        {
            bool latest;
            string[] parts = url.Split('!');
            latest = parts[0] == Latest;
            url = parts[1];

            string webData = GetWebData(url, null, null, null, true);
            int i = webData.IndexOf("head-video-recentoverview");
            int j = webData.IndexOf("head-video-mostviewed");
            int k = webData.IndexOf("END video content");

            Regex regex;

            if (latest)
            {
                webData = webData.Substring(i, j - i);
                regex = regEx_VoetbalLatest;
            }
            else
            {
                webData = webData.Substring(j, k - j);
                regex = regEx_VoetbalPop;
            }

            Uri uri = new Uri(url);
            string baseUrl = uri.Scheme + "://" + uri.Host;
            //baseVoetbalUrl = baseUrl;

            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regex.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;
                    video.Description = m.Groups["descr"].Value + '\n' + m.Groups["descr2"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;

        }

        private List<VideoInfo> getPagedVideoList(string url, string deftitle, string day)
        {
            currentPageTitle = deftitle;
            if (url.Contains("!"))
                return getVoetbalVideos(url);
            baseVoetbalUrl = null;

            List<VideoInfo> videos = new List<VideoInfo>();

            if (url == LaatsteJournaals)
            {
                string webData2 = GetWebData(baseUrl);
                Match m = regEx_LaatsteJournaal.Match(webData2);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = m.Groups["title1"].Value + m.Groups["title2"].Value;
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
					video.Length = '|' + Translation.Instance.Airdate + ": " + m.Groups["airdate"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }

                return videos;
            }

            if (url == Live)
            {
                VideoInfo video = new VideoInfo();
                video.Title = "Journaal24";
                video.VideoUrl = @"http://livestreams.omroep.nl/nos/journaal24-bb";
                video.Other = Live;
                videos.Add(video);

                video = new VideoInfo();
                video.Title = "Politiek24";
                video.Other = Live;
                video.VideoUrl = @"http://livestreams.omroep.nl/nos/politiek24-bb";
                videos.Add(video);

                string osData = GetWebData(@"http://nos.nl/os2010/live/");
                Match m = regEx_os2010List.Match(osData);
                while (m.Success)
                {
                    video = new VideoInfo();
                    video.Title = "OS2010 " + m.Groups["title"].Value;
                    string osurl = baseUrl + m.Groups["url"].Value;
                    string liveOsData = GetWebData(osurl);
                    video.Title = video.Title + " " + HttpUtility.HtmlDecode(GetSubString(liveOsData, @"<h1><span>", @"</h1>"));
                    video.Title = Regex.Replace(video.Title, @"<[^>]*>", "", RegexOptions.Multiline);

                    //<h1><span>Kanaal Multi 4 </span> Nu: Snowboarden - kwal. halfpipe (m)</h1>

                    liveOsData = GetSubString(liveOsData, @"SterCommercials('", @"'");
                    video.VideoUrl = liveOsData;
                    video.Other = Os2010;
                    videos.Add(video);
                    m = m.NextMatch();
                }

                return videos;
            }

            if (!String.IsNullOrEmpty(currentCat))
                url = url + currentCat;
            url = url.Replace("$DATE", day);

            string webData = GetWebData(url, null, null, null, true);
            string title = GetSubString(webData, @"id=""article"">", "</h1>");
            title = title.Replace("<h1>", String.Empty);
            title = title.Replace("<span>", String.Empty);
            title = title.Replace("</span>", String.Empty);
            currentPageTitle = title.Trim();

            webData = GetSubString(webData, @"img-list", @"class=""content-menu");


            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;
                    video.Description = m.Groups["descr"].Value + '\n' + m.Groups["descr2"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override bool HasFilterCategories
        {
            get { return true; }
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            return categories;
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            currentCat = String.Empty;
            if (baseVoetbalUrl != null)
                return getPagedVideoList(
                String.Format(baseVoetbalUrl + @"/video/zoek/keyword/{0}",
                HttpUtility.UrlEncode(query)), String.Empty, string.Empty);
            else
                return getPagedVideoList(
                    String.Format(@"http://nos.nl/zoeken/?s={0}&sort=2&type%5B%5D=video&datumvan=&datumtot=",
                    HttpUtility.UrlEncode(query)), String.Empty, string.Empty);
        }

        public override List<VideoInfo> Search(string query, string category)
        {
            currentCat = category;
            return null;
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

        #region IFilter Members

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            bareCategoryUrl = ((RssLink)category).Url;
            return getPagedVideoList(bareCategoryUrl, String.Empty, timeFrame);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            throw new NotImplementedException();
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            throw new NotImplementedException();
        }

        public List<int> getResultSteps()
        {
            return new List<int>();
        }

        public Dictionary<string, string> getOrderbyList()
        {
            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return timeFrames;
        }

        #endregion
    }
}
