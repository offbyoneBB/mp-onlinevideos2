using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

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
                foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
                {
                    VideoInfo video = new VideoInfo();
                    if (!String.IsNullOrEmpty(rssItem.Description))
                    {
                        video.Description = rssItem.Description;
                    }
                    else
                    {
                        video.Description = rssItem.MediaDescription;
                    }
                    if (rssItem.MediaThumbnails != null && rssItem.MediaThumbnails.Count > 0)
                    {
                        video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                    }
                    //get the video
                    if (rssItem.Enclosure != null && !string.IsNullOrEmpty(rssItem.Enclosure.Url))
                    {
                        video.VideoUrl = rssItem.Enclosure.Url;
                        video.Length = string.IsNullOrEmpty(rssItem.Enclosure.Length) ? rssItem.PubDate : rssItem.Enclosure.Length; // if no duration at least display the Publication date
                    }
                    else if (rssItem.MediaContents.Count > 0)
                    {
                        foreach (RssItem.MediaContent content in rssItem.MediaContents)
                        {
                            if (isPossibleVideo(content.Url))
                            {
                                video.VideoUrl = content.Url;
                                if (content.Duration > 0) video.Length = content.Duration.ToString();
                                break;
                            }
                        }
                    }
                    video.Title = rssItem.Title;
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
