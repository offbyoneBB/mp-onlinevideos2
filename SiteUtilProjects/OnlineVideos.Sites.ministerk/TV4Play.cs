using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase, IBrowserSiteUtil
    {
        #region Config

        [Category("OnlineVideosUserConfiguration"), Description("TV4Play username"), LocalizableDisplayName("Username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), Description("TV4Play password"), LocalizableDisplayName("Password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show loading spinner"), Description("Show the loading spinner in the Browser Player")]
        protected bool showLoadingSpinner = true;
        //[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Prefer internal player"), Description("Try to play videos in Mediaportal. If not possible use browser player as fallback")]
        protected bool preferInternal = true;

        #endregion

        #region constants, vars and properties

        protected const string loginUrl = "https://www.tv4play.se/session/new";
        protected const string loginPostUrl = "https://account.services.tv4play.se/session/authenticate";
        protected const string showsUrl = "http://www.tv4play.se/tv/more_programs?order_by=name&per_page=40&page={0}";
        protected const string helaProgramUrl = "http://www.tv4play.se/videos/episodes_search?per_page=100&sort_order=desc&is_live=false&type=video&nodes_mode=any&page={0}&node_nids=";
        protected const string klippUrl = "http://www.tv4play.se/videos/search?node_nids_mode=any&per_page=100&sort_order=desc&type=clip&page={0}&node_nids=";
        protected const string liveUrl = "http://www.tv4play.se/videos/episodes_search?per_page=100&sort_order=asc&is_live=true&type=video&nodes_mode=any&page={0}&node_nids=";
        protected const string videoPlayUrl = "https://prima.tv4play.se/api/web/asset/{0}/play";
        protected const string videoAssetUrl = "https://www.tv4play.se/player/assets/common/{0}.json";
        protected const string filmerUrl = "http://www.tv4play.se/film/tags?order_by=name";
        protected const string barnUrl = "http://www.tv4play.se/barn";
        protected const string barnFilmerUrl = "http://www.tv4play.se/film/tags?genre=barn&order_by=name";
        protected const string familjFilmerUrl = "http://www.tv4play.se/film/tags?genre=familj&order_by=name";
        protected const string episodeSearchUrl = "http://www.tv4play.se/videos/search?is_channel=false&type=episode&per_page=100&page={0}&q=";

        private Dictionary<string, string> tvCategories = new Dictionary<string, string>()
        {
            { "Alla program", "" },
            { "Drama", "drama" },
            { "Dokumentärer", "dokument%C3%A4rer" },
            { "Hem & fritid", "hem%20%26%20fritid" },
            { "Humor", "humor" },
            { "Nyheter & debatt", "nyheter%20%26%20debatt" },
            { "Nöje", "n%C3%B6je" },
            { "Sport", "sport" },
            { "Deckare", "deckare" },
            { "Mat & dryck", "mat%20%26%20dryck" },
            { "Kändisar", "k%C3%A4ndisar" },
            { "Musik", "musik" },
            { "Reality", "reality" },
            { "Övernaturligt", "%C3%B6vernaturligt" }
        };

        private int currentPage = 1;
        private string currentUrl = "";
        protected CookieContainer cc = new CookieContainer();

        bool showPremium = false;

        protected bool HasLogin
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password));
            }
        }

        #endregion

        #region Log in

        bool isLoggedIn = false;

        private void login()
        {
            if (!HasLogin)
            {
                throw new OnlineVideosException("Fyll i inloggningsuppgifter");                
            }
            else if (!isLoggedIn)
            {
                cc = new CookieContainer();
                HtmlDocument htmlDoc = GetWebData<HtmlDocument>(loginUrl, cookies: cc);
                HtmlNode input = htmlDoc.DocumentNode.SelectSingleNode("//input[@id = 'authenticity_token']");
                string authenticity_token = input.GetAttributeValue("value", "");
                string postData = string.Format("username={0}&password={1}&client=web&authenticity_token={2}&https=", HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password), HttpUtility.UrlEncode(authenticity_token));
                JObject json = GetWebData<JObject>(loginPostUrl, postData, cc);
                try
                {
                    cc.Add(new Cookie("user_name", HttpUtility.UrlEncode(username), "/", ".tv4play.se"));
                    cc.Add(new Cookie("username", HttpUtility.UrlEncode(json["user_id"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("user_id", HttpUtility.UrlEncode(json["user_id"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("contact_id", HttpUtility.UrlEncode(json["contact_id"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("rememberme", HttpUtility.UrlEncode(json["session_token"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("sessionToken", HttpUtility.UrlEncode(json["session_token"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("JSESSIONID", HttpUtility.UrlEncode(json["vimond_session_token"].Value<string>()), "/", ".tv4play.se"));
                    cc.Add(new Cookie("pSessionToken", HttpUtility.UrlEncode(json["vimond_remember_me"].Value<string>()), "/", ".tv4play.se"));
                    // cc.Add(new Cookie("tv4_token", HttpUtility.UrlEncode(GetWebData("").Trim()), "/", ".tv4play.se"));
                    isLoggedIn = true;
                    showPremium = json["active_subscriptions"].Values() != null && (json["active_subscriptions"].Values().Count() > 0 && json["active_subscriptions"].First()["product_group_nid"].Value<string>() != "freemium");
                } 
                catch
                {
                    throw new OnlineVideosException("Inloggningen misslyckades");
                }
            }
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            login();
            if (!showPremium)
            {
                GetShows(page: 1).ForEach((Category s) => Settings.Categories.Add(s));
            }
            else
            {
                Category tv = new Category() { Name = "TV", SubCategories = new List<Category>(), HasSubCategories = true, SubCategoriesDiscovered = true };
                foreach (KeyValuePair<string, string> keyValuePair in tvCategories)
                {
                    Category category = new Category();
                    category.Name = keyValuePair.Key;
                    category.HasSubCategories = true;
                    category.ParentCategory = tv;
                    category.Other = (Func<List<Category>>)(() => GetShows(1, category, keyValuePair.Value));
                    tv.SubCategories.Add(category);
                }
                Settings.Categories.Add(tv);
                Category film = new Category() { Name = "Film", HasSubCategories = false };
                film.Other = (Func<List<VideoInfo>>)(() => GetFilmer(filmerUrl));
                Settings.Categories.Add(film);
                Category sport = new Category() { Name = "Sport", HasSubCategories = false };
                sport.Other = (Func<List<VideoInfo>>)(() => GetSport());
                Settings.Categories.Add(sport);
                RssLink nyheter = new RssLink() { Name = "Nyheter", Url = "nyheterna", SubCategories = new List<Category>(), HasSubCategories = true };
                nyheter.Other = (Func<List<Category>>)(() => GetShow(nyheter));
                Settings.Categories.Add(nyheter);
                Category barn = new Category() { Name = "Barn", SubCategories = new List<Category>(), HasSubCategories = true, SubCategoriesDiscovered = true };
                Category barnprogram = new Category() { Name = "Alla barnprogram", HasSubCategories = true, ParentCategory = barn };
                barnprogram.Other = (Func<List<Category>>)(() => GetShows(1, barnprogram, "barn"));
                barn.SubCategories.Add(barnprogram);
                Category barnserier = new Category() { Name = "Barnserier", HasSubCategories = true, ParentCategory = barn };
                barnserier.Other = (Func<List<Category>>)(() => GetBarnserier(barnserier));
                barn.SubCategories.Add(barnserier);
                Category barnFilmer = new Category() { Name = "Barnfilm", HasSubCategories = false, ParentCategory = barn };
                barnFilmer.Other = (Func<List<VideoInfo>>)(() => GetFilmer(barnFilmerUrl));
                barn.SubCategories.Add(barnFilmer);
                Category familjFilmer = new Category() { Name = "Familjefilm", HasSubCategories = false, ParentCategory = barn };
                familjFilmer.Other = (Func<List<VideoInfo>>)(() => GetFilmer(familjFilmerUrl));
                barn.SubCategories.Add(familjFilmer);
                Settings.Categories.Add(barn);
                Category kanaler = new Category() { Name = "Kanaler", HasSubCategories = false };
                kanaler.Other = (Func<List<VideoInfo>>)(() => GetKanaler());
                Settings.Categories.Add(kanaler);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            if (category.ParentCategory == null)
            {
                Settings.Categories.Remove(category);
            }
            else
            {
                category.ParentCategory.SubCategories.Remove(category);
            }
            Func<List<Category>> method = category.Other as Func<List<Category>>;
            if (method != null)
            {
                List<Category> cats = method();
                if (cats != null)
                {
                    if (category.ParentCategory == null)
                    {
                        cats.ForEach((Category c) => Settings.Categories.Add(c));
                    }
                    else
                    {
                        category.ParentCategory.SubCategories.AddRange(cats);
                    }
                    return cats.Count;
                }
            }
            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Func<List<Category>> method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        private List<Category> GetBarnserier(Category barnserier)
        {
            List<Category> categories = new List<Category>();
            HtmlNodeCollection nodes = GetWebData<HtmlDocument>(barnUrl).DocumentNode.SelectNodes("//section[@class='module']");
            HtmlNode seriesNode = null;
            foreach (HtmlNode node in nodes)
            {
                if (node.InnerText.ToLower().Contains("barnserier"))
                {
                    seriesNode = node;
                    break;
                }
            }
            if (seriesNode != null)
            {
                HtmlNodeCollection items = seriesNode.SelectNodes(".//li[@class='card']");
                if (items != null)
                {
                    foreach (HtmlNode item in items)
                    {
                        RssLink cat = new RssLink();
                        cat.ParentCategory = barnserier;
                        HtmlNode imgNode = item.SelectSingleNode(".//img");
                        cat.Thumb = imgNode != null ? imgNode.GetAttributeValue("src", "") : "";
                        cat.Name = HttpUtility.HtmlDecode(imgNode != null ? imgNode.GetAttributeValue("alt", "") : "");
                        HtmlNode aNode = item.SelectSingleNode(".//h3/a");
                        cat.Url = "http://www.tv4play.se" + aNode.GetAttributeValue("href", "");
                        cat.Other = (Func<List<VideoInfo>>)(() => GetFilmer(cat.Url));
                        categories.Add(cat);
                    }
                }

            }
            return categories;
        }

        private List<Category> GetShow(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            RssLink helaProgram = new RssLink() { Name = "Hela program", HasSubCategories = false, ParentCategory = parentCategory, Url = helaProgramUrl + (parentCategory as RssLink).Url };
            HtmlNodeCollection nodes = GetWebData<HtmlDocument>(string.Format(helaProgram.Url, 1)).DocumentNode.SelectNodes("//li[contains(@class,'episode')]");
            if (nodes != null && nodes.Count > 0)
                categories.Add(helaProgram);
            RssLink klipp = new RssLink() { Name = "Klipp", HasSubCategories = false, ParentCategory = parentCategory, Url = klippUrl + (parentCategory as RssLink).Url };
            nodes = GetWebData<HtmlDocument>(string.Format(klipp.Url, 1)).DocumentNode.SelectNodes("//li[contains(@class,'clip')]");
            if (nodes != null && nodes.Count > 0)
                categories.Add(klipp);
            RssLink live = new RssLink() { Name = "Live", HasSubCategories = false, ParentCategory = parentCategory, Url = liveUrl + (parentCategory as RssLink).Url };
            nodes = GetWebData<HtmlDocument>(string.Format(live.Url, 1)).DocumentNode.SelectNodes("//li[contains(@class,'episode')]");
            if (nodes != null && nodes.Count > 0)
                categories.Add(live);
            return categories;
        }

        private List<Category> GetShows(int page, Category parentCategory = null, string tag = null)
        {
            List<Category> shows = new List<Category>();
            string url = string.Concat(string.Concat(string.Format(showsUrl, page), (showPremium ? "" : "&is_free=true")), (string.IsNullOrEmpty(tag) ? "" : string.Concat("&tags=", tag)));
            HtmlDocument htmlDoc = GetWebData<HtmlDocument>(url);
            HtmlNodeCollection items = htmlDoc.DocumentNode.SelectNodes("//li[@class='card']");
            if (items != null)
            {
                foreach (HtmlNode item in items)
                {
                    RssLink show = new RssLink();
                    HtmlNode h3 = item.SelectSingleNode(".//h3");
                    if (h3 != null)
                    {
                        show.Name = HttpUtility.HtmlDecode(h3.InnerText.Trim());
                        show.ParentCategory = parentCategory;
                        show.Url = item.GetAttributeValue("data-nid", "");
                        show.HasSubCategories = true;
                        show.SubCategories = new List<Category>();
                        show.Other = (Func<List<Category>>)(() => GetShow(show));
                        HtmlNode img = item.SelectSingleNode(".//img");
                        if (img != null)
                        {
                            show.Thumb = img.GetAttributeValue("data-original", "");
                        }
                        shows.Add(show);
                    }
                }
            }
            if (htmlDoc.DocumentNode.SelectNodes("//footer/p/a") != null)
            {
                NextPageCategory next = new NextPageCategory();
                next.ParentCategory = parentCategory;
                next.Other = (Func<List<Category>>)(() => GetShows(page + 1, parentCategory, tag));
                shows.Add(next);
            }
            return shows;
        }

        #endregion

        #region Videos

        private List<VideoInfo> GetKanaler()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlNodeCollection nodes = GetWebData<HtmlDocument>("http://www.tv4play.se/kanaler/tv4").DocumentNode.SelectNodes("//li[contains(@class,'js-channel-info')]");
            foreach (HtmlNode node in nodes)
            {
                JObject json = JObject.Parse(HttpUtility.HtmlDecode(node.GetAttributeValue("data-asset", "")));
                VideoInfo video = new VideoInfo();
                video.VideoUrl = json["id"].Value<string>();
                video.Title = HttpUtility.HtmlDecode(json["title"].Value<string>());
                video.Description = HttpUtility.HtmlDecode(json["description"].Value<string>());
                video.Thumb = json["image"].Value<string>();
                videos.Add(video);
            }
            return videos;
        }

        private List<VideoInfo> GetSport()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlNodeCollection nodes = GetWebData<HtmlDocument>("http://www.tv4play.se/sport").DocumentNode.SelectNodes("//li[contains(@class,'sport_listing')]");
            foreach (HtmlNode node in nodes)
            {
                VideoInfo video = new VideoInfo();
                HtmlNode a = node.SelectSingleNode(".//a");
                video.VideoUrl = a.GetAttributeValue("href", "").Replace("/sport/", "");
                video.Title = HttpUtility.HtmlDecode(node.SelectSingleNode(".//span[contains(@class,'sport_listing-sport')]").InnerText.Trim());
                video.Airdate = node.SelectSingleNode(".//span[contains(@class,'time')]").InnerText.Trim();
                video.Description = node.SelectSingleNode(".//div[contains(@class,'sports-game-info')]").InnerText.Trim();
                video.Description += " - " + node.SelectSingleNode(".//span[contains(@class,'ports-league-info')]").InnerText.Trim();
                if (node.SelectSingleNode(".//span[contains(@class,'premium_sport-badge')]") != null)
                    video.Description += " - C More Sport";
                video.Description = HttpUtility.HtmlDecode(video.Description);
                videos.Add(video);
            }
            return videos;
        }

        private List<VideoInfo> GetVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(string.Format(currentUrl, currentPage));
            HtmlNodeCollection items = doc.DocumentNode.SelectNodes(".//li[contains(@class,'episode') or contains(@class,'clip') or contains(@class,'card')]");
            if (items != null)
            {
                foreach (HtmlNode item in items)
                {
                    VideoInfo video = new VideoInfo();
                    HtmlNodeCollection premium = item.SelectNodes(".//span[contains(@class,'premium-badge')]");
                    if ((showPremium || premium == null || premium.Count == 0))
                    {
                        HtmlNode titleNode = item.SelectSingleNode(".//h3");
                        video = new VideoInfo();
                        video.VideoUrl = item.GetAttributeValue("data-video-id", "");
                        video.Title = HttpUtility.HtmlDecode(titleNode != null ? titleNode.InnerText.Trim() : "");
                        HtmlNode imgNode = item.SelectSingleNode(".//img");
                        video.Thumb = imgNode != null ? imgNode.GetAttributeValue("src", "") : "";
                        if (string.IsNullOrWhiteSpace(video.Thumb))
                        {
                            video.Thumb = (imgNode != null ? imgNode.GetAttributeValue("data-original", "") : "");
                        }
                        HtmlNode descNode = item.SelectSingleNode(".//p[contains(@class,'description')]");
                        video.Description = HttpUtility.HtmlDecode((descNode != null ? descNode.InnerText.Trim() : ""));
                        HtmlNode airNode = item.SelectSingleNode(".//p[contains(@class,'broadcast')]/span");
                        video.Airdate = (airNode != null ? airNode.InnerText.Trim() : "");
                        videos.Add(video);
                    }
                }
            }
            HasNextPage = doc.DocumentNode.SelectNodes("//footer/p/a") != null;
            return videos;
        }

        private List<VideoInfo> GetFilmer(string url)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(url);
            HtmlNodeCollection items = doc.DocumentNode.SelectNodes("//li[@class='card']");
            if (items != null)
            {
                foreach (HtmlNode item in items)
                {
                    VideoInfo video = new VideoInfo();
                    HtmlNode imgNode = item.SelectSingleNode(".//img");
                    video.Thumb = imgNode != null ? imgNode.GetAttributeValue("src", "") : "";
                    video.Title = HttpUtility.HtmlDecode(imgNode != null ? imgNode.GetAttributeValue("alt", "") : "");
                    HtmlNode aNode = item.SelectSingleNode(".//h3/a");
                    video.VideoUrl = aNode.GetAttributeValue("href", "").Replace("/film/", "");
                    HtmlNode lengthNode = item.SelectSingleNode(".//span[contains(@class,'length')]");
                    video.Length = (lengthNode != null ? lengthNode.InnerText.Replace(",", "").Trim() : "");
                    HtmlNode airNode = item.SelectSingleNode(".//span[contains(@class,'year')]");
                    video.Airdate = (airNode != null ? airNode.InnerText.Replace(",", "").Trim() : "");
                    videos.Add(video);
                }
            }
            return videos;

        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            if (category.Other is Func<List<VideoInfo>>)
            {
                Func<List<VideoInfo>> method = category.Other as Func<List<VideoInfo>>;
                if (method != null)
                {
                    return method();
                }
                return new List<VideoInfo>();
            }
            else
            {
                currentPage = 1;
                currentUrl = (category as RssLink).Url;
                return GetVideos();
            }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            return GetVideos();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            login();
            Settings.Player = PlayerType.Internal;
            string url = string.Format(videoPlayUrl, video.VideoUrl);
            XmlDocument xDoc = GetWebData<XmlDocument>(url, cookies: cc);
            XmlNode errorElement = xDoc.SelectSingleNode("//error");
            if (errorElement != null)
            {
                throw new OnlineVideosException(errorElement.SelectSingleNode("./description/text()").InnerText);
            }
            XmlNode drm = xDoc.SelectSingleNode("//drmProtected");
            if (!preferInternal || (drm == null ? false : drm.InnerText.Trim().ToLower() == "true"))
            {
                Settings.Player = PlayerType.Browser;
                JObject json = GetWebData<JObject>(string.Format(videoAssetUrl, video.VideoUrl), cookies: cc);
                return json["share_url"].Value<string>();
            }
            foreach (XmlElement item in xDoc.SelectNodes("//items/item"))
            {
                string mediaformat = item.GetElementsByTagName("mediaFormat")[0].InnerText.ToLower();
                string itemUrl = item.GetElementsByTagName("url")[0].InnerText.Trim();
                if (mediaformat.StartsWith("mp4") && itemUrl.ToLower().EndsWith(".f4m"))
                {
                    url = string.Concat(itemUrl, "?hdcore=3.5.0&g=", HelperUtils.GetRandomChars(12));
                }
                else if (mediaformat.StartsWith("mp4") && itemUrl.ToLower().Contains(".f4m?"))
                {
                    url = string.Concat(itemUrl, "&hdcore=3.5.0&g=", HelperUtils.GetRandomChars(12));
                }
                else if (mediaformat.StartsWith("smi"))
                {
                    video.SubtitleText = GetWebData(itemUrl, cookies: cc, encoding: System.Text.Encoding.Default);
                }
            }
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

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            HasNextPage = false;
            currentPage = 1;
            currentUrl = episodeSearchUrl + HttpUtility.UrlEncode(query);
            List<SearchResultItem> results = new List<SearchResultItem>();
            GetVideos().ForEach(v => results.Add(v));
            return results;
        }

        #endregion

        #region Browser

        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get { return "OnlineVideos.Sites.BrowserUtilConnectors.TV4PlayConnector"; }
        }

        string IBrowserSiteUtil.UserName
        {
            get { return username + (showLoadingSpinner ? "SHOWLOADING" : "") + (showPremium ? "PREMIUM" : ""); }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }

        #endregion
    }
}