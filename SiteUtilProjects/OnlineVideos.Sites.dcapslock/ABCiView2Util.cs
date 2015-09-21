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

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL. This is used to look up Description information and Play URLs.")]
        string iViewUserAgent = @"ABC iview/3.9.4 (iPad; iOS 8.2; Scale/2.00)";

        [Category("OnlineVideosConfiguration"), Description("iView Device that allows best user experience with Online Videos")]
        string iViewDevice = @"ios-tablet";

        [Category("OnlineVideosConfiguration"), Description("iView Tablet App Version that was used for testing and development")]
        string iViewAppver = @"3.9.4-7";

        [Category("OnlineVideosConfiguration"), Description("iView TV Feed URL. This is used to look up Description information and Play URLs.")]
        string iViewTVFeedURL = @"https://tviview.abc.net.au/iview/feed/sony/?keyword=0-Z";

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
            //Note: These are always unmetered. 
            //TODO: Play F4M manifest files which will allow unmetered playback.

            new System.Threading.Thread(FeedSyncWorker) { IsBackground = true, Name = "TVFeedDownload" }.Start(iViewTVFeedURL);
            
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
                    VideoInfo video = new VideoInfo();

                    SearchResults.Add(GetVideoInfoFromItem(SearchItem));
                }
            }

            return SearchResults.ConvertAll(v => (SearchResultItem)v);
        }

        #endregion

        #region API Helper

        private string GetiViewWebData(string url)
        {
            return GetWebData(url: iViewURLBase + url, userAgent: iViewUserAgent);
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
                    video.Description = "Descriptions downloading...";
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

            // Next line takes time. We can do this in the background while browsing takes place
            // Need to only wait once a URL needs to be played
            Log.Debug("ABCiView2Util: FeedSync Worker Thread Begin");
            string feedData = GetWebData(o as String);

            doc.LoadXml(feedData);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", "http://namespace.feedsync");

            XmlNodeList nodes = doc.GetElementsByTagName("asset");
            foreach (XmlNode node in nodes)
            {
                if (node["type"].InnerText == "video")
                {
                    ProgramData programData;
                    programData.title = node["title"].InnerText;
                    programData.description = node["description"].InnerText;
                    programData.playURL = node["assetUrl"].InnerText;

                    try
                    {
                        ProgramDictionary.Add(node["id"].InnerText, programData);
                    }
                    catch (ArgumentException)
                    {
                        Log.Debug("ABCiView2Util: Duplicate id reading TV feed: {0}", node["id"].InnerText);
                    }
                }
            }

            ThreadFeedSync.ReleaseMutex();
        }
        #endregion
    }
}
