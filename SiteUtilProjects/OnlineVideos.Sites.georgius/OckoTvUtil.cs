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

        private static String baseUrl = @"http://ocko.idnes.cz/stream.aspx";

        private static String dynamicCategoryStart = @"Misc.videoFLV({";
        private static String dynamicCategoryEnd = @"})";
        private static String showUrlRegex = @"data: ""(?<showUrl>[^""]+)""";

        //private static String showEpisodesStart = @"<div class=""post"">";
        //private static String showEpisodesEnd = @"<script type=""text/javascript"">";
        //private static String showEpisodeStart = @"<h2 class=""postTitle"">";
        //private static String showEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]+)"" title=""[^""]*"">(?<showEpisodeTitle>[^<]+)</a>";
        //private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)";
        //private static String showEpisodeDescriptionStart = @"<div id=""obs"">";
        //private static String showEpisodeDescriptionEnd = @"</div>";

        //private static String showEpisodeNextPageRegex = @"<a href=""(?<nextPageUrl>[^""]+)"" class=""nextpostslink"">&raquo;</a>";

        //private static String optionTitleRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| \([\s]*[0-9]+\) \| \.(?<format>[a-z0-9]+)";
        //private static String videoSectionStart = @"<div class=""postContent"">";
        //private static String videoSectionEnd = @"</div>";
        //private static String videoUrlRegex = @";file=(?<videoUrl>[^&]+)";
        //private static String videoCaptionsRegex = @";captions.file=(?<videoUrl>[^&]+)";

        //private static String showVideoUrlsRegex = @"<param name=""movie"" value=""(?<showVideoUrl>[^""]+)"">";
        //private static String optionTitleRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| \([\s]*[0-9]+\) \| \.(?<format>[a-z0-9]+)";

        //private static String searchQueryUrl = @"http://www.pohadkar.cz/?s={0}";

        //private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

        //private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        //private static String flashVarsEnd = @"/>";
        //private static String idRegex = @"id=(?<id>[^&]+)";
        //private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&]+)";
        //private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&]+)";

        // the number of show episodes per page
        private static int pageSize = 28;

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
            String baseWebData = SiteUtilBase.GetWebData(OckoTvUtil.baseUrl);

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
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);
                XmlDocument document = new XmlDocument();
                document.LoadXml(baseWebData);

                XmlNode replace = document.SelectSingleNode("//root/properties/replace[@old and @new]");
                XmlNode item = document.SelectSingleNode("//root/items/item/linkvideo/server");
                XmlNode title = document.SelectSingleNode("//root/items/item/title");
                XmlNode image = document.SelectSingleNode("//root/items/item/imageprev");

                String rtmpUrl = String.Empty;
                if (item != null)
                {
                    rtmpUrl = item.InnerText;
                }
                if (replace != null)
                {
                    rtmpUrl = rtmpUrl.Replace(replace.Attributes["old"].Value, replace.Attributes["new"].Value);
                }
                if ((!String.IsNullOrEmpty(rtmpUrl)) && (!rtmpUrl.Contains("rtmp://")))
                {
                    rtmpUrl = "rtmp://" + rtmpUrl;
                }

                String tcUrl = rtmpUrl.Replace("rtmp://", "rtmpt://");
                String app = "live";
                String playPath = "ocko";
                String swfUrl = "http://g.idnes.cz/swf/flv/player.swf?v=20110601";

                String resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&tcUrl={1}&app={2}&playpath={3}&swfurl={4}&pageurl={5}&live=true",
                                    rtmpUrl, //rtmpUrl
                                    tcUrl, //tcUrl
                                    app, //app
                                    playPath, //playpath
                                    swfUrl,
                                    OckoTvUtil.baseUrl
                                    ));

                VideoInfo videoInfo = new VideoInfo()
                {
                    Title = (title == null) ? "Live" : title.InnerText,
                    ImageUrl = (image == null) ? String.Empty : image.InnerText,
                    VideoUrl = resultUrl
                };
                pageVideos.Add(videoInfo);

                this.nextPageUrl = String.Empty;
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
            return this.GetVideoList(category, OckoTvUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, OckoTvUtil.pageSize);
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
