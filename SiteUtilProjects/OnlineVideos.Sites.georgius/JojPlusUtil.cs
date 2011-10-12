using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace OnlineVideos.Sites.georgius
{
    public class JojPlusUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://plus.joj.sk/plus-archiv.html";

        private static String dynamicCategoryStart = @"<div class=""j-filter-item"">";
        private static String showStart = @"<div class=""j-filter-item"">";
        private static String showUrlRegex = @"<li class=""trailer""><a href=""(?<showUrl>[^""]+)""";
        private static String showTitleRegex = @"<strong><a href=""[^""]+"" >(?<showTitle>[^<]+)";

        private static String showEpisodesStart = @"<div class=""b b-table";
        private static String showEpisodeStart = @"<tr>";
        private static String showEpisodeDateRegex = @"<td><b>(?<showEpisodeDate>[^<]*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)";

        private static String showEpisodeNextPageRegex = @"<a title=""Nasledujúce"" href=""(?<nextPageUrl>[^""]+)""";

        private static String pageIdRegex = @"pageId: ""(?<pageId>[^""]+)";
        private static String videoIdRegex = @"videoId: ""(?<videoId>[^""]+)";
        private static String servicesUrlFormat = @"http://www.joj.sk/services/Video.php?clip={0}&pageId={1}";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public JojPlusUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(JojPlusUtil.baseUrl, null, null, null, true);
            List<RssLink> categories = new List<RssLink>();

            int index = baseWebData.IndexOf(JojPlusUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                while (true)
                {
                    index = baseWebData.IndexOf(JojPlusUtil.showStart);

                    if (index >= 0)
                    {
                        int nextIndex = baseWebData.IndexOf(JojPlusUtil.showStart, index + JojPlusUtil.showStart.Length);

                        String webData = (nextIndex > 0) ? baseWebData.Substring(index, nextIndex - index) : baseWebData;
                        
                        String showUrl = String.Empty;
                        String showTitle = String.Empty;

                        Match match = Regex.Match(webData, JojPlusUtil.showTitleRegex);
                        if (match.Success)
                        {
                            showTitle = match.Groups["showTitle"].Value;
                            webData = webData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(webData, JojPlusUtil.showUrlRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            webData = webData.Substring(match.Index + match.Length);
                        }

                        if ((!String.IsNullOrEmpty(showUrl)) && (!String.IsNullOrEmpty(showTitle)))
                        {
                            categories.Add(
                                new RssLink()
                                {
                                    Name = showTitle,
                                    Url = Utils.FormatAbsoluteUrl(showUrl, JojPlusUtil.baseUrl)
                                });
                            dynamicCategoriesCount++;
                        }

                        baseWebData = (nextIndex > 0) ? baseWebData.Substring(nextIndex) : baseWebData.Substring(index + JojPlusUtil.showStart.Length);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (var category in categories.OrderBy(cat => cat.Name))
            {
                this.Settings.Categories.Add(category);
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

                int index = baseWebData.IndexOf(JojPlusUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    while (true)
                    {
                        index = baseWebData.IndexOf(JojPlusUtil.showEpisodeStart);

                        if (index > 0)
                        {
                            baseWebData = baseWebData.Substring(index);

                            String showEpisodeUrl = String.Empty;
                            String showEpisodeTitle = String.Empty;
                            String showEpisodeDate = String.Empty;

                            Match match = Regex.Match(baseWebData, JojPlusUtil.showEpisodeDateRegex);
                            if (match.Success)
                            {
                                showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, JojPlusUtil.showEpisodeUrlAndTitleRegex);
                            if (match.Success)
                            {
                                showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDate)))
                            {
                                break;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showEpisodeDate,
                                Title = showEpisodeTitle,
                                VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, JojPlusUtil.baseUrl)
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, JojPlusUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = nextPageMatch.Groups["nextPageUrl"].Value;
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
            return this.GetVideoList(category, JojPlusUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, JojPlusUtil.pageSize);
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

            Match pageId = Regex.Match(baseWebData, JojPlusUtil.pageIdRegex);
            Match videoId = Regex.Match(baseWebData, JojPlusUtil.videoIdRegex);

            video.PlaybackOptions = new Dictionary<string, string>();
            if (pageId.Success && videoId.Success)
            {
                String servicesUrl = String.Format(JojPlusUtil.servicesUrlFormat, videoId.Groups["videoId"].Value, pageId.Groups["pageId"].Value);

                XmlDocument videoData = new XmlDocument();
                videoData.LoadXml(SiteUtilBase.GetWebData(servicesUrl));

                XmlNode highQuality = videoData.SelectSingleNode("//file[@type = \"rtmp-archiv\"]");
                XmlNode lowQuality = videoData.SelectSingleNode("//file[@type = \"flv-archiv\"]");

                String highQualityUrl = String.Empty;
                String lowQualityUrl = String.Empty;

                if ((highQuality != null) && (lowQuality != null))
                {
                    highQualityUrl = "rtmp://n05.joj.sk/" + highQuality.Attributes["path"].Value;
                    lowQualityUrl = "rtmp://n05.joj.sk/" + lowQuality.Attributes["path"].Value;
                }
                else if (highQuality != null)
                {
                    highQualityUrl = "rtmp://n05.joj.sk/" + highQuality.Attributes["path"].Value;
                }
                else if (lowQuality != null)
                {
                    lowQualityUrl = "rtmp://n05.joj.sk/" + lowQuality.Attributes["path"].Value;
                }

                if (!String.IsNullOrEmpty(lowQualityUrl))
                {
                    string host = lowQualityUrl.Substring(lowQualityUrl.IndexOf(":") + 3, lowQualityUrl.IndexOf("/", lowQualityUrl.IndexOf(":") + 3) - (lowQualityUrl.IndexOf(":") + 3));
                    string app = "";
                    string tcUrl = "rtmp://" + host;
                    string playPath = lowQualityUrl.Substring(lowQualityUrl.IndexOf(tcUrl) + tcUrl.Length + 1);

                    string resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}",
                            lowQualityUrl, //rtmpUrl
                            host, //host
                            tcUrl, //tcUrl
                            app, //app
                            playPath //playpath
                            ));

                    video.PlaybackOptions.Add("Low quality", resultUrl);
                }
                if (!String.IsNullOrEmpty(highQualityUrl))
                {
                    string host = highQualityUrl.Substring(highQualityUrl.IndexOf(":") + 3, highQualityUrl.IndexOf("/", highQualityUrl.IndexOf(":") + 3) - (highQualityUrl.IndexOf(":") + 3));
                    string app = "";
                    string tcUrl = "rtmp://" + host;
                    string playPath = highQualityUrl.Substring(highQualityUrl.IndexOf(tcUrl) + tcUrl.Length + 1);

                    string resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}",
                            highQualityUrl, //rtmpUrl
                            host, //host
                            tcUrl, //tcUrl
                            app, //app
                            playPath //playpath
                            ));

                    video.PlaybackOptions.Add("High quality", resultUrl);
                }
            }

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return String.Empty;
        }

        #endregion
    }
}
