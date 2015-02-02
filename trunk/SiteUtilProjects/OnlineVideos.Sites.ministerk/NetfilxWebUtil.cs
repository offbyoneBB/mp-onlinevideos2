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
using OnlineVideos.Sites.Ministerk.Extensions;
using OnlineVideos.Sites.Utils;
using System.Collections.Specialized;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{

    public class NetfilxWebUtil : SiteUtilBase, IBrowserSiteUtil
    {
        #region Meta data
        protected class NetflixData
        {
            public string Title { get; set; }
            public string Id { get; set; }
            public string TrackId { get; set; }
            public string Cover { get; set; }
            public string Description { get; set; }
        }
        #endregion

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
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Netflix password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable descriptions"), Description("Enable descriptions for titles")]
        protected bool enableDesc = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Also enable descriptions in listings"), Description("Enable descriptions for titles in listings, slower browsing")]
        protected bool enableDescInListing = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable Add to and Remove from My List"), Description("Enable Add to and Remove from My List in titles listings")]
        protected bool enableAddRemoveMylist = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable Watch Now in My List"), Description("Enable Watch Now in MyList Category")]
        protected bool enableMylListPlayNow = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable Watch now in Netflix Home"), Description("Enable watch now in Netflix Home category")]
        protected bool enableHomePlayNow = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Sort titles in Browse by..."), Description("Sort titles in Browse by...")]
        protected BrowseSortOrders browseSort = BrowseSortOrders.SuggestionsForYou;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Max search results"), Description("Maximum number of titles in search result, default and max is 500")]
        protected uint maxSearchResultsx = 500;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Remember log-in in browser"), Description("Remember the log-in in the Browser Player")]
        protected bool rememberLogin = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show loading spinner"), Description("Show the loading spinner in the Browser Player")]
        protected bool showLoadingSpinner = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show help Category"), Description("Show link to forum or not, http://tinyurl.com/ov-netflix")]
        protected bool enableHelp = true;

        protected bool GetAuthUrl { get { return enableDesc || enableAddRemoveMylist; } }
        protected uint MaxSearchResults { get { return (maxSearchResultsx > 500 ? 500 : maxSearchResultsx); } }

        #endregion

        #region Lots of urls
        private string loginUrl = @"https://www2.netflix.com/Login";
        private string homeUrl = @"https://www2.netflix.com/WiHome";
        private string loginPostData = @"authURL={0}&email={1}&password={2}&RememberMe=on";
        private string myListUrl = @"http://www2.netflix.com/MyList";
        private string movieUrl = @"http://www.netflix.com/WiMovie/";
        private string genreUrl = @"{0}/{1}/wigenre?genreId={2}&full=false&from={3}&to={4}&secure=false&_retry=0&orderBy={5}";
        private string kidsUrl = @"http://www2.netflix.com/Kids";
        private string seasonDetailsUrl = @"http://www2.netflix.com/WiMovie/{0}?actionMethod=seasonDetails&seasonId={1}&seasonKind=ELECTRONIC";
        private string playerUrl = @"http://www2.netflix.com/WiPlayer?movieid={0}";/*&trkid={1}*/
        private string searchUrl = @"{0}/desktop/search/instantsearch?esn=www&term={1}&ngv={2}"; //500 results maximum
        private string switchProfileUrl = @"{0}/desktop/account/profiles/switch?switchProfileGuid={1}";
        private string bobUrl = @"{0}/{1}/bob?&titleid={2}&trackid={3}&authURL={4}";
        private string netflixOrgEpisodes = @"{0}/desktop/odp/episodes?video={1}&authURL={2}";
        private string apiRoot = @"https://www.netflix.com/api";

        private string addRemoveMyListUrl = @"{0}/{1}/playlistop";
        private string addRemoveMyListPostData = "{{\"operation\":\"{0}\",\"videoId\":{1},\"trackId\":{2},\"authURL\":\"{3}\"}}";
        private string addMyListOperation = "add";
        private string removeMyListOperation = "remove";
        #endregion

        #region Private parts
        protected CookieContainer cc = null;
        private string latestAuthUrl = "";
        private WebProxy proxy = null; //new WebProxy("127.0.0.1", 8888); //Debug proxy
        private JObject currentProfile = null;
        private JArray profiles = null;
        private string _shaktiApi = "";
        private string ShaktiApi
        {
            get
            {
                if (string.IsNullOrEmpty(_shaktiApi))
                {
                    SetShaktiApiAndBuildId();
                }
                return _shaktiApi;
            }
        }
        private string _buildId = "";

        private string BuildId
        {
            get
            {
                if (string.IsNullOrEmpty(_shaktiApi))
                {
                    SetShaktiApiAndBuildId();
                }
                return _buildId;
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

        private string CangedToProfile
        {
            get { return "Switched to profile " + ProfileName + "."; }
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

        private void LoadProfiles()
        {
            if (profiles == null || profiles.Count == 0)
            {
                string data = MyGetWebData(homeUrl, true);
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
                    MyGetWebData(string.Format(switchProfileUrl, apiRoot, ProfileToken), true);
                }
                else
                {
                    cc = null;
                    Settings.DynamicCategoriesDiscovered = false;
                    Settings.Categories.Clear();
                    profiles = null;
                    throw new OnlineVideosException("Error loading profiles. Please try again");
                }
            }
        }

        private void Login()
        {
            cc = new CookieContainer();
            string url = WebCache.Instance.GetRedirectedUrl(loginUrl).Replace("entrytrap", "Login");
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            headers.Add("Referer", url);
            // No caching in this case.
            var htmlDoc = GetWebData<HtmlAgilityPack.HtmlDocument>(url, null, cookies: cc, proxy: proxy, headers: headers, cache: false);
            HtmlNode form = htmlDoc.DocumentNode.SelectSingleNode("//form[@id = 'login-form']");
            HtmlNode authInput = form.SelectSingleNode("//input[@name = 'authURL']");
            string authUrl = authInput != null ? authInput.GetAttributeValue("value", "") : "";
            var data = GetWebData(url, string.Format(loginPostData, HttpUtility.UrlEncode(authUrl), HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password)), cc, proxy: proxy);
            if (!IsLoggedIn(data))
            {
                cc = null;
                Settings.DynamicCategoriesDiscovered = false;
                Settings.Categories.Clear();
                throw new OnlineVideosException("Email and password does not match, or error in login process. Please try again.");
            }
        }

        #endregion

        #region GetWebData

        private string MyGetWebData(string url, bool isLoadingProfile = false)
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
            if (!isLoadingProfile)
                LoadProfiles();
            string data = GetWebData(url, cookies: cc, proxy: proxy);
            if (GetAuthUrl)
            {
                Regex rgx = new Regex(@"\""authURL\"":""([^\""]*)");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    latestAuthUrl = m.Groups[1].Value;
                }
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
            get { return username + "¥" + ProfileToken + (showLoadingSpinner ? "SHOWLOADING" : "") + (rememberLogin ? "REMEMBERLOGIN" : ""); }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }

        #endregion

        #region Helpers

        private void SetShaktiApiAndBuildId(string data = "")
        {
            if (string.IsNullOrEmpty(data))
            {
                data = MyGetWebData(@"http://www.netflix.com/WiGenre?agid=");
            }
            Regex rgx = new Regex(@"\""SHAKTI_API_ROOT\"":""([^\""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                _shaktiApi = m.Groups[1].Value;
            }
            rgx = new Regex(@"\""BUILD_IDENTIFIER\"":""([^\""]*)");
            m = rgx.Match(data);
            if (m.Success)
            {
                _buildId = m.Groups[1].Value;
            }
        }

        private string GetTitleDescription(string movieId, string trkid)
        {
            string desc = "";
            if (enableDesc && !string.IsNullOrEmpty(movieId) && !string.IsNullOrEmpty(trkid))
            {
                try
                {
                    string data = MyGetWebData(string.Format(bobUrl, ShaktiApi, BuildId, movieId, trkid, latestAuthUrl));
                    JObject json = (JObject)JsonConvert.DeserializeObject(data);
                    JValue value = (JValue)json["synopsis"];
                    if (value != null && value.ToString() != "")
                    {
                        desc += value.Value<string>() + "\r\n";
                    }
                    value = (JValue)json["yourRating"];
                    if (value != null && value.ToString() != "")
                    {
                        desc += "Your Rating: " + value.Value<float>() + "\r\n";
                    }
                    else
                    {
                        value = (JValue)json["predictedRating"];
                        if (value != null && value.ToString() != "")
                        {
                            desc += "Predicted Rating: " + value.Value<float>() + "\r\n";
                        }
                    }

                    value = (JValue)json["year"];
                    if (value != null && value.ToString() != "")
                    {
                        desc += "Year: " + value.Value<int>() + "\r\n";
                    }

                    JToken token = json["actors"];
                    if (token != null && token.Count() > 0)
                    {
                        desc += "Actors: ";
                        foreach (JObject o in token)
                        {
                            desc += o["name"].Value<string>() + ", ";
                        }
                        desc += "\r\n";
                    }

                    token = json["directors"];
                    if (token != null && token.Count() > 0)
                    {
                        desc += "Directors: ";
                        foreach (JObject o in token)
                        {
                            desc += o["name"].Value<string>() + ", ";
                        }
                        desc += "\r\n";
                    }
                    token = json["creators"];
                    if (token != null && token.Count() > 0)
                    {
                        desc += "Creators: ";
                        foreach (JObject o in token)
                        {
                            desc += o["name"].Value<string>() + ", ";
                        }
                        desc += "\r\n";
                    }
                }
                catch { }
            }
            return desc;
        }

        private List<NetflixData> GetSinglePageNetflixData(Category parentCategory, bool forceGetTrkid = false )
        {
            string content = parentCategory.GetData();
            List<NetflixData> dicts = new List<NetflixData>();
            if (string.IsNullOrEmpty(content))
                content = MyGetWebData((parentCategory as RssLink).Url);
            Regex rgx;
            if (forceGetTrkid || enableDesc)
                rgx = new Regex(@"alt=""(?<name>[^""]*)"".h{0,1}src=""(?<thumb>[^""]*)""(?:[^\?]*)\?movieid=(?<id>\d*).*?trkid=(?<trkid>\d*)");
            else
                rgx = new Regex(@"alt=\""(?<name>[^\""]*)\"".h{0,1}src=\""(?<thumb>[^\""]*)\""(?:[^\?]*)\?movieid=(?<id>\d*)");
            foreach (Match m in rgx.Matches(content))
            {
                NetflixData datum = new NetflixData();
                datum.TrackId = enableDesc || forceGetTrkid ? m.Groups["trkid"].Value : "";
                datum.Title = HttpUtility.HtmlDecode(m.Groups["name"].Value);
                datum.Id = HttpUtility.HtmlDecode(m.Groups["id"].Value);
                datum.Cover = m.Groups["thumb"].Value;
                datum.Description = enableDesc && enableDescInListing ? GetTitleDescription(m.Groups["id"].Value, m.Groups["trkid"].Value) : "";
                dicts.Add(datum);
            }
            return dicts;
        }

        #endregion

        #region Categories


        private List<Category> GetHomeCategories(string data, Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            Regex rgx = new Regex(@"nf\.constants\.page\.contextData =(.*); }\(netflix\)\);");
            Match m = rgx.Match(data);
            JObject json = null;
            if (m.Success)
            {
                string jsonData = m.Groups[1].Value;
                json = (JObject)JsonConvert.DeserializeObject(jsonData);
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(data);
            try
            {
                HtmlNodeCollection mrows = doc.DocumentNode.SelectSingleNode("//div[@class = 'mrows']").SelectNodes("div[contains(@class, 'mrow')]");
                foreach (HtmlNode mrow in mrows.Where(mr => mr.GetAttributeValue("class", "") == "mrow" || mr.GetAttributeValue("class", "").Contains("evidence")))
                {
                    HtmlNode name = mrow.SelectSingleNode(".//h3");
                    HtmlNode imageNode = mrow.SelectSingleNode(".//img");
                    string img = "";
                    if (imageNode != null)
                    {
                        img = imageNode.GetAttributeValue("src", "");
                        if (string.IsNullOrEmpty(img))
                            img = imageNode.GetAttributeValue("hsrc", "");
                    }

                    RssLink cat = new RssLink()
                    {
                        Name = name.InnerText.Trim(),
                        HasSubCategories = !enableHomePlayNow,
                        Thumb = img,
                        ParentCategory = parentCategory,
                        
                    };
                    string html = mrow.OuterHtml;
                    if (json != null)
                    {
                        HtmlNode slider = mrow.SelectSingleNode(".//div[starts-with(@id, 'slider_')]");
                        if (slider != null)
                        {
                            string sliderId = slider.GetAttributeValue("id", "");
                            JToken initData = json["sliders"]["data"]["initData"].FirstOrDefault(n => n["domId"].Value<string>() == sliderId);
                            if (initData != null)
                            {
                                string remainderHTML = initData["remainderHTML"].Value<string>();
                                if (!string.IsNullOrEmpty(remainderHTML))
                                    html += remainderHTML;
                            }
                        }
                    }
                    cat.SetData(html);
                    cat.SetState(NetflixUtils.SinglePageCategoriesState);
                    cat.SetPlayNow(enableHomePlayNow);
                    cats.Add(cat);
                }
            }
            catch { }

            return cats;
        }
         

        public override int DiscoverDynamicCategories()
        {
            string data = MyGetWebData(homeUrl);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(data);
            
            RssLink home = new RssLink() { Name = "Netflix Home", HasSubCategories = true, Url = homeUrl};
            home.SetState(NetflixUtils.HomeCategoriesState);
            Settings.Categories.Add(home);
            
            RssLink myList = new RssLink() { Name = "My List", HasSubCategories = !enableMylListPlayNow, Url = myListUrl };
            myList.SetState(NetflixUtils.SinglePageCategoriesState);
            myList.SetPlayNow(enableMylListPlayNow);
            myList.SetRememberDiscoveredItems(false);
            Settings.Categories.Add(myList);
            if (!IsKidsProfile)
            {
                try
                {
                    Category genres = new Category() { Name = "Browse", HasSubCategories = true, SubCategories = new List<Category>() };
                    HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'netflix.com/WiGenre')]");
                    if (genreNodes != null)
                    {
                        foreach (HtmlNode a in genreNodes)
                        {
                            RssLink rl = new RssLink()
                            {
                                Name = a.InnerText.Trim(),
                                Url = a.GetAttributeValue("href", ""),
                                HasSubCategories = true,
                                ParentCategory = genres
                            };
                            rl.SetState(NetflixUtils.MultiplePageCategoriesState);
                            genres.SubCategories.Add(rl);
                        }
                    }
                    genres.SubCategoriesDiscovered = genres.SubCategories.Count > 0;
                    Settings.Categories.Add(genres);
                }
                catch { }
            }

            RssLink kids = new RssLink() { Name = "Kids", HasSubCategories = true, Url = kidsUrl };
            kids.SetState(NetflixUtils.KidsState);
            Settings.Categories.Add(kids);
            RssLink profiles = new RssLink() { Name = ProfileCategoryName, HasSubCategories = true };
            profiles.SetState(NetflixUtils.ProfilesState);
            Settings.Categories.Add(profiles);
            if (enableHelp)
            {
                RssLink help = new RssLink() { Name = "Help", HasSubCategories = true };
                help.SetState(NetflixUtils.HelpState);
                Settings.Categories.Add(help);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > (enableHelp ? 4 : 3) || IsKidsProfile;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            #region Help

            if (parentCategory.IsHelpState())
                throw new OnlineVideosException("Forum: http://tinyurl.com/ov-netflix");

            #endregion

            #region Profiles
            if (parentCategory.IsProfilesState())
            {
                foreach (JToken profile in profiles)
                {
                    Category profileCat = new Category() { Name = profile["profileName"].Value<string>(), HasSubCategories = true, Thumb = profile["avatar"]["images"]["100"].Value<string>(), ParentCategory = parentCategory };
                    profileCat.SetState(NetflixUtils.ProfileState);
                    profileCat.SetProfile(profile);
                    parentCategory.SubCategories.Add(profileCat);
                }
            }

            else if (parentCategory.IsProfileState())
            {
                Settings.Categories.Clear();
                Settings.DynamicCategoriesDiscovered = false;
                currentProfile = parentCategory.GetProfile();
                parentCategory.ParentCategory.Name = ProfileCategoryName;
                MyGetWebData(string.Format(switchProfileUrl, apiRoot, ProfileToken));
                throw new OnlineVideosException(CangedToProfile);

            }
            #endregion

            #region Home Categories
            

            else if (parentCategory.IsHomeCategoriesState())
            {
                string data = MyGetWebData((parentCategory as RssLink).Url);
                List<Category> cats = GetHomeCategories(data, parentCategory);
                parentCategory.SubCategories = cats;
            }

            #endregion

            #region Kids
            else if (parentCategory.IsKidsState())
            {
                string data = MyGetWebData((parentCategory as RssLink).Url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);
                RssLink characters = new RssLink() { Name = "Characters", HasSubCategories = true, SubCategories = new List<Category>(), ParentCategory = parentCategory };
                //Well use regex in this case...
                Regex rgx = new Regex(@"title=\\\""(?<name>[^\\]*)\\\"".href=\\\""(?<url>[^\\]*)(?:[^<]*)<img.src=\\""(?<thumb>[^\\]*)");
                foreach (Match m in rgx.Matches(data))
                {
                    RssLink character = new RssLink()
                    {
                        Name = m.Groups["name"].Value,
                        Url = m.Groups["url"].Value,
                        Thumb = m.Groups["thumb"].Value,
                        ParentCategory = characters,
                        HasSubCategories = true
                    };
                    character.SetState(NetflixUtils.SinglePageCategoriesState);
                    characters.SubCategories.Add(character);
                }
                characters.SubCategoriesDiscovered = characters.SubCategories.Count > 0;
                parentCategory.SubCategories.Add(characters);

                foreach (HtmlNode a in doc.DocumentNode.SelectNodes("//a[contains(@href, 'netflix.com/KidsAltGenre')]"))
                {
                    string url = a.GetAttributeValue("href", "");
                    if (!parentCategory.SubCategories.Any(c => (c as RssLink).Url == url))
                    {
                        RssLink category = new RssLink()
                        {
                            Name = a.InnerText.Trim(),
                            Url = a.GetAttributeValue("href", ""),
                            HasSubCategories = true,
                            ParentCategory = parentCategory
                        };
                        category.SetState(NetflixUtils.SinglePageCategoriesState);
                        parentCategory.SubCategories.Add(category);
                    }
                }

            }
            #endregion

            #region SinglePageCategories
            else if (parentCategory.IsSinglePageCategoriesState())
            {
                foreach (NetflixData d in GetSinglePageNetflixData(parentCategory, enableAddRemoveMylist))
                {
                    RssLink cat = new RssLink()
                    {
                        Name = d.Title,
                        Url = d.Id,
                        Thumb = d.Cover,
                        ParentCategory = parentCategory,
                        HasSubCategories = !(parentCategory.ParentCategory != null && parentCategory.ParentCategory.Name == "Characters"),
                        Description = d.Description
                    };
                    cat.SetState(NetflixUtils.TitleState);
                    if (!string.IsNullOrEmpty(d.TrackId))
                        cat.SetTrackId(d.TrackId);
                    parentCategory.SubCategories.Add(cat);
                }
            }
            #endregion

            #region MultiplePageCategories
            else if (parentCategory.IsMultiplePageCategoriesState()) //This is browse categories
            {
                string url = (parentCategory as RssLink).Url;
                string data = MyGetWebData(url);
                SetShaktiApiAndBuildId(data);
                Uri uri = new Uri(url);
                string agid = HttpUtility.ParseQueryString(uri.Query).Get("agid");
                if (!string.IsNullOrEmpty(ShaktiApi) && !string.IsNullOrEmpty(BuildId) && !string.IsNullOrEmpty(agid))
                {
                    data = MyGetWebData(string.Format(genreUrl, ShaktiApi, BuildId, agid, 0, 50, BrowseSort));
                    JObject json = (JObject)JsonConvert.DeserializeObject(data);
                    foreach (JObject item in json["catalogItems"])
                    {
                        RssLink category = new RssLink()
                        {
                            Name = (string)item["title"],
                            Url = ((int)item["titleId"]).ToString(),
                            Thumb = (string)item["boxart"],
                            ParentCategory = parentCategory,
                            HasSubCategories = true,
                            Description = enableDesc && enableDescInListing ? GetTitleDescription(((int)item["titleId"]).ToString(), ((int)item["trackId"]).ToString()) : ""
                        };
                        category.SetState(NetflixUtils.TitleState);
                        if (enableDesc || enableAddRemoveMylist)
                            category.SetTrackId(((int)item["trackId"]).ToString());
                        parentCategory.SubCategories.Add(category);
                    }
                    if (parentCategory.SubCategories.Count >= 50)
                    {
                        NextPageCategory next = new NextPageCategory() { Url = string.Format(genreUrl, ShaktiApi, BuildId, agid, "START_INDEX", "STOP_INDEX", BrowseSort), ParentCategory = parentCategory };
                        next.SetState(NetflixUtils.TitleState);
                        next.SetStartIndex("51");
                        parentCategory.SubCategories.Add(next);
                    }
                }
            }
            #endregion

            #region Title
            else if (parentCategory.IsTitleState())
            {
                string data = MyGetWebData(movieUrl + (parentCategory as RssLink).Url);
                Regex rgx = new Regex(@"nf\.constants\.page\.contextData =(.*); }\(netflix\)\);");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    string jsonData = m.Groups[1].Value;
                    JObject json = (JObject)JsonConvert.DeserializeObject(jsonData);

                    #region Series
                    if ((bool)json["displayPage"]["data"]["isShow"])
                    {

                        #region Multiple seasons
                        if (data.Contains("class=\"seasonItem"))
                        {
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(data);
                            foreach (HtmlNode li in doc.DocumentNode.SelectNodes("//li[starts-with(@class, 'seasonItem')]"))
                            {
                                HtmlNode a = li.SelectSingleNode("a");
                                RssLink season = new RssLink()
                                {
                                    Name = "Season " + a.InnerText,
                                    Url = string.Format(seasonDetailsUrl, (parentCategory as RssLink).Url, a.GetAttributeValue("data-vid", "")),
                                    HasSubCategories = true,
                                    ParentCategory = parentCategory,
                                    Thumb = parentCategory.Thumb
                                };
                                season.SetState(NetflixUtils.EpisodesState);
                                parentCategory.SubCategories.Add(season);
                            }
                        }
                        #endregion

                        #region Single season
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
                                HtmlNode pDesc = enableDesc ? episode.SelectSingleNode(".//p[@class = 'synopsis']") : null;
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    Name = season + (seqNum.Count() < 2 ? ("0" + seqNum) : seqNum) + " " + episode.SelectSingleNode("span[@class = 'episodeTitle']").InnerText,
                                    Url = episode.GetAttributeValue("data-episodeid", ""),
                                    ParentCategory = parentCategory,
                                    Thumb = parentCategory.Thumb,
                                    Description = pDesc != null ? pDesc.InnerText : "",
                                    HasSubCategories = false
                                });
                            }
                        }
                        #endregion

                        #region Netflix title
                        else
                        {
                            JArray episodesExtras = new JArray();
                            if (enableDesc)
                            {
                                string extraData = MyGetWebData(string.Format(netflixOrgEpisodes, apiRoot, (parentCategory as RssLink).Url, latestAuthUrl));
                                JObject extraInfo = (JObject)JsonConvert.DeserializeObject(extraData);
                                foreach (JArray seasonArray in extraInfo["episodes"])
                                {
                                    foreach (JToken et in seasonArray)
                                    {
                                        episodesExtras.Add(et);
                                    }
                                }

                            }
                            foreach (JProperty season in json["odp"]["data"]["meta"]["episodes"])
                            {
                                JToken episodes = season.Value<JToken>();
                                foreach (JArray episodesArray in episodes)
                                {
                                    foreach (JToken episode in episodesArray)
                                    {
                                        int e = (int)episode["episodeSequenceNumber"];
                                        int s = (int)episode["seasonSequenceNumber"];
                                        RssLink cat = new RssLink()
                                        {
                                            Name = s + "x" + (e < 10 ? ("0" + e) : e.ToString()) + " " + (string)episode["title"],
                                            ParentCategory = parentCategory,
                                            Thumb = parentCategory.Thumb,
                                            HasSubCategories = false,
                                            Url = ((int)episode["silverlightId"]).ToString()
                                        };
                                        if (enableDesc)
                                        {
                                            JToken episodeExtra = episodesExtras.FirstOrDefault(t => (int)t["season"] == s && (int)t["episode"] == e);
                                            if (episodeExtra != null)
                                            {
                                                cat.Description = episodeExtra["synopsis"].Value<string>();
                                                JToken stills = episodeExtra["stills"];
                                                if (stills != null && stills.Count() > 0)
                                                {
                                                    cat.Thumb = stills.Last()["url"].Value<string>();
                                                }
                                            }
                                        }
                                        parentCategory.SubCategories.Add(cat);
                                    }
                                }
                            }
                            parentCategory.SubCategories.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
                        }
                        #endregion
                    }
                    #endregion

                    #region Movies
                    else
                    {
                        parentCategory.SubCategories.Add(new RssLink() { Description = GetTitleDescription((parentCategory as RssLink).Url, parentCategory.GetTrackId()), Name = parentCategory.Name, Url = (parentCategory as RssLink).Url, Thumb = parentCategory.Thumb, ParentCategory = parentCategory, HasSubCategories = false });
                    }
                    #endregion
                }
            }


            #endregion

            #region Episodes
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
                    HtmlNode pDesc = enableDesc ? episode.SelectSingleNode(".//p[@class = 'synopsis']") : null;
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = parentCategory.Name.Replace("Season", "") + "x" + (seqNum.Count() < 2 ? ("0" + seqNum) : seqNum) + " " + episode.SelectSingleNode("span[@class = 'episodeTitle']").InnerText,
                        Url = episode.GetAttributeValue("data-episodeid", ""),
                        ParentCategory = parentCategory,
                        Thumb = parentCategory.Thumb,
                        Description = pDesc != null ? pDesc.InnerText : "",
                        HasSubCategories = false
                    });
                }
            }
            #endregion

            parentCategory.SubCategoriesDiscovered = parentCategory.RememberDiscoveredItems() && parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory nextPagecategory)
        {
            nextPagecategory.ParentCategory.SubCategories.Remove(nextPagecategory);

            string index = nextPagecategory.GetStartIndex();
            int iIndex = 0;
            int.TryParse(index, out iIndex);
            string url = nextPagecategory.Url;
            url = url.Replace("START_INDEX", index);
            url = url.Replace("STOP_INDEX", (iIndex + 50 - 1).ToString());

            string data = MyGetWebData(url);
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            int i = 0;
            foreach (JObject item in json["catalogItems"])
            {
                RssLink cat = new RssLink()
                {
                    Name = (string)item["title"],
                    Url = ((int)item["titleId"]).ToString(),
                    Thumb = (string)item["boxart"],
                    ParentCategory = nextPagecategory.ParentCategory,
                    HasSubCategories = true,
                    Description = enableDesc && enableDescInListing ? GetTitleDescription(((int)item["titleId"]).ToString(), ((int)item["trackId"]).ToString()) : ""
                };
                cat.SetState(NetflixUtils.TitleState);
                if (enableDesc || enableAddRemoveMylist)
                    cat.SetTrackId(((int)item["trackId"]).ToString());
                i++;
                nextPagecategory.ParentCategory.SubCategories.Add(cat);
            }
            if (i == 50)
            {
                NextPageCategory next = new NextPageCategory() { Url = nextPagecategory.Url, ParentCategory = nextPagecategory.ParentCategory };
                next.SetState(NetflixUtils.TitleState);
                next.SetStartIndex((iIndex + 50).ToString());
                nextPagecategory.ParentCategory.SubCategories.Add(next);
            }
            return i;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.IsPlayNow())
            {
                List<VideoInfo> videos = new List<VideoInfo>();
                if (category.IsSinglePageCategoriesState())
                {
                    foreach (NetflixData d in GetSinglePageNetflixData(category, true))
                    {
                        videos.Add(new VideoInfo()
                        {
                            Title = d.Title,
                            VideoUrl = string.Format(playerUrl, d.Id) + "&trkid=" + d.TrackId,
                            ImageUrl = d.Cover,
                            Description = d.Description,
                            Other = new SerializableDictionary<string, string>() { { "TrackId", d.TrackId }, { "VideoId", d.Id } }
                        });
                    }
                }
                return videos;
            }
            return new List<VideoInfo>() { new VideoInfo() { Description = category.Description, VideoUrl = string.Format(playerUrl, (category as RssLink).Url), Title = category.Name, ImageUrl = category.Thumb } };
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

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            string data = MyGetWebData(string.Format(searchUrl, apiRoot, HttpUtility.UrlEncode(query), MaxSearchResults));
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            JToken galery = json["galleryVideos"];
            if (galery != null)
            {
                JToken items = galery["items"];
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        RssLink cat = new RssLink() { Name = (string)item["title"], Url = ((int)item["id"]).ToString(), Thumb = (string)item["boxart"], HasSubCategories = true };
                        cat.SetState(NetflixUtils.TitleState);
                        if (enableDesc || enableAddRemoveMylist)
                            cat.SetTrackId(((int)item["trackId"]).ToString());
                        results.Add(cat);

                    }
                }
            }
            return results;
        }

        #endregion

        #region Context menu

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> result = new List<ContextMenuEntry>();
            if (enableAddRemoveMylist && selectedItem == null && selectedCategory.IsTitleState())
            {
                try
                {
                    if (!string.IsNullOrEmpty(selectedCategory.GetTrackId()))
                    {
                        string data = MyGetWebData(string.Format(bobUrl, ShaktiApi, BuildId, (selectedCategory as RssLink).Url, selectedCategory.GetTrackId(), latestAuthUrl));
                        JObject json = (JObject)JsonConvert.DeserializeObject(data);
                        if (json["isMovie"].Value<bool>() || json["isShow"].Value<bool>())
                        {
                            bool inPlayList = json["inPlayList"].Value<bool>();
                            ContextMenuEntry entry = new ContextMenuEntry() { DisplayText = (inPlayList ? "Remove from " : "Add to ") + "My List" };
                            result.Add(entry);
                        }
                    }
                }
                catch { }
            }
            if (enableAddRemoveMylist && selectedItem != null && selectedItem.Other is SerializableDictionary<string,string>)
            {
                try
                {
                    SerializableDictionary<string, string> other = selectedItem.Other as SerializableDictionary<string, string>;
                    if (other.ContainsKey("TrackId") && other.ContainsKey("VideoId"))
                    {
                        string data = MyGetWebData(string.Format(bobUrl, ShaktiApi, BuildId, other["VideoId"], other["TrackId"], latestAuthUrl));
                        JObject json = (JObject)JsonConvert.DeserializeObject(data);
                        if (json["isMovie"].Value<bool>() || json["isShow"].Value<bool>())
                        {
                            bool inPlayList = json["inPlayList"].Value<bool>();
                            ContextMenuEntry entry = new ContextMenuEntry() { DisplayText = (inPlayList ? "Remove from " : "Add to ") + "My List" };
                            result.Add(entry);
                        }
                    }
                }
                catch { }
            }
            return result;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (choice.DisplayText == "Add to My List")
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                //private string addRemoveMyListUrl = @"{0}/{1}/playlistop";
                //private string addRemoveMyListPostData = @"{""operation"":""{0}"",""videoId"":{1},""trackId"":{2},""authURL"":""{3}""}";

                string videoId;
                string trackId;
                string title;
                if (selectedItem == null)
                {
                    videoId = (selectedCategory as RssLink).Url;
                    trackId = selectedCategory.GetTrackId();
                    title = selectedCategory.Name;
                }
                else
                {
                    SerializableDictionary<string, string> other = selectedItem.Other as SerializableDictionary<string, string>;
                    videoId = other["VideoId"];
                    trackId = other["TrackId"];
                    title = selectedItem.Title;
                }
                GetWebData(string.Format(addRemoveMyListUrl, ShaktiApi, BuildId),
                    string.Format(addRemoveMyListPostData, addMyListOperation, videoId, trackId, latestAuthUrl),
                    cc);
                result.RefreshCurrentItems = true;
                result.ExecutionResultMessage = title + " added to My List";
                return result;
            }
            if (choice.DisplayText == "Remove from My List")
            {
                string videoId;
                string trackId;
                string title;
                if (selectedItem == null)
                {
                    videoId = (selectedCategory as RssLink).Url;
                    trackId = selectedCategory.GetTrackId();
                    title = selectedCategory.Name;
                }
                else
                {
                    SerializableDictionary<string, string> other = selectedItem.Other as SerializableDictionary<string, string>;
                    videoId = other["VideoId"];
                    trackId = other["TrackId"];
                    title = selectedItem.Title;
                }
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                GetWebData(string.Format(addRemoveMyListUrl, ShaktiApi, BuildId),
                    string.Format(addRemoveMyListPostData, removeMyListOperation, videoId, trackId, latestAuthUrl),
                    cc);
                result.RefreshCurrentItems = true;
                result.ExecutionResultMessage = title + " removed from My List";
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion

    }
}
