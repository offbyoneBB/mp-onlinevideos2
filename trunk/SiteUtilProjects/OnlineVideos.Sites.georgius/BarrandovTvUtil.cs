using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.georgius
{
    public sealed class BarrandovTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.barrandov.tv/video";

        private static String dynamicCategoryStart = @"<div class=""videosGenre"">";
        private static String dynamicCategoryEnd = @"bmone2n";

        private static String showStart = @"<div class=""genreDetail"">";
        private static String showEnd = @"</ul>";

        private static String showTitleRegex = @"<h3>(?<showTitle>[^<]*)";
        private static String showUrlRegex = @"<a href=""(?<showUrl>[^""]+)";

        private static String showEpisodesStart = @"<ul class=""videoHpList"">";
        private static String showEpisodesEnd = @"<div class=""cols footerMenu"">";

        private static String showEpisodeStart = @"<li";
        private static String showEpisodeEnd = @"</li>";

        private static String showEpisodeUrlRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)";
        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)";
        private static String showEpisodeTitleRegex = @"<span>(?<showEpisodeTitle>[^<]+)";
        private static String showEpisodeDateRegex = @"Vysíláno:(?<showEpisodeDate>[^<]*)";

        private static String showEpisodeNextPageRegex = @"<a class=""right"" href=""(?<nextPageUrl>[^""]+)"">další";
        
        private static String showEpisodeVideoIdRegex = @"SWFObject\('[^\?]*\?itemid=(?<videoId>[^']+)'";
        private static String showEpisodeVideoDataFormat = "http://www.barrandov.tv/special/voddata/{1}"; // videoId

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public BarrandovTvUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(BarrandovTvUtil.baseUrl);

            int startIndex = baseWebData.IndexOf(BarrandovTvUtil.dynamicCategoryStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(BarrandovTvUtil.dynamicCategoryEnd, startIndex + BarrandovTvUtil.dynamicCategoryStart.Length);

                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(BarrandovTvUtil.showStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEnd, startIndex + BarrandovTvUtil.showStart.Length);

                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, BarrandovTvUtil.showTitleRegex);
                                if (match.Success)
                                {
                                    showTitle = match.Groups["showTitle"].Value;
                                }

                                match = Regex.Match(showData, BarrandovTvUtil.showUrlRegex);
                                if (match.Success)
                                {
                                    showUrl = match.Groups["showUrl"].Value;
                                }

                                if (!((String.IsNullOrEmpty(showUrl)) || (String.IsNullOrEmpty(showTitle))))
                                {
                                    this.Settings.Categories.Add(
                                    new RssLink()
                                    {
                                        Name = showTitle,
                                        Url = Utils.FormatAbsoluteUrl(showUrl, BarrandovTvUtil.baseUrl)
                                    });
                                    dynamicCategoriesCount++;
                                }

                                baseWebData = baseWebData.Substring(endIndex + BarrandovTvUtil.showEnd.Length);
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

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                int startIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesEnd, startIndex + BarrandovTvUtil.showEpisodesStart.Length);

                    if (endIndex > 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        Match nextPageMatch = Regex.Match(baseWebData, BarrandovTvUtil.showEpisodeNextPageRegex);
                        this.nextPageUrl = (nextPageMatch.Success) ? Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(nextPageMatch.Groups["nextPageUrl"].Value), pageUrl) : String.Empty;

                        while (true)
                        {
                            startIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodeStart);

                            if (startIndex >= 0)
                            {
                                endIndex = baseWebData.IndexOf(BarrandovTvUtil.showEpisodeEnd, startIndex + BarrandovTvUtil.showEpisodeStart.Length);

                                if (endIndex >= 0)
                                {
                                    String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeThumb = String.Empty;
                                    String showEpisodeDate = String.Empty;

                                    Match match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeUrlRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                    }

                                    match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeThumbRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeThumb = Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(match.Groups["showEpisodeThumbUrl"].Value), BarrandovTvUtil.baseUrl);
                                    }

                                    match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                    }

                                    match = Regex.Match(episodeData, BarrandovTvUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                    }

                                    if (!((String.IsNullOrEmpty(showEpisodeUrl)) || (String.IsNullOrEmpty(showEpisodeThumb)) || (String.IsNullOrEmpty(showEpisodeTitle)) || (String.IsNullOrEmpty(showEpisodeDate))))
                                    {
                                        VideoInfo videoInfo = new VideoInfo()
                                        {
                                            Description = showEpisodeDate,
                                            ImageUrl = showEpisodeThumb,
                                            Title = showEpisodeTitle,
                                            VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, BarrandovTvUtil.baseUrl)
                                        };

                                        pageVideos.Add(videoInfo);
                                    }

                                    baseWebData = baseWebData.Substring(endIndex + BarrandovTvUtil.showEpisodeEnd.Length);
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
            return this.GetVideoList(category, BarrandovTvUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, BarrandovTvUtil.pageSize);
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
            Match showEpisodeVideoIdMatch = Regex.Match(baseWebData, BarrandovTvUtil.showEpisodeVideoIdRegex);

            if ((video.PlaybackOptions == null) && (showEpisodeVideoIdMatch.Success))
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                String showEpisodeVideoDataUrl = String.Format(BarrandovTvUtil.showEpisodeVideoDataFormat, BarrandovTvUtil.baseUrl, showEpisodeVideoIdMatch.Groups["videoId"].Value);
                baseWebData = SiteUtilBase.GetWebData(showEpisodeVideoDataUrl);

                XmlDocument document = new XmlDocument();
                document.LoadXml(baseWebData);

                XmlNode mediainfo = document.SelectSingleNode("//mediainfo");
                XmlNode hostname = document.SelectSingleNode("//host");
                XmlNode streamname = document.SelectSingleNode("//file");

                if ((hostname != null) && (streamname != null))
                {
                    String movieUrl = String.Format("rtmpe://{0}/{1}", hostname.InnerText, streamname.InnerText);

                    String host = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf("/", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));
                    String app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                    String tcUrl = "rtmpe://" + host + "/" + app;
                    String playPath = movieUrl.Substring(movieUrl.IndexOf(app) + app.Length + 1);

                    String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(movieUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath, Token = "#ed%h0#w@1" }.ToString();

                    video.PlaybackOptions.Add("Low quality", resultUrl);
                }

                if ((mediainfo != null) && (mediainfo.Attributes != null) && (mediainfo.Attributes["multibitrate"] != null))
                {
                    // multi-bitrate
                    if (mediainfo.Attributes["multibitrate"].Value.ToUpperInvariant() == "TRUE")
                    {
                        String movieUrl = String.Format("rtmpe://{0}/{1}", hostname.InnerText, streamname.InnerText).Replace("_500", "_1000");

                        String host = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf("/", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));
                        String app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                        String tcUrl = "rtmpe://" + host + "/" + app;
                        String playPath = movieUrl.Substring(movieUrl.IndexOf(app) + app.Length + 1);

                        String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(movieUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath, Token = "#ed%h0#w@1" }.ToString();

                        video.PlaybackOptions.Add("High quality", resultUrl);
                    }
                }

                if ((mediainfo != null) && (mediainfo.Attributes != null) && (mediainfo.Attributes["hd"] != null))
                {
                    // multi-bitrate
                    if (mediainfo.Attributes["hd"].Value.ToUpperInvariant() == "TRUE")
                    {
                        String movieUrl = String.Format("rtmpe://{0}/{1}", hostname.InnerText, streamname.InnerText).Replace("_500", "_HD");

                        String host = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf("/", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));
                        String app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                        String tcUrl = "rtmpe://" + host + "/" + app;
                        String playPath = movieUrl.Substring(movieUrl.IndexOf(app) + app.Length + 1);

                        String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(movieUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath, Token = "#ed%h0#w@1" }.ToString();

                        video.PlaybackOptions.Add("HD", resultUrl);
                    }
                }
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        #endregion
    }
}
