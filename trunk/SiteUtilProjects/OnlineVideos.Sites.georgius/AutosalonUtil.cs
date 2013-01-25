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
        
        private static String showEpisodeVideoStart = @"LBX.init";
        private static String showEpisodeVideoEnd = @");";
        private static String showEpisodeVideoRegex = @"year=(?<year>[0-9]*),week=(?<week>[0-9]*)";

        private static String smoothServer = "http://assmooth.livebox.cz/UP_Smooth/AS/{0}/{1:00}{0}/{1:00}{0}.ism/Manifest";
        private static String smoothServerWowza = "http://bcasthw.livebox.cz:80/AS/_definst_/AS/smil:{1:00}{0}.smil/Manifest";

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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl);
            String videoUrl = String.Empty;

            int startIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodeVideoStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(AutosalonUtil.showEpisodeVideoEnd, startIndex);
                if (endIndex >= 0)
                {
                    String data = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match match = Regex.Match(data, AutosalonUtil.showEpisodeVideoRegex);
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups["year"].Value);
                        int week = int.Parse(match.Groups["week"].Value);

                        if (year < 2012)
                        {
                            videoUrl = String.Format(AutosalonUtil.smoothServer, year, week);
                        }
                        else
                        {
                            if (year == 2012)
                            {
                                if (week >= 46)
                                {
                                    videoUrl = String.Format(AutosalonUtil.smoothServerWowza, year, week);
                                }
                                else
                                {
                                    videoUrl = String.Format(AutosalonUtil.smoothServer, year, week);
                                }
                            }
                            else
                            {
                                videoUrl = String.Format(AutosalonUtil.smoothServerWowza, year, week);
                            }
                        }
                    }
                }
            }

            return videoUrl;
        }

        #endregion
    }
}
