using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site utility for Much Music.
    /// </summary>
    public class MuchMusicUtil : GenericSiteUtil
    {
        // look for <dt> tag with exact text and then find all following sibiling <dd> tags and <a> inside them
        private static string mainCategoriesXpathFormat = @"//dt[./text() = '{0}']/following-sibling::dd/a";
        private static string datafeedUrlFormat = @"http://esi.ctv.ca/datafeed/content_much.aspx?cid={0}";
        private static string urlgenUrlFormat = @"http://esi.ctv.ca/datafeed/flv/urlgenjsext.aspx?formatid=27&vid={0}&timeZone=%2D4";
        
        private static Regex videoIdRegex = new Regex(@"mpFlashVars\.id\s=\s(?<videoId>[^;]*);",
                                                      RegexOptions.Compiled);
        private static Regex manifestRegex = new Regex(@"Video\.Load\((?<json>[^\)]*)\)",
                                                       RegexOptions.Compiled);
        //                          skip following shows
        // MuchMusic Countdown
        // New.Music.Live.
        // New.Music.Live. Performances
        // Perez Hilton All Access
        // RapCity Interviews
        // The Wedge Interviews
        // When I Was 17
        // >> All Much Shows
        private static Regex skippedShowsRegex = new Regex(@"(MuchMusic Countdown|New.Music.Live.|New.Music.Live. Performances|Perez Hilton All Access|RapCity Interviews|The Wedge Interviews|When I Was 17|&gt;&gt; All Much Shows)",
                                                           RegexOptions.Compiled);
        
        private static int NUM_EPISODES_PER_AJAX_REQUEST = 9;
        
        private enum MuchMusic { None, Main, EpisodesWithAjax, EpisodesInDottedCarousel, EpisodesInFlatCarousel, EpisodesPunkd };
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Much Shows", HasSubCategories = true, Other = MuchMusic.Main });
//            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Specials", HasSubCategories = true, Other = MuchMusic.Main });
//            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Music On Much", HasSubCategories = true, Other = MuchMusic.Main });
//            Settings.Categories.Add(new RssLink() {Url = baseUrl, Name = @"Music Video Playlists", HasSubCategories = true, Other = MuchMusic.Main });
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            string url = (parentCategory as RssLink).Url;
            
            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                switch ((MuchMusic) parentCategory.Other)
                {
                    case MuchMusic.Main:
                        {
                            // main subcategories
                            string xPath = string.Format(mainCategoriesXpathFormat, parentCategory.Name);
                            HtmlNodeCollection anchors = document.DocumentNode.SelectNodes(xPath);
                            if (anchors != null)
                            {
                                foreach (HtmlNode anchor in anchors)
                                {
                                    // skip some shows
                                    if (skippedShowsRegex.Match(anchor.InnerText).Success) continue;

                                    parentCategory.SubCategories.Add(new RssLink() {
                                                                         ParentCategory = parentCategory,
                                                                         Name = HttpUtility.HtmlDecode(anchor.InnerText),
                                                                         Url = anchor.GetAttributeValue(@"href", string.Empty),
                                                                         HasSubCategories = true,
                                                                         Other = MuchMusic.None
                                                                     });
                                }
                            }
                            break;
                        }

                    case MuchMusic.None:
                        {
                            HtmlNode showLogo = document.DocumentNode.SelectSingleNode(@"//div[@id = 'ShowInfo']/div[@id = 'ShowLogo']");
                            if (showLogo == null)
                            {
                                MuchMusic subcategoryType = MuchMusic.None;
    
                                HtmlNode subnav = document.DocumentNode.SelectSingleNode(@"//li[@id = 'episodesubnav']/a");
    
                                if (subnav != null && !url.EndsWith(@"/episodes/"))
                                {
                                    // billyonthestreet
                                    url = string.Format(@"{0}{1}", baseUrl, subnav.GetAttributeValue(@"href", string.Empty));
                                    // follow subnavigation link
                                    document = GetWebData<HtmlDocument>(url);
                                }
    
                                HtmlNode episodesAjax = document.DocumentNode.SelectSingleNode(@"//div[@id = 'EpisodesAjax']");
                                HtmlNode showInfo = document.DocumentNode.SelectSingleNode(@"//div[@id = 'SecondaryContent']/div[@id = 'ShowInfo']");
                                HtmlNode nextVideo = document.DocumentNode.SelectSingleNode(@"//div[@id = 'Episodes']/a[@id = 'NextVideo']");
                                
                                if (nextVideo != null)
                                {
                                    // mydatewith
                                    subcategoryType = MuchMusic.EpisodesInFlatCarousel;
                                }
                                else if (episodesAjax != null || showInfo != null)
                                {
                                    subcategoryType = MuchMusic.EpisodesWithAjax;
                                    if (url.EndsWith(@"/episodes/"))
                                    {
                                        url = url.Replace(@"/episodes/", @"/ajax/loadepisodes.aspx?videoindex=0");
                                    }
                                }
                                else
                                {
                                    // degrassi
                                    subcategoryType = MuchMusic.EpisodesInDottedCarousel;
                                }
    
                                parentCategory.SubCategories.Add(new RssLink() {
                                                                     ParentCategory = parentCategory,
                                                                     Name = @"Episodes",
                                                                     Url = url,
                                                                     Other = subcategoryType,
                                                                     HasSubCategories = false
                                                                 });
                            }
                            else
                            {
                                // punkd
                                HtmlNodeCollection items = document.DocumentNode.SelectNodes(@"//div[@id = 'MainFeed']/ul/li[a]");
                                if (items != null)
                                {
                                    foreach (HtmlNode item in items)
                                    {
                                        HtmlNode anchor = item.SelectSingleNode(@"./a");
                                        HtmlNode titleNode = item.SelectSingleNode(@"./dl/dt");
                                        HtmlNode descriptionNode = item.SelectSingleNode(@"./dl/dd");
                                        parentCategory.SubCategories.Add(new RssLink() {
                                                                             ParentCategory = parentCategory,
                                                                             Name = titleNode.InnerText,
                                                                             Description = descriptionNode.InnerText,
                                                                             Url = string.Format(@"{0}{1}", url, anchor.GetAttributeValue(@"href", string.Empty)),
                                                                             Other = MuchMusic.EpisodesPunkd,
                                                                             HasSubCategories = false
                                                                         });
                                    }
                                }
                            }
                            break;
                        }

                    default:
                        throw new NotImplementedException(string.Format(@"Much Music Type: {0} has not been implemented", (MuchMusic) parentCategory.Other));
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            return getVideoListForSinglePage(category, (category as RssLink).Url);
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = string.Empty;
            currentCategory = category;
            MuchMusic type = category.Other != null ? (MuchMusic) category.Other : MuchMusic.None;

            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                string xpath = string.Empty;
                switch (type)
                {
                    case MuchMusic.EpisodesPunkd:
                        xpath = @"//ul/li[dl[dd[@class = 'Description']]]";
                        break;
                    case MuchMusic.EpisodesInFlatCarousel:
                        xpath = @"//div[@id = 'EpisodesAjax']//ul/li[a]";
                        break;
                    case MuchMusic.EpisodesInDottedCarousel:
                        xpath = @"//div[@id = 'EpisodesInner']//ul/li[a]";
                        break;
                    default:
                        xpath = @"//ul/li[a]";
                        break;
                }
                HtmlNodeCollection items = document.DocumentNode.SelectNodes(xpath);
                
                if (items != null)
                {
                    foreach (HtmlNode item in items)
                    {
                        HtmlNode titleNode, descriptionNode;
                        HtmlNode img = item.SelectSingleNode(@"./img");
                        HtmlNode anchor = item.SelectSingleNode(@"./a");
                        
                        switch (type)
                        {
                            case MuchMusic.EpisodesPunkd:
                                img = item.SelectSingleNode(@"./a/img");
                                titleNode = item.SelectSingleNode(@"./dl/dt/a");
                                descriptionNode = item.SelectSingleNode(@"./dl/dd[@class = 'Description']");
                                break;
                            case MuchMusic.EpisodesInDottedCarousel:
                                titleNode = item.SelectSingleNode(@"./dl/dt");
                                descriptionNode = item.SelectSingleNode(@"./dl/dd");
                                break;
                                
                            case MuchMusic.EpisodesInFlatCarousel:
                                titleNode = anchor.SelectSingleNode(@"./div/p");
                                descriptionNode = null;
                                break;
                                
                            default:
                                if (img != null)
                                {
                                    titleNode = item.SelectSingleNode(@"./dl/dt");
                                    descriptionNode = item.SelectSingleNode(@"./dl/dd");
                                }
                                else
                                {
                                    titleNode = anchor.SelectSingleNode(@"./dl/dt");
                                    descriptionNode = anchor.SelectSingleNode(@"./dl/dd");
                                    img = anchor.SelectSingleNode(@"(./img)[2]");
                                }
                                break;
                        }
                        
                        string imageUrl = img.GetAttributeValue(@"src", string.Empty);
                        if (imageUrl.Contains("imgurl="))
                        {
                            string[] urlParts = imageUrl.Split(new string[] { "imgurl=" }, StringSplitOptions.None);
                            imageUrl = urlParts[1];
                        }
                        string title = titleNode == null ? item.SelectSingleNode(@"./a[@class = 'moreepstitle']").InnerText : titleNode.InnerText;
                        string description = descriptionNode == null ? string.Empty : descriptionNode.InnerText;
                        string videoUrl;
                        if (type.Equals(MuchMusic.EpisodesPunkd))
                        {
                            string href = anchor.GetAttributeValue(@"href", string.Empty);
                            videoUrl = new Uri(new Uri(url), href).AbsoluteUri;
                        }
                        else
                        {
                            videoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty));
                        }
                        result.Add(new VideoInfo() {
                                       VideoUrl = videoUrl,
                                       Title = title,
                                       Description = description,
                                       ImageUrl = imageUrl
                                   });
                    }
                }
                
                double listItemCount = (double) document.CreateNavigator().Evaluate(@"count(//ul/li)");
                if (type != MuchMusic.EpisodesInDottedCarousel &&
                    type != MuchMusic.EpisodesPunkd &&
                    listItemCount == (NUM_EPISODES_PER_AJAX_REQUEST + 1))
                {
                    UriBuilder builder = new UriBuilder(url);
                    NameValueCollection parameters = HttpUtility.ParseQueryString(builder.Query);
                    int nextPageNumber = int.Parse(parameters["videoindex"]) + NUM_EPISODES_PER_AJAX_REQUEST;
                    parameters["videoindex"] = Convert.ToString(nextPageNumber);
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
        
        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist)
        {
            List<string> result = new List<string>();
            // find videoId
            string data = GetWebData(video.VideoUrl);
            Match videoIdMatch = videoIdRegex.Match(data);
            if (videoIdMatch.Success)
            {
                XmlDocument xml = GetWebData<XmlDocument>(string.Format(datafeedUrlFormat, videoIdMatch.Groups["videoId"].Value));
                if (xml != null)
                {
                    XmlNodeList elements = xml.SelectNodes(@"//playlist/element");
                    if (elements != null)
                    {
                        foreach (XmlNode element in elements)
                        {
                            XmlNode clipId = element.SelectSingleNode(@"./videoid");
                            result.Add(string.Format(urlgenUrlFormat, clipId.InnerText));
                        }
                    }
                }
            }
            else
            {
                Log.Warn("Video ID not found for {0}", video.VideoUrl);
            }
            return result;
        }
        
        public override string GetPlaylistItemVideoUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist)
        {
            string result = string.Empty;
            string webData = GetWebData(clonedVideoInfo.VideoUrl);
            Log.Debug(@"urlgen output: {0}", webData);
            if (!string.IsNullOrEmpty(webData))
            {
                Match manifestMatch = manifestRegex.Match(webData);
                JToken json = JToken.Parse(manifestMatch.Groups["json"].Value);
                string url = json.Value<string>("url");
                if (url.EndsWith(@".smil"))
                {

                    clonedVideoInfo.PlaybackOptions = new Dictionary<string, string>();
                    // keep track of bitrates and URLs
                    Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

                    // process SMIL directly and set playback options
                    XmlDocument xml = GetWebData<XmlDocument>(url);
                    Log.Debug(@"SMIL loaded from {0}", url);
        
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                    namespaceManager.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");
                    XmlNode httpBase = xml.SelectSingleNode(@"//a:meta[@name ='httpBase']", namespaceManager);
                    // base URL may be stored in the base attribute of <meta> tag
                    string prefix = httpBase != null ? httpBase.Attributes["content"].Value : string.Empty;

                    foreach (XmlNode node in xml.SelectNodes("//a:body/a:switch/a:video", namespaceManager))
                    {
                        int bitrate = int.Parse(node.Attributes["system-bitrate"].Value);
                        // do not bother unless bitrate is non-zero
                        if (bitrate == 0) continue;
                        
                        urlsDictionary.Add(bitrate / 1000, new MPUrlSourceFilter.HttpUrl(string.Format(@"{0}{1}", prefix, node.Attributes["src"].Value)).ToString());
                    }

                    // sort the URLs ascending by bitrate
                    foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                    {
                        clonedVideoInfo.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                        // return last URL as the default (will be the highest bitrate)
                        result = item.Value;
                    }
                }
                else
                {
                    result = string.Format(@"{0}&hdcore=2.10.3", url);
                }
                Log.Debug(@"Manifest URL: {0}", result);
            }
            return result;
        }
    }
}
