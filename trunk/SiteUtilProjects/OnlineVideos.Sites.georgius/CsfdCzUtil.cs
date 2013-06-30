using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;

namespace OnlineVideos.Sites.georgius
{
    public sealed class CsfdCzUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.csfd.cz/videa/";

        private static String dynamicCategoryStart = @"<select name=""filter""";
        private static String dynamicCategoryEnd = @"<noscript>";
        private static String dynamicCategoryRegex = @"(<option value=""(?<dynamicCategoryUrl>[^""]+)"" selected=""selected"">(?<dynamicCategoryTitle>[^<]+))|(<option value=""(?<dynamicCategoryUrl>[^""]+)"">(?<dynamicCategoryTitle>[^<]+))";

        private static String dynamicCategoryUrlFormat = "http://www.csfd.cz/videa/filtr-{0}";

        private static String showsBlockStart = @"<ul class=""ui-image-list"">";
        private static String showsBlockEnd = @"<div class=""footer"">";

        private static String showsNextPageRegex = @"<a class=""button"" href=""(?<nextPageUrl>[^""]+)"" rel=""nofollow"">starší &gt;</a>";
        
        private static String showBlockStart = @"<li>";
        private static String showBlockEnd = @"</li>";

        private static String showThumbUrl = @"<img src=""(?<showThumbUrl>[^""]+)";
        private static String showUrl = @"<a href=""(?<showUrl>[^""]+)"" class=""[^""]*"">(?<showTitle>[^<]+)";

        private static String navigationStart = @"<h2 class=""header"">";
        private static String navigationEnd = @"</div>";

        private static String navigationItemStart = @"<li";
        private static String navigationItemEnd = @"</li>";
        private static String navigationItemRegex = @"<a href=""(?<navigationItemUrl>[^""]+)";

        private static String videoBlockStart = @"<div class=""ui-video-player"">";
        private static String videoBlockEnd = @"</script>";
        private static String videoSubBlockStart = @"<script";

        private static String videoTitleStart = @"<div class=""description"">";
        private static String videoTitleEnd = @"</div>";

