using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;

namespace OnlineVideos.Sites
{
    enum CBSItemType { Movie, Show, ShowCarousels, Classic, ClassicSeasons, ClassicEpisodes };
    /// <summary>
    /// Site utility for CBS.
    /// </summary>
    public class CBSUtil : GenericSiteUtil
    {
    	[Category("OnlineVideosUserConfiguration"), DefaultValue(false), Description("Whether to download subtitles"), LocalizableDisplayName("Retrieve Subtitles")]
        protected bool retrieveSubtitles;
        private static Regex videoSectionsRegex = new Regex(@"video\.section_ids\s=\s\[(?<videoSections>[^\\]*?)\]",
                                                           RegexOptions.Compiled);
        private static Regex showIdRegex = new Regex(@"var\sshow\s=\snew\sCBS\.Show\({id:(?<showId>[^}]*)}",
                                                     RegexOptions.Compiled);
        private static Regex classicSeasonRegex = new Regex(@"(?<seasonInfo>.*?)<br>",
                                                            RegexOptions.Compiled);
        private static Regex classicLengthRegex = new Regex(@"(Clip|Episode)\s\((?<length>.*?)\)",
                                                            RegexOptions.Compiled);
        private static Regex pidRegex = new Regex(@"video\.settings\.pid\s=\s'(?<pid>[^']*)';",
                                                  RegexOptions.Compiled);

        private static string mainCategoriesUrlFormat = @"http://www.cbs.com/carousels/showsByCategory/{0}/offset/0/limit/100";
        private static string videoSectionUrlFormat = @"http://www.cbs.com/carousels/videosBySection/{0}/offset/0/limit/1/xs/0/";
        private static string videoListUrlFormat = @"http://www.cbs.com/carousels/videosBySection/{0}/offset/0/limit/40/xs/0/";
        private static string thePlatformUrlFormat = @"http://link.theplatform.com/s/dJ5BDC/{0}?format=SMIL&Tracking=true&mbr=true";
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            Settings.Categories.Add(
                new RssLink() {
                    Url = string.Format(mainCategoriesUrlFormat, "0"), HasSubCategories = true, Name = @"Shows", Other = CBSItemType.Show
                });
            Settings.Categories.Add(
                new RssLink() {
                    Url = string.Format(mainCategoriesUrlFormat, "4"), HasSubCategories = true, Name = @"TV Classics", Other = CBSItemType.Classic
                });
            Settings.Categories.Add(
                new RssLink() {
                    Url = string.Format(mainCategoriesUrlFormat, "6"), HasSubCategories = true, Name = @"Movies & Specials", Other = CBSItemType.Movie
                });
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            RssLink parentRssLink = parentCategory as RssLink;
            
            CBSItemType itemType = (CBSItemType) parentCategory.Other;
            
