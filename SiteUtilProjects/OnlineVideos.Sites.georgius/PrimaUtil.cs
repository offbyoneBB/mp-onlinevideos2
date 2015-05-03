using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;

namespace OnlineVideos.Sites.georgius
{
    public sealed class PrimaUtil : SiteUtilBase
    {
        #region Private fields

        //private static String baseUrl = "http://play.iprima.cz/az";
        private static String categoriesUrl = "http://play.iprima.cz/zanry";

        private static String episodeUrlFormat = @"/all/{0}/{1}";
        private static String episodeUrlJS = @"http://embed.livebox.cz/iprimaplay/player-embed-v2.js";

        private static String episodeHqFileNameFormat = @"""hq_id"":""(?<hqFileName>[^""]*)";
        private static String episodeLqFileNameFormat = @"""lq_id"":""(?<lqFileName>[^""]*)";

        private static String episodeAuthSectionStart = "embed['typeStream'] = 'vod';";
        private static String episodeAuthSectionEnd = "}";

        private static String episodeAuthStart = @"auth='+""""+'";
        private static String episodeAuthEnd = @"'";
        private static String episodeZone = @"""zoneGEO"":(?<zone>[^,]*)";
        private static String episodeBaseUrlStart = @"embed['stream'] = 'rtmp";
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
            String baseWebData = GetWebData(PrimaUtil.categoriesUrl, forceUTF8: true);

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection categories = document.DocumentNode.SelectNodes(".//div[@class='programs-menu-genres']/*/a");

            foreach (var category in categories)
            {
                this.Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = category.InnerText,
                        HasSubCategories = true,
                        Url = Utils.FormatAbsoluteUrl(category.Attributes["href"].Value, PrimaUtil.categoriesUrl)
                    });

