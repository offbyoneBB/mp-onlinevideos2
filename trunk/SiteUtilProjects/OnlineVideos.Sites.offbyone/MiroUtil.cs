using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Miro API documentation: https://develop.participatoryculture.org/trac/democracy/wiki/MiroGuideApi
    /// </summary>
    public class MiroUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Number of Pages to retrieve when looking for categories of a genre. Default is 5.")]
        int maxCategoryPages = 5;        
        
        public override int DiscoverDynamicCategories()
        {
            string catsString = GetWebData(baseUrl);
            if (!string.IsNullOrEmpty(catsString))
            {
                Settings.Categories.Clear();
                Match m = regEx_dynamicCategories.Match(catsString);
                while (m.Success)
                {
                    RssLink rss = new RssLink();
                    rss.HasSubCategories = true;
                    rss.Name = m.Groups["title"].Value;
                    rss.Url = m.Groups["url"].Value;
                    Settings.Categories.Add(rss);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string catsString = GetWebData((parentCategory as RssLink).Url);
            parentCategory.SubCategories = new List<Category>();
            if (!string.IsNullOrEmpty(catsString))
            {
                int maxPages = 1;
                Match m = regEx_NextPage.Match(catsString);
                if (m.Success) int.TryParse(m.Groups["lastPage"].Value, out maxPages);
                if (maxPages > maxCategoryPages) maxPages = maxCategoryPages;
                int currentPage = 1;
                while (currentPage <= maxPages)
                {
                    m = regEx_dynamicSubCategories.Match(catsString);
                    while (m.Success)
                    {
                        RssLink rss = new RssLink();
                        rss.SubCategoriesDiscovered = true;
                        rss.HasSubCategories = false;
                        rss.Name = m.Groups["name"].Value;
                        rss.Url = System.Web.HttpUtility.ParseQueryString(System.Web.HttpUtility.HtmlDecode(new System.Uri(m.Groups["url"].Value).Query))[0];
                        rss.Description = m.Groups["desc"].Value;
                        string feedId = m.Groups["mirourl"].Value.Substring(m.Groups["mirourl"].Value.LastIndexOf('/') + 1);
                        rss.Thumb = string.Format("http://s3.miroguide.com/static/media/thumbnails/200x133/{0}.jpeg", feedId)
                        + "|" + string.Format("http://s3.miroguide.com/static/media/thumbnails/97x65/{0}.jpeg", feedId);
                        parentCategory.SubCategories.Add(rss);
                        rss.ParentCategory = parentCategory;
                        m = m.NextMatch();
                    }
                    currentPage++;
                    try
                    {
                        catsString = GetWebData((parentCategory as RssLink).Url + "?page=" + currentPage.ToString());
                    }
                    catch (Exception)
                    {
                        break; // couldn't get page, stop trying to get any further
                    }
                    
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }
    }
}
