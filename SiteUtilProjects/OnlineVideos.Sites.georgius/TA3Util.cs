using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class TA3Util : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.ta3.com/archiv.html";
        private static String baseLiveUrl = "http://www.ta3.com/live.html";

        private static String dynamicCategoryStart = @"<div class=""archive"">";
        private static String dynamicCategoryEnd = @"<div class=""inside archive-filter"">";
        private static String categoryStartRegex = @"<a href=""(?<categoryUrl>[^<]*)"">(?<categoryTitle>[^<]*)</a>";

        private static String showsStart = @"<ul class=""items"">";
        private static String showsEnd = @"</ul>";

        private static String showBlockStartRegex = @"<li";
        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)""><span class=""vicon""></span>(?<showTitle>[^<]*)</a>";

        private static String showEpisodeNextPageStart = @"<li class=""next"">";
        private static String showEpisodeNextPageRegex = @"<a href=""(?<url>[^""]*)"">";

        private static String videoIdStart = @"arrTa3VideoPlayer.push";
        private static String videoIdEnd = @"));";
        private static String videoIdAndTypeRegex = @"(?<videoId>[0-9A-Z]{8}\-[0-9A-Z]{8}\-[0-9A-Z]{4}\-[0-9A-Z]{4}\-[0-9A-Z]{4}\-[0-9A-Z]{12})""\,[\s]*""(?<videoType>[0-9]*)";

        private static String playerOfflineUrl = @"http://embed.livebox.cz/ta3/player-offline.js";
        private static String playerOnlineUrl = @"http://embed.livebox.cz/ta3/player-live.js";

        // zero index is default (undefined)
        private static String[] videoType = { @"Videoteka/mp4:", @"Videoteka/mp4:", @"VideotekaEncoder/mp4:" };

        private static String videoUrlPrefixRegex = @"prefix:[\s]*'(?<prefix>[^']+)";
        private static String videoUrlPostfixRegex = @"postfix:[\s]*'(?<postfix>[^']+)";

        private static String videoIdLiveRegex = @"videoID0:[\s]*'(?<videoId>[^']+)";
        private static String videoLowIdLiveRegex = @"videoID1:[\s]*'(?<videoLowId>[^']+)";
        private static String videoMediumIdLiveRegex = @"videoID2:[\s]*'(?<videoMediumId>[^']+)";
        private static String videoHighIdLiveRegex = @"videoID3:[\s]*'(?<videoHighId>[^']+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public TA3Util()
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
            String baseWebData = GetWebData(TA3Util.baseUrl);

            int startIndex = baseWebData.IndexOf(TA3Util.dynamicCategoryStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(TA3Util.dynamicCategoryEnd, startIndex);

                baseWebData = (endIndex == (-1)) ? baseWebData.Substring(startIndex) : baseWebData.Substring(startIndex, endIndex - startIndex);

                Match match = Regex.Match(baseWebData, TA3Util.categoryStartRegex);
                while (match.Success)
                {
                    String categoryUrl = match.Groups["categoryUrl"].Value;
                    String categoryTitle = match.Groups["categoryTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = categoryTitle,
                            HasSubCategories = false,
                            Url = categoryUrl
                        });

                    dynamicCategoriesCount++;
                    match = match.NextMatch();
                }

                this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = "Live",
                            HasSubCategories = false,
                            Url = TA3Util.baseLiveUrl
                        });

                dynamicCategoriesCount++;
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                if (pageUrl == TA3Util.baseLiveUrl)
                {
                    this.nextPageUrl = String.Empty;
                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Title = "Live",
                        VideoUrl = pageUrl
                    };

                    pageVideos.Add(videoInfo);

                }
                else
                {
                    String baseWebData = GetWebData(pageUrl);
                    String shows = String.Empty;

                    int index = baseWebData.IndexOf(TA3Util.showsStart);
                    if (index > 0)
                    {
                        int endIndex = baseWebData.IndexOf(TA3Util.showsEnd, index);
                        shows = (endIndex == (-1)) ? baseWebData.Substring(index) : baseWebData.Substring(index, endIndex - index);
                    }

                    while (true)
                    {
                        Match showEpisodeBlockStart = Regex.Match(shows, TA3Util.showBlockStartRegex);
                        if (showEpisodeBlockStart.Success)
                        {
                            shows = shows.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                            String showTitle = String.Empty;
                            String showThumbUrl = String.Empty;
                            String showUrl = String.Empty;
                            String showLength = String.Empty;
                            String showDescription = String.Empty;

                            Match showEpisodeUrlAndTitle = Regex.Match(shows, TA3Util.showUrlAndTitleRegex);
                            if (showEpisodeUrlAndTitle.Success)
                            {
                                showUrl = showEpisodeUrlAndTitle.Groups["showUrl"].Value;
                                showTitle = HttpUtility.HtmlDecode(showEpisodeUrlAndTitle.Groups["showTitle"].Value);
                                shows = shows.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                            }

                            if (!(showEpisodeUrlAndTitle.Success))
                            {
                                break;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showDescription,
                                Thumb = showThumbUrl,
                                Length = showLength,
                                Title = showTitle,
                                VideoUrl = showUrl
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }

                    index = baseWebData.IndexOf(TA3Util.showEpisodeNextPageStart);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);
                        Match nextPageMatch = Regex.Match(baseWebData, TA3Util.showEpisodeNextPageRegex);
                        this.nextPageUrl = (nextPageMatch.Success) ? nextPageMatch.Groups["url"].Value : String.Empty;
                    }
                    else
                    {
                        this.nextPageUrl = String.Empty;
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
            String baseWebData = GetWebData(video.VideoUrl);

            String showUrl = String.Empty;
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }

            video.PlaybackOptions.Clear();

            if (video.VideoUrl != TA3Util.baseLiveUrl)
            {
                String playerOfflineWebData = GetWebData(TA3Util.playerOfflineUrl, referer: video.VideoUrl);

                int startIndex = baseWebData.IndexOf(TA3Util.videoIdStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(TA3Util.videoIdEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String videoId = String.Empty;
                        int videoType = 0;
                        String prefix = String.Empty;
                        String postfix = String.Empty;

                        String videoIdAndType = baseWebData.Substring(startIndex, endIndex - startIndex);
                        Match match = Regex.Match(videoIdAndType, TA3Util.videoIdAndTypeRegex);
                        if (match.Success)
                        {
                            videoId = match.Groups["videoId"].Value;
                            if (!String.IsNullOrEmpty(match.Groups["videoType"].Value))
                            {
                                videoType = int.Parse(match.Groups["videoType"].Value) + 1;
                            }
                        }

                        match = Regex.Match(playerOfflineWebData, TA3Util.videoUrlPrefixRegex);
                        if (match.Success)
                        {
                            prefix = match.Groups["prefix"].Value;
                        }

                        match = Regex.Match(playerOfflineWebData, TA3Util.videoUrlPostfixRegex);
                        if (match.Success)
                        {
                            postfix = match.Groups["postfix"].Value;
                        }

                        video.PlaybackOptions.Add("Low", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}_ta3d.mp4{3}", prefix, TA3Util.videoType[videoType], videoId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639" }.ToString());
                        video.PlaybackOptions.Add("Medium", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}_ta2d.mp4{3}", prefix, TA3Util.videoType[videoType], videoId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639" }.ToString());
                        video.PlaybackOptions.Add("High", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}_ta1d.mp4{3}", prefix, TA3Util.videoType[videoType], videoId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639" }.ToString());
                    }
                }
            }
            else
            {
                String playerOnlineWebData = GetWebData(TA3Util.playerOnlineUrl, referer: video.VideoUrl);

                String videoId = String.Empty;
                String videoLowId = String.Empty;
                String videoMediumId = String.Empty;
                String videoHighId = String.Empty;
                String prefix = String.Empty;
                String postfix = String.Empty;

                Match match = Regex.Match(playerOnlineWebData, TA3Util.videoIdLiveRegex);
                if (match.Success)
                {
                    videoId = match.Groups["videoId"].Value;
                }

                match = Regex.Match(playerOnlineWebData, TA3Util.videoLowIdLiveRegex);
                if (match.Success)
                {
                    videoLowId = match.Groups["videoLowId"].Value;
                }
                match = Regex.Match(playerOnlineWebData, TA3Util.videoMediumIdLiveRegex);
                if (match.Success)
                {
                    videoMediumId = match.Groups["videoMediumId"].Value;
                }
                match = Regex.Match(playerOnlineWebData, TA3Util.videoHighIdLiveRegex);
                if (match.Success)
                {
                    videoHighId = match.Groups["videoHighId"].Value;
                }

                match = Regex.Match(playerOnlineWebData, TA3Util.videoUrlPrefixRegex);
                if (match.Success)
                {
                    prefix = match.Groups["prefix"].Value;
                }

                match = Regex.Match(playerOnlineWebData, TA3Util.videoUrlPostfixRegex);
                if (match.Success)
                {
                    postfix = match.Groups["postfix"].Value;
                }

                if (!String.IsNullOrEmpty(videoLowId))
                {
                    video.PlaybackOptions.Add("Low", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}", prefix, videoLowId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639", LiveStream = true }.ToString());
                }
                if (!String.IsNullOrEmpty(videoMediumId))
                {
                    video.PlaybackOptions.Add("Medium", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}", prefix, videoMediumId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639", LiveStream = true }.ToString());
                }
                if (!String.IsNullOrEmpty(videoHighId))
                {
                    video.PlaybackOptions.Add("High", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}", prefix, videoHighId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639", LiveStream = true }.ToString());
                }

                if (video.PlaybackOptions.Count == 0)
                {
                    video.PlaybackOptions.Add("Auto", new OnlineVideos.MPUrlSourceFilter.HttpUrl(String.Format("{0}{1}{2}", prefix, videoId, postfix)) { Referer = "http://embed.livebox.cz/ta3/player.swf?nocache=1343671458639", LiveStream = true }.ToString());
                }
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                showUrl = enumer.Current.Value;
            }

            return showUrl;
        }

        #endregion
    }
}
