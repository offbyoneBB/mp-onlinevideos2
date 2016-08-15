using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using OnlineVideos.Sites.georgius;

namespace OnlineVideos.Sites
{
    public class XVideosUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected new string baseUrl = @"http://www.xvideos.com";
        [Category("OnlineVideosConfiguration"), Description("Tags Url used for categories if UseTagsCategories is true")]
        protected string tagsUrl = @"http://www.xvideos.com/tags";
        [Category("OnlineVideosConfiguration"), Description("If true then Tags Url will be used to build categories")]
        protected bool useTagsCategories = false;
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        protected new string searchUrl = @"http://www.xvideos.com/?k={0}";
        [Category("OnlineVideosConfiguration"), Description("Url used in category to show all time best videos.")]
        protected string bestAllTimeUrl = @"http://www.xvideos.com/best";
        [Category("OnlineVideosConfiguration"), Description("Url used in category to show this month best videos.")]
        protected string bestThisMonthTimeUrl = @"http://www.xvideos.com/best/month";
        [Category("OnlineVideosConfiguration"), Description("Url used in category to show this week best videos.")]
        protected string bestThisWeekTimeUrl = @"http://www.xvideos.com/best/week";
        [Category("OnlineVideosConfiguration"), Description("Url used in category to show today best videos.")]
        protected string bestThisDayTimeUrl = @"http://www.xvideos.com/best/day";

        private static readonly Regex VideoUrlHighRegex = new Regex(@"html5player\.setVideoUrlHigh\s*\(\s*'(?<videoUrl>[^']*)'\)",
                                                       RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex VideoUrlLowRegex = new Regex(@"html5player\.setVideoUrlLow\s*\(\s*'(?<videoUrl>[^']*)'\)",
                                                       RegexOptions.Singleline | RegexOptions.Compiled);
        private Category _currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            if (!string.IsNullOrEmpty(bestAllTimeUrl))
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = "TOP All time",
                    Url = bestAllTimeUrl,
                    HasSubCategories = false
                });
            }
            if (!string.IsNullOrEmpty(bestThisMonthTimeUrl))
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = "TOP This Month",
                    Url = bestThisMonthTimeUrl,
                    HasSubCategories = false
                });
            }
            if (!string.IsNullOrEmpty(bestThisWeekTimeUrl))
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = "TOP This Week",
                    Url = bestThisWeekTimeUrl,
                    HasSubCategories = false
                });
            }
            if (!string.IsNullOrEmpty(bestThisDayTimeUrl))
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = "TOP Today",
                    Url = bestThisDayTimeUrl,
                    HasSubCategories = false
                });
            }

            if (useTagsCategories)
            {
                var document = GetWebData<HtmlDocument>(tagsUrl);
                if (document != null)
                {
                    foreach (var anchor in document.DocumentNode.SelectNodes(@"//*[@id='tags']/li"))
                    {
                        Settings.Categories.Add(new RssLink()
                        {
                            Name = anchor.SelectSingleNode(".//a/b").InnerText,
                            Url = Utils.FormatAbsoluteUrl(anchor.SelectSingleNode(".//a").GetAttributeValue(@"href", string.Empty), baseUrl),
                            HasSubCategories = false
                        });
                    }
                }
            }
            else
            {
                var document = GetWebData<HtmlDocument>(baseUrl);
                if (document != null)
                {
                    foreach (var anchor in document.DocumentNode.SelectNodes(@"//div[@class ='main-categories']/ul/li[position()>1 and position() < last()]"))
                    {
                        Settings.Categories.Add(new RssLink()
                        {
                            Name = anchor.SelectSingleNode(".//a").InnerText,
                            Url = Utils.FormatAbsoluteUrl(anchor.SelectSingleNode(".//a").GetAttributeValue(@"href", string.Empty), baseUrl),
                            HasSubCategories = false
                        });
                    }
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        //public override int DiscoverSubCategories(Category parentCategory)
        //{
        //    throw new NotImplementedException();
        //}

        public override List<VideoInfo> GetVideos(Category category)
        {
            return GetVideoListForSinglePage(category, (category as RssLink)?.Url);
        }

        private List<VideoInfo> GetVideoListForSinglePage(Category category, string url)
        {
            var result = new List<VideoInfo>();
            nextPageUrl = string.Empty;
            _currentCategory = category;

            var document = GetWebData<HtmlDocument>(url);
            if (document != null)
            {
                var nextPageNode = document.DocumentNode.SelectSingleNode(@"//a[@class='no-page']");
                if (nextPageNode != null)
                {
                    nextPageUrl = Utils.FormatAbsoluteUrl(nextPageNode.GetAttributeValue(@"href", string.Empty), baseUrl);
                }

                foreach (var anchor in document.DocumentNode.SelectNodes(@"//div[@class ='mozaique']/div"))
                {
                    var thumbNode = anchor.SelectSingleNode(@".//div[@class ='thumb']");
                    if (thumbNode == null)
                        continue;

                    var rawThumb = thumbNode.InnerText;
                    rawThumb = rawThumb.Substring(rawThumb.IndexOf("'", StringComparison.Ordinal)+1,
                        rawThumb.LastIndexOf("'", StringComparison.Ordinal) - rawThumb.IndexOf("'", StringComparison.Ordinal)+1);
                    var documentThumb = new HtmlDocument();
                    documentThumb.LoadHtml(rawThumb);
                    var thumb = documentThumb.DocumentNode.SelectSingleNode(@"//a/img").GetAttributeValue(@"src", string.Empty);

                    result.Add(new VideoInfo()
                    {
                        VideoUrl = Utils.FormatAbsoluteUrl(anchor.SelectSingleNode(".//p/a").GetAttributeValue(@"href", string.Empty), baseUrl),
                        Thumb = thumb,
                        Title = anchor.SelectSingleNode(".//p/a/@title").InnerText,
                        Length = anchor.SelectSingleNode(".//p/span/span[@class ='duration']").InnerText
                    });
                }
            }
            return result;
        }

        public override bool HasNextPage
        {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return GetVideoListForSinglePage(_currentCategory, nextPageUrl);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                var videoUrlHighMatch = VideoUrlHighRegex.Match(data);
                var videoUrl = videoUrlHighMatch.Groups["videoUrl"].Value;
                if (string.IsNullOrEmpty(videoUrl))
                {
                    var videoUrlLowMatch = VideoUrlLowRegex.Match(data);
                    videoUrl = videoUrlLowMatch.Groups["videoUrl"].Value;
                }
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    return new MPUrlSourceFilter.HttpUrl(videoUrl) { UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0" }.ToString();
                } 
            }
            return string.Empty;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            query = string.Format(searchUrl, HttpUtility.UrlEncode(query));
            var internalResults = GetVideoListForSinglePage(_currentCategory, query);
            internalResults.ForEach(ir => results.Add(ir));
            return results;
        }
    }
}
