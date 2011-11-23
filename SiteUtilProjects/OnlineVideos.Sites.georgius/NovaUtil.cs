using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections;
using System.Xml;
using System.Security.Cryptography;

namespace OnlineVideos.Sites.georgius
{
    public class NovaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://archiv.nova.cz";

        private static String dynamicCategoryStart = @"<li class=""menu_item"" id=""main_letter"">";
        private static String showRegex = @"<a class=""list_item"" href=""(?<showUrl>[^""]*)"">(?<showTitle>[^""]*)</a>";

        private static String showEpisodesStart = @"<div id=""searched_videos"">";

        private static String showEpisodeBlockStartRegex = @"<li class=""catchup_related_video status";
        private static String showEpisodeThumbUrlRegex = @"<img class=""img"" src=""(?<showThumbUrl>[^""]*)""";
        private static String showEpisodeUrlAndTitleRegex = @"<a class=""title_url"" href=""(?<showUrl>[^""]*)"">(?<showTitle>[^<]*)</a>";
        private static String showEpisodeDateLengthRegex = @"<span class=""date"">(?<showDate>[^\s]+) \((?<showLength>[^\)]+)\)</span>";
        private static String showEpisodeDescriptionRegex = @"<p class=""perex"">(?<showDescription>[^<]*)</p>";

        private static String showEpisodeNextPageBlockStartRegex = @"<div id=""pager"" class=""lister"">";
        private static String showEpisodeNextPageStartRegex = @"<span class=""selected";
        private static String showEpisodeNextPageRegex = @"<a href=""(?<nextPageUrl>[^""]+)"" class=""normal";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        private static String variableBlockRegex = @"<script language=""JavaScript1.1"" type=""text/javascript"">(?<variableBlock>[^<]+)</script>";
        private static String variableRegex = @"var[\s]+(?<variableName>[^\s]+)[\s]+=[\s]+(""|')?(?<variableValue>[^'"";]+)(""|')?;";

        #endregion

        #region Constructors

