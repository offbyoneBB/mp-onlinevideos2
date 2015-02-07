using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Xml;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Util for ytv.com.
    /// </summary>
    public class YTVUtil : GenericSiteUtil
    {
        protected virtual string landingPageUrl { get { return @"http://www.ytv.com/videos/"; } }
        protected virtual string iframeXpath { get { return @"(//iframe)[2]"; } }
        protected virtual int itemsPerPage { get { return 8; } }

        private static string byCategories;
        private static string player;
        private static string feedsServiceUrl;
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            HtmlDocument html = GetWebData<HtmlDocument>(landingPageUrl);
            if (html != null)
            {
                // retrieve Main-Player URL
                HtmlNode iframe = html.DocumentNode.SelectSingleNode(iframeXpath);
                if (iframe != null)
                {
                    string mainPlayerUrl = iframe.GetAttributeValue(@"src", string.Empty);
                    html = GetWebData<HtmlDocument>(mainPlayerUrl);
                    if (html != null)
                    {
                        HtmlNode div = html.DocumentNode.SelectSingleNode(@"//div[@id = 'tpReleaseModel1']");
                        if (div != null)
                        {
                            string tpParams = div.GetAttributeValue(@"tp:params", string.Empty);
                            feedsServiceUrl = div.GetAttributeValue(@"tp:feedsServiceURL", string.Empty);
                            if (!string.IsNullOrEmpty(tpParams))
                            {
                                string tpParamsDecoded = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(tpParams));
                                Log.Debug("Decoded tp:params {0}", tpParamsDecoded);
                                NameValueCollection parameters = HttpUtility.ParseQueryString(tpParamsDecoded);
                                byCategories = parameters["byCategories"];
                                player = parameters["params"];
                                
                                // add All category
                                Settings.Categories.Add(new RssLink() {
                                                            Name = @"All",
                                                            HasSubCategories = false
                                                        });
                                // add other main categories
                                populateMainCategories();
                            }
                        }
                    }
                }
            }
            
            return Settings.Categories.Count;
        }
        
        private void populateMainCategories()
        {
            // dictionary with names as keys and HasSubCategories as values
            Dictionary<string, bool> mainCategories = new Dictionary<string, bool>();
            
            foreach (string category in byCategories.Split('|'))
            {
                string[] parts = category.Split('/');
                string key = parts[0];
                if (!mainCategories.ContainsKey(key))
                {
                    mainCategories.Add(key, parts.Length > 1);
                }
                else
                {
                    // see if value needs to be updated
                    if (!mainCategories[key])
                    {
                        mainCategories[key] = parts.Length > 1;
                    }
                }
            }
            
            // sort categories and add to top-level categories
            foreach (var item in mainCategories.OrderBy(c => c.Key))
            {
                Settings.Categories.Add(new RssLink() {
                                            Name = item.Key,
                                            HasSubCategories = item.Value
                                        });
            }
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            List<string> subCategories = new List<string>();
            
            foreach (string category in byCategories.Split('|'))
            {
                string[] parts = category.Split('/');
                if (parts.Length < 2 || !parentCategory.Name.Equals(parts[0])) continue;
                
                subCategories.Add(parts[1]);
            }
            subCategories.Sort();
            
            foreach (string subCategory in subCategories)
            {
                parentCategory.SubCategories.Add(new RssLink() {
                                                     Name = subCategory,
                                                     HasSubCategories = false
                                                 });
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters[@"byCategories"] = @"All".Equals(category.Name) ? byCategories : string.Format(@"{0},{1}", byCategories, category.Name);
            parameters[@"fields"] = @"author,content,defaultThumbnailUrl,description,pubDate,title";
            parameters[@"range"] = string.Format(@"{0}-{1}", 1, itemsPerPage);
            parameters[@"validFeed"] = @"false";
            parameters[@"count"] = @"true";
            parameters[@"form"] = @"json";
            parameters[@"types"] = @"none";
            parameters[@"byContent"] = @"byFormat=mpeg4|f4m|flv";
            parameters[@"fileFields"] = @"bitrate,duration,format,url";
            parameters[@"params"] = player;
            
            UriBuilder builder = new UriBuilder(feedsServiceUrl);
            builder.Query = parameters.ToString();
            
            return getVideoListForSinglePage(category, builder.ToString());
        }

        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = string.Empty;
            currentCategory = category;
            
            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                int totalResults = json.Value<int>("totalResults");
                int entryCount = json.Value<int>("entryCount");
                int startIndex = json.Value<int>("startIndex");

                JArray entries = json["entries"] as JArray;
                if (entries != null)
                {
                    foreach (JToken entry in entries)
                    {
                        VideoInfo video = CreateVideoInfo();
                        video.Title = entry.Value<string>("title");
                        video.Description = entry.Value<string>("description");
                        long epochSeconds = entry.Value<long>("pubDate") / 1000;
                        // convert epoch (seconds since unix time) to a date string
                        video.Airdate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds).ToShortDateString();
                        video.Thumb = entry.Value<string>("plmedia$defaultThumbnailUrl");
                        result.Add(video);
                        
                        video.PlaybackOptions = new Dictionary<string, string>();
                        // keep track of bitrates and URLs
                        Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

                        JArray mediaList = entry["media$content"] as JArray;
                        
                        if (mediaList != null)
                        {
                            foreach (JToken media in mediaList)
                            {
                                // convert seconds to timespan
                                video.Length = TimeSpan.FromSeconds(Math.Round(media.Value<float>("plfile$duration"))).ToString();
                                int bitrate = media.Value<int>("plfile$bitrate");
                                if (!urlsDictionary.ContainsKey(bitrate / 1000))
                                {
                                    urlsDictionary.Add(bitrate / 1000, media.Value<string>("plfile$url"));
                                }
                            }
                            
                            // sort the URLs ascending by bitrate
                            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                            {
                                video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), string.Format(@"{0}&manifest=f4m", item.Value));
                            }
                        }
                    }
                }
                
                if (totalResults > (startIndex + entryCount - 1))
                {
                    UriBuilder builder = new UriBuilder(url);
                    NameValueCollection parameters = HttpUtility.ParseQueryString(builder.Query);
                    string[] rangeParts = parameters["range"].Split('-');
                    int lowerRange = int.Parse(rangeParts[0]) + itemsPerPage;
                    int upperRange = int.Parse(rangeParts[1]) + itemsPerPage;
                    // modify range parameter for next page URL
                    parameters["range"] = string.Format(@"{0}-{1}", lowerRange, upperRange);
                    builder.Query = parameters.ToString();
                    nextPageUrl = builder.Uri.ToString();
                }
            }
            
            return result;
        }
        
        public override bool HasNextPage {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }
        
        public override List<VideoInfo> GetNextPageVideos()
        {
            return getVideoListForSinglePage(currentCategory, nextPageUrl);
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            string result = string.Empty;
            if (string.IsNullOrEmpty(video.VideoUrl))
            {
                // video URL is empty, so URLs must be explored from the playback options
                foreach (var item in video.PlaybackOptions.OrderBy(u => u.Key))
                {
                    result = item.Value;
                }
            }
            return result;
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new YTVVideoInfo();
        }

        private class YTVVideoInfo : VideoInfo {
            // class created solely for the purpose of overriding GetPlaybackOptionUrl
            public override string GetPlaybackOptionUrl(string option)
            {
                string url = PlaybackOptions[option];
                XmlDocument xml = WebCache.Instance.GetWebData<XmlDocument>(url);
    
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                namespaceManager.AddNamespace("a", @"http://www.w3.org/2005/SMIL21/Language");
                XmlNode src = xml.SelectSingleNode(@"//a:video/@src", namespaceManager);
                string manifestUrl = src.InnerText.EndsWith("?") ? src.InnerText : string.Format("{0}?", src.InnerText);
                return new MPUrlSourceFilter.HttpUrl(string.Format(@"{0}hdcore=2.11.3", manifestUrl)).ToString();
            }
        }
    }
}