            if (itemType.Equals(CBSItemType.Show)
                || itemType.Equals(CBSItemType.Movie)
                || itemType.Equals(CBSItemType.Classic))
            {
                JObject json = GetWebData<JObject>(parentRssLink.Url);
                if (json != null)
                {
                    JArray items = (JArray) json["result"]["data"];
                    foreach (JToken item in items) {
                        parentCategory.SubCategories.Add(
                            new RssLink() {
                                ParentCategory = parentCategory,
                                Name = (string) item["title"],
                                Url =
                                    itemType.Equals(CBSItemType.Classic)
                                    ? (string) item["link"]
                                    : string.Format(@"{0}video", (string) item["link"]),
                                Thumb = (string) item["filepath_nav_logo"],
                                HasSubCategories = true,
                                Other =
                                    itemType.Equals(CBSItemType.Show) || itemType.Equals(CBSItemType.Movie)
                                    ? CBSItemType.ShowCarousels
                                    : CBSItemType.ClassicSeasons
                            });
                    }
                }
            }
            else if (itemType.Equals(CBSItemType.ClassicSeasons))
            {
                HtmlDocument document = GetWebData<HtmlDocument>(parentRssLink.Url);
                if (document != null) {
                    foreach (HtmlNode season in document.DocumentNode.SelectNodes(@"//div[@id = 'season-nav-wrapper']//div[@class = 'season-nav-text']"))
                    {
                        string seasonText = season.InnerText;
                        if (@"Full Episodes".Equals(seasonText) || @"Clips".Equals(seasonText))
                        {
                            continue;
                        }
                        parentCategory.SubCategories.Add(
                            new RssLink() {
                                ParentCategory = parentCategory,
                                Name = seasonText,
                                Url = string.Format(@"{0}filter/?season={1}&start=0", parentRssLink.Url, seasonText.Replace(@"Season ", string.Empty)),
                                HasSubCategories = false,
                                Other = CBSItemType.ClassicEpisodes
                            });
                    }
                }
            }
            else if (itemType.Equals(CBSItemType.ShowCarousels))
            {
                string webData = GetWebData(parentRssLink.Url);
                if (!string.IsNullOrEmpty(webData))
                {
                    Match videoSectionsMatch = videoSectionsRegex.Match(webData);
                    if (videoSectionsMatch.Success)
                    {
                        // retrieve info for each section
                        foreach (string sectionId in videoSectionsMatch.Groups["videoSections"].Value.Split(',').ToList())
                        {
                            JObject json = GetWebData<JObject>(string.Format(videoSectionUrlFormat, sectionId));
                            if (json != null)
                            {
                                parentCategory.SubCategories.Add(
                                    new RssLink() {
                                        ParentCategory = parentCategory,
                                        Name = (string) json["result"]["title"],
                                        Url = string.Format(videoListUrlFormat, sectionId),
                                        HasSubCategories = false
                                    });
                            }
                        }
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListForSinglePage(category, (category as RssLink).Url);
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            currentCategory = category;
            nextPageUrl = string.Empty;

            if (category.Other != null && CBSItemType.ClassicEpisodes.Equals((CBSItemType) category.Other))
            {
                string webData = GetWebDataFromPost(url, string.Empty);
                if (!string.IsNullOrEmpty(webData))
                {
                    JObject json = JObject.Parse(webData);
                    if (json != null)
                    {
                        HtmlDocument document = new HtmlDocument();
                        document.LoadHtml((string) json["html"]);
                        
                        foreach (HtmlNode div in document.DocumentNode.SelectNodes(@"//div[@class = 'video-content-item']"))
                        {
                            HtmlNode title = div.SelectSingleNode(@"./div[@class = 'video-content-title']");
                            HtmlNode info = div.SelectSingleNode(@".//div[@class = 'video-content-info']");
                            HtmlNode anchor = div.SelectSingleNode(@".//a[img]");
                            
                            string seasonInfo = string.Empty;
                            Match seasonInfoMatch = classicSeasonRegex.Match(
                                info.SelectSingleNode(@"./div[@class = 'video-content-season-info']").InnerHtml);
                            if (seasonInfoMatch.Success)
                            {
                                seasonInfo = seasonInfoMatch.Groups["seasonInfo"].Value;
                            }
                            
                            string length = string.Empty;
                            Match lengthMatch = classicLengthRegex.Match(
                                info.SelectSingleNode(@"./div[@class = 'video-content-duration']").InnerText);
                            if (lengthMatch.Success)
                            {
                                length = lengthMatch.Groups["length"].Value;
                            }

                            result.Add(
                                new VideoInfo() {
                                    Title = title.InnerText,
                                    VideoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                    ImageUrl = anchor.Element("img").GetAttributeValue(@"src", string.Empty),
                                    Airdate = info.SelectSingleNode(@"./div[@class = 'video-content-air-date']").InnerText,
                                    Description =
                                        string.Format(@"{0}: {1}",
                                                      seasonInfo,
                                                      info.SelectSingleNode(@"./div[@class = 'video-content-description']").InnerText),
                                    Length = length
                                });
                        }
                        
                        if ((bool) json["page"]["more"])
                        {
                            UriBuilder builder = new UriBuilder(url);
                            NameValueCollection parameters = HttpUtility.ParseQueryString(builder.Query);
                            parameters["start"] = Convert.ToString(json["page"]["next"]);
                            builder.Query = parameters.ToString();
                            nextPageUrl = builder.Uri.ToString();
                        }
                    }
                }
            }
            else
            {
                JObject json = GetWebData<JObject>(url);
                if (json != null)
                {
                    foreach (JToken item in json["result"]["data"] as JArray)
                    {
                        string description = string.Empty;
                        if (!string.IsNullOrEmpty((string) item["season_number"]))
                        {
                            description = string.Format(@"Season {0}", item["season_number"]);
                        }
                        if (!string.IsNullOrEmpty((string) item["episode_number"]))
                        {
                            description = string.Format(@"{0}, Episode {1}", description, item["episode_number"]);
                        }
                        result.Add(new VideoInfo() {
                                       Title = (string) item["title"],
                                       Description =
                                           string.IsNullOrEmpty(description) ?
                                           (string) item["description"] :
                                           string.Format(@"{0}: {1}", description, item["description"]),
                                       Airdate = (string) item["airdate"],
                                       Length = (string) item["duration"],
                                       VideoUrl = string.Format(@"{0}{1}", baseUrl, item["url"]),
                                       ImageUrl = (string) item["thumb"]["large"]
                                   });
                    }
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
                
                // closed captions
                string subtitleText = string.Empty;
                if (retrieveSubtitles) {
	                XmlNode ccUrl = xml.SelectSingleNode("//a:body/a:switch/a:ref/a:param[@name='ClosedCaptionURL']", nsmRequest);
	                if (ccUrl != null) {
	                	subtitleText = SubtitleReader.TimedText2SRT(GetWebData(ccUrl.Attributes["value"].Value));
	                }
                }
    
                // sort the URLs ascending by bitrate
                foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    if (!string.IsNullOrEmpty(subtitleText)) {
                    	video.SubtitleText = subtitleText;
                    }
                    // return last URL as the default (will be the highest bitrate)
                    result = item.Value;
                }
                
                // if result is still empty then perhaps we are geo-locked
                if (string.IsNullOrEmpty(result))
                {
                    XmlNode geolockReference = xml.SelectSingleNode(@"//a:seq/a:ref", nsmRequest);
                    if (geolockReference != null)
                    {
                        string message = geolockReference.Attributes["abstract"] != null ?
                            geolockReference.Attributes["abstract"].Value :
                            @"This content is not available in your location.";
                        Log.Error(message);
                        throw new OnlineVideosException(message, true);
                    }
                }
            }
            return result;
        }
    }
}
