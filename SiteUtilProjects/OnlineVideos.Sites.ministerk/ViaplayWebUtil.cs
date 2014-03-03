using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ViaplayWebUtil : SiteUtilBase, IBrowserSiteUtil
    {
        #region Config
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Viaplay username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Viaplay password")]
        protected string password = null;
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
                    _translations = GetWebData<JObject>("https://cms-api.viaplay.se/translations/web");
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

        private JObject MyGetWebData(string url)
        {
            JObject data;
            if (!HaveCredentials())
            {
                data = GetWebData<JObject>(url);
                return data;
            }
            data = GetWebData<JObject>(url, cc);
            if (data["user"] == null)
            {
                data = GetWebData<JObject>(string.Format(LoginUrl, HttpUtility.UrlEncode(UserName), HttpUtility.UrlEncode(Password)), cc);
                if ((bool)data["success"])
                {
                    data = GetWebData<JObject>(url, cc);
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
            data = GetWebData(url, cc);
            if (data.Contains("Unauthorized"))
            {
                JObject logindata = GetWebData<JObject>(string.Format(LoginUrl, HttpUtility.UrlEncode(UserName), HttpUtility.UrlEncode(Password)), cc);
                if ((bool)logindata["success"])
                {
                    data = GetWebData(url, cc);
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
            if (category.ParentCategory == null)
                return category.Name == GetTranslation("Series");
            return IsSeries(category.ParentCategory);
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
                    Name = (string)starred["title"],
                    Url = (string)starred["href"],
                    HasSubCategories = false,
                });
            }
            if (watched != null)
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = (string)watched["title"],
                    Url = (string)watched["href"],
                    HasSubCategories = false,
                    Other = _watched
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
                case _search:
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
                                (parentCategory as RssLink).EstimatedVideoCount += (uint)block["totalProductCount"];
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
                case _block:
                case _sorting:
                    JToken firstBlock;
                    JToken sortingBlocks = data["_embedded"]["viaplay:blocks"];
                    if (sortingBlocks == null)
                        firstBlock = data;
                    else
                        firstBlock = sortingBlocks.First(b => ((string)b["type"]).Contains("list"));

                    (parentCategory as RssLink).EstimatedVideoCount = (uint)firstBlock["totalProductCount"];
                    JToken content;
                    RssLink cat;
                    List<Category> categories = new List<Category>();
                    foreach (var product in firstBlock["_embedded"]["viaplay:products"])
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
                            cat.Thumb = content["images"]["landscape"] != null ? (string)content["images"]["landscape"]["url"] : string.Empty;
                            cat.Description = content["series"]["synopsis"] != null ? (string)content["series"]["synopsis"] : (string)content["synopsis"];
                        }
                        else
                        {
                            cat.Name = (string)content["title"];
                            cat.Thumb = content["images"]["boxart"] != null ? (string)content["images"]["boxart"]["url"] : string.Empty;
                            cat.Description = (string)content["synopsis"];
                        }
                        cat.Url = (string)product["_links"]["viaplay:page"]["href"];
                        cat.Other = _seriesBlock;
                        parentCategory.SubCategories.Add(cat);
                    }
                    if (firstBlock["_links"]["next"] != null)
                    {
                        parentCategory.SubCategories.Add(new NextPageCategory()
                        {
                            Url = (string)firstBlock["_links"]["next"]["href"],
                            ParentCategory = parentCategory
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
            return DiscoverSubCategories(category.ParentCategory, category.Url);
        }
        #endregion

        #region Video

        private VideoInfo getProduct(JToken product)
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

            other.Add("starred", (user != null && user["starred"] != null && (bool)user["starred"]).ToString());
            if (product["_links"]["viaplay:star"] != null)
                other.Add("starUrl", ((string)product["_links"]["viaplay:star"]["href"]).Replace("{starred}", string.Empty));
            if (product["_links"]["viaplay:watched"] != null)
                other.Add("watchedUrl", ((string)product["_links"]["viaplay:watched"]["href"]).Replace("{watched}", string.Empty));
            if (content["duration"] != null && content["duration"]["milliseconds"] != null)
                other.Add("duration", ((int)content["duration"]["milliseconds"]).ToString());

            string lenght = string.Empty;
            if (content["duration"] != null && content["duration"]["readable"] != null)
                lenght = (string)content["duration"]["readable"];

            bool isEpisode = (string)product["type"] == "episode";
            string title;
            if (isEpisode)
            {
                if (string.IsNullOrEmpty((string)content["series"]["episodeTitle"]))
                {
                    title = content["series"]["title"] != null ? (string)content["series"]["title"] + " -" : string.Empty;
                    title += content["series"]["season"] != null ? " " + GetTranslation("Season", "Season") + " " + (int)content["series"]["season"]["seasonNumber"] : string.Empty;
                    title += content["series"]["episodeNumber"] != null ? " " + GetTranslation("Episode", "Episode") + " " + (int)content["series"]["episodeNumber"] : string.Empty;
                }
                else
                {
                    title = (string)content["series"]["episodeTitle"];
                }
            }
            else
            {
                title = (string)content["title"];
            }

            return new VideoInfo()
            {
                Title = title,
                ImageUrl = (string)product["type"] == "movie" ? (string)content["images"]["boxart"]["url"] : (string)content["images"]["landscape"]["url"],
                Description = (string)content["synopsis"],
                VideoUrl = (string)product["_links"]["viaplay:page"]["href"],
                Airdate = airTime,
                Length = lenght,
                Other = other
            };
        }

        private List<VideoInfo> getVideos(JToken block, out uint totalProductCount)
        {
            totalProductCount = 0;
            totalProductCount += (uint)block["totalProductCount"];
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
                    videos.Add(getProduct(product));
                }
            }
            else
            {
                JToken product = block["_embedded"]["viaplay:product"];
                if (product != null)
                {
                    videos.Add(getProduct(product));
                }
            }
            return videos;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            var data = MyGetWebData(nextPageVideosUrl);
            uint dummy = 0;
            return getVideos(data, out dummy);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            var data = MyGetWebData((category as RssLink).Url);
            List<VideoInfo> videos = new List<VideoInfo>();
            uint count;
            uint totalProductCount = 0;
            var blocks = data["_embedded"]["viaplay:blocks"];
            if (blocks != null)
            {
                string loopType = "list";
                if (blocks.Any(b => (string)b["type"] == "starred"))
                    loopType = "starred";
                else if (blocks.Any(b => (string)b["type"] == "product"))
                    loopType = "product";
                foreach (JToken block in blocks.Where(b => (string)b["type"] == loopType))
                {
                    count = 0;
                    getVideos(block, out count).ForEach(v => videos.Add(v));
                    totalProductCount += count;
                }
            }
            else
            {
                getVideos(data, out totalProductCount).ForEach(v => videos.Add(v));
            }
            (category as RssLink).EstimatedVideoCount = totalProductCount;
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            if (!HaveCredentials())
                throw new OnlineVideosException(GetTranslation("Log in", "Log in"));
            string url = video.VideoUrl.Replace(ApiUrl, string.Empty);
            return url;
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

        public override List<OnlineVideos.ISearchResultItem> DoSearch(string query)
        {
            RssLink cat = new RssLink()
            {
                Name = GetTranslation("Search", "Search"),
                Url = ApiUrl + "/search?query=" + HttpUtility.UrlEncode(query),
                Other = _search,
                SubCategories = new List<Category>()
            };
            List<ISearchResultItem> results = new List<ISearchResultItem>();
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
            if (HaveCredentials() && selectedItem != null && selectedItem.Other != null && selectedItem.Other is SerializableDictionary<string,string>)
            {
                var other = selectedItem.Other as SerializableDictionary<string, string>;

                if (other.ContainsKey("starred") && other.ContainsKey("starUrl"))
                {
                    ContextMenuEntry starMenuEntry = new ContextMenuEntry();
                    bool starred = false;
                    bool.TryParse((string)other["starred"],out starred);
                    if (starred)
                        starMenuEntry.DisplayText = GetTranslation("Unstar","Unstar");
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
                string data = MyGetWebStringData((selectedItem.Other as SerializableDictionary<string, string>)["watchedUrl"] + "false");
                result.ExecutionResultMessage = GetTranslation("Remove from History", "Remove from History");
                if (data.Contains("OK"))
                {
                    result.RefreshCurrentItems = true;
                    result.ExecutionResultMessage += ": OK";
                }
                else
                    result.ExecutionResultMessage += ": Error";
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion
        #endregion
    }
}
