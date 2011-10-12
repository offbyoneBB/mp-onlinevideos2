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

        private static String dynamicCategoryStart = @"<div class=""j-filter-item"">";
        private static String showStart = @"<div class=""j-filter-item"">";
        private static String showUrlRegex = @"<li class=""trailer""><a href=""(?<showUrl>[^""]+)""";
        private static String showTitleRegex = @"(<strong><a href=""[^""]+"" >(?<showTitle>[^<]+))|(<strong><a href=""[^""]+"" target=""_blank"">(?<showTitle>[^<]+))";

        private static List<ShowEpisodesRegex> showsAndEpisodes = new List<ShowEpisodesRegex>()
        {            
            new ShowEpisodesRegex()
            {
                 ShowNames = new List<string>() { "Farmár hľadá ženu" },
                 ShowEpisodesBlockStartRegex = @"<ul class=""l c"">",
                 ShowEpisodesBlockEndRegex = @"</ul>",
                 ShowEpisodeStartRegex = @"<li",
                 ShowEpisodeEndRegex = @"</li>",
                 ShowEpisodeUrlAndTitleRegex = @"<a title=""(?<showEpisodeTitle>[^""]+)"" href=""(?<showEpisodeUrl>[^""]+)"">",
                 ShowEpisodeThumbUrlRegex = @"<img alt=""[^""]"" src=""(?<showEpisodeThumbUrl>[^""]+)""",
                 ShowEpisodeDescriptionRegex = @"<span class=""[^""]*"">(?<showEpisodeDescription>[^<]+)",
                 SkipFirstPage = false
            },

            new ShowEpisodesRegex()
            {
                 ShowNames = new List<string>() { "ČESKO SLOVENSKO MÁ TALENT" },
                 ShowEpisodesBlockStartRegex = @"<ul class=""l c"">",
                 ShowEpisodesBlockEndRegex = @"</ul>",
                 ShowEpisodeStartRegex = @"<li",
                 ShowEpisodeEndRegex = @"</li>",
                 ShowEpisodeUrlAndTitleRegex = @"<a title=""(?<showEpisodeTitle>[^""]+)"" href=""(?<showEpisodeUrl>[^""]+)"">",
                 ShowEpisodeThumbUrlRegex = @"<img alt=""[^""]"" width=""[^""]"" height=""[^""]"" src=""(?<showEpisodeThumbUrl>[^""]+)""",
                 SkipFirstPage = false
            },

            new ShowEpisodesRegex()
            {
                 ShowNames = new List<string>() { "Mama, ožeň ma!" },
                 ShowEpisodesBlockStartRegex = @"<ul class=""l c"">",
                 ShowEpisodesBlockEndRegex = @"</ul>",
                 ShowEpisodeStartRegex = @"<li",
                 ShowEpisodeEndRegex = @"</li>",
                 ShowEpisodeUrlAndTitleRegex = @"<a title=""(?<showEpisodeTitle>[^""]+)"" href=""(?<showEpisodeUrl>[^""]+)"">",
                 ShowEpisodeThumbUrlRegex = @"<img width=""[^""]"" height=""[^""]"" alt=""[^""]"" src=""(?<showEpisodeThumbUrl>[^""]+)""",
                 SkipFirstPage = false
            },
            
            new ShowEpisodesRegex()
            {
                 ShowNames = new List<string>(),
                 ShowEpisodesBlockStartRegex = @"<div class=""b b-table",
                 ShowEpisodesBlockEndRegex = @"<script type=""text/javascript""",
                 ShowEpisodeStartRegex = @"<tr>",
                 ShowEpisodeEndRegex = @"</tr>",
                 ShowEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)",
                 ShowEpisodesNextPageRegex = @"<a title=""Nasledujúce"" href=""(?<nextPageUrl>[^""]+)""",
                 ShowEpisodeDescriptionRegex = @"<td><b>(?<showEpisodeDescription>[^<]*)",
                 SkipFirstPage = false
            }
        };

        private static String pageIdRegex = @"pageId: ""(?<pageId>[^""]+)";
        private static String videoIdRegex = @"(videoId: ""(?<videoId>[^""]+))|(videoId=(?<videoId>[^&]+))";
        private static String servicesUrlFormat = @"/services/Video.php?clip={0}&pageId={1}";
        private static String servicesUrlWithoutPageIdFormat = @"/services/Video.php?clip={0}";

        // the number of show episodes per page
        private static int pageSize = 28;

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
            String baseWebData = SiteUtilBase.GetWebData(JojUtil.baseUrl, null, null, null, true);
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
                        baseWebData = baseWebData.Substring(index);

                        String showUrl = String.Empty;
                        String showTitle = String.Empty;

                        Match match = Regex.Match(baseWebData, JojUtil.showTitleRegex);
                        if (match.Success)
                        {
                            showTitle = match.Groups["showTitle"].Value;
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(baseWebData, JojUtil.showUrlRegex);
                        if (match.Success)
                        {
                            showUrl = match.Groups["showUrl"].Value;
                            baseWebData = baseWebData.Substring(match.Index + match.Length);
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

        private List<VideoInfo> GetPageVideos(String pageUrl, ShowEpisodesRegex showEpisodesRegex)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                Match showEpisodesStart = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesBlockStartRegex);
                if (showEpisodesStart.Success)
                {
                    baseWebData = baseWebData.Substring(showEpisodesStart.Index);

                    if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodesBlockEndRegex))
                    {
                        Match showEpisodesEnd = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesBlockEndRegex);
                        if (showEpisodesEnd.Success)
                        {
                            baseWebData = baseWebData.Substring(0, showEpisodesEnd.Index);
                        }
                    }

                    while (true)
                    {
                        Match episode = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodeStartRegex);
                        if (episode.Success)
                        {
                            String episodeData = String.Empty;

                            if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodeEndRegex))
                            {
                                String tempEpisodeData = baseWebData.Substring(episode.Index + episode.Length);
                                Match episodeEnd = Regex.Match(tempEpisodeData, showEpisodesRegex.ShowEpisodeEndRegex);
                                if (episodeEnd.Success)
                                {
                                    episodeData = tempEpisodeData.Substring(0, episodeEnd.Index);
                                    baseWebData = tempEpisodeData.Substring(episodeEnd.Index + episodeEnd.Length);
                                }
                            }

                            if (String.IsNullOrEmpty(episodeData))
                            {
                                episodeData = baseWebData.Substring(episode.Index + episode.Length);
                                baseWebData = baseWebData.Substring(episode.Index + episode.Length);
                            }

                            String showEpisodeUrl = String.Empty;
                            String showEpisodeTitle = String.Empty;
                            String showEpisodeDescription = String.Empty;
                            String showEpisodeThumbUrl = String.Empty;
                            String showEpisodeLength = String.Empty;

                            if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodeDescriptionRegex))
                            {
                                Match match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeDescriptionRegex);
                                if (match.Success)
                                {
                                    showEpisodeDescription = match.Groups["showEpisodeDescription"].Value;
                                }
                            }

                            if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodeThumbUrlRegex))
                            {
                                Match match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeThumbUrlRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumbUrl = match.Groups["showEpisodeThumbUrl"].Value;
                                }
                            }

                            if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodeUrlAndTitleRegex))
                            {
                                Match match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                    showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                }
                            }

                            if (!(String.IsNullOrEmpty(showEpisodeTitle) || String.IsNullOrEmpty(showEpisodeUrl)))
                            {
                                VideoInfo videoInfo = new VideoInfo()
                                {
                                    Description = showEpisodeDescription,
                                    Title = showEpisodeTitle,
                                    ImageUrl = (!String.IsNullOrEmpty(showEpisodeThumbUrl)) ? Utils.FormatAbsoluteUrl(showEpisodeThumbUrl, new Uri(pageUrl).GetLeftPart(UriPartial.Authority)) : String.Empty,
                                    VideoUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, new Uri(pageUrl).GetLeftPart(UriPartial.Authority))
                                };

                                pageVideos.Add(videoInfo);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                this.nextPageUrl = String.Empty;
                if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodesNextPageRegex))
                {
                    Match nextPageMatch = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesNextPageRegex);
                    if (nextPageMatch.Success)
                    {
                        String subUrl = HttpUtility.HtmlDecode(nextPageMatch.Groups["url"].Value);
                        this.nextPageUrl = Utils.FormatAbsoluteUrl(subUrl, pageUrl);
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

            ShowEpisodesRegex showEpisodesRegex = null;
            foreach (var showAndEpisodes in JojUtil.showsAndEpisodes)
            {
                if ((showAndEpisodes.ShowNames.Contains(this.currentCategory.Name)) || (showAndEpisodes.ShowNames.Count == 0))
                {
                    showEpisodesRegex = showAndEpisodes;
                    break;
                }
            }

            while (true)
            {
                while (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) && (addedVideos < videoCount))
                {
                    videoList.Add(this.loadedEpisodes[this.currentStartIndex + addedVideos]);
                    addedVideos++;
                }

                if (addedVideos < videoCount)
                {
                    if (showEpisodesRegex.SkipFirstPage && (this.loadedEpisodes.Count == 0))
                    {
                        // skip first page and get next page url
                        this.GetPageVideos(this.nextPageUrl, showEpisodesRegex);
                    }
                    List<VideoInfo> loadedVideos = this.GetPageVideos(this.nextPageUrl, showEpisodesRegex);

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
            return this.GetVideoList(category, JojUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, JojUtil.pageSize);
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

            Match pageId = Regex.Match(baseWebData, JojUtil.pageIdRegex);
            Match videoId = Regex.Match(baseWebData, JojUtil.videoIdRegex);

            video.PlaybackOptions = new Dictionary<string, string>();
            if (videoId.Success)
            {
                String safeVideoId = videoId.Groups["videoId"].Value.Replace("-", "%2D");
                String servicesUrl = (pageId.Success) ? String.Format(JojUtil.servicesUrlFormat, safeVideoId, pageId.Groups["pageId"].Value) : String.Format(JojUtil.servicesUrlWithoutPageIdFormat, safeVideoId);
                servicesUrl = Utils.FormatAbsoluteUrl(servicesUrl, video.VideoUrl);

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
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}&live=true",
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
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}&live=true",
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
