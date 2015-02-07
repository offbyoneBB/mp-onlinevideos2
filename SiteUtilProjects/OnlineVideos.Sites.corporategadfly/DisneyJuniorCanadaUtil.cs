using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site util for DisneyJunior.ca.
    /// </summary>
    public class DisneyJuniorCanadaUtil : GenericSiteUtil
    {
        private static string tokenUrl = @"/en/video/tokenas3";
        private static string rtmpUrlFormat = @"rtmpe://cp107996.edgefcs.net/ondemand?{0}";
        private static string playPathFormat = @"mp4:videos/phd/{0}";

        private static Regex tokenRegex = new Regex(@"error=&uri=(?<token>.+)",
                                                    RegexOptions.Compiled);
        private static Regex filenameRegex = new Regex(@"""fn"":""(?<filename>[^""]*)""",
                                                       RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            int result = base.DiscoverDynamicCategories();
            foreach (Category category in Settings.Categories) {
                if ("By Show".Equals(category.Name))
                {
                    category.HasSubCategories = true;
                    break;
                }
            }
            return result;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            int result = 0;
            
            if ("By Show".Equals(parentCategory.Name))
            {
                HtmlDocument document = GetWebData<HtmlDocument>((parentCategory as RssLink).Url);
                if (document != null)
                {
                    HtmlNodeCollection anchors = document.DocumentNode.SelectNodes(@"//div[@class = 'panel sub-navigation-videos-shows']/ul/li[@class = 'show']/a");
                    if (anchors != null)
                    {
                        parentCategory.SubCategories = new List<Category>();
                        foreach (HtmlNode anchor in anchors)
                        {
                            HtmlNode img = anchor.SelectSingleNode(@"./img");
                            parentCategory.SubCategories.Add(new RssLink() {
                                                                 Name = img.GetAttributeValue(@"alt", string.Empty),
                                                                 Url = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                                                 Thumb = string.Format(@"{0}{1}", baseUrl, img.GetAttributeValue(@"src", string.Empty))
                                                             });
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        result = parentCategory.SubCategories.Count;
                    }
                }
            }
            else
            {
                result = base.DiscoverSubCategories(parentCategory);
            }

            return result;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            
            HtmlDocument document = GetWebData<HtmlDocument>((category as RssLink).Url);
            if (document != null)
            {
                HtmlNodeCollection anchors = document.DocumentNode.SelectNodes(@"//ul[@class = 'container']/li/a[@class = 'thumbnail']");
                if (anchors != null)
                {
                    foreach (HtmlNode anchor in anchors)
                    {
                        HtmlNode img = anchor.SelectSingleNode(@"./img");
                        result.Add(new VideoInfo() {
                                       Title = anchor.GetAttributeValue(@"title", string.Empty),
                                       VideoUrl = string.Format(@"{0}{1}", baseUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                                       Thumb = string.Format(@"{0}{1}", baseUrl, img.GetAttributeValue(@"src", string.Empty))
                                   });
                    }
                }
            }
            
            return result;
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            string result = string.Empty;
            
            string data = GetWebData(string.Format(@"{0}{1}", baseUrl, tokenUrl));
            
            if (!string.IsNullOrEmpty(data))
            {
                Match tokenMatch = tokenRegex.Match(data);
                
                if (tokenMatch.Success)
                {
                    data = GetWebData(video.VideoUrl);
                    string token = HttpUtility.UrlDecode(tokenMatch.Groups["token"].Value);
                    string url = string.Format(rtmpUrlFormat, token);
                    
                    if (!string.IsNullOrEmpty(data))
                    {
                        Match filenameMatch = filenameRegex.Match(data);
                        if (filenameMatch.Success)
                        {
                            string playPath = string.Format(playPathFormat, filenameMatch.Groups["filename"].Value);
                            result = new MPUrlSourceFilter.RtmpUrl(url) { PlayPath = playPath }.ToString();
                        }
                    }
                }
            }
            return result;
        }
    }
}
