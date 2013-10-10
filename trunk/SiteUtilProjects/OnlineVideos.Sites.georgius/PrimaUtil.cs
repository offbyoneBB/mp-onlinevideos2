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

        private static String baseUrl = "http://play.iprima.cz/az";

        private static String dynamicCategoryStart = @"<div class=""genres"">";
        private static String dynamicCategoryEnd = @"<div class=""channels"">";

        private static String categoryRegex = @"<a href=""(?<categoryUrl>[^""]*)"">(?<categoryTitle>[^<]*)";

        private static String showListBlockStart = @"<div class=""mainContent"">";
        private static String showListBlockEnd = @"<div id=""rightContainer"">";

        private static String showBlockStart = @"<div class=""item"">";
        private static String showBlockEnd = @"<div class=""field-video-count";

        private static String showThumbUrl = @"<img src=""(?<showThumbUrl>[^""]*)";
        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"">(?<showTitle>[^<]*)";

        private static String showListNextPageRegex = @"<a href=""(?<showListNextPage>[^""]+)"" class=""active"">další<";

        private static String showEpisodesBlockStart = @"<div class=""video-strip"">";
        private static String showEpisodesBlockEnd = @"<div id=""rightContainer"">";

        private static String showEpisodeBlockStart = @"<div class=""item";
        private static String showEpisodeBlockEnd = @"</a></div></div>";

        private static String showEpisodeThumbUrl = @"<img src=""(?<showEpisodeThumbUrl>[^""]*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]*)"">(?<showEpisodeTitle>[^<]*)";

        private static String showEpisodeNextPageRegex = @"<a href=""(?<showEpisodeNextPage>[^""]+)"" class=""active"">další<";

        private static String showUrlFormat = @"videoarchiv_ajax/all/{0}?method=json&action=relevant&per_page=10&page={1}";

        private static String episodeUrlFormat = @"/all/{0}/{1}";
        private static String episodeUrlJS = @"http://embed.livebox.cz/iprimaplay/player-embed-v2.js";

        private static String episodeHqFileNameFormat = @"""hq_id"":""(?<hqFileName>[^""]*)";
        private static String episodeLqFileNameFormat = @"""lq_id"":""(?<lqFileName>[^""]*)";

        private static String episodeAuthSectionStart = "embed['typeStream'] = 'vod';";
        private static String episodeAuthSectionEnd = "}";

        private static String episodeAuthStart = @"auth='+""""+'";
        private static String episodeAuthEnd = @"'";
        private static String episodeZone = @"""zoneGEO"":(?<zone>[^,]*)";
        private static String episodeBaseUrlStart = @"embed['stream'] = 'rtmp";
        private static String episodeBaseUrlEnd = @"'+(";

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @"/>";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&]+)";
        private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&""]+)";
        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

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
            String baseWebData = SiteUtilBase.GetWebData(PrimaUtil.baseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(PrimaUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                index = baseWebData.IndexOf(PrimaUtil.dynamicCategoryEnd);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);
                }

                Match match = Regex.Match(baseWebData, PrimaUtil.categoryRegex);
                while (match.Success)
                {
                    String categoryUrl = match.Groups["categoryUrl"].Value;
                    String categoryTitle = match.Groups["categoryTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = categoryTitle,
                            HasSubCategories = true,
                            Url = Utils.FormatAbsoluteUrl(categoryUrl, PrimaUtil.baseUrl)
                        });

                    dynamicCategoriesCount++;
                    match = match.NextMatch();
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int showsCount = 0;
            String url = ((RssLink)parentCategory).Url;
            if (parentCategory.ParentCategory != null)
            {
                parentCategory = parentCategory.ParentCategory;
                // last category is next category, remove it
                parentCategory.SubCategories.RemoveAt(parentCategory.SubCategories.Count - 1);
            }
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
            }
            RssLink category = (RssLink)parentCategory;

            String baseWebData = SiteUtilBase.GetWebData(url, null, null, null, true);

            int startIndex = baseWebData.IndexOf(PrimaUtil.showListBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(PrimaUtil.showListBlockEnd, startIndex);

                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(PrimaUtil.showBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(PrimaUtil.showBlockEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, PrimaUtil.showThumbUrl);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, PrimaUtil.baseUrl);
                                    showData = showData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(showData, PrimaUtil.showUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, PrimaUtil.baseUrl);
                                    showTitle = match.Groups["showTitle"].Value;
                                }

                                category.SubCategories.Add(
                                    new RssLink()
                                    {
                                        Name = showTitle,
                                        HasSubCategories = false,
                                        Url = showUrl,
                                        Thumb = showThumbUrl
                                    });

                                showsCount++;
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

                        baseWebData = baseWebData.Substring(endIndex);
                    }

                    Match nextPage = Regex.Match(baseWebData, PrimaUtil.showListNextPageRegex);
                    if (nextPage.Success)
                    {
                        parentCategory.SubCategories.Add(new NextPageCategory() { Url = Utils.FormatAbsoluteUrl(nextPage.Groups["showListNextPage"].Value, PrimaUtil.baseUrl), ParentCategory = parentCategory });
                    }
                }
            }

            if (showsCount > 0)
            {
                parentCategory.SubCategoriesDiscovered = true;
            }

            return showsCount;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(PrimaUtil.showEpisodesBlockStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(PrimaUtil.showEpisodesBlockEnd, startIndex);

                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            startIndex = baseWebData.IndexOf(PrimaUtil.showEpisodeBlockStart);
                            if (startIndex >= 0)
                            {
                                endIndex = baseWebData.IndexOf(PrimaUtil.showEpisodeBlockEnd, startIndex);
                                if (endIndex >= 0)
                                {
                                    String showEpisodeData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                    String showEpisodeThumbUrl = String.Empty;
                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;

                                    Match match = Regex.Match(showEpisodeData, PrimaUtil.showEpisodeThumbUrl);
                                    if (match.Success)
                                    {
                                        showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showEpisodeThumbUrl"].Value, PrimaUtil.baseUrl);
                                        showEpisodeData = showEpisodeData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showEpisodeData, PrimaUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = Utils.FormatAbsoluteUrl(match.Groups["showEpisodeUrl"].Value, PrimaUtil.baseUrl);
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                    }

                                    if (!(String.IsNullOrEmpty(showEpisodeUrl) || String.IsNullOrEmpty(showEpisodeTitle)))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            ImageUrl = showThumbUrl,
                                            Title = showEpisodeTitle,
                                            VideoUrl = showEpisodeUrl
                                        };

                                        pageVideos.Add(videoInfo);
                                    }
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

                            baseWebData = baseWebData.Substring(endIndex);
                        }

                        Match nextPage = Regex.Match(baseWebData, PrimaUtil.showEpisodeNextPageRegex);
                        if (nextPage.Success)
                        {
                            this.nextPageUrl = Utils.FormatAbsoluteUrl(nextPage.Groups["showEpisodeNextPage"].Value, PrimaUtil.baseUrl);
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
            String episodeJS = SiteUtilBase.GetWebData(PrimaUtil.episodeUrlJS, null, video.VideoUrl, null, true);
            baseWebData = HttpUtility.HtmlDecode(baseWebData);

            video.PlaybackOptions = new Dictionary<string, string>();

            String baseRtmpUrl = String.Empty;
            String lqFileName = String.Empty;
            String hqFileName = String.Empty;
            String auth = String.Empty;
            String zone = String.Empty;

            Match match = Regex.Match(baseWebData, PrimaUtil.episodeLqFileNameFormat);
            if (match.Success)
            {
                lqFileName = match.Groups["lqFileName"].Value;
            }

            match = Regex.Match(baseWebData, PrimaUtil.episodeHqFileNameFormat);
            if (match.Success)
            {
                hqFileName = match.Groups["hqFileName"].Value;
            }

            match = Regex.Match(baseWebData, PrimaUtil.episodeZone);
            if (match.Success)
            {
                zone = match.Groups["zone"].Value;
            }

            int startIndex = episodeJS.IndexOf(PrimaUtil.episodeBaseUrlStart);
            if (startIndex >= 0)
            {
                int endIndex = episodeJS.IndexOf(PrimaUtil.episodeBaseUrlEnd, startIndex + PrimaUtil.episodeBaseUrlStart.Length);
                if (endIndex >= 0)
                {
                    baseRtmpUrl = "rtmp" + episodeJS.Substring(startIndex + PrimaUtil.episodeBaseUrlStart.Length, endIndex - startIndex - PrimaUtil.episodeBaseUrlStart.Length).Replace("iprima_token", "");
                }
            }

            startIndex = episodeJS.IndexOf(PrimaUtil.episodeAuthSectionStart);
            if (startIndex >= 0)
            {
                int endIndex = episodeJS.IndexOf(PrimaUtil.episodeAuthSectionEnd, startIndex + PrimaUtil.episodeAuthSectionStart.Length);
                if (endIndex >= 0)
                {
                    String authSection = episodeJS.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = authSection.IndexOf(PrimaUtil.episodeAuthStart);
                        if (startIndex >= 0)
                        {
                            endIndex = authSection.IndexOf(PrimaUtil.episodeAuthEnd, startIndex + PrimaUtil.episodeAuthStart.Length);
                            if (endIndex >= 0)
                            {
                                auth = authSection.Substring(startIndex + PrimaUtil.episodeAuthStart.Length, endIndex - startIndex - PrimaUtil.episodeAuthStart.Length);

                                authSection = authSection.Substring(startIndex + PrimaUtil.episodeAuthStart.Length + auth.Length);
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

            if ((!String.IsNullOrEmpty(auth)) && (!(String.IsNullOrEmpty(lqFileName) || String.IsNullOrEmpty(hqFileName) || String.IsNullOrEmpty(baseRtmpUrl))))
            {
                String app = String.Format("iprima_token{0}?auth={1}", zone == "0" ? "" : "_" + zone , auth);
                String tcUrl = String.Format("{0}{1}", baseRtmpUrl, app);

                if (!String.IsNullOrEmpty(lqFileName))
                {
                    String playPath = "mp4:" + lqFileName;
                    OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(baseRtmpUrl)
                    {
                        App = app,
                        TcUrl = tcUrl,
                        PlayPath = playPath,
                        SwfUrl = String.Format("http://embed.livebox.cz/iprimaplay/flash/LiveboxPlayer.swf?nocache={0}", (UInt64)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)),
                        PageUrl = video.VideoUrl
                    };

                    video.PlaybackOptions.Add("Low quality", rtmpUrl.ToString());
                }

                if (!String.IsNullOrEmpty(hqFileName))
                {
                    String playPath = "mp4:" + hqFileName;
                    OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(baseRtmpUrl)
                    {
                        App = app,
                        TcUrl = tcUrl,
                        PlayPath = playPath,
                        SwfUrl = String.Format("http://embed.livebox.cz/iprimaplay/flash/LiveboxPlayer.swf?nocache={0}", (UInt64)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)),
                        PageUrl = video.VideoUrl
                    };

                    video.PlaybackOptions.Add("High quality", rtmpUrl.ToString());
                }
            }
            else
            {
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
