using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.SK_CZ
{
    public sealed class TA3Util : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.ta3.com/archiv.html";
        //private static String baseWebPageUrl = "http://www.ta3.com/sk/archiv?den=&mesiac=&rok=";

        private static String dynamicCategoryStart = @"<div class=""archive"">";
        private static String dynamicCategoryEnd = @"<div class=""inside archive-filter"">";
        private static String categoryStartRegex = @"<a href=""(?<categoryUrl>[^<]*)"">(?<categoryTitle>[^<]*)</a>";

        private static String showsStart = @"<ul class=""items"">";
        private static String showsEnd = @"</ul>";

        private static String showBlockStartRegex = @"<li";
        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)""><span class=""vicon""></span>(?<showTitle>[^<]*)</a>";

        private static String showEpisodeNextPageStart = @"<li class=""next"">";
        private static String showEpisodeNextPageRegex = @"<a href=""(?<url>[^""]*)"">";

        private static String showEpisodeStartRegex = @"<div id=""mainMiddleCont"">";
        private static String showEpisodeEndRegex = @"</object>";

        private static String showEpisodeUrlRegex = @"<param name=""url"" value=""(?<showEpisodeUrl>[^""]*)"" />";

        // the number of show episodes per page
        private static int pageSize = 28;

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
            String baseWebData = SiteUtilBase.GetWebData(TA3Util.baseUrl);

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
                            ImageUrl = showThumbUrl,
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
            return this.GetVideoList(category, TA3Util.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, TA3Util.pageSize);
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
            String showEpisodeWebBaseData = SiteUtilBase.GetWebData(video.VideoUrl);
            String showUrl = String.Empty;
            Match showEpisodeStart = Regex.Match(showEpisodeWebBaseData, TA3Util.showEpisodeStartRegex);
            if (showEpisodeStart.Success)
            {
                showEpisodeWebBaseData = showEpisodeWebBaseData.Substring(showEpisodeStart.Index);

                String showData = showEpisodeWebBaseData;
                Match showEpisodeEnd = Regex.Match(showEpisodeWebBaseData, TA3Util.showEpisodeEndRegex);
                if (showEpisodeEnd.Success)
                {
                    showData = showData.Substring(0, showEpisodeEnd.Index);
                }

                Match showEpisodeUrl = Regex.Match(showData, TA3Util.showEpisodeUrlRegex);
                if (showEpisodeUrl.Success)
                {
                    showUrl = String.Format("{0}{1}", TA3Util.baseUrl, showEpisodeUrl.Groups["showEpisodeUrl"].Value);
                }
            }

            return showUrl;
        }

        #endregion
    }
}
