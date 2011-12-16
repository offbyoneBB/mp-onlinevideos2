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

        private static String baseUrl = "http://voyo.nova.cz/serialy";

        private static String dynamicCategoryStart = @"<!--p id=""seriesFilterAllSeries9055d130301""";
        private static String dynamicCategoryEnd = @"<div class=""cw_1""";

        private static String categoryNextPage = @"<a href='(?<categoryNextPage>[^']*)' onclick='[^']*'>další</a>";

        private static String showStart = @"<div class='poster'>";
        private static String showEnd = @"</div>";

		private static String showUrlTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>";
        private static String showThumbRegex = @"<img src='(?<showThumbUrl>[^']*)";

        private static String showEpisodesStart = @"<div class=""productsList"">";

        private static String showEpisodeBlockStart = @"<div class='section_item'>";
        private static String showEpisodeBlockEnd = @"<div class='clearer'>";

        private static String showEpisodeThumbUrlRegex = @"<img src='(?<showThumbUrl>[^']*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>";
        private static String showEpisodeDescriptionStart = @"<div class=""padding"" >";
        private static String showEpisodeDescriptionEnd = @"</div>";

        private static String showEpisodeNextPageRegex = @"<a href='(?<nextPageUrl>[^']*)' onclick='[^']*'>další</a>";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        private static String mediaIdRegex = @"mainVideo = new mediaData\([^,]*, [^,]*, (?<mediaId>[^,]*)";

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
            List<RssLink> unsortedCategories = new List<RssLink>();
            String pageUrl = NovaUtil.baseUrl;

            while (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);
                pageUrl = String.Empty;

                int startIndex = baseWebData.IndexOf(NovaUtil.dynamicCategoryStart);
                if (startIndex > 0)
                {
                    int endIndex = baseWebData.IndexOf(NovaUtil.dynamicCategoryEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        Match match = Regex.Match(baseWebData, NovaUtil.categoryNextPage);
                        if (match.Success)
                        {
                            pageUrl = Utils.FormatAbsoluteUrl(match.Groups["categoryNextPage"].Value, NovaUtil.baseUrl);
                        }

                        while (true)
                        {
                            int showStartIndex = baseWebData.IndexOf(NovaUtil.showStart);
                            if (showStartIndex >= 0)
                            {
                                int showEndIndex = baseWebData.IndexOf(NovaUtil.showEnd, showStartIndex);
                                if (showEndIndex >= 0)
                                {
                                    String showData = baseWebData.Substring(showStartIndex, showEndIndex - showStartIndex);

                                    String showUrl = String.Empty;
                                    String showTitle = String.Empty;
                                    String showThumbUrl = String.Empty;

                                    match = Regex.Match(showData, NovaUtil.showUrlTitleRegex);
                                    if (match.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, NovaUtil.baseUrl);
                                        showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                    }

                                    match = Regex.Match(showData, NovaUtil.showThumbRegex);
                                    if (match.Success)
                                    {
                                        showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, NovaUtil.baseUrl);
                                    }

                                    if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                    {
                                        unsortedCategories.Add(new RssLink()
                                        {
                                            Name = showTitle,
                                            HasSubCategories = false,
                                            Url = showUrl,
                                            Thumb = showThumbUrl
                                        });
                                    }
                                }

                                baseWebData = baseWebData.Substring(showStartIndex + NovaUtil.showStart.Length);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var category in unsortedCategories.OrderBy(cat => cat.Name))
            {
                this.Settings.Categories.Add(category);
                dynamicCategoriesCount++;
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();
            this.nextPageUrl = String.Empty;

            if (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(NovaUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    Match match = Regex.Match(baseWebData, NovaUtil.showEpisodeNextPageRegex);
                    if (match.Success)
                    {
                        this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, NovaUtil.baseUrl);
                    }

                    while (true)
                    {
                        int showEpisodeBlockStart = baseWebData.IndexOf(NovaUtil.showEpisodeBlockStart);
                        if (showEpisodeBlockStart >= 0)
                        {
                            int showEpisodeBlockEnd = baseWebData.IndexOf(NovaUtil.showEpisodeBlockEnd, showEpisodeBlockStart);
                            if (showEpisodeBlockEnd >= 0)
                            {
                                String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                String showTitle = String.Empty;
                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showDescription = String.Empty;

                                match = Regex.Match(showData, NovaUtil.showEpisodeThumbUrlRegex);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, NovaUtil.baseUrl);
                                }

                                match = Regex.Match(showData, NovaUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, NovaUtil.baseUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                int descriptionStart = showData.IndexOf(NovaUtil.showEpisodeDescriptionStart);
                                if (descriptionStart >= 0)
                                {
                                    int descriptionEnd = showData.IndexOf(NovaUtil.showEpisodeDescriptionEnd, descriptionStart);
                                    if (descriptionEnd >= 0)
                                    {
                                        showDescription = showData.Substring(descriptionStart + NovaUtil.showEpisodeDescriptionStart.Length, descriptionEnd - descriptionStart - NovaUtil.showEpisodeDescriptionStart.Length);
                                    }
                                }

                                if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                {
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

                            baseWebData = baseWebData.Substring(showEpisodeBlockStart + NovaUtil.showEpisodeBlockStart.Length);
                        }
                        else
                        {
                            break;
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
            Match mediaIdMatch = Regex.Match(baseWebData, NovaUtil.mediaIdRegex);

            if ((video.PlaybackOptions == null) && (mediaIdMatch.Success))
            {
                video.PlaybackOptions = new Dictionary<string, string>();

                if (mediaIdMatch.Success)
                {
                    String mediaId = mediaIdMatch.Groups["mediaId"].Value;

                    String time = DateTime.Now.ToString("yyyyMMddHHmmss");
                    String signature = String.Format("nova-vod|{0}|{1}|tajne.heslo", mediaId, time);
                    String encodedHash = String.Empty;
                    using (MD5 md5 = MD5.Create())
                    {
                        Byte[] md5hash = md5.ComputeHash(Encoding.Default.GetBytes(signature));
                        encodedHash = Convert.ToBase64String(md5hash);
                    }

                    String videoPlaylistUrl = String.Format("http://master-ng.nacevi.cz/cdn.server/PlayerLink.ashx?t={1}&c=nova-vod|{0}&h=0&d=1&s={2}&tm=nova", mediaId, time, encodedHash);
                    String videoPlaylistWebData = SiteUtilBase.GetWebData(videoPlaylistUrl);

                    XmlDocument videoPlaylist = new XmlDocument();
                    videoPlaylist.LoadXml(videoPlaylistWebData);

                    if (videoPlaylist.SelectSingleNode("//baseUrl") != null)
                    {
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

                            switch (quality.ToUpperInvariant())
                            {
                                case "FLV":
                                case "LQ":
                                    video.PlaybackOptions.Add("Low quality", resultUrl);
                                    break;
                                case "HQ":
                                    video.PlaybackOptions.Add("High quality", resultUrl);
                                    break;
                                default:
                                    video.PlaybackOptions.Add(quality.ToUpperInvariant(), resultUrl);
                                    break;
                            }
                            
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
            return "";
        }

        #endregion
    }
}
