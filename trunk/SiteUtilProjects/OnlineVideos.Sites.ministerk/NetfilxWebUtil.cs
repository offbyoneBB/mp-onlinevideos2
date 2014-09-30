using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{

    public class NetfilxWebUtil : SiteUtilBase, IBrowserSiteUtil
    {
        #region Config

        public enum BrowseSortOrders
        {
            SuggestionsForYou,
            HighestRated,
            MaturityRating,
            YearReleased,
            A_Z,
            Z_A
        }

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Netflix email")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Netflix password")]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Sort titles in Browse by..."), Description("Sort titles in Browse by...")]
        protected BrowseSortOrders browseSort = BrowseSortOrders.SuggestionsForYou;
        #endregion

        #region Lots of urls
        private string loginUrl = @"https://www2.netflix.com/Login";
        private string homeUrl = @"https://www2.netflix.com/Login"; //loginurl redirects to main page if logged on, makes it easy to check login
        private string loginPostData = @"authURL={0}&email={1}&password={2}&RememberMe=on";
        private string myListUrl = @"http://www2.netflix.com/MyList";
        private string movieUrl = @"http://www.netflix.com/WiMovie/";
        private string genreUrl = @"{0}/{1}/wigenre?genreId={2}&full=false&from={3}&to={4}&secure=false&_retry=0&orderBy={5}";
        private string kidsUrl = @"http://www2.netflix.com/Kids";
        private string seasonDetailsUrl = @"http://www2.netflix.com/WiMovie/{0}?actionMethod=seasonDetails&seasonId={1}&seasonKind=ELECTRONIC";
        private string playerUrl = @"http://www2.netflix.com/WiPlayer?movieid={0}";/*&trkid={1}*/
        private string searchUrl = @"{0}/desktop/search/instantsearch?esn=www&term={1}&ngv=500"; //500 results maximum
        private string switchProfileUrl = @"{0}/desktop/account/profiles/switch?switchProfileGuid={1}";
        #endregion

        #region Private parts
        protected CookieContainer cc = null;
        private WebProxy proxy = null; //new WebProxy("127.0.0.1", 8888); //Debug proxy
        private JObject currentProfile = null;
        private JArray profiles = null;
        private string _apiRoot = "";
        private string ApiRoot
        {
            get
            {
                if (string.IsNullOrEmpty(_apiRoot))
                {
                    string data = MyGetWebData(homeUrl);
                    Regex rgx = new Regex(@"\""API_ROOT\"":""([^\""]*)");
                    Match m = rgx.Match(data);
                    if (m.Success)
                    {
                        _apiRoot = m.Groups[1].Value;
                    }
                }
                return _apiRoot;
            }
        }
        private string ProfileName
        {
            get
            {
                return currentProfile == null ? "" : currentProfile["profileName"].Value<string>();
            }
        }
        private string ProfileToken
        {
            get
            {
                return currentProfile == null ? "" : currentProfile["token"].Value<string>();
            }
        }
        private bool IsKidsProfile
         {
            get
            {
                return currentProfile == null ? false : currentProfile["isKidsProfile"].Value<bool>();
            }
         }
        private string ProfileCategoryName
        {
            get { return "Switch profile (" + ProfileName + ")"; }
        }

        private string BrowseSort
        {
            get
            {
                switch (browseSort)
                {
                    case BrowseSortOrders.A_Z:
                        return "az";
                    case BrowseSortOrders.HighestRated:
                        return "rt";
                    case BrowseSortOrders.MaturityRating:
                        return "mr";
                    case BrowseSortOrders.YearReleased:
                        return "yr";
                    case BrowseSortOrders.Z_A:
                        return "za";
                    case BrowseSortOrders.SuggestionsForYou:
                    default:
                        return "su";
                }
            }
        }
        #endregion
        
        #region Login User Profile

        private bool HaveCredentials
        {
            get { return !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(password); }
        }

        private bool IsLoggedIn(string data)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(data);
            return doc.DocumentNode.SelectSingleNode("//form[@id = 'login-form']") == null;
        }

        private void Login()
        {
            cc = new CookieContainer();
            string url = GetRedirectedUrl(loginUrl).Replace("entrytrap", "Login");
            HtmlNode docNode = GetWebData<HtmlDocument>(url, cc, proxy: proxy).DocumentNode;
            HtmlNode form = docNode.SelectSingleNode("//form[@id = 'login-form']");
            HtmlNode authInput = form.SelectSingleNode("//input[@name = 'authURL']");
            string authUrl = authInput != null ? authInput.GetAttributeValue("value", "") : "";
            string data = GetWebDataFromPost(url, string.Format(loginPostData, HttpUtility.UrlEncode(authUrl), HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password)), cc, proxy: proxy);
            if (!IsLoggedIn(data))
            {
                cc = null;
                Settings.DynamicCategoriesDiscovered = false;
                Settings.Categories.Clear();
                throw new OnlineVideosException("Email and password does not match, or error in login process. Please try again.");
            }
            if (profiles == null || profiles.Count == 0)
            {
                Regex rgx = new Regex(@"nf\.constants\.page\.contextData =(.*); }\(netflix\)\);");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    string jsonData = m.Groups[1].Value;
                    JObject json = (JObject)JsonConvert.DeserializeObject(jsonData);
                    profiles = json["profiles"]["data"]["allProfiles"].Value<JArray>();
                    if (string.IsNullOrEmpty(ProfileName) || !profiles.Any(p => p["profileName"].Value<string>() == ProfileName))
                    {
                        currentProfile = (JObject)profiles.FirstOrDefault(p => p["isAccountOwner"].Value<bool>());
                    }
                    else
                    {
                        currentProfile = (JObject)profiles.FirstOrDefault(p => p["profileName"].Value<string>() == ProfileName);
                    }
                    MyGetWebData(string.Format(switchProfileUrl, ApiRoot, ProfileToken));
                }
                else
                {
                    cc = null;
                    Settings.DynamicCategoriesDiscovered = false;
                    Settings.Categories.Clear();
                    throw new OnlineVideosException("Error loading profiles. Please try again");
                }
            }
        }

        #endregion

        #region GetWebData
        private string MyGetWebData(string url)
        {
            if (!HaveCredentials)
            {
                cc = null;
                throw new OnlineVideosException("Please enter your email and password");
            }
            if (cc == null)
            {
                Login();
            }
            string data = GetWebData(url, cc, proxy: proxy);
            if (!IsLoggedIn(data))
            {
                Login();
                data = GetWebData(url, cc, proxy: proxy);
            }
            return data;
        }
        #endregion

        #region IBrowserSiteUtil
        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get
            {
                return "OnlineVideos.Sites.BrowserUtilConnectors.NetflixConnector";
            }
        }

        string IBrowserSiteUtil.UserName
        {
            get { return username + "¥" + ProfileToken; }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            string data = MyGetWebData(homeUrl);
            if (!IsKidsProfile)
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);
                Category genres = new Category() { Name = "Browse", HasSubCategories = true, SubCategories = new List<Category>() };
                HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'netflix.com/WiGenre')]");
                if (genreNodes != null)
                {
                    foreach (HtmlNode a in genreNodes)
                    {
                        genres.SubCategories.Add(new RssLink()
                        {
                            Name = a.InnerText.Trim(),
                            Url = a.GetAttributeValue("href", ""),
                            HasSubCategories = true,
                            ParentCategory = genres
                        });
                    }
                }
                genres.SubCategoriesDiscovered = genres.SubCategories.Count > 0;
                Settings.Categories.Add(genres);
            }
            Settings.Categories.Add(new RssLink() { Name = "My List", HasSubCategories = true, Url = myListUrl });
            Settings.Categories.Add(new RssLink() { Name = "Kids", HasSubCategories = true, Url = kidsUrl });
            Settings.Categories.Add(new RssLink() { Name = ProfileCategoryName, HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 2 || IsKidsProfile;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string other = (string)parentCategory.Other;
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Name == ProfileCategoryName)
            {
                foreach (JToken profile in profiles)
                {
                    parentCategory.SubCategories.Add(new Category() { Name = profile["profileName"].Value<string>(), HasSubCategories = true, Thumb = profile["avatar"]["images"]["100"].Value<string>(), ParentCategory = parentCategory, Other = profile.ToString() });
                }
            }
            else if (parentCategory.ParentCategory != null && parentCategory.ParentCategory.Name == ProfileCategoryName)
            {
                Settings.Categories.Clear();
                Settings.DynamicCategoriesDiscovered = false;
                currentProfile = (JObject)JsonConvert.DeserializeObject((parentCategory.Other is string) ? parentCategory.Other as string : "");
                parentCategory.ParentCategory.Name = ProfileCategoryName;
                MyGetWebData(string.Format(switchProfileUrl,ApiRoot, ProfileToken));
            }
            else if (parentCategory.Name == "Kids")
            {
                string data = MyGetWebData((parentCategory as RssLink).Url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);
                RssLink characters = new RssLink() { Name = "Characters", HasSubCategories = true, SubCategories = new List<Category>(), ParentCategory = parentCategory };
                //Well use regex in this case...
                Regex rgx = new Regex(@"title=\\\""(?<name>[^\\]*)\\\"".href=\\\""(?<url>[^\\]*)(?:[^<]*)<img.src=\\""(?<thumb>[^\\]*)");
                foreach (Match m in rgx.Matches(data))
                {
                    characters.SubCategories.Add(new RssLink()
                    {
                        Name = m.Groups["name"].Value,
                        Url = m.Groups["url"].Value,
                        Thumb = m.Groups["thumb"].Value,
                        ParentCategory = characters,
                        HasSubCategories = true

                    });
                }
                characters.SubCategoriesDiscovered = characters.SubCategories.Count > 0;
                parentCategory.SubCategories.Add(characters);

                foreach (HtmlNode a in doc.DocumentNode.SelectNodes("//a[contains(@href, 'netflix.com/KidsAltGenre')]"))
                {
                    string url = a.GetAttributeValue("href", "");
                    if (!parentCategory.SubCategories.Any(c => (c as RssLink).Url == url))
                    {
                        parentCategory.SubCategories.Add(new RssLink()
                        {
                            Name = a.InnerText.Trim(),
                            Url = a.GetAttributeValue("href", ""),
                            HasSubCategories = true,
                            ParentCategory = parentCategory
                        });
                    }
                }

            }
            else if (parentCategory.Name == "My List" || (parentCategory.ParentCategory != null && (parentCategory.ParentCategory.Name == "Kids" || parentCategory.ParentCategory.Name == "Characters"))) //Kids Genres
            {
                string data = MyGetWebData((parentCategory as RssLink).Url);
                //Well use regex in this case...
                Regex rgx = new Regex(@"alt=\""(?<name>[^\""]*)\"".src=\""(?<thumb>[^\""]*)\""(?:[^\?]*)\?movieid=(?<id>\d*)");
                foreach (Match m in rgx.Matches(data))
                {
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = HttpUtility.HtmlDecode(m.Groups["name"].Value),
                        Url = m.Groups["id"].Value,
                        Thumb = m.Groups["thumb"].Value,
                        ParentCategory = parentCategory,
                        HasSubCategories = !(parentCategory.ParentCategory != null && parentCategory.ParentCategory.Name == "Characters")

                    });
                }
            }
            else if (parentCategory.ParentCategory != null && parentCategory.ParentCategory.Name == "Browse") //Genres
            {
                string url = (parentCategory as RssLink).Url;
                string data = MyGetWebData(url);
                string shaktiApi = "";
                Regex rgx = new Regex(@"\""SHAKTI_API_ROOT\"":""([^\""]*)");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    shaktiApi = m.Groups[1].Value;
                }
                string buildId = "";
                rgx = new Regex(@"\""BUILD_IDENTIFIER\"":""([^\""]*)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    buildId = m.Groups[1].Value;
                }
                Uri uri = new Uri(url);
                string agid = HttpUtility.ParseQueryString(uri.Query).Get("agid");
                if (!string.IsNullOrEmpty(shaktiApi) && !string.IsNullOrEmpty(buildId) && !string.IsNullOrEmpty(agid))
                {
                    data = MyGetWebData(string.Format(genreUrl, shaktiApi, buildId, agid, 0, 50, BrowseSort));
                    JObject json = (JObject)JsonConvert.DeserializeObject(data);
                    foreach (JObject item in json["catalogItems"])
                    {
                        parentCategory.SubCategories.Add(new RssLink()
                        {
                            Name = (string)item["title"],
                            Url = ((int)item["titleId"]).ToString(),
                            Thumb = (string)item["boxart"],
                            ParentCategory = parentCategory,
                            HasSubCategories = true
                        });
                    }
                    if (parentCategory.SubCategories.Count >= 50)
                        parentCategory.SubCategories.Add(new NextPageCategory() { Url = string.Format(genreUrl, shaktiApi, buildId, agid, "START_INDEX", "STOP_INDEX", BrowseSort), ParentCategory = parentCategory, Other = 51 });
                }
            }
            else if (other != "Season")
            {
                string data = MyGetWebData(movieUrl + (parentCategory as RssLink).Url);
                Regex rgx = new Regex(@"nf\.constants\.page\.contextData =(.*); }\(netflix\)\);");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    string jsonData = m.Groups[1].Value;
                    JObject json = (JObject)JsonConvert.DeserializeObject(jsonData);
                    if ((bool)json["displayPage"]["data"]["isShow"])
                    {
                        if (data.Contains("class=\"seasonItem"))
                        {
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(data);
                            foreach (HtmlNode li in doc.DocumentNode.SelectNodes("//li[starts-with(@class, 'seasonItem')]"))
                            {
                                HtmlNode a = li.SelectSingleNode("a");
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    Name = "Season " + a.InnerText,
                                    Url = string.Format(seasonDetailsUrl, (parentCategory as RssLink).Url, a.GetAttributeValue("data-vid", "")),
                                    HasSubCategories = true,
                                    ParentCategory = parentCategory,
                                    Thumb = parentCategory.Thumb,
                                    Other = "Season"
                                });
                            }
                        }
                        else if (data.Contains("data-episodeid=\""))
                        {
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(data);
                            HtmlNode docNode = doc.DocumentNode;
                            HtmlNode seasonNode = docNode.SelectSingleNode("//span[@class = 'selectorTxt']");
                            string season = seasonNode == null ? "1x" : (seasonNode.InnerText + "x");
                            HtmlNode episodes = docNode.SelectSingleNode("//ul[@class = 'episodeList']");
                            foreach (HtmlNode episode in episodes.SelectNodes("li"))
                            {
                                string seqNum = episode.SelectSingleNode("span[@class = 'seqNum']").InnerText;
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    Name = season + (seqNum.Count() < 2 ? ("0" + seqNum) : seqNum) + " " + episode.SelectSingleNode("span[@class = 'episodeTitle']").InnerText,
                                    Url = episode.GetAttributeValue("data-episodeid", ""),
                                    ParentCategory = parentCategory,
                                    Thumb = parentCategory.Thumb,
                                    HasSubCategories = false
                                });
                            }
                        }
                        else
                        {
                            foreach (JProperty season in json["odp"]["data"]["meta"]["episodes"])
                            {
                                JToken episodes = season.Value<JToken>();
                                foreach (JArray episodesArray in episodes)
                                {
                                    foreach (JToken episode in episodesArray)
                                    {
                                        int e = (int)episode["episodeSequenceNumber"];
                                        parentCategory.SubCategories.Add(new RssLink()
                                        {
                                            Name = (int)episode["seasonSequenceNumber"] + "x" + (e < 10 ? ("0" + e) : e.ToString()) + " " + (string)episode["title"],
                                            ParentCategory = parentCategory,
                                            Thumb = parentCategory.Thumb,
                                            HasSubCategories = false,
                                            Url = ((int)episode["silverlightId"]).ToString()
                                        });
                                        parentCategory.SubCategories.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
                                    }
                                }
                            }
                        }
                    }
                    else //Movie
                    {
                        parentCategory.SubCategories.Add(new RssLink() { Name = parentCategory.Name, Url = (parentCategory as RssLink).Url, Thumb = parentCategory.Thumb, ParentCategory = parentCategory, HasSubCategories = false });
                    }
                }
            }
            else
            {
                string jsonData = MyGetWebData((parentCategory as RssLink).Url);
                JObject json = (JObject)JsonConvert.DeserializeObject(jsonData);
                string data = (string)json["html"];
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);
                HtmlNode docNode = doc.DocumentNode;
                HtmlNode episodes = docNode.SelectSingleNode("//ul[@class = 'episodeList']");
                foreach (HtmlNode episode in episodes.SelectNodes("li"))
                {
                    string seqNum = episode.SelectSingleNode("span[@class = 'seqNum']").InnerText;
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = parentCategory.Name.Replace("Season", "") + "x" + (seqNum.Count() < 2 ? ("0" + seqNum) : seqNum) + " " + episode.SelectSingleNode("span[@class = 'episodeTitle']").InnerText,
                        Url = episode.GetAttributeValue("data-episodeid", ""),
                        ParentCategory = parentCategory,
                        Thumb = parentCategory.Thumb,
                        HasSubCategories = false
                    });
                }
            }

            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory nextPagecategory)
        {
            nextPagecategory.ParentCategory.SubCategories.Remove(nextPagecategory);
            int? index = nextPagecategory.Other as int?;
            index = index == null ? 0 : index;
            string url = nextPagecategory.Url;
            url = url.Replace("START_INDEX", index.ToString());
            url = url.Replace("STOP_INDEX", (index + 50 - 1).ToString());

            string data = MyGetWebData(url);
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            int i = 0;
            foreach (JObject item in json["catalogItems"])
            {
                i++;
                nextPagecategory.ParentCategory.SubCategories.Add(new RssLink()
                {
                    Name = (string)item["title"],
                    Url = ((int)item["titleId"]).ToString(),
                    Thumb = (string)item["boxart"],
                    ParentCategory = nextPagecategory.ParentCategory,
                    HasSubCategories = true
                });
            }
            if (i == 50)
                nextPagecategory.ParentCategory.SubCategories.Add(new NextPageCategory() { Url = nextPagecategory.Url, ParentCategory = nextPagecategory.ParentCategory, Other = index + 50 });
            return i;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> getVideoList(Category category)
        {
            return new List<VideoInfo>() { new VideoInfo() { VideoUrl = string.Format(playerUrl, (category as RssLink).Url), Title = category.Name, ImageUrl = category.Thumb } };
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

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            string data = MyGetWebData(string.Format(searchUrl, ApiRoot, HttpUtility.UrlEncode(query)));
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            JToken items = json["galleryVideos"]["items"];
            if (items != null)
            {
                foreach (JToken item in items)
                {
                    results.Add(new RssLink() { Name = (string)item["title"], Url = ((int)item["id"]).ToString(), Thumb = (string)item["boxart"], HasSubCategories = true, Other = "search" });
                }
            }
            return results;
        }
        #endregion
    }
}
