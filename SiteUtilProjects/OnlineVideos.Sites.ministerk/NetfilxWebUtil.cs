﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Web;
using System.Text.RegularExpressions;
using OnlineVideos.Sites.Utils;
using System.Collections.Specialized;
using OnlineVideos.Helpers;
using OnlineVideos._3rdParty.Newtonsoft.Json;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{

    public class NetfilxWebUtil : SiteUtilBase, IBrowserSiteUtil
    {

        #region Helper classes

        private class NetflixCategory : RssLink
        {
            internal bool InQueue { get; set; }
            internal int Runtime { get; set; }
            internal bool IsShow { get; set; }
        }

        #endregion

        #region Settings

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Netflix email")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Netflix password"), PasswordPropertyText(true)]
        protected string password = null;
        //[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Remember log-in in browser"), Description("Remember the log-in in the Browser Player")]
        //No need to configure this at the moment...
        protected bool rememberLogin = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show loading spinner"), Description("Show the loading spinner in the Browser Player")]
        protected bool showLoadingSpinner = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable Netflix Info/Stat OSD"), Description("Enable info and statistics OSD. Toggle OSD with 0 when video is playing. Do not enable this if you need to enter 0 in parental control pin")]
        protected bool enableNetflixOsd = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Number of Home categories"), Description("Change only if necessary. Number of categories in home. Default value 38 => results in 38+1-2=37 categories")]
        protected int noOfCatsInHome = 38;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Number of categories per page in other listings"), Description("Change only if necessary. Number of items in listings. Default 100")]
        protected uint noOfItems = 100;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable verbose logging"), Description("DEBUG only! Enable only if you have problems. Very verbose logging, generates a lot of data in log.")]
        protected bool enableVerboseLog = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Disable browser logging"), Description("Change only if necessary/nothing else helps. If browser player fails. Change back if it does not help!")]
        protected bool disableLogging = false;
        //

        protected Dictionary<string, string> i18n = null;

        #endregion

        #region Urls

        private string loginUrl = @"https://www.netflix.com/Login";
        private string homeUrl = @"https://www.netflix.com/";
        private string kidsUrl = @"https://www.netflix.com/kid";
        private string playerUrl = @"http://www.netflix.com/watch/{0}";
        private string loginPostData = @"authURL={0}&email={1}&password={2}&RememberMe=on";
        private string switchProfileUrl = @"{0}/{1}/profiles/switch?switchProfileGuid={2}";

        #endregion

        #region GetWebData
        private string MyGetWebData(string url, string postData = null, string referer = null, string contentType = null, bool forceUTF8 = true)
        {
            //Never cache, problems with profiles sometimes
            string data = HboNordic.HboWebCache.Instance.GetWebData(url, postData: postData, cookies: Cookies, referer: referer, contentType: contentType, cache: false, forceUTF8: forceUTF8);
            if (enableVerboseLog) Log.Debug(data);
            //Side effects
            //AuthUrl
            Regex rgx = new Regex(@"""authURL"":""(?<authURL>[^""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                latestAuthUrl = m.Groups["authURL"].Value;
                if (enableVerboseLog) Log.Debug("NETFLIX: new authURL");
            }
            if (i18n == null)
            {
                rgx = new Regex(@"""header.browse"":""(?<val>[^""]*)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    i18n = new Dictionary<string, string>();
                    i18n.Add("Browse", m.Groups["val"].Value.Trim());

                    rgx = new Regex(@"""navitem.mylist"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("My List", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("My List", "My List");
                    
                    rgx = new Regex(@"""navitem.subnav.home"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Home", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Home", "Home");

                    rgx = new Regex(@"""billboard.actions.continueWatching"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Continue Watching", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Continue Watching", "Continue Watching");

                    rgx = new Regex(@"""subgenres"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Subgenres", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Subgenres", "Subgenres");


                    rgx = new Regex(@"""tab.show.details"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Details", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Details", "Details");

                    rgx = new Regex(@"""details.creator"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Creator", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Creator", "Creator");

                    rgx = new Regex(@"""details.director"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Director", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Director", "Director");

                    rgx = new Regex(@"""details.cast"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Cast", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Cast", "Cast");

                    rgx = new Regex(@"""details.genres"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Genres", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Genres", "Genres");

                    rgx = new Regex(@"""details.this.show.is"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("This show is", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("This show is", "This show is");

                    rgx = new Regex(@"""details.this.movie.is"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("This movie is", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("This movie is", "This movie is");

                    rgx = new Regex(@"""tab.more.like.this"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("More like this", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("More like this", "More like this");

                    rgx = new Regex(@"""billboard.actions.watchNow"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Watch Now", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Watch Now", "Watch Now");

                    rgx = new Regex(@"""billboard.actions.play"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Play", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Play", "Play");

                    rgx = new Regex(@"""my.list.add"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("My List Add", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("My List Add", "My List Add");

                    rgx = new Regex(@"""my.list.remove"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("My List Remove", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("My List Remove", "My List Remove");

                    rgx = new Regex(@"""rating.rated.label"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("User rating", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("User rating", "User rating");

                    rgx = new Regex(@"""rating.avg.label"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Avg. rating", m.Groups["val"].Value.Trim());
                    else
                        i18n.Add("Avg. rating", "Avg. rating");

                    rgx = new Regex(@"""rating.predicted.label"":""(?<val>[^""]*)");
                    m = rgx.Match(data);
                    if (m.Success)
                        i18n.Add("Predicted rating for {0}", m.Groups["val"].Value.Trim().Replace("{profileName}","{0}"));
                    else
                        i18n.Add("Predicted rating for {0}", "Predicted rating for {0}");

                    //

                }
            }
            return data;
        }
        #endregion

        private string Translate(string key)
        {
            return (i18n != null && i18n.ContainsKey(key)) ? i18n[key] : key;
        }

        #region profiles
        private JToken currentProfile = null;
        private List<JToken> profiles = null;
        private string ProfileName
        {
            get
            {
                return currentProfile == null ? "" : (HttpUtility.HtmlDecode(currentProfile["summary"]["profileName"].Value<string>()) + (IsKidsProfile ? " (Kids)" : string.Empty));
            }
        }

        private string ProfileToken
        {
            get
            {
                return currentProfile == null ? "" : currentProfile["summary"]["guid"].Value<string>();
            }
        }

        private bool IsKidsProfile
        {
            get
            {
                return currentProfile != null && currentProfile["summary"]["isKids"].Value<bool>();
            }
        }

        private string ProfileIcon
        {
            get
            {
                if (currentProfile == null)
                    return string.Empty;
                string icon = currentProfile["summary"]["avatarName"].Value<string>().Replace("icon", string.Empty);
                if (icon.Count() == 1)
                {
                    icon = "00" + icon;
                }
                else if (icon.Count() == 2)
                {
                    icon = "0" + icon;
                }
                return string.Format(@"http://cdn-0.nflximg.com/ffe/profiles/avatars_v2/320x320/PICON_{0}.png", icon);
            }
        }

        private void LoadProfiles()
        {
            if (profiles == null || profiles.Count == 0)
            {
                string data = MyGetWebData(homeUrl);
                Regex rgx = new Regex(@"netflix\.falkorCache = (.*)?;</script><script>window\.netflix");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    string jsonData = m.Groups[1].Value;
                    JObject json = (JObject)JsonConvert.DeserializeObject(jsonData);
                    profiles = new List<JToken>();
                    foreach (JToken profile in json["profiles"])
                    {
                        JToken val = profile.FirstOrDefault<JToken>();

                        if (val != null && val.HasValues && val.Children<JToken>().Count() > 0)
                        {
                            profiles.Add(val.Value<JToken>());
                        }
                    }
                    profiles.Reverse();
                }
                else
                {
                    _cc = null;
                    Settings.DynamicCategoriesDiscovered = false;
                    Settings.Categories.Clear();
                    profiles = null;
                    throw new OnlineVideosException("Error loading profiles. Please try again");
                }
            }
        }
        #endregion

        #region Shakti Api

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

        private void SetShaktiApiAndBuildId(string data = "")
        {
            if (string.IsNullOrEmpty(data))
            {
                data = MyGetWebData(homeUrl);
            }
            Regex rgx = new Regex(@"\""SHAKTI_API_ROOT\"":""([^\""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                _shaktiApi = m.Groups[1].Value.Replace("http:", "https:");
            }
            rgx = new Regex(@"\""BUILD_IDENTIFIER\"":""([^\""]*)");
            m = rgx.Match(data);
            if (m.Success)
            {
                _buildId = m.Groups[1].Value;
            }
        }

        #endregion

        #region Cookies, credentials and auth
 
        private string latestAuthUrl = "";

        private bool HaveCredentials
        {
            get { return !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(password); }
        }

        private CookieContainer _cc = null;
        private CookieContainer Cookies
        {
            get
            {
                if (!HaveCredentials)
                {
                    _cc = null;
                    throw new OnlineVideosException("Please enter your email and password");
                }
                if (_cc == null)
                {
                    _cc = new CookieContainer();
                    string url = WebCache.Instance.GetRedirectedUrl(loginUrl).Replace("entrytrap", "Login");
                    // No caching in this case.
                    var htmlDoc = GetWebData<HtmlDocument>(url, cookies: _cc, cache: false);
                    HtmlNode form = htmlDoc.DocumentNode.SelectSingleNode("//form[@id = 'login-form']");
                    HtmlNode authInput = form.SelectSingleNode("//input[@name = 'authURL']");
                    string authUrl = authInput != null ? authInput.GetAttributeValue("value", "") : "";
                    var data = GetWebData<HtmlDocument>(url, string.Format(loginPostData, HttpUtility.UrlEncode(authUrl), HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password)), _cc, cache: false);
                    if (!(data.DocumentNode.SelectSingleNode("//form[@id = 'login-form']") == null))
                    {
                        _cc = null;
                        Settings.DynamicCategoriesDiscovered = false;
                        Settings.Categories.Clear();
                        throw new OnlineVideosException("Email and password does not match, or error in login process. Please try again.");
                    }

                }
                return _cc;
            }
        }

        #endregion

        #region SiteUtilBase

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            LoadProfiles();
            foreach (JToken p in profiles)
            {
                currentProfile = p;
                RssLink profile = new RssLink()
                {
                    HasSubCategories = true,
                    Name = ProfileName,
                    Thumb = ProfileIcon
                };
                currentProfile = null;
                profile.Other = (Func<List<Category>>)(() => GetProfileSubCategories(profile, p));
                Settings.Categories.Add(profile);
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

        private List<Category> GetProfileSubCategories(Category parentCategory, JToken profile)
        {
            currentProfile = profile;
            MyGetWebData(string.Format(switchProfileUrl, ShaktiApi, BuildId, ProfileToken), referer: homeUrl);
            List<Category> cats = new List<Category>();
            if (!IsKidsProfile)
            {
                RssLink home = new RssLink() { Name = Translate("Home"), HasSubCategories = true, ParentCategory = parentCategory };
                home.Other = (Func<List<Category>>)(() => GetHomeCategories(home));
                cats.Add(home);
            }
            else
            {
                RssLink kids = new RssLink() { Name = Translate("Home") + " (Kids)", HasSubCategories = true, ParentCategory = parentCategory };
                kids.Other = (Func<List<Category>>)(() => GetKidsHomeCategories(kids));
                cats.Add(kids);
            }

            //My List
            RssLink myList = new RssLink() { Name = Translate("My List"), HasSubCategories = true, ParentCategory = parentCategory };
            myList.Other = (Func<List<Category>>)(() => GetListCategories(myList, "mylist", 0));
            cats.Add(myList);
            //continueWatching
            RssLink continueWatching = new RssLink() { Name = Translate("Continue Watching"), HasSubCategories = true, ParentCategory = parentCategory };
            continueWatching.Other = (Func<List<Category>>)(() => GetListCategories(continueWatching, "continueWatching", 0));
            cats.Add(continueWatching);

            if (!IsKidsProfile)
            {
                RssLink browse = new RssLink() { Name = Translate("Browse"), HasSubCategories = true, ParentCategory = parentCategory };
                browse.Other = (Func<List<Category>>)(() => GetGenreListCategories(browse));
                cats.Add(browse);
            }
            else
            {
                RssLink browse = new RssLink() { Name = Translate("Browse") + " (Kids)", HasSubCategories = true, ParentCategory = parentCategory };
                browse.Other = (Func<List<Category>>)(() => GetKidsGenreListCategories(browse));
                cats.Add(browse);
            }

            //Do not remember profile cats, need to be loaded every time
            parentCategory.SubCategoriesDiscovered = false;
            return cats;
        }

        private List<Category> GetHomeCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=bale&esn=www",
                postData: @"{""paths"":[[""lolomo"",{""from"":0,""to"":" + noOfCatsInHome + @"},[""summary"",""title"",""playListEvidence"",""bookmark"",""queue"",""displayName"",""context""]]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            String lolmoGuid = json["value"]["lolomo"].Values().Last().ToString();
            if (enableVerboseLog) Log.Debug("lolmoGuid: {0}", lolmoGuid);
            for (int i = 0; i < (noOfCatsInHome + 1); i++)
            {
                JToken token = json["value"]["lolomos"][lolmoGuid][i.ToString()];
                if (token != null)
                {
                    if (enableVerboseLog) Log.Debug("token: {0}", token);
                    if (token.Values().Count() > 1)
                    {
                        JToken item = token.First();
                        if (enableVerboseLog) Log.Debug("item: {0}", item);
                        string list = token.Values().Last().ToString();
                        if (enableVerboseLog) Log.Debug("list: {0}", list);
                        if (enableVerboseLog) Log.Debug("context: {0}", json["value"]["lists"][list]["context"]);
                        if (json["value"]["lists"][list]["context"].Value<string>() != "queue" && json["value"]["lists"][list]["context"].Value<string>() != "continueWatching")
                        {
                            RssLink cat = new RssLink() { ParentCategory = parentCategory, Name = json["value"]["lists"][list]["displayName"].Value<string>(), Url = "\"" + list + "\"", HasSubCategories = true };
                            cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "lists", 0));
                            cats.Add(cat);
                        }
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetKidsGenreListCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(kidsUrl, forceUTF8: true);
            Regex rgx = new Regex(@"<li><a href=""/[kK]ids{0,1}/category/(?<url>[^""]*)"">(?<title>[^<]*)");
            foreach (Match m in rgx.Matches(data))
            {
                RssLink cat = new RssLink() { Name = HttpUtility.HtmlDecode(m.Groups["title"].Value), Url = m.Groups["url"].Value, ParentCategory = parentCategory, HasSubCategories = true  };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "genres", 0));
                cats.Add(cat);
            }
            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetKidsHomeCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(kidsUrl, forceUTF8: true);
            Regex rgx = new Regex(@"<div class=""mrow"".*?href=""/[kK]ids{0,1}/(?<type>[^/]*)/(?<url>[^""]*)"">(?<title>[^<]*)");
            foreach (Match m in rgx.Matches(data))
            {
                RssLink cat = new RssLink() { Name = HttpUtility.HtmlDecode(m.Groups["title"].Value), Url = m.Groups["url"].Value, ParentCategory = parentCategory, HasSubCategories = true };
                if (m.Groups["type"].Value == "category")
                {
                    cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "genres", 0));
                    cats.Add(cat);
                }
                else if (m.Groups["type"].Value == "similars")
                {
                    cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "similars", 0));
                    cats.Add(cat);
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetGenreListCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=bale&esn=www",
                postData: @"{""paths"":[[""genreList"",{""from"":0,""to"":24},[""id"",""menuName""]],[""genreList"",""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            foreach (JToken token in json["value"]["genres"].Where(t => t.Values().Count() > 1 && t.First()["menuName"] != null))
            {
                JToken item = token.First();
                RssLink cat = new RssLink() { ParentCategory = parentCategory, Name = item["menuName"].Value<string>(), Url = item["id"].Value<UInt32>().ToString(), HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "genres", 0, true));
                cats.Add(cat);
            }
            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetDetailsCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string id = (parentCategory as RssLink).Url;
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=bale&esn=www",
                postData: @"{""paths"":[[""videos""," + id + @",[""creators"",""cast"",""directors"",""tags"",""genres""],{""from"":0,""to"":49},[""id"",""name""]]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);

            // Creators
            Category creators = new Category() 
            {
                Name = Translate("Creator"),
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                ParentCategory = parentCategory
            };
            foreach (JToken token in json["value"]["videos"][id]["creators"].Where(t => t.Values().Count() == 2))
            {
                JToken item = token.First();
                string person = token.Values().Last().ToString();
                RssLink cat = new RssLink() { ParentCategory = creators, Name = json["value"]["person"][person]["name"].Value<string>(), Url = person, HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "person", 0));
                creators.SubCategories.Add(cat);
            }
            if (creators.SubCategories.Count > 0)
            {
                creators.Description = String.Join(", ", creators.SubCategories.Select(i => i.Name));
                cats.Add(creators);
            }

            // Directors
            Category directors = new Category()
            {
                Name = Translate("Director"),
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                ParentCategory = parentCategory
            };
            foreach (JToken token in json["value"]["videos"][id]["directors"].Where(t => t.Values().Count() == 2))
            {
                JToken item = token.First();
                string person = token.Values().Last().ToString();
                RssLink cat = new RssLink() { ParentCategory = directors, Name = json["value"]["person"][person]["name"].Value<string>(), Url = person, HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "person", 0));
                directors.SubCategories.Add(cat);
            }
            if (directors.SubCategories.Count > 0)
            {
                directors.Description = String.Join(", ", directors.SubCategories.Select(i => i.Name));
                cats.Add(directors);
            }

            // Cast
            Category cast = new Category()
            {
                Name = Translate("Cast"),
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                ParentCategory = parentCategory
            };
            foreach (JToken token in json["value"]["videos"][id]["cast"].Where(t => t.Values().Count() == 2))
            {
                JToken item = token.First();
                string person = token.Values().Last().ToString();
                RssLink cat = new RssLink() { ParentCategory = cast, Name = json["value"]["person"][person]["name"].Value<string>(), Url = person, HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "person", 0));
                cast.SubCategories.Add(cat);
            }
            if (cast.SubCategories.Count > 0)
            {
                cast.Description = String.Join(", ", cast.SubCategories.Select(i => i.Name));
                cats.Add(cast);
            }

            // Genres
            Category genres = new Category()
            {
                Name = Translate("Genres"),
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                ParentCategory = parentCategory
            };
            foreach (JToken token in json["value"]["videos"][id]["genres"].Where(t => t.Values().Count() == 2))
            {
                JToken item = token.First();
                string genre = token.Values().Last().ToString();
                RssLink cat = new RssLink() { ParentCategory = genres, Name = json["value"]["genres"][genre]["name"].Value<string>(), Url = genre, HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "genres", 0));
                genres.SubCategories.Add(cat);
            }
            if (genres.SubCategories.Count > 0)
            {
                genres.Description = String.Join(", ", genres.SubCategories.Select(i => i.Name));
                cats.Add(genres);
            }

            // Tags
            Category tags = new Category()
            {
                Name = (parentCategory.ParentCategory as NetflixCategory).IsShow ? Translate("This show is") : Translate("This movie is"),
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                ParentCategory = parentCategory
            };

            foreach (JToken token in json["value"]["videos"][id]["tags"].Where(t => t.First().Values().Count() > 0 && t.First<JToken>()["size"].Value<int>() == 2))
            {
                JToken item = token.First<JToken>();
                RssLink cat = new RssLink() { ParentCategory = tags, Name = item["name"].Value<string>(), Url = item["id"].Value<UInt32>().ToString(), HasSubCategories = true };
                cat.Other = (Func<List<Category>>)(() => GetSubCategories(cat, "genres", 0));
                tags.SubCategories.Add(cat);
            }
            if (tags.SubCategories.Count > 0)
            {
                tags.Description = String.Join(", ", tags.SubCategories.Select(i => i.Name));
                cats.Add(tags);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetListCategories(Category parentCategory, string listType, uint startIndex)
        {
            List<Category> cats = new List<Category>();

            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""lolomo"",""summary""],[""lolomo"",""" + listType + @""",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems) + @"},[""summary"",""title"",""synopsis"",""queue"",""userRating"",""runtime"",""releaseYear""]],[""lolomo"",""" + listType + @""",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems) + @"},""boxarts"",""_342x192"",""jpg""],[""lolomo"",""" + listType + @""",[""context"",""id"",""length"",""name"",""trackIds"",""requestId""]]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            if (json["value"] != null && json["value"]["videos"] != null)
            {

                foreach (JToken token in json["value"]["videos"].Where(t => t.Values().Count() > 1 && t.First()["title"] != null))
                {
                    JToken item = token.First();
                    JToken summary = item["summary"];
                    JToken userRating = item["userRating"];
                    NetflixCategory cat = new NetflixCategory() { ParentCategory = parentCategory, Name = item["title"].Value<string>(), HasSubCategories = true, InQueue = item["queue"]["inQueue"].Value<bool>(), IsShow = summary["type"].Value<string>() == "show" };
                    cat.Description = item["synopsis"].Value<string>() + "\r\n" + item["releaseYear"].Value<string>();
                    if (!string.IsNullOrWhiteSpace(userRating["userRating"].ToString()))
                        cat.Description += "\r\n" + Translate("User rating") + ": " + userRating["userRating"].ToString();
                    else if (!string.IsNullOrWhiteSpace(userRating["predicted"].ToString()))
                        cat.Description += "\r\n"+ string.Format (Translate("Predicted rating for {0}"), ProfileName) +": " + userRating["predicted"].ToString();
                    if (!string.IsNullOrWhiteSpace(userRating["average"].ToString()))
                        cat.Description += "\r\n" + Translate("Avg. rating") + ": " + userRating["average"].ToString();
                    cat.Runtime = cat.IsShow ? 0 : item["runtime"].Value<int>();
                    cat.Thumb = item["boxarts"]["_342x192"]["jpg"]["url"].Value<string>();
                    cat.Url = summary["id"].Value<UInt32>().ToString();
                    cat.Other = (Func<List<Category>>)(() => GetTitleCategories(cat));
                    cats.Add(cat);
                }

                //Paging
                int length = json["value"]["lists"].First(t => t.Values().Count() > 1).First()["length"].Value<int>();
                if (length > noOfItems + startIndex)
                {
                    NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                    next.Other = (Func<List<Category>>)(() => GetListCategories(parentCategory, listType, noOfItems + startIndex + 1));
                    cats.Add(next);
                }
            }
            //Do not remember My List, need to be able to load new items
            parentCategory.SubCategoriesDiscovered = false;
            return cats;
        }

        private List<Category> GetSubCategories(Category parentCategory, string categoryType, uint startIndex, bool getSubGenres = false)
        {
            List<Category> cats = new List<Category>();
            string id = (parentCategory as RssLink).Url;
            string data;
            JObject json;
            if (getSubGenres)
            {
                Category subgenreCat = new Category() {Name = Translate("Subgenres"), SubCategories = new List<Category>(), ParentCategory = parentCategory, HasSubCategories = true, SubCategoriesDiscovered = true};
                data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                    postData: @"{""paths"":[[""genres""," + id + @",""subgenres"",{""from"":0,""to"":20},""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                    contentType: "application/json");
                json = (JObject)JsonConvert.DeserializeObject(data);
                foreach (JToken token in json["value"]["genres"].Where(t => t.Values().Count() > 1 && t.First()["summary"] != null))
                {
                    JToken summary = token.First()["summary"];
                    RssLink subCat = new RssLink() { Name = summary["menuName"].Value<string>(), Url = summary["id"].Value<UInt32>().ToString(), HasSubCategories = true, ParentCategory = subgenreCat};
                    subCat.Other = (Func<List<Category>>)(() => GetSubCategories(subCat, categoryType, 0));
                    subgenreCat.SubCategories.Add(subCat);
                }
                if (subgenreCat.SubCategories.Count > 0)
                {
                    cats.Add(subgenreCat);
                }
            }

            data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""" + categoryType + @"""," + id + @",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems) + @"},[""summary"",""title"",""synopsis"",""queue"",""userRating"",""runtime"",""releaseYear""]],[""" + categoryType + @"""," + id + @",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems) + @"},""boxarts"",""_342x192"",""jpg""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            json = (JObject)JsonConvert.DeserializeObject(data);
            if (json["value"] != null && json["value"]["videos"] != null)
            {
                foreach (JToken token in json["value"]["videos"].Where(t => t.Values().Count() > 1 && t.First()["title"] != null))
                {
                    JToken item = token.First();
                    JToken summary = item["summary"];
                    JToken userRating = item["userRating"];
                    NetflixCategory cat = new NetflixCategory() { ParentCategory = parentCategory, Name = item["title"].Value<string>(), HasSubCategories = true, InQueue = item["queue"]["inQueue"].Value<bool>(), IsShow = summary["type"].Value<string>() == "show" };
                    cat.Description = item["synopsis"].Value<string>() + "\r\n " + item["releaseYear"].Value<string>();
                    if (!string.IsNullOrWhiteSpace(userRating["userRating"].ToString()))
                        cat.Description += "\r\n" + Translate("User rating") + ": " + userRating["userRating"].ToString();
                    else if (!string.IsNullOrWhiteSpace(userRating["predicted"].ToString()))
                        cat.Description += "\r\n" + string.Format(Translate("Predicted rating for {0}"), ProfileName) + ": " + userRating["predicted"].ToString();
                    if (!string.IsNullOrWhiteSpace(userRating["average"].ToString()))
                        cat.Description += "\r\n" + Translate("Avg. rating") + ": " + userRating["average"].ToString();
                    cat.Runtime = cat.IsShow ? 0 : item["runtime"].Value<int>();
                    cat.Thumb = item["boxarts"]["_342x192"]["jpg"]["url"].Value<string>();
                    cat.Url = summary["id"].Value<UInt32>().ToString();
                    cat.Other = (Func<List<Category>>)(() => GetTitleCategories(cat));
                    cats.Add(cat);
                }
                if (cats.Count() > noOfItems)
                {
                    NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                    next.Other = (Func<List<Category>>)(() => GetSubCategories(parentCategory, categoryType, noOfItems + startIndex + 1)); 
                    cats.Add(next);
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> GetTitleCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string id = (parentCategory as RssLink).Url;
            bool isShow = (parentCategory as NetflixCategory).IsShow;
            //Play Now/Continue
            RssLink playNowCat = new RssLink() { Name = Translate("Watch Now") + "/" + Translate("Play"), Description = parentCategory.Description, Thumb = parentCategory.Thumb, HasSubCategories = false, ParentCategory = parentCategory, Url = id };
            if (isShow)
                playNowCat.Other = (Func<List<VideoInfo>>)(() => GetPlayNowShowVideos(playNowCat));
            else
                playNowCat.Other = (Func<List<VideoInfo>>)(() => GetPlayNowMovieVideos(playNowCat));

            cats.Add(playNowCat);

            if (isShow)
            {
                //Seasons
                string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                    postData: @"{""paths"":[[""videos""," + id + @",""seasonList"",{""from"":0,""to"":20},""summary""],[""videos""," + id + @",""seasonList"",""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                    contentType: "application/json");
                JObject json = (JObject)JsonConvert.DeserializeObject(data);

                foreach (JToken token in json["value"]["seasons"].Where(t => t.Values().Count() > 1))
                {
                    JToken item = token.First();
                    RssLink cat = new RssLink() { HasSubCategories = false, Thumb = parentCategory.Thumb, ParentCategory = parentCategory };
                    JToken summary = item["summary"];
                    cat.Url = summary["id"].Value<UInt32>().ToString();
                    cat.Name = summary["name"].Value<string>();
                    cat.Description = parentCategory.Name + ", " + cat.Name; 
                    cat.Other = (Func<List<VideoInfo>>)(() => GetSeasonVideos(cat, summary["length"].Value<uint>()));
                    cats.Add(cat);
                }
            }
            //Similar
            RssLink similarCat = new RssLink() { Name = Translate("More like this"), Thumb = parentCategory.Thumb, HasSubCategories = true, ParentCategory = parentCategory, Url = id };
            similarCat.Other = (Func<List<Category>>)(() => GetSubCategories(similarCat, "similars", 0));
            cats.Add(similarCat);

            //Details
            RssLink detailsCat = new RssLink() { Name = Translate("Details"), Thumb = parentCategory.Thumb, HasSubCategories = true, ParentCategory = parentCategory, Url = id };
            detailsCat.Other = (Func<List<Category>>)(() => GetDetailsCategories(detailsCat));
            cats.Add(detailsCat);

            //Manage My List
            RssLink myListCat = new RssLink() { Thumb = parentCategory.Thumb, HasSubCategories = true, ParentCategory = parentCategory, Url = id };
            myListCat.Name = Translate("My List Add") + "/" + Translate("My List Remove");
            myListCat.Other = (Func<List<Category>>)(() => AddToMyListCategories(myListCat));
            cats.Add(myListCat);
            

            parentCategory.SubCategoriesDiscovered = true;
            return cats;
        }

        private List<Category> AddToMyListCategories(Category parentCategory)
        {
            bool inQ = (parentCategory.ParentCategory as NetflixCategory).InQueue;
            string addRemove = inQ ? "remove" : "add";
            string videoId = (parentCategory.ParentCategory as NetflixCategory).Url;
            string title = parentCategory.ParentCategory.Name;
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/playlistop?fallbackEsn=NFCDSF-01-",
                postData: @"{""operation"":""" + addRemove + @""",""videoId"":" + videoId + @",""trackId"":0,""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            //Do something with the result json...
            (parentCategory.ParentCategory as NetflixCategory).InQueue = !inQ;
            throw new OnlineVideosException("OK: " + (inQ ? Translate("My List Remove") : Translate("My List Add")));
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            var method = category.Other as Func<List<VideoInfo>>;
            List<VideoInfo> videos = new List<VideoInfo>();
            if (method != null)
            {
                videos = method.Invoke();
            }
            return videos;
        }

        private List<VideoInfo> GetSeasonVideos(Category category, uint noOfEpisodes)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string id = (category as RssLink).Url;
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""seasons""," + id + @",""episodes"",{""from"":-1,""to"":" + noOfEpisodes + @"},[""summary"",""synopsis"",""title"",""runtime""]],[""seasons""," + id + @",""episodes"",{""from"":-1,""to"":" + noOfEpisodes + @"},""interestingMoment"",""_260x146"",""jpg""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            foreach (JToken token in json["value"]["videos"].Where(t => t.Values().Count() > 1))
            {
                JToken item = token.First();
                VideoInfo video = new VideoInfo();
                JToken summary = item["summary"];
                uint e = summary["episode"].Value<uint>();
                uint s = summary["season"].Value<uint>();
                video.Title = s.ToString() + "x" + (e > 9 ? e.ToString() : ("0" + e.ToString())) + " " + item["title"].Value<string>();
                video.Description = item["synopsis"].Value<string>();
                video.Thumb = item["interestingMoment"]["_260x146"]["jpg"]["url"].Value<string>();
                video.VideoUrl = string.Format(playerUrl, summary["id"].Value<UInt32>());
                video.Length = OnlineVideos.Helpers.TimeUtils.TimeFromSeconds(item["runtime"].Value<int>().ToString());
                videos.Add(video);
            }

            return videos;
        }

        private List<VideoInfo> GetPlayNowShowVideos(Category category)
        {
            string id = (category as RssLink).Url;
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""videos""," + id + @",""current"",[""summary"",""runtime"",""title"",""synopsis""]],[""videos""," + id + @",""current"",[""interestingMoment""],""_260x146"",""jpg""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);
            JToken item = json["value"]["videos"].First(t => t.Values().Count() > 1 && t.First()["size"].Value<int>() > 2).First();
            VideoInfo video = new VideoInfo();
            JToken summary = item["summary"];
            uint e = summary["episode"].Value<uint>();
            uint s = summary["season"].Value<uint>();
            video.Title = s.ToString() + "x" + (e > 9 ? e.ToString() : ("0" + e.ToString())) + " " + item["title"].Value<string>();
            video.Description = item["synopsis"].Value<string>();
            video.Length = OnlineVideos.Helpers.TimeUtils.TimeFromSeconds(item["runtime"].Value<int>().ToString());
            video.Thumb = item["interestingMoment"]["_260x146"]["jpg"]["url"].Value<string>();
            video.VideoUrl = string.Format(playerUrl, summary["id"].Value<UInt32>());
            return new List<VideoInfo>() { video };
        }

        private List<VideoInfo> GetPlayNowMovieVideos(Category category)
        {
            string id = (category as RssLink).Url;
            VideoInfo video = new VideoInfo();
            video.Title = category.ParentCategory.Name;
            video.Description = category.ParentCategory.Description;
            video.Length = OnlineVideos.Helpers.TimeUtils.TimeFromSeconds((category.ParentCategory as NetflixCategory).Runtime.ToString());
            video.Thumb = category.Thumb;
            video.VideoUrl = string.Format(playerUrl, id);
            return new List<VideoInfo>() { video };
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
            List<SearchResultItem> results = new List<SearchResultItem>();
            if (currentProfile != null)
            {
                RssLink cat = new RssLink() { Url = @"""" + HttpUtility.UrlEncode(query) + @""""};
                cat.SubCategories = GetSubCategories(cat, "search", 0);
                cat.SubCategories.ForEach(c => results.Add(c));
            }
            else
            {
                throw new OnlineVideosException("Please select a profile before searching.");
            }
            return results;
        }

        #endregion

        #region Context menu

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> result = new List<ContextMenuEntry>();
            if (selectedCategory != null && selectedCategory is NetflixCategory)
            {
                ContextMenuEntry entry = new ContextMenuEntry() { DisplayText = (selectedCategory as NetflixCategory).InQueue ? Translate("My List Remove") : Translate("My List Add") };
                result.Add(entry);
            }
            return result;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if ((choice.DisplayText.StartsWith(Translate("My List Remove")) || choice.DisplayText.StartsWith(Translate("My List Add"))) && selectedCategory != null && selectedCategory is NetflixCategory)
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                bool inQ = (selectedCategory as NetflixCategory).InQueue;
                string addRemove = inQ ? "remove" : "add";
                string videoId = (selectedCategory as NetflixCategory).Url;
                string title = selectedCategory.Name;
                string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/playlistop?fallbackEsn=NFCDSF-01-",
                    postData: @"{""operation"":"""+ addRemove + @""",""videoId"":" + videoId + @",""trackId"":0,""authURL"":""" + latestAuthUrl + @"""}",
                    contentType: "application/json");
                JObject json = (JObject)JsonConvert.DeserializeObject(data);
                //Do something with the result json...
                (selectedCategory as NetflixCategory).InQueue = !inQ;
                result.RefreshCurrentItems = true;
                result.ExecutionResultMessage = title + " - OK: " + (inQ ? Translate("My List Remove") : Translate("My List Add"));
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion

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
            get { return username + "¥" + ProfileToken + (showLoadingSpinner ? "SHOWLOADING" : "") + (rememberLogin ? "REMEMBERLOGIN" : "") + (enableNetflixOsd ? "ENABLENETFLIXOSD" : "") + (disableLogging ? "DISABLELOGGING" : ""); }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }

        #endregion

    }
}
