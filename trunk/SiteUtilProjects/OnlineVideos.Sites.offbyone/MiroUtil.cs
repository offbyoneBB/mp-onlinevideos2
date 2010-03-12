using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Miro API documentation: https://develop.participatoryculture.org/trac/democracy/wiki/MiroGuideApi
    /// </summary>
    public class MiroUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the results of apiUrl_categoryList for dynamic categories. Group names: 'url', 'title'.")]
        string dynamicCategoriesRegEx = @"\{'url'\:\su'(?<url>[^']+)',\s'name'\:\su'(?<title>[^']+)'";
        [Category("OnlineVideosConfiguration"), Description("Url for retrieving the list of categories.")]
        string apiUrl_categoryList = "https://www.miroguide.com/api/list_categories";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'name', 'desc', 'mirourl'.")]
        string dynamicSubCategoriesRegEx = @"<div\sclass=""searchResultContent"">\s*
                                              <h4><a\shref=""(?<mirourl>[^""]+)""[^>]*>(?<name>[^<]*)</a></h4>\s*
                                              <p>(?<desc>(?:(?!</p>).)*)</p>\s*</div>
                                              (?:(?!http\://subscribe\.getmiro\.com).)*(?<url>[^""]*)""";

        Regex regEx_dynamicCategories, regEx_dynamicSubCategories;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(dynamicCategoriesRegEx)) regEx_dynamicCategories = new Regex(dynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (!string.IsNullOrEmpty(dynamicSubCategoriesRegEx)) regEx_dynamicSubCategories = new Regex(dynamicSubCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            string catsString = GetWebData(apiUrl_categoryList);
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
                Match m = regEx_dynamicSubCategories.Match(catsString);
                while (m.Success)
                {
                    RssLink rss = new RssLink();
                    rss.SubCategoriesDiscovered = true;
                    rss.HasSubCategories = false;                    
                    rss.Name = m.Groups["name"].Value;
                    rss.Url = System.Web.HttpUtility.ParseQueryString(System.Web.HttpUtility.HtmlDecode(new System.Uri(m.Groups["url"].Value).Query))[0];
                    rss.Description = m.Groups["desc"].Value;
                    string feedId = m.Groups["mirourl"].Value.Substring(m.Groups["mirourl"].Value.LastIndexOf('/') + 1); 
                    rss.Thumb = string.Format("http://s3.miroguide.com/static/media/thumbnails/97x65/{0}.jpeg", feedId);
                    parentCategory.SubCategories.Add(rss);
                    rss.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            string catsString = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(catsString))
            {
                foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
                {
                    loVideoList.Add(VideoInfo.FromRssItem(rssItem, false, new Predicate<string>(delegate(string url) { return url.StartsWith("http://"); })));
                }
            }
            return loVideoList;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.VideoUrl.ToLower().Contains("youtube.com")) return YouTubeUtil.ConvertUrl(video.VideoUrl);
            else return base.getUrl(video);
        }
    }
}
