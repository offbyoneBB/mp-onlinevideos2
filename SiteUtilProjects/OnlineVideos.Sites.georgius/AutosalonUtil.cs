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

        private static String baseUrl = "http://autosalontv.cz";
        private static String showEpisodeBaseUrl = "http://autosalontv.cz/default.aspx";

        private static String showEpisodesStart = @"<table class=""vyber-dilu"">";
        private static String showEpisodeBlockStartRegex = @"<tr class=""radek-dil"">";
        
        private static String showEpisodeThumbUrlRegex = @"<img src=""(?<showThumbUrl>[^""]*)"" />";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"">(?<showTitle>[^<]*)</a>";
        private static String showEpisodeDescriptionEnd = @"</div>";
        
        private static String showEpisodeVideoUrlFormat = @"mms://bcastd.livebox.cz/up/as/{1}/_{0}{1}.wmv"; // 0 - week, 1 - year
        private static String showEpisodeVideoUrlRegex = @"year=(?<year>[0-9]+)&week=(?<week>[0-9]+)";

        // the number of show episodes per page
        private static int pageSize = 28;

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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int index = baseWebData.IndexOf(AutosalonUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);
                }

                Match showEpisodeBlockStart = Regex.Match(baseWebData, AutosalonUtil.showEpisodeBlockStartRegex);
                if (showEpisodeBlockStart.Success)
                {
                    while (true)
                    {
                        baseWebData = baseWebData.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                        String showTitle = String.Empty;
                        String showThumbUrl = String.Empty;
                        String showUrl = String.Empty;
                        String showDescription = String.Empty;

                        Match showEpisodeThumbUrl = Regex.Match(baseWebData, AutosalonUtil.showEpisodeThumbUrlRegex);
                        if (showEpisodeThumbUrl.Success)
                        {
                            showThumbUrl = String.Format("{0}{1}", AutosalonUtil.baseUrl, showEpisodeThumbUrl.Groups["showThumbUrl"].Value);
                            baseWebData = baseWebData.Substring(showEpisodeThumbUrl.Index + showEpisodeThumbUrl.Length);
                        }

                        Match showEpisodeUrlAndTitle = Regex.Match(baseWebData, AutosalonUtil.showEpisodeUrlAndTitleRegex);
                        if (showEpisodeUrlAndTitle.Success)
                        {
                            showUrl = String.Format("{0}{1}", AutosalonUtil.showEpisodeBaseUrl, showEpisodeUrlAndTitle.Groups["showUrl"].Value);
                            showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value;
                            baseWebData = baseWebData.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                        }

                        index = baseWebData.IndexOf(AutosalonUtil.showEpisodeDescriptionEnd);
                        if (index > 0)
                        {
                            showDescription = baseWebData.Substring(0, index - 1);
                            baseWebData = baseWebData.Substring(index);

                            // remove all between '<' and '>'
                            while (true)
                            {
                                Match match = Regex.Match(showDescription, "<[^>]+>");
                                if (match.Success)
                                {
                                    showDescription = showDescription.Remove(match.Index, match.Length);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if (!((showEpisodeThumbUrl.Success) || (showEpisodeUrlAndTitle.Success)))
                        {
                            break;
                        }

                        VideoInfo videoInfo = new VideoInfo()
                        {
                            Description = showDescription.Trim(),
                            ImageUrl = showThumbUrl,
                            Title = showTitle,
                            VideoUrl = showUrl
                        };

                        pageVideos.Add(videoInfo);
                    }
                }

                this.nextPageUrl = String.Empty;
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
            return this.GetVideoList(category, AutosalonUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, AutosalonUtil.pageSize);
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
