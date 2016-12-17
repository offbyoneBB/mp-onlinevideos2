using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class DrakenFilmUtil : LatestVideosSiteUtilBase
    {

        public class DrakenVideo : VideoInfo
        {
            public string Director { get; set; }
        }

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
            Settings.Categories.Add(new RssLink() { Name = "Filmer A-Ö", Url = "https://www.drakenfilm.se/films?size=1024" });
            Settings.Categories.Add(new RssLink() { Name = "Regisörer", Url = "https://www.drakenfilm.se/films?size=1024", HasSubCategories = true });
            Settings.Categories.Add(new RssLink() { Name = "Årtal", Url = "https://www.drakenfilm.se/films?size=1024", HasSubCategories = true });
            Settings.Categories.Add(new RssLink() { Name = "Nytt på Draken Film", Url = "https://www.drakenfilm.se/films?size=20" });
            Settings.DynamicCategoriesDiscovered = true;
            return 4;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            List<DrakenVideo> videos = GetVideos((parentCategory as RssLink).Url);
            if (parentCategory.Name == "Regisörer")
                videos = videos.OrderBy(v => v.Director).ToList();
            if (parentCategory.Name == "Årtal")
                videos = videos.OrderByDescending(v => int.Parse(v.Airdate)).ToList();
            string currentName = "";
            foreach(DrakenVideo video in videos)
            {
                string name = parentCategory.Name == "Regisörer" ? video.Director : video.Airdate;
                if (currentName != name)
                {
                    currentName = name;
                    parentCategory.SubCategories.Add(new RssLink() { Name = name, ParentCategory = parentCategory, Other = new List<VideoInfo>(), EstimatedVideoCount = 0});
                }
                (parentCategory.SubCategories.Last() as RssLink).EstimatedVideoCount++;
                (parentCategory.SubCategories.Last().Other as List<VideoInfo>).Add(video);
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }
        #endregion

        #region Videos

        private List<DrakenVideo> GetVideos(string url)
        {
            string data = ExtendedWebCache.Instance.GetWebData(url);
            List<DrakenVideo> videos = new List<DrakenVideo>();
            Regex rgx = new Regex(@"<img\s*src=""(?<ImageUrl>[^""]*)(?:(?!<a\s*class=""title""\s*).)*<a\s*class=""title""\s*href=""(?<VideoUrl>/film/[^""]*)[^>]*>(?<Title>[^<]*)(?:(?!role=""button""><span>).)*role=""button""><span>(?<Director>.*?),\s(?<Airdate>\d\d\d\d)(?:(?!<span\sclass=""summary"">).)*<span\sclass=""summary"">(?<Description>[^<]*)", RegexOptions.Singleline);
            foreach (Match m in rgx.Matches(data))
            {
                string description = HttpUtility.HtmlDecode(m.Groups["Director"].Value) + ", " + m.Groups["Airdate"].Value + "\r\n" + HttpUtility.HtmlDecode(m.Groups["Description"].Value);
                DrakenVideo video = new DrakenVideo() { Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value), Description = description, Thumb = m.Groups["ImageUrl"].Value, VideoUrl = "https://www.drakenfilm.se" + m.Groups["VideoUrl"].Value, Airdate = m.Groups["Airdate"].Value, Director = HttpUtility.HtmlDecode(m.Groups["Director"].Value) };
                videos.Add(video);
            }
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return category.Other as List<VideoInfo>;
            List<VideoInfo> videos = new List<VideoInfo>();
            List<DrakenVideo> drakenVideos = GetVideos((category as RssLink).Url);
            if (category.Name.Contains("A-Ö"))
                videos = drakenVideos.OrderBy(v => v.Title).ToList<VideoInfo>();
            else
                videos = drakenVideos.ToList<VideoInfo>();
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
                        string key = (rendition["videos"].First()["bitrate"].Value<int>() / 1000) + "kbps";
                        if (!video.PlaybackOptions.ContainsKey(key))
                        {
                            video.PlaybackOptions.Add(key, rendition["links"].First()["href"].Value<string>());
                        }
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
            List<DrakenVideo> videos = GetVideos("https://www.drakenfilm.se/films?size=1024&utf8=✓&button=&query=" + HttpUtility.UrlEncode(query));
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
            return GetVideos("https://www.drakenfilm.se/films?size=" + LatestVideosCount).ToList<VideoInfo>();
        }

        #endregion
    }
}
