using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
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
        string iViewUserAgent = @"https://tviview.abc.net.au/iview/feed/sony/?keyword=0-Z";

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

            new System.Threading.Thread(delegate(object o)
            {
                // Next line takes time. We can do this in the background while browsing takes place
                // Need to only wait once a URL needs to be played

                System.Threading.Mutex ThreadFeedSync = System.Threading.Mutex.OpenExisting("FeedSyncMutex");
                ThreadFeedSync.WaitOne();

                XmlDocument doc = new XmlDocument();
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

                        ProgramDictionary.Add(node["id"].InnerText, programData);
                    }
                }

                ThreadFeedSync.ReleaseMutex();
            }) { IsBackground = true, Name = "TVFeedDownload" }.Start(iViewTVFeedURL);
            
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
                    VideoInfo video = new VideoInfo();

                    video.Title = episode.Value<string>("seriesTitle");
                    if (!String.IsNullOrEmpty(episode.Value<string>("title")))
                    {
                        video.Title += ": " + episode.Value<string>("title");
                    }
                    
                    video.Description = "";
                    if (FeedSync.WaitOne(0))
                    {
                        ProgramData programData;

                        if (ProgramDictionary.TryGetValue(episode.Value<string>("episodeHouseNumber"), out programData))
                        {
                            video.Description = programData.description;
                        }

                        FeedSync.ReleaseMutex();
                    }

                    video.VideoUrl = episode.Value<string>("href");
                    video.Thumb = episode.Value<string>("thumbnail");
                    video.Length = Helpers.TimeUtils.TimeFromSeconds(episode.Value<string>("duration"));
                    video.Airdate = episode.Value<string>("pubDate");
                    video.Other = episode.Value<string>("episodeHouseNumber");
                    res.Add(video);
                }
            }
            
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string playURL = "";
            
            if (!FeedSync.WaitOne(60000))
            {
                //write error
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
                    //write error
                }
                FeedSync.ReleaseMutex();
            }

            return playURL;
        }

        #region API Helper

        private string GetiViewWebData(string url)
        {
            return GetWebData(url: iViewURLBase + url, userAgent: iViewUserAgent);
        }

        #endregion
    }
}