                dynamicCategoriesCount++;
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int showsCount = 0;
            String url = ((RssLink)parentCategory).Url;
            if (parentCategory.ParentCategory != null)
            {
                parentCategory = parentCategory.ParentCategory;
                // last category is next category, remove it
                parentCategory.SubCategories.RemoveAt(parentCategory.SubCategories.Count - 1);
            }
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
            }
            RssLink category = (RssLink)parentCategory;

            String baseWebData = GetWebData(url, forceUTF8: true);

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection series = document.DocumentNode.SelectNodes(".//div[@class='programs-wrapper']/*/div[@class='item']");
            foreach (var serie in series)
            {
                HtmlAgilityPack.HtmlNode serieNode = serie.SelectSingleNode(".//div[@class='field-title']/./a");
                HtmlAgilityPack.HtmlNode imgNode = serie.SelectSingleNode(".//div[@class='field-image-primary']/*/*/img");

                category.SubCategories.Add(
                    new RssLink()
                    {
                        Name = serieNode.InnerText,
                        HasSubCategories = false,
                        Url = Utils.FormatAbsoluteUrl(serieNode.Attributes["href"].Value, url),
                        Thumb = imgNode.Attributes["src"].Value
                    });

                showsCount++;
            }

            HtmlAgilityPack.HtmlNode nextPageNode =  document.DocumentNode.SelectSingleNode(".//li[@class='pager-next last']/./a");
            if (nextPageNode != null)
            {
                parentCategory.SubCategories.Add(new NextPageCategory() { Url = Utils.FormatAbsoluteUrl(nextPageNode.Attributes["href"].Value, url), ParentCategory = parentCategory });
            }

            if (showsCount > 0)
            {
                parentCategory.SubCategoriesDiscovered = true;
            }

            return showsCount;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = GetWebData(pageUrl, forceUTF8: true);

                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(baseWebData);

                HtmlAgilityPack.HtmlNodeCollection episodes = document.DocumentNode.SelectNodes(".//div[@class='video-strip']/div[contains(@class, 'item')]");

                foreach (var episode in episodes)
                {
                    HtmlAgilityPack.HtmlNode imgNode = episode.SelectSingleNode(".//div[@class='field-image-primary']/*/*/img");
                    HtmlAgilityPack.HtmlNode episodeNode = episode.SelectSingleNode(".//div[@class='field-title']/./a");
                    HtmlAgilityPack.HtmlNode lengthNode = episode.SelectSingleNode(".//div[@class='field-stream-length']");

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Thumb = Utils.FormatAbsoluteUrl(imgNode.Attributes["src"].Value, pageUrl),
                        Title = episodeNode.InnerText,
                        VideoUrl = Utils.FormatAbsoluteUrl(episodeNode.Attributes["href"].Value, pageUrl),
                        Length = lengthNode.InnerText
                    };

                    pageVideos.Add(videoInfo);
                }

                HtmlAgilityPack.HtmlNode nextPageNode = document.DocumentNode.SelectSingleNode(".//li[@class='pager-next last']/./a");
                if (nextPageNode != null)
                {
                    this.nextPageUrl = Utils.FormatAbsoluteUrl(nextPageNode.Attributes["href"].Value, pageUrl);
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> GetNextPageVideos()
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

        public override string GetVideoUrl(VideoInfo video)
        {
            String baseWebData = GetWebData(video.VideoUrl, forceUTF8: true);
            String episodeJS = GetWebData(PrimaUtil.episodeUrlJS, referer: video.VideoUrl, forceUTF8: true);
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
                    baseRtmpUrl = "rtmp" + episodeJS.Substring(startIndex + PrimaUtil.episodeBaseUrlStart.Length, endIndex - startIndex - PrimaUtil.episodeBaseUrlStart.Length).Replace("iprima_token", "");
                }
            }

            startIndex = episodeJS.IndexOf(PrimaUtil.episodeAuthSectionStart);
            if (startIndex >= 0)
            {
                int endIndex = episodeJS.IndexOf(PrimaUtil.episodeAuthSectionEnd, startIndex + PrimaUtil.episodeAuthSectionStart.Length);
                if (endIndex >= 0)
                {
                    String authSection = episodeJS.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = authSection.IndexOf(PrimaUtil.episodeAuthStart);
                        if (startIndex >= 0)
                        {
                            endIndex = authSection.IndexOf(PrimaUtil.episodeAuthEnd, startIndex + PrimaUtil.episodeAuthStart.Length);
                            if (endIndex >= 0)
                            {
                                auth = authSection.Substring(startIndex + PrimaUtil.episodeAuthStart.Length, endIndex - startIndex - PrimaUtil.episodeAuthStart.Length);

                                authSection = authSection.Substring(startIndex + PrimaUtil.episodeAuthStart.Length + auth.Length);
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

            if ((!String.IsNullOrEmpty(auth)) && (!(String.IsNullOrEmpty(lqFileName) || String.IsNullOrEmpty(hqFileName) || String.IsNullOrEmpty(baseRtmpUrl))))
            {
                String app = String.Format("iprima_token{0}?auth={1}", zone == "0" ? "" : "_" + zone, auth);
                String tcUrl = String.Format("{0}{1}", baseRtmpUrl, app);

                if (!String.IsNullOrEmpty(lqFileName))
                {
                    String playPath = "mp4:" + lqFileName;
                    OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(baseRtmpUrl)
                    {
                        App = app,
                        TcUrl = tcUrl,
                        PlayPath = playPath,
                        SwfUrl = String.Format("http://embed.livebox.cz/iprimaplay/flash/LiveboxPlayer.swf?nocache={0}", (UInt64)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)),
                        PageUrl = video.VideoUrl
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
                        PlayPath = playPath,
                        SwfUrl = String.Format("http://embed.livebox.cz/iprimaplay/flash/LiveboxPlayer.swf?nocache={0}", (UInt64)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)),
                        PageUrl = video.VideoUrl
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
                            String lowQualityUrl = WebCache.Instance.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                            String highQualityUrl = WebCache.Instance.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));

                            video.PlaybackOptions = new Dictionary<string, string>();
                            video.PlaybackOptions.Add("Low quality", lowQualityUrl);
                            video.PlaybackOptions.Add("High quality", highQualityUrl);
                        }
                        else if (!String.IsNullOrEmpty(cdnLq))
                        {
                            video.VideoUrl = WebCache.Instance.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnLq));
                        }
                        else if (!String.IsNullOrEmpty(cdnHq))
                        {
                            video.VideoUrl = WebCache.Instance.GetRedirectedUrl(String.Format(PrimaUtil.videoUrlFormat, cdnHq));
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
