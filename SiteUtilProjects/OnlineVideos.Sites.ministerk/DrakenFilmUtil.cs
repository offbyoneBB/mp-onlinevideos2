using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.HboNordic;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class DrakenFilmUtil : LatestVideosSiteUtilBase
    {
        #region Configuration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("E-post"), Description("Skriv in din e-postadress.")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Lösenord"), Description("Skriv in ditt lösenord."), PasswordPropertyText(true)]
        protected string password = null;

        #endregion

        #region Session/login

        private CookieContainer cc = null;
        private string login()
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(username))
                throw new OnlineVideosException("Kontrollera dina inloggningsuppgifter");
            cc = new CookieContainer();
            HtmlDocument doc = HboWebCache.Instance.GetWebData<HtmlDocument>("https://www.drakenfilm.se/user", cookies: cc, cache: false);
            string postDataFormat = "name={0}&pass={1}&form_build_id={2}&form_id=user_login&op=Logga+in";
            string formBuildId = doc.DocumentNode.SelectSingleNode("//input[@name='form_build_id']").GetAttributeValue("value", "");
            string postData = string.Format(postDataFormat, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password), formBuildId);
            string data = HboWebCache.Instance.GetWebData("https://www.drakenfilm.se/user", cookies: cc, postData: postData, cache: false);
            Regex rgx = new Regex(@"""giffToken"":""(?<token>[^""]*)");
            Match match = rgx.Match(data);
            if (!match.Success || !data.Contains("Logga ut"))
            {
                cc = null;
                throw new OnlineVideosException("Kontrollera dina inloggningsuppgifter");
            }
            return match.Groups["token"].Value;
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            Category filmer = new Category() { Name = "Filmer", HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>() };
            RssLink allaFilmer = new RssLink() { Name = "Alla filmer", HasSubCategories = false, Url = "https://www.drakenfilm.se/site/type/film?site_fulltext=&page={0}", ParentCategory = filmer };
            filmer.SubCategories.Add(allaFilmer);
            RssLink genre = new RssLink() { Name = "Genre", HasSubCategories = true, Url = "genre", ParentCategory = filmer };
            filmer.SubCategories.Add(genre);
            RssLink tags = new RssLink() { Name = "Nyckelord", HasSubCategories = true, Url = "tags", ParentCategory = filmer };
            filmer.SubCategories.Add(tags);
            RssLink year = new RssLink() { Name = "Festivalår", HasSubCategories = true, Url = "year", ParentCategory = filmer };
            filmer.SubCategories.Add(year);
            RssLink country = new RssLink() { Name = "Land", HasSubCategories = true, Url = "country-of-origin", ParentCategory = filmer };
            filmer.SubCategories.Add(country);
            RssLink language = new RssLink() { Name = "Språk", HasSubCategories = true, Url = "spoken-language", ParentCategory = filmer };
            filmer.SubCategories.Add(language);
            RssLink director = new RssLink() { Name = "Regissör", HasSubCategories = true, Url = "director", ParentCategory = filmer };
            filmer.SubCategories.Add(director);
            Settings.Categories.Add(filmer);
            Settings.Categories.Add(new RssLink() { Name = "Mest sedda", Url = "https://www.drakenfilm.se/filmer/mest-sedda?page={0}" });
            Settings.Categories.Add(new RssLink() { Name = "Nytt på Draken Film", Url = "https://www.drakenfilm.se/filmer/nytt-pa-draken?page={0}" });
            Settings.Categories.Add(new RssLink() { Name = "Film Väst", Url = "https://www.drakenfilm.se/films/film-vast?page={0}" });
            Settings.Categories.Add(new RssLink() { Name = "A-Ö", Url = "https://www.drakenfilm.se/filmer/a-o" });
            Settings.DynamicCategoriesDiscovered = true;
            return 5;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string type = (parentCategory as RssLink).Url;
            HtmlDocument doc = HboWebCache.Instance.GetWebData<HtmlDocument>("https://www.drakenfilm.se/site/type/film");
            List<Category> cats = new List<Category>();
            foreach (HtmlNode item in doc.DocumentNode.SelectNodes("//ul[contains(@id,'-" + type + "')]/li/a"))
            {
                RssLink cat = new RssLink() { ParentCategory = parentCategory};
                cat.Url = "https://www.drakenfilm.se" + item.GetAttributeValue("href", "") + "?site_fulltext=&page={0}";
                cat.Name = HttpUtility.HtmlDecode(item.SelectSingleNode("text()").InnerText.Trim());
                Regex rgx = new Regex(@"\((?<count>\d+?)\)");
                Match m = rgx.Match(cat.Name);
                if (m.Success)
                {
                    string count = m.Groups["count"].Value;
                    cat.Name = cat.Name.Replace("(" + count + ")", "").Trim();
                    uint estimatedVideoCount = 0;
                    uint.TryParse(count, out estimatedVideoCount);
                    cat.EstimatedVideoCount = estimatedVideoCount;
                }
                cats.Add(cat);
            }
            parentCategory.SubCategories = cats;
            parentCategory.SubCategoriesDiscovered = cats.Count > 0;
            return cats.Count;
        }

        #endregion

        #region Videos

        private string currentUrl = "";
        private int currentPage = 0;
        private List<VideoInfo> GetVideos(string url)
        {
            string data = HboWebCache.Instance.GetWebData(url);
            List<VideoInfo> videos = new List<VideoInfo>();
            Regex rgx = new Regex(@"foaf:Image"" src=""(?<thumb>[^""]*).*?<h4>(?<title>[^<]*).*?field__item even"">(?<description>[^<]*).*?<a href=""(?<url>/film/[^""]*)", RegexOptions.Singleline);
            foreach (Match m in rgx.Matches(data))
            {
                VideoInfo video = new VideoInfo() { Title = HttpUtility.HtmlDecode(m.Groups["title"].Value), Description = HttpUtility.HtmlDecode(m.Groups["description"].Value), Thumb = m.Groups["thumb"].Value, VideoUrl = "https://www.drakenfilm.se" + m.Groups["url"].Value };
                videos.Add(video);
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            List<VideoInfo> videos = GetVideos(string.Format(currentUrl, currentPage));
            HasNextPage = (videos.Count == 16 || videos.Count == 32);
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            currentUrl = (category as RssLink).Url;
            currentPage = 0;
            HasNextPage = false;
            if (category.Name == "A-Ö")
            {
                string data = HboWebCache.Instance.GetWebData(currentUrl);
                Regex rgx = new Regex(@"href=""(?<url>/film/[^""]*).*?>(?<title>[^<]*)");
                foreach (Match m in rgx.Matches(data))
                {
                    VideoInfo video = new VideoInfo() { Title = HttpUtility.HtmlDecode(m.Groups["title"].Value), VideoUrl = "https://www.drakenfilm.se" + m.Groups["url"].Value };
                    videos.Add(video);
                }
            }
            else if (currentUrl.EndsWith("{0}"))
            {
                videos = GetVideos(string.Format(currentUrl, currentPage));
                HasNextPage = (videos.Count == 16 || videos.Count == 32);
            }
            else
            {

            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string token;
            string data = HboWebCache.Instance.GetWebData(video.VideoUrl, cookies: cc, cache: false);
            Regex rgx = new Regex(@"""giffToken"":""(?<token>[^""]*)");
            Match match = rgx.Match(data);
            if (!match.Success || cc == null || !data.Contains("Logga ut"))
            {
                token = login();
                data = HboWebCache.Instance.GetWebData(video.VideoUrl, cookies: cc, cache: false);
            }
            else
            {
                token = match.Groups["token"].Value;
            }
            rgx = new Regex(@"data-qbrick-media-id=""(?<id>[^""]*)");
            match = rgx.Match(data);
            if (!match.Success)
                return new List<string>();
            string id = match.Groups["id"].Value;
            string playerHandlerUrl = "https://professional.player.qbrick.com/Html5/Web/PlayerHandler.ashx?action=getdata&embedId=qbrick_professional_qbrick1&types=mp4&widgettype=professional&mid={0}&init=false&dsat={1}";
            rgx = new Regex(@"http://www.imdb.com/title/(?<imdb>tt\d+)");
            match = rgx.Match(data);
            if (match.Success)
            {
                video.Other = new TrackingInfo() { VideoKind = VideoKind.Movie, ID_IMDB = match.Groups["imdb"].Value };
            }
            data = HboWebCache.Instance.GetWebData(string.Format(playerHandlerUrl, id, token), cookies: cc, cache: false);
            rgx = new Regex(@"""(?<url>http[^""]*?m3u8)");
            match = rgx.Match(data);
            if (!match.Success)
                return new List<string>();
            string payload = @"{{""mediaId"":""{0}"",""urls"":[{{""parameter"":""stream_index_0"",""url"":""{1}""}}]}}";
            data = string.Format(payload, id, match.Groups["url"].Value);
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Authorization", "Token " + token);
            string qticketUrl = @"http://zmey.drakenfilm.se/qtickets?format=json";
            JObject json = HboWebCache.Instance.GetWebData<JObject>(qticketUrl, postData: data, headers: headers, contentType: "application/json", cookies: cc, cache: false);
            string m3u8 = json["urls"].First()["url"].Value<string>();
            m3u8 = HboWebCache.Instance.GetWebData(m3u8, cookies: cc, cache: false);
            rgx = new Regex(@"BANDWIDTH=(?<bandwidth>\d+)000.*?(?<url>http[^\n]*)", RegexOptions.Singleline);
            Dictionary<string, string> pbo = new Dictionary<string, string>();
            foreach (Match m in rgx.Matches(m3u8))
            {
                pbo.Add(m.Groups["bandwidth"].Value + " kbps", m.Groups["url"].Value);
            }
            pbo = pbo.OrderByDescending((p) =>
            {
                string resKey = p.Key.Replace(" kbps", "");
                int parsedRes = 0;
                int.TryParse(resKey, out parsedRes);
                return parsedRes;
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            string url = pbo.First().Value;
            if (inPlaylist)
                pbo = null;
            video.PlaybackOptions = pbo;
            return new List<string>() { url };
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> result = new List<SearchResultItem>();
            currentUrl = "https://www.drakenfilm.se/site/type/film?site_fulltext=" + HttpUtility.UrlEncode(query) + "&page={0}";
            currentPage = 0;
            List<VideoInfo> videos = GetVideos(string.Format(currentUrl,currentPage));
            HasNextPage = videos.Count > 15;
            videos.ForEach(v => result.Add(v));
            return result;
        }

        #endregion

        #region GetFileNameForDownload

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            return Helpers.FileUtils.GetSaveFilename(video.Title) + ".mp4";
        }

        #endregion

        #region Tracking

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
                return video.Other as ITrackingInfo;
            return base.GetTrackingInfo(video);
        }

        #endregion

        #region Latest

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = GetVideos("https://www.drakenfilm.se/filmer/nytt-pa-draken");
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        #endregion
    }
}
