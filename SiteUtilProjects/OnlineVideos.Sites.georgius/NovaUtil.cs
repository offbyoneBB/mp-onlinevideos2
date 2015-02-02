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

        private static String dynamicCategoryStart = @"<div class=""productsList series"" id=";
        private static String dynamicCategoryEnd = @"<div class=""productsPagination"">";

        private static String categoryNextPage = @"<a href=""(?<categoryNextPage>[^""]*)"" onclick=""[^""]*"">další</a>";

        private static String showStart = @"<div class=""poster"">";
        private static String showEnd = @"<div class=""ratings"">";

		private static String showUrlTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""(?<showTitle>[^""]*)""";
        private static String showThumbRegex = @"<img src=""(?<showThumbUrl>[^""]*)";

        private static String showEpisodesStart = @"<div class=""productsList series"" id=";

        private static String showEpisodeBlockStart = @"<div class=""poster"">";
        private static String showEpisodeBlockEnd = @"<div class=""clearer"">";

        private static String showEpisodeThumbUrlRegex = @"<img src=""(?<showThumbUrl>[^""]*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""(?<showTitle>[^""]*)""";

        private static String showEpisodeNextPageRegex = @"<a href=""(?<nextPageUrl>[^""]*)"" onclick=""[^""]*"">další</a>";

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
            String pageUrl = NovaUtil.baseUrl;

            String baseWebData = GetWebData(pageUrl, null, null, null, true);
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
                                    this.Settings.Categories.Add(new RssLink()
                                    {
                                        Name = showTitle,
                                        HasSubCategories = false,
                                        Url = showUrl,
                                        Thumb = showThumbUrl
                                    });
                                    dynamicCategoriesCount++;
                                }
                            }

                            baseWebData = baseWebData.Substring(showStartIndex + NovaUtil.showStart.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!String.IsNullOrEmpty(pageUrl))
                    {
                        this.Settings.Categories.Add(new NextPageCategory() { Url = pageUrl });
                    }
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            int dynamicCategoriesCount = 0;
            String pageUrl = category.Url;

            String baseWebData = GetWebData(pageUrl, null, null, null, true);
            pageUrl = String.Empty;

            this.Settings.Categories.RemoveAt(this.Settings.Categories.Count - 1);

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
                                    this.Settings.Categories.Add(new RssLink()
                                    {
                                        Name = showTitle,
                                        HasSubCategories = false,
                                        Url = showUrl,
                                        Thumb = showThumbUrl
                                    });
                                    dynamicCategoriesCount++;
                                }
                            }

                            baseWebData = baseWebData.Substring(showStartIndex + NovaUtil.showStart.Length);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!String.IsNullOrEmpty(pageUrl))
                    {
                        this.Settings.Categories.Add(new NextPageCategory() { Url = pageUrl });
                    }
                }
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
                this.nextPageUrl = String.Empty;
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

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

                                if (!(String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showTitle)))
                                {
                                    VideoInfo videoInfo = new VideoInfo()
                                    {
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
            Match mediaIdMatch = Regex.Match(baseWebData, NovaUtil.mediaIdRegex);

            if (mediaIdMatch.Success)
            {
                video.PlaybackOptions = new Dictionary<string, string>();

                String mediaId = mediaIdMatch.Groups["mediaId"].Value;

                String time = DateTime.Now.ToString("yyyyMMddHHmmss");
                String signature = String.Format("nova-vod|{0}|{1}|bae8ca04b7d23ab2d62968d2ea54", mediaId, time);
                String encodedHash = String.Empty;
                using (MD5 md5 = MD5.Create())
                {
                    Byte[] md5hash = md5.ComputeHash(Encoding.Default.GetBytes(signature));
                    encodedHash = Convert.ToBase64String(md5hash);
                }

                String videoPlaylistUrl = String.Format("http://master-ng.nacevi.cz/cdn.server/PlayerLink.ashx?t={1}&c=nova-vod|{0}&h=0&d=1&s={2}&tm=nova", mediaId, time, encodedHash);
                String videoPlaylistWebData = GetWebData(videoPlaylistUrl);

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
