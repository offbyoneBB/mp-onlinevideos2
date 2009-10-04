using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class MiroUtil : SiteUtilBase
    {
        static Regex categoriesRegEx = new Regex(@"\{'url'\:\su'(?<url>[^']+)',\s'name'\:\su'(?<name>[^']+)'", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex subCategoriesRegEx = new Regex(@"<div\sclass=""searchResultContent"">\s*
                                                      <h4><a\shref=""(?<mirourl>[^""]+)""[^>]*>(?<name>[^<]*)</a></h4>\s*
                                                      <p>(?<desc>(?:(?!</p>).)*)</p>\s*</div>
                                                      (?:(?!http\://subscribe\.getmiro\.com).)*(?<url>[^""]*)""",
                                                      RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override System.Collections.Generic.List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();

            string catsString = GetWebData((category as RssLink).Url);

            if (!string.IsNullOrEmpty(catsString))
            {
                List<RssItem> loRssItemList = getRssDataItems(((RssLink)category).Url);
                foreach (RssItem rssItem in loRssItemList)
                {
                    VideoInfo video = new VideoInfo();
                    if (!String.IsNullOrEmpty(rssItem.description))
                    {
                        video.Description = rssItem.description;
                    }
                    else
                    {
                        video.Description = rssItem.mediaDescription;
                    }
                    if (!String.IsNullOrEmpty(rssItem.mediaThumbnail))
                    {
                        video.ImageUrl = rssItem.mediaThumbnail;
                    }
                    else if (!String.IsNullOrEmpty(rssItem.exInfoImage))
                    {
                        video.ImageUrl = rssItem.exInfoImage;
                    }
                    //get the video
                    if (!String.IsNullOrEmpty(rssItem.enclosure) && isPossibleVideo(rssItem.enclosure))
                    {
                        video.VideoUrl = rssItem.enclosure;
                        video.Length = rssItem.enclosureDuration != null ? rssItem.enclosureDuration : rssItem.pubDate; // if no duration at least display the Publication date
                    }
                    else if (rssItem.contentList.Count > 0)
                    {
                        foreach (MediaContent content in rssItem.contentList)
                        {
                            if (isPossibleVideo(content.url))
                            {
                                video.VideoUrl = content.url;
                                video.Length = content.duration;
                                break;
                            }
                        }
                    }
                    video.Title = rssItem.title;
                    if (String.IsNullOrEmpty(video.VideoUrl) == false)
                    {
                        loVideoList.Add(video);
                    }
                }
            }

            return loVideoList;
        }

        public override int DiscoverDynamicCategories()
        {
            string catsString = GetWebData("https://www.miroguide.com/api/list_categories");
            if (!string.IsNullOrEmpty(catsString))
            {
                Settings.Categories.Clear();
                Match m = categoriesRegEx.Match(catsString);
                while (m.Success)
                {
                    RssLink rss = new RssLink();
                    rss.HasSubCategories = true;
                    rss.Name = m.Groups["name"].Value;
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
                Match m = subCategoriesRegEx.Match(catsString);
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
    }
}
