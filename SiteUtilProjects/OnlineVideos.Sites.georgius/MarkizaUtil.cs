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
    public class MarkizaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://video.markiza.sk/archiv-tv-markiza";
        private static String dynamicCategoryStart = @"<div id=""VagonContent"">";
        private static String dynamicCategoryEnd = @"<div class=""boxVagon_foot"">";
        private static String showStart = @"<div class=""item"">";
        private static String showUrlRegex = @"<a href=""(?<showUrl>[^>]+)"">";
        private static String showThumbRegex = @"<img src=""(?<showThumbUrl>[^""]+)""";
        private static String showTitleRegex = @"<a href=""[^""]+"">(?<showTitle>[^<]+)</a>";

        // --- old Markiza archive ---

        private static String showEpisodesStart = @"<div id=""VagonContent"">";
        private static String showEpisodeStart = @"<div class=""item"">";
        private static String showEpisodeUrlRegex = @"<a href=""/archiv-tv-markiza/(?<showEpisodeUrl>[^>]+)"">";
        private static String showEpisodeThumbRegex = @"src=""(?<showEpisodeThumbUrl>[^""]+)""";
        private static String showEpisodeTitleRegex = @"<a href=""[^""]+"">(?<showEpisodeTitle>[^<]+)</a></div>";
        private static String showEpisodeDateRegex = @"<span>(?<showEpisodeDate>[^<]*)<br/>";

        private static String showEpisodeNextPageRegex = @"<div class=""right""><a href=""(?<nextPageUrl>[^""]+)"">[^<]+</a></div>";

        private static String showEpisodePlaylistUrlFormat = @"http://www.markiza.sk/js/flowplayer/config.js?&media={0}";

        private static String showEpisodePlaylistStart = @"""playlist"":[";
        private static String showEpisodePlaylistEnd = @"]";

        private static String showVideoUrlsRegex = @"""url"":""(?<showVideosUrl>[^""]+)""";

        // --- VOYO ---

        private static String showVoyoStart = @"<div class='poster'>";
        private static String showVoyoEnd = @"</div>";

        private static String showVoyoUrlTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>";
        private static String showVoyoThumbRegex = @"<img src='(?<showThumbUrl>[^']*)";

        private static String showVoyoEpisodesStart = @"<div class=""productsList"">";

        private static String showVoyoEpisodeBlockStart = @"<div class='section_item'>";
        private static String showVoyoEpisodeBlockEnd = @"<div class='clearer'>";

        private static String showVoyoEpisodeThumbUrlRegex = @"<img src='(?<showThumbUrl>[^']*)";
        private static String showVoyoEpisodeUrlAndTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>";
        private static String showVoyoEpisodeDescriptionStart = @"<div class=""padding"" >";
        private static String showVoyoEpisodeDescriptionEnd = @"</div>";

        private static String showVoyoEpisodeNextPageRegex = @"<a href='(?<nextPageUrl>[^']*)' onclick='[^']*'>&gt;</a>";

        private static String showVoyoVideoUrlFormat = @"http://voyo.markiza.sk/bin/eshop/ws/plusPlayer.php?x=playerFlash&prod={0}&unit={1}&media={2}&site={3}&section={4}&subsite={5}&embed=0&mute=0&size=&realSite={6}&width={7}&height={8}&hdEnabled=1&hash=&finish=finishedPlayer&dev=null&r={9}";
        private static String showVoyoParamsStart = @"voyoPlayer.params = {";
        private static String showVoyoParamsEnd = @"voyoPlayer.setMain";

        private static String showVoyoSiteParamRegex = @"siteId:\s(?<site>[0-9]*)";
        private static String showVoyoSectionParamRegex = @"sectionId:\s(?<section>[0-9]*)";
        private static String showVoyoSubsiteParamRegex = @"subsite:\s'(?<subsite>[^']*)";
        private static String showVoyoWidthParamRegex = @"width:\s(?<width>[0-9]*)";
        private static String showVoyoHeightParamRegex = @"height:\s(?<height>[0-9]*)";
        private static String showVoyoProdUnitMediaRegex = @"mediaData\((?<prod>[^,]*),\s(?<unit>[^,]*),\s(?<media>[^,]*)";

        private static String showVoyoUrlParamStartFormat = @"var voyoPlusConfig{0} = """;
        private static String showVoyoUrlParamEnd = @""";";

        private static String showVoyoHostRegex = @"""host"":""(?<host>[^""]*)";
        private static String showVoyoFileNameRegex = @"""filename"":""(?<fileName>[^""]*)";
	
        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public MarkizaUtil()
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
            String baseWebData = SiteUtilBase.GetWebData(MarkizaUtil.baseUrl);

            int startIndex = baseWebData.IndexOf(MarkizaUtil.dynamicCategoryStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(MarkizaUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        int index = baseWebData.IndexOf(MarkizaUtil.showStart);

                        if (index > 0)
                        {
                            baseWebData = baseWebData.Substring(index);

                            String showUrl = String.Empty;
                            String showTitle = String.Empty;
                            String showThumb = String.Empty;

                            Match match = Regex.Match(baseWebData, MarkizaUtil.showUrlRegex);
                            if (match.Success)
                            {
                                showUrl = match.Groups["showUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, MarkizaUtil.showThumbRegex);
                            if (match.Success)
                            {
                                showThumb = match.Groups["showThumbUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, MarkizaUtil.showTitleRegex);
                            if (match.Success)
                            {
                                showTitle = match.Groups["showTitle"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            if (!((String.IsNullOrEmpty(showUrl)) || (String.IsNullOrEmpty(showThumb)) || (String.IsNullOrEmpty(showTitle))))
                            {
                                this.Settings.Categories.Add(
                                new RssLink()
                                {
                                    Name = showTitle,
                                    Url = Utils.FormatAbsoluteUrl(showUrl, MarkizaUtil.baseUrl),
                                    Thumb = showThumb
                                });
                                dynamicCategoriesCount++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    this.Settings.DynamicCategoriesDiscovered = true;
                }
            }

            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                if (!pageUrl.Contains("voyo"))
                {
                    #region Old Markiza archive

                    String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                    int index = baseWebData.IndexOf(MarkizaUtil.showEpisodesStart);
                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(MarkizaUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;
                                String showEpisodeDate = String.Empty;

                                Match match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeUrlRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = match.Groups["showEpisodeThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeDateRegex);
                                if (match.Success)
                                {
                                    showEpisodeDate = match.Groups["showEpisodeDate"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDate)))
                                {
                                    break;
                                }

                                VideoInfo videoInfo = new VideoInfo()
                                {
                                    Description = showEpisodeDate,
                                    ImageUrl = showEpisodeThumb,
                                    Title = showEpisodeTitle,
                                    VideoUrl = String.Format("{0}/{1}", MarkizaUtil.baseUrl, showEpisodeUrl)
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }

                        Match nextPageMatch = Regex.Match(baseWebData, MarkizaUtil.showEpisodeNextPageRegex);
                        this.nextPageUrl = nextPageMatch.Groups["nextPageUrl"].Value;
                    }

                    #endregion
                }
                else
                {
                    #region VOYO archive

                    String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                    int index = baseWebData.IndexOf(MarkizaUtil.showVoyoEpisodesStart);
                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        Match match = Regex.Match(baseWebData, MarkizaUtil.showVoyoEpisodeNextPageRegex);
                        if (match.Success)
                        {
                            this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value,pageUrl);
                        }

                        while (true)
                        {
                            int showEpisodeBlockStart = baseWebData.IndexOf(MarkizaUtil.showVoyoEpisodeBlockStart);
                            if (showEpisodeBlockStart >= 0)
                            {
                                int showEpisodeBlockEnd = baseWebData.IndexOf(MarkizaUtil.showVoyoEpisodeBlockEnd, showEpisodeBlockStart);
                                if (showEpisodeBlockEnd >= 0)
                                {
                                    String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                    String showTitle = String.Empty;
                                    String showThumbUrl = String.Empty;
                                    String showUrl = String.Empty;
                                    String showDescription = String.Empty;

                                    match = Regex.Match(showData, MarkizaUtil.showVoyoEpisodeThumbUrlRegex);
                                    if (match.Success)
                                    {
                                        showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, pageUrl);
                                    }

                                    match = Regex.Match(showData, MarkizaUtil.showVoyoEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, pageUrl);
                                        showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                    }

                                    int descriptionStart = showData.IndexOf(MarkizaUtil.showVoyoEpisodeDescriptionStart);
                                    if (descriptionStart >= 0)
                                    {
                                        int descriptionEnd = showData.IndexOf(MarkizaUtil.showVoyoEpisodeDescriptionEnd, descriptionStart);
                                        if (descriptionEnd >= 0)
                                        {
                                            showDescription = showData.Substring(descriptionStart + MarkizaUtil.showVoyoEpisodeDescriptionStart.Length, descriptionEnd - descriptionStart - MarkizaUtil.showVoyoEpisodeDescriptionStart.Length);
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

                                baseWebData = baseWebData.Substring(showEpisodeBlockStart + MarkizaUtil.showVoyoEpisodeBlockStart.Length);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    #endregion
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
            return this.GetVideoList(category, MarkizaUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, MarkizaUtil.pageSize);
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

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> videoUrls = new List<string>();

            if (!video.VideoUrl.Contains("voyo"))
            {
                #region Old Markiza archive

                String showEpisodesId = video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("/") + 1);
                String configUrl = String.Format(MarkizaUtil.showEpisodePlaylistUrlFormat, showEpisodesId);
                String baseWebData = SiteUtilBase.GetWebData(configUrl);

                int start = baseWebData.IndexOf(MarkizaUtil.showEpisodePlaylistStart);
                if (start > 0)
                {
                    int end = baseWebData.IndexOf(MarkizaUtil.showEpisodePlaylistEnd, start);
                    if (end > 0)
                    {
                        String showEpisodePlaylist = baseWebData.Substring(start, end - start);

                        MatchCollection matches = Regex.Matches(showEpisodePlaylist, MarkizaUtil.showVideoUrlsRegex);
                        foreach (Match tempMatch in matches)
                        {
                            videoUrls.Add(tempMatch.Groups["showVideosUrl"].Value);
                        }
                    }
                }

                return videoUrls;

                #endregion
            }
            else
            {
                #region VOYO

                String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
                int startIndex = baseWebData.IndexOf(MarkizaUtil.showVoyoParamsStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(MarkizaUtil.showVoyoParamsEnd, startIndex + MarkizaUtil.showVoyoParamsStart.Length);
                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        String prod = String.Empty;
                        String unit = String.Empty;
                        String media = String.Empty;
                        String site = String.Empty;
                        String section = String.Empty;
                        String subsite = String.Empty;
                        String width = String.Empty;
                        String height = String.Empty;

                        Match match = Regex.Match(baseWebData, MarkizaUtil.showVoyoSiteParamRegex);
                        if (match.Success)
                        {
                            site = match.Groups["site"].Value;
                        }
                        match = Regex.Match(baseWebData, MarkizaUtil.showVoyoSectionParamRegex);
                        if (match.Success)
                        {
                            section = match.Groups["section"].Value;
                        }
                        match = Regex.Match(baseWebData, MarkizaUtil.showVoyoSubsiteParamRegex);
                        if (match.Success)
                        {
                            subsite = match.Groups["subsite"].Value;
                        }
                        match = Regex.Match(baseWebData, MarkizaUtil.showVoyoWidthParamRegex);
                        if (match.Success)
                        {
                            width = match.Groups["width"].Value;
                        }
                        match = Regex.Match(baseWebData, MarkizaUtil.showVoyoHeightParamRegex);
                        if (match.Success)
                        {
                            height = match.Groups["height"].Value;
                        }
                        match = Regex.Match(baseWebData, MarkizaUtil.showVoyoProdUnitMediaRegex);
                        if (match.Success)
                        {
                            prod = match.Groups["prod"].Value;
                            unit = match.Groups["unit"].Value;
                            media = match.Groups["media"].Value;
                        }

                        String showParamsUrl = String.Format(MarkizaUtil.showVoyoVideoUrlFormat, prod, unit, media, site, section, subsite, site, width, height, new Random().NextDouble());
                        String showParamsData = SiteUtilBase.GetWebData(showParamsUrl, null, null, null, true);

                        Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(showParamsData);
                        String decodedPage = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)jObject.First).Value);

                        String base64EncodedParameters = String.Empty;
                        String showVoyoUrlParamStart = String.Format(MarkizaUtil.showVoyoUrlParamStartFormat, media);
                        startIndex = decodedPage.IndexOf(showVoyoUrlParamStart);
                        if (startIndex >= 0)
                        {
                            endIndex = decodedPage.IndexOf(MarkizaUtil.showVoyoUrlParamEnd, startIndex + showVoyoUrlParamStart.Length);
                            if (endIndex >= 0)
                            {
                                base64EncodedParameters = decodedPage.Substring(startIndex + showVoyoUrlParamStart.Length, endIndex - startIndex - showVoyoUrlParamStart.Length);
                            }
                        }

                        if (!String.IsNullOrEmpty(base64EncodedParameters))
                        {
                            String decodedParams = HttpUtility.HtmlDecode(Flowplayer.Commercial.V3_1_5_17_002.Aes.Decrypt(base64EncodedParameters, Flowplayer.Commercial.V3_1_5_17_002.Aes.Key, Flowplayer.Commercial.V3_1_5_17_002.Aes.KeyType.Key128));

                            String host = String.Empty;
                            String fileName = String.Empty;

                            match = Regex.Match(decodedParams, MarkizaUtil.showVoyoHostRegex);
                            if (match.Success)
                            {
                                jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject("{ \"host\": \"" + match.Groups["host"].Value + "\" }");
                                host = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)jObject.First).Value);
                            }
                            match = Regex.Match(decodedParams, MarkizaUtil.showVoyoFileNameRegex);
                            if (match.Success)
                            {
                                jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject("{ \"filename\": \"" + match.Groups["fileName"].Value + "\" }");
                                fileName = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)jObject.First).Value);
                            }

                            if (!(String.IsNullOrEmpty(host) || String.IsNullOrEmpty(fileName)))
                            {
                                String playPath = String.Format("mp4:{0}-1.mp4", fileName);

                                String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(host) { TcUrl = host, PlayPath = playPath }.ToString();

                                videoUrls.Add(resultUrl);
                            }
                        }
                    }
                }

                return videoUrls;

                #endregion
            }
        }

        #endregion
    }
}
