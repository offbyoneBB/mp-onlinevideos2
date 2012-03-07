using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Util for treehousetv.com
    /// </summary>
    public class TreehouseTVUtil : CanadaBrightCoveUtilBase
    {
        protected override string hashValue { get { return @"b956f752886e0e38a5ad4ffef43f48c839316602"; } }
        protected override string playerId { get { return @"904944191001"; } }
        protected override string publisherId { get { return @"694915333001"; } }
        protected override BrightCoveType RequestType { get { return BrightCoveType.FindMediaById; } }
        
        protected virtual string baseUrlPrefix { get { return @"http://media.treehousetv.com"; } }
        protected virtual string categoryUrl { get { return baseUrlPrefix + @"/videos.ashx{0}"; } }
        
        private static string mainCategoryXpath = @"//div[@id='video-navigation']/ul[@class='level_0']/li";
        private static string subCategoryXpath = mainCategoryXpath + "{0}";
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            HtmlDocument html = GetWebData<HtmlDocument>(baseUrlPrefix);
            if (html != null)
            {
                foreach (HtmlNode lineItem in html.DocumentNode.SelectNodes(mainCategoryXpath))
                {
                    HtmlNode anchor = lineItem.SelectSingleNode(@"./a");
                    string url = anchor.GetAttributeValue("href", "");
                    
                    // skip in case of the "All Videos" category
                    if ("All Videos".Equals(anchor.InnerText)) continue;
                    
                    RssLink cat = new RssLink() {
                        Name = anchor.InnerText,
                        Url = string.Format(categoryUrl, url),
                        HasSubCategories = false
                    };
                    
                    HtmlNode unorderedList = lineItem.SelectSingleNode(@"./ul[@class='level_1']");
                    
                    if (unorderedList != null)
                    {
                        cat.HasSubCategories = true;
                    }
                    
                    Settings.Categories.Add(cat);
                }
            }
            
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            HtmlDocument html = GetWebData<HtmlDocument>(baseUrlPrefix);
            if (html != null)
            {
                foreach (HtmlNode unorderedList in html.DocumentNode.SelectNodes(string.Format(subCategoryXpath, @"/ul[@class='level_1']")))
                {
                    HtmlNode previousSibling = unorderedList.PreviousSibling;
                    
                    if (previousSibling != null && parentCategory.Name.Equals(previousSibling.InnerText))
                    {
                        foreach (HtmlNode anchor in unorderedList.SelectNodes(@"./li/a"))
                        {
                            RssLink cat = new RssLink() {
                                ParentCategory = parentCategory,
                                Name = anchor.InnerText,
                                Url = string.Format(categoryUrl, anchor.GetAttributeValue("href", "")),
                                HasSubCategories = false
                            };
                            
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url = ((RssLink) category).Url;
            string json = GetWebData(url);

            if (json != null)
            {
                foreach (JToken item in JArray.Parse(json))
                {
                    result.Add(new VideoInfo() {
                                   VideoUrl = item.Value<string>("Id"),
                                   Title = item.Value<string>("Name"),
                                   Description = item.Value<string>("ShortDescription"),
                                   Length = item.Value<string>("Duration"),
                                   ImageUrl = item.Value<string>("ThumbnailURL")
                               });
                }
            }
            
            return result;
        }
    }
}
