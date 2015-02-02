using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class MuviCzUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://muvi.cz/";

        private static String showEpisodesStart = @"<div class=""leftColumnBg"">";
        private static String showEpisodesEnd = @"<div class=""right-rail"">";

        private static String showEpisodeStart = @"<li class=""listItem"">";
        private static String showEpisodeEnd = @"</li>";

        private static String showEpisodeThumbRegex = @"(<img height=""[^""]*"" width=""[^""]*"" src=""(?<showEpisodeThumbUrl>[^""]+)"")|(<img src=""(?<showEpisodeThumbUrl>[^""]+)"")";
        private static String showEpisodeUrlAndTitleRegex = @"<a name=""[^""]*"" href=""(?<showEpisodeUrl>[^""]+)"" target=""[^""]*"" alt=""[^""]*"">(?<showEpisodeTitle>[^<]+)";
        private static String showEpisodeDescriptionRegex = @"<div class="" carouselItemText"">(?<showEpisodeDescription>[^<]*)";

        private static String showEpisodeNextPageStart = @"<div class=""pager"">";
        private static String showEpisodeNextPageEnd = @"</div>";
        private static String showEpisodeNextPageRegex = @"<a href='(?<nextPageUrl>[^']+)'>(?<nextPageValue>[^<]+)";

        private static String searchQueryUrl = @"http://muvi.cz/hledej2?q={0}";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        private static String subcategoriesStart = @"<div class=""other_content_list"">";
        private static String subcategoriesEnd = @"</center>";
        private static String subcategoryStart = @"<div class=""item_2col"">";
        private static String subcategoryEnd = @"</div>";
        private static String subcategoryUrlRegex = @"<a href=""(?<subcategoryUrl>[^""]+)";
        private static String subcategoryThumbAndTitleRegex = @"<img src=""(?<subcategoryThumbUrl>[^""]+)"" alt=""(?<subcategoryTitle>[^""]+)";

        private static String videoSectionStart = @"if(youtube==0) {";
        private static String videoSectionEnd = @"}";
        private static String videoUrlRegex = @"so.addVariable\('(?<videoQuality>[^']+)','(?<videoUrl>[^']+)";

        #endregion

        #region Constructors

        public MuviCzUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "úvod",
                    Url = Utils.FormatAbsoluteUrl("", MuviCzUtil.baseUrl)
                });

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "videa",
                    Url = Utils.FormatAbsoluteUrl("/video/top20", MuviCzUtil.baseUrl)
                });

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "témata",
                    Url = Utils.FormatAbsoluteUrl("/temata", MuviCzUtil.baseUrl)
                });

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "pořady",
                    Url = Utils.FormatAbsoluteUrl("/porady/vsechny", MuviCzUtil.baseUrl),
                    HasSubCategories = true
                });

            this.Settings.DynamicCategoriesDiscovered = true;

            return this.Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int dynamicSubCategoriesCount = 0;
            RssLink category = (RssLink)parentCategory;

            String baseWebData = GetWebData(category.Url, null, null, null, true);

            int startIndex = baseWebData.IndexOf(MuviCzUtil.subcategoriesStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(MuviCzUtil.subcategoriesEnd, startIndex);
                if (endIndex >= 0)
                {
                    String subcategoriesData = baseWebData.Substring(startIndex, endIndex - startIndex);
                    category.SubCategories = new List<Category>();
                    category.SubCategoriesDiscovered = true;

                    while (true)
                    {
                        startIndex = endIndex = -1;

                        startIndex = subcategoriesData.IndexOf(MuviCzUtil.subcategoryStart);
                        if (startIndex >= 0)
                        {
                            endIndex = subcategoriesData.IndexOf(MuviCzUtil.subcategoryEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String title = String.Empty;
                                String thumbUrl = String.Empty;
                                String url = String.Empty;
                                String subcatData = subcategoriesData.Substring(startIndex, endIndex - startIndex);
                                subcategoriesData = subcategoriesData.Substring(endIndex);

                                Match match = Regex.Match(subcatData, MuviCzUtil.subcategoryUrlRegex);
                                if (match.Success)
                                {
                                    url = Utils.FormatAbsoluteUrl(match.Groups["subcategoryUrl"].Value, MuviCzUtil.baseUrl);
                                }

                                match = Regex.Match(subcatData, MuviCzUtil.subcategoryThumbAndTitleRegex);
                                if (match.Success)
                                {
                                    thumbUrl = Utils.FormatAbsoluteUrl(match.Groups["subcategoryThumbUrl"].Value, MuviCzUtil.baseUrl);
                                    title = match.Groups["subcategoryTitle"].Value;
                                }

                                if (String.IsNullOrEmpty(title) || String.IsNullOrEmpty(thumbUrl) || String.IsNullOrEmpty(url))
                                {
                                    break;
                                }

                                category.SubCategories.Add(
                                    new RssLink()
                                    {
                                        Name = title,
                                        HasSubCategories = false,
                                        Url = url,
                                        Thumb = thumbUrl
                                    });

                                dynamicSubCategoriesCount++;
                            }
                        }

                        if ((startIndex == (-1)) || (endIndex == (-1)))
                        {
                            break;
                        }
                    }
                }
            }

            return dynamicSubCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

                this.nextPageUrl = String.Empty;

                int nextPageStartIndex = baseWebData.IndexOf(MuviCzUtil.showEpisodeNextPageStart);
                if (nextPageStartIndex >= 0)
                {
                    int nextPageEndIndex = baseWebData.IndexOf(MuviCzUtil.showEpisodeNextPageEnd, nextPageStartIndex);

                    if (nextPageEndIndex >= 0)
                    {
                        String nextPageData = baseWebData.Substring(nextPageStartIndex, nextPageEndIndex - nextPageStartIndex);

                        MatchCollection matches = Regex.Matches(nextPageData, MuviCzUtil.showEpisodeNextPageRegex);
                        for (int i = 0; i < (matches.Count - 1); i++)
                        {
                            int pageValue = int.Parse(matches[i].Groups["nextPageValue"].Value);

                            if (pageValue != (i + 1))
                            {
                                this.nextPageUrl = Utils.FormatAbsoluteUrl(matches[i].Groups["nextPageUrl"].Value, pageUrl);
                                break;
                            }
                        }
                    }
                }

                int index = baseWebData.IndexOf(MuviCzUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    index = baseWebData.IndexOf(MuviCzUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            int startIndex = baseWebData.IndexOf(MuviCzUtil.showEpisodeStart);

                            if (startIndex >= 0)
                            {
                                int endIndex = baseWebData.IndexOf(MuviCzUtil.showEpisodeEnd, startIndex);

                                if (endIndex >= 0)
                                {
                                    String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);
                                    baseWebData = baseWebData.Substring(endIndex);

                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeThumb = String.Empty;
                                    String showEpisodeDescription = String.Empty;

                                    Match match = Regex.Match(episodeData, MuviCzUtil.showEpisodeThumbRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeThumb = Utils.FormatAbsoluteUrl(match.Groups["showEpisodeThumbUrl"].Value, MuviCzUtil.baseUrl);
                                        episodeData = episodeData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(episodeData, MuviCzUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = Utils.FormatAbsoluteUrl(HttpUtility.UrlDecode(match.Groups["showEpisodeUrl"].Value), MuviCzUtil.baseUrl);
                                        showEpisodeTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                                        episodeData = episodeData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(episodeData, MuviCzUtil.showEpisodeDescriptionRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDescription = HttpUtility.HtmlDecode(match.Groups["showEpisodeDescription"].Value);
                                        episodeData = episodeData.Substring(match.Index + match.Length);
                                    }

                                    if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDescription)))
                                    {
                                        break;
                                    }

                                    VideoInfo videoInfo = new VideoInfo()
                                    {
                                        Description = showEpisodeDescription,
                                        ImageUrl = showEpisodeThumb,
                                        Title = showEpisodeTitle,
                                        VideoUrl = showEpisodeUrl
                                    };

                                    pageVideos.Add(videoInfo);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category)
        {
            hasNextPage = false;
            String baseWebData = String.Empty;
            RssLink parentCategory = (RssLink)category;
            List<VideoInfo> videoList = new List<VideoInfo>();

            if (parentCategory.Name != this.currentCategory.Name)
            {
                this.currentStartIndex = 0;
                this.nextPageUrl = parentCategory.Url;
                this.loadedEpisodes.Clear();
            }

            if ((parentCategory.Other != null) && (this.currentCategory.Other != null))
            {
                String parentCategoryOther = (String)parentCategory.Other;
                String currentCategoryOther = (String)currentCategory.Other;

                if (parentCategoryOther != currentCategoryOther)
                {
                    this.currentStartIndex = 0;
                    this.nextPageUrl = parentCategory.Url;
                    this.loadedEpisodes.Clear();
                }
            }

            this.currentCategory = parentCategory;

            this.loadedEpisodes.AddRange(this.GetPageVideos(this.nextPageUrl));
            while (this.currentStartIndex < this.loadedEpisodes.Count)
            {
                videoList.Add(this.loadedEpisodes[this.currentStartIndex++]);
            }

            if (!String.IsNullOrEmpty(this.nextPageUrl))
            {
                hasNextPage = true;
            }

            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory);
        }

        public override bool HasNextPage
        {
            get
            {
                return this.hasNextPage;
            }
            protected set
            {
                this.hasNextPage = value;
            }
        }

        public override string getUrl(VideoInfo video)
        {
            String baseWebData = GetWebData(video.VideoUrl, null, null, null, true);
            video.PlaybackOptions = new Dictionary<String, String>();

            while (true)
            {
                int startIndex = -1;
                int endIndex = -1;

                startIndex = baseWebData.IndexOf(MuviCzUtil.videoSectionStart);
                if (startIndex >= 0)
                {
                    endIndex = baseWebData.IndexOf(MuviCzUtil.videoSectionEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String videoData = baseWebData.Substring(startIndex, endIndex - startIndex);
                        baseWebData = baseWebData.Substring(endIndex);

                        MatchCollection matches = Regex.Matches(videoData, MuviCzUtil.videoUrlRegex);
                        if (matches.Count > 0)
                        {
                            foreach (Match match in matches)
                            {
                                String quality = match.Groups["videoQuality"].Value;
                                String url = match.Groups["videoUrl"].Value;

                                if (quality.Contains("file"))
                                {
                                    video.PlaybackOptions.Add((quality == "file") ? "Medium quality" : "High quality", url);
                                }
                            }
                        }
                    }
                }

                if ((startIndex == (-1)) || (endIndex == (-1)))
                {
                    break;
                }
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return String.Empty;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            List<VideoInfo> videoList = this.getVideoList(new RssLink()
            {
                Name = "Search",
                Other = query,
                Url = String.Format(MuviCzUtil.searchQueryUrl, query)
            });

            List<ISearchResultItem> result = new List<ISearchResultItem>(videoList.Count);
            foreach (var video in videoList)
            {
                result.Add(video);
            }

            return result;
        }

        #endregion
    }
}
