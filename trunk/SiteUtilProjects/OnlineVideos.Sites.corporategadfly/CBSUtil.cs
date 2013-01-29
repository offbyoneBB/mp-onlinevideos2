using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site utility for CBS.
    /// </summary>
    public class CBSUtil : GenericSiteUtil
    {
        private static Regex carouselRegex = new Regex(@"loadUpCarousel\('(?<title>[^,]*)','(?<section>[^,]*)',\s'(?<hash>[^,]*)',\s(?<showId>[^,]*),\strue,\sstored",
                                                       RegexOptions.Compiled);
        private static Regex pidRegex = new Regex(@"video\.settings\.pid\s=\s'(?<pid>[^']*)';",
                                                  RegexOptions.Compiled);

        private static string CAROUSEL = @"carousel";
        private static string thePlatformUrlFormat = @"http://link.theplatform.com/s/dJ5BDC/{0}?format=SMIL&Tracking=true&mbr=true";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            HtmlDocument document = GetWebData<HtmlDocument>(string.Format(@"{0}/video/", baseUrl));
            if (document != null)
            {
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(@"//div[@id = 'daypart_nav']//a"))
                {
                    string title = anchor.GetAttributeValue("onclick", string.Empty).Replace("showDaypart('", string.Empty).Replace("');", string.Empty);
                    Settings.Categories.Add(new RssLink() {
                                                Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title),
                                                Other = title,
                                                HasSubCategories = true
                                            });
                    Log.Debug(@"Category: {0}", title);
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            if (!CAROUSEL.Equals(parentCategory.Other as string))
            {
                HtmlDocument document = GetWebData<HtmlDocument>(string.Format(@"{0}/video/", baseUrl));
                if (document != null)
                {
                    foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(string.Format(@"//div[@id = '{0}']//a", parentCategory.Other as string)))
                    {
                        string clazz = anchor.GetAttributeValue("class", string.Empty);
                        if ("vidgreen".Equals(clazz)) continue;
                        
                        HtmlNode image = anchor.SelectSingleNode("./img");
                        string name = image.GetAttributeValue("alt", string.Empty);
                        string url = anchor.GetAttributeValue("href", string.Empty);
                        if (!url.StartsWith("http") && url.StartsWith("/")) url = string.Format(@"{0}{1}", baseUrl, url);
                        string thumb = string.Format("{0}{1}", baseUrl, image.GetAttributeValue("src", string.Empty));
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             ParentCategory = parentCategory,
                                                             Name = HttpUtility.HtmlDecode(name),
                                                             Url = url,
                                                             Thumb = thumb,
                                                             Other = CAROUSEL,
                                                             HasSubCategories = true
                                                         });
                    }
                }
            }
            else
            {
                Log.Debug(@"Trying new carousel");
                string webData = GetWebData((parentCategory as RssLink).Url);
                
                if (!string.IsNullOrEmpty(webData))
                {
                    foreach (Match m in carouselRegex.Matches(webData))
                    {
                        // URL is formatted as /carousels/#showId#/video/#section#/#hash#/#begin#/#size#/
                        parentCategory.SubCategories.Add(new RssLink() {
                                                             Url = string.Format(@"{0}/carousels/{1}/video/{2}/{3}/0/400",
                                                                                 baseUrl,
                                                                                 m.Groups["showId"].Value,
                                                                                 m.Groups["section"].Value,
                                                                                 m.Groups["hash"].Value),
                                                             Name = Regex.Unescape(m.Groups["title"].Value),
                                                             HasSubCategories = false
                                                         });
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            JObject json = GetWebData<JObject>((category as RssLink).Url);
            if (json != null)
            {
                foreach (JToken item in json["itemList"] as JArray)
                {
                    long epochSeconds = item.Value<long>("airDate") / 1000;
                    result.Add(new VideoInfo() {
                                   Title = item.Value<string>("title"),
                                   Description = item.Value<string>("description"),
                                   VideoUrl = item.Value<string>("url"),
                                   Length = TimeSpan.FromSeconds(item.Value<int>("duration")).ToString(),
                                   ImageUrl = item.Value<string>("thumbnail"),
                                   // convert epoch (seconds since unix time) to a date string
                                   Airdate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochSeconds).ToShortDateString()
                               });
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

            string pid = string.Empty;
            
            // must find pid before proceeding
            if (video.VideoUrl.Contains(@"pid="))
            {
                pid = HttpUtility.ParseQueryString(new Uri(video.VideoUrl).Query)["pid"];

            }
            else
            {
                string data = GetWebData(video.VideoUrl);
                
                Match pidMatch = pidRegex.Match(data);
                if (pidMatch.Success)
                {
                    pid = pidMatch.Groups["pid"].Value;
                }
            }
            
            if (!string.IsNullOrEmpty(pid))
            {
                XmlDocument xml = GetWebData<XmlDocument>(string.Format(thePlatformUrlFormat, pid));
                Log.Debug(@"SMIL loaded from {0}", string.Format(thePlatformUrlFormat, pid));
    
                XmlNamespaceManager nsmRequest = new XmlNamespaceManager(xml.NameTable);
                nsmRequest.AddNamespace("a", @"http://www.w3.org/2005/SMIL21/Language");
    
                XmlNode metaBase = xml.SelectSingleNode(@"//a:meta", nsmRequest);
                // base URL may be stored in the base attribute of <meta> tag
                string url = metaBase != null ? metaBase.Attributes["base"].Value : string.Empty;
    
                foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
                {
                    int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                    // do not bother unless bitrate is non-zero
                    if (bitrate == 0) continue;
    
                    if (url.StartsWith("rtmp") && !urlsDictionary.ContainsKey(bitrate / 1000))
                    {
                        string playPath = node.Attributes["src"].Value;
                        if (playPath.EndsWith(@".mp4") && !playPath.StartsWith(@"mp4:"))
                        {
                            // prepend with mp4:
                            playPath = @"mp4:" + playPath;
                        }
                        else if (playPath.EndsWith(@".flv"))
                        {
                            // strip extension
                            playPath = playPath.Replace(@".flv", string.Empty);
                        }
                        Log.Debug(@"bitrate: {0}, url: {1}, PlayPath: {2}", bitrate / 1000, url, playPath);
                        urlsDictionary.Add(bitrate / 1000, new MPUrlSourceFilter.RtmpUrl(url) { PlayPath = playPath }.ToString());
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
                    XmlNode geolockReference = xml.SelectSingleNode(@"//a:seq/a:ref", nsmRequest);
                    if (geolockReference != null)
                    {
                        Log.Error(@"This content is not available in your location.");
                        result = string.Format(@"{0}{1}",
                                               url,
                                               geolockReference.Attributes["src"].Value);
                    }
                }
            }
            return result;
        }
    }
}
