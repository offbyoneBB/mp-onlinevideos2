using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public sealed class ApetitTvUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.apetitonline.cz/apetit-tv";

        private static String videoUrlStart = "\"file\":\"";
        private static String videoUrlEnd = "\"";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public ApetitTvUtil()
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

            String baseWebData = GetWebData(ApetitTvUtil.baseUrl, forceUTF8: true);
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection categories = document.DocumentNode.SelectNodes(".//div[@class='clear']/div/div[@class='view-footer']/div");

            foreach (var category in categories)
            {
                HtmlAgilityPack.HtmlNode cat1 = category.SelectSingleNode(".//div[@class='view-header']/h3/text()");
                HtmlAgilityPack.HtmlNode cat2 = category.SelectSingleNode(".//div[@class='view-header']/a");

                if (cat1 != null)
                {
                    this.Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = cat1.InnerText,
                        Other = category
                    });

                    dynamicCategoriesCount++;
                }
                else if (cat2 != null)
                {
                    this.Settings.Categories.Add(
                    new RssLink()
                    {
                        Name = cat2.SelectSingleNode(".//text()").InnerText,
                        Url = Utils.FormatAbsoluteUrl(cat2.Attributes["href"].Value, ApetitTvUtil.baseUrl)
                    });

                    dynamicCategoriesCount++;
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(RssLink category, String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (String.IsNullOrEmpty(pageUrl) && (category.Other != null))
            {
                HtmlAgilityPack.HtmlNode root = (HtmlAgilityPack.HtmlNode)category.Other;

                HtmlAgilityPack.HtmlNodeCollection shows = root.SelectNodes(".//div[contains(@class, 'article-default')]");

                foreach (var show in shows)
                {
                    HtmlAgilityPack.HtmlNode linkNode = show.SelectSingleNode(".//h3/a");
                    HtmlAgilityPack.HtmlNode thumbNode = show.SelectSingleNode(".//img");

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Thumb = Utils.FormatAbsoluteUrl(thumbNode.Attributes["src"].Value, ApetitTvUtil.baseUrl),
                        Title = linkNode.InnerText,
                        VideoUrl = Utils.FormatAbsoluteUrl(linkNode.Attributes["href"].Value, ApetitTvUtil.baseUrl)
                    };

                    pageVideos.Add(videoInfo);
                }
            }
            else if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = GetWebData(pageUrl, forceUTF8: true);
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(baseWebData);

                HtmlAgilityPack.HtmlNodeCollection shows = document.DocumentNode.SelectNodes(".//div[contains(@class, 'article-default')]");

                foreach (var show in shows)
                {
                    HtmlAgilityPack.HtmlNode linkNode = show.SelectSingleNode(".//h3/a");
                    HtmlAgilityPack.HtmlNode thumbNode = show.SelectSingleNode(".//img");

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Thumb = Utils.FormatAbsoluteUrl(thumbNode.Attributes["src"].Value, ApetitTvUtil.baseUrl),
                        Title = linkNode.InnerText,
                        VideoUrl = Utils.FormatAbsoluteUrl(linkNode.Attributes["href"].Value, ApetitTvUtil.baseUrl)
                    };

                    pageVideos.Add(videoInfo);
                }

                HtmlAgilityPack.HtmlNode nextPageLink = document.DocumentNode.SelectSingleNode(".//li[@class='pager-next']/a");
                this.nextPageUrl = (nextPageLink == null) ? this.nextPageUrl : Utils.FormatAbsoluteUrl(System.Web.HttpUtility.HtmlDecode(nextPageLink.Attributes["href"].Value), pageUrl);
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

            if ((parentCategory.Other != null) && (this.currentCategory.Other != null) && (parentCategory.Other is String) && (currentCategory.Other is String))
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

            this.loadedEpisodes.AddRange(this.GetPageVideos(parentCategory, this.nextPageUrl));
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

            int startIndex = baseWebData.IndexOf(ApetitTvUtil.videoUrlStart);
            if (startIndex != (-1))
            {
                startIndex += ApetitTvUtil.videoUrlStart.Length;
                int endIndex = baseWebData.IndexOf(ApetitTvUtil.videoUrlEnd, startIndex);

                if (endIndex != (-1))
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    return new MPUrlSourceFilter.HttpUrl(baseWebData) { UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0" }.ToString();
                }
            }

            return String.Empty;
        }

        #endregion
    }
}
