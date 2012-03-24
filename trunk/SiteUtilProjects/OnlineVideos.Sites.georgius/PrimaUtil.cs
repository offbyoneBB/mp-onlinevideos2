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

        private static String baseUrl = "http://play.iprima.cz";
        private static String dynamicCategoryStart = "var topcat = [";
        private static String dynamicCategoryEnd = @"];var channel =";

        private static String showRegex = @"{""name"":""(?<showTitle>[^""]*)"",""tid"":""(?<showUrl>[^""]*)""}";
        private static String showUrlFormat = @"videoarchiv_ajax/all/{0}?method=json&action=relevant&per_page=10&page={1}";

        private static String episodeUrlFormat = @"/all/{0}/{1}";
        private static String episodeUrlJS = @"http://embed.livebox.cz/iprimaplay/player-embed-v2.js";

        private static String episodeHqFileNameFormat = @"'hq_id':'(?<hqFileName>[^']*)";
        private static String episodeLqFileNameFormat = @"'lq_id':'(?<lqFileName>[^']*)";
        private static String episodeAuth = @"'?auth=(?<auth>[^']*)";
        private static String episodeZone = @"'zoneGEO':(?<zone>[^,]*)";
        private static String episodeBaseUrlStart = @"embed['stream'] = '";
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
            List<RssLink> unsortedShows = new List<RssLink>();

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

                    unsortedShows.Add(
                        new RssLink()
                        {
                            Name = OnlineVideos.Utils.ReplaceEscapedUnicodeCharacter(HttpUtility.UrlDecode(showTitle)),
                            HasSubCategories = false,
                            Url = Utils.FormatAbsoluteUrl(String.Format(PrimaUtil.showUrlFormat, showUrl, 0), PrimaUtil.baseUrl)
                        });

                    dynamicCategoriesCount++;
                    match = match.NextMatch();
                }
            }

            if (unsortedShows.Count > 0)
            {
                foreach (var show in unsortedShows.OrderBy(show => show.Name))
                {
                    this.Settings.Categories.Add(show);
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

                Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(baseWebData);
                Newtonsoft.Json.Linq.JValue tid = (Newtonsoft.Json.Linq.JValue)jObject["tid"];
                Newtonsoft.Json.Linq.JArray data = (Newtonsoft.Json.Linq.JArray)jObject["data"];
                long total = (long)((Newtonsoft.Json.Linq.JValue)jObject["pager"]["total"]).Value;
                long to = (long)((Newtonsoft.Json.Linq.JValue)jObject["pager"]["to"]).Value;
                int page = int.Parse((String)((Newtonsoft.Json.Linq.JValue)jObject["pager"]["page"]).Value);

                if (to < total)
                {
                    this.nextPageUrl = Utils.FormatAbsoluteUrl(String.Format(PrimaUtil.showUrlFormat, tid.Value, page + 1), PrimaUtil.baseUrl);
                }
                else
                {
                    this.nextPageUrl = String.Empty;
                }

                foreach (var episode in data)
                {
                    Newtonsoft.Json.Linq.JValue nid = (Newtonsoft.Json.Linq.JValue)episode["nid"];
                    Newtonsoft.Json.Linq.JValue title = (Newtonsoft.Json.Linq.JValue)episode["title"];
                    Newtonsoft.Json.Linq.JValue image = (Newtonsoft.Json.Linq.JValue)episode["image"];
                    Newtonsoft.Json.Linq.JValue date = (Newtonsoft.Json.Linq.JValue)episode["date"];

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Description = (String)date.Value,
                        ImageUrl = Utils.FormatAbsoluteUrl((String)image.Value, PrimaUtil.baseUrl),
                        Title = OnlineVideos.Utils.ReplaceEscapedUnicodeCharacter(HttpUtility.UrlDecode((String)title.Value)),
                        VideoUrl = Utils.FormatAbsoluteUrl(String.Format(PrimaUtil.episodeUrlFormat, nid.Value, tid.Value), PrimaUtil.baseUrl)
                    };

                    pageVideos.Add(videoInfo);
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
            String episodeJS = SiteUtilBase.GetWebData(PrimaUtil.episodeUrlJS, null, null, null, true);
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
                    baseRtmpUrl = episodeJS.Substring(startIndex + PrimaUtil.episodeBaseUrlStart.Length, endIndex - startIndex - PrimaUtil.episodeBaseUrlStart.Length).Replace("iprima_token", "");
                }
            }

            match = Regex.Match(episodeJS, PrimaUtil.episodeAuth);
            if (match.Success)
            {
                auth = match.Groups["auth"].Value;
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
                        PlayPath = playPath
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
                        PlayPath = playPath
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
