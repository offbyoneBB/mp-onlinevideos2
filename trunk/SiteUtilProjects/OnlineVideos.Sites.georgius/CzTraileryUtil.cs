using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.georgius
{
    public class CzTraileryUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.cztrailery.cz/";

        private static String dynamicCategoryStart = @"<div id=""menu"">";
        private static String dynamicCategoryEnd = @"</ul>";

        private static String showStart = @"<li>";
        private static String showEnd = @"</li>";
        
		private static String showUrlTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""(?<showTitle>[^""]*)"">";

        private static String showEpisodes1Start = @"<div id=""maincontent"">";

        private static String showEpisode1BlockStart = @"<div class=""galleryitem"">";
        private static String showEpisode1BlockEnd = @"</h3>";

        private static String showEpisodes2Start = @"<div class=""azindex"">";

        private static String showEpisode2BlockStart = @"<li>";
        private static String showEpisode2BlockEnd = @"</li>";

        private static String showEpisodeThumb1UrlRegex = @"<img class=""thumbw"" src=""(?<showThumbUrl>[^""]*)";
        private static String showEpisodeUrlAndTitle1Regex = @"<a href=""(?<showUrl>[^""]*)"" rel=""[^""]*"" title=""[^""]*"">(?<showTitle>[^<]*)</a>";

        private static String showEpisodeUrlAndTitle2Regex = @"<a href=""(?<showUrl>[^""]*)"" ><span class=""head"">(?<showTitle>[^<]*)";

        private static String showEpisodeNextPage1Regex = @"<a href=""(?<nextPageUrl>[^""]*)"" class=""next"">&raquo;";
        private static String showEpisodeNextPage2Regex = @"<a href=""(?<nextPageUrl>[^""]*)""  title=""Next page"">&gt;";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        private static String videoBlockStart = @"<div class=""video"">";
        private static String videoBlockEnd = @"<div id=""lista"">";

        private static String searchQueryFormat = @"http://www.cztrailery.cz/?s={0}&x=0&y=0";

        /* first possibility of video url */
        private static String videoUrlBlock1Start = @"SWFObject(";
        private static String videoUrlBlock1End = @"</script>";
        private static String videoConfigUrlRegex = @"s1.addVariable\('file','(?<videoConfigUrl>[^']*)'\);";
        private static String videoSubtitleUrlRegex = @"s1.addVariable\('captions.file','(?<videoSubtitleUrl>[^']*)'\);";

        private static String locationUrlBlockStart = @"<location>";
        private static String locationUrlBlockEnd = @"</location>";

        private static String subtitlesUrlBlockStart = @"<jwplayer:captions.file>";
        private static String subtitlesUrlBlockEnd = @"</jwplayer:captions.file>";

        /* second possibility of video url */
        private static String videoUrlBlock2Start = @"<object";
        private static String videoUrlBlock2End = @"</object";
        private static String videoObjectUrlRegex = @"<param name=""movie"" value=""(?<videoObjectUrl>[^""]*)";

        private static String trailerAddictComUrlRegex = @"http://www.traileraddict.com/emd/(?<videoId>[0-9]*)";
        private static String trailerAddictComConfigVideoUrl = @"http://www.traileraddict.com/fvare.php?tid={0}";

        private static String trailerAddictComFileUrlBlockStart = @"fileurl=";
        private static String trailerAddictComFileUrlBlockEnd = @"&";

        /* third possibility of video url */
        private static String videoUrlBlock3Start = @"""http://stor.cztrailery.cz/mediaplayer/player.swf""";
        private static String videoUrlBlock3End = @">";

        private static String locationUrlBlock3Start = @"&file=";
        private static String locationUrlBlock3End = @"&";

        private static String subtitlesUrlBlock3Start = @"captions.file=";
        private static String subtitlesUrlBlock3End = @"&";


        #endregion

        #region Constructors

        public CzTraileryUtil()
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
            String pageUrl = CzTraileryUtil.baseUrl;

            String baseWebData = GetWebData(pageUrl, null, null, null, true);
            pageUrl = String.Empty;

            int startIndex = baseWebData.IndexOf(CzTraileryUtil.dynamicCategoryStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(CzTraileryUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        int showStartIndex = baseWebData.IndexOf(CzTraileryUtil.showStart);
                        if (showStartIndex >= 0)
                        {
                            int showEndIndex = baseWebData.IndexOf(CzTraileryUtil.showEnd, showStartIndex);
                            if (showEndIndex >= 0)
                            {
                                String showData = baseWebData.Substring(showStartIndex, showEndIndex - showStartIndex);

                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, CzTraileryUtil.showUrlTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, CzTraileryUtil.baseUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                {
                                    this.Settings.Categories.Add(new RssLink()
                                    {
                                        Name = showTitle,
                                        HasSubCategories = false,
                                        Url = showUrl
                                    });
                                    dynamicCategoriesCount++;
                                }
                            }

                            baseWebData = baseWebData.Substring(showStartIndex + CzTraileryUtil.showStart.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!String.IsNullOrEmpty(pageUrl))
                    {
                        this.Settings.Categories.Add(new NextPageCategory() { Url = pageUrl });
                    }
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
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(CzTraileryUtil.showEpisodes1Start);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    Match match = Regex.Match(baseWebData, CzTraileryUtil.showEpisodeNextPage1Regex);
                    if (match.Success)
                    {
                        this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, CzTraileryUtil.baseUrl);
                    }

                    while (true)
                    {
                        int showEpisodeBlockStart = baseWebData.IndexOf(CzTraileryUtil.showEpisode1BlockStart);
                        if (showEpisodeBlockStart >= 0)
                        {
                            int showEpisodeBlockEnd = baseWebData.IndexOf(CzTraileryUtil.showEpisode1BlockEnd, showEpisodeBlockStart);
                            if (showEpisodeBlockEnd >= 0)
                            {
                                String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                String showTitle = String.Empty;
                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;

                                match = Regex.Match(showData, CzTraileryUtil.showEpisodeThumb1UrlRegex);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, CzTraileryUtil.baseUrl);
                                }

                                match = Regex.Match(showData, CzTraileryUtil.showEpisodeUrlAndTitle1Regex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, CzTraileryUtil.baseUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                {
                                    VideoInfo videoInfo = new VideoInfo()
                                    {
                                        ImageUrl = showThumbUrl,
                                        Title = showTitle,
                                        VideoUrl = showUrl
                                    };

                                    pageVideos.Add(videoInfo);
                                }
                            }

                            baseWebData = baseWebData.Substring(showEpisodeBlockStart + CzTraileryUtil.showEpisode1BlockStart.Length);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                index = baseWebData.IndexOf(CzTraileryUtil.showEpisodes2Start);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    Match match = Regex.Match(baseWebData, CzTraileryUtil.showEpisodeNextPage2Regex);
                    if (match.Success)
                    {
                        this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, CzTraileryUtil.baseUrl);
                    }

                    while (true)
                    {
                        int showEpisodeBlockStart = baseWebData.IndexOf(CzTraileryUtil.showEpisode2BlockStart);
                        if (showEpisodeBlockStart >= 0)
                        {
                            int showEpisodeBlockEnd = baseWebData.IndexOf(CzTraileryUtil.showEpisode2BlockEnd, showEpisodeBlockStart);
                            if (showEpisodeBlockEnd >= 0)
                            {
                                String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                String showTitle = String.Empty;
                                String showUrl = String.Empty;

                                match = Regex.Match(showData, CzTraileryUtil.showEpisodeUrlAndTitle2Regex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, CzTraileryUtil.baseUrl);
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

                            baseWebData = baseWebData.Substring(showEpisodeBlockStart + CzTraileryUtil.showEpisode2BlockStart.Length);
                        }
                        else
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
            String baseWebData = GetWebData(video.VideoUrl, null, null, null, true);

            // select video url block
            int startIndex = baseWebData.IndexOf(CzTraileryUtil.videoBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CzTraileryUtil.videoBlockEnd, startIndex + CzTraileryUtil.videoBlockStart.Length);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);
                }
            }

            #region try first type url
            startIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock1Start);
            if (startIndex >= 0)
            {
                // first type url
                int endIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock1End, startIndex + CzTraileryUtil.videoUrlBlock1Start.Length);
                if (endIndex >= 0)
                {
                    String videoConfigUrlData = baseWebData.Substring(startIndex, endIndex - startIndex);
                    Match match = Regex.Match(videoConfigUrlData, CzTraileryUtil.videoConfigUrlRegex);

                    if (match.Success)
                    {
                        String subtitleUrl = String.Empty;
                        String videoUrl = String.Empty;

                        String videoConfigUrl = match.Groups["videoConfigUrl"].Value;

                        match = Regex.Match(videoConfigUrlData, CzTraileryUtil.videoSubtitleUrlRegex);
                        if (match.Success)
                        {
                            videoUrl = videoConfigUrl;
                            subtitleUrl = match.Groups["videoSubtitleUrl"].Value;
                        }

                        if (videoConfigUrl.Contains(".xml"))
                        {
                            String videoData = GetWebData(videoConfigUrl, null, null, null, true);

                            startIndex = videoData.IndexOf(CzTraileryUtil.subtitlesUrlBlockStart);
                            if (startIndex >= 0)
                            {
                                endIndex = videoData.IndexOf(CzTraileryUtil.subtitlesUrlBlockEnd, startIndex + CzTraileryUtil.subtitlesUrlBlockStart.Length);
                                if (endIndex >= 0)
                                {
                                    subtitleUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(HttpUtility.UrlDecode(videoData.Substring(startIndex + CzTraileryUtil.subtitlesUrlBlockStart.Length, endIndex - startIndex - CzTraileryUtil.subtitlesUrlBlockStart.Length))).ToString();
                                }
                            }

                            startIndex = videoData.IndexOf(CzTraileryUtil.locationUrlBlockStart);
                            if (startIndex >= 0)
                            {
                                endIndex = videoData.IndexOf(CzTraileryUtil.locationUrlBlockEnd, startIndex + CzTraileryUtil.locationUrlBlockStart.Length);
                                if (endIndex >= 0)
                                {
                                    videoUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(HttpUtility.UrlDecode(videoData.Substring(startIndex + CzTraileryUtil.locationUrlBlockStart.Length, endIndex - startIndex - CzTraileryUtil.locationUrlBlockStart.Length))).ToString();
                                }
                            }
                        }

                        if (videoConfigUrl.Contains("http://www.youtube.com"))
                        {
                            Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(videoConfigUrl);
                            if (playbackOptions != null)
                            {
                                video.PlaybackOptions = playbackOptions;
                            }
                        }

                        if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                        {
                            var enumer = video.PlaybackOptions.GetEnumerator();
                            enumer.MoveNext();
                            return enumer.Current.Value;
                        }

                        video.SubtitleUrl = subtitleUrl;
                        return videoUrl;
                    }
                }
            }
            #endregion

            #region try second type url
            startIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock2Start);
            if (startIndex >= 0)
            {
                // second type url
                int endIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock2End, startIndex + CzTraileryUtil.videoUrlBlock2Start.Length);
                if (endIndex >= 0)
                {
                    String videoObjectUrlData = baseWebData.Substring(startIndex, endIndex - startIndex);
                    Match match = Regex.Match(videoObjectUrlData, CzTraileryUtil.videoObjectUrlRegex);

                    if (match.Success)
                    {
                        String videoUrl = String.Empty;
                        String videoConfigUrl = match.Groups["videoObjectUrl"].Value;

                        if (videoConfigUrl.Contains("http://www.youtube.com"))
                        {
                            Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(videoConfigUrl);
                            if (playbackOptions != null)
                            {
                                video.PlaybackOptions = playbackOptions;
                            }

                            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                            {
                                var enumer = video.PlaybackOptions.GetEnumerator();
                                enumer.MoveNext();
                                return enumer.Current.Value;
                            }

                            return String.Empty;
                        }

                        match = Regex.Match(videoConfigUrl, CzTraileryUtil.trailerAddictComUrlRegex);
                        if (match.Success)
                        {
                            String videoId = match.Groups["videoId"].Value;
                            String videoData = GetWebData(String.Format(CzTraileryUtil.trailerAddictComConfigVideoUrl, videoId), null, null, null, true);

                            startIndex = videoData.IndexOf(CzTraileryUtil.trailerAddictComFileUrlBlockStart);
                            if (startIndex >= 0)
                            {
                                endIndex = videoData.IndexOf(CzTraileryUtil.trailerAddictComFileUrlBlockEnd, startIndex + CzTraileryUtil.trailerAddictComFileUrlBlockStart.Length);
                                if (endIndex >= 0)
                                {
                                    videoUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(HttpUtility.UrlDecode(videoData.Substring(startIndex + CzTraileryUtil.trailerAddictComFileUrlBlockStart.Length, endIndex - startIndex - CzTraileryUtil.trailerAddictComFileUrlBlockStart.Length))).ToString();
                                }
                            }
                        }

                        return videoUrl;
                    }
                }
            }
            #endregion

            #region try third type url
            startIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock3Start);
            if (startIndex >= 0)
            {
                // second type url
                int endIndex = baseWebData.IndexOf(CzTraileryUtil.videoUrlBlock3End, startIndex);
                if (endIndex >= 0)
                {
                    String videoObjectData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    String subtitleUrl = String.Empty;
                    String videoUrl = String.Empty;

                    startIndex = videoObjectData.IndexOf(CzTraileryUtil.subtitlesUrlBlock3Start);
                    if (startIndex >= 0)
                    {
                        endIndex = videoObjectData.IndexOf(CzTraileryUtil.subtitlesUrlBlock3End, startIndex + CzTraileryUtil.subtitlesUrlBlock3Start.Length);
                        if (endIndex >= 0)
                        {
                            subtitleUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(HttpUtility.UrlDecode(videoObjectData.Substring(startIndex + CzTraileryUtil.subtitlesUrlBlock3Start.Length, endIndex - startIndex - CzTraileryUtil.subtitlesUrlBlock3Start.Length))).ToString();
                        }
                    }

                    startIndex = videoObjectData.IndexOf(CzTraileryUtil.locationUrlBlock3Start);
                    if (startIndex >= 0)
                    {
                        endIndex = videoObjectData.IndexOf(CzTraileryUtil.locationUrlBlock3End, startIndex + CzTraileryUtil.locationUrlBlock3Start.Length);
                        if (endIndex >= 0)
                        {
                            videoUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(HttpUtility.UrlDecode(videoObjectData.Substring(startIndex + CzTraileryUtil.locationUrlBlock3Start.Length, endIndex - startIndex - CzTraileryUtil.locationUrlBlock3Start.Length))).ToString();
                        }
                    }

                    video.SubtitleUrl = subtitleUrl;
                    return videoUrl;
                }
            }
            #endregion

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
                Url = String.Format(CzTraileryUtil.searchQueryFormat, query)
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
