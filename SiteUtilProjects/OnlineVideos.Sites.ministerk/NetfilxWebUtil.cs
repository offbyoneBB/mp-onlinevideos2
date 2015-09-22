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
using OnlineVideos.Sites.Utils;
using System.Collections.Specialized;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{

    public class NetfilxWebUtil : SiteUtilBase, IBrowserSiteUtil
    {

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Netflix email")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Netflix password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Remember log-in in browser"), Description("Remember the log-in in the Browser Player")]
        protected bool rememberLogin = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show loading spinner"), Description("Show the loading spinner in the Browser Player")]
        protected bool showLoadingSpinner = true;


        private const uint noOfItems = 48;

        #region urls
        private string loginUrl = @"https://www.netflix.com/Login";
        private string homeUrl = @"https://www.netflix.com/";
        private string playerUrl = @"http://www.netflix.com/watch/{0}";
        private string loginPostData = @"authURL={0}&email={1}&password={2}&RememberMe=on";
        private string switchProfileUrl = @"{0}/{1}/profiles/switch?switchProfileGuid={2}";
        #endregion

        private string latestAuthUrl = "";

        #region GetWebData
        private string MyGetWebData(string url, string postData = null, string referer = null, string contentType = null)
        {
            //Never cache, problems with profiles sometimes
            string data = HboNordic.HboWebCache.Instance.GetWebData(url, postData: postData, cookies: Cookies, referer: referer, contentType: contentType, cache: false);
            //Side effects
            //AuthUrl
            Regex rgx = new Regex(@"""authURL"":""(?<authURL>[^""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                latestAuthUrl = m.Groups["authURL"].Value;
            }
            return data;
        }
        #endregion

        #region profiles
        private JToken currentProfile = null;
        private List<JToken> profiles = null;
        private string ProfileName
        {
            get
            {
                return currentProfile == null ? "" : HttpUtility.HtmlDecode(currentProfile["summary"]["profileName"].Value<string>());
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

        #region SiteUtilBase

        public override int DiscoverDynamicCategories()
        {
            LoadProfiles();
            foreach (JToken p in profiles)
            {
                currentProfile = p;
                RssLink profile = new RssLink()
                {
                    HasSubCategories = true,
                    Name = ProfileName + (IsKidsProfile ? " (Kids)" : string.Empty),
                    Thumb = ProfileIcon
                };
                currentProfile = null;
                profile.Other = (Func<List<Category>>)(() => GetProfileCategories(profile, p));
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
                parentCategory.SubCategoriesDiscovered = true;
                parentCategory.SubCategories = method.Invoke();
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        private List<Category> GetProfileCategories(Category parentCategory, JToken profile)
        {
            currentProfile = profile;
            MyGetWebData(string.Format(switchProfileUrl, ShaktiApi, BuildId, ProfileToken), referer: homeUrl);
            List<Category> cats = new List<Category>();
            if (!IsKidsProfile)
            {
                RssLink browse = new RssLink() { Name = "Browse", HasSubCategories = true, ParentCategory = parentCategory };
                browse.Other = (Func<List<Category>>)(() => GetBrowseCategories(browse));
                cats.Add(browse);
            }
            else
            {
                //Add kids categories
            }
            RssLink myList = new RssLink() { Name = "My List", HasSubCategories = true, ParentCategory = parentCategory };
            myList.Other = (Func<List<Category>>)(() => GetMyListCategories(myList, 0));
            cats.Add(myList);
            //Do not remember profile cats, need to be loaded every time
            parentCategory.SubCategoriesDiscovered = false;
            return cats;
        }

        private List<Category> GetBrowseCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=bale&esn=www",
                postData: @"{""paths"":[[""genreList"",{""from"":0,""to"":24},[""id"",""menuName""]],[""genreList"",""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            //TODO... Genarate cats from data....
            return cats;
        }

        private List<Category> GetMyListCategories(Category parentCategory, uint startIndex)
        {
            List<Category> cats = new List<Category>();

            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""lolomo"",""summary""],[""lolomo"",""mylist"",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems - 1) + @"},[""summary"",""title"",""synopsis""]],[""lolomo"",""mylist"",{""from"":" + startIndex + @",""to"":" + (startIndex + noOfItems) + @"},""boxarts"",""_342x192"",""jpg""],[""lolomo"",""mylist"",[""context"",""id"",""length"",""name"",""trackIds"",""requestId""]]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);

            foreach (JToken token in json["value"]["videos"].Where(t => t.Values().Count() > 1 && t.First()["title"] != null))
            {
                JToken item = token.First();
                RssLink cat = new RssLink() { ParentCategory = parentCategory, Name = item["title"].Value<string>(), Description = item["synopsis"].Value<string>(), HasSubCategories = true };
                cat.Thumb = item["boxarts"]["_342x192"]["jpg"]["url"].Value<string>();
                JToken summary = item["summary"];
                cat.Url = summary["id"].Value<UInt32>().ToString();
                if (summary["type"].Value<string>() == "show")
                    cat.Other = (Func<List<Category>>)(() => GetShowCategories(cat));
                else
                    cat.Other = (Func<List<Category>>)(() => GetMovieCategories(cat));
                cats.Add(cat);
            }

            //Paging
            int length = json["value"]["lists"].First(t => t.Values().Count() > 1).First()["length"].Value<int>();
            if (length > noOfItems + startIndex)
            {
                NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                next.Other = (Func<List<Category>>)(() => GetMyListCategories(parentCategory, noOfItems + startIndex));
                cats.Add(next);
            }
            //Do not remember My List, need to be able to load new items
            parentCategory.SubCategoriesDiscovered = false;
            return cats;
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

        private List<Category> GetShowCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string id = (parentCategory as RssLink).Url;
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=harris&fallbackEsn=SLW32",
                postData: @"{""paths"":[[""videos""," + id + @",""seasonList"",{""from"":0,""to"":20},""summary""],[""videos""," + id + @",""seasonList"",""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            JObject json = (JObject)JsonConvert.DeserializeObject(data);

            //Play Now/Continue
            RssLink playNowCat = new RssLink() { Name = "Play Now/Continue", HasSubCategories = false, ParentCategory = parentCategory, Url = id };
            playNowCat.Other = (Func<List<VideoInfo>>)(() => GetPlayNowShowVideos(playNowCat));
            cats.Add(playNowCat);

            //Seasons
            foreach (JToken token in json["value"]["seasons"].Where(t => t.Values().Count() > 1))
            {
                JToken item = token.First();
                RssLink cat = new RssLink() { HasSubCategories = false, ParentCategory = parentCategory };
                JToken summary = item["summary"];
                cat.Url = summary["id"].Value<UInt32>().ToString();
                cat.Name = summary["name"].Value<string>();
                cat.Other = (Func<List<VideoInfo>>)(() => GetSeasonVideos(cat, summary["length"].Value<uint>()));
                cats.Add(cat);
            }
            return cats;
        }

        private List<Category> GetMovieCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string id = (parentCategory as RssLink).Url;
            //Play Now/Continue
            RssLink playNowCat = new RssLink() { Name = "Play Now", Thumb = parentCategory.Thumb, HasSubCategories = false, ParentCategory = parentCategory, Url = id };
            playNowCat.Other = (Func<List<VideoInfo>>)(() => GetPlayNowMovieVideos(playNowCat));
            cats.Add(playNowCat);

            return cats;
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
            video.Thumb = category.Thumb;
            video.VideoUrl = string.Format(playerUrl, id);
            return new List<VideoInfo>() { video };
        }

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

    }
}