        public NovaUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(NovaUtil.baseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(NovaUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                Match match = Regex.Match(baseWebData, NovaUtil.showRegex);
                while (match.Success)
                {
                    String showUrl = match.Groups["showUrl"].Value;
                    String showTitle = match.Groups["showTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = showTitle,
                            HasSubCategories = false,
                            Url = String.Format("{0}{1}", NovaUtil.baseUrl, showUrl)
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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(NovaUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);


                    while (true)
                    {
                        Match showEpisodeBlockStart = Regex.Match(baseWebData, NovaUtil.showEpisodeBlockStartRegex);
                        if (showEpisodeBlockStart.Success)
                        {
                            baseWebData = baseWebData.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                            String showTitle = String.Empty;
                            String showThumbUrl = String.Empty;
                            String showUrl = String.Empty;
                            String showLength = String.Empty;
                            String showDescription = String.Empty;
                            String showDate = String.Empty;

                            Match showEpisodeThumbUrl = Regex.Match(baseWebData, NovaUtil.showEpisodeThumbUrlRegex);
                            if (showEpisodeThumbUrl.Success)
                            {
                                showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                                baseWebData = baseWebData.Substring(showEpisodeThumbUrl.Index + showEpisodeThumbUrl.Length);
                            }

                            Match showEpisodeUrlAndTitle = Regex.Match(baseWebData, NovaUtil.showEpisodeUrlAndTitleRegex);
                            if (showEpisodeUrlAndTitle.Success)
                            {
                                showUrl = showEpisodeUrlAndTitle.Groups["showUrl"].Value;
                                showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value;
                                baseWebData = baseWebData.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                            }

                            Match showEpisodeDateLength = Regex.Match(baseWebData, NovaUtil.showEpisodeDateLengthRegex);
                            if (showEpisodeDateLength.Success)
                            {
                                showLength = showEpisodeDateLength.Groups["showLength"].Value;
                                showDate = showEpisodeDateLength.Groups["showDate"].Value;
                                baseWebData = baseWebData.Substring(showEpisodeDateLength.Index + showEpisodeDateLength.Length);
                            }

                            Match showEpisodeDescription = Regex.Match(baseWebData, NovaUtil.showEpisodeDescriptionRegex);
                            if (showEpisodeDescription.Success)
                            {
                                showDescription = showEpisodeDescription.Groups["showDescription"].Value;
                                baseWebData = baseWebData.Substring(showEpisodeDescription.Index + showEpisodeDescription.Length);
                            }

                            if (!((showEpisodeThumbUrl.Success) && (showEpisodeDateLength.Success) && (showEpisodeUrlAndTitle.Success) && (showEpisodeDescription.Success)))
                            {
                                break;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showDescription,
                                ImageUrl = showThumbUrl,
                                Length = showLength,
                                Title = showTitle,
                                VideoUrl = String.Format("{0}{1}", NovaUtil.baseUrl, showUrl)
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }


                    Match showEpisodeNextPageBlockStart = Regex.Match(baseWebData, NovaUtil.showEpisodeNextPageBlockStartRegex);
                    if (showEpisodeNextPageBlockStart.Success)
                    {
                        baseWebData = baseWebData.Substring(showEpisodeNextPageBlockStart.Index + showEpisodeNextPageBlockStart.Length);

                        Match showEpisodeNextPageStart = Regex.Match(baseWebData, NovaUtil.showEpisodeNextPageStartRegex);
                        if (showEpisodeNextPageStart.Success)
                        {
                            baseWebData = baseWebData.Substring(showEpisodeNextPageStart.Index + showEpisodeNextPageStart.Length);

                            Match nextPageMatch = Regex.Match(baseWebData, NovaUtil.showEpisodeNextPageRegex);
                            this.nextPageUrl = (nextPageMatch.Success) ? String.Format("{0}{1}", NovaUtil.baseUrl, HttpUtility.HtmlDecode(nextPageMatch.Groups["nextPageUrl"].Value)) : String.Empty;
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
            return this.GetVideoList(category, NovaUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, NovaUtil.pageSize);
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
            Match variableBlockMatch = Regex.Match(baseWebData, NovaUtil.variableBlockRegex);

            if ((video.PlaybackOptions == null) && (variableBlockMatch.Success))
            {
                video.PlaybackOptions = new Dictionary<string, string>();

                if (variableBlockMatch.Success)
                {
                    String variableBlock = variableBlockMatch.Groups["variableBlock"].Value;

                    MatchCollection matches = Regex.Matches(variableBlock, NovaUtil.variableRegex);
                    Hashtable variables = new Hashtable(matches.Count);
                    foreach (Match match in matches)
                    {
                        variables.Add(match.Groups["variableName"].Value, match.Groups["variableValue"].Value);
                    }

                    String time = DateTime.Now.ToString("yyyyMMddHHmmss");
                    String signature = String.Format("nova-vod|{0}|{1}|tajne.heslo", variables["media_id"], time);
                    String encodedHash = String.Empty;
                    using (MD5 md5 = MD5.Create())
                    {
                        Byte[] md5hash = md5.ComputeHash(Encoding.Default.GetBytes(signature));
                        encodedHash = Convert.ToBase64String(md5hash);
                    }

                    String videoPlaylistUrl = String.Format("http://master-ng.nacevi.cz/cdn.server/PlayerLink.ashx?t={1}&c=nova-vod|{0}&h=0&d=1&s={2}&tm=nova", variables["media_id"], time, encodedHash);
                    String videoPlaylistWebData = SiteUtilBase.GetWebData(videoPlaylistUrl);

                    XmlDocument videoPlaylist = new XmlDocument();
                    videoPlaylist.LoadXml(videoPlaylistWebData);

                    String videoBaseUrl = videoPlaylist.SelectSingleNode("//baseUrl").InnerText;

                    foreach (XmlNode node in videoPlaylist.SelectNodes("//media"))
                    {
                        String quality = node.SelectSingleNode("quality").InnerText;
                        String url = node.SelectSingleNode("url").InnerText;

                        String movieUrl = String.Format("{0}/{1}", videoBaseUrl, url);

                        String host = movieUrl.Substring(movieUrl.IndexOf(":") + 3, movieUrl.IndexOf(":", movieUrl.IndexOf(":") + 3) - (movieUrl.IndexOf(":") + 3));
                        String app = movieUrl.Substring(movieUrl.IndexOf("/", host.Length) + 1, movieUrl.IndexOf("/", movieUrl.IndexOf("/", host.Length) + 1) - movieUrl.IndexOf("/", host.Length) - 1);
                        String tcUrl = videoBaseUrl;
                        String playPath = url;

                        string resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(movieUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath }.ToString();

                        video.PlaybackOptions.Add(((quality == "flv") || (quality == "lq")) ? "Low quality" : "High quality", resultUrl);
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
