using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class ApetitTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.apetitonline.cz/apetit-tv";
        private static String videoBaseUrl = @"http://www.apetitonline.cz/video/apetit_tv";

        private static String showEpisodesStart = @"<div class=""dalsi-videa-obsah"">";
        private static String showEpisodesEnd = @"<div id=""sidebar"">";
        private static String showEpisodeStart = @"<div class=""dalsi-video"">";
        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)"" alt=""[^""]*"" width=""[^""]*"" />";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)</a>";

        private static String showEpisodeNextPageRegex = @"<dd class=""next""><a href=""(?<nextPageUrl>[^""]+)"">Další strana</a></dd>";

        private static String showVideoUrlRegex = @"file: ""/video/apetit_tv/(?<showVideoUrl>[^""]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public ApetitTvUtil()
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
            int dynamicCategoriesCount = 1;

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "Apetit.tv",
                    Url = ApetitTvUtil.baseUrl
                });

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(ApetitTvUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    index = baseWebData.IndexOf(ApetitTvUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(ApetitTvUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;

                                Match match = Regex.Match(baseWebData, ApetitTvUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = match.Groups["showEpisodeThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, ApetitTvUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = HttpUtility.UrlDecode(match.Groups["showEpisodeUrl"].Value);
                                    showEpisodeTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)))
                                {
                                    break;
                                }

                                VideoInfo videoInfo = new VideoInfo()
                                {
                                    ImageUrl = Utils.FormatAbsoluteUrl(showEpisodeThumb, ApetitTvUtil.baseUrl),
                                    Title = showEpisodeTitle,
                                    VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, ApetitTvUtil.baseUrl)
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, ApetitTvUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, ApetitTvUtil.baseUrl);
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
            Match match = Regex.Match(baseWebData, ApetitTvUtil.showVideoUrlRegex);
            if (match.Success)
            {
                String url = Utils.FormatAbsoluteUrl(match.Groups["showVideoUrl"].Value, ApetitTvUtil.videoBaseUrl);
                return new MPUrlSourceFilter.HttpUrl(url) { UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0" }.ToString();
            }

            return String.Empty;
        }

        #endregion
    }
}
