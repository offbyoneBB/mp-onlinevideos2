using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.MPUrlSourceFilter;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class ABCiView2Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("iView API Base URL")]
        string iViewURLBase = @"https://iview.abc.net.au/api/";

        [Category("OnlineVideosConfiguration"), Description("iView Home URL, relative off the Base URL")]
        string iViewURLHome = @"navigation/mobile/2/";

        [Category("OnlineVideosConfiguration"), Description("User agent string used for testing and development")]
        string iViewUserAgent = @"ABC iview/3.9.4 (iPad; iOS 8.2; Scale/2.00)";

        [Category("OnlineVideosConfiguration"), Description("iView Device that allows best user experience with Online Videos")]
        string iViewDevice = @"ios-tablet";

        [Category("OnlineVideosConfiguration"), Description("iView Tablet App Version that was used for testing and development")]
        string iViewAppver = @"3.9.4-7";

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL. This is the base URL used to look up Description information and Play URLs.")]
        string iViewTVFeedURL = @"https://tviview.abc.net.au/iview/feed/samsung/?keyword=";

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL Subsets. This is the subsets to break up the time to retrieve TV feed from server.")]
        string iViewTVFeedURLSubsets = @"a-g,h-m,n-t,u-z";

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL Subsets. This is the subsets to break up the time to retrieve TV feed from server.")]
        string iViewTVFeedURLUsername = @"feedtest";

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL Subsets. This is the subsets to break up the time to retrieve TV feed from server.")]
        string iViewTVFeedURLPassword = @"abc123";

        public struct TVFeed
        {
            public string URL;
            public List<string> URLSubsets;
            public string Username;
            public string Password;
        }

        public struct ProgramData
        {
            public string title;
            public string description;
            public string playURL;
        }

        static private Dictionary<string, ProgramData> ProgramDictionary = new Dictionary<string,ProgramData>();

        private System.Threading.Mutex FeedSync = new System.Threading.Mutex(false, "FeedSyncMutex");

        public override int DiscoverDynamicCategories()
        {
            //Download the Master TV Feed List from which we get unrestricted play URLs
            //Note: These are always metered. 
            //TODO: Play F4M manifest files which will allow unmetered playback.

            List<string> TVFeedURLSubsets = new List<string>(iViewTVFeedURLSubsets.Split(','));
            TVFeed ivewTVFeed = new TVFeed() { URL = iViewTVFeedURL, URLSubsets = TVFeedURLSubsets, Username = iViewTVFeedURLUsername, Password = iViewTVFeedURLPassword };
            new System.Threading.Thread(FeedSyncWorker) { IsBackground = true, Name = "TVFeedDownload" }.Start(ivewTVFeed);
            
            List<Category> dynamicCategories = new List<Category>();

            string webData = "{items:" + GetiViewWebData(iViewURLHome + "?device=" + iViewDevice + "?appver=" + iViewAppver) + "}";
            JObject contentData = (JObject)JObject.Parse(webData);

            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        switch (item.Value<string>("title"))
                        {
                            case "Home":
                                RssLink cat = new RssLink();
                                cat.Name = item.Value<string>("title");
                                cat.Url = item.Value<string>("path");
                                cat.HasSubCategories = true;
                                dynamicCategories.Add(cat);
                                break;
                            case "Browse":
                                JArray submenus = item["submenus"] as JArray;
                                if (submenus != null)
                                {
                                    foreach (JToken submenu in submenus)
                                    {
                                        RssLink subcat = new RssLink();
                                        subcat.Name = submenu.Value<string>("title");
                                        subcat.HasSubCategories = true;
                                        switch (submenu.Value<string>("title"))
                                        {
                                            case "Channels":
                                                JArray channels = submenu["channels"] as JArray;
                                                AddSubcats(subcat, channels);
                                                dynamicCategories.Add(subcat);
                                                break;
                                            case "Categories":
                                                JArray categories = submenu["submenus"] as JArray;
                                                AddSubcats(subcat, categories);
                                                dynamicCategories.Add(subcat);
                                                break;
                                            case "Programs A-Z":
                                                subcat.Url = submenu.Value<string>("path");
                                                dynamicCategories.Add(subcat);
                                                break;
                                        }
                                    }
                                }
                                break;
                            //end switch
                        }
                    }
                }
            }

            foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void AddSubcats(RssLink parentCat, JArray categories)
        {
            parentCat.SubCategories = new List<Category>();

            foreach (JToken category in categories)
            {
                RssLink cat = new RssLink();
                cat.Name = category.Value<string>("title");
                cat.ParentCategory = parentCat;
                cat.Url = category.Value<string>("path");
                cat.HasSubCategories = true;
                parentCat.SubCategories.Add(cat);
            }

            parentCat.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string Sort = "";

            switch (parentCategory.Name)
            {
                case "Home":
                case "Programs A-Z":
                    break;
                default:
                    Sort = @"/all/?sort=az";
                    break;
            }

            string webData = GetiViewWebData(((RssLink)parentCategory).Url + Sort);
            JObject contentData = (JObject)JObject.Parse(webData);
            parentCategory.SubCategories = new List<Category>();
            if (contentData != null)
            {
                JArray carousels = contentData["carousels"] as JArray;
                if (carousels != null)
                {
                    foreach (JToken carousel in carousels)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = carousel.Value<string>("title");
                        cat.ParentCategory = parentCategory;
                        cat.Other = carousel["episodes"];
                        cat.HasSubCategories = false;
                        parentCategory.SubCategories.Add(cat);
                    }
                }

                JArray collections = contentData["collections"] as JArray;
                if (collections != null)
                {
                    foreach (JToken collection in collections)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = collection.Value<string>("title");
                        cat.ParentCategory = parentCategory;
                        cat.Other = collection["episodes"];
                        cat.HasSubCategories = false;
                        parentCategory.SubCategories.Add(cat);
                    }
                }
                JArray indexLetters = contentData["index"] as JArray;
                if (indexLetters != null)
                {
                    foreach (JToken indexLetter in indexLetters)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = indexLetter.Value<string>("title");
                        cat.ParentCategory = parentCategory;
                        cat.Other = indexLetter["episodes"];
                        cat.HasSubCategories = false;
                        parentCategory.SubCategories.Add(cat);
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> res = new List<VideoInfo>();
            JArray episodes = category.Other as JArray;
            if (episodes != null)
            {
                foreach (JToken episode in episodes)
                {
                    res.Add(GetVideoInfoFromItem(episode));
                }
            }
            
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string playURL = "";
            
            if (!FeedSync.WaitOne(60000))
            {
                Log.Error("ABCiViewUtil2: Wait for FeedSync Thread failed");
            }
            else
            {
                ProgramData programData;

                if (ProgramDictionary.TryGetValue(video.Other.ToString(), out programData))
                {
                    playURL = programData.playURL;
                }
                else
                {
                    //Try and get the Feed Again as it may be out of date
                    Log.Debug("ABCiView2Util: Cannot find episode in TV Feed. Refresh");

                    new System.Threading.Thread(FeedSyncWorker) { IsBackground = true, Name = "TVFeedDownload" }.Start(iViewTVFeedURL);
                    if (!FeedSync.WaitOne(60000))
                    {
                        if (ProgramDictionary.TryGetValue(video.Other.ToString(), out programData))
                        {
                            playURL = programData.playURL;
                        }
                        else
                        {
                            Log.Debug("ABCiView2Util: Cannot find episode in TV Feed after refresh");
                        }
                    }
                    else
                    {
                        Log.Error("ABCiViewUtil2: Wait for FeedSync Thread failed");
                    }
                }
                FeedSync.ReleaseMutex();
            }

            return playURL;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            base.HasNextPage = false;

            List<VideoInfo> SearchResults = new List<VideoInfo>();

            string webData = "{items:" + GetiViewWebData(@"/search/?keyword=" + HttpUtility.UrlEncode(query) + "&fields=seriesTitle,title,href,episodeHouseNumber,thumbnail,duration,pubDate") + "}";
            JObject contentData = (JObject)JObject.Parse(webData);

            if (contentData != null)
            {
                JArray SearchItems = contentData["items"] as JArray;

                foreach (JToken SearchItem in SearchItems)
                {
                    SearchResults.Add(GetVideoInfoFromItem(SearchItem));
                }
            }

            return SearchResults.ConvertAll(v => (SearchResultItem)v);
        }

        #endregion

        #region ContextMenu

        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> result = new List<ContextMenuEntry>();
            if (selectedItem != null)
            {
                result.Add(new ContextMenuEntry() { DisplayText = Translation.Instance.RelatedVideos, Action = ContextMenuEntry.UIAction.Execute });
                result.Add(new ContextMenuEntry() { DisplayText = Translation.Instance.Recommendations, Action = ContextMenuEntry.UIAction.Execute });
            }

            return result;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            ContextMenuExecutionResult result = new ContextMenuExecutionResult();
            try
            {
                if (choice.DisplayText == Translation.Instance.RelatedVideos)
                {
                    result.ResultItems = GetRelatedVideos(selectedItem, "Episode").ConvertAll<SearchResultItem>(v => v as SearchResultItem);
                }
                else if (choice.DisplayText == Translation.Instance.Recommendations)
                {
                    result.ResultItems = GetRelatedVideos(selectedItem, "More Like This").ConvertAll<SearchResultItem>(v => v as SearchResultItem);
                }
            }
            catch (Exception ex)
            {
                throw new OnlineVideosException(ex.Message);
            }
            
            return result;
        }

        #endregion
        
        #region API Helper

        private static string SanitizeXml(string message)
        {
            // Invalid XML will stymie the XmlDocument.Load method, so we'll check for possible problems

            System.Text.StringBuilder sanitized = new System.Text.StringBuilder(message);

            for (int i = 0; i < sanitized.Length; ++i)
            {
                if (  sanitized[i] >= 0x00 && sanitized[i] <= 0x08 ||
                      sanitized[i] >= 0x0B && sanitized[i] <= 0x0C ||
                      sanitized[i] >= 0x0E && sanitized[i] <= 0x19    )
                {
                    sanitized.Remove(i, 1);
                }
            }

            return sanitized.ToString();
        }

        private string GetiViewWebData(string url)
        {
            return GetWebData(url: iViewURLBase + url, userAgent: iViewUserAgent);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private List<VideoInfo> GetRelatedVideos(VideoInfo video, string indexTitleSearch)
        {
            List<VideoInfo> RelatedVideos = new List<VideoInfo>();

            // "/related/<episodeHouseNumber> gives a JSON file including
            // index list containing two arrays, one with title like "2 Other Episodes"
            // and one like "More Like This"

            string webData = GetiViewWebData(@"/related/" + video.Other );
            JObject contentData = (JObject)JObject.Parse(webData);

            if (contentData != null)
            {
                JArray relatedItemsIndex = contentData["index"] as JArray;
                if (relatedItemsIndex != null)
                {
                    foreach (JToken episodeArray in relatedItemsIndex)
                    {
                        if (episodeArray["title"].ToString().Contains(indexTitleSearch))
                        {
                            foreach (JToken episode in episodeArray["episodes"])
                            {
                                RelatedVideos.Add(GetVideoInfoFromItem(episode));
                            }
                        }
                    }
                }
            }            
            
            return RelatedVideos;
        }

        private VideoInfo GetVideoInfoFromItem(JToken item)
        {
            VideoInfo video = new VideoInfo();

            video.Title = item.Value<string>("seriesTitle");
            if (!String.IsNullOrEmpty(item.Value<string>("title")))
            {
                video.Title += ": " + item.Value<string>("title");
            }

            video.Description = "";
            if (FeedSync.WaitOne(0))
            {
                ProgramData programData;

                if (ProgramDictionary.TryGetValue(item.Value<string>("episodeHouseNumber"), out programData))
                {
                    video.Description = programData.description;
                }
                else
                {
                    video.Description = "<" + Translation.Instance.GettingVideoDetails + ">";
                }

                FeedSync.ReleaseMutex();
            }

            video.VideoUrl = item.Value<string>("href");
            video.Thumb = item.Value<string>("thumbnail");
            video.Length = Helpers.TimeUtils.TimeFromSeconds(item.Value<string>("duration"));
            video.Airdate = item.Value<string>("pubDate");
            video.Other = item.Value<string>("episodeHouseNumber");

            return video;
        }

        private void FeedSyncWorker(object o)
        {
            System.Threading.Mutex ThreadFeedSync = System.Threading.Mutex.OpenExisting("FeedSyncMutex");
            ThreadFeedSync.WaitOne();

            XmlDocument doc = new XmlDocument();
            TVFeed FeedData = (TVFeed)o;
            string Base64Auth = Base64Encode(String.Format("{0}:{1}", FeedData.Username, FeedData.Password));
            string Authorization = String.Format("Basic {0}", Base64Auth);
            System.Collections.Specialized.NameValueCollection headers = new System.Collections.Specialized.NameValueCollection() { { "Authorization", Authorization } };

            Log.Debug("ABCiView2Util: FeedSync Worker Thread Begin");

            // We break up the TV Feed requests as when we ask for teh full a-z the server can crash
            // with exception that the request is too long

            foreach (string Subset in FeedData.URLSubsets)
            {
                Log.Debug("ABCiView2Util: Downloaidng TV Feed: " + FeedData.URL + Subset);

                string feedData = GetWebData(url: FeedData.URL + Subset, headers: headers);

                // TVFeed has been known to contain invalid characters.
                // Need to convert to spaces

                doc.LoadXml(SanitizeXml(feedData));
                XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
                nsmRequest.AddNamespace("a", "http://namespace.feedsync");

                XmlNodeList nodes = doc.GetElementsByTagName("item");
                foreach (XmlNode node in nodes)
                {
                    ProgramData programData;
                    programData.title = node["title"].InnerText;
                    programData.description = node["description"].InnerText;
                    programData.playURL = node["abc:videoAsset"].InnerText;

                    List<string> guidParts = new List<string>(node["guid"].InnerText.Split('/'));
                    string episodeHouseNumber = guidParts[guidParts.Count - 1];

                    if (!ProgramDictionary.ContainsKey(episodeHouseNumber))
                    {
                        ProgramDictionary.Add(episodeHouseNumber, programData);
                    }
                }
            }

            ThreadFeedSync.ReleaseMutex();
        }
        #endregion
    }
}
