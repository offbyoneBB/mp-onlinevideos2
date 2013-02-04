using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// site util for V Télé.
    /// </summary>
    public class VTeleUtil : GenericSiteUtil
    {
        private static string dispatchUrl = @"http://vtele.ca/videos/includes/librairie/dispatch.inc.php";
        private static string brightCoveUrlFormat = @"http://api.brightcove.com/services/library?command=find_video_by_id&token=2sgr1KCsKKJXcqUFQdti_mXZAhdNB-wCFwCbGW6lz5atwI1QTrElxQ..&media_delivery=http&video_id={0}";
        private static string LIVE_STREAMING = @"En Direct";
        private static string liveEpisodeUrl = @"http://vtele.ca/en-direct/includes/retreiveEmiV3.inc.php";

        private static Regex contentIdRegex = new Regex(@"var\sidBC\s=\s(?<contentId>[^;]*);",
                                                        RegexOptions.Compiled);
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            string url = string.Format(@"{0}/videos", baseUrl);
            HtmlDocument document = GetWebData<HtmlDocument>(url);
            
            if (document != null)
            {
                bool skipFirst = true;
                Dictionary<string, RssLink> categories = new Dictionary<string, RssLink>();
                foreach (HtmlNode list in document.DocumentNode.SelectNodes(@"//div[@class = 'span3']/ul[@class = 'emissionListe']"))
                {
                    if (skipFirst)
                    {
                        skipFirst = false;
                        continue;
                    }
                    else
                    {
                        foreach (HtmlNode anchor in list.SelectNodes(@"./li/a")) {
                            string name = HttpUtility.HtmlDecode(anchor.InnerText);
                            categories.Add(name, new RssLink() {
                                                   Name = name,
                                                   Url = retrieveDispatchUrl(anchor.GetAttributeValue(@"data-lib", string.Empty)),
                                                   HasSubCategories = true
                                               });
                        }
                    }
                }
                
                // Add live category
                Settings.Categories.Add(new RssLink() {
                                            Name = LIVE_STREAMING,
                                            HasSubCategories = false
                                        });
                foreach (var item in categories.OrderBy(cat => cat.Key))
                {
                    Settings.Categories.Add(item.Value);
                }
            }
                        
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
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(@"//nav[@class = 'subSubMenu']/a"))
                {
                    string name = HttpUtility.HtmlDecode(anchor.InnerText);
                    
                    if (@"Exclusivité web".Equals(name) || @"Site de l'émission".Equals(name)) continue;

                    parentCategory.SubCategories.Add(new RssLink() {
                                                         Name = name,
                                                         Url = retrieveDispatchUrl(anchor.GetAttributeValue(@"data-lib", string.Empty)),
                                                         HasSubCategories = false
                                                     });
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

            if (LIVE_STREAMING.Equals(category.Name))
            {
                JObject json = GetWebData<JObject>(liveEpisodeUrl);
                string name = json.Value<string>("emiCourNom");
                string image = string.Format(@"http://image-v.com/tele/en-direct/large/{0}", json.Value<string>("imgSuiv"));

                if (!string.IsNullOrEmpty(name))
                {
                    result.Add(new VideoInfo() {
                                   Title = @"En direct actuellement",
                                   Description = name,
                                   ImageUrl = image,
                                   Other = LIVE_STREAMING,
                                   VideoUrl = new MPUrlSourceFilter.RtmpUrl(@"rtmp://cp101680.live.edgefcs.net/live/livev_1@50832") {
                                       SwfUrl = @"http://admin.brightcove.com/viewer/us20121128.1314/federatedVideoUI/BrightcovePlayer.swf",
                                       PageUrl = @"http://vtele.ca/en-direct/",
                                       App = @"live",
                                       SwfVerify = true,
                                       Live = true }.ToString()
                               });
                }
                else
                {
                    result.Add(new VideoInfo() {
                                   Title = @"Rien en direct en ce moment.",
                                   Description = string.Format(@"{0} à {1}", json.Value<string>("nomEmiSuiv"), json.Value<string>("dateSuiv")),
                                   ImageUrl = image,
                                   VideoUrl = string.Empty
                               });
                }
                return result;
            }


            nextPageUrl = string.Empty;
            currentCategory = category;

            HtmlDocument document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                foreach (HtmlNode row in document.DocumentNode.SelectNodes(@"//div[@class = 'row segmentListEl']"))
                {
                    HtmlNode img = row.SelectSingleNode(@".//img");
                    HtmlNode anchor = row.SelectSingleNode(@".//h3/a");
                    HtmlNode airdate = row.SelectSingleNode(@".//div[@class = 'infoSupp']/span");
                    result.Add(new VideoInfo() {
                                   Title = HttpUtility.HtmlDecode(anchor.InnerText),
                                   VideoUrl = anchor.GetAttributeValue(@"href", string.Empty),
                                   ImageUrl = img.GetAttributeValue(@"src", string.Empty),
                                   Airdate = airdate.InnerText.Replace(@"Ajouté le", string.Empty)
                               });
                }
                
                HtmlNode nextPage = document.DocumentNode.SelectSingleNode(@"//span[@class = 'pageInactive flecheNavPage']");
                if (nextPage == null)
                {
                    UriBuilder builder = new UriBuilder(url);
                    NameValueCollection queryParameters = HttpUtility.ParseQueryString(builder.Query);
                    queryParameters["p"] = string.IsNullOrEmpty(queryParameters["p"]) ?
                        "2" :
                        Convert.ToString(int.Parse(queryParameters["p"]) + 1);
                    builder.Query = queryParameters.ToString();
                    nextPageUrl = builder.Uri.ToString();
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
            if (LIVE_STREAMING.Equals(video.Other as string)) return video.VideoUrl;

            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();
           
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match contentIdMatch = contentIdRegex.Match(data);
                if (contentIdMatch.Success)
                {
                    JObject json = GetWebData<JObject>(string.Format(brightCoveUrlFormat, contentIdMatch.Groups["contentId"].Value));
                    foreach (JToken rendition in json["renditions"] as JArray)
                    {
                        int bitrate = rendition.Value<int>("encodingRate");
                        if (!urlsDictionary.ContainsKey(bitrate / 1000))
                        {
                            urlsDictionary.Add(bitrate / 1000, new MPUrlSourceFilter.HttpUrl(rendition.Value<string>("url")).ToString());
                        }
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

        private static string retrieveDispatchUrl(string dataLib)
        {
            string result = string.Empty;
            
            if (!string.IsNullOrEmpty(dataLib))
            {
                UriBuilder builder = new UriBuilder(dispatchUrl);
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(builder.Query);
                string[] dataLibParts = dataLib.Split(',');
                foreach (string param in dataLibParts)
                {
                    string[] paramParts = param.Split(':');
                    queryParameters.Set(paramParts[0], paramParts[1]);
                }
                builder.Query = queryParameters.ToString();
                result = builder.Uri.ToString();
            }
            return result;
        }
    }
}
