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
    public class JojUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.joj.sk/archiv.html";

        private static String dynamicCategoryStart = @"<div class=""archiveList preloader";

        private static String showStart = @"<ul class=""clearfix";
        private static String showEnd = @"</ul>";

        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)""[^>]*>(?<showTitle>[^<]+)";

        private static String showEpisodesStart = @"<div class=""box-carousel"">";
        private static String showEpisodesEnd = @"</div>";

        private static String showEpisodeStart = @"<li";
        private static String showEpisodeEnd = @"</li>";

        private static String showEpisodeUrlRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)";
        private static String showEpisodeDateRegex = @"<span class=""date"">(?<showEpisodeDate>[^<]+)";
        private static String showEpisodeTitleRegex = @"<span class=""title"">(?<showEpisodeTitle>[^<]+)";

        private static String videoIdRegex = @"(videoId: ""(?<videoId>[^""]+))|(videoId=(?<videoId>[^&]+))";
        private static String servicesUrlWithoutPageIdFormat = @"/services/Video.php?clip={0}";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public JojUtil()
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
            String baseWebData = GetWebData(JojUtil.baseUrl, null, null, null, true);
            List<RssLink> categories = new List<RssLink>();

            int index = baseWebData.IndexOf(JojUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                while (true)
                {
                    index = baseWebData.IndexOf(JojUtil.showStart);

                    if (index >= 0)
                    {
                        int endIndex = baseWebData.IndexOf(JojUtil.showEnd, index);

                        if (endIndex >= 0)
                        {
                            String showData = baseWebData.Substring(index, endIndex - index);

                            String showUrl = String.Empty;
                            String showTitle = String.Empty;

                            Match match = Regex.Match(showData, JojUtil.showUrlAndTitleRegex);
                            if (match.Success)
                            {
                                showTitle = match.Groups["showTitle"].Value;
                                showUrl = match.Groups["showUrl"].Value;
                                showData = showData.Substring(match.Index + match.Length);
                            }

                            if ((String.IsNullOrEmpty(showUrl)) && (String.IsNullOrEmpty(showTitle)))
                            {
                                break;
                            }

                            categories.Add(
                                new RssLink()
                                {
                                    Name = showTitle,
                                    Url = Utils.FormatAbsoluteUrl(showUrl, JojUtil.baseUrl)
                                });
                            dynamicCategoriesCount++;

                            baseWebData = baseWebData.Substring(endIndex);
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
                this.nextPageUrl = String.Empty;
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(JojUtil.showEpisodesStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(JojUtil.showEpisodesEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String showEpisodesData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        while (true)
                        {
                            startIndex = showEpisodesData.IndexOf(JojUtil.showEpisodeStart);
                            if (startIndex >= 0)
                            {
                                endIndex = showEpisodesData.IndexOf(JojUtil.showEpisodeEnd, startIndex);
                                if (endIndex >= 0)
                                {
                                    String showEpisodeUrl = String.Empty;
                                    String showEpisodeTitle = String.Empty;
                                    String showEpisodeDate = String.Empty;

                                    String showEpisodeData = showEpisodesData.Substring(startIndex, endIndex - startIndex);

                                    Match match = Regex.Match(showEpisodeData, JojUtil.showEpisodeUrlRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                        showEpisodeData = showEpisodeData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showEpisodeData, JojUtil.showEpisodeDateRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                        showEpisodeData = showEpisodeData.Substring(match.Index + match.Length);
                                    }

                                    match = Regex.Match(showEpisodeData, JojUtil.showEpisodeTitleRegex);
                                    if (match.Success)
                                    {
                                        showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                        showEpisodeData = showEpisodeData.Substring(match.Index + match.Length);
                                    }

                                    if (!((String.IsNullOrEmpty(showEpisodeUrl) || String.IsNullOrEmpty(showEpisodeTitle) || String.IsNullOrEmpty(showEpisodeDate))))
                                    {
                                        pageVideos.Add(new VideoInfo()
                                        {
                                            VideoUrl = showEpisodeUrl,
                                            Title = showEpisodeTitle,
                                            Airdate = showEpisodeDate
                                        });
                                    }

                                    showEpisodesData = showEpisodesData.Substring(endIndex);
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

                this.nextPageUrl = String.Empty;
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
            String baseWebData = GetWebData(video.VideoUrl, null, null, null, true);

            Match videoId = Regex.Match(baseWebData, JojUtil.videoIdRegex);

            video.PlaybackOptions = new Dictionary<string, string>();
            if (videoId.Success)
            {
                String safeVideoId = videoId.Groups["videoId"].Value.Replace("-", "%2D");
                String servicesUrl = String.Format(JojUtil.servicesUrlWithoutPageIdFormat, safeVideoId);
                servicesUrl = Utils.FormatAbsoluteUrl(servicesUrl, video.VideoUrl);

                XmlDocument videoData = new XmlDocument();
                videoData.LoadXml(GetWebData(servicesUrl));

                XmlNodeList files = videoData.SelectNodes("//file[@type=\"rtmp-archiv\"]");
                foreach (XmlNode file in files)
                {
                    String videoQualityUrl = "rtmp://n15.joj.sk/" + file.Attributes["path"].Value;

                    string host = videoQualityUrl.Substring(videoQualityUrl.IndexOf(":") + 3, videoQualityUrl.IndexOf("/", videoQualityUrl.IndexOf(":") + 3) - (videoQualityUrl.IndexOf(":") + 3));
                    string app = "";
                    string tcUrl = "rtmp://" + host;
                    string playPath = videoQualityUrl.Substring(videoQualityUrl.IndexOf(tcUrl) + tcUrl.Length + 1);

                    string resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(videoQualityUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath }.ToString();

                    video.PlaybackOptions.Add(file.Attributes["label"].Value, resultUrl);
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
