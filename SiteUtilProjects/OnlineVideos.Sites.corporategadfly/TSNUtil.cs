using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TSNUtil : CTVUtil
    {
        private static string mainCategoriesUrl = @"http://www.tsn.ca/VideoHub";
        private static string contentUrlFormat = mediaBaseUrl + @"/{0}_web/platforms/desktop/contents/{1}?$include=[Id,ContentPackages]";
        
        private static Regex jsonRegex = new Regex(@"var TVE_Obj ={(?<json>.*?)(?=)};",
                                                   RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {          
            Settings.Categories.Clear();
            
            HtmlDocument document = GetWebData<HtmlDocument>(mainCategoriesUrl);
            if (document != null)
            {
                foreach (HtmlNode anchor in document.DocumentNode.SelectNodes(@"//div[@id = 'Sports']/ul/li/a"))
                {
                    Settings.Categories.Add(
                        new RssLink() {
                            Url = string.Format(@"{0}/{1}", mainCategoriesUrl, anchor.GetAttributeValue(@"href", string.Empty)),
                            Name = anchor.InnerText,
                            HasSubCategories = false
                        });
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            
            string webData = GetWebData(((RssLink) category).Url);
            
            if (!string.IsNullOrEmpty(webData))
            {
                Match jsonMatch = jsonRegex.Match(webData);
                if (jsonMatch.Success)
                {
                    JObject json = JObject.Parse("{" + jsonMatch.Groups["json"].Value + "}");
                    JArray items = (JArray) json["Items"];
                    
                    foreach (JToken item in items)
                    {
                        result.Add(
                            new VideoInfo() {
                                VideoUrl = string.Format(contentUrlFormat, siteCode, (int) item["Id"]),
                                Title = (string) item["Name"],
                                Description = (string) item["Desc"],
                                ImageUrl = (string) item["Images"][0]["Url"],
                            });
                    }
                }
            }

            return result;
        }
        
        public override string getUrl(VideoInfo video)
        {
            string result = video.VideoUrl;
            
            JObject json = GetWebData<JObject>(result);
            if (json != null)
            {
                video.VideoUrl = string.Format(stacksUrlFormat, siteCode, (int) json["Id"], (int) json["ContentPackages"][0]["Id"]);
                result = base.getUrl(video);
            }
            return result;
        }
    }
}
