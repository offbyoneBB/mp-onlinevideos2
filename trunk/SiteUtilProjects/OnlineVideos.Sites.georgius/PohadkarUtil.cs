using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using System.Net;

namespace OnlineVideos.Sites.georgius
{
    public class PohadkarUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = @"http://www.pohadkar.cz";

        private static String categoriesStart = @"<div class=""vypis_body extra2"">";
        private static String categoriesEnd = @"<div class=""vypis_data""";
        private static String categoryStart = @"<a class";
        private static String categoryEnd = @"</a>";
        private static String categoryUrlAndTitleRegex = @"<a class=""[^""]*""[\s]*onclick=""loadVypis\('(?<categoryUrl>[^']+)'[^""]*"">(?<categoryTitle>[^<]+)";

        private static String showListUrlFormat = @"http://www.pohadkar.cz/system/load-vypis/?znak={0}&typ=1&zar=hp";

        private static String showStart = @"<span>";
        private static String showEnd = @"</span>";

        private static String showUrlAndTitleRegex = @"<a href=""(?<showUrl>[^']*)"">(?<showTitle>[^<]*)";

        private static String showEpisodesStart = @"<div class=""tale_char_div"">";
        private static String showEpisodesEnd = @"<script type=""text/javascript"">";

        private static String showEpisodeStart = @"<div class=""tale_char_div"">";
        private static String showEpisodeThumbRegex = @"<img class=""tale_char_pic"" src=""(?<showEpisodeThumbUrl>[^""]+)""";
        private static String showEpisodeUrlAndTitleRegex = @"<a title=""[^""]*"" class=""tale_char_name"" style=""[^""]*"" href=""(?<showEpisodeUrl>[^""]+)"">(?<showEpisodeTitle>[^<]+)";
        private static String showEpisodeDescriptionRegex = @"<p title=""(?<showEpisodeDescription>[^""]*)";

        private static String showEpisodeNextPageRegex = @"<a class=""right_active"" href=""(?<nextPageUrl>[^""]+)"" >&gt;</a>";

        private static String showVideoUrlsRegex = @"(<param name=""movie"" value=""(?<showVideoUrl>[^""]+)"">)|(<iframe title=""YouTube video player"" width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")|(<iframe title=""YouTube video player"" class=""youtube-player"" type=""text/html"" width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")|(<iframe width=""[^""]*"" height=""[^""]*"" src=""(?<showVideoUrl>[^""]+)"")";

        private static String optionTitleRegex = @"(?<width>[0-9]+)x(?<height>[0-9]+) \| (?<format>[a-z0-9]+) \([\s]*[0-9]+\)";

        private static String searchQueryUrl = @"http://www.pohadkar.cz/videa/";
        private static String searchRequest = @"animated=on&creatures=&country=&acted=on&sstring={0}&radeni=abc&akce=yes";

        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @"/>";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&]+)";
        private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();
        private CookieContainer cookieContainer = new CookieContainer();

        #endregion

        #region Constructors

        public PohadkarUtil()
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
            String baseWebData = GetWebData(PohadkarUtil.baseUrl, forceUTF8: true);

            int startIndex = baseWebData.IndexOf(PohadkarUtil.categoriesStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(PohadkarUtil.categoriesEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(PohadkarUtil.categoryStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(PohadkarUtil.categoryEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String categoryData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String categoryUrl = String.Empty;
                                String categoryTitle = String.Empty;

                                Match match = Regex.Match(categoryData, PohadkarUtil.categoryUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    categoryUrl = match.Groups["categoryUrl"].Value;
                                    categoryTitle = match.Groups["categoryTitle"].Value;
                                }

                                if (!((String.IsNullOrEmpty(categoryUrl)) || (String.IsNullOrEmpty(categoryTitle))))
                                {
                                    this.Settings.Categories.Add(
                                    new RssLink()
                                    {
                                        Name = categoryTitle,
                                        Url = String.Format(PohadkarUtil.showListUrlFormat, HttpUtility.UrlEncode(categoryUrl)),
                                        HasSubCategories = true
                                    });
                                    categoriesCount++;
                                }

                                baseWebData = baseWebData.Substring(endIndex + PohadkarUtil.categoryEnd.Length);
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

            String baseWebData = GetWebData(url, forceUTF8: true);

            while (true)
            {
                int startIndex = baseWebData.IndexOf(PohadkarUtil.showStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(PohadkarUtil.showEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        String showTitle = String.Empty;
                        String showUrl = String.Empty;

                        Match match = Regex.Match(showData, PohadkarUtil.showUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, baseUrl);
                            showTitle = HttpUtility.HtmlDecode(match.Groups["showTitle"].Value);
                        }

                        if (!((String.IsNullOrEmpty(showUrl)) || (String.IsNullOrEmpty(showTitle))))
                        {
                            parentCategory.SubCategories.Add(
                            new RssLink()
                            {
                                Name = showTitle,
                                Url = showUrl.EndsWith("/") ? (showUrl + "video") : (showUrl + "/video")
                            });
                            showsCount++;
                        }

                        baseWebData = baseWebData.Substring(endIndex + PohadkarUtil.showEnd.Length);
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

            return showsCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = String.Empty;
                
                if ((this.currentCategory.Name == "Search") && (this.currentCategory.Url == pageUrl))
                {
                    this.cookieContainer = new CookieContainer();
                    baseWebData = GetWebData(pageUrl, String.Format(PohadkarUtil.searchRequest, HttpUtility.UrlEncode((String)this.currentCategory.Other)), this.cookieContainer, null, null, true);
                }
                else
                {
                    baseWebData = GetWebData(pageUrl, cookies: (this.currentCategory.Name == "Search") ? this.cookieContainer : null, forceUTF8: true);
                }

                int index = baseWebData.IndexOf(PohadkarUtil.showEpisodesStart);
                if (index > 0)
                {
                    baseWebData = baseWebData.Substring(index);

                    index = baseWebData.IndexOf(PohadkarUtil.showEpisodesEnd);

                    if (index > 0)
                    {
                        baseWebData = baseWebData.Substring(0, index);

                        while (true)
                        {
                            index = baseWebData.IndexOf(PohadkarUtil.showEpisodeStart);

                            if (index >= 0)
                            {
                                baseWebData = baseWebData.Substring(index);

                                String showEpisodeUrl = String.Empty;
                                String showEpisodeTitle = String.Empty;
                                String showEpisodeThumb = String.Empty;
                                String showEpisodeDescription = String.Empty;

                                Match match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeThumbRegex);
                                if (match.Success)
                                {
                                    showEpisodeThumb = Utils.FormatAbsoluteUrl(match.Groups["showEpisodeThumbUrl"].Value, PohadkarUtil.baseUrl);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeUrlAndTitleRegex);
                                if (match.Success)
                                {
                                    showEpisodeUrl = Utils.FormatAbsoluteUrl(HttpUtility.UrlDecode(match.Groups["showEpisodeUrl"].Value), PohadkarUtil.baseUrl);
                                    showEpisodeTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                match = Regex.Match(baseWebData, PohadkarUtil.showEpisodeDescriptionRegex);
                                if (match.Success)
                                {
                                    showEpisodeDescription = HttpUtility.HtmlDecode(match.Groups["showEpisodeDescription"].Value);
                                    baseWebData = baseWebData.Substring(match.Index + match.Length);
                                }

                                if ((String.IsNullOrEmpty(showEpisodeUrl)) && (String.IsNullOrEmpty(showEpisodeThumb)) && (String.IsNullOrEmpty(showEpisodeTitle)) && (String.IsNullOrEmpty(showEpisodeDescription)))
                                {
                                    break;
                                }

                                VideoInfo videoInfo = new VideoInfo()
                                {
                                    Description = showEpisodeDescription,
                                    ImageUrl = showEpisodeThumb,
                                    Title = showEpisodeTitle,
                                    VideoUrl = showEpisodeUrl
                                };

                                pageVideos.Add(videoInfo);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, PohadkarUtil.showEpisodeNextPageRegex);
                    this.nextPageUrl = nextPageMatch.Success ? Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, pageUrl) : String.Empty;
                    
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

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> urls = new List<string>();
            String baseWebData = GetWebData(video.VideoUrl, forceUTF8: true);
            MatchCollection matches = Regex.Matches(baseWebData, PohadkarUtil.showVideoUrlsRegex);
            foreach (Match match in matches)
            {
                String showVideoUrl = match.Groups["showVideoUrl"].Value;
                if (showVideoUrl.Contains("stream.cz"))
                {
                    baseWebData = GetWebData(showVideoUrl.Replace("object", "video"));
                    baseWebData = HttpUtility.HtmlDecode(baseWebData);

                    Match flashVarsStart = Regex.Match(baseWebData, PohadkarUtil.flashVarsStartRegex);
                    if (flashVarsStart.Success)
                    {
                        int end = baseWebData.IndexOf(PohadkarUtil.flashVarsEnd, flashVarsStart.Index);
                        if (end > 0)
                        {
                            baseWebData = baseWebData.Substring(flashVarsStart.Index, end - flashVarsStart.Index);

                            Match idMatch = Regex.Match(baseWebData, PohadkarUtil.idRegex);
                            Match cdnLqMatch = Regex.Match(baseWebData, PohadkarUtil.cdnLqRegex);
                            Match cdnHqMatch = Regex.Match(baseWebData, PohadkarUtil.cdnHqRegex);

                            String id = (idMatch.Success) ? idMatch.Groups["id"].Value : String.Empty;
                            String cdnLq = (cdnLqMatch.Success) ? cdnLqMatch.Groups["cdnLQ"].Value : String.Empty;
                            String cdnHq = (cdnHqMatch.Success) ? cdnHqMatch.Groups["cdnHQ"].Value : String.Empty;

                            String url = String.Empty;
                            if (!String.IsNullOrEmpty(cdnHq))
                            {
                                url = WebCache.Instance.GetRedirectedUrl(String.Format(PohadkarUtil.videoUrlFormat, cdnHq));
                            }
                            else
                            {
                                url = WebCache.Instance.GetRedirectedUrl(String.Format(PohadkarUtil.videoUrlFormat, cdnLq));
                            }

                            urls.Add(url);
                        }
                    }
                }
                else
                {
                    Dictionary<String, String> playbackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").GetPlaybackOptions(HttpUtility.UrlDecode(match.Groups["showVideoUrl"].Value.Replace("-nocookie", "")));
                    if (playbackOptions != null)
                    {
                        int width = 0;
                        String format = String.Empty;
                        String url = String.Empty;

                        foreach (var option in playbackOptions)
                        {
                            Match optionMatch = Regex.Match(option.Key, PohadkarUtil.optionTitleRegex);
                            if (optionMatch.Success)
                            {
                                int tempWidth = int.Parse(optionMatch.Groups["width"].Value);
                                String tempFormat = optionMatch.Groups["format"].Value;

                                if ((tempWidth > width) ||
                                    ((tempWidth == width) && (tempFormat == "mp4")))
                                {
                                    width = tempWidth;
                                    url = option.Value;
                                    format = tempFormat;
                                }
                            }
                        }

                        urls.Add(url);
                    }
                }
            }

            return urls;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            return String.Empty;
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            List<VideoInfo> videoList = this.GetVideoList(new RssLink()
            {
                Name = "Search",
                Other = query,
                Url = String.Format(PohadkarUtil.searchQueryUrl, query)
            });

            List<ISearchResultItem> result = new List<ISearchResultItem>(videoList.Count);
            foreach (var video in videoList)
            {
                result.Add(video);
            }

            return result;
        }

        #endregion
    }
}
