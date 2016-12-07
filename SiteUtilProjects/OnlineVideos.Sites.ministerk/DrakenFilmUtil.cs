using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

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

        private CookieContainer Cookies
        {
            get
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(username))
                    throw new OnlineVideosException("Kontrollera dina inloggningsuppgifter");
                CookieContainer cc = new CookieContainer();
                Dictionary<string, Dictionary<string, string>> d = new Dictionary<string, Dictionary<string, string>>();
                d.Add("user", new Dictionary<string, string>() { { "email", username }, { "password", password } });
                string postData = JsonConvert.SerializeObject(d);
                JObject json = ExtendedWebCache.Instance.GetWebData<JObject>("https://www.drakenfilm.se/users/api_sign_in", cookies: cc, postData: postData, cache: false, contentType: "application/json;charset=UTF-8");
                return cc;
            }
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
            HtmlDocument doc = ExtendedWebCache.Instance.GetWebData<HtmlDocument>("https://www.drakenfilm.se/site/type/film");
            List<Category> cats = new List<Category>();
            foreach (HtmlNode item in doc.DocumentNode.SelectNodes("//ul[contains(@id,'-" + type + "')]/li/a"))
            {
                RssLink cat = new RssLink() { ParentCategory = parentCategory };
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
            string data = ExtendedWebCache.Instance.GetWebData(url);
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
                string data = ExtendedWebCache.Instance.GetWebData(currentUrl);
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
            string url = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = ExtendedWebCache.Instance.GetWebData(video.VideoUrl, cookies: Cookies);
            Regex r = new Regex(@"playMovie\('[^']*','(?<id>[^']*)'\s*?\)"">");
            //Regex r = new Regex(@";media_id&quot;:&quot;(?<id>.*?)&quot;"); //super secret regex...
            Match m = r.Match(data);
            if (m.Success)
            {
                JObject json = ExtendedWebCache.Instance.GetWebData<JObject>("https://video.arkena.com/api/v1/public/accounts/571358/medias/" + m.Groups["id"].Value);
                JToken videoRenditions = json["asset"]?["resources"]?.FirstOrDefault(resource => resource["type"].Value<string>() == "video")?["renditions"];
                if (videoRenditions != null)
                {
                    foreach (JToken rendition in videoRenditions)
                    {
                        video.PlaybackOptions.Add((rendition["videos"].First()["bitrate"].Value<int>() / 1000) + "kbps", rendition["links"].First()["href"].Value<string>());
                    }
                    if (video.PlaybackOptions.Count > 0)
                    {
                        video.PlaybackOptions = video.PlaybackOptions.OrderByDescending((p) =>
                        {
                            string resKey = p.Key.Replace("kbps", "");
                            int parsedRes = 0;
                            int.TryParse(resKey, out parsedRes);
                            return parsedRes;
                        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        url = video.PlaybackOptions.First().Value;
                        if (inPlaylist)
                            video.PlaybackOptions.Clear();
                        video.SubtitleText = GetSubtitle(json);
                        r = new Regex(@"http://www.imdb.com/title/(?<imdb>tt\d+)");
                        m = r.Match(data);
                        if (m.Success)
                        {
                            video.Other = new TrackingInfo() { VideoKind = VideoKind.Movie, ID_IMDB = m.Groups["imdb"].Value };
                        }
                    }
                }
            }
            else
            {
                throw new OnlineVideosException("Kontrollera dina inloggningsuppgifter");
            }
            return new List<string>() { url };
        }

        private string GetSubtitle(JObject json)
        {
            string srt = string.Empty;
            try
            {
                string url = json["asset"]["resources"].FirstOrDefault(resource => resource["type"].Value<string>() == "subtitle" && resource["renditions"].FirstOrDefault()["links"].FirstOrDefault()["mimeType"].Value<string>() == "text/ttml")["renditions"].FirstOrDefault()["links"].FirstOrDefault()["href"].Value<string>();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    XmlDocument xDoc = GetWebData<XmlDocument>(url, encoding: System.Text.Encoding.UTF8);
                    string srtFormat = "{0}\r\n{1} --> {2}\r\n{3}\r\n";
                    string begin;
                    string end;
                    string text;
                    string textPart;
                    int line;
                    foreach (XmlElement p in xDoc.GetElementsByTagName("p"))
                    {
                        text = string.Empty;
                        begin = p.GetAttribute("begin");

                        end = p.GetAttribute("end");
                        line = int.Parse(p.GetAttribute("xml:id")) + 1;
                        XmlNodeList textNodes = p.SelectNodes(".//text()");
                        foreach (XmlNode textNode in textNodes)
                        {
                            textPart = textNode.InnerText;
                            textPart.Trim();
                            text += string.IsNullOrEmpty(textPart) ? "" : textPart.Replace("<br />", "\r\n") + "\r\n";
                        }
                        srt += string.Format(srtFormat, line, begin.Replace(".", ","), end.Replace(".", ","), text);
                    }
                }
            }
            catch { }
            return srt;
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
            List<VideoInfo> videos = GetVideos(string.Format(currentUrl, currentPage));
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
