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

        private static String baseUrl = @"http://ocko.tv/ocko-tv-zive/";

        private static String dynamicCategoryStart = @"mediaPlayer.options";
        private static String dynamicCategoryEnd = @"mediaPlayer.init";
        private static String showUrlRegex = @"file : '(?<showUrl>[^']+)";

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
            String baseWebData = SiteUtilBase.GetWebData(OckoTvUtil.baseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(OckoTvUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);

                index = baseWebData.IndexOf(OckoTvUtil.dynamicCategoryEnd);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(0, index);

                    String showUrl = String.Empty;
                    String showTitle = String.Empty;

                    Match match = Regex.Match(baseWebData, OckoTvUtil.showUrlRegex);
                    if (match.Success)
                    {
                        showUrl = match.Groups["showUrl"].Value;
                        showTitle = "Óčko Live";
                        baseWebData = baseWebData.Substring(match.Index + match.Length);
                    }

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = showTitle,
                            Url = Utils.FormatAbsoluteUrl(showUrl, OckoTvUtil.baseUrl),
                            HasSubCategories = false
                        });
                    dynamicCategoriesCount++;
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
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);
                XmlDocument document = new XmlDocument();
                document.LoadXml(baseWebData);

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
                namespaceManager.AddNamespace("media", "http://search.yahoo.com/mrss/");
                namespaceManager.AddNamespace("jwplayer", "http://developer.longtailvideo.com/trac/wiki/FlashFormats");

                XmlNodeList media = document.SelectNodes("//media:content", namespaceManager);
                XmlNode url = document.SelectSingleNode("//jwplayer:streamer", namespaceManager);

                if ((media != null) && (url != null) && (media.Count != 0))
                {
                    String rtmpUrl = url.InnerText;

                    String tcUrl = rtmpUrl;
                    String app = rtmpUrl.Substring(rtmpUrl.IndexOf("/", rtmpUrl.IndexOf("//") + 2) + 1);
                    String playPath = "ockoHQ3";

                    String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(rtmpUrl) { LiveStream = true, TcUrl = tcUrl, App = app, PlayPath = playPath, PageUrl = OckoTvUtil.baseUrl, Live = true }.ToString();

                    VideoInfo videoInfo = new VideoInfo()
                    {
                        Title = "Live",
                        ImageUrl = "http://ocko.tv/public/templates/default/img/drop-logo.png",
                        VideoUrl = resultUrl
                    };
                    pageVideos.Add(videoInfo);
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
            return video.VideoUrl;
        }

        #endregion
    }
}
