using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.ComponentModel;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public class SlovenskaTeleviziaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.stv.sk";
        private static String showsBaseUrl = @"http://www.stv.sk/online/archiv/";

        private static String dynamicCategoryStartRegex = @"<ul id=""[^""]+"" class=""(arch-list)|(arch-list last)"">";
        private static String dynamicCategoryEnd = @"</ul>";
        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>";

        private static String showEpisodesStart = @"<th class=""arr"">";
        private static String showEpisodesEnd = @"</table>";
        private static String showEpisodeUrlRegex = @"(<a href=""(?<showEpisodeUrl>[^""]+)"" class=""bc-one"")|(<a href=""(?<showEpisodeUrl>[^""]+)"" class=""bc-two"")|(<a href=""(?<showEpisodeUrl>[^""]+)"" class=""act"")";
        private static String dateRegex = @"\?date=(?<date>[^\&]*)";

        private static String nextPageUrlRegex = @"<a href=""(?<nextPageUrl>[^""]*)"" class=""prew"">";
        
        private static String flashVarsRegex = @"so.addParam\('flashvars','playlistfile=(?<playListFile>[^\&]+)&";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public SlovenskaTeleviziaUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(SlovenskaTeleviziaUtil.showsBaseUrl, null, null, null, true);

            while (true)
            {
                Match start = Regex.Match(baseWebData, SlovenskaTeleviziaUtil.dynamicCategoryStartRegex);
                if (start.Success)
                {
                    int end = baseWebData.IndexOf(SlovenskaTeleviziaUtil.dynamicCategoryEnd, start.Index);
                    if (end > 0)
                    {
                        String showsData = baseWebData.Substring(start.Index, end - start.Index);

                        while (true)
                        {
                            String showUrl = String.Empty;
                            String showTitle = String.Empty;

                            Match match = Regex.Match(showsData, SlovenskaTeleviziaUtil.showUrlAndTitleRegex);
                            if (match.Success)
                            {
                                showUrl = match.Groups["showUrl"].Value;
                                showTitle = match.Groups["showTitle"].Value;
                                showsData = showsData.Substring(match.Index + match.Length);
                            }

                            if ((String.IsNullOrEmpty(showUrl)) && (String.IsNullOrEmpty(showTitle)))
                            {
                                break;
                            }

                            this.Settings.Categories.Add(
                                new RssLink()
                                {
                                    Name = showTitle,
                                    Url = String.Format("{0}{1}", SlovenskaTeleviziaUtil.baseUrl, showUrl),
                                });
                            dynamicCategoriesCount++;
                        }

                        baseWebData = baseWebData.Substring(end);
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

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl, String showTitle)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int start = baseWebData.IndexOf(SlovenskaTeleviziaUtil.showEpisodesStart);
                if (start > 0)
                {
                    int end = baseWebData.IndexOf(SlovenskaTeleviziaUtil.showEpisodesEnd, start);
                    if (end > 0)
                    {
                        baseWebData = baseWebData.Substring(start, end - start);

                        MatchCollection matches = Regex.Matches(baseWebData, SlovenskaTeleviziaUtil.showEpisodeUrlRegex);
                        for (int i = (matches.Count - 1); i >= 0; i--)
                        {
                            Match match = matches[i];
                            String showEpisodeUrl = String.Empty;
                            String showEpisodeTitle = String.Empty;
                            String showEpisodeDate = String.Empty;

                            showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;

                            if (String.IsNullOrEmpty(showEpisodeUrl))
                            {
                                continue;
                            }

                            match = Regex.Match(showEpisodeUrl, SlovenskaTeleviziaUtil.dateRegex);
                            if (match.Success)
                            {
                                showEpisodeDate = DateTime.Parse(match.Groups["date"].Value).ToShortDateString();
                            }

                            showEpisodeTitle = String.Format("{0} - {1}", showTitle, showEpisodeDate);
                            showEpisodeUrl = Utils.FormatAbsoluteUrl(showEpisodeUrl, pageUrl);

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showEpisodeDate,
                                Title = showEpisodeTitle,
                                VideoUrl = showEpisodeUrl
                            };

                            pageVideos.Add(videoInfo);
                        }
                    }

                    Match nextPageUrlMatch = Regex.Match(baseWebData, SlovenskaTeleviziaUtil.nextPageUrlRegex);
                    this.nextPageUrl = (nextPageUrlMatch.Success) ? Utils.FormatAbsoluteUrl(nextPageUrlMatch.Groups["nextPageUrl"].Value, pageUrl) : String.Empty;
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
            int emptyMonths = 0;

            while (true)
            {
                while (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) && (addedVideos < videoCount))
                {
                    videoList.Add(this.loadedEpisodes[this.currentStartIndex + addedVideos]);
                    addedVideos++;
                }

                if (addedVideos < videoCount)
                {
                    List<VideoInfo> loadedVideos = this.GetPageVideos(this.nextPageUrl, this.currentCategory.Name);

                    if ((loadedVideos.Count == 0) && (emptyMonths >= 12))
                    {
                        this.nextPageUrl = String.Empty;
                        break;
                    }
                    else if (loadedVideos.Count == 0)
                    {
                        emptyMonths++;
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
            return this.GetVideoList(category, SlovenskaTeleviziaUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, SlovenskaTeleviziaUtil.pageSize);
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
            video.PlaybackOptions = new Dictionary<string, string>();
            Match match = Regex.Match(baseWebData, SlovenskaTeleviziaUtil.flashVarsRegex);
            if (match.Success)
            {
                baseWebData = SiteUtilBase.GetWebData(match.Groups["playListFile"].Value.Replace("%26", "&"), null, null, null, true).Replace("xmlns=\"http://xspf.org/ns/0/\"", "");
                XmlDocument movieData = new XmlDocument();
                movieData.LoadXml(baseWebData);

                XmlNode location = movieData.SelectSingleNode("//location");
                XmlNode stream = movieData.SelectSingleNode("//meta[@rel = \"streamer\"]");

                if ((location != null) && (stream != null))
                {
                    String movieUrl = stream.InnerText + "/" + location.InnerText;

                    string host = String.Empty;
                    string port = "1935";
                    string firstSlash = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf("/", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));

                    String[] parts = firstSlash.Split(':');
                    if (parts.Length == 2)
                    {
                        // host name and port
                        host = parts[0];
                        port = parts[1];
                    }
                    else
                    {
                        host = firstSlash;
                    }

                    string app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                    string tcUrl = "rtmp://" + host + "/" + app;
                    string playPath = "mp4:" + movieUrl.Substring(movieUrl.IndexOf(app) + app.Length + 1);

                    string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&playpath={4}&port={5}&live=true",
                            movieUrl, //rtmpUrl
                            host, //host
                            tcUrl, //tcUrl
                            app, //app
                            playPath, //playpath
                            port // port
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
