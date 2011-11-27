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

        private static String baseUrl = "http://www.iprima.cz/videoarchiv";

        private static String dynamicCategoryStart = @"<div class=""categoryChoice"">";
        private static String dynamicCategoryEnd = @"</ul>";

        private static String showRegex = @"<a href=""(?<showUrl>[^""]*)"" class=""[^""]*"">(?<showTitle>[^""]*)</a>";

        private static String playerStart = @"<div id=""player"">";
        private static String playerEnd = @"</div>";

        private static String youtubeEpisodeUrlRegex = @"<param name=""movie"" value=""(?<episodeUrl>[^""]*)""";
        private static String youtubeFormatRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| (?<format>[a-z0-9]+) \([\s]*[0-9]+\)";

        private static String primaEpisodeUrlRegex = @"LiveboxPlayer.init\('[^']*', width, height, '(?<hqVideoPart>[^']*)', '(?<lqVideoPart>[^']*)', '[^']*', '[^']*','[^']*','[^']*','[^']*'\);";
        private static String primaEpisodeConfiguration = @"http://embed.livebox.cz/iprima/player-1.js";
        private static String primaEpisodeStreamRegex = @"stream: '(?<tcUrl>[^']*)'";

        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @">";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&""]+)";
        private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&""]+)";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        private int pageCounter = 0;

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
                            Url = Utils.FormatAbsoluteUrl(showUrl, PrimaUtil.baseUrl)
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
                String modifiedPageUrl = pageUrl.Replace("videoarchiv", "videoarchiv_ajax") + "?method=json&action=relevant&page=" + this.pageCounter++;
                String baseWebData = SiteUtilBase.GetWebData(modifiedPageUrl);

                Newtonsoft.Json.Linq.JObject jsonObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(baseWebData);
                Newtonsoft.Json.Linq.JArray data = (Newtonsoft.Json.Linq.JArray)jsonObject["data"];

                this.nextPageUrl = (data.Count == 0) ? String.Empty : pageUrl;

                for (int i = 0; i < data.Count; i++)
                {
                    Newtonsoft.Json.Linq.JObject showObject = (Newtonsoft.Json.Linq.JObject)data[i];

                    String showTitle = (String)showObject["title"];
                    String showThumbUrl = Utils.FormatAbsoluteUrl(String.Format("/{0}", (String)showObject["image"]), PrimaUtil.baseUrl);
                    String showUrl = pageUrl.Replace("all/", String.Format("{0}/", (String)showObject["nid"]));
                    String showDescription = (String)showObject["date"];

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Description = showDescription,
                        ImageUrl = showThumbUrl,
                        Title = showTitle,
                        VideoUrl = showUrl
                    };

                    pageVideos.Add(videoInfo);
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
            this.pageCounter = 0;
            this.currentStartIndex = 0;
            return this.GetVideoList(category, PrimaUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, PrimaUtil.pageSize);
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
            this.Settings.Player = PlayerType.Internal;

            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(PrimaUtil.playerStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(PrimaUtil.playerEnd, startIndex);
                if (endIndex >= 0)
                {
                    String playerData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match youtubeMatch = Regex.Match(playerData, PrimaUtil.youtubeEpisodeUrlRegex);
                    if ((youtubeMatch.Success) && (youtubeMatch.Groups["episodeUrl"].Value.IndexOf("youtube", StringComparison.InvariantCultureIgnoreCase) >= 0))
                    {
                        Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(youtubeMatch.Groups["episodeUrl"].Value);
                        if (playbackOptions != null)
                        {
                            int width = 0;
                            String format = String.Empty;
                            String url = String.Empty;

                            foreach (var option in playbackOptions)
                            {
                                Match optionMatch = Regex.Match(option.Key, PrimaUtil.youtubeFormatRegex);
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

                            return url;
                        }
                    }

                    Match primaMatch = Regex.Match(playerData, PrimaUtil.primaEpisodeUrlRegex);
                    if (primaMatch.Success)
                    {
                        String data = SiteUtilBase.GetWebData(PrimaUtil.primaEpisodeConfiguration, null, video.VideoUrl, null, true);
                        Match tcUrlMatch = Regex.Match(data, PrimaUtil.primaEpisodeStreamRegex);
                        if (tcUrlMatch.Success)
                        {
                            video.PlaybackOptions = new Dictionary<string, string>();

                            String tcUrl = tcUrlMatch.Groups["tcUrl"].Value;
                            String app = tcUrl.Substring(tcUrl.IndexOf('/', tcUrl.IndexOf(':') + 3) + 1);

                            if (!String.IsNullOrEmpty(primaMatch.Groups["hqVideoPart"].Value))
                            {
                                String playPath = "mp4:" + primaMatch.Groups["hqVideoPart"].Value;
                                String rtmpUrl = tcUrl + "/" + playPath;

                                String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(rtmpUrl) { App = app, TcUrl = tcUrl, PlayPath = playPath }.ToString();

                                video.PlaybackOptions.Add("High quality", resultUrl);
                            }

                            if (!String.IsNullOrEmpty(primaMatch.Groups["lqVideoPart"].Value))
                            {
                                String playPath = "mp4:" + primaMatch.Groups["lqVideoPart"].Value;
                                String rtmpUrl = tcUrl + "/" + playPath;

                                String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(rtmpUrl) { App = app, TcUrl = tcUrl, PlayPath = playPath }.ToString();

                                video.PlaybackOptions.Add("Low quality", resultUrl);
                            }

                            if ((video.PlaybackOptions != null) && (video.PlaybackOptions.Count > 0))
                            {
                                var enumer = video.PlaybackOptions.GetEnumerator();
                                enumer.MoveNext();
                                return enumer.Current.Value;
                            }
                        }
                    }

                    Match flashVarsStart = Regex.Match(playerData, PrimaUtil.flashVarsStartRegex);
                    if (flashVarsStart.Success)
                    {
                        int end = playerData.IndexOf(PrimaUtil.flashVarsEnd, flashVarsStart.Index);
                        if (end > 0)
                        {
                            playerData = playerData.Substring(flashVarsStart.Index, end - flashVarsStart.Index);

                            Match idMatch = Regex.Match(playerData, PrimaUtil.idRegex);
                            Match cdnLqMatch = Regex.Match(playerData, PrimaUtil.cdnLqRegex);
                            Match cdnHqMatch = Regex.Match(playerData, PrimaUtil.cdnHqRegex);

                            String id = (idMatch.Success) ? idMatch.Groups["id"].Value : String.Empty;
                            String cdnLq = (cdnLqMatch.Success) ? cdnLqMatch.Groups["cdnLQ"].Value : String.Empty;
                            String cdnHq = (cdnHqMatch.Success) ? cdnHqMatch.Groups["cdnHQ"].Value : String.Empty;

                            if ((!String.IsNullOrEmpty(cdnLq)) && (!String.IsNullOrEmpty(cdnHq)))
                            {
                                // we got low and high quality
                                String lowQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                                String highQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));

                                video.PlaybackOptions = new Dictionary<string, string>();
                                video.PlaybackOptions.Add("High quality", highQualityUrl);
                                video.PlaybackOptions.Add("Low quality", lowQualityUrl);

                                var enumer = video.PlaybackOptions.GetEnumerator();
                                enumer.MoveNext();
                                return enumer.Current.Value;
                            }
                            else if (!String.IsNullOrEmpty(cdnLq))
                            {
                                return SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                            }
                            else if (!String.IsNullOrEmpty(cdnHq))
                            {
                                return SiteUtilBase.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));
                            }
                        }
                    }
                }
            }

            return String.Empty;
        }

        #endregion
    }
}
