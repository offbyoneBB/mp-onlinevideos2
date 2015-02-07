using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class CanWestUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("The Url from where to discover the feed PID")]
        protected string feedPIDUrl = null;
        [Category("OnlineVideosConfiguration"), Description("The Regex String for parsing the feed PID")]
        protected string feedPIDRegexString = null;
        [Category("OnlineVideosConfiguration"), Description("The player tag which is part of the URL for getting the main categories via JSON")]
        protected string playerTag = null;
        [Category("OnlineVideosConfiguration"), Description("URL of the SWF player (SwfVfy will also be set to true if this is provided)")]
        protected string swfUrl = null;

        private static string baseUrlPrefix = @"http://feeds.theplatform.com/ps/JSON/PortalService/2.2/";
        private static string categoriesJsonUrl = baseUrlPrefix + @"getCategoryList?PID={0}&field=ID&field=depth&field=title&field=hasReleases&field=fullTitle&field=hasChildren&query=CustomText|PlayerTag|{1}";
        private static string releasesJsonUrl = baseUrlPrefix + @"getReleaseList?PID={0}&field=title&field=PID&field=ID&field=description&field=categoryIDs&field=thumbnailURL&field=URL&field=airdate&field=length&field=bitrate&sortField=airdate&sortDescending=true&startIndex=1&endIndex=100&query=CategoryIDs|{1}";
        private static string videoContentUrl = @"http://release.theplatform.com/content.select?pid={0}&format=SMIL&mbr=true";

        private static int rootDepth = 1;

        protected string feedPID;
        
        protected Regex feedPIDRegex;

        private static Regex rtmpUrlRegex = new Regex(@"^(?<host>rtmp.*?)(\{break\}|\<break\>)(?<playPath>.*?)$",
            RegexOptions.Compiled);

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            
            if (!string.IsNullOrEmpty(feedPIDRegexString)) feedPIDRegex = new Regex(feedPIDRegexString, RegexOptions.Compiled);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(feedPIDUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                Match match = feedPIDRegex.Match(webData);
                if (match.Success)
                {
                    feedPID = match.Groups["feedPID"].Value;

                    Log.Debug(@"Feed PID: {0}", feedPID);

                    DiscoverDynamicCategoriesUsingJson(null);
                }
                else
                {
                    Log.Warn(@"Feed PID not found at {0}", feedPIDUrl);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        protected void DiscoverDynamicCategoriesUsingJson(Category parentCategory)
        {
            JObject json = GetWebData<JObject>(String.Format(categoriesJsonUrl, feedPID, playerTag));
            if (json != null)
            {
                JArray allItems = json["items"] as JArray;
                if (allItems != null)
                {
                    JToken itemToFetchChildrenFor = null;
                    foreach (JToken item in allItems)
                    {
                        // find first element with root depth - 1 (which also has children)
                        if (item.Value<int>("depth") == rootDepth - 1 && item.Value<bool>("hasChildren"))
                        {
                            itemToFetchChildrenFor = item;
                            break;
                        }
                    }

                    if (itemToFetchChildrenFor != null)
                    {
                        // fetch all children (which have releases) for a particular item
                        JArray childItemsWithReleases = FetchChildItemsWithReleases(itemToFetchChildrenFor, allItems, itemToFetchChildrenFor.Value<int>("depth"));

                        foreach (JToken item in childItemsWithReleases)
                        {
                            bool hasChildren = item.Value<bool>("hasChildren");

                            // populate category
                            RssLink cat = new RssLink() {
                                Name = item.Value<string>("title"),
                                Other =  hasChildren
                                    ?
                                    item.Value<string>("depth") + @"|" + item.Value<string>("fullTitle")
                                    :
                                    item.Value<string>("ID"),
                                HasSubCategories = hasChildren
                            };
                            if (parentCategory == null)
                            {
                                Settings.Categories.Add(cat);
                            }
                            else
                            {
                                parentCategory.SubCategories.Add(cat);
                            }
                        }
                    }
                }
            }
        }

        /*
         * checks an item to see if any of it's children has a release. Iterates over the items array
         * and recursively digs deeper into the children.
         */
        private JArray FetchChildItemsWithReleases(JToken itemToFetchChildrenFor, JArray allItems, int originalDepth)
        {
            int itemDepth = itemToFetchChildrenFor.Value<int>("depth");
            string itemFullTitle = itemToFetchChildrenFor.Value<string>("fullTitle");
            bool itemHasReleases = itemToFetchChildrenFor.Value<bool>("hasReleases");
            Log.Debug(@"Looking to fetch children for {0} {1} {2}", itemDepth, itemFullTitle, itemHasReleases);

            JArray result = new JArray();
            foreach (JToken item in allItems)
            {
                if (result.HasValues && item.Value<int>("depth") > originalDepth + 1)
                {
                    // already found sufficient results for this level, no need to dig deeper
                    continue;
                }

                if (item.Value<int>("depth") == itemDepth + 1
                    &&
                    item.Value<string>("fullTitle").StartsWith(itemFullTitle)
                    &&
                    (item.Value<bool>("hasReleases") || FetchChildItemsWithReleases(item, allItems, originalDepth).HasValues)
                    )
                {
                    Log.Debug(@"Adding child (with release) {0} when checking for {1}", item.Value<string>("fullTitle"), itemFullTitle);
                    result.Add(item);
                }
            }
            return result;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink)parentCategory;
            string parentOther = (string)parentRssLink.Other;
            string[] splitSubstrings = parentOther.Split('|');
            int parentDepth = int.Parse(splitSubstrings[0]);
            string parentFullTitle = splitSubstrings[1];

            Log.Debug(@"Discovering subcategories for: {0}, depth: {1}, full title: {2}", parentCategory.Name, parentDepth, parentFullTitle);

            JObject json = GetWebData<JObject>(String.Format(categoriesJsonUrl, feedPID, playerTag));

            if (json != null)
            {
                JArray allItems = json["items"] as JArray;

                if (allItems != null)
                {
                    JToken itemToFetchChildrenFor = null;
                    foreach (JToken item in allItems)
                    {
                        // find item which matches the subcategory being discovered
                        if (item.Value<int>("depth") == parentDepth && item.Value<string>("fullTitle").Equals(parentFullTitle))
                        {
                            itemToFetchChildrenFor = item;
                            break;
                        }
                    }

                    if (itemToFetchChildrenFor != null)
                    {
                        // fetch all children (which have releases) for a particular item
                        JArray childItemsWithReleases = FetchChildItemsWithReleases(itemToFetchChildrenFor, allItems, itemToFetchChildrenFor.Value<int>("depth"));

                        foreach (JToken item in childItemsWithReleases)
                        {
                            // populate subcategories
                            string fullTitle = item.Value<string>("fullTitle");
                            bool hasChildren = item.Value<bool>("hasChildren");

                            Log.Debug(@"Subcategory: {0}", fullTitle);

                            RssLink cat = new RssLink();
                            cat.Name = item.Value<string>("title");
                            cat.HasSubCategories = hasChildren;
                            cat.Other = hasChildren
                                ?
                                item.Value<int>("depth") + @"|" + fullTitle
                                :
                                item.Value<string>("ID");
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            Log.Debug(@"Looking for videos in category: {0}", category.Name);

            string id = (string)((RssLink)category).Other;
            JObject json = GetWebData<JObject>(String.Format(releasesJsonUrl, feedPID, id));

            if (json != null)
            {
                JArray items = json["items"] as JArray;

                if (items != null)
                {
                    // use dictionary to keep track of which titles have been seen already
                    // (the titles may appear multiple times due to multiple bitrates)
                    Dictionary<string, bool> dictionary = new Dictionary<string, bool>();

                    foreach (JToken item in items)
                    {
                        string title = item.Value<string>("title");

                        if (!dictionary.ContainsKey(title))
                        {
                            long epochSeconds = long.Parse(item.Value<string>("airdate")) / 1000;
                            Log.Debug(@"Video Title: {0}, airdate: {1}", title, epochSeconds);

                            dictionary.Add(title, true);
                            result.Add(new VideoInfo()
                            {
                                Title = title,
                                VideoUrl = String.Format(videoContentUrl, item.Value<string>("PID")),
                                Description = item.Value<string>("description"),
                                Thumb = item.Value<string>("thumbnailURL"),
                                Length = TimeSpan.FromSeconds(item.Value<int>("length") / 1000).ToString(),
                                // convert epoch (seconds since unix time) to a date string
                                Airdate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds).ToShortDateString()
                            });
                        }
                    }
                }
            }

            return result;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            XmlDocument xml = GetWebData<XmlDocument>(video.VideoUrl);

            Log.Debug(@"SMIL loaded");

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");

            XmlNode metaBase = xml.SelectSingleNode(@"//a:meta", nsmRequest);
            string metaBaseValue = metaBase.Attributes["base"].Value;

            // base URL may be stored in the base attribute of <meta> tag
            string baseRtmp = metaBaseValue.StartsWith("rtmp") ? metaBaseValue : String.Empty;

            foreach (XmlNode node in xml.SelectNodes("//a:switch/a:video", nsmRequest))
            {
                int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                // skip bitrate is zero
                if (bitrate == 0) continue;

                string url = node.Attributes["src"].Value;
                // skip if advertisement
                if (url.StartsWith("pfadx")) continue;
                    
                if (!string.IsNullOrEmpty(baseRtmp))
                {
                    // prefix url with base (from <meta> tag) and artifical <break>
                    url = baseRtmp + @"<break>" + url;
                }
                Log.Debug(@"bitrate: {0}, url: {1}", bitrate / 1000, url);

                if (url.StartsWith("rtmp"))
                {
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);

                    if (rtmpUrlMatch.Success && !urlsDictionary.ContainsKey(bitrate / 1000))
                    {
                        string host = rtmpUrlMatch.Groups["host"].Value;
                        string playPath = rtmpUrlMatch.Groups["playPath"].Value;
                        if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                        {
                            // prepend with mp4:
                            playPath = @"mp4:" + playPath;
                        }
                        else if (playPath.EndsWith(@".flv"))
                        {
                            // strip extension
                            playPath = playPath.Substring(0, playPath.Length - 4);
                        }
                        Log.Debug(@"Host: {0}, PlayPath: {1}", host, playPath);
                        MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = playPath };
                        if (!string.IsNullOrEmpty(swfUrl))
                        {
                            rtmpUrl.SwfUrl = swfUrl;
                            rtmpUrl.SwfVerify = true;
                        }
                        urlsDictionary.Add(bitrate / 1000, rtmpUrl.ToString());
                    }
                }
            }

            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
            {
                video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            
            // if result is still empty then perhaps we are geo-locked
            if (string.IsNullOrEmpty(result))
            {
                XmlNode geolockReference = xml.SelectSingleNode(@"//a:ref", nsmRequest);
                if (geolockReference != null)
                {
                    Log.Error(@"You are not in a geographic region that has access to this content.");
                    result = string.Format(@"{0}{1}",
                                           metaBaseValue,
                                           geolockReference.Attributes["src"].Value);
                }
            }
            return result;
        }
    }
}
