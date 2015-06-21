using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class NetflixBetaUtil : SiteUtilBase, IBrowserSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Netflix email")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Netflix password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Startup user profile"), Description("User profile to start with, defaults to owner profile. Case sensitive.")]
        protected string startupUserProfile = null;

        #region urls
        private string loginUrl = @"https://www.netflix.com/Login";
        private string homeUrl = @"https://www.netflix.com/";
        private string loginPostData = @"authURL={0}&email={1}&password={2}&RememberMe=on";
        private string switchProfileUrl = @"{0}/{1}/profiles/switch?switchProfileGuid={2}";
        #endregion

        private string latestAuthUrl = "";

        #region GetWebData
        private string MyGetWebData(string url, string postData = null, string referer = null, string contentType = null, bool isLoadingProfile = false)
        {
//            if (!isLoadingProfile)
//                LoadProfiles();
            string data = HboNordic.HboWebCache.Instance.GetWebData(url, postData: postData, cookies: Cookies, referer: referer, contentType: contentType);
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

        private void LoadProfiles()
        {
            if (profiles == null || profiles.Count == 0)
            {
                string data = MyGetWebData(homeUrl, isLoadingProfile: true);
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
                    if (string.IsNullOrEmpty(ProfileName) || !profiles.Any(p => HttpUtility.HtmlDecode(p["summary"]["profileName"].Value<string>()) == ProfileName))
                    {
                        if (!string.IsNullOrWhiteSpace(startupUserProfile) && profiles.Any(p => HttpUtility.HtmlDecode(p["summary"]["profileName"].Value<string>()) == startupUserProfile))
                        {
                            currentProfile = profiles.FirstOrDefault(p => HttpUtility.HtmlDecode(p["summary"]["profileName"].Value<string>()) == startupUserProfile);
                        }
                        else
                        {
                            currentProfile = profiles.FirstOrDefault(p => p["summary"]["isAccountOwner"].Value<bool>());
                        }
                    }
                    else
                    {
                        currentProfile = profiles.FirstOrDefault(p => HttpUtility.HtmlDecode(p["summary"]["profileName"].Value<string>()) == ProfileName);
                    }
                    MyGetWebData(string.Format(switchProfileUrl, ShaktiApi, BuildId, ProfileToken), referer: homeUrl, isLoadingProfile: true);
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
                _shaktiApi = m.Groups[1].Value.Replace("http:","https:");
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
                    var data = GetWebData<HtmlDocument>(url, string.Format(loginPostData, HttpUtility.UrlEncode(authUrl), HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password)), _cc);
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
            RssLink browse = new RssLink() { Name = "Browse", HasSubCategories = true, Other = (Func<List<Category>>)(() => GetBrowseCategories()) };
            Settings.Categories.Add(browse);
            return 1;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        private List<Category> GetBrowseCategories()
        {
            List<Category> cats = new List<Category>();
            string data = MyGetWebData(ShaktiApi + "/" + BuildId + "/pathEvaluator?withSize=true&materialize=true&model=bale&esn=www",
                postData: @"{""paths"":[[""genreList"",{""from"":0,""to"":24},[""id"",""menuName""]],[""genreList"",""summary""]],""authURL"":""" + latestAuthUrl + @"""}",
                contentType: "application/json");
            return cats;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return new List<VideoInfo>();
        }

        #endregion

        #region IBrowserSiteUtil
        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get
            {
                return "OnlineVideos.Sites.BrowserUtilConnectors.NetflixBetaConnector";
            }
        }

        string IBrowserSiteUtil.UserName
        {
            get { return username; }
        }

        string IBrowserSiteUtil.Password
        {
            get { return password; }
        }

        #endregion

    }
}
