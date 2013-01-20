using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site utility for RDS.ca
    /// </summary>
    public class RDSUtil : GenericSiteUtil
    {
        private static Regex jsonOpenBracketRegex = new Regex(@"^\[",
                                                        RegexOptions.Compiled);
        private static Regex jsonCloseBracketRegex = new Regex(@"\]$",
                                                        RegexOptions.Compiled);

        private static string mainCategoryXpath = @"//div[@id='playlist-menu']/ul[@class='ul-parent']/li";
        private static string videoListUrlFormat = @"http://www.rds.ca/zone-video/cache/menus/{1}_{2}.json";
        private static string videoListUrlSecondaryFormat = @"http://www.rds.ca/videos/zonevideo/menuitemvideos?id={0}&page={1}";
        private static string videoUrlFormat = @"http://www.rds.ca/videos/zonevideo?id={0}&format=json";
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            HtmlDocument html = GetWebData<HtmlDocument>(baseUrl);
            string currentParentTitle = string.Empty;
            
            if (html != null)
            {
                foreach (HtmlNode lineItem in html.DocumentNode.SelectNodes(mainCategoryXpath))
                {
                    HtmlNode anchor = lineItem.SelectSingleNode(@"./a");
                    string anchorClass = anchor.GetAttributeValue("class", "");
                    
                    RssLink category = new RssLink() {
                        Name = anchor.InnerText,
                        Url = string.Format(videoListUrlFormat, baseUrl, anchor.GetAttributeValue("rel", ""), "1"),
                        Other = anchor.GetAttributeValue("rel", ""),
                        HasSubCategories = false
                    };
                    Settings.Categories.Add(category);

                    if (anchorClass.Contains(@"hasChildren"))
                    {
                        foreach (HtmlNode subAnchor in lineItem.SelectNodes(@"./ul/li/a[@class='menu-child']"))
                        {
                            Settings.Categories.Add(new RssLink() {
                                                        Name = string.Format(@"{0} - {1}", anchor.InnerText, subAnchor.InnerText),
                                                        Url = string.Format(videoListUrlFormat, baseUrl, subAnchor.GetAttributeValue("rel", ""), "1"),
                                                        Other = subAnchor.GetAttributeValue("rel", ""),
                                                        HasSubCategories = false
                                                    });
                        }
                    }
                    
                }
            }
            
            return Settings.Categories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            return getVideoListForSinglePage(category, (category as RssLink).Url);
        }
        
        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            Log.Debug(@"Looking for videos in {0} at {1}", category.Name, url);
            List<VideoInfo> result = new List<VideoInfo>();

            nextPageUrl = string.Empty;
            currentCategory = category;
            
            JObject json = GetWebData<JObject>((category as RssLink).Url);
            JArray videos = json["videos"] as JArray;
            
            if (videos != null)
            {
                foreach (JToken video in videos)
                {
                    result.Add(new VideoInfo() {
                                   Title = video.Value<string>("title"),
                                   Description = video.Value<string>("description"),
                                   VideoUrl = video.Value<string>("id"),
                                   Airdate = video.Value<string>("broadcast_date"),
                                   Length = video.Value<string>("length"),
                                   ImageUrl = video.Value<JArray>("images")[0].Value<string>("url")
                               });
                }
            }
            int totalPages = (json["totalPages"] as JToken).Value<int>();
            int currentPage = (json["currentPage"] as JToken).Value<int>();
            
            if (currentPage < totalPages)
            {
                nextPageUrl = string.Format(currentPage == 1 ? videoListUrlSecondaryFormat : videoListUrlSecondaryFormat,
                                            category.Other as string, currentPage + 1);
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
            string data = GetWebData(string.Format(videoUrlFormat, video.VideoUrl));
            if (!string.IsNullOrEmpty(data))
            {
                // replace opening and closing brackets [] with empty
                data = jsonOpenBracketRegex.Replace(data, "");
                data = jsonCloseBracketRegex.Replace(data, "");
                
                JToken file = JToken.Parse(data).Value<JArray>("files")[0];
                result = string.Format(@"{0}://{1}{2}{3}",
                                    file.Value<string>("protocol"),
                                    file.Value<string>("server"),
                                    file.Value<string>("folder"),
                                    file.Value<string>("name"));
            }
            return result;
        }
    }
}
