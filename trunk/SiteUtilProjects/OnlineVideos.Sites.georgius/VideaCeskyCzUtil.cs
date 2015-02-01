using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace OnlineVideos.Sites.georgius
{
    public class VideaCeskyCzUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.videacesky.cz";

        private static String dynamicCategoryStart = @"<ul id=""headerMenu2"">";
        private static String dynamicCategoryEnd = @"<div class=""sidebars"">";
        private static String showStart = @"<li";
        private static String showUrlAndTitleRegex = @"(<a href=""(?<showUrl>[^""]+)"" class=""homeIcon"">(?<showTitle>[^<]+)</a>)|(<a href=""(?<showUrl>[^""]+)"" title=""[^""]*"">(?<showTitle>[^<]+)</a>)";

        private static String showEpisodesStart = @"<div class=""postHeader"">";
        private static String showEpisodesEnd = @"<script type=""text/javascript"">";
        private static String showEpisodeStart = @"<h2 class=""postTitle"">";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"" title=""[^""]*""><span>(?<showEpisodeTitle>[^<]+)";

        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)";
        private static String showEpisodeDescriptionStart = @"<div class=""obs"">";
        private static String showEpisodeDescriptionEnd = @"</div>";

        private static String showEpisodeNextPageRegex = @"<a href=""(?<nextPageUrl>[^""]+)""><span>Následující</span>";

        private static String optionTitleRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| (?<format>[a-z0-9]+) \([\s]*[0-9]+\)";
        private static String videoSectionStart = @"<div class=""postContent"">";
        private static String videoSectionEnd = @"</div>";
        private static String videoUrlRegex = @";file=(?<videoUrl>[^&]+)";
        private static String videoCaptionsRegex = @";captions.file=(?<videoUrl>[^&]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public VideaCeskyCzUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(VideaCeskyCzUtil.baseUrl);

            int index = baseWebData.IndexOf(VideaCeskyCzUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                index = baseWebData.IndexOf(VideaCeskyCzUtil.dynamicCategoryEnd);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);
                }

                while (true)
                {
                    index = baseWebData.IndexOf(VideaCeskyCzUtil.showStart);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        String showUrl = String.Empty;
                        String showTitle = String.Empty;

                        Match match = Regex.Match(baseWebData, VideaCeskyCzUtil.showUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
                        }

                        if ((String.IsNullOrEmpty(showUrl)) && (String.IsNullOrEmpty(showTitle)))
                        {
                            break;
                        }

                        this.Settings.Categories.Add(
                            new RssLink()
                            {
                                Name = showTitle,
                                Url = Utils.FormatAbsoluteUrl(showUrl, VideaCeskyCzUtil.baseUrl)
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
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int index = baseWebData.IndexOf(VideaCeskyCzUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    index = baseWebData.IndexOf(VideaCeskyCzUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(VideaCeskyCzUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;
                                String showEpisodeDescription = String.Empty;

                                Match match = Regex.Match(baseWebData, VideaCeskyCzUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = HttpUtility.UrlDecode(match.Groups["showEpisodeUrl"].Value);
                                    showEpisodeTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, VideaCeskyCzUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = match.Groups["showEpisodeThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                int showDescriptionStart = baseWebData.IndexOf(VideaCeskyCzUtil.showEpisodeDescriptionStart);
                                if (showDescriptionStart > 0)
                                {
                                    int showDescriptionEnd = baseWebData.IndexOf(VideaCeskyCzUtil.showEpisodeDescriptionEnd, showDescriptionStart);
                                    showEpisodeDescription = HttpUtility.HtmlDecode(baseWebData.Substring(showDescriptionStart + VideaCeskyCzUtil.showEpisodeDescriptionStart.Length, showDescriptionEnd - showDescriptionStart - VideaCeskyCzUtil.showEpisodeDescriptionStart.Length));
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
                                    VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, VideaCeskyCzUtil.baseUrl)
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, VideaCeskyCzUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = String.IsNullOrEmpty(nextPageMatch.Groups["nextPageUrl"].Value) ? String.Empty : Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, VideaCeskyCzUtil.baseUrl);
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl);
            String videoUrl = String.Empty;
            String captionsUrl = String.Empty;

            int index = baseWebData.IndexOf(VideaCeskyCzUtil.videoSectionStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                index = baseWebData.IndexOf(VideaCeskyCzUtil.videoSectionEnd);

                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);
                }

                Match match = Regex.Match(baseWebData, VideaCeskyCzUtil.videoUrlRegex);
                if (match.Success)
                {
                    videoUrl = match.Groups["videoUrl"].Value;
                }

                Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(HttpUtility.UrlDecode(videoUrl));
                if (playbackOptions != null)
                {
                    int width = 0;
                    String format = String.Empty;
                    String url = String.Empty;

                    foreach (var option in playbackOptions)
                    {
                        Match optionMatch = Regex.Match(option.Key, VideaCeskyCzUtil.optionTitleRegex);
                        if (optionMatch.Success)
                        {
                            int tempWidth = int.Parse(optionMatch.Groups["width"].Value);
                            String tempFormat = optionMatch.Groups["format"].Value;

                            if ((tempWidth > width) ||
                                ((tempWidth == width) && (tempFormat == "mp4")))
                            {
                                width = tempWidth;
                                url = option.Value;
                                format = tempFormat;
                            }
                        }
                    }

                    videoUrl = url;
                }

                match = Regex.Match(baseWebData, VideaCeskyCzUtil.videoCaptionsRegex);
                if (match.Success)
                {
                    captionsUrl = match.Groups["videoUrl"].Value;
                }

                video.SubtitleUrl = captionsUrl;
            }

            return videoUrl;
        }

        //public override bool CanSearch
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        //public override List<ISearchResultItem> Search(string query, string category = null)
        //{
        //    return this.getVideoList(new RssLink()
        //    {
        //        Name = "Search",
        //        Other = query,
        //        Url = String.Format(VideaCeskyCzUtil.searchQueryUrl, query)
        //    });
        //}

        #endregion
    }
}
