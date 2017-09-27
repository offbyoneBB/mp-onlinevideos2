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

        [Category("OnlineVideosConfiguration"), Description("Device used in the ?device= part of the query")]
        string iViewHTTPDevice = @"hbb";

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

        private string GetiViewWebData(string url)
        {
            return GetWebData(url: iViewURLBase + url, userAgent: iViewUserAgent);
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

            video.Thumb = item.Value<string>("thumbnail");
            video.Length = Helpers.TimeUtils.TimeFromSeconds(item.Value<string>("duration"));
            video.Airdate = item.Value<string>("pubDate");
            video.Other = item.Value<string>("episodeHouseNumber");

            // Description and video come form the reading the json returned by href
            // If ?device=hbb (Hybrid Broadcast Broadband) is used then http URLs are returned for playback
            // TODO: Work with f4m manifest files etc. to support unmetered playback.
            string webData = GetiViewWebData(url: item.Value<string>("href") + "?device=" + iViewHTTPDevice);
            JObject programData = (JObject)JObject.Parse(webData);

            if (programData != null)
            {
                string description = programData.Value<string>("description");
                if (!String.IsNullOrEmpty(description))
                {
                    video.Description = description;
                }

                JArray playlist = programData["playlist"] as JArray;
                if (playlist != null)
                {
                    foreach (JToken playlistItem in playlist)
                    {
                        if (playlistItem.Value<string>("type").Equals("program"))
                        {
                            string stream = playlistItem.Value<string>("http");
                            if (!String.IsNullOrEmpty(stream))
                            {
                                video.VideoUrl = stream;
                            }
                        }
                    }
                }
            }

            return video;
        }

        #endregion
    }
}
