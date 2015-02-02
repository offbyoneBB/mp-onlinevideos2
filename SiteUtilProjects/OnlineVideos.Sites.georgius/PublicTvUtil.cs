using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.SK_CZ
{
    public sealed class PublicTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.publictv.cz";
        private static String archiveUrl = @"http://www.publictv.cz/cz/menu/3/videoarchiv/";

        private static String dynamicCategoryStart = @"<h3>Pořady</h3>";
        private static String showStart = @"<li>";
        private static String showUrlAndTitleRegex = @"<a href=""/cz/menu/3/videoarchiv/(?<showUrl>[^"">]+)"">(?<showTitle>[^<]+)</a>";

        private static String showEpisodesStart = @"<h3 style=""[^""]*"">Další videa</h3>";
        private static String showEpisodesEnd = @"<p class=""clear-10px"">&nbsp;</p>";
        private static String showEpisodeStart = @"<div style=""";
        private static String showEpisodeEnd = @"</p></div>";
        private static String showEpisodeDateRegex = @"<strong>(?<showEpisodeDate>[^-]*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""/cz/menu/3/videoarchiv/(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)</a>";

        private static String showEpisodeVideoUrl = @"file:'(?<showEpisodeVideoUrl>[^']+)'";

        // the number of show episodes per page
        private static int pageSize = int.MaxValue;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public PublicTvUtil()
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
            String baseWebData = GetWebData(PublicTvUtil.archiveUrl, null, null, null, true);

            int index = baseWebData.IndexOf(PublicTvUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                while (true)
                {
                    index = baseWebData.IndexOf(PublicTvUtil.showStart);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        String showUrl = String.Empty;
                        String showTitle = String.Empty;

                        Match match = Regex.Match(baseWebData, PublicTvUtil.showUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            showTitle = match.Groups["showTitle"].Value;
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
                                Url = String.Format("{0}{1}", PublicTvUtil.archiveUrl, showUrl),
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
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

                Match match = Regex.Match(baseWebData, PublicTvUtil.showEpisodesStart);
                if (match.Success)
                {
                    baseWebData = baseWebData.Substring(match.Index);
                    int index = baseWebData.IndexOf(PublicTvUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        String episodesData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = episodesData.IndexOf(PublicTvUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                episodesData = episodesData.Substring(index);
                                index = episodesData.IndexOf(PublicTvUtil.showEpisodeEnd);

                                if (index > 0)
                                {
                                    String showData = episodesData.Substring(0, index);

                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeDate = String.Empty;

                                    match = Regex.Match(showData, PublicTvUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDate = HttpUtility.HtmlDecode(match.Groups["showEpisodeDate"].Value).Trim();
                                        showData = showData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showData, PublicTvUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                        showData = showData.Substring(match.Index + match.Length);
                                    }

                                    if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDate)))
                                    {
                                        break;
                                    }

                                    VideoInfo videoInfo = new VideoInfo()
                                    {
                                        Description = showEpisodeDate,
                                        Title = showEpisodeTitle,
                                        VideoUrl = String.Format("{0}{1}", PublicTvUtil.archiveUrl, showEpisodeUrl)
                                    };

                                    pageVideos.Add(videoInfo);

                                    episodesData = episodesData.Substring(index);
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

                    this.nextPageUrl = String.Empty;
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
            return this.GetVideoList(category, PublicTvUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, PublicTvUtil.pageSize);
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
            Match match = Regex.Match(baseWebData, PublicTvUtil.showEpisodeVideoUrl);

            if (match.Success)
            {
                this.Settings.Player = PlayerType.WMP;
                return String.Format("{0}{1}", PublicTvUtil.baseUrl, match.Groups["showEpisodeVideoUrl"].Value);
            }

            return "";
        }

        #endregion
    }
}
