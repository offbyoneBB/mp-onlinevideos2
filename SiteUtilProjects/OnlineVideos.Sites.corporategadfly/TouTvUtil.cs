using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TouTvUtil : GenericSiteUtil
    {
        private static Regex jsonMainPageRegex = new Regex(@"var\ssearchModels\s=\s{(?<json>.*?)(?=)};",
                                                   RegexOptions.Compiled);
        private static Regex jsonSeasonRegex = new Regex(@"jsonData\s+:\s+(?<json>.*?})\s+$",
                                                         RegexOptions.Compiled | RegexOptions.Multiline);
        
        private static string baseUrlPrefix = @"http://ici.tou.tv";
        private static string mainCategoriesUrl = baseUrlPrefix + @"/a-z";
        private static string presentationSectionUrl = baseUrlPrefix + @"/Presentation/section/a-z?includePromoItems=true";
        private static string manifestUrlFormat = @"{0}&g={1}&hdcore=3.3.0";
        
        private Dictionary<string, VideoInfo> metaInfo = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            HtmlDocument document = GetWebData<HtmlDocument>(mainCategoriesUrl);

            if (document != null)
            {
                foreach (HtmlNode heading in document.DocumentNode.SelectNodes(@"//div[@id = 'section']//h2"))
                {
                    string title = heading.InnerText.Trim();
                    Settings.Categories.Add(
                        new RssLink() {
                            Name = HttpUtility.HtmlDecode(title),
                            Url = mainCategoriesUrl,
                            Other = title,
                            HasSubCategories = @"Films".Equals(title) ? false : true    // go straight to video list for "Films"
                        });
                }
            }
            
            Settings.DynamicCategoriesDiscovered = true;

            if (metaInfo == null)
            {
                // initialize dictionary
                metaInfo = new Dictionary<string, VideoInfo>();
                JObject json = GetWebData<JObject>(presentationSectionUrl);
                
                foreach (JToken lineup in (JArray) json["Lineups"])
                {
                    foreach (JToken lineupItem in (JArray) lineup["LineupItems"])
                    {
                        if (string.IsNullOrEmpty((string) lineupItem["Url"]) || !(bool) lineupItem["IsFree"])
                        {
                            continue;
                        }

                        string url = (string) lineupItem["Url"];
                        if (!metaInfo.ContainsKey(url))
                        {
                            metaInfo.Add(url, new VideoInfo() {
                                             Title = (string) lineupItem["Title"],
                                             VideoUrl = string.Format(@"{0}{1}", baseUrlPrefix, url),
                                             ImageUrl = (string) lineupItem["ImageUrl"],
                                             Description = (string) lineupItem["Details"]["Description"],
                                             Airdate = (string) lineupItem["Details"]["AirDate"],
                                             Length = (string) TimeSpan.FromSeconds((int) lineupItem["Details"]["Length"]).ToString()
                                         });
                        }
                    }
                }
            }

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink) parentCategory;
            if (mainCategoriesUrl.Equals(parentRssLink.Url))
            {
                // still working with sub categories
                HtmlDocument document = GetWebData<HtmlDocument>(parentRssLink.Url);
                
                if (document != null)
                {
                    string xpath = string.Format(@"//div[@id = 'section']//div[h2[contains(., '{0}')]]//a", parentRssLink.Other);
                    foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(xpath)) {
                        string href = anchor.GetAttributeValue(@"href", string.Empty);

                        if (!metaInfo.ContainsKey(href))
                        {
                            // must be a non-free item (since we only adde free items to metaInfo earlier)
                            continue;
                        }

                        parentCategory.SubCategories.Add(
                            new RssLink() {
                                ParentCategory = parentCategory,
                                Name = HttpUtility.HtmlDecode(anchor.InnerText),
                                Url = string.Format(@"{0}{1}", baseUrlPrefix, href),
                                Thumb = metaInfo[href].ImageUrl,
                                HasSubCategories = true
                            });
                    }
                }
            } else {
                // working with seasons (for a particular show)
                string webData = GetWebData(parentRssLink.Url);
                if (!string.IsNullOrEmpty(webData))
                {
                    Match jsonSeasonMatch = jsonSeasonRegex.Match(webData);
                    if (jsonSeasonMatch.Success)
                    {
                        JObject json = JObject.Parse(jsonSeasonMatch.Groups["json"].Value);
                        if (json["seasonLineups"].HasValues)
                        {
                            JArray seasons = (JArray) json["seasonLineups"];
                            foreach (JToken season in seasons)
                            {
                                parentCategory.SubCategories.Add(
                                    new RssLink() {
                                        ParentCategory = parentCategory,
                                        Name = (string) season["title"],
                                        Url = parentRssLink.Url,
                                        Other = (string) season["name"],
                                        HasSubCategories = false
                                    });
                            }
                        }
                        else
                        {
                            // create dummy season (as there are no seasons, e.g., in documentaries)
                            parentCategory.SubCategories.Add(
                                new RssLink() {
                                    ParentCategory = parentCategory,
                                    Name = parentRssLink.Name,
                                    Url = parentRssLink.Url,
                                    Other = (string) json["share"]["url"],
                                    HasSubCategories = false
                                });
                        }
                    }
                    else
                    {
                        Log.Debug(@"JSON for seasons not found");
                    }
                }
            }
            
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            
            string parentRssLinkUrl = ((RssLink) category).Url;

            string webData = GetWebData(parentRssLinkUrl);
            
            if (!string.IsNullOrEmpty(webData))
            {
                if (@"Films".Equals(category.Name))
                {
                    HtmlDocument document = GetWebData<HtmlDocument>(parentRssLinkUrl);
                    
                    if (document != null)
                    {
                        string xpath = string.Format(@"//div[@id = 'section']//div[h2[contains(., '{0}')]]//a", category.Other);
                        foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(xpath)) {
                            string href = anchor.GetAttributeValue(@"href", string.Empty);
                            if (!metaInfo.ContainsKey(href))
                            {
                                // must be a non-free item (since we only adde free items to metaInfo earlier)
                                continue;
                            }
                            result.Add(metaInfo[href]);
                        }
                    }
                }
                else
                {
                    Match jsonSeasonMatch = jsonSeasonRegex.Match(webData);
                    if (jsonSeasonMatch.Success)
                    {
                        JObject json = JObject.Parse(jsonSeasonMatch.Groups["json"].Value);
                        if (json["seasonLineups"].HasValues)
                        {
                            foreach (JToken season in (JArray) json["seasonLineups"])
                            {
                                if (category.Other.Equals((string) season["name"]))
                                {
                                    foreach (JToken episode in (JArray) season["lineupItems"])
                                    {
                                        result.Add(
                                            new VideoInfo() {
                                                Title = (string) episode["title"],
                                                Description = (string) episode["details"]["description"],
                                                VideoUrl = string.Format(@"{0}{1}", baseUrlPrefix, (string) episode["url"]),
                                                ImageUrl = (string) episode["imageUrl"],
                                                Airdate = (string) episode["details"]["airDate"],
                                                Length = TimeSpan.FromSeconds((int) episode["details"]["length"]).ToString()
                                            });
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // single video (there are no seasons)
                            result.Add(metaInfo[(string) category.Other]);
                        }
                    }
                }
            }
            return result;
        }
                
        public override string GetVideoUrl(VideoInfo video)
        {
            string playListUrl = getPlaylistUrl(video.VideoUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error
            
            Log.Debug(@"video: {0}", video.Title);
            string result = string.Empty;

            string data = GetWebData(playListUrl);
            
            Log.Debug(@"Validation JSON: {0}", data);

            if (!string.IsNullOrEmpty(data))
            {
                JToken json = JToken.Parse(data);
                string url = (string) json["url"];
                if (!string.IsNullOrEmpty(url))
                {
                    string manifestUrl = string.Format(manifestUrlFormat, url, GetRandomChars(12));                
                    result = new MPUrlSourceFilter.HttpUrl(manifestUrl).ToString();
                }
                else
                {
                    throw new OnlineVideosException((string) json["message"]);
                }
            }
            return result;
        }

        string GetRandomChars(int amount)
        {
            var random = new Random();
            var sb = new StringBuilder(amount);
            for (int i = 0; i < amount; i++ ) sb.Append(Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
            return sb.ToString();
        }

    }
}
