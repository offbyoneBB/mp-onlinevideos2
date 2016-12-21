using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.georgius
{
    public class OckoTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String archivUrl = @"http://ocko.tv/archiv";
        private static String liveUrl = @"http://ocko-live.service.cdn.cra.cz/playlist/live/ocko";
        private static String liveGoldUrl = @"http://ocko-live.service.cdn.cra.cz/playlist/live/ocko_gold";
        private static String liveExpresUrl = @"http://ocko-live.service.cdn.cra.cz/playlist/live/ocko_expres";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public OckoTvUtil()
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

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "Živě",
                    HasSubCategories = false,
                    Url = OckoTvUtil.liveUrl
                });

            dynamicCategoriesCount++;

            String baseWebData = GetWebData(OckoTvUtil.archivUrl, forceUTF8: true);
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection shows = document.DocumentNode.SelectNodes(".//ul[contains(@class, 'archive_list')]/li");

            foreach (var show in shows)
            {
                HtmlAgilityPack.HtmlNode titleNode = show.SelectSingleNode(".//span[@class='title']");
                HtmlAgilityPack.HtmlNode linkNode = show.SelectSingleNode(".//a[@class='nettv-show-link']");
                HtmlAgilityPack.HtmlNode thumbNode = show.SelectSingleNode(".//img");
                HtmlAgilityPack.HtmlNode descriptionNode = show.SelectSingleNode(".//div[@class='nettv-archive-team-desc']");

                this.Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = titleNode.InnerText,
                        HasSubCategories = false,
                        Url = Utils.FormatAbsoluteUrl(linkNode.Attributes["href"].Value, OckoTvUtil.archivUrl),
                        Thumb = thumbNode.Attributes["src"].Value
                    });

                dynamicCategoriesCount++;
                
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

                if (this.currentCategory.Name == "Živě")
                {
                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Title = "ÓČKO",
                        VideoUrl = OckoTvUtil.liveUrl
                    };
                    pageVideos.Add(videoInfo);

                    videoInfo = new VideoInfo()
                    {
                        Title = "ÓČKO GOLD",
                        VideoUrl = OckoTvUtil.liveGoldUrl
                    };
                    pageVideos.Add(videoInfo);

                    videoInfo = new VideoInfo()
                    {
                        Title = "ÓČKO EXPRES",
                        VideoUrl = OckoTvUtil.liveExpresUrl
                    };
                    pageVideos.Add(videoInfo);
                }
                else
                {
                    String baseWebData = GetWebData(pageUrl, forceUTF8: true);
                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                    document.LoadHtml(baseWebData);

                    HtmlAgilityPack.HtmlNodeCollection episodes = document.DocumentNode.SelectNodes(".//ul[contains(@class, 'archive_list')]/li");

                    foreach (var episode in episodes)
                    {
                        HtmlAgilityPack.HtmlNode linkNode = episode.SelectSingleNode(".//h2/a");
                        HtmlAgilityPack.HtmlNode thumbNode = episode.SelectSingleNode(".//img");
                        HtmlAgilityPack.HtmlNode descriptionNode = episode.SelectSingleNode(".//p[@class='nettv-team-desc']");

                        VideoInfo videoInfo = new VideoInfo()
                        {
                            Title = linkNode.SelectSingleNode(".//text()").InnerText,
                            Thumb = thumbNode.Attributes["src"].Value,
                            VideoUrl = Utils.FormatAbsoluteUrl(linkNode.Attributes["href"].Value, pageUrl),
                            Description = descriptionNode.InnerText
                        };
                        pageVideos.Add(videoInfo);
                    }

                    HtmlAgilityPack.HtmlNode nextPageNode = document.DocumentNode.SelectSingleNode(".//a[@class='weebo_pager_next']");
                    if (nextPageNode != null)
                    {
                        this.nextPageUrl = nextPageNode.Attributes["href"].Value;
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

            if ((parentCategory.Other != null) && (this.currentCategory.Other != null))
            {
                String parentCategoryOther = (String)parentCategory.Other;
                String currentCategoryOther = (String)currentCategory.Other;

                if (parentCategoryOther != currentCategoryOther)
                {
                    this.currentStartIndex = 0;
                    this.nextPageUrl = parentCategory.Url;
                    this.loadedEpisodes.Clear();
                }
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
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }
            video.PlaybackOptions.Clear();

            if (this.currentCategory.Name == "Živě")
            {
                video.PlaybackOptions.Add("Low quality", Utils.FormatAbsoluteUrl("live_lq.m3u8", video.VideoUrl));
                video.PlaybackOptions.Add("High quality", Utils.FormatAbsoluteUrl("live_hq.m3u8", video.VideoUrl));
            }
            else
            {
                String baseWebData = GetWebData(video.VideoUrl, forceUTF8: true);
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(baseWebData);

                return document.DocumentNode.SelectSingleNode(".//div[@class='nettv-archive-video']/video").Attributes["src"].Value;
            }

            if (video.PlaybackOptions.Count > 0)
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
