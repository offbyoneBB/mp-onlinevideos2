using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class ViaplayWebUtil : LatestVideosSiteUtilBase, IBrowserSiteUtil
    {
        #region Config
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Viaplay username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Viaplay password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Prefer internal player"), Description("Try to play videos in Mediaportal. If not possible use browser player as fallback")]
        protected bool preferInternal = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Always use browser player for sports"), Description("Always use browser player for sports")]
        protected bool preferBrowserSport = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show help category"), Description("Enable or disable help category (Link to forum - http://tinyurl.com/olv-viaplay)")]
        protected bool showHelpCategory = true;
        #endregion

        #region Constants
        protected const string _typeVod = "vod";
        #region States
        protected const string _section = "SECTION";
        protected const string _filterGroup = "FILTER_GROUP";
        protected const string _filter = "FILTER";
        protected const string _search = "SEARCH";
        protected const string _sorting = "SORTING";
        protected const string _block = "BLOCK";
        protected const string _seriesBlock = "PRODUCT";
        protected const string _watched = "WATCHED";
        protected const string _latest = "LATEST";
        #endregion
        #endregion

        #region Members
        protected CookieContainer cc = new CookieContainer();
        protected string nextPageVideosUrl = string.Empty;
        private JObject _translations = null;
        #endregion

        #region BrowserSiteUtil
        public string UserName
        {
            get
            {
                return username;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }
        }

        public string ConnectorEntityTypeName
        {
            get
            {
                string connector;
                switch (Settings.Language)
                {
                    case "sv":
                        connector = "OnlineVideos.Sites.BrowserUtilConnectors.ViaplayConnectorSv";
                        break;
                    case "da":
                        connector = "OnlineVideos.Sites.BrowserUtilConnectors.ViaplayConnectorDa";
                        break;
                    case "fi":
                        connector = "OnlineVideos.Sites.BrowserUtilConnectors.ViaplayConnectorFi";
                        break;
                    case "no":
                        connector = "OnlineVideos.Sites.BrowserUtilConnectors.ViaplayConnectorNo";
                        break;
                    default:
                        connector = string.Empty;
                        break;
                }
                return connector;
            }
        }
        #endregion

        #region Localization
        protected JObject Translations
        {
            get
            {
                if (_translations == null)
                    _translations = GetWebData<JObject>("https://cms-service.viaplay.se/translations/web");
                return _translations;
            }
        }

        protected string ApiUrl
        {
            get
            {
                string apiUrl;
                switch (Settings.Language)
                {
                case "sv":
                    apiUrl = @"https://content.viaplay.se/pc-se";
                    break;
                case "da":
                    apiUrl = @"https://content.viaplay.dk/pc-dk";
                    break;
                case "fi":
                    apiUrl = @"https://content.viaplay.fi/pc-fi";
                    break;
                case "no":
                    apiUrl = @"https://content.viaplay.no/pc-no";
                    break;
                default:
                    apiUrl = string.Empty;
                    break;
                }
                return apiUrl;
            }
        }
        protected string AndroidApiUrl
        {
            get
            {
                string apiUrl;
                switch (Settings.Language)
                {
                    case "sv":
                        apiUrl = @"https://content.viaplay.se/androidnodrmv2-se";
                        break;
                    case "da":
                        apiUrl = @"https://content.viaplay.dk/androidnodrmv2-dk";
                        break;
                    case "fi":
                        apiUrl = @"https://content.viaplay.fi/androidnodrmv2-fi";
                        break;
                    case "no":
                        apiUrl = @"https://content.viaplay.no/androidnodrmv2-no";
                        break;
                    default:
                        apiUrl = string.Empty;
                        break;
                }
                return apiUrl;
            }
        }

        protected string LatestUrl
        {
            get
            {
                string apiUrl;
                switch (Settings.Language)
                {
                    case "sv":
                        apiUrl = @"/film/samtliga?sort=recently_added";
                        break;
                    case "da":
                        apiUrl = @"/film/alle?sort=recently_added";
                        break;
                    case "fi":
                        apiUrl = @"/leffat/kaikki?sort=recently_added";
                        break;
                    case "no":
                        apiUrl = @"/filmer/alle?sort=recently_added";
                        break;
                    default:
                        apiUrl = string.Empty;
                        break;
                }
                return apiUrl;
            }
        }

        protected string LanguageCode
        {
            get
            {
                string languageCode;
                switch (Settings.Language)
                {
                    case "sv":
                        languageCode = "sv-se";
                        break;
                    case "da":
                        languageCode = "da-dk";
                        break;
                    case "fi":
                        languageCode = "fi-fi";
                        break;
                    case "no":
                        languageCode = "nb-no";
                        break;
                    default:
                        languageCode = string.Empty;
                        break;
                }
                return languageCode;
            }
        }

        protected string LoginUrl
        {
            get
            {
                string loginUrl;
                switch (Settings.Language)
                {
                    case "sv":
                        loginUrl = @"https://login.viaplay.se/api/login/v1?deviceKey=pc-se&username={0}&password={1}";
                        break;
                    case "da":
                        loginUrl = @"https://login.viaplay.dk/api/login/v1?deviceKey=pc-dk&username={0}&password={1}";
                        break;
                    case "fi":
                        loginUrl = @"https://login.viaplay.fi/api/login/v1?deviceKey=pc-fi&username={0}&password={1}";
                        break;
                    case "no":
                        loginUrl = @"https://login.viaplay.fi/api/login/v1?deviceKey=pc-fi&username={0}&password={1}";
                        break;
                    default:
                        loginUrl = string.Empty;
                        break;
                }
                return loginUrl;
            }
        }

        protected string GetTranslation(string key, string defaultValue = "")
        {
            var keyVal = Translations[LanguageCode][key];
            if (keyVal != null && !string.IsNullOrEmpty((string)keyVal["text"]))
                return (string)keyVal["text"];
            else
                return defaultValue;
        }

        protected string EroticCategoryName
        {
            get
            {
                string eroticCategoryName;
                switch (Settings.Language)
                {
                    case "sv":
                        eroticCategoryName = "Erotik";
                        break;
                    case "da":
                        eroticCategoryName = "Erotik";
                        break;
                    case "fi":
                        eroticCategoryName = "Erotiikka";
                        break;
                    case "no":
                        eroticCategoryName = "Erotikk";
                        break;
                    default:
                        eroticCategoryName = string.Empty;
                        break;
                }
                return eroticCategoryName;
            }
        }
        #endregion

        #region Helpers

        private bool HaveCredentials()
        {
            return !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(password);
        }

        private bool IsBlockedMainCategory(string categoryTitle)
        {
            return categoryTitle == "OS"
                || IsBlockedCategory(categoryTitle);
        }

        private bool IsBlockedCategory(string categoryTitle)
        {
            return categoryTitle == GetTranslation("Rental store")
                || categoryTitle == EroticCategoryName
                || categoryTitle == "NHL GameCenter";
        }

        private JObject MyGetWebData(string url, string postData = null)
        {
            JObject data;
            url = url.Replace("{?dtg}", "");
            if (!HaveCredentials())
            {
                data = GetWebData<JObject>(url,postData);
                return data;
            }
            data = GetWebData<JObject>(url, postData, cc);
            if (data["user"] == null)
            {
                data = GetWebData<JObject>(string.Format(LoginUrl, HttpUtility.UrlEncode(UserName), HttpUtility.UrlEncode(Password)), cookies: cc);
                if ((bool)data["success"])
                {
                    data = GetWebData<JObject>(url, postData, cc);
                }
                else
                {
                    cc = new CookieContainer();
                    throw new OnlineVideosException(GetTranslation("Username and password does not match. Please try again.", "Username and password does not match. Please try again."));
                }
            }
            return data;
        }

        private string MyGetWebStringData(string url)
        {
            string data;
            if (!HaveCredentials())
            {
                data = GetWebData(url);
                return data;
            }
            data = GetWebData(url, cookies: cc);
            if (data.Contains("Unauthorized"))
            {
                JObject logindata = GetWebData<JObject>(string.Format(LoginUrl, HttpUtility.UrlEncode(UserName), HttpUtility.UrlEncode(Password)), cookies: cc);
                if ((bool)logindata["success"])
                {
                    data = GetWebData(url, cookies: cc);
                }
                else
                {
                    cc = new CookieContainer();
                    throw new OnlineVideosException(GetTranslation("Username and password does not match. Please try again.", "Username and password does not match. Please try again."));
                }
            }
            return data;
        }

        private bool IsSeries(Category category)
        {
            return category.Name.ToLower() == GetTranslation("Series").ToLower() || category.Name.ToLower() == GetTranslation("Children series").ToLower() || category.Name.ToLower() == GetTranslation("All children series").ToLower() || (category.ParentCategory != null && IsSeries(category.ParentCategory));
        }
        #endregion

        #region SiteUtil

        #region Category
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            JObject data = MyGetWebData(ApiUrl);
            var sections = data["_links"]["viaplay:sections"];
            foreach (JToken section in sections.Where(s => ((string)s["type"]) == _typeVod && !IsBlockedMainCategory((string)s["title"])))
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = (string)section["title"],
                    Url = (string)section["href"],
                    HasSubCategories = true,
                    SubCategories = new List<Category>(),
                    Other = _section
                });
            }
            var starred = data["_links"]["viaplay:starred"];
            var watched = data["_links"]["viaplay:watched"];
            if (starred != null)
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = GetTranslation("Starred", "Starred"),
                    Url = (string)starred["href"],
                    HasSubCategories = false,
                });
            }
            if (watched != null)
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = GetTranslation("Watched", "Watched"),
                    Url = (string)watched["href"],
                    HasSubCategories = false,
                    Other = _watched
                });
            }
            if (showHelpCategory)
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = GetTranslation("Viaplay at your service", "Do you need help?"),
                });
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        //To complex and complicated, but "works"
        private int DiscoverSubCategories(Category parentCategory, string url)
        {
            JObject data = MyGetWebData(url);
            switch (parentCategory.Other as string)
            {
                case _section:
                    var filters = data["_links"]["viaplay:categoryFilters"];
                    if (filters != null && filters.Count() > 0)
                    {
                        Dictionary<string, RssLink> filterGroups = new Dictionary<string, RssLink>();
                        string filterGroupName;
                        foreach (JToken filter in filters.Where(f => !IsBlockedCategory((string)f["title"])))
                        {
                            filterGroupName = filter["group"] != null ? (string)filter["group"]["title"] : string.Empty;
                            if (string.IsNullOrEmpty(filterGroupName))
                            {
                                //"Show all"
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    ParentCategory = parentCategory,
                                    Name = (string)filter["title"],
                                    Url = (string)filter["href"],
                                    HasSubCategories = true,
                                    SubCategories = new List<Category>(),
                                    Other = _filter
                                });
                            }
                            else
                            {
                                //"Group, categories, themes"
                                if (!filterGroups.ContainsKey(filterGroupName))
                                {
                                    filterGroups.Add(filterGroupName, new RssLink()
                                    {
                                        ParentCategory = parentCategory,
                                        Name = filterGroupName,
                                        HasSubCategories = true,
                                        SubCategoriesDiscovered = true,
                                        SubCategories = new List<Category>(),
                                        Other = _section
                                    });
                                }
                                filterGroups[filterGroupName].SubCategories.Add(new RssLink()
                                {
                                    ParentCategory = filterGroups[filterGroupName],
                                    Name = (string)filter["title"],
                                    Url = (string)filter["href"],
                                    HasSubCategories = true,
                                    SubCategories = new List<Category>(),
                                    Other = _filter
                                });

                            }
                        }
                        filterGroups.Values.ToList().ForEach(c => parentCategory.SubCategories.Add(c));
                    }
                    else //Try blocks... Sport has no categoty filters
                    {
                        if (data["_embedded"]["viaplay:blocks"] != null)
                        {
                            foreach (JToken block in data["_embedded"]["viaplay:blocks"])
                            {
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    ParentCategory = parentCategory,
                                    HasSubCategories = true,
                                    Name = (string)block["title"],
                                    Url = (string)block["_links"]["self"]["href"],
                                    Other = _block,
                                    SubCategories = new List<Category>()
                                });
                            }
                        }
                    }
                    break;
                case _filter:
                    var sortings = data["_links"]["viaplay:sortings"];
                    if (sortings != null && sortings.Count() > 0)
                    {
                        bool isSeries = IsSeries(parentCategory);
                        foreach (JToken sorting in sortings)
                        {
                            parentCategory.SubCategories.Add(new RssLink()
                            {
                                ParentCategory = parentCategory,
                                Name = (string)sorting["title"],
                                Url = ((string)sorting["href"]).Replace("{&letter}", string.Empty),
                                HasSubCategories = isSeries || ((string)sorting["id"]) == "alphabetical",
                                SubCategories = new List<Category>(),
                                Other = _sorting
                            });
                        }
                    }
                    else //Try blocks... Search and things from related material
                    {
                        if (data["_embedded"]["viaplay:blocks"] != null)
                        {
                            foreach (JToken block in data["_embedded"]["viaplay:blocks"].Where(b => !IsBlockedCategory((string)b["title"])))
                            {
                                if (block["_embedded"]["viaplay:products"] != null && block["_embedded"]["viaplay:products"].Count() > 0)
                                {
                                    bool isSeries = (string)block["_embedded"]["viaplay:products"].First()["type"] == "series";
                                    parentCategory.SubCategories.Add(new RssLink()
                                    {
                                        ParentCategory = parentCategory,
                                        HasSubCategories = isSeries,
                                        Name = (string)block["title"],
                                        Description = (string)block["synopsis"],
                                        Url = (string)block["_links"]["self"]["href"],
                                        Other = _block,
                                        SubCategories = new List<Category>()
                                    });
                                }
                            }
                        }
                    }
                    break;
                case _search:
                case _block:
                case _sorting:
                    JToken firstBlock;
                    JToken sortingBlocks = data["_embedded"]["viaplay:blocks"];
                    if (sortingBlocks == null)
                        firstBlock = data;
                    else
                        firstBlock = sortingBlocks.First(b => ((string)b["type"]).ToLower().Contains("list"));

                    JToken content;
                    RssLink cat;
                    List<Category> categories = new List<Category>();
                    foreach (var product in firstBlock["_embedded"]["viaplay:products"])
                    {
                        JToken system = product["system"];
                        if (system == null || system["availability"] == null || system["availability"]["planInfo"] == null || !system["availability"]["planInfo"]["isRental"].Value<bool>())
                        {
                            cat = new RssLink();
                            content = product["content"];
                            bool isSeries = (string)product["type"] == "series";
                            cat.ParentCategory = parentCategory;
                            cat.HasSubCategories = isSeries;
                            cat.SubCategories = new List<Category>();
                            if (isSeries)
                            {
                                cat.Name = (string)content["series"]["title"];
                                cat.Thumb = content["images"] != null && content["images"]["landscape"] != null ? (string)content["images"]["landscape"]["url"] : string.Empty;
                                cat.Description = content["series"]["synopsis"] != null ? (string)content["series"]["synopsis"] : (string)content["synopsis"];
                            }
                            else
                            {
                                cat.Name = (string)content["title"];
                                cat.Thumb = content["images"] != null && content["images"]["boxart"] != null ? (string)content["images"]["boxart"]["url"] : string.Empty;
                                cat.Description = (string)content["synopsis"];
                            }
                            cat.Url = (string)product["_links"]["viaplay:page"]["href"];
                            cat.Other = _seriesBlock;
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                    if (firstBlock["_links"]["next"] != null)
                    {
                        parentCategory.SubCategories.Add(new NextPageCategory()
                        {
                            Url = (string)firstBlock["_links"]["next"]["href"],
                            ParentCategory = parentCategory,
                            Other = parentCategory.Other
                        });
                    }
                    break;
                case _seriesBlock:
                    JToken seasonBlocks = data["_embedded"]["viaplay:blocks"];
                    foreach (JToken seasonBlock in seasonBlocks.Where(b => (string)b["type"] == "season-list"))
                    {
                        string seasonUrl = (string)seasonBlock["_links"]["self"]["href"];
                        string sportSeriesUrl = ApiUrl + "/" + GetTranslation("Sport").ToLower();
                        string seriesSeriesUrl = ApiUrl + "/" + GetTranslation("Series").ToLower();
                        if (seasonUrl.StartsWith(sportSeriesUrl))
                            seasonUrl = seasonUrl.Replace(sportSeriesUrl, seriesSeriesUrl);
                        parentCategory.SubCategories.Add(new RssLink()
                        {
                            Name = string.Format("{0} {1}", GetTranslation("Season", "Season"), (string)seasonBlock["title"]),
                            Url = seasonUrl,
                            ParentCategory = parentCategory,
                            HasSubCategories = false,

                        });
                    }
                    break;
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            return DiscoverSubCategories(parentCategory, (parentCategory as RssLink).Url);
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            if (category.ParentCategory.Other == null)
                category.ParentCategory.Other = category.Other;
            return DiscoverSubCategories(category.ParentCategory, category.Url);
        }
        #endregion

        #region Video

        private VideoInfo GetProduct(JToken product)
        {
            SerializableDictionary<string, string> other = new SerializableDictionary<string, string>();
            JToken content = product["content"];
            JToken user = product["user"];
            JToken epg = product["epg"];
            string airTime = string.Empty;
            if (epg != null)
            {
                airTime = ((DateTime)epg["start"]).ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale);
            }
            string format = "{0}|{1}|{2}|{3}|{4}|{5}";
            other.Add("starred", (user != null && user["starred"] != null && (bool)user["starred"]).ToString());
            if (product["type"] != null && product["type"].Value<string>() == "movie")
            {
                string tracktitle = string.Empty;
                string imdb = string.Empty;
                string year = string.Empty;
                if (content["title"] != null)
                    tracktitle = content["title"].Value<string>();
                if (content["imdb"] != null && content["imdb"]["id"] != null)
                    imdb = content["imdb"]["id"].Value<string>();
                if (content["production"] != null && content["production"]["year"] != null)
                    year = content["production"]["year"].ToString();

                other.Add("tracking", string.Format(format,"Movie", tracktitle,year,imdb,string.Empty,string.Empty));

            }
            else if (product["type"] != null && product["type"].Value<string>() == "episode")
            {
                string tracktitle = string.Empty;
                string season = string.Empty;
                string episode = string.Empty;
                string year = string.Empty;
                if (content["series"] != null)
                {
                    if (content["series"]["title"] != null)
                        tracktitle = content["series"]["title"].Value<string>();
                    if (content["series"]["episodeNumber"] != null)
                        episode = content["series"]["episodeNumber"].ToString();
                    if (content["series"]["season"] != null && content["series"]["season"]["seasonNumber"] != null)
                        season = content["series"]["season"]["seasonNumber"].ToString();
                }
                if (content["production"] != null && content["production"]["year"] != null)
                    year = content["production"]["year"].ToString();
                other.Add("tracking", string.Format(format, "TvSeries", tracktitle, year, string.Empty, season, episode));
            }
            if (product["_links"]["viaplay:star"] != null)
                other.Add("starUrl", ((string)product["_links"]["viaplay:star"]["href"]).Replace("{starred}", string.Empty));
            if (product["_links"]["viaplay:deleteProgress"] != null)
                other.Add("watchedUrl", ((string)product["_links"]["viaplay:deleteProgress"]["href"]));
            if (product["_links"]["viaplay:peopleSearch"] != null)
                other.Add("peopleSearchUrl", ((string)product["_links"]["viaplay:peopleSearch"]["href"]));
            var people = content["people"];
            if (people != null)
            {
                if (people["directors"] != null)
                    other.Add("directors", (string)(people["directors"].Aggregate((current, next) => (string)current + ";" + (string)next)));
                if (people["actors"] != null)
                    other.Add("actors", (string)(people["actors"].Aggregate((current, next) => (string)current + ";" + (string)next)));
            }


            string lenght = string.Empty;
            if (content["duration"] != null && content["duration"]["readable"] != null)
                lenght = (string)content["duration"]["readable"];

            bool isEpisode = (string)product["type"] == "episode";
            string title;
            if (isEpisode)
            {
                int s = content["series"]["season"] != null ? (int)content["series"]["season"]["seasonNumber"]:0;
                int e = content["series"]["episodeNumber"] != null?(int)content["series"]["episodeNumber"]:0;
                string stitle = content["series"]["title"] != null ? (string)content["series"]["title"] : string.Empty;
                string ctitle = (string)content["title"];
                string etitle = content["series"]["episodeTitle"] != null ? (string)content["series"]["episodeTitle"]:string.Empty;
                title = !string.IsNullOrEmpty(stitle)? stitle + " - " : string.Empty;
                title += s > 9 ? "S" + s.ToString()  : "S0" + s.ToString();
                title += e > 9 ? "E" + e.ToString() : "E0" + e.ToString();
                if (stitle == ctitle)
                    title += " " + etitle;
                else
                    title += " " + ctitle;
            }
            else
            {
                title = (string)content["title"];
            }

            return new VideoInfo()
            {
                Title = title,
                Thumb = (string)product["type"] == "movie" ? (string)content["images"]["boxart"]["url"] : (string)content["images"]["landscape"]["url"],
                Description = (string)content["synopsis"],
                VideoUrl = (string)product["_links"]["viaplay:page"]["href"],
                Airdate = airTime,
                Length = lenght,
                Other = other
            };
        }

        private List<VideoInfo> getVideos(JToken block)
        {
            HasNextPage = block["_links"] != null && block["_links"]["next"] != null && block["_links"]["next"]["href"] != null;
            if (HasNextPage)
            {
                nextPageVideosUrl = (string)block["_links"]["next"]["href"];
            }
            else
            {
                nextPageVideosUrl = string.Empty;
            }
            List<VideoInfo> videos = new List<VideoInfo>();
            JToken products = block["_embedded"]["viaplay:products"];
            if (products != null)
            {
                foreach (var product in products)
                {
                    videos.Add(GetProduct(product));
                }
            }
            else
            {
                JToken product = block["_embedded"]["viaplay:product"];
                if (product != null)
                {
                    videos.Add(GetProduct(product));
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            var data = MyGetWebData(nextPageVideosUrl);
            return getVideos(data);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Name == GetTranslation("Viaplay at your service", "Do you need help?"))
                throw new OnlineVideosException("Forum http://tinyurl.com/olv-viaplay");
            JObject data;
            if (category.Other is string && (category.Other as string) == _latest)
                data = GetWebData<JObject>((category as RssLink).Url);
            else
                data = MyGetWebData((category as RssLink).Url);
            List<VideoInfo> videos = new List<VideoInfo>();
            var blocks = data["_embedded"]["viaplay:blocks"];
            if (blocks != null)
            {
                string loopType = "dynamicList";
                if (blocks.Any(b => (string)b["type"] == "starred"))
                    loopType = "starred";
                else if (blocks.Any(b => (string)b["type"] == "product"))
                    loopType = "product";
                else if (blocks.Any(b => (string)b["type"] == "list"))
                    loopType = "list";
                foreach (JToken block in blocks.Where(b => (string)b["type"] == loopType))
                {
                    getVideos(block).ForEach(v => videos.Add(v));
                }
            }
            else
            {
                getVideos(data).ForEach(v => videos.Add(v));
            }
            return videos;
        }

        private void GetSubtitle(VideoInfo video, JObject data)
        {
            try
            {
                JToken subs = data["_links"]["viaplay:sami"];
                if (subs != null)
                {
                    JToken sub = subs.FirstOrDefault(t => t["languageCode"].Value<string>() == Settings.Language);
                    if (sub != null && sub["href"] != null)
                    {
                        string srtFormat = "{0}\r\n{1} --> {2}\r\n{3}\r\n\r\n";
                        string sami = sub["href"].Value<string>();
                        sami = MyGetWebStringData(sami);
                        Regex rgx = new Regex(@"<SYNC START=(?<time>\d+)[^>]>[^<]*<P[^>]*>(?<text>[^(?:\n|\r)]*)");
                        MatchCollection matches = rgx.Matches(sami);
                        if (matches != null && matches.Count > 0)
                        {
                            string subtitle = "";
                            int id = 1;
                            for (int x = 0; x < matches.Count - 1; x++)
                            {
                                string text = HttpUtility.HtmlDecode(matches[x].Groups["text"].Value).Trim();
                                if (string.IsNullOrEmpty(text))
                                    continue;
                                int time = 0;
                                if (!int.TryParse(matches[x].Groups["time"].Value, out time))
                                    continue;
                                if (x >= matches.Count)
                                    continue;
                                int time2 = 0;
                                string tm = matches[x + 1].Groups["time"].Value;
                                if (!int.TryParse(tm, out time2))
                                    continue;
                                System.TimeSpan ts = TimeSpan.FromMilliseconds(time * 10);
                                string startTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                                ts = TimeSpan.FromMilliseconds(time2 * 10);
                                string endTime = new DateTime(ts.Ticks).ToString("HH:mm:ss,fff");
                                subtitle += string.Format(srtFormat, id, startTime, endTime, text.Replace("<br>", "\r\n"));
                                id++;
                            }
                            video.SubtitleText = subtitle;
                        }
                    }
                }
            }
            catch { }
        }

        private string GetInternalUrl(VideoInfo video, bool inPlaylist)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            try
            {
                JObject data = MyGetWebData(video.VideoUrl.Replace(ApiUrl, AndroidApiUrl) + "?partial=true&block=1");
                if (preferBrowserSport && data["_embedded"]["viaplay:product"]["type"].Value<string>() == "sport")
                {
                    video.PlaybackOptions.Clear();
                    return string.Empty;
                }
                JToken stream = data["_embedded"]["viaplay:product"]["_links"]["viaplay:stream"];
                if (stream == null)
                {
                    video.PlaybackOptions.Clear();
                    return string.Empty;
                }
                string url = stream["href"].Value<string>();
                url = url.Replace("{?deviceId,deviceName,deviceType,userAgent}", "?deviceId=x&deviceName=x&deviceType=x&userAgent=x");
                data = MyGetWebData(url);
                url = data["_links"]["viaplay:playlist"]["href"].Value<string>();
                string m3u8 = MyGetWebStringData(url);
                Regex rgx = new Regex(@"RESOLUTION=(?<res>\d+x\d+).*?[\r|\n]*(?<url>.*?m3u8)");
                foreach (Match m in rgx.Matches(m3u8))
                {
                    video.PlaybackOptions.Add(m.Groups["res"].Value, Regex.Replace(url, @"([^/]*)?\?", delegate(Match match)
                    {
                        return m.Groups["url"].Value + "?";
                    }));
                }
                if (video.PlaybackOptions.Count < 1)
                {
                    video.PlaybackOptions.Clear();
                    return string.Empty;
                }
                video.PlaybackOptions = video.PlaybackOptions.OrderByDescending((p) =>
                {
                    string[] size = p.Key.Split('x');
                    string width = "0";
                    int parsedWidth = 0;
                    if (size.Count() == 2)
                        width = size[0];
                    int.TryParse(width, out parsedWidth);
                    return parsedWidth;
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                url = video.PlaybackOptions.First().Value;
                if (inPlaylist)
                    video.PlaybackOptions.Clear();
                // Subtitle
                GetSubtitle(video, data);
                return url;
            }
            catch
            {
                video.PlaybackOptions.Clear();
            }
            return string.Empty;

        }
        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            if (!HaveCredentials())
                throw new OnlineVideosException(GetTranslation("Log in", "Log in"));
            if (preferInternal)
            {
                string url = GetInternalUrl(video, inPlaylist);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Settings.Player = PlayerType.Internal;
                    return new List<string>() { url };
                }
            }
            Settings.Player = PlayerType.Browser;
            return new List<string>() { video.VideoUrl.Replace(ApiUrl, string.Empty) };
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (Settings.Player == PlayerType.Internal)
            {//tracking
                if(video.Other is Dictionary<string,string>)
                {
                    Dictionary<string, string> d = video.Other as Dictionary<string, string>;
                    if(d.ContainsKey("tracking"))
                    {
                        Regex rgx = new Regex(@"(?<VideoKind>[^\|]*)\|(?<Title>[^\|]*)\|(?<Year>[^\|]*)\|(?<ID_IMDB>[^\|]*)\|(?<Season>[^\|]*)\|(?<Episode>.*)");
                        Match m = rgx.Match(d["tracking"]);
                        ITrackingInfo ti = new TrackingInfo() { Regex = m };
                        return ti;
                    }
                }
            }
            return base.GetTrackingInfo(video);
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
            RssLink cat = new RssLink()
            {
                Name = GetTranslation("Search", "Search"),
                Url = ApiUrl + "/search?query=" + HttpUtility.UrlEncode(query) + "&block=1&partial=1&pageNumber=1",
                Other = _search,
                SubCategories = new List<Category>()
            };
            List<SearchResultItem> results = new List<SearchResultItem>();
            DiscoverSubCategories(cat);
            foreach (Category c in cat.SubCategories)
                results.Add(c);
            return results;
        }
        #endregion

        #region Contextmenu
        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> menuItems = new List<ContextMenuEntry>();
            if (HaveCredentials() && selectedItem != null && selectedItem.Other != null && selectedItem.Other is SerializableDictionary<string, string>)
            {
                var other = selectedItem.Other as SerializableDictionary<string, string>;

                if (other.ContainsKey("starred") && other.ContainsKey("starUrl"))
                {
                    ContextMenuEntry starMenuEntry = new ContextMenuEntry();
                    bool starred = false;
                    bool.TryParse((string)other["starred"], out starred);
                    if (starred)
                        starMenuEntry.DisplayText = GetTranslation("Unstar", "Unstar");
                    else
                        starMenuEntry.DisplayText = GetTranslation("Star", "Star");
                    menuItems.Add(starMenuEntry);
                }
            }
            if (HaveCredentials() && selectedCategory.Other != null && selectedCategory.Other is string && (selectedCategory.Other as string) == _watched && selectedItem != null && selectedItem.Other != null && selectedItem.Other is SerializableDictionary<string, string>)
            {
                var other = selectedItem.Other as SerializableDictionary<string, string>;
                if (other.ContainsKey("watchedUrl"))
                {
                    ContextMenuEntry watchedMenuEntry = new ContextMenuEntry();
                    watchedMenuEntry.DisplayText = selectedCategory.Name + ": " + GetTranslation("Remove from History", "Remove from History");
                    menuItems.Add(watchedMenuEntry);
                }
            }
            if (selectedItem != null && selectedItem.Other != null && selectedItem.Other is SerializableDictionary<string, string>)
            {
                var other = selectedItem.Other as SerializableDictionary<string, string>;
                if (other.ContainsKey("peopleSearchUrl"))
                {

                    if (other.ContainsKey("actors"))
                    {
                        ContextMenuEntry actorsMenuEntry = new ContextMenuEntry();
                        actorsMenuEntry.DisplayText = GetTranslation("Cast", "Cast");
                        menuItems.Add(actorsMenuEntry);
                    }
                    if (other.ContainsKey("directors"))
                    {
                        ContextMenuEntry directorsMenuEntry = new ContextMenuEntry();
                        directorsMenuEntry.DisplayText = GetTranslation("Director", "Director");
                        menuItems.Add(directorsMenuEntry);
                    }
                }
            }
            return menuItems;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            ContextMenuExecutionResult result = new ContextMenuExecutionResult();
            if (choice.DisplayText == GetTranslation("Unstar", "Unstar"))
            {
                JToken data = MyGetWebData((selectedItem.Other as SerializableDictionary<string, string>)["starUrl"] + "false");
                result.RefreshCurrentItems = true;
                if (data["success"] != null && (bool)data["success"])
                    result.ExecutionResultMessage = GetTranslation("Unstar", "Unstar") + ": OK";
                else
                    result.ExecutionResultMessage = GetTranslation("Unstar", "Unstar") + ": Error";
                return result;
            }

            if (choice.DisplayText == GetTranslation("Star", "Star"))
            {
                JToken data = MyGetWebData((selectedItem.Other as SerializableDictionary<string, string>)["starUrl"] + "true");
                result.ExecutionResultMessage = GetTranslation("Star", "Star");
                if (data["success"] != null && (bool)data["success"])
                {
                    result.RefreshCurrentItems = true;
                    result.ExecutionResultMessage += ": OK";
                }
                else
                    result.ExecutionResultMessage += ": Error";
                return result;
            }
            if (choice.DisplayText == selectedCategory.Name + ": " + GetTranslation("Remove from History", "Remove from History"))
            {
                JObject json = MyGetWebData(ApiUrl);
                string userId = json["user"]["userId"].Value<string>();
                json = MyGetWebData((selectedItem.Other as SerializableDictionary<string, string>)["watchedUrl"].Replace("{userId}",userId), string.Empty);
                result.ExecutionResultMessage = GetTranslation("Remove from History", "Remove from History");
                if (json["status"] != null && json["status"].Value<string>() == "ok")
                {
                    result.RefreshCurrentItems = true;
                    result.ExecutionResultMessage += ": OK";
                }
                else
                    result.ExecutionResultMessage += ": Error";
                return result;
            }
            //People search
            if (choice.DisplayText != null && choice.DisplayText is string && (string)choice.DisplayText == GetTranslation("Cast", "Cast"))
            {
                List<SearchResultItem> results = new List<SearchResultItem>();
                foreach (string actor in (selectedItem.Other as SerializableDictionary<string, string>)["actors"].Split(';'))
                {
                    results.Add(new RssLink()
                    {
                        Name = actor,
                        Url = ApiUrl + "/search?query=" + HttpUtility.UrlEncode("\"" + actor + "\"") + "&block=1&partial=1&pageNumber=1",
                        Other = _search,
                        ParentCategory = selectedCategory,
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });
                }
                result.ResultItems = results;
                return result;
            }
            if (choice.DisplayText != null && choice.DisplayText is string && (string)choice.DisplayText == GetTranslation("Director", "Director"))
            {
                List<SearchResultItem> results = new List<SearchResultItem>();
                foreach (string director in (selectedItem.Other as SerializableDictionary<string, string>)["directors"].Split(';'))
                {
                    results.Add(new RssLink()
                    {
                        Name = director,
                        Url = ApiUrl + "/search?query=" + HttpUtility.UrlEncode("\"" + director + "\"") + "&block=1&partial=1&pageNumber=1",
                        Other = _search,
                        ParentCategory = selectedCategory,
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });
                }
                result.ResultItems = results;
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion
        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            RssLink latest = new RssLink() { Name = "Latest Videos", Url = ApiUrl + LatestUrl, Other = _latest };
            List<VideoInfo> videos = GetVideos(latest);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        #endregion
    }
}
