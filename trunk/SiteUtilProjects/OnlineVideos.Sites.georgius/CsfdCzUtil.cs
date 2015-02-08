using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;
using System.Web;

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

        private static String showsNextPageRegex = @"<a class=""button"" href=""(?<nextPageUrl>[^""]+)"" rel=""nofollow"">starší";
        
        private static String showsBlockStart = @"var playlist = player.getPlaylist()";
        private static String showsBlockEnd = @"player.initialize()";

        private static String showBlockStart = @"var clip = playlist.addClip(";

        private static String showThumbUrl = @"""(?<showThumbUrl>[^""]+)"",";
        private static String showUrl = @"<a href=\\""(?<showUrl>[^>]+)>(?<showTitle>[^<]+)";

        private static String navigationStart = @"<li class=""selected";
        private static String navigationEnd = @"</ul>";

        private static String navigationItemStart = @"<li";
        private static String navigationItemEnd = @"</li>";
        private static String navigationItemRegex = @"<a href=""(?<navigationItemUrl>[^""]+)";

        private static String videoBlockStart = @"<video";
        private static String videoBlockEnd = @"<script";

        private static String videoThumbUrlRegex = @"poster=""(?<videoThumbUrl>[^""]+)";
        private static String videoTitleStart = @"<div class=""description flag"">";
        private static String videoTitleEnd = @"</div>";

        private static String videoNextPageRegex = @"<a class=""next"" href=""(?<nextPageUrl>[^""]+)"">následující";

        private static String videoUrlRegex = @"<source src=""(?<videoUrl>[^""]+)"" type=""video/(?<videoType>[^""]+)"" width=""(?<videoWidth>[^""]+)"" height=""(?<videoHeight>[^""]+)""";
        private static String subtitleUrlRegex = @"<track src=""(?<subtitleUrl>[^""]+)";

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
            String baseWebData = GetWebData(CsfdCzUtil.baseUrl, forceUTF8: true);

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

            String baseWebData = GetWebData(url, forceUTF8: true);

            int startIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockEnd, startIndex);
                if (endIndex >= 0)
                {
                    String baseData = baseWebData;
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockStart, startIndex + CsfdCzUtil.showBlockStart.Length);
                            if (endIndex == (-1))
                            {
                                endIndex = baseWebData.Length - 1;
                            }

                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, CsfdCzUtil.showThumbUrl);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value.Replace("\\/", "/"), url);
                                }

                                match = Regex.Match(showData, CsfdCzUtil.showUrl);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl("videa", Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value.Replace("\\/", "/").Replace("\\\"", ""), url));
                                    showTitle = Helpers.StringUtils.PlainTextFromHtml(Utils.DecodeEncodedNonAsciiCharacters(match.Groups["showTitle"].Value));
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

                    Match nextPageMatch = Regex.Match(baseData, CsfdCzUtil.showsNextPageRegex);
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
                List<String> navigationItems = new List<String>();
                String baseWebData = GetWebData(pageUrl, forceUTF8: true);

                int startIndex = baseWebData.IndexOf(CsfdCzUtil.navigationStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(CsfdCzUtil.navigationEnd, startIndex);
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

                int i = 0;
                while (i < navigationItems.Count)
                {
                    var navItem = navigationItems[i];
                    baseWebData = GetWebData(navItem, forceUTF8: true);

                    while (true)
                    {
                        startIndex = -1;
                        int endIndex = -1;

                        startIndex = baseWebData.IndexOf(CsfdCzUtil.videoBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.videoBlockEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);
                                baseWebData = baseWebData.Substring(endIndex);

                                startIndex = episodeData.IndexOf(CsfdCzUtil.videoTitleStart);
                                if (startIndex >= 0)
                                {
                                    endIndex = episodeData.IndexOf(CsfdCzUtil.videoTitleEnd, startIndex);
                                    if (endIndex >= 0)
                                    {
                                        String title = episodeData.Substring(startIndex + CsfdCzUtil.videoTitleStart.Length, endIndex - startIndex - CsfdCzUtil.videoTitleStart.Length);
                                        String videoThumbUrl = String.Empty;

                                        Match match = Regex.Match(episodeData, CsfdCzUtil.videoThumbUrlRegex);
                                        if (match.Success)
                                        {
                                            videoThumbUrl = match.Groups["videoThumbUrl"].Value;
                                        }

                                        pageVideos.Add(new VideoInfo() { Title = title, Other = episodeData, VideoUrl = CsfdCzUtil.baseUrl, Thumb = videoThumbUrl });
                                    }
                                }
                            }
                        }

                        if ((startIndex == (-1)) || (endIndex == (-1)))
                        {
                            break;
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, CsfdCzUtil.videoNextPageRegex);
                    if (nextPageMatch.Success)
                    {
                        navigationItems.Add(Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, CsfdCzUtil.baseUrl));
                    }

                    i++;
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> GetNextPageVideos()
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

        public override string GetVideoUrl(VideoInfo video)
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
                String url = matches[i].Groups["videoUrl"].Value;
                String type = matches[i].Groups["videoType"].Value;
                String width = matches[i].Groups["videoWidth"].Value;
                String height = matches[i].Groups["videoHeight"].Value;

                video.PlaybackOptions.Add(String.Format("{0} ({1}x{2})", type, width, height), url);
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
