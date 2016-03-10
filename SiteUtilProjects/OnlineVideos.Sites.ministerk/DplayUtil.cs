using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class DplayUtil : SiteUtilBase, IBrowserSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Base Url")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Programs category url")]
        protected string programUrl;
        [Category("OnlineVideosConfiguration"), Description("Programs category name")]
        protected string programCategoryName;
        [Category("OnlineVideosConfiguration"), Description("Sports category url")]
        protected string sportUrl;
        [Category("OnlineVideosConfiguration"), Description("Sports category name")]
        protected string sportCategoryName;
        [Category("OnlineVideosConfiguration"), Description("Channels category url")]
        protected string channelUrl;
        [Category("OnlineVideosConfiguration"), Description("Channels category name")]
        protected string channelCategoryName;
        [Category("OnlineVideosConfiguration"), Description("Secure Url")]
        protected string secureUrl;
        [Category("OnlineVideosConfiguration"), Description("Geo Url")]
        protected string geoUrl;
        [Category("OnlineVideosConfiguration"), Description("Hardcoded geo code")]
        protected string hardCodeGeo;
        [Category("OnlineVideosConfiguration"), Description("Cookie domain")]
        protected string cookieDomain;
        [Category("OnlineVideosConfiguration"), Description("Login realm code")]
        protected string realmCode;
        [Category("OnlineVideosConfiguration"), Description("Key to select srt for video")]
        protected string srtSelector;
        [Category("OnlineVideosConfiguration"), Description("Needs premium (only premium content")]
        protected bool needsPremium;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Dplay email")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Dplay password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Always show premium"), Description("Always show premium content")]
        protected bool showPremium = false;


        private bool HaveCredentials
        {
            get { return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password); }
        }

        private bool ShowPremium
        {
            get { return HaveCredentials || showPremium; }
        }

        private CookieContainer cc = null;
        private CookieContainer Cookies
        {
            get
            {
                if (cc == null)
                {
                    cc = new CookieContainer();
                    Cookie c = new Cookie("dsc-geo", hardCodeGeo, "/", cookieDomain);
                    //Cookie c = new Cookie("dsc-geo", HttpUtility.UrlEncode(GetWebData(geoUrl)), "/", cookieDomain);
                    Cookies.Add(c);
                    if (HaveCredentials)
                    {
                        GetWebData(secureUrl + "secure/api/v1/user/auth/login", string.Format("realm_code={0}&username={1}&password={2}&remember_me=true", realmCode, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password)), cc);
                        if (cc.Count <= 1)
                        {
                            cc = null;
                            throw new OnlineVideosException("Unable to log in with given credentials");
                        }
                    }
                }
                return cc;
            }
        }

        public override int DiscoverDynamicCategories()
        {
            if (needsPremium && !HaveCredentials)
                throw new OnlineVideosException("Please enter your credentials");
            Category programs = new Category() { Name = programCategoryName, HasSubCategories = true };
            programs.Other = (Func<List<Category>>)(() => GetProgramsCategories(programs, 0));
            Settings.Categories.Add(programs);

            if (ShowPremium)
            {
                string data = GetWebData(baseUrl + sportUrl, cookies: Cookies);
                Regex rgx = new Regex(@"data-page-id=""(?<pageId>\d*)"".*?data-module-id=""(?<moduleId>\d*)""");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    string pageId = m.Groups["pageId"].Value;
                    string moduleId = m.Groups["moduleId"].Value;
                    RssLink sport = new RssLink() { Name = sportCategoryName + (HaveCredentials ? "" : " [Premium]"), HasSubCategories = false };
                    sport.Url = string.Format("{0}api/v2/ajax/modules?page_id={1}&module_id={2}&items=14&sort=sort_date_asc", baseUrl, pageId, moduleId) + "&page={0}";
                    Settings.Categories.Add(sport);
                }
                RssLink channels = new RssLink() { Name = channelCategoryName + (HaveCredentials ? "" : " [Premium]"), HasSubCategories = false };
                channels.Url = baseUrl + channelUrl;
                Settings.Categories.Add(channels);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            var method = category.Other as Func<List<Category>>;
            if (method != null)
            {
                List<Category> cats = method.Invoke();
                category.ParentCategory.SubCategories.AddRange(cats);
                return cats.Count;
            }
            return 0;
        }

        private List<Category> GetProgramsCategories(Category parentCategory, int page, string pageId = null, string moduleId = null, string query = null)
        {
            List<Category> cats = new List<Category>();
            bool isSearch = query != null;
            if (!isSearch && (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(moduleId)))
            {
                string data = GetWebData(baseUrl + programUrl, cookies: Cookies);
                Regex rgx = new Regex(@"data-page-id=""(?<pageId>\d*)"".*?data-module-id=""(?<moduleId>\d*)""");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    pageId = m.Groups["pageId"].Value;
                    moduleId = m.Groups["moduleId"].Value;
                }
                if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(moduleId))
                {
                    throw new OnlineVideosException("Could not get the " + programCategoryName + " category");
                }
            }
            string url;
            if (!isSearch)
                url = string.Format("{0}api/v2/ajax/modules?items=50&page_id={1}&module_id={2}&page={3}", baseUrl, pageId, moduleId, page);
            else
                url = string.Format("{0}api/v2/ajax/search/?q={1}&items=50&types=show&page={2}", baseUrl, query, page);

            JObject json = GetWebData<JObject>(url, cookies: Cookies);
            foreach (JToken show in json["data"])
            {
                bool isPremium = show["content_info"]["package_label"]["value"].Value<string>() == "Premium";
                if (ShowPremium || !isPremium)
                {
                    JToken taxonomyItem = show["taxonomy_items"].FirstOrDefault(ti => ti["type"].Value<string>() == "show");
                    if (taxonomyItem != null)
                    {
                        RssLink cat = new RssLink();
                        cat.ParentCategory = parentCategory;
                        cat.Name = show["title"].Value<string>();
                        cat.Name += isPremium && !HaveCredentials ? " [Premium]" : "";
                        cat.Description = show["description"].Value<string>();
                        string imageFile = show["image_data"]["file"].Value<string>();
                        cat.Thumb = "http://a5.res.cloudinary.com/dumrsasw1/image/upload/c_fill,h_245,w_368/" + imageFile;
                        int termId = taxonomyItem["term_id"].Value<int>();
                        cat.Url = string.Format("{0}api/v2/ajax/shows/{1}/seasons/?show_id={2}&items=14&sort=sort_date_asc", baseUrl, termId, termId) + "&page={0}";
                        uint episodes = 0;
                        if (taxonomyItem["metadata"]["episodes"] != null)
                            uint.TryParse(taxonomyItem["metadata"]["episodes"].Value<string>(), out episodes);
                        cat.EstimatedVideoCount = episodes;
                        cats.Add(cat);
                    }
                }
            }
            int pages = json["total_pages"].Value<int>();

            if (page < pages - 1)
            {
                NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                next.Other = (Func<List<Category>>)(() => GetProgramsCategories(parentCategory, page + 1, pageId, moduleId, query));
                cats.Add(next);
            }
            return cats;
        }

        private string currentUrl = string.Empty;
        private int currentPage = 0;
        public override List<VideoInfo> GetVideos(Category category)
        {
            currentUrl = (category as RssLink).Url;
            currentPage = 0;
            if (currentUrl == baseUrl + channelUrl)
            {
                return GetChannels();
            }
            return GetVideos();
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            return GetVideos();
        }

        private List<VideoInfo> GetChannels()
        {
            HasNextPage = false;
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(currentUrl, cookies: Cookies);
            HtmlNodeCollection channelNodes = doc.DocumentNode.SelectNodes("//div[@class='channel-logo']");
            foreach (HtmlNode channel in channelNodes)
            {
                HtmlNode a = channel.SelectSingleNode("a");
                if (a != null)
                {
                    HtmlNode img = a.SelectSingleNode("img");
                    if (img != null)
                    {
                        VideoInfo video = new VideoInfo();
                        video.VideoUrl = a.GetAttributeValue("data-page-loader", "");
                        video.Thumb = img.GetAttributeValue("src", "");
                        video.Title = img.GetAttributeValue("alt", "");
                        videos.Add(video);
                    }
                }
            }
            return videos;
        }

        private List<VideoInfo> GetVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = string.Format(currentUrl, currentPage);
            JObject json = GetWebData<JObject>(url, cookies: Cookies);
            if (json["data"] != null && json["data"].Type != JTokenType.Null)
            {
                foreach (JToken episode in json["data"])
                {
                    bool isPremium = episode["content_info"]["package_label"]["value"].Value<string>() == "Premium";
                    if (ShowPremium || !isPremium)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = episode["title"].Value<string>();
                        video.Title += isPremium && !HaveCredentials ? " [Premium]" : "";
                        if (episode["video_metadata_longDescription"] != null)
                            video.Description = episode["video_metadata_longDescription"].Value<string>();
                        if (string.IsNullOrEmpty(video.Description) && episode["description"] != null)
                            video.Description = episode["description"].Value<string>();
                        video.Thumb = episode["video_metadata_videoStillURL"].Value<string>();
                        video.VideoUrl = episode["id"].Value<string>();
                        int s = 0;
                        if (episode["season"] != null)
                            int.TryParse(episode["season"].Value<string>(), out s);
                        int e = 0;
                        if (episode["episode"] != null)
                            int.TryParse(episode["episode"].Value<string>(), out e);
                        if (s > 0 && e > 0)
                        {
                            JToken taxonomyItem = episode["taxonomy_items"].FirstOrDefault(taxi => taxi["type"].Value<string>() == "show");
                            if (taxonomyItem != null)
                            {
                                TrackingInfo ti = new TrackingInfo();
                                ti.Episode = (uint)e;
                                ti.Season = (uint)s;
                                ti.Title = taxonomyItem["name"].Value<string>();
                                ti.VideoKind = VideoKind.TvSeries;
                                video.Other = ti;
                            }
                            video.Title = (s + "x") + ((e > 9) ? e.ToString() : "0" + e.ToString()) + " - " + video.Title;
                        }
                        if (episode["video_metadata_first_startTime"] != null)
                        {
                            System.DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                            dt = dt.AddSeconds(episode["video_metadata_first_startTime"].Value<long>()).ToLocalTime();
                            video.Airdate = dt.ToString();
                        }
                        if (episode["video_metadata_length"] != null)
                        {
                            long seconds = episode["video_metadata_length"].Value<long>() / 1000;
                            if (seconds > 0)
                                video.Length = OnlineVideos.Helpers.TimeUtils.TimeFromSeconds(seconds.ToString());
                        }

                        videos.Add(video);
                    }
                }
            }
            HasNextPage = false;
            if (json["total_pages"] != null && json["total_pages"].Type != JTokenType.Null)
            {
                int pages = json["total_pages"].Value<int>();
                HasNextPage = currentPage < pages - 1;
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Settings.Player = PlayerType.Internal;
            //Workaround, cookies can exire fast. Force new login...
            cc = null;
            //End workaround...
            string url = string.Format("{0}secure/api/v2/user/authorization/stream/{1}?stream_type=hds", secureUrl, video.VideoUrl);
            JObject json = GetWebData<JObject>(url, cookies: Cookies, cache: false);
            if (json["type"].Value<string>() == "drm")
            {
                Settings.Player = PlayerType.Browser;
                url = string.Format("{0}api/v2/ajax/videos?video_id={1}&page=0&items=500", baseUrl, video.VideoUrl);
                json = GetWebData<JObject>(url, cookies: Cookies, cache: false);
                if (json["data"] != null && json["data"].Count() > 0 && json["data"].First()[srtSelector] != null)
                    return json["data"].First()["url"].Value<string>();
                else
                    throw new OnlineVideosException("Unable to play video");
            }
            url = json["hds"].Value<string>();
            if (url.EndsWith("master.f4m"))
            {
                //Need to use HLS... Why?
                url = string.Format("{0}secure/api/v2/user/authorization/stream/{1}?stream_type=hls", secureUrl, video.VideoUrl);
                json = GetWebData<JObject>(url, cookies: Cookies, cache: false);
                url = json["hls"].Value<string>();
                string data = GetWebData<string>(url, cookies: Cookies, cache: false);
                Regex rgx = new Regex(@"(?<url>.*.m3u8)");
                foreach (Match m in rgx.Matches(data))
                {
                    url = url.Replace("master.m3u8", m.Groups["url"].Value);
                    break;
                }
            }
            else
            {
                url = url + (url.Contains("?") ? "&" : "?") + "g=" + Utils.HelperUtils.GetRandomChars(12) + "&hdcore=3.8.0&plugin=flowplayer-3.8.0.0";
            }
            try
            {
                string subUrl = string.Format("{0}api/v2/ajax/videos?video_id={1}&page=0&items=500", baseUrl, video.VideoUrl);
                json = GetWebData<JObject>(subUrl, cookies: Cookies, cache: false);
                if (json["data"] != null && json["data"].Count() > 0 && json["data"].First()[srtSelector] != null)
                    video.SubtitleUrl = json["data"].First()[srtSelector].Value<string>();
            }
            catch { }
            return url;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is TrackingInfo)
                return video.Other as TrackingInfo;
            return base.GetTrackingInfo(video);
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            List<Category> cats = GetProgramsCategories(new Category(), 0, query: HttpUtility.UrlEncode(query));
            cats.ForEach(c => results.Add(c));
            return results;
        }


        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.BrowserUtilConnectors.DplayConnector"; }
        }

        string IBrowserSiteUtil.UserName
        {
            get { return username + "¥" + secureUrl + "login/"; }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }
    }
}

