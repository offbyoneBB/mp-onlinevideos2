using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// site util for ztele.com
    /// </summary>
    public class ZTeleUtil : CanadaBrightCoveUtilBase
    {
        private static string baseUrlPrefix = @"http://www.ztele.com";
        private static string mainCategoryUrl = baseUrlPrefix + @"/webtele";
        
        private static Regex subcategoryMainRegex = new Regex(@"<header>\s+<h4>(?<title>[^<]*)</h4>\s+</header>\s+<ul>\s+(?<categories>(<li>\s+<a[^<]*</a>\s+</li>\s+)+)",
                                                          RegexOptions.Compiled);
        private static Regex subcategoryEntriesRegex = new Regex(@"<li>\s+<a\s+href=""(?<url>[^""]*)""[^>]*>(?<title>[^<]*)</a>\s+</li>",
                                                                 RegexOptions.Compiled);
        
        protected override string hashValue { get { return @"faf1b90dbe278e370da5a43bde59b6efcf841d9d"; } }
        protected override string playerId { get { return @"1381361288001"; } }
        protected override string publisherId { get { return @"681685607001"; } }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "Émissions", Url = mainCategoryUrl, HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Séries Web", Url = mainCategoryUrl, HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Thèmes", Url = mainCategoryUrl, HasSubCategories = true }
               );

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string url = ((RssLink) parentCategory).Url;
            string webData = GetWebData(url);
            
            if (webData != null)
            {
                foreach (Match m in subcategoryMainRegex.Matches(webData))
                {
                    // skip this match unless title is same as category name
                    if (!parentCategory.Name.Equals(m.Groups["title"].Value)) continue;
                    
                    string categories = m.Groups["categories"].Value;
                    
                    if (categories != null)
                    {
                        foreach (Match categoryMatch in subcategoryEntriesRegex.Matches(categories))
                        {
                            RssLink cat = new RssLink();
                            cat.ParentCategory = parentCategory;
                            cat.Name = HttpUtility.HtmlDecode(categoryMatch.Groups["title"].Value);
                            cat.Url = string.Format(@"{0}{1}", baseUrlPrefix, categoryMatch.Groups["url"].Value.Trim());
                            cat.HasSubCategories = false;
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

            HtmlDocument html = GetWebData<HtmlDocument>(((RssLink) category).Url);
            
            if (html != null)
            {
                HtmlNodeCollection items = html.DocumentNode.SelectNodes(@"//ul[@id = 'gallerie_0']/li");
                if (items != null)
                {
                    foreach (HtmlNode item in items)
                    {
                        HtmlNode anchor = item.SelectSingleNode(@"./div[@class = 'picture']/a");
                        HtmlNode img = anchor.SelectSingleNode(@"./img");
                        HtmlNode paragraph = item.SelectSingleNode(@"./div[@class = 'txt']/p");
                        result.Add(new VideoInfo() {
                                       VideoUrl = string.Format(@"{0}{1}", baseUrlPrefix, anchor.GetAttributeValue(@"href", string.Empty)),
                                       Title = HttpUtility.HtmlDecode(paragraph.InnerText),
                                       ImageUrl = string.Format(@"{0}{1}", baseUrlPrefix, img.GetAttributeValue(@"src", string.Empty))
                                   });
                    }
                }
            }
            
            return result;
        }
    }
}
