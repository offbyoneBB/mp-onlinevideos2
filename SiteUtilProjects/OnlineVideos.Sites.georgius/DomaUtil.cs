using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites.georgius
{
    public class DomaUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://doma.markiza.sk/archiv-doma";
        private static String dynamicCategoryStart = @"<div id=""VagonContent"">";
        private static String dynamicCategoryEnd = @"<div class=""boxVagon_foot"">";
        private static String showStart = @"<div class=""item"">";
        private static String showUrlRegex = @"<a href=""(?<showUrl>[^>]+)"">";
        private static String showThumbRegex = @"<img src=""(?<showThumbUrl>[^""]+)""";
        private static String showTitleRegex = @"<a href=""[^""]+"">(?<showTitle>[^<]+)</a>";

        // --- old Doma archive ---

        private static String showEpisodesStart = @"<div id=""VagonContent"">";
        private static String showEpisodeStart = @"<div class=""item"">";
        private static String showEpisodeUrlRegex = @"<a href=""/archiv-doma/(?<showEpisodeUrl>[^>]+)"">";
        private static String showEpisodeThumbRegex = @"<img src=""(?<showEpisodeThumbUrl>[^""]+)""";
        private static String showEpisodeTitleRegex = @"<a href=""[^""]+"">(?<showEpisodeTitle>[^<]+)</a>";
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
	
        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public DomaUtil()
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
            String baseWebData = GetWebData(DomaUtil.baseUrl);

            int startIndex = baseWebData.IndexOf(DomaUtil.dynamicCategoryStart);
            if (startIndex > 0)
            {
                int endIndex = baseWebData.IndexOf(DomaUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        int index = baseWebData.IndexOf(DomaUtil.showStart);

                        if (index > 0)
                        {
                            baseWebData = baseWebData.Substring(index);

                            String showUrl = String.Empty;
                            String showTitle = String.Empty;
                            String showThumb = String.Empty;

                            Match match = Regex.Match(baseWebData, DomaUtil.showUrlRegex);
                            if (match.Success)
                            {
                                showUrl = match.Groups["showUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, DomaUtil.showThumbRegex);
                            if (match.Success)
                            {
                                showThumb = match.Groups["showThumbUrl"].Value;
                                baseWebData = baseWebData.Substring(match.Index + match.Length);
                            }

                            match = Regex.Match(baseWebData, DomaUtil.showTitleRegex);
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
                                    Url = Utils.FormatAbsoluteUrl(showUrl, DomaUtil.baseUrl),
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
                this.nextPageUrl = String.Empty;
                if (!pageUrl.Contains("voyo"))
                {
                    #region Old Doma archive
                    String baseWebData = GetWebData(pageUrl);

                    int index = baseWebData.IndexOf(DomaUtil.showEpisodesStart);
                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(DomaUtil.showEpisodeStart);

                            if (index > 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;
                                String showEpisodeDate = String.Empty;

                                Match match = Regex.Match(baseWebData, DomaUtil.showEpisodeUrlRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = match.Groups["showEpisodeUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, DomaUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = match.Groups["showEpisodeThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, DomaUtil.showEpisodeTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeTitle = match.Groups["showEpisodeTitle"].Value;
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, DomaUtil.showEpisodeDateRegex);
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
                                    VideoUrl = String.Format("{0}/{1}", DomaUtil.baseUrl, showEpisodeUrl)
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }

                        Match nextPageMatch = Regex.Match(baseWebData, DomaUtil.showEpisodeNextPageRegex);
                        this.nextPageUrl = nextPageMatch.Groups["nextPageUrl"].Value;
                    }

                    #endregion
                }
                else
                {
                    #region VOYO archive

                    String baseWebData = GetWebData(pageUrl, null, null, null, true);

                    int index = baseWebData.IndexOf(DomaUtil.showVoyoEpisodesStart);
                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(index);

                        Match match = Regex.Match(baseWebData, DomaUtil.showVoyoEpisodeNextPageRegex);
                        if (match.Success)
                        {
                            this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, pageUrl);
                        }

                        while (true)
                        {
                            int showEpisodeBlockStart = baseWebData.IndexOf(DomaUtil.showVoyoEpisodeBlockStart);
                            if (showEpisodeBlockStart >= 0)
                            {
                                int showEpisodeBlockEnd = baseWebData.IndexOf(DomaUtil.showVoyoEpisodeBlockEnd, showEpisodeBlockStart);
                                if (showEpisodeBlockEnd >= 0)
                                {
                                    String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                    String showTitle = String.Empty;
                                    String showThumbUrl = String.Empty;
                                    String showUrl = String.Empty;
                                    String showDescription = String.Empty;

                                    match = Regex.Match(showData, DomaUtil.showVoyoEpisodeThumbUrlRegex);
                                    if (match.Success)
                                    {
                                        showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, pageUrl);
                                    }

                                    match = Regex.Match(showData, DomaUtil.showVoyoEpisodeUrlAndTitleRegex);
                                    if (match.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, pageUrl);
                                        showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                    }

                                    int descriptionStart = showData.IndexOf(DomaUtil.showVoyoEpisodeDescriptionStart);
                                    if (descriptionStart >= 0)
                                    {
                                        int descriptionEnd = showData.IndexOf(DomaUtil.showVoyoEpisodeDescriptionEnd, descriptionStart);
                                        if (descriptionEnd >= 0)
                                        {
                                            showDescription = showData.Substring(descriptionStart + DomaUtil.showVoyoEpisodeDescriptionStart.Length, descriptionEnd - descriptionStart - DomaUtil.showVoyoEpisodeDescriptionStart.Length);
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

                                baseWebData = baseWebData.Substring(showEpisodeBlockStart + DomaUtil.showVoyoEpisodeBlockStart.Length);
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

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> videoUrls = new List<string>();

            if (!video.VideoUrl.Contains("voyo"))
            {
                #region Old Doma archive
                String showEpisodesId = video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("/") + 1);
                String configUrl = String.Format(DomaUtil.showEpisodePlaylistUrlFormat, showEpisodesId);
                String baseWebData = GetWebData(configUrl);

                int start = baseWebData.IndexOf(DomaUtil.showEpisodePlaylistStart);
                if (start > 0)
                {
                    int end = baseWebData.IndexOf(DomaUtil.showEpisodePlaylistEnd, start);
                    if (end > 0)
                    {
                        String showEpisodePlaylist = baseWebData.Substring(start, end - start);

                        MatchCollection matches = Regex.Matches(showEpisodePlaylist, DomaUtil.showVideoUrlsRegex);
                        foreach (Match tempMatch in matches)
                        {
                            videoUrls.Add(tempMatch.Groups["showVideosUrl"].Value);
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region VOYO

                String baseWebData = GetWebData(video.VideoUrl, null, null, null, true);
                int startIndex = baseWebData.IndexOf(DomaUtil.showVoyoParamsStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(DomaUtil.showVoyoParamsEnd, startIndex + DomaUtil.showVoyoParamsStart.Length);
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

                        Match match = Regex.Match(baseWebData, DomaUtil.showVoyoSiteParamRegex);
                        if (match.Success)
                        {
                            site = match.Groups["site"].Value;
                        }
                        match = Regex.Match(baseWebData, DomaUtil.showVoyoSectionParamRegex);
                        if (match.Success)
                        {
                            section = match.Groups["section"].Value;
                        }
                        match = Regex.Match(baseWebData, DomaUtil.showVoyoSubsiteParamRegex);
                        if (match.Success)
                        {
                            subsite = match.Groups["subsite"].Value;
                        }
                        match = Regex.Match(baseWebData, DomaUtil.showVoyoWidthParamRegex);
                        if (match.Success)
                        {
                            width = match.Groups["width"].Value;
                        }
                        match = Regex.Match(baseWebData, DomaUtil.showVoyoHeightParamRegex);
                        if (match.Success)
                        {
                            height = match.Groups["height"].Value;
                        }
                        match = Regex.Match(baseWebData, DomaUtil.showVoyoProdUnitMediaRegex);
                        if (match.Success)
                        {
                            prod = match.Groups["prod"].Value;
                            unit = match.Groups["unit"].Value;
                            media = match.Groups["media"].Value;
                        }

                        String showParamsUrl = String.Format(DomaUtil.showVoyoVideoUrlFormat, prod, unit, media, site, section, subsite, site, width, height, new Random().NextDouble());
                        String showParamsData = GetWebData(showParamsUrl, null, null, null, true);

                        Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(showParamsData);
                        String decodedPage = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)jObject.First).Value);

                        String base64EncodedParameters = String.Empty;
                        String showVoyoUrlParamStart = String.Format(DomaUtil.showVoyoUrlParamStartFormat, media);
                        startIndex = decodedPage.IndexOf(showVoyoUrlParamStart);
                        if (startIndex >= 0)
                        {
                            endIndex = decodedPage.IndexOf(DomaUtil.showVoyoUrlParamEnd, startIndex + showVoyoUrlParamStart.Length);
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

                            match = Regex.Match(decodedParams, DomaUtil.showVoyoHostRegex);
                            if (match.Success)
                            {
                                jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject("{ \"host\": \"" + match.Groups["host"].Value + "\" }");
                                host = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)jObject.First).Value);
                            }
                            match = Regex.Match(decodedParams, DomaUtil.showVoyoFileNameRegex);
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

            return videoUrls;
        }

        #endregion
    }
}
