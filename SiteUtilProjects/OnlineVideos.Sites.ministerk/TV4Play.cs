using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase
    {
        #region Config

        [Category("OnlineVideosUserConfiguration"), Description("TV4Play username"), LocalizableDisplayName("Username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), Description("TV4Play password"), LocalizableDisplayName("Password"), PasswordPropertyText(true)]
        protected string password = null;

        #endregion

        #region constants, vars and properties

        protected const string loginPostUrl = "https://account.services.tv4play.se/session/authenticate";
        protected const string showsUrl = "http://www.tv4play.se/api/programs?per_page=40&is_cmore=false&order_by=&page={0}&tags={1}";
        protected const string videosOfShowUrl = "http://www.tv4play.se/videos/search?node_nids_mode=any&per_page=100&sort_order=desc&page={0}";
        protected const string liveUrls = "http://www.tv4play.se/videos/search?node_nids_mode=any&per_page=100&sort_order=asc&page={0}&is_live=true&type=video";
        protected const string videoPlayUrl = "https://prima.tv4play.se/api/web/asset/{0}/play";
        protected const string episodeSearchUrl = "http://www.tv4play.se/videos/search?is_channel=false&type=episode&per_page=100&page={0}&q=";

        private Dictionary<string, string> tvCategories = new Dictionary<string, string>()
        {
            { "Alla program", "" },
            { "Deckare", "deckare" },
            { "Djur", "djur" },
            { "Dokumentärt", "dokument%C3%A4rt" },
            { "Drama", "drama" },
            { "Humor", "humor" },
            { "Hus & hem", "hus%20%26%20hem" },
            { "Livsstil", "livsstil" },
            { "Mat & bakning", "mat%20%26%20bakning" },
            { "Nöje", "n%C3%B6je" },
            { "Relationer", "relationer" },
            { "Samhälle & fakta", "samh%C3%A4lle%20%26%20fakta" },
            { "Sport", "sport" },
            { "Övernaturligt", "%C3%B6vernaturligt" }
        };

        private int currentPage = 1;
        private string currentUrl = "";
        protected CookieContainer cc = new CookieContainer();

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
                string postData = string.Format("username={0}&password={1}&client=tv4play-web", HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password));
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
                    isLoggedIn = true;
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
            Category tv = new Category() { Name = "Program", SubCategories = new List<Category>(), HasSubCategories = true, SubCategoriesDiscovered = true };
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
            RssLink nyheter = new RssLink() { Name = "Nyheter", Url = "nyheterna", SubCategories = new List<Category>(), HasSubCategories = true };
            nyheter.Other = (Func<List<Category>>)(() => GetShow(nyheter));
            Settings.Categories.Add(nyheter);
            RssLink live = new RssLink() { Name = "Livesändningar", Url = liveUrls, SubCategories = new List<Category>(), HasSubCategories = false };
            Settings.Categories.Add(live);
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

        private List<Category> GetShow(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            RssLink helaProgram = new RssLink() { Name = "Hela program", HasSubCategories = false, ParentCategory = parentCategory, Url = videosOfShowUrl + "&node_nids=" + (parentCategory as RssLink).Url + "&type=episode" };
            HtmlNodeCollection nodes = GetWebData<HtmlDocument>(string.Format(helaProgram.Url, 1)).DocumentNode.SelectNodes("//li[contains(@class,'card')]");
            if (nodes != null && nodes.Count > 0)
                categories.Add(helaProgram);
            RssLink klipp = new RssLink() { Name = "Klipp", HasSubCategories = false, ParentCategory = parentCategory, Url = videosOfShowUrl + "&node_nids=" + (parentCategory as RssLink).Url + "&type=clip" };
            nodes = GetWebData<HtmlDocument>(string.Format(klipp.Url, 1)).DocumentNode.SelectNodes("//li[contains(@class,'card')]");
            if (nodes != null && nodes.Count > 0)
                categories.Add(klipp);
            return categories;
        }

        private List<Category> GetShows(int page, Category parentCategory = null, string tag = "")
        {
            List<Category> shows = new List<Category>();
            string url = string.Format(showsUrl, page, tag);
            JArray items = JArray.Parse(GetWebData<string>(url));
            foreach (JToken item in items)
            {
                RssLink show = new RssLink();

                show.Name = (item["name"] == null) ? "" : item["name"].Value<string>();
                show.Url = (item["nid"] == null) ? "" : item["nid"].Value<string>();
                show.Description = (item["description"] == null) ? "" : item["description"].Value<string>();
                show.Thumb = (item["program_image"] == null) ? "" : item["program_image"].Value<string>();
                show.ParentCategory = parentCategory;
                show.SubCategories = new List<Category>();
                show.HasSubCategories = true;
                show.Other = (Func<List<Category>>)(() => GetShow(show));
                shows.Add(show);
            }

            if (items.Count > 39)
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
                    HtmlNode descNode = item.SelectSingleNode(".//div[contains(@class,'card__info-description')]");
                    video.Description = HttpUtility.HtmlDecode((descNode != null ? descNode.InnerText.Trim() : ""));
                    HtmlNode airNode = item.SelectSingleNode(".//span[contains(@class,'card__info-published')]/span");
                    video.Airdate = (airNode != null ? airNode.InnerText.Trim() : "");
                    videos.Add(video);
                }
            }
            HasNextPage = doc.DocumentNode.SelectNodes("//footer/p/a") != null;
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
            string url = string.Format(videoPlayUrl, video.VideoUrl);
            XmlDocument xDoc = GetWebData<XmlDocument>(url, cookies: cc);
            XmlNode errorElement = xDoc.SelectSingleNode("//error");
            if (errorElement != null)
            {
                throw new OnlineVideosException(errorElement.SelectSingleNode("./description/text()").InnerText);
            }
            XmlNode drm = xDoc.SelectSingleNode("//drmProtected");
            if (drm != null && drm.InnerText.Trim().ToLower() == "true")
            {
                    throw new OnlineVideosException("DRM protected content, sorry! :/");
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

    }
}