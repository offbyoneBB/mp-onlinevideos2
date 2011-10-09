using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;


namespace OnlineVideos.Sites.SK_CZ
{
    public sealed class BarrandovTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.barrandov.tv";
        private static String archiveUrl = @"http://www.barrandov.tv/video";

        private static String dynamicCategoryStart = @"<ul class=""videosmenu"">";
        private static String showStart = @"<li>";
        private static String showUrlAndTitleRegex = @"<a href=""video/(?<showUrl>[^""]+)"" class=""[^""]*"">(?<showTitle>[^<]+)</a>";

        private static String showEpisodesStart = @"<ul class=""prglist video"">";
        private static String showEpisodesEnd = @"</ul>";
        private static String showEpisodeStart = @"<li";
        private static String showEpisodeEnd = @"</li>";
        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)"" alt=""[^""]*"" />";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)</a>";
        private static String showEpisodeDateRegex = @"<span class=""ml"">(?<showEpisodeDate>[^<]*)</span>";

        private static String showEpisodeNextPageRegex = @"<a href=""javascript:Ajax\('[^']*', '(?<nextPageUrl>[^']+)'\)"">další »</a>";

        private static String showEpisodeVideoIdRegex = @"SWFObject\('/flash/unigramPlayer_v1.swf\?itemid=(?<videoId>[^']+)'";
        private static String showEpisodeVideoDataFormat = "{0}/special/videoplayerdata/{1}"; // baseUrl, videoId

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
            String baseWebData = SiteUtilBase.GetWebData(BarrandovTvUtil.archiveUrl);

            int index = baseWebData.IndexOf(BarrandovTvUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                while (true)
                {
                    index = baseWebData.IndexOf(BarrandovTvUtil.showStart);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        String showUrl = String.Empty;
                        String showTitle = String.Empty;

                        Match match = Regex.Match(baseWebData, BarrandovTvUtil.showUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            showTitle = match.Groups["showTitle"].Value;
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
                                Url = String.Format("{0}/{1}", BarrandovTvUtil.archiveUrl, showUrl),
                            });
                        dynamicCategoriesCount++;
                    }
                    else
                    {
                        break;
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

                int index = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);
                    index = baseWebData.IndexOf(BarrandovTvUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        String episodesData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = episodesData.IndexOf(BarrandovTvUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                episodesData = episodesData.Substring(index);
                                index = episodesData.IndexOf(BarrandovTvUtil.showEpisodeEnd);

                                if (index > 0)
                                {
                                    String showData = episodesData.Substring(0, index);

                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeThumb = String.Empty;
                                    String showEpisodeDate = String.Empty;

                                    Match match = Regex.Match(showData, BarrandovTvUtil.showEpisodeThumbRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeThumb = String.Format("{0}{1}", BarrandovTvUtil.baseUrl, HttpUtility.HtmlDecode(match.Groups["showEpisodeThumbUrl"].Value));
                                        showData = showData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showData, BarrandovTvUtil.showEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                        showData = showData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showData, BarrandovTvUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                        showData = showData.Substring(match.Index + match.Length);
                                    }

                                    if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDate)))
                                    {
                                        break;
                                    }

                                    VideoInfo videoInfo = new VideoInfo()
                                    {
                                        Description = showEpisodeDate,
                                        ImageUrl = showEpisodeThumb,
                                        Title = showEpisodeTitle,
                                        VideoUrl = String.Format("{0}{1}", BarrandovTvUtil.baseUrl, showEpisodeUrl)
                                    };

                                    pageVideos.Add(videoInfo);

                                    episodesData = episodesData.Substring(index);
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

                    Match nextPageMatch = Regex.Match(baseWebData, BarrandovTvUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = (nextPageMatch.Success) ? String.Format("{0}{1}", BarrandovTvUtil.baseUrl, HttpUtility.HtmlDecode(nextPageMatch.Groups["nextPageUrl"].Value)) : String.Empty;
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

                XmlNode hostname = document.SelectSingleNode("//hostname");
                XmlNode streamname = document.SelectSingleNode("//streamname");

                if ((hostname != null) && (streamname != null))
                {
                    String movieUrl = String.Format("rtmp://{0}/{1}", hostname.InnerText, streamname.InnerText);

                    string host = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf("/", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));
                    string app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                    string tcUrl = "rtmp://" + host + ":1935/" + app;
                    string playPath = movieUrl.Substring(movieUrl.IndexOf(app) + app.Length + 1);

                    string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}",
                            movieUrl, //rtmpUrl
                            host, //host
                            tcUrl, //tcUrl
                            app, //app
                            playPath //playpath
                            ));

                    video.PlaybackOptions.Add(video.Title, resultUrl);
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
