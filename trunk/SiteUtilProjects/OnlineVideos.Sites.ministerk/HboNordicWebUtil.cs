using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using OnlineVideos.Sites.Utils;
using System.Xml;
using OnlineVideos.Sites.HboNordic;
using System.Web;

namespace OnlineVideos.Sites
{
    #region Session class
    internal class HboNordicSession
    {
        private string deviceId = null;
        public string DeviceId
        {
            get
            {
                if (deviceId == null)
                {
                    var engine = new Jurassic.ScriptEngine();
                    engine.Execute(Properties.Resources.HboNordicJs);
                    deviceId = engine.CallGlobalFunction("__guid").ToString();
                }
                return deviceId;
            }
        }
        public string Token { get; set; }
        public string IdentityGuid { get; set; }
        public string AccountGuid { get; set; }
    }

    #endregion

    public class HboNordicWebUtil : SiteUtilBase, IBrowserSiteUtil
    {
        
        #region SiteUtilBase Config

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("E-mail"), Description("HBO Nordic username e-mail")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("HBO Nordic password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("In English please"), Description("Get titles and descriptions in english (does not affect subtitles).")]
        protected bool useEnglish = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show forum category"), Description("Enable or disable forum category (Link to forum - http://tinyurl.com/olv-hbonordic)")]
        protected bool showForumCategory = true;

        #endregion

        #region Variables

        private const string ItemTypeCategory = "CATEGORY";
        private const string ItemTypeWatchList = "SAVED";
        private const string ItemTypeSeen = "LEAF";
        private HboNordicSession session = null;
        
        #endregion

        #region Properties

        private string BaseUrl
        {
            get
            {
                string lang = "";
                switch (Settings.Language)
                {
                    case "sv":
                        lang = "https://se.hbonordic.com/";
                        break;
                    case "da":
                        lang = "https://dk.hbonordic.com/";
                        break;
                    case "fi":
                        lang = "https://fi.hbonordic.com/";
                        break;
                    case "no":
                        lang = "https://no.hbonordic.com/";
                        break;
                    default:
                        break;
                }
                return lang;
            }
        }
        private string ApiUrl
        {
            get
            {
                return BaseUrl + "cloffice/client/web/browse/";
            }
        }
        private string SaveUrl
        {
            get
            {
                return BaseUrl + "cloffice/client/web/savedAsset";
            }
        }
        private string AccountUrl
        {
            get
            {
                return BaseUrl + "account/settings";
            }
        }
        private string WatchlistUrl
        {
            get
            {
                return BaseUrl + "watchlist";
            }
        }
        private string SearchUrl
        {
            get
            {
                return BaseUrl + "cloffice/client/web/search";
            }
        }
        private string Language
        {
            get
            {
                string lang = "en_hbon";
                if (!useEnglish)
                {
                    switch (Settings.Language)
                    {
                        case "sv":
                            lang = "sv_hbon";
                            break;
                        case "da":
                            lang = "da_hbon";
                            break;
                        case "fi":
                            lang = "fi_hbon";
                            break;
                        case "no":
                            lang = "nb_hbon";
                            break;
                        default:
                            break;
                    }
                }
                return lang;
            }
        }
        private HboNordicSession Session
        {
            get
            {
                if (session == null)
                {
                    session = new HboNordicSession();
                    InitializeSession();
                }
                return session;
            }
            set
            {
                session = value;
            }
        }
        private bool HaveCredentials
        {
            get
            {
                return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
            }
        }
        #endregion

        #region Public SiteUtilBase overrides

        #region Category
 
        public override int DiscoverDynamicCategories()
        {
            if (!HaveCredentials) throw new OnlineVideosException("Please enter your HBO Nordic credentials in site configuration");
   
            string url = ConstructUrl(ApiUrl);
            XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(url, referer: BaseUrl, headers: ConstructNameValueCollection());
            foreach (XmlElement item in xDoc.SelectNodes("//item"))
            {
                string cat = item.GetElementsByTagName("clearleap:itemType")[0].InnerText.ToUpper();
                if (cat == ItemTypeCategory || cat == ItemTypeSeen || cat == ItemTypeWatchList)
                {
                    Settings.Categories.Add(new RssLink()
                    {
                        Name = item.GetElementsByTagName("title")[0].InnerText,
                        HasSubCategories = cat == ItemTypeCategory,
                        Url = item.GetElementsByTagName("link")[0].InnerText,
                        SubCategories = new List<Category>()
                    });
                }
            }
            if (showForumCategory) Settings.Categories.Add(new Category() { Name = "Forum", Description = "Visit the forums for instructions, help and support: http://tinyurl.com/olv-hbonordic", Other = "forum", HasSubCategories = false });
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            GetCategories(parentCategory);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        #endregion
        
        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is string && category.GetOtherAsString() == "forum")
                throw new OnlineVideosException("Visit http://tinyurl.com/olv-hbonordic");
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = ConstructUrl((category as RssLink).Url);
            XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(url, referer: BaseUrl, headers: ConstructNameValueCollection(), cache: false);
            foreach (XmlElement item in xDoc.SelectNodes("//item"))
            {
                //I don't know how to select with namespace...
                XmlNodeList medias = item.GetElementsByTagName("media:group")[0].ChildNodes;
                XmlElement thumb = null;
                foreach (XmlElement elt in medias)
                {
                    if (elt.Name == "media:thumbnail")
                    {
                        thumb = elt;
                        break;
                    }
                }
                string title = item.GetElementsByTagName("title")[0].InnerText;
                XmlNodeList episodeNodes = item.GetElementsByTagName("clearleap:episodeInSeason");
                if (episodeNodes != null && episodeNodes.Count > 0)
                {
                    string episode = episodeNodes[0].InnerText;
                    string season = item.GetElementsByTagName("clearleap:season")[0].InnerText;
                    string series = item.GetElementsByTagName("clearleap:series")[0].InnerText;
                    int s = 0;
                    int e = 0;
                    int.TryParse(episode, out e);
                    int.TryParse(season, out s);
                    title = series + " - " + s + "x" + (e > 9 ? e.ToString() : "0" + e.ToString()) + " " + title;
                }
                bool isSaved = false;
                XmlNodeList savedAsset = item.GetElementsByTagName("clearleap:savedAsset");
                if (savedAsset != null && savedAsset.Count > 0)
                {
                    bool.TryParse(savedAsset[0].InnerText, out isSaved);
                }
                string other = @"{{""saved"":{0}, ""guid"":""{1}""}}";
                XmlNodeList desc = item.GetElementsByTagName("description");
                videos.Add(new VideoInfo()
                {
                    Title = title,
                    Description = (desc != null && desc.Count > 0) ? desc[0].InnerText : string.Empty,
                    Thumb = (thumb != null) ? thumb.Attributes["url"].Value : "",
                    Other = string.Format(other, isSaved.ToString().ToLower(), item.GetElementsByTagName("guid")[0].InnerText)
                });
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string other = video.GetOtherAsString();
            JObject json = (JObject)JsonConvert.DeserializeObject(other);
            bool isSaved = json["saved"].Value<bool>();
            if (isSaved)
                if (!UnsaveVideo(json["guid"].Value<string>()))
                    throw new OnlineVideosException("Could not play video. Please try agian");
            if (!SaveVideo(json["guid"].Value<string>()))
                throw new OnlineVideosException("Could not play video. Please try agian");
            return WatchlistUrl + "|" + (!isSaved).ToString();
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
            Category cat = new RssLink() { Url = SearchUrl, Name = "Search", SubCategories = new List<Category>() };
            GetCategories(cat, query);
            cat.SubCategories.ForEach(c => results.Add(c));
            return results;
        }

        #endregion

        #region Context menu

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
            if (selectedItem != null)
            {
                ContextMenuEntry entry = new ContextMenuEntry();
                string other = selectedItem.GetOtherAsString();
                JObject json = (JObject)JsonConvert.DeserializeObject(other);
                bool isSaved = json["saved"].Value<bool>();
                entry.DisplayText = isSaved ? "Remove from watchlist" : "Add to watchlist";
                entries.Add(entry);
            }
            return entries;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (selectedItem != null && choice.DisplayText.Contains("watchlist"))
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                string other = selectedItem.GetOtherAsString();
                JObject json = (JObject)JsonConvert.DeserializeObject(other);
                bool isSaved = json["saved"].Value<bool>();
                string guid = json["guid"].Value<string>();
                bool success;
                if (isSaved)
                {
                    success = UnsaveVideo(guid);
                }
                else
                {
                    success = SaveVideo(guid);
                }
                result.RefreshCurrentItems = success;
                result.ExecutionResultMessage = (success ? "OK: " : "ERROR: ") + choice.DisplayText + " (" + selectedItem.Title + ")";
                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion

        #endregion

        #region Private methods

        private void GetCategories(Category parentCategory, string query = null)
        {
            string url = ConstructUrl((parentCategory as RssLink).Url, query: query);
            XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(url, referer: BaseUrl, headers: ConstructNameValueCollection());
            foreach (XmlElement item in xDoc.SelectNodes("//item"))
            {
                string cat = item.GetElementsByTagName("clearleap:itemType")[0].InnerText.ToUpper();
                string keywords = "";
                XmlNodeList keywordNode = item.GetElementsByTagName("media:keywords");
                if (keywordNode != null && keywordNode.Count > 0) keywords = keywordNode[0].InnerText;
                XmlNodeList thumbs = item.GetElementsByTagName("media:thumbnail");
                XmlNodeList descriptions = item.GetElementsByTagName("description");
                parentCategory.SubCategories.Add(new RssLink()
                {
                    Name = item.GetElementsByTagName("title")[0].InnerText,
                    Url = item.GetElementsByTagName("link")[0].InnerText,
                    Description = (descriptions != null && descriptions.Count > 0) ? descriptions[0].InnerText : parentCategory.Description,
                    HasSubCategories = cat == ItemTypeCategory || keywords == "series",
                    Thumb = (thumbs != null && thumbs.Count > 0) ? thumbs[0].Attributes["url"].Value : parentCategory.Thumb,
                    ParentCategory = parentCategory,
                    SubCategories = new List<Category>()
                });
            }
        }

        private string ConstructUrl(string url, string guid = null, string query = null)
        {
            url += "?responseType=xml&language=" + Language;
            url += "&deviceId=" + Session.DeviceId;
            url += "&deviceToken=" + Session.Token;
            url += guid != null ? ("&guid=" + guid) : "";
            url += query != null ? ("&query=" + HttpUtility.UrlEncode(query)) : "";
            return url;
        }

        private NameValueCollection ConstructNameValueCollection()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("Accept", "application/xml, text/xml, */*; q=0.01");
            nvc.Add("X-Requested-With", "XMLHttpRequest");
            nvc.Add("Cookie", ConstructCookiesString());
            return nvc;
        }

        private string ConstructCookiesString()
        {
            return string.Format("deviceId={0}; language={1}; isPaying=false; deviceToken={2}",
               HttpUtility.UrlEncode(Session.DeviceId), HttpUtility.UrlEncode(Language), HttpUtility.UrlEncode(Session.Token));
        }

        private bool SaveVideo(string guid)
        {
            string url = ConstructUrl(SaveUrl, guid);
            XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(url, postData: "", referer: BaseUrl, headers: ConstructNameValueCollection(), contentType: "application/xml");
            XmlNode status = xDoc.SelectSingleNode("//status");
            return status != null && status.InnerText.ToLower().Contains("success");
        }

        private bool UnsaveVideo(string guid)
        {
            string url = ConstructUrl(SaveUrl, guid);
            XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(url, referer: BaseUrl, headers: ConstructNameValueCollection(), requestMethod: "DELETE", contentType: "application/xml");
            XmlNode status = xDoc.SelectSingleNode("//status");
            return status != null && status.InnerText.ToLower().Contains("success");
        }

        private void InitializeSession()
        {
            try
            {
                NameValueCollection nvc = new NameValueCollection();
                string basic = username + ":" + System.Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                basic = "Basic " + System.Convert.ToBase64String(Encoding.UTF8.GetBytes(basic));
                nvc.Add("Authorization", basic);
                nvc.Add("Accept", "application/xml, text/xml, */*; q=0.01");
                nvc.Add("X-Requested-With", "XMLHttpRequest");
                string data = string.Format("<device><deviceId>{0}</deviceId><type>web</type></device>", Session.DeviceId);
                XmlDocument xDoc = HboWebCache.Instance.GetWebData<XmlDocument>(BaseUrl + "cloffice/client/device/login?language=" + Language, postData: data, headers: nvc, cache: false, referer: BaseUrl, contentType: "application/xml");
                XmlNode status = xDoc.SelectSingleNode("//status");
                if (status != null && status.InnerText.Contains("Success"))
                {
                    Session.Token = xDoc.SelectSingleNode("//token").InnerText;
                    Session.IdentityGuid = xDoc.SelectSingleNode("//identityGuid").InnerText;
                    Session.AccountGuid = xDoc.SelectSingleNode("//accountGuid").InnerText;
                }
                else
                {
                    Session = null;
                    throw new OnlineVideosException("Unable to login");
                }
            }
            catch
            {
                Session = null;
                throw new OnlineVideosException("Unable to login");
            }
        }

        #endregion

        #region IBrowserSiteUtil implementation

        public string UserName
        {
            get
            {
                return username + "|" + AccountUrl;
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
                return "OnlineVideos.Sites.BrowserUtilConnectors.HboNordicConnector";
            }
        }

        #endregion

    }
}