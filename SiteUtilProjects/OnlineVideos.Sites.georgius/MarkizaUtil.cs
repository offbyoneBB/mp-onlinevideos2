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
        private class CategoryShowsRegex
        {
            #region Private fields
            #endregion

            #region Constructors

            public CategoryShowsRegex()
                : base()
            {
                this.CategoryNames = new List<string>();
                this.ShowsBlockStart = String.Empty;
                this.ShowsBlockEnd = String.Empty;
                this.ShowStart = String.Empty;
                this.ShowEnd = String.Empty;
                this.ShowsNextPageRegex = String.Empty;
                this.ShowThumbUrlRegex = String.Empty;
                this.ShowUrlAndTitleRegex = String.Empty;
                this.ShowDescriptionStart = String.Empty;
                this.ShowDescriptionEnd = String.Empty;
            }

            #endregion

            #region Properties

            public List<String> CategoryNames { get; set; }
            public String ShowsBlockStart { get; set; }
            public String ShowsBlockEnd { get; set; }
            public String ShowStart { get; set; }
            public String ShowEnd { get; set; }
            public String ShowThumbUrlRegex { get; set; }
            public String ShowUrlAndTitleRegex { get; set; }
            public String ShowDescriptionStart { get; set; }
            public String ShowDescriptionEnd { get; set; }
            public String ShowsNextPageRegex { get; set; }

            #endregion
        }

        #region Private fields

        private static String baseUrl = @"http://voyo.markiza.sk";

        private static String categoriesStart = @"<div class=""logo"">";
        private static String categoriesEnd = @"</ul>";
        private static String categoryStart = @"<li";
        private static String categoryEnd = @"</li>";
        private static String categoryUrlAndTitleRegex = @"<a href=""(?<categoryUrl>[^""]+)"" title=""(?<categoryTitle>[^<]+)""";

        private static List<CategoryShowsRegex> categoriesAndShows = new List<CategoryShowsRegex>()
        {
            new CategoryShowsRegex()
            {
                CategoryNames = new List<string>() { "Filmy" },
                ShowsBlockStart = @"<div class=""productsList"">",
                ShowsBlockEnd = @"<div class=""body"">",
                ShowStart = @"<div class='section_item'>",
                ShowEnd = @"<div class='clearer'>",
                ShowThumbUrlRegex = @"<img src='(?<showThumbUrl>[^']*)",
                ShowUrlAndTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>",
                ShowDescriptionStart = @"<div class=""padding"" >",
                ShowDescriptionEnd = @"</div>",
                ShowsNextPageRegex = @"<a href='(?<nextPageUrl>[^']*)' onclick='[^']*'>&gt;</a>"
            },

            new CategoryShowsRegex()
            {
                CategoryNames = new List<string>(),
                ShowsBlockStart = @"<div class=""box tv-broadcast-list"">",
                ShowsBlockEnd = @"<div class=""clear"">",
                ShowStart = @"<div class=""item"">",
                ShowEnd = @"</h2>",
                ShowThumbUrlRegex = @"<img src=""(?<showThumbUrl>[^""]*)",
                ShowUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""(?<showTitle>[^""]*)",
                ShowDescriptionStart = @"<div class=""padding"" >",
                ShowDescriptionEnd = @"</div>",
                ShowsNextPageRegex = @"<a href='(?<nextPageUrl>[^']*)' onclick='[^']*'>&gt;</a>"
            }
        };

        private static String showEpisodesStart = @"<div class=""productsList"">";

        private static String showEpisodeBlockStart = @"<div class='section_item'>";
        private static String showEpisodeBlockEnd = @"<div class='clearer'>";

        private static String showEpisodeThumbUrlRegex = @"<img src='(?<showThumbUrl>[^']*)";
        private static String showEpisodeUrlAndTitleRegex = @"<a href='(?<showUrl>[^']*)' title='(?<showTitle>[^']*)'>";
        private static String showEpisodeDescriptionStart = @"<div class=""padding"" >";
        private static String showEpisodeDescriptionEnd = @"</div>";

        private static String showEpisodeNextPageRegex = @"<a href='(?<nextPageUrl>[^']*)' onclick='[^']*'>&gt;</a>";

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
        private static String showVoyoConnectionArgsRegex = @"""connectionArgs"":\[(?<connectionArgs>[^\]]*)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Login"), Description("User name on voyo.markiza.sk.")]
        String login = String.Empty;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Password on voyo.markiza.sk."), PasswordPropertyText(true)]
        String password = String.Empty;

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
            int categoriesCount = 0;
            String baseWebData = GetWebData(MarkizaUtil.baseUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(MarkizaUtil.categoriesStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(MarkizaUtil.categoriesEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(MarkizaUtil.categoryStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(MarkizaUtil.categoryEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String categoryData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String categoryUrl = String.Empty;
                                String categoryTitle = String.Empty;

                                Match match = Regex.Match(categoryData, MarkizaUtil.categoryUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    categoryUrl = Utils.FormatAbsoluteUrl(match.Groups["categoryUrl"].Value, MarkizaUtil.baseUrl);
                                    categoryTitle = match.Groups["categoryTitle"].Value;
                                }

                                if (!((String.IsNullOrEmpty(categoryUrl)) || (String.IsNullOrEmpty(categoryTitle))))
                                {
                                    this.Settings.Categories.Add(
                                    new RssLink()
                                    {
                                        Name = categoryTitle,
                                        Url = categoryUrl,
                                        HasSubCategories = true
                                    });
                                    categoriesCount++;
                                }

                                baseWebData = baseWebData.Substring(endIndex + MarkizaUtil.categoryEnd.Length);
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

                    this.Settings.DynamicCategoriesDiscovered = true;
                }
            }

            return categoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int showsCount = 0;
            String url = (parentCategory as RssLink).Url;
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

            String baseWebData = GetWebData(url, null, null, null, true);

            // find shows
            CategoryShowsRegex categoryShow = null;
            foreach (var categoryAndShow in MarkizaUtil.categoriesAndShows)
            {
                if ((categoryAndShow.CategoryNames.Contains(parentCategory.Name)) || (categoryAndShow.CategoryNames.Count == 0))
                {
                    categoryShow = categoryAndShow;
                    break;
                }
            }

            int startIndex = baseWebData.IndexOf(categoryShow.ShowsBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(categoryShow.ShowsBlockEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);
                    String baseUrl = url;

                    Match match = Regex.Match(baseWebData, categoryShow.ShowsNextPageRegex);
                    if (match.Success)
                    {
                        url = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, url);
                    }
                    else
                    {
                        url = String.Empty;
                    }

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(categoryShow.ShowStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(categoryShow.ShowEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showTitle = String.Empty;
                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showDescription = String.Empty;

                                match = Regex.Match(showData, categoryShow.ShowThumbUrlRegex);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, baseUrl);
                                }

                                match = Regex.Match(showData, categoryShow.ShowUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, baseUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                int descriptionStart = showData.IndexOf(categoryShow.ShowDescriptionStart);
                                if (descriptionStart >= 0)
                                {
                                    int descriptionEnd = showData.IndexOf(categoryShow.ShowDescriptionEnd, descriptionStart);
                                    if (descriptionEnd >= 0)
                                    {
                                        showDescription = OnlineVideos.Utils.PlainTextFromHtml(HttpUtility.HtmlDecode(showData.Substring(descriptionStart + categoryShow.ShowDescriptionStart.Length, descriptionEnd - descriptionStart - categoryShow.ShowDescriptionStart.Length)));
                                    }
                                }

                                if (!((String.IsNullOrEmpty(showUrl)) || (String.IsNullOrEmpty(showTitle))))
                                {
                                    parentCategory.SubCategories.Add(
                                    new RssLink()
                                    {
                                        Name = showTitle,
                                        Url = showUrl,
                                        Description = showDescription,
                                        Thumb = showThumbUrl
                                    });
                                    showsCount++;
                                }

                                baseWebData = baseWebData.Substring(endIndex + categoryShow.ShowEnd.Length);
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

                    parentCategory.SubCategoriesDiscovered = true;
                }
                else
                {
                    url = String.Empty;
                }
            }
            else
            {
                url = String.Empty;
            }

            if (!String.IsNullOrEmpty(url))
            {
                parentCategory.SubCategories.Add(new NextPageCategory() { Url = url, ParentCategory = parentCategory });
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
                String baseWebData = GetWebData(pageUrl, null, null, null, true);

                int index = baseWebData.IndexOf(MarkizaUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    Match match = Regex.Match(baseWebData, MarkizaUtil.showEpisodeNextPageRegex);
                    if (match.Success)
                    {
                        this.nextPageUrl = Utils.FormatAbsoluteUrl(match.Groups["nextPageUrl"].Value, pageUrl);
                    }

                    while (true)
                    {
                        int showEpisodeBlockStart = baseWebData.IndexOf(MarkizaUtil.showEpisodeBlockStart);
                        if (showEpisodeBlockStart >= 0)
                        {
                            int showEpisodeBlockEnd = baseWebData.IndexOf(MarkizaUtil.showEpisodeBlockEnd, showEpisodeBlockStart);
                            if (showEpisodeBlockEnd >= 0)
                            {
                                String showData = baseWebData.Substring(showEpisodeBlockStart, showEpisodeBlockEnd - showEpisodeBlockStart);

                                String showTitle = String.Empty;
                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showDescription = String.Empty;

                                match = Regex.Match(showData, MarkizaUtil.showEpisodeThumbUrlRegex);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, pageUrl);
                                }

                                match = Regex.Match(showData, MarkizaUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, pageUrl);
                                    showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                                }

                                int descriptionStart = showData.IndexOf(MarkizaUtil.showEpisodeDescriptionStart);
                                if (descriptionStart >= 0)
                                {
                                    int descriptionEnd = showData.IndexOf(MarkizaUtil.showEpisodeDescriptionEnd, descriptionStart);
                                    if (descriptionEnd >= 0)
                                    {
                                        showDescription = HttpUtility.HtmlDecode(showData.Substring(descriptionStart + MarkizaUtil.showEpisodeDescriptionStart.Length, descriptionEnd - descriptionStart - MarkizaUtil.showEpisodeDescriptionStart.Length));
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

                            baseWebData = baseWebData.Substring(showEpisodeBlockStart + MarkizaUtil.showEpisodeBlockStart.Length);
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

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> videoUrls = new List<string>();
            System.Net.CookieContainer cookies = new System.Net.CookieContainer();

            String baseWebData = GetWebData(video.VideoUrl, cookies, null, null, true);
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

                    String cookiesRequestUrl = String.Format("http://voyo.markiza.sk/bin/usrtrck-new.php?section_id={0}&article_id=&gallery_id=&media_id=&article_date=&r={1}&c=", section, new Random().NextDouble());
                    GetWebData(cookiesRequestUrl, cookies, null, null, true);

                    cookiesRequestUrl = String.Format("http://voyo.markiza.sk/bin/eshop/ws/user.php?x=isLoggedIn&r={0}", new Random().NextDouble());
                    GetWebData(cookiesRequestUrl, cookies, null, null, true);

                    cookiesRequestUrl = String.Format("http://voyo.markiza.sk/bin/eshop/ws/user.php?x=login&r={0}", new Random().NextDouble());
                    GetWebDataFromPost(cookiesRequestUrl, String.Format("u={0}&p={1}", this.login, this.password), cookies, null, null, true);
                
                    String showParamsUrl = String.Format(MarkizaUtil.showVoyoVideoUrlFormat, prod, unit, media, site, section, subsite, site, width, height, new Random().NextDouble());
                    String showParamsData = GetWebData(showParamsUrl, cookies, null, null, true);

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
                        String connectionArgs = String.Empty;

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

                            OnlineVideos.MPUrlSourceFilter.RtmpUrl rtmpUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(host) { TcUrl = host, PlayPath = playPath, App = "voyosk" };

                            match = Regex.Match(decodedParams, MarkizaUtil.showVoyoConnectionArgsRegex);
                            if (match.Success)
                            {
                                jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject("{ \"connectionArgs\":[" + match.Groups["connectionArgs"].Value + "] }");

                                Newtonsoft.Json.Linq.JArray jConnectionArray = jObject["connectionArgs"] as Newtonsoft.Json.Linq.JArray;
                                if ((jConnectionArray != null) && (jConnectionArray.Count > 0))
                                {
                                    MPUrlSourceFilter.RtmpObjectArbitraryData rtmpConnectionParams = new MPUrlSourceFilter.RtmpObjectArbitraryData();

                                    for (int i = 0; i < jConnectionArray.Count; i++)
                                    {
                                        var connectionItem = jConnectionArray[i];

                                        switch (connectionItem.Type)
                                        {
                                            case Newtonsoft.Json.Linq.JTokenType.None:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Object:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Array:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Constructor:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Property:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Comment:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Integer:
                                                rtmpConnectionParams.Objects.Add(new MPUrlSourceFilter.RtmpNumberArbitraryData(i.ToString(), (long)((Newtonsoft.Json.Linq.JValue)connectionItem).Value));
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Float:
                                                rtmpConnectionParams.Objects.Add(new MPUrlSourceFilter.RtmpNumberArbitraryData(i.ToString(), (float)((Newtonsoft.Json.Linq.JValue)connectionItem).Value));
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.String:
                                                rtmpConnectionParams.Objects.Add(new MPUrlSourceFilter.RtmpStringArbitraryData(i.ToString(), (String)((Newtonsoft.Json.Linq.JValue)connectionItem).Value));
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Boolean:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Null:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Undefined:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Date:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Raw:
                                                break;
                                            case Newtonsoft.Json.Linq.JTokenType.Bytes:
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    
                                    rtmpUrl.ArbitraryData.Add(rtmpConnectionParams);
                                }
                            }

                            videoUrls.Add(rtmpUrl.ToString());
                        }
                    }
                }
            }

            return videoUrls;
        }

        #endregion
    }
}
