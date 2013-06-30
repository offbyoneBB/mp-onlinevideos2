using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.georgius
{
    public sealed class BarrandovTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.barrandov.tv/video";

        private static String dynamicCategoryStart = @"<div id=""right-menu"">";
        private static String dynamicCategoryEnd = @"</ul>";

        private static String showStart = @"<li";
        private static String showEnd = @"</li>";

        private static String showTitleAndUrlRegex = @"<a href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)";

        private static String showEpisodesStart = @"<div class=""block video show-archive"">";
        private static String showEpisodesEnd = @"<div id=""right-menu"">";

        private static String showEpisodeStart = @"<div class=""item";
        private static String showEpisodeEnd = @"</div>";

        private static String showEpisodeTitleAndUrlRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)";
        private static String showEpisodeDateRegex = @"<p class=""desc"">(?<showEpisodeDate>[^<]*)";
        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)";

        private static String showEpisodeNextPageRegex = @"<a href=""(?<nextPageUrl>[^""]+)"" class=""next"">další videa";

        private static String showVideoStart = @"jwplayer(""videoBox"").setup";
        private static String showVideoEnd = @"</script>";

        private static String showVideoUrlAndLabelRegex = @"file: ""(?<showVideoUrl>[^""]+)"",label: ""(?<showVideoLabel>[^""]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public BarrandovTvUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(BarrandovTvUtil.baseUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(BarrandovTvUtil.dynamicCategoryStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(BarrandovTvUtil.dynamicCategoryEnd, startIndex + BarrandovTvUtil.dynamicCategoryStart.Length);

                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(BarrandovTvUtil.showStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEnd, startIndex + BarrandovTvUtil.showStart.Length);

                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, BarrandovTvUtil.showTitleAndUrlRegex);
                                if (match.Success)
                                {
                                    showTitle = match.Groups["showTitle"].Value;
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, BarrandovTvUtil.baseUrl);
                                }

                                if (!((String.IsNullOrEmpty(showUrl)) || (String.IsNullOrEmpty(showTitle))))
                                {
                                    this.Settings.Categories.Add(
                                    new RssLink()
                                    {
                                        Name = showTitle,
                                        Url = showUrl
                                    });
                                    dynamicCategoriesCount++;
                                }

                                baseWebData = baseWebData.Substring(endIndex + BarrandovTvUtil.showEnd.Length);
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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesEnd, startIndex + BarrandovTvUtil.showEpisodesStart.Length);

                    if (endIndex > 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        Match nextPageMatch = Regex.Match(baseWebData, BarrandovTvUtil.showEpisodeNextPageRegex);
                        this.nextPageUrl = (nextPageMatch.Success) ? Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(nextPageMatch.Groups["nextPageUrl"].Value), pageUrl) : String.Empty;

                        while (true)
                        {
                            startIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodeStart);

                            if (startIndex >= 0)
                            {
                                endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodeEnd, startIndex + BarrandovTvUtil.showEpisodeStart.Length);

                                if (endIndex >= 0)
                                {
                                    String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeThumb = String.Empty;
                                    String showEpisodeDate = String.Empty;

                                    Match match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeTitleAndUrlRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                    }

                                    match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeThumbRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeThumb = Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(match.Groups["showEpisodeThumbUrl"].Value), BarrandovTvUtil.baseUrl);
                                    }

                                    match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                    }

                                    if (!((String.IsNullOrEmpty(showEpisodeUrl)) || (String.IsNullOrEmpty(showEpisodeThumb)) || (String.IsNullOrEmpty(showEpisodeTitle)) || (String.IsNullOrEmpty(showEpisodeDate))))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            Description = showEpisodeDate,
                                            ImageUrl = Utils.FormatAbsoluteUrl(showEpisodeThumb, BarrandovTvUtil.baseUrl),
                                            Title = showEpisodeTitle,
                                            VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, BarrandovTvUtil.baseUrl)
                                        };

                                        pageVideos.Add(videoInfo);
                                    }

                                    baseWebData = baseWebData.Substring(endIndex + BarrandovTvUtil.showEpisodeEnd.Length);
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl);

            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }
            video.PlaybackOptions.Clear();

            int startIndex = baseWebData.IndexOf(BarrandovTvUtil.showVideoStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(BarrandovTvUtil.showVideoEnd, startIndex);
                if (endIndex >= 0)
                {
                    String videoData = baseWebData.Substring(startIndex, endIndex - startIndex).Replace("\r", "").Replace("\n", "");

                    MatchCollection matches = Regex.Matches(videoData, BarrandovTvUtil.showVideoUrlAndLabelRegex);

                    foreach (Match match in matches)
                    {
                        video.PlaybackOptions.Add(match.Groups["showVideoLabel"].Value, Utils.FormatAbsoluteUrl(match.Groups["showVideoUrl"].Value, BarrandovTvUtil.baseUrl));
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
