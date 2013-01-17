using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TSNUtil : GenericSiteUtil
    {
        private Regex _episodeListRegex = new Regex(@"<dt><a\shref=""[^#]*#clip(?<episode>[^""]*)""\sonclick.*?Thumbnail:'(?<thumb>[^']*)'.*?Description:'(?<description>[^']*)'.*?Title:'(?<title>[^']*)'",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private Regex clipUrlRegex = new Regex(@"Video\.Load\({url:'(?<url>[^']*)'.*?",
            RegexOptions.Compiled);
        // capture group <params> is optional
        private Regex rtmpUrlRegex = new Regex(@"rtmpe://(?<host>[^/]*)/ondemand/(?<file>[^?]*)\??(?<params>.*)?",
            RegexOptions.Compiled);
        
        private static int PAGE_SIZE_FOR_NON_CURRENT_SPORTS = 25;
        private static string mainCategoriesUrl = @"http://www.tsn.ca/config/videoHubMenu.xml";
        private static string feedHandlerUrl = @"http://www.tsn.ca/video_services/videobinfeedhandler_v3.ashx?id={0}&clipsperpage=35";
        private static string urlgenFormat = @"http://esi.ctv.ca/datafeed/flv/urlgenjsext.aspx?formatid={0}&timeZone=%2D4&vid={1}";
        private static string CURRENT_SPORTS = @"current-sports";
        // top-level <item>s which have <urlLatest> as a child
        private static string mainCategoriesXpathFeatured = @"/menu/item[urlLatest]";
        // top-level <item>s which have <item> as a child
        private static string mainCategoriesXpath = @"/menu/item[item]";
        // sub-level <items> which contain exact title in the <text> node in the parent <item>
        private static string subcategoriesXpath = @"//item[text[.='{0}']]/item";

        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            // remember that Settings.Categories already has Current Sports categories from XML config
            // so do not Clear() Settings.Categories
            
            // mark live TV categories as a separate type so that they can be handled separately
            Settings.Categories.ToList().ForEach(c => MarkCategoryAsCurrentSports(c));
            
            XmlDocument xml = GetWebData<XmlDocument>(mainCategoriesUrl);
            if (xml != null) {
                foreach (XmlNode item in xml.SelectNodes(mainCategoriesXpathFeatured))
                {
                     Settings.Categories.Add(new RssLink() {
                                                Name = item.SelectSingleNode("text").InnerText,
                                                HasSubCategories = false,
                                                Url = item.SelectSingleNode("urlLatest").InnerText.Trim()
                                            });
                }
                foreach (XmlNode item in xml.SelectNodes(mainCategoriesXpath))
                {                    
                    Settings.Categories.Add(new RssLink() {
                                                Name = item.SelectSingleNode("text").InnerText,
                                                HasSubCategories = true,
                                                Other = item.SelectSingleNode("tag").InnerText
                                            });
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
                
                XmlDocument xml = GetWebData<XmlDocument>(mainCategoriesUrl);
                if (xml != null)
                {
                    foreach (XmlNode item in xml.SelectNodes(string.Format(subcategoriesXpath, parentCategory.Name)))
                    {
                        // type="special" means item has subcategories
                        XmlAttribute type = item.Attributes["type"];
                        XmlNode tag = item.SelectSingleNode("tag");
                        
                        if (type != null && type.Value.Equals("icon"))
                        {
                            // "icon" nodes should be from the same item tree hieararchy
                            if (tag != null && !tag.InnerText.StartsWith(parentCategory.Other as string)) continue;
                        }
                        XmlNode urlLatest = item.SelectSingleNode("urlLatest");
                        
                        RssLink rssLink = new RssLink() {
                            Name = item.SelectSingleNode("text").InnerText,
                            Url = urlLatest != null ? item.SelectSingleNode("urlLatest").InnerText.Trim() : string.Empty,
                            HasSubCategories = type != null && type.Value.Equals("special"),
                            ParentCategory = parentCategory,
                            Other = parentCategory.Other
                        };
                        Log.Debug(@"Subcategory: {0}", rssLink.Name);

                        XmlNode icon = item.SelectSingleNode("icon");
                        if (icon != null)
                        {
                            // replace with larger logo in the thumbnail URL
                            rssLink.Thumb = icon.InnerText.Trim().Replace("24x24", "128x128");
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        parentCategory.SubCategories.Add(rssLink);
                    }
                }
            }
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            RssLink rssLink = (RssLink) category;
            
            if (CURRENT_SPORTS.Equals(rssLink.Other))
            {
                Log.Debug(@"Looking for videos in {0} at {1}", rssLink.Name, rssLink.Url);
                JObject json = GetWebData<JObject>(rssLink.Url);
                JArray playlists = json["playlists"] as JArray;
                if (playlists != null)
                {
                    string datasource = playlists[0].Value<string>("datasource");
                    XmlDocument xml = GetWebData<XmlDocument>(string.Format(feedHandlerUrl, datasource));
                    if (xml != null)
                    {
                        //<playlist>
                        //  <video>
                        //    <title>Australian Open: Day 1 Highlights</title>
                        //    <description>Novak Djokovic, Maria Sharapova and Venus Williams all advanced easily into the second round.</description>
                        //    <image>http://images.ctvdigital.com/images/pub2upload/9/2013_1_14/jokovic_011313.jpg</image>
                        //    <id>842888</id>
                        //    <bin>16175</bin>
                        //    <date>1/13/2013 8:55:40 PM</date>
                        //    <isPkg>False</isPkg>
                        //  </video>
                        //</playlist>
                        foreach (XmlNode video in xml.SelectNodes(@"//video"))
                        {
                            result.Add(new VideoInfo() {
                                           Title = video.SelectSingleNode(@"title").InnerText,
                                           Description = video.SelectSingleNode(@"description").InnerText,
                                           ImageUrl = video.SelectSingleNode(@"image").InnerText,
                                           VideoUrl = string.Format(@"{0}|{1}", "26", video.SelectSingleNode(@"id").InnerText),
                                           Airdate = video.SelectSingleNode(@"date").InnerText
                                       });
                        }
                    }
                }                
            }
            else
            {
                result = getVideoListForSinglePage(category, string.Format(@"{0}&pageSize={1}", rssLink.Url, PAGE_SIZE_FOR_NON_CURRENT_SPORTS));
            }
            return result;
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            Log.Debug(@"Looking for videos in {0} at {1}", category.Name, url);
            List<VideoInfo> result = new List<VideoInfo>();

            nextPageUrl = string.Empty;
            currentCategory = category;
            
            XmlDocument xml = GetWebData<XmlDocument>(url);
            
            if (xml != null)
            {
                foreach (XmlNode item in xml.SelectNodes(@"//rss/channel/item"))
                {
                    result.Add(new VideoInfo() {
                                   Title = item.SelectSingleNode(@"title").InnerText,
                                   Description = item.SelectSingleNode(@"description").InnerText,
                                   ImageUrl = item.SelectSingleNode(@"imgUrl").InnerText,
                                   VideoUrl = string.Format(@"{0}|{1}", "27", item.SelectSingleNode(@"id").InnerText),
                               });
                }

                XmlNode totalCountNode = xml.SelectSingleNode(@"//rss/channel/totalCount");
                int totalCount = int.Parse(totalCountNode.InnerText);
                XmlNode pageNumNode = xml.SelectSingleNode(@"//rss/channel/pageNum");
                XmlNode pageNode = xml.SelectSingleNode(@"//rss/channel/page");
                int pageNum = int.Parse(pageNumNode != null ? pageNumNode.InnerText : pageNode.InnerText);

                if (pageNum * PAGE_SIZE_FOR_NON_CURRENT_SPORTS < totalCount)    // is there a next page?
                {
                    NameValueCollection parameters = HttpUtility.ParseQueryString(new Uri(url).Query);
                    parameters.Add("pageNum", Convert.ToString(pageNum + 1));
                    Uri nextPageUri = new UriBuilder(url) { Query = parameters.ToString() }.Uri;
                    Log.Debug("Next Page URL: {0}", nextPageUri);
                    nextPageUrl = nextPageUri.ToString();
                }
            }
            return result;
        }
        
        public override bool HasNextPage {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }
        
        public override List<VideoInfo> getNextPageVideos()
        {
            return getVideoListForSinglePage(currentCategory, nextPageUrl);
        }

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            string[] parts = video.VideoUrl.Split('|');
            string webData = GetWebData(string.Format(urlgenFormat, parts[0], parts[1]));
            
            if (!string.IsNullOrEmpty(webData))
            {
                Match urlMatch = clipUrlRegex.Match(webData);
                if (urlMatch.Success)
                {
                    string rtmpFromScraper = urlMatch.Groups["url"].Value;

                    Log.Debug("RTMP URL found: {0}", rtmpFromScraper);

                    Match m = rtmpUrlRegex.Match(rtmpFromScraper);
                    if (m.Success)
                    {
                        string rtmpUrl = String.Format(@"rtmpe://{0}/ondemand?{1}", m.Groups["host"], m.Groups["params"]);
                        string playPath = String.Format(@"mp4:{0}", m.Groups["file"]);
                        result = new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString();
                        Log.Debug(@"RTMP URL (after): {0}", result);
                    }
                    else if (rtmpFromScraper.Contains("manifest.f4m"))
                    {
                        result = new MPUrlSourceFilter.HttpUrl(string.Format("{0}?hdcore=2.11.3", rtmpFromScraper)).ToString();
                        Log.Debug("Manifest URL found: ", result);
                    }
                }
            }

            return result;
        }
        
        private void MarkCategoryAsCurrentSports(Category category)
        {
            if (category.HasSubCategories)
            {
                category.SubCategories.ToList().ForEach(subCategory => MarkCategoryAsCurrentSports(subCategory));
            }
            else
            {
                category.Other = CURRENT_SPORTS;
            }
        }
    }
}