        private static String videoUrlRegex = @"""src"":""(?<videoUrl>[^""]+)";

        private static String videoTypeRegex = @"""type"":""(?<videoType>[^""]+)";
        private static String videoQualityRegex = @"""quality"":""(?<videoQuality>[^""]+)";
        private static String videoWidthRegex = @"""width"":""(?<videoWidth>[^""]+)";
        private static String videoHeightRegex = @"""height"":""(?<videoHeight>[^""]+)";

        private static String subtitleUrlRegex = @"subtitles"":\[\{""src"":""(?<subtitleUrl>[^""]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public CsfdCzUtil()
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
            int dynamicCategoriesCount = 0;
            String baseWebData = SiteUtilBase.GetWebData(CsfdCzUtil.baseUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(CsfdCzUtil.dynamicCategoryStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CsfdCzUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match match = Regex.Match(baseWebData, CsfdCzUtil.dynamicCategoryRegex);
                    while (match.Success)
                    {
                        String dynamicCategoryUrl = match.Groups["dynamicCategoryUrl"].Value;
                        String dynamicCategoryTitle = match.Groups["dynamicCategoryTitle"].Value;

                        this.Settings.Categories.Add(
                            new RssLink()
                            {
                                Name = dynamicCategoryTitle,
                                HasSubCategories = true,
                                Url = String.Format(CsfdCzUtil.dynamicCategoryUrlFormat, dynamicCategoryUrl)
                            });

                        dynamicCategoriesCount++;
                        match = match.NextMatch();
                    }
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int showsCount = 0;
            String url = ((RssLink)parentCategory).Url;
            if (parentCategory.ParentCategory != null)
            {
                parentCategory = parentCategory.ParentCategory;
                // last category is next category, remove it
                parentCategory.SubCategories.RemoveAt(parentCategory.SubCategories.Count - 1);
            }
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
            }
            RssLink category = (RssLink)parentCategory;

            Hashtable titles = new Hashtable();
            foreach (var cat in category.SubCategories)
            {
                titles.Add(cat.Name, null);
            }

            String baseWebData = SiteUtilBase.GetWebData(url, null, null, null, true);

            int startIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, CsfdCzUtil.showThumbUrl);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, url);
                                }

                                match = Regex.Match(showData, CsfdCzUtil.showUrl);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl("videa", Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, url));
                                    showTitle = OnlineVideos.Utils.PlainTextFromHtml(match.Groups["showTitle"].Value);
                                }

                                if (!String.IsNullOrEmpty(showUrl))
                                {
                                    if (!titles.ContainsKey(showTitle))
                                    {
                                        category.SubCategories.Add(new RssLink()
                                        {
                                            Name = showTitle,
                                            Thumb = showThumbUrl,
                                            Url = showUrl,
                                            HasSubCategories = false
                                        });
                                        showsCount++;
                                        titles.Add(showTitle, null);
                                    }
                                }

                                baseWebData = baseWebData.Substring(endIndex);
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

                    Match nextPageMatch = Regex.Match(baseWebData, CsfdCzUtil.showsNextPageRegex);
                    if (nextPageMatch.Success)
                    {
                        String nextPageUrl = Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, url);
                        parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextPageUrl, ParentCategory = parentCategory });
                        showsCount++;
                    }
                }
            }

            if (showsCount > 0)
            {
                parentCategory.SubCategoriesDiscovered = true;
            }

            return showsCount;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = CsfdCzUtil.GetWebData(pageUrl, null, null, null, true);
                List<String> navigationItems = new List<String>();

                int startIndex = baseWebData.IndexOf(CsfdCzUtil.navigationStart);
                int endIndex = -1;
                if (startIndex >= 0)
                {
                    endIndex = baseWebData.IndexOf(CsfdCzUtil.navigationEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String navigation = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            startIndex = endIndex = -1;

                            startIndex = navigation.IndexOf(CsfdCzUtil.navigationItemStart);
                            if (startIndex >= 0)
                            {
                                endIndex = navigation.IndexOf(CsfdCzUtil.navigationItemEnd, startIndex);
                                if (endIndex >= 0)
                                {
                                    String item = navigation.Substring(startIndex, endIndex - startIndex);
                                    Match match = Regex.Match(item, CsfdCzUtil.navigationItemRegex);
                                    if (match.Success)
                                    {
                                        navigationItems.Add(Utils.FormatAbsoluteUrl(match.Groups["navigationItemUrl"].Value, CsfdCzUtil.baseUrl));
                                    }

                                    navigation = navigation.Substring(endIndex + CsfdCzUtil.navigationItemEnd.Length);
                                }
                            }

                            if ((startIndex == (-1)) || (endIndex == (-1)))
                            {
                                break;
                            }
                        }
                    }
                }

                foreach (var navItem in navigationItems)
                {
                    baseWebData = CsfdCzUtil.GetWebData(navItem, null, null, null, true);

                    while (true)
                    {
                        startIndex = endIndex = -1;

                        startIndex = baseWebData.IndexOf(CsfdCzUtil.videoBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.videoBlockEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                int subBlockStart = baseWebData.IndexOf(CsfdCzUtil.videoSubBlockStart, startIndex);
                                int titleStart = baseWebData.IndexOf(CsfdCzUtil.videoTitleStart, startIndex);

                                if ((titleStart >= 0) && (subBlockStart >= 0))
                                {
                                    int titleEnd = baseWebData.IndexOf(CsfdCzUtil.videoTitleEnd, titleStart);
                                    if (titleEnd >= 0)
                                    {
                                        String title = baseWebData.Substring(titleStart + CsfdCzUtil.videoTitleStart.Length, titleEnd - titleStart - CsfdCzUtil.videoTitleStart.Length);

                                        pageVideos.Add(new VideoInfo() { Title = title, Other = baseWebData.Substring(subBlockStart, endIndex - subBlockStart), VideoUrl = CsfdCzUtil.baseUrl });
                                    }
                                }

                                baseWebData = baseWebData.Substring(endIndex);
                            }
                        }

                        if ((startIndex == (-1)) || (endIndex == (-1)))
                        {
                            break;
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
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }

            video.PlaybackOptions.Clear();

            String videoData = (String)video.Other;

            MatchCollection matches = Regex.Matches(videoData, CsfdCzUtil.videoUrlRegex);
            for (int i = 0; i < matches.Count; i++)
            {
                String url = matches[i].Groups["videoUrl"].Value.Replace("\\/", "/");
                String extension = Path.GetExtension(url).Replace(".", "");
                String fileName = Path.GetFileNameWithoutExtension(url);

                if (!url.Contains("/ads/"))
                {
                    String item = videoData.Substring(matches[i].Index);

                    String videoType = String.Empty;
                    String quality = String.Empty;
                    String width = String.Empty;
                    String height = String.Empty;

                    Match match = Regex.Match(item, videoTypeRegex);
                    if (match.Success)
                    {
                        videoType = match.Groups["videoType"].Value;
                    }

                    match = Regex.Match(item, videoQualityRegex);
                    if (match.Success)
                    {
                        quality = match.Groups["videoQuality"].Value;
                    }

                    match = Regex.Match(item, videoWidthRegex);
                    if (match.Success)
                    {
                        width = match.Groups["videoWidth"].Value;
                    }

                    match = Regex.Match(item, videoHeightRegex);
                    if (match.Success)
                    {
                        height = match.Groups["videoHeight"].Value;
                    }

                    video.PlaybackOptions.Add(String.Format("{0} {1} {2}x{3}", videoType, quality, width, height), url);
                }
            }

            Match subtitleMatch = Regex.Match(videoData, CsfdCzUtil.subtitleUrlRegex);
            if (subtitleMatch.Success)
            {
                video.SubtitleUrl = System.Web.HttpUtility.HtmlDecode(subtitleMatch.Groups["subtitleUrl"].Value).Replace(@"\/", "/");
            }

            if (video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }

            return String.Empty;
        }

        #endregion
    }
}
