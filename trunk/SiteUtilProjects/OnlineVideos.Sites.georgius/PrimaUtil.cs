using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class PrimaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://prima.stream.cz";
        private static String dynamicCategoryStart = "<ul id=\"sekce_kanalu\">";
        private static String dynamicCategoryEnd = @"</ul>";

        private static String showRegex = @"<a[\s]*href=""(?<showUrl>[^""]*)"">(?<showTitle>[^""]*)</a>";

        private static String showEpisodesStart = @"<div id=""videa_kanalu_list"">";
        private static String showEpisodeBlockStartRegex = @"<div class=""(kanal_1video)|(kanal_1video third)"">";
        private static String showEpisodeThumbUrlRegex = @"style=""background:#000 url\('(?<showThumbUrl>[^']*)'\) no-repeat 50% 50%;";
        private static String showEpisodeLengthRegex = @"<span class=""kanal_1vidoe_time"">(?<showLength>[^<]*)</span>";
        private static String showEpisodeUrlAndTitleRegex = @"<a class=""[^""]*"" href=""(?<showUrl>[^""]*)"" title=""[^""]*"">(?<showTitle>[^<]*)</a>";

        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'
        private static String showEpisodeNextPageRegex = @"<a href=""(?<url>[^""]*)"" class=""fakeButtonInline"">další&nbsp;&raquo;</a>";

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @"/>";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"cdnLQ=(?<cdnLQ>[^&]+)";
        private static String cdnHqRegex = @"cdnHQ=(?<cdnHQ>[^&]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public PrimaUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(PrimaUtil.baseUrl);

            int index = baseWebData.IndexOf(PrimaUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                index = baseWebData.IndexOf(PrimaUtil.dynamicCategoryEnd);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);
                }

                Match match = Regex.Match(baseWebData, PrimaUtil.showRegex);
                while (match.Success)
                {
                    String showUrl = match.Groups["showUrl"].Value;
                    String showTitle = match.Groups["showTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = showTitle,
                            HasSubCategories = false,
                            Url = showUrl
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
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int index = baseWebData.IndexOf(PrimaUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);
                }

                while (true)
                {
                    Match showEpisodeBlockStart = Regex.Match(baseWebData, PrimaUtil.showEpisodeBlockStartRegex);
                    if (showEpisodeBlockStart.Success)
                    {
                        baseWebData = baseWebData.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                        String showTitle = String.Empty;
                        String showThumbUrl = String.Empty;
                        String showUrl = String.Empty;
                        String showLength = String.Empty;
                        String showDescription = String.Empty;

                        Match showEpisodeThumbUrl = Regex.Match(baseWebData, PrimaUtil.showEpisodeThumbUrlRegex);
                        if (showEpisodeThumbUrl.Success)
                        {
                            showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                            baseWebData = baseWebData.Substring(showEpisodeThumbUrl.Index + showEpisodeThumbUrl.Length);
                        }

                        Match showEpisodeLength = Regex.Match(baseWebData, PrimaUtil.showEpisodeLengthRegex);
                        if (showEpisodeLength.Success)
                        {
                            showLength = showEpisodeLength.Groups["showLength"].Value;
                            baseWebData = baseWebData.Substring(showEpisodeLength.Index + showEpisodeLength.Length);
                        }

                        Match showEpisodeUrlAndTitle = Regex.Match(baseWebData, PrimaUtil.showEpisodeUrlAndTitleRegex);
                        if (showEpisodeUrlAndTitle.Success)
                        {
                            showUrl = showEpisodeUrlAndTitle.Groups["showUrl"].Value;
                            showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value;
                            baseWebData = baseWebData.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                        }

                        if (!((showEpisodeThumbUrl.Success) && (showEpisodeLength.Success) && (showEpisodeUrlAndTitle.Success)))
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


                Match nextPageMatch = Regex.Match(baseWebData, PrimaUtil.showEpisodeNextPageRegex);
                this.nextPageUrl = (nextPageMatch.Success) ? String.Format("{0}{1}", PrimaUtil.baseUrl, nextPageMatch.Groups["url"].Value.Replace("amp;", "")) : String.Empty;
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
            baseWebData = HttpUtility.HtmlDecode(baseWebData);

            Match flashVarsStart = Regex.Match(baseWebData, PrimaUtil.flashVarsStartRegex);
            if (flashVarsStart.Success)
            {
                int end = baseWebData.IndexOf(PrimaUtil.flashVarsEnd, flashVarsStart.Index);
                if (end > 0)
                {
                    baseWebData = baseWebData.Substring(flashVarsStart.Index, end - flashVarsStart.Index);

                    Match idMatch = Regex.Match(baseWebData, PrimaUtil.idRegex);
                    Match cdnLqMatch = Regex.Match(baseWebData, PrimaUtil.cdnLqRegex);
                    Match cdnHqMatch = Regex.Match(baseWebData, PrimaUtil.cdnHqRegex);

                    String id = (idMatch.Success) ? idMatch.Groups["id"].Value : String.Empty;
                    String cdnLq = (cdnLqMatch.Success) ? cdnLqMatch.Groups["cdnLQ"].Value : String.Empty;
                    String cdnHq = (cdnHqMatch.Success) ? cdnHqMatch.Groups["cdnHQ"].Value : String.Empty;

                    if ((!String.IsNullOrEmpty(cdnLq)) && (!String.IsNullOrEmpty(cdnHq)))
                    {
                        // we got low and high quality
                        String lowQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                        String highQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));

                        video.PlaybackOptions = new Dictionary<string, string>();
                        video.PlaybackOptions.Add("Low quality", lowQualityUrl);
                        video.PlaybackOptions.Add("High quality", highQualityUrl);
                    }
                    else if (!String.IsNullOrEmpty(cdnLq))
                    {
                        video.VideoUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                    }
                    else if (!String.IsNullOrEmpty(cdnHq))
                    {
                        video.VideoUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));
                    }
                }
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }

            return video.VideoUrl;
        }

        #endregion
    }    
}
