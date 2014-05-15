using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class CTVUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Short code for site utility")]
        protected string siteCode = @"ctv";

        private static string mediaBaseUrl              = @"http://capi.9c9media.com/destinations";
        private static string mainCategoriesUrlFormat   = mediaBaseUrl + @"/{0}_web/platforms/desktop/collections/67/medias?$sort=name&$include=[id,images,name]&$page=1&$top=1000";
        private static string seasonsUrlFormat          = mediaBaseUrl + @"/{0}_web/platforms/desktop/medias/{1}/seasons?$sort=name";
        private static string videoListUrlFormat        = mediaBaseUrl + @"/{0}_web/platforms/desktop/medias/{1}/seasons/{2}/contents?$sort=BroadcastDate&$order=desc&$include=[broadcastdate,contentpackages,desc,id,images,name]&$page=1&$top=1000";
        private static string stacksUrlFormat           = mediaBaseUrl + @"/{0}_web/platforms/desktop/contents/{1}/contentpackages/{2}/stacks";
        private static string manifestUrlFormat         = @"{0}/{1}/manifest.f4m";

        private static Regex jsonPayloadRegex = new Regex(@"define\((?<json>.*)\);",
                                                          RegexOptions.Compiled | RegexOptions.Singleline);
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(string.Format(mainCategoriesUrlFormat, siteCode));
            if (!string.IsNullOrEmpty(webData))
            {
                JObject json = JObject.Parse(webData);
                JArray items = (JArray) json["Items"];
                foreach (JToken item in items)
                {
                    Settings.Categories.Add(
                        new RssLink() {
                            Name = (string) item["Name"],
                            Url = string.Format(seasonsUrlFormat, siteCode, (int) item["Id"]),
                            Thumb = (string) item["Images"][0]["Url"],
                            Other = (int) item["Id"],
                            HasSubCategories = true
                        });
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink) parentCategory;
            JObject json = GetWebData<JObject>(parentRssLink.Url);

            if (json != null)
            {
                JArray seasons = (JArray) json["Items"];
                foreach (JToken season in seasons)
                {
                    parentCategory.SubCategories.Add(
                        new RssLink() {
                            ParentCategory = parentCategory,
                            Name = (string) season["Name"],
                            Url = string.Format(videoListUrlFormat, siteCode, parentCategory.Other, (int) season["Id"]),
                            HasSubCategories = false
                        });
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url = (string) ((RssLink) category).Url;
            JObject json = GetWebData<JObject>(url);

            if (json != null)
            {
                JArray videos = (JArray) json["Items"];
                foreach (JToken video in videos)
                {
                    result.Add(
                        new VideoInfo() {
                            Title = (string) video["Name"],
                            Description = (string) video["Desc"],
                            VideoUrl = string.Format(stacksUrlFormat, siteCode, (int) video["Id"], (int) video["ContentPackages"][0]["Id"]),
                            // convert seconds to timespan
                            Length = new DateTime(TimeSpan.FromSeconds((double) video["ContentPackages"][0]["Duration"]).Ticks).ToString("HH:mm:ss"),
                            Airdate = (string) video["BroadcastDate"],
                            ImageUrl = (string) video["Images"][0]["Url"]
                        });
                }
            }

            return result;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            
            JObject json = GetWebData<JObject>(video.VideoUrl);
            if (json != null)
            {
                JArray items = (JArray) json["Items"];
                int manifestId = (int) items[0]["Id"];
                string manifestUrl = string.Format(manifestUrlFormat, video.VideoUrl, manifestId);

                video.PlaybackOptions = new Dictionary<string, string>();
                // keep track of bitrates and URLs
                Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

                HttpWebRequest request = WebRequest.Create(manifestUrl) as HttpWebRequest;
                try {
                    HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var errorResponse = (HttpWebResponse) ex.Response;
                        if (errorResponse != null && errorResponse.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            string message = string.Format(
                                @"Not authorized to view '{0}' (plugin does not support subscription-based content at this time)", video.Title);
                            Log.Error(message);
                            throw new OnlineVideosException(message, true);
                        }
                    }
                }
                
                XmlDocument xml = GetWebData<XmlDocument>(manifestUrl);                
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xml.NameTable);
                namespaceManager.AddNamespace("a", @"http://ns.adobe.com/f4m/2.0");
    
                foreach (XmlNode node in xml.SelectNodes("//a:media", namespaceManager))
                {
                    int bitrate = int.Parse(node.Attributes["bitrate"].Value);
                    
                    // do not bother unless bitrate is non-zero
                    if (bitrate == 0) continue;
                    
                    if (!urlsDictionary.ContainsKey(bitrate))
                    {
                        urlsDictionary.Add(bitrate,
                                           new MPUrlSourceFilter.HttpUrl(node.Attributes["href"].Value) {
                                               UserAgent = @"Mozilla/5.0 (Windows NT 5.1; rv:17.0) Gecko/20100101 Firefox/17.0"
                                           }.ToString()
                                          );
                    }                    
                }

                // sort the URLs ascending by bitrate
                foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    // return last URL as the default (will be the highest bitrate)
                    result = item.Value;
                }
            }
            
            return result;
        }
    }
}
