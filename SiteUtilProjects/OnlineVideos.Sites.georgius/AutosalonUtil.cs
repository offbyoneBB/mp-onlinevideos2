using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class AutosalonUtil : SiteUtilBase
    {
        #region Private fields

        private static String title = "Autosalon";

        private static String baseUrl = "http://autosalontv.cz/videa";
        private static String showEpisodeBaseUrl = "http://autosalontv.cz/default.aspx";

        private static String showEpisodesStart = @"<div class=""video_items"">";
        private static String showEpisodesEnd = @"<div class=""index_banner"">";

        private static String showEpisodeStart = @"<div class=""newVideo"">";
        private static String showEpisodeEnd = @"</p>";

        private static String showEpisodeDateRegex = @"<span class=""date"">(?<showDate>[^<]*)";
        private static String showEpisodeDescriptionRegex = @"<div class=""s"">(?<showDescription>[^<]*)";
        private static String showEpisodeThumbUrlRegex = @"href=""[^""]*""><img src=""(?<showThumbUrl>[^""]*)";
        private static String showEpisodeUrlAndTitleRegex = @"<h4><a href=""(?<showUrl>[^""]*)"">(?<showTitle>[^<]*)";
        
        private static String showEpisodeVideoUrlFormat = @"mms://bcastd.livebox.cz/up/as/{1}/_{0}{1}.wmv"; // 0 - week, 1 - year
        private static String showEpisodeVideoUrlRegex = @"year=(?<year>[0-9]+)&week=(?<week>[0-9]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public AutosalonUtil()
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
            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = AutosalonUtil.title,
                    HasSubCategories = false,
                    Url = AutosalonUtil.baseUrl
                });

            this.Settings.DynamicCategoriesDiscovered = true;
            return 1;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int startIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodesStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodesEnd, startIndex + AutosalonUtil.showEpisodesStart.Length);
                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            startIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodeStart);
                            if (startIndex >= 0)
                            {
                                endIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodeEnd, startIndex + AutosalonUtil.showEpisodeStart.Length);
                                if (endIndex >= 0)
                                {
                                    String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                    String showTitle = String.Empty;
                                    String showThumbUrl = String.Empty;
                                    String showUrl = String.Empty;
                                    String showDescription = String.Empty;
                                    String showDate = String.Empty;

                                    Match match = Regex.Match(episodeData, AutosalonUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showDate = match.Groups["showDate"].Value;
                                    }

                                    match = Regex.Match(episodeData, AutosalonUtil.showEpisodeDescriptionRegex);
                                    if (match.Success)
                                    {
                                        showDescription = HttpUtility.HtmlDecode(match.Groups["showDescription"].Value);
                                    }

                                    match = Regex.Match(episodeData, AutosalonUtil.showEpisodeThumbUrlRegex);
                                    if (match.Success)
                                    {
                                        showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, pageUrl);
                                    }

                                    match = Regex.Match(episodeData, AutosalonUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, pageUrl);
                                        showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                    }

                                    if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            Description = showDescription.Trim(),
                                            ImageUrl = showThumbUrl,
                                            Title = showTitle,
                                            VideoUrl = showUrl,
                                            Airdate = showDate
                                        };

                                        pageVideos.Add(videoInfo);
                                    }

                                    baseWebData = baseWebData.Substring(endIndex + AutosalonUtil.showEpisodeEnd.Length);
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

                this.nextPageUrl = String.Empty;
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
            Match match = Regex.Match(video.VideoUrl, AutosalonUtil.showEpisodeVideoUrlRegex);
            if (match.Success)
            {
                return String.Format(AutosalonUtil.showEpisodeVideoUrlFormat, match.Groups["week"].Value, match.Groups["year"].Value);
            }

            return video.VideoUrl;
        }

        #endregion
    }
}
