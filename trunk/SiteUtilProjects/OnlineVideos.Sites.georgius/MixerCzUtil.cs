using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace OnlineVideos.Sites.georgius
{
    public sealed class MixerCzUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.mixer.cz";

        private static String dynamicCategoryStart = @"<ul id=""playlists";
        private static String dynamicCategoryEnd = @"</ul>";
        private static String dynamicCategoryRegex = @"<a href=""(?<dynamicCategoryUrl>[^""]+)"" id=""[^""]*"" class=""[^""]*"" data-playlist-id=""[^""]*"" data-key=""[^""]*"" title=""(?<dynamicCategoryTitle>[^""]+)""";

        private static String showEpisodesBlockStart = @"<div id=""list"">";
        private static String showEpisodesBlockEnd = @"<form";

        private static String showEpisodeBlockStart = @"<div id=""clip";
        private static String showEpisodeBlockEnd = @"</h2>";

        private static String showEpisodeThumbUrlRegex = @"<img class=""[^""]*"" alt=""[^""]*"" src=""(?<showThumbUrl>[^""]+)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)""[^>]*>(?<showTitle>[^<]+)";

        private static String videoUrlBlockStart = @"cdn_qualities.push";
        private static String videoUrlBlockEnd = @"var interpreters";
        private static String videoUrlRegex = @"Number\('(?<videoUrl>[^']+)'\),[\s]*quality:[\s]*'(?<videoQuality>[^']+)'";
        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public MixerCzUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(MixerCzUtil.baseUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(MixerCzUtil.dynamicCategoryStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(MixerCzUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match match = Regex.Match(baseWebData, MixerCzUtil.dynamicCategoryRegex);
                    while (match.Success)
                    {
                        String dynamicCategoryUrl = match.Groups["dynamicCategoryUrl"].Value;
                        String dynamicCategoryTitle = match.Groups["dynamicCategoryTitle"].Value;

                        this.Settings.Categories.Add(
                            new RssLink()
                            {
                                Name = dynamicCategoryTitle,
                                HasSubCategories = false,
                                Url = Utils.FormatAbsoluteUrl(dynamicCategoryUrl, MixerCzUtil.baseUrl)
                            });

                        dynamicCategoriesCount++;
                        match = match.NextMatch();
                    }
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = CsfdCzUtil.GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(MixerCzUtil.showEpisodesBlockStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(MixerCzUtil.showEpisodesBlockEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            startIndex = baseWebData.IndexOf(MixerCzUtil.showEpisodeBlockStart);
                            if (startIndex >= 0)
                            {
                                endIndex = baseWebData.IndexOf(MixerCzUtil.showEpisodeBlockEnd, startIndex);
                                if (endIndex >= 0)
                                {
                                    String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);
                                    String episodeTitle = String.Empty;
                                    String episodeUrl = String.Empty;
                                    String episodeThumbUrl = String.Empty;

                                    Match match = Regex.Match(episodeData, MixerCzUtil.showEpisodeThumbUrlRegex);
                                    if (match.Success)
                                    {
                                        episodeThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, pageUrl); ;
                                    }

                                    match = Regex.Match(episodeData, MixerCzUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        episodeTitle = OnlineVideos.Utils.PlainTextFromHtml(match.Groups["showTitle"].Value);
                                        episodeUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, pageUrl);
                                    }

                                    if ((!String.IsNullOrEmpty(episodeUrl)) && (!String.IsNullOrEmpty(episodeTitle)))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            Title = episodeTitle,
                                            VideoUrl = episodeUrl,
                                            ImageUrl = episodeThumbUrl
                                        };

                                        pageVideos.Add(videoInfo);
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

                        //Match nextPageUrlMatch = Regex.Match(baseWebData, MixerCzUtil.nextPageUrlRegex);
                        //if (nextPageUrlMatch.Success)
                        //{
                        //}
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

            int startIndex = baseWebData.IndexOf(MixerCzUtil.videoUrlBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(MixerCzUtil.videoUrlBlockEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match match = Regex.Match(baseWebData, MixerCzUtil.videoUrlRegex);
                    while (match.Success)
                    {
                        String url = new MPUrlSourceFilter.HttpUrl(SiteUtilBase.GetRedirectedUrl(String.Format(MixerCzUtil.videoUrlFormat, match.Groups["videoUrl"].Value))).ToString();
                        String quality = match.Groups["videoQuality"].Value;

                        video.PlaybackOptions.Add(quality, url);
                        match = match.NextMatch();
                    }
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

        #endregion
    }
}
