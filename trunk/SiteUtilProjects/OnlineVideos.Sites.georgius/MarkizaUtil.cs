using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.ComponentModel;

namespace OnlineVideos.Sites.georgius
{
    public class MarkizaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://video.markiza.sk/archiv-tv-markiza";
        private static String dynamicCategoryStart = @"<div id=""VagonContent"">";
        private static String showStart = @"<div class=""item"">";
        private static String showUrlRegex = @"<a href=""/archiv-tv-markiza/(?<showUrl>[^>]+)"">";
        private static String showThumbRegex = @"<img src=""(?<showThumbUrl>[^""]+)""";
        private static String showTitleRegex = @"<a href=""[^""]+"">(?<showTitle>[^<]+)</a>";

        private static String showEpisodesStart = @"<div id=""VagonContent"">";
        private static String showEpisodeStart = @"<div class=""item"">";
        private static String showEpisodeUrlRegex = @"<a href=""/archiv-tv-markiza/(?<showEpisodeUrl>[^>]+)"">";
        private static String showEpisodeThumbRegex = @"src=""(?<showEpisodeThumbUrl>[^""]+)""";
        private static String showEpisodeTitleRegex = @"<a href=""[^""]+"">(?<showEpisodeTitle>[^<]+)</a></div>";
        private static String showEpisodeDateRegex = @"<span>(?<showEpisodeDate>[^<]*)<br/>";

        private static String showEpisodeNextPageRegex = @"<div class=""right""><a href=""(?<nextPageUrl>[^""]+)"">[^<]+</a></div>";

        private static String showEpisodePlaylistUrlFormat = @"http://www.markiza.sk/js/flowplayer/config.js?&media={0}";

        private static String showEpisodePlaylistStart = @"""playlist"":[";
        private static String showEpisodePlaylistEnd = @"]";

        private static String showVideoUrlsRegex = @"""url"":""(?<showVideosUrl>[^""]+)""";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public MarkizaUtil()
            : base()
        {
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;
            String baseWebData = SiteUtilBase.GetWebData(MarkizaUtil.baseUrl);

            int index = baseWebData.IndexOf(MarkizaUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                while (true)
                {
                    index = baseWebData.IndexOf(MarkizaUtil.showStart);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        String showUrl = String.Empty;
                        String showTitle = String.Empty;
                        String showThumb = String.Empty;

                        Match match = Regex.Match(baseWebData, MarkizaUtil.showUrlRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(baseWebData, MarkizaUtil.showThumbRegex);
                        if (match.Success)
                        {
                            showThumb = match.Groups["showThumbUrl"].Value;
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(baseWebData, MarkizaUtil.showTitleRegex);
                        if (match.Success)
                        {
                            showTitle = match.Groups["showTitle"].Value;
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
                        }

                        if ((String.IsNullOrEmpty(showUrl)) && (String.IsNullOrEmpty(showThumb)) && (String.IsNullOrEmpty(showTitle)))
                        {
                            break;
                        }

                        this.Settings.Categories.Add(
                            new RssLink()
                            {
                                Name = showTitle,
                                Url = String.Format("{0}/{1}", MarkizaUtil.baseUrl, showUrl),
                                Thumb = showThumb
                            });
                        dynamicCategoriesCount++;
                    }
                    else
                    {
                        break;
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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int index = baseWebData.IndexOf(MarkizaUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    while (true)
                    {
                        index = baseWebData.IndexOf(MarkizaUtil.showEpisodeStart);

                        if (index > 0)
                        {
                            baseWebData = baseWebData.Substring(index);

                            String showEpisodeUrl = String.Empty;
                            String showEpisodeTitle = String.Empty;
                            String showEpisodeThumb = String.Empty;
                            String showEpisodeDate = String.Empty;

                            Match match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeUrlRegex);
                            if (match.Success)
                            {
                                showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeThumbRegex);
                            if (match.Success)
                            {
                                showEpisodeThumb = match.Groups["showEpisodeThumbUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeTitleRegex);
                            if (match.Success)
                            {
                                showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeDateRegex);
                            if (match.Success)
                            {
                                showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDate)))
                            {
                                break;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showEpisodeDate,
                                ImageUrl = showEpisodeThumb,
                                Title = showEpisodeTitle,
                                VideoUrl = String.Format("{0}/{1}", MarkizaUtil.baseUrl, showEpisodeUrl)
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, MarkizaUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = nextPageMatch.Groups["nextPageUrl"].Value;
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category, int videoCount)
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
            int addedVideos = 0;

            while (true)
            {
                while (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) && (addedVideos < videoCount))
                {
                    videoList.Add(this.loadedEpisodes[this.currentStartIndex + addedVideos]);
                    addedVideos++;
                }

                if (addedVideos < videoCount)
                {
                    List<VideoInfo> loadedVideos = this.GetPageVideos(this.nextPageUrl);

                    if (loadedVideos.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        this.loadedEpisodes.AddRange(loadedVideos);
                    }
                }
                else
                {
                    break;
                }
            }

            if (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) || (!String.IsNullOrEmpty(this.nextPageUrl)))
            {
                hasNextPage = true;
            }

            this.currentStartIndex += addedVideos;

            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category, MarkizaUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, MarkizaUtil.pageSize);
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

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> videoUrls = new List<string>();

            String showEpisodesId = video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("/") + 1);
            String configUrl = String.Format(MarkizaUtil.showEpisodePlaylistUrlFormat, showEpisodesId);
            String baseWebData = SiteUtilBase.GetWebData(configUrl);

            int start = baseWebData.IndexOf(MarkizaUtil.showEpisodePlaylistStart);
            if (start > 0)
            {
                int end = baseWebData.IndexOf(MarkizaUtil.showEpisodePlaylistEnd, start);
                if (end > 0)
                {
                    String showEpisodePlaylist = baseWebData.Substring(start, end - start);

                    MatchCollection matches = Regex.Matches(showEpisodePlaylist, MarkizaUtil.showVideoUrlsRegex);
                    foreach (Match tempMatch in matches)
                    {
                        videoUrls.Add(tempMatch.Groups["showVideosUrl"].Value);
                    }
                }
            }

            return videoUrls;
        }

        #endregion
    }
}
