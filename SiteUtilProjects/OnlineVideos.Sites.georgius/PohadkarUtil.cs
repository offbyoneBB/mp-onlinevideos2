using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public class PohadkarUtil : SiteUtilBase
    {
         #region Private fields

        private static String baseUrl = @"http://www.pohadkar.cz";

        private static String dynamicCategoryStart = @"<div class=""vypis_data"">";
        private static String dynamicCategoryEnd = @"</div>";
        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>";

        private static String showEpisodesStart = @"<div class=""tale_char_div"">";
        private static String showEpisodesEnd = @"<script type=""text/javascript"">";

        private static String showEpisodeStart = @"<div class=""tale_char_div"">";
        private static String showEpisodeThumbRegex = @"<img class=""tale_char_pic"" src=""(?<showEpisodeThumbUrl>[^""]+)""";
        private static String showEpisodeUrlAndTitleRegex = @"<a title=""[^""]*"" class=""tale_char_name"" style=""[^""]*"" href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)";
        private static String showEpisodeDescriptionRegex = @"<p title=""(?<showEpisodeDescription>[^""]*)";

        private static String showEpisodeNextPageRegex = @"<a class=""right_active"" href=""(?<nextPageUrl>[^""]+)"" >&gt;</a>";

        private static String showVideoUrlsRegex = @"(<param name=""movie"" value=""(?<showVideoUrl>[^""]+)"">)|(<iframe title=""YouTube video player"" width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")|(<iframe title=""YouTube video player"" class=""youtube-player"" type=""text/html"" width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")|(<iframe width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")";

        private static String optionTitleRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| (?<format>[a-z0-9]+) \([\s]*[0-9]+\)";

        private static String searchQueryUrl = @"http://www.pohadkar.cz/?s={0}";

        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @"/>";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&]+)";
        private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&]+)";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public PohadkarUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(PohadkarUtil.baseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(PohadkarUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                index = baseWebData.IndexOf(PohadkarUtil.dynamicCategoryEnd);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);
                }

                while (true)
                {
                    String showUrl = String.Empty;
                    String showTitle = String.Empty;

                    Match match = Regex.Match(baseWebData, PohadkarUtil.showUrlAndTitleRegex);
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
                            Url = Utils.FormatAbsoluteUrl(String.Format("{0}video/", showUrl), PohadkarUtil.baseUrl)
                        });
                    dynamicCategoriesCount++;
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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(PohadkarUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    index = baseWebData.IndexOf(PohadkarUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(PohadkarUtil.showEpisodeStart);

                            if (index >= 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;
                                String showEpisodeDescription = String.Empty;

                                Match match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = Utils.FormatAbsoluteUrl(match.Groups["showEpisodeThumbUrl"].Value, PohadkarUtil.baseUrl);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = Utils.FormatAbsoluteUrl(HttpUtility.UrlDecode(match.Groups["showEpisodeUrl"].Value), PohadkarUtil.baseUrl);
                                    showEpisodeTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeDescriptionRegex);
                                if (match.Success)
                                {
                                    showEpisodeDescription = HttpUtility.HtmlDecode(match.Groups["showEpisodeDescription"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
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
                                    VideoUrl = showEpisodeUrl
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, PohadkarUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = nextPageMatch.Success ? Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, pageUrl) : String.Empty;
                    
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
            return this.GetVideoList(category, PohadkarUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, PohadkarUtil.pageSize);
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

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> urls = new List<string>();
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
            MatchCollection matches = Regex.Matches(baseWebData, PohadkarUtil.showVideoUrlsRegex);
            foreach (Match match in matches)
            {
                String showVideoUrl = match.Groups["showVideoUrl"].Value;
                if (showVideoUrl.Contains("stream.cz"))
                {
                    baseWebData = SiteUtilBase.GetWebData(showVideoUrl.Replace("object", "video"));
                    baseWebData = HttpUtility.HtmlDecode(baseWebData);

                    Match flashVarsStart = Regex.Match(baseWebData, PohadkarUtil.flashVarsStartRegex);
                    if (flashVarsStart.Success)
                    {
                        int end = baseWebData.IndexOf(PohadkarUtil.flashVarsEnd, flashVarsStart.Index);
                        if (end > 0)
                        {
                            baseWebData = baseWebData.Substring(flashVarsStart.Index, end - flashVarsStart.Index);

                            Match idMatch = Regex.Match(baseWebData, PohadkarUtil.idRegex);
                            Match cdnLqMatch = Regex.Match(baseWebData, PohadkarUtil.cdnLqRegex);
                            Match cdnHqMatch = Regex.Match(baseWebData, PohadkarUtil.cdnHqRegex);

                            String id = (idMatch.Success) ? idMatch.Groups["id"].Value : String.Empty;
                            String cdnLq = (cdnLqMatch.Success) ? cdnLqMatch.Groups["cdnLQ"].Value : String.Empty;
                            String cdnHq = (cdnHqMatch.Success) ? cdnHqMatch.Groups["cdnHQ"].Value : String.Empty;

                            String url = String.Empty;
                            if (!String.IsNullOrEmpty(cdnHq))
                            {
                                url = SiteUtilBase.GetRedirectedUrl(String.Format(PohadkarUtil.videoUrlFormat, cdnHq));
                            }
                            else
                            {
                                url = SiteUtilBase.GetRedirectedUrl(String.Format(PohadkarUtil.videoUrlFormat, cdnLq));
                            }

                            urls.Add(url);
                        }
                    }
                }
                else
                {
                    Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(HttpUtility.UrlDecode(match.Groups["showVideoUrl"].Value.Replace("-nocookie", "")));
                    if (playbackOptions != null)
                    {
                        int width = 0;
                        String format = String.Empty;
                        String url = String.Empty;

                        foreach (var option in playbackOptions)
                        {
                            Match optionMatch = Regex.Match(option.Key, PohadkarUtil.optionTitleRegex);
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

                        urls.Add(url);
                    }
                }
            }

            return urls;
        }

        public override string getUrl(VideoInfo video)
        {
            return String.Empty;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<VideoInfo> Search(string query)
        {
            return this.getVideoList(new RssLink()
            {
                Name = "Search",
                Other = query,
                Url = String.Format(PohadkarUtil.searchQueryUrl, query)
            });
        }

        #endregion
    }
}
