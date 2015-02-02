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
        private static String showEpisodesBlockEnd = @"<p id=""more""";

        private static String showEpisodeBlockStart = @"<div id=""clip";
        private static String showEpisodeBlockEnd = @"</h2>";

        private static String showEpisodeThumbUrlRegex = @"<img class=""[^""]*"" alt=""[^""]*"" src=""(?<showThumbUrl>[^""]+)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)""[^>]*>(?<showTitle>[^<]+)";
        private static String showEpisodeDataClipIdRegex = @"data-clip-id=""(?<showDataClipId>[^""]+)""";

        private static String videoUrlBlockStart = @"var cdn2";
        private static String videoUrlBlockEndFormat = @"id: Number('{0}')";

        private static String videoUrlQualityRegex = @"quality: '(?<videoQuality>[^']+)'";
        private static String videoUrlRegex = @"url: '(?<videoUrl>[^']+)'";
        private static String videoUrlFormatRegex = @"format: '(?<videoFormat>[^']+)'";

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
            String baseWebData = GetWebData(MixerCzUtil.baseUrl, forceUTF8: true);

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
                String baseWebData = GetWebData(pageUrl, forceUTF8: true);

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
                                    String episodeDataClipId = String.Empty;

                                    Match match = Regex.Match(episodeData, MixerCzUtil.showEpisodeDataClipIdRegex);
                                    if (match.Success)
                                    {
                                        episodeDataClipId = match.Groups["showDataClipId"].Value;
                                    }

                                    match = Regex.Match(episodeData, MixerCzUtil.showEpisodeThumbUrlRegex);
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
                                            ImageUrl = episodeThumbUrl,
                                            Other = episodeDataClipId
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
            String baseWebData = GetWebData(video.VideoUrl, forceUTF8: true);

            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }
            video.PlaybackOptions.Clear();

            int endIndex = baseWebData.LastIndexOf(String.Format(MixerCzUtil.videoUrlBlockEndFormat, video.Other));
            if (endIndex >= 0)
            {
                baseWebData = baseWebData.Substring(0, endIndex);

                int startIndex = baseWebData.LastIndexOf(MixerCzUtil.videoUrlBlockStart);
                if (startIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex);

                    String[] entries = baseWebData.Split(new String[] { "}" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var entry in entries)
                    {
                        String quality = String.Empty;
                        String url = String.Empty;
                        String format = String.Empty;

                        Match match = Regex.Match(entry, MixerCzUtil.videoUrlQualityRegex);
                        if (match.Success)
                        {
                            quality = match.Groups["videoQuality"].Value;
                        }

                        match = Regex.Match(entry, MixerCzUtil.videoUrlRegex);
                        if (match.Success)
                        {
                            url = match.Groups["videoUrl"].Value;
                        }

                        match = Regex.Match(entry, MixerCzUtil.videoUrlFormatRegex);
                        if (match.Success)
                        {
                            format = match.Groups["videoFormat"].Value;
                        }

                        if ((!String.IsNullOrEmpty(quality)) && (!String.IsNullOrEmpty(url)) && (!String.IsNullOrEmpty(format)))
                        {
                            video.PlaybackOptions.Add(String.Format("{0} | {1}", quality, format), url);
                        }
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
