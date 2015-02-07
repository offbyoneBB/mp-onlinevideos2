using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site utility for Univision
    /// </summary>
    public class UnivisionUtil : GenericSiteUtil
    {
        private static Regex clipsRegex = new Regex(@"<span\sclass=""left"">Clips</span>",
                                                    RegexOptions.Compiled);
        private static Regex jsonVideoListRegex = new Regex(@"uim\.collection\.loadCallback\((?<json>[^\)]*)\)",
                                                            RegexOptions.Compiled);
        private static Regex jsonUrlListRegex = new Regex(@"anvatoVideoJSONLoaded\s\(\s(?<json>[^\)]*)\s\)",
                                                          RegexOptions.Compiled);
        private static Regex airDateDurationRegex = new Regex(@"(?<airDate>[^\s]*)\s\|\s(?<duration>.*)",
                                                              RegexOptions.Compiled);
        private static Regex videoIdRegex = new Regex(@"(var\svideoCID\s=\s'(?<videoId>[^,]*|player=video_id=(?<videoId>[^,]*),)')",
                                                      RegexOptions.Compiled);
        private static string rtmpUrlListFormat = @"http://vmscdn-download.s3.amazonaws.com/videos_mcm/{0}.js";
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            string webData = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in regEx_dynamicCategories.Matches(webData))
                {
                    RssLink cat = new RssLink() {
                        Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                        Url = m.Groups["url"].Value,
                        Thumb = m.Groups["thumb"].Value,
                        Description = HttpUtility.HtmlDecode(m.Groups["description"].Value),
                        HasSubCategories = true
                    };

                    Settings.Categories.Add(cat);
                }
            }

            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            string url = ((RssLink) parentCategory).Url;
            string webData = GetWebData(url);
            
            Match clipsMatch = clipsRegex.Match(webData);
            bool hasClips = false;
            
            // clips
            if (clipsMatch.Success)
            {
                hasClips = true;
                parentCategory.SubCategories.Add(
                    new RssLink() {
                        ParentCategory = parentCategory,
                        Name = "Clips",
                        Url = string.Format(@"{0}/collection-1.json", url),
                        HasSubCategories = false
                    }
                );
            }

            // episodes
            parentCategory.SubCategories.Add(
                new RssLink() {
                    ParentCategory = parentCategory,
                    Name = "Episodios",
                    Url = hasClips ? url : string.Format(@"{0}/collection-1.json", url),
                    HasSubCategories = false
                }
            );

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            Dictionary<string, VideoInfo> dictionary = new Dictionary<string, VideoInfo>();

            string url = ((RssLink) category).Url;
            string webData = string.Empty;
            
            if (url.EndsWith(".json"))
            {
                webData = GetWebData(url);
                
                if (!string.IsNullOrEmpty(webData))
                {
                    Match jsonMatch = jsonVideoListRegex.Match(webData);
                    if (jsonMatch.Success)
                    {
                        JObject json = JObject.Parse(jsonMatch.Groups["json"].Value);
                        JArray documents = json.Value<JArray>("documents");
                        
                        if (documents != null)
                        {
                            foreach (JToken document in documents)
                            {
                                string videoUrl = document.Value<string>("url");
                                string[] videoUrlParts = videoUrl.Split('/');
                                string key = string.Format(@"{0}-{1}",
                                                           videoUrlParts[videoUrlParts.Length - 2],
                                                           videoUrlParts[videoUrlParts.Length - 1]);
                                // keep the videos in a dictionary using the last 2 parts of the URL (contains air date)
                                dictionary.Add(key,
                                               new VideoInfo() {
                                                   VideoUrl = videoUrl,
                                                   Thumb = document.Value<string>("img"),
                                                   Length = document.Value<string>("duration"),
                                                   Airdate = document.Value<string>("airDate"),
                                                   Title = document.Value<string>("title"),
                                                   Description = document.Value<string>("description")
                                               });
                            }

                            // sort the videos by key in descending order
                            foreach (var item in dictionary.OrderByDescending(u => u.Key))
                            {
                                result.Add(item.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                HtmlDocument html = GetWebData<HtmlDocument>(url);

                if (html != null)
                {
                    foreach (HtmlNode div in html.DocumentNode.SelectNodes("//div[@id='episodes']//div[contains(@class, 'item_wrapper')]"))
                    {
                        HtmlNode anchor = div.SelectSingleNode(".//h5/a");
                        string episodio = div.SelectSingleNode(".//span[@class='episodio']").InnerText;
                        Match airDateDurationMatch = airDateDurationRegex.Match(episodio);
                        
                        result.Add(new VideoInfo() {
                                       VideoUrl = anchor.GetAttributeValue("href", ""),
                                       Title = HttpUtility.HtmlDecode(anchor.InnerText),
                                       Thumb = div.SelectSingleNode(".//div[@class='wrapper']//img").GetAttributeValue("original", ""),
                                       Description = HttpUtility.HtmlDecode(div.SelectSingleNode(".//p[@class='description']").InnerText),
                                       Length = airDateDurationMatch.Groups["duration"].Value,
                                       Airdate = airDateDurationMatch.Groups["airDate"].Value
                                   });
                    }
                }
            }
            
            return result;
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();
            
            string webData = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = videoIdRegex.Match(webData);
                if (m.Success)
                {
                    string videoId = m.Groups["videoId"].Value;
                    webData = GetWebData(string.Format(rtmpUrlListFormat, videoId));
                    
                    if (!string.IsNullOrEmpty(webData))
                    {
                        Match jsonMatch = jsonUrlListRegex.Match(webData);
                        if (jsonMatch.Success)
                        {
                           JObject json = JObject.Parse(jsonMatch.Groups["json"].Value);
                           JToken publishedUrls = json.Value<JToken>("published_urls");
                           
                           if (publishedUrls != null)
                           {
                               foreach (JToken child in publishedUrls.Values<JToken>()) {
                                   JToken publishedUrl = child.First();
                                   string embedUrl = publishedUrl.Value<string>("embed_url");
                                   
                                   if (!embedUrl.StartsWith("rtmp")) continue;
                                   
                                   int kbps = int.Parse(publishedUrl.Value<string>("kbps"));
                                   
                                   if (urlsDictionary.ContainsKey(kbps)) continue;

                                   urlsDictionary.Add(kbps, new RtmpUrl(embedUrl).ToString());
                               }
                           }
                        }
                    }
                }
            }

            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key))
            {
                video.PlaybackOptions.Add(string.Format(@"{0} kbps", item.Key.ToString()), item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            
            return result;
        }
    }
}
