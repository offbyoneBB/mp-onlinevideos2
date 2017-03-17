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
    public class DrakenFilmUtil : LatestVideosSiteUtilBase, IChoice
    {

        public enum DrakenCategorySelector
        {
            countries_of_origin_list,
            director_list,
            genre_list,
            keyword_list,
            recommended,
            spoken_language_list,
            year_of_release,
            none
        }

        public class DrakenVideo : VideoInfo
        {
            public List<string> Directors { get; set; }
            public List<string> Countries { get; set; }
            public List<string> Genres { get; set; }
            public List<string> Keywords { get; set; }
            public List<string> Languages { get; set; }
            public bool Recommended { get; set; }
            public string TrailerUrl { get; set; }
            public List<string> getListFromSelector(DrakenCategorySelector selector)
            {
                List<string> list;
                switch (selector)
                {
                    case DrakenCategorySelector.countries_of_origin_list:
                        list = Countries;
                        break;
                    case DrakenCategorySelector.director_list:
                        list = Directors;
                        break;
                    case DrakenCategorySelector.genre_list:
                        list = Genres;
                        break;
                    case DrakenCategorySelector.keyword_list:
                        list = Keywords;
                        break;
                    case DrakenCategorySelector.spoken_language_list:
                        list = Languages;
                        break;
                    case DrakenCategorySelector.year_of_release:
                        list = new List<string>() { Airdate };
                        break;
                    default:
                        list = new List<string>();
                        break;
                }
                return list;
            }
        }

        public class DrakenCategory : RssLink
        {
            public const string A_TO_Z = "Filmer A-Ö";
            public const string NEW = "Nytt på Draken Film";
            public const string RECOMMENDED = "Rekommenderat";
            public const string DIRECTOR = "Regisör";
            public const string GENRE = "Genre";
            public const string COUNTRY = "Land";
            public const string LANGUAGE = "Språk";
            public const string KEYWORD = "Ketegori";
            public const string YEAR = "Årtal";
            public DrakenCategorySelector Selector { get; set; }
            public bool SortVideosByLetter
            {
                get
                {
                    return Name != NEW && Name != RECOMMENDED;
                }
            }
        }

        #region Configuration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("E-post"), Description("Skriv in din e-postadress.")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Lösenord"), Description("Skriv in ditt lösenord."), PasswordPropertyText(true)]
        protected string password = null;

        #endregion

        #region Urls

        private const string youtubeVideoUrl = "https://www.youtube.com/watch?v={0}";
        private const string vimeoVideoUrl = "https://player.vimeo.com/video/{0}";
        private const string drakenVideosUrl = "https://www.drakenfilm.se/films_search?query={0}&size={1}";
        private const string drakenVideoUrl = "https://www.drakenfilm.se/film/{0}";
        private const string drakenResourceUrl = "https://video.arkena.com/api/v1/public/accounts/571358/medias/{0}";

        #endregion
        
        #region Session/login

        private bool HaveCredentials
        {
            get
            {
                return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            }
        }
        private CookieContainer Cookies
        {
            get
            {
                if (!HaveCredentials)
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
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.A_TO_Z, Selector = DrakenCategorySelector.none});
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.NEW, Selector = DrakenCategorySelector.none });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.RECOMMENDED, Selector = DrakenCategorySelector.recommended });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.DIRECTOR, HasSubCategories = true, Selector = DrakenCategorySelector.director_list });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.GENRE, HasSubCategories = true, Selector = DrakenCategorySelector.genre_list });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.COUNTRY, HasSubCategories = true, Selector = DrakenCategorySelector.countries_of_origin_list });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.LANGUAGE, HasSubCategories = true, Selector = DrakenCategorySelector.spoken_language_list });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.KEYWORD, HasSubCategories = true, Selector = DrakenCategorySelector.keyword_list });
            Settings.Categories.Add(new DrakenCategory() { Name = DrakenCategory.YEAR, HasSubCategories = true, Selector = DrakenCategorySelector.year_of_release });
            Settings.DynamicCategoriesDiscovered = true;
            return 9;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            DrakenCategory dc = parentCategory as DrakenCategory;
            foreach (DrakenVideo video in GetVideos())
            {
                foreach (string name in video.getListFromSelector(dc.Selector).Where(n => !parentCategory.SubCategories.Any(c => c.Name == n)))
                {
                    parentCategory.SubCategories.Add(
                        new DrakenCategory()
                        {
                            Name = name,
                            ParentCategory = parentCategory,
                            Selector = dc.Selector,
                        }
                    );
                }
            }
            parentCategory.SubCategories = parentCategory.SubCategories.OrderBy(c => c.Name).ToList<Category>();
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }
        #endregion

        #region Videos

        private List<DrakenVideo> GetVideos(DrakenCategorySelector selector = DrakenCategorySelector.none, string categoryNameSelector = null, uint size = 1024, string query = "", bool cache = true)
        {
            string url = string.Format(drakenVideosUrl, query, size);
            JObject data = ExtendedWebCache.Instance.GetWebData<JObject>(url, cache: cache);
            List<DrakenVideo> videos = new List<DrakenVideo>();
            foreach (JToken vt in data["hits"].Value<JArray>())
            {
                DrakenVideo video = new DrakenVideo();
                video.Airdate = vt["year_of_release"].Value<string>();
                video.Countries = vt["countries_of_origin_list"].Value<string>().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                video.Directors = vt["director_list"].Value<string>().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                video.Genres = vt["genre_list"].Value<string>().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                video.Keywords = vt["keyword_list"].Value<string>().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                video.Languages = vt["spoken_language_list"].Value<string>().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                video.Description = string.Format( "{0} | {1} | {2}\n{3}", string.Join(", ", video.Genres), string.Join(", ", video.Directors), string.Join(", ", video.Countries), vt["summary"].Value<string>());
                video.Recommended = vt["recommended"] != null && vt["recommended"].Type == JTokenType.Boolean && vt["recommended"].Value<bool>();
                video.Thumb = vt["image_background"].Value<string>();
                if (video.Thumb.StartsWith("//"))
                    video.Thumb = "http:" + video.Thumb;
                video.Title = vt["title"].Value<string>();
                string trailerPovider = (vt["trailer_provider"] == null || vt["trailer_provider"].Type != JTokenType.String) ? string.Empty : vt["trailer_provider"].Value<string>();
                if (trailerPovider == "vimeo")
                    video.TrailerUrl = string.Format(vimeoVideoUrl, vt["trailer_id"].Value<string>());
                else if (trailerPovider == "youtube")
                    video.TrailerUrl = string.Format(youtubeVideoUrl, vt["trailer_id"].Value<string>());
                else
                    video.HasDetails = false;
                video.VideoUrl = string.Format(drakenVideoUrl, vt["search_title"].Value<string>());
                videos.Add(video);
            }
            if (selector == DrakenCategorySelector.recommended)
                videos = videos.Where(v => v.Recommended).ToList();
            else if (selector != DrakenCategorySelector.none && !string.IsNullOrWhiteSpace(categoryNameSelector))
                videos = videos.Where(v => v.getListFromSelector(selector).Any(t => t == categoryNameSelector)).ToList();
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos;
            DrakenCategory dc = category as DrakenCategory;
            List<DrakenVideo> drakenVideos = GetVideos(dc.Selector, dc.Name, (uint)(dc.Name == DrakenCategory.NEW ? 20 : 1024));
            if (dc.SortVideosByLetter)
                videos = drakenVideos.OrderBy(v => v.Title).ToList<VideoInfo>();
            else
                videos = drakenVideos.ToList<VideoInfo>();
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            bool isVimeo = video.VideoUrl.Contains("vimeo.com");
            bool isYoutube = video.VideoUrl.Contains("youtube.com");
            video.PlaybackOptions = new Dictionary<string, string>();
            if (isVimeo || isYoutube)
            {
                Hoster.HosterBase hoster = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => video.VideoUrl.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                if (hoster != null)
                    video.PlaybackOptions = hoster.GetPlaybackOptions(video.VideoUrl);
                if (isYoutube)
                    video.PlaybackOptions = video.PlaybackOptions.Reverse().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                else if (isVimeo)
                    video.PlaybackOptions = video.PlaybackOptions.OrderByDescending((p) =>
                    {
                        string resKey = p.Key.Replace("p", "");
                        int parsedRes = 0;
                        int.TryParse(resKey, out parsedRes);
                        return parsedRes;
                    }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
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
            }
            
            string url = video.PlaybackOptions.First().Value;
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            return new List<string>() { url };
        }

        private string GetSubtitle(JObject json)
        {
            string srt = string.Empty;
            try
            {
                string jString = json.ToString();
                if (jString.Contains("text/ttml"))
                {
                    var url = json["asset"]["resources"].Where(r => r["type"].Value<string>() == "subtitle" && r["renditions"].Any(rend => rend["links"].Any(l => l["mimeType"].Value<string>() == "text/ttml"))).FirstOrDefault()["renditions"].Where(r => r["links"].Any(l => l["mimeType"].Value<string>() == "text/ttml")).FirstOrDefault()["links"].FirstOrDefault()["href"].Value<string>();
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
                else if (jString.Contains("text/vtt"))
                {

                    string url = json["asset"]["resources"].Where(r => r["type"].Value<string>() == "subtitle" && r["renditions"].Any(rend => rend["links"].Any(l => l["mimeType"].Value<string>() == "text/vtt"))).FirstOrDefault()["renditions"].Where(r => r["links"].Any(l => l["mimeType"].Value<string>() == "text/vtt")).FirstOrDefault()["links"].FirstOrDefault()["href"].Value<string>();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        srt = GetWebData(url, encoding: System.Text.Encoding.UTF8);
                        srt = srt.Replace("WEBVTT", "").Trim();
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
            List<DrakenVideo> videos = GetVideos(query: HttpUtility.UrlEncode(query));
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

        #region Latest Videos

        public override List<VideoInfo> GetLatestVideos()
        {
            return GetVideos(size: LatestVideosCount, cache: false).ToList<VideoInfo>();
        }

        #endregion

        #region IChoice

        List<DetailVideoInfo> IChoice.GetVideoChoices(VideoInfo video)
        {
            DrakenVideo dv = video as DrakenVideo;
            DetailVideoInfo movie = new DetailVideoInfo(video);
            movie.Title2 = dv.Title + (HaveCredentials ? string.Empty : " [Inloggning krävs]");
            DetailVideoInfo trailer = new DetailVideoInfo(video);
            trailer.Title2 = "Trailer";
            trailer.VideoUrl = dv.TrailerUrl;
            List<DetailVideoInfo> videos = new List<DetailVideoInfo>() { movie, trailer };
            return videos;
        }

        #endregion
    }
}
