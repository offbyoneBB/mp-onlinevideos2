using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class GlobalUtil : SiteUtilBase
    {
        private static string baseUrl = @"http://feeds.theplatform.com/ps/JSON/PortalService/2.2/";
        private static string feedPIDUrl = @"http://www.globaltv.com/widgets/ThePlatformContentBrowser/js/cwpGlobalVC.js";
        private static string categoriesJsonUrl = baseUrl + @"getCategoryList?PID={0}&field=ID&field=depth&field=title&field=hasReleases&field=fullTitle&field=hasChildren&query=CustomText|PlayerTag|z/Global%20Video%20Centre";
        private static string releasesJsonUrl = baseUrl + @"getReleaseList?PID={0}&field=title&field=PID&field=ID&field=description&field=categoryIDs&field=thumbnailURL&field=URL&field=airdate&field=length&field=bitrate&sortField=airdate&sortDescending=true&startIndex=1&endIndex=100&query=CategoryIDs|{1}";
        private static string videoContentUrl = @"http://release.theplatform.com/content.select?pid={0}&format=SMIL&mbr=true";

        private static int rootDepth = 1;

        private string feedPID;

        private static Regex feedPIDRegex = new Regex(@"data.PID\s=\sdata\.PID\s\|\|\s""(?<feedPID>[^""]*)"";",
            RegexOptions.Compiled);
        private static Regex rtmpUrlRegex = new Regex(@"^(?<host>rtmp.*?)(\{break\}|\<break\>)(?<playPath>.*?)$",
            RegexOptions.Compiled);

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

                    JObject json = GetWebData<JObject>(String.Format(categoriesJsonUrl, feedPID));
                    if (json != null)
                    {
                        JArray allItems = json["items"] as JArray;
                        if (allItems != null)
                        {
                            JToken itemToFetchChildrenFor = null;
                            foreach (JToken item in allItems)
                            {
                                // find first element with root depth - 1
                                if (item.Value<int>("depth") == rootDepth - 1)
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
                                    // populate category
                                    RssLink cat = new RssLink();
                                    cat.Name = item.Value<string>("title");
                                    cat.Other = item.Value<string>("depth") + @"|" + item.Value<string>("fullTitle");
                                    cat.HasSubCategories = true;
                                    Settings.Categories.Add(cat);
                                }
                            }
                        }
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
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

            RssLink parentRssLink = (RssLink) parentCategory;
            string parentOther = (string) parentRssLink.Other;
            string[] splitSubstrings = parentOther.Split('|');
            int parentDepth = int.Parse(splitSubstrings[0]);
            string parentFullTitle = splitSubstrings[1];

            Log.Debug(@"Discovering subcategories for: {0}, depth: {1}, full title: {2}", parentCategory.Name, parentDepth, parentFullTitle);

            JObject json = GetWebData<JObject>(String.Format(categoriesJsonUrl, feedPID));

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

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            Log.Debug(@"Looking for videos in category: {0}", category.Name);

            string id = (string) ((RssLink) category).Other;
            JObject json = GetWebData<JObject>(String.Format(releasesJsonUrl, feedPID, id));

            if (json != null)
            {
                JArray items = json["items"] as JArray;

                if (items != null)
                {
                    // use dictionary to keep track of which titles have been seen already
                    // (the titles appear multiple times due to multiple bitrates)
                    Dictionary<string, bool> dictionary = new Dictionary<string, bool>();

                    foreach (JToken item in items)
                    {
                        string title = item.Value<string>("title");

                        if (!dictionary.ContainsKey(title))
                        {
                            long epochSeconds = long.Parse(item.Value<string>("airdate"))/1000;
                            Log.Debug(@"Video Title: {0}, airdate: {1}", title, epochSeconds);

                            dictionary.Add(title, true);
                            result.Add(new VideoInfo()
                            {
                                 Title = title,
                                 VideoUrl = String.Format(videoContentUrl, item.Value<string>("PID")),
                                 Description = item.Value<string>("description"),
                                 ImageUrl = item.Value<string>("thumbnailURL"),
                                 Length = TimeSpan.FromSeconds(item.Value<int>("length")/1000).ToString(),
                                 // convert epoch (seconds since unix time) to a date string
                                 Airdate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds).ToShortDateString()
                            });
                        }
                    }
                }
            }

            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

            string webData = GetWebData(video.VideoUrl);
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(webData);

            Log.Debug(@"SMIL loaded");

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");

            foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
            {
                int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                // do not bother unless bitrate is non-zero
                if (bitrate == 0) continue;

                string url = node.Attributes["src"].Value;
                Log.Debug(@"bitrate: {0}, url: {1}", bitrate / 1000, url);

                if (url.StartsWith("rtmp"))
                {
                    Match rtmpUrlMatch = rtmpUrlRegex.Match(url);

                    if (rtmpUrlMatch.Success)
                    {
                        string host = rtmpUrlMatch.Groups["host"].Value;
                        string playPath = rtmpUrlMatch.Groups["playPath"].Value;
                        if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                        {
                            playPath = @"mp4:" + playPath;
                        }
                        else if (playPath.EndsWith(@".flv"))
                        {
                            playPath = playPath.Substring(0, playPath.Length - 4);
                        }
                        Log.Debug(@"Host: {0}, PlayPath: {1}", host, playPath);
                        urlsDictionary.Add(bitrate / 1000, new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = playPath}.ToString());
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
            return result;
        }
    }
}
