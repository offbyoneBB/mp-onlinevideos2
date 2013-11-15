using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;

namespace OnlineVideos.Sites.georgius
{
    public sealed class StreamCzUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.stream.cz/porady";

        private static String dynamicCategoryStart = @"<ul id=""listShows";
        private static String dynamicCategoryEnd = @"</ul>";

        private static String showStart = @"<li";
        private static String showEnd = @"</li>";

        private static String showUrlTitleRegex = @"<a href=""(?<showUrl>[^""]+)"" class[^>]*>(?<showTitle>[^<]+)";
        private static String showThumbRegex = @"<img src=""(?<showThumbUrl>[^""]+)";

        private static String showEpisodesStart = @"<div id=""episodesBox";
        private static String showEpisodesEnd = @"<div id=""seriesBoxPlace";

        private static String showEpisodeBlockStart = @"<li";
        private static String showEpisodeBlockEnd = @"</li";

        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)"" data[^>]*>(?<showTitle>[^<]+)";

        private static String streamSectionStart = @"Stream.Data.Episode";
        private static String streamSectionEnd = @"</script>";

        private static String streamRegex = @"{""source"": ""(?<streamUrl>[^""]+)"", ""type"": ""(?<streamType>[^""]*)"", ""quality_label"": ""(?<streamQualityLabel>[^""]*)"", ""quality"": ""(?<streamQuality>[^""]*)""}";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public StreamCzUtil()
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
            String pageUrl = StreamCzUtil.baseUrl;

            List<RssLink> unsortedCategories = new List<RssLink>();

            String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(StreamCzUtil.dynamicCategoryStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(StreamCzUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        int showStartIndex = baseWebData.IndexOf(StreamCzUtil.showStart);
                        if (showStartIndex >= 0)
                        {
                            int showEndIndex = baseWebData.IndexOf(StreamCzUtil.showEnd, showStartIndex);
                            if (showEndIndex >= 0)
                            {
                                String showData = baseWebData.Substring(showStartIndex, showEndIndex - showStartIndex);

                                String showUrl = String.Empty;
                                String showTitle = String.Empty;
                                String showThumbUrl = String.Empty;

                                Match match = Regex.Match(showData, StreamCzUtil.showUrlTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, StreamCzUtil.baseUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                match = Regex.Match(showData, StreamCzUtil.showThumbRegex);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, StreamCzUtil.baseUrl);
                                }

                                if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                {
                                    unsortedCategories.Add(new RssLink()
                                    {
                                        Name = showTitle,
                                        HasSubCategories = false,
                                        Url = showUrl,
                                        Thumb = showThumbUrl
                                    });
                                    dynamicCategoriesCount++;
                                }
                            }

                            baseWebData = baseWebData.Substring(showStartIndex + StreamCzUtil.showStart.Length);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (dynamicCategoriesCount > 0)
            {
                foreach (var item in unsortedCategories.OrderBy(i => i.Name))
                {
                    this.Settings.Categories.Add(item);
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();
            this.nextPageUrl = String.Empty;

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(StreamCzUtil.showEpisodesStart);
                if (startIndex > 0)
                {
                    int endIndex = baseWebData.IndexOf(StreamCzUtil.showEpisodesEnd, startIndex);
                    if (endIndex > 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            int showEpisodeBlockStart = baseWebData.IndexOf(StreamCzUtil.showEpisodeBlockStart);
                            if (showEpisodeBlockStart >= 0)
                            {
                                int showEpisodeBlockEnd = baseWebData.IndexOf(StreamCzUtil.showEpisodeBlockEnd, showEpisodeBlockStart);
                                if (showEpisodeBlockEnd >= 0)
                                {
                                    String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                    String showTitle = String.Empty;
                                    String showUrl = String.Empty;

                                    Match match = Regex.Match(showData, StreamCzUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, StreamCzUtil.baseUrl);
                                        showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                    }

                                    if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            Title = showTitle,
                                            VideoUrl = showUrl
                                        };

                                        pageVideos.Add(videoInfo);
                                    }
                                }

                                baseWebData = baseWebData.Substring(showEpisodeBlockStart + StreamCzUtil.showEpisodeBlockStart.Length);
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }
            video.PlaybackOptions.Clear();

            int startIndex = baseWebData.IndexOf(StreamCzUtil.streamSectionStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(StreamCzUtil.streamSectionEnd, startIndex);
                if (endIndex > 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    MatchCollection matches = Regex.Matches(baseWebData, StreamCzUtil.streamRegex);

                    foreach (Match match in matches)
                    {
                        // streamUrl
                        // streamQuality
                        video.PlaybackOptions.Add(String.Format("{0} {1}", match.Groups["streamQuality"].Value, match.Groups["streamType"].Value), match.Groups["streamUrl"].Value);
                    }
                }
            }

            if (video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        #endregion
    }
}
