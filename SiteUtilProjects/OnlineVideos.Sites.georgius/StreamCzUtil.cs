using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;

namespace OnlineVideos.Sites.georgius
{
    public sealed class StreamCzUtil : SiteUtilBase
    {
        private class CategoryShowsRegex
        {
            #region Private fields
            #endregion

            #region Constructors

            public CategoryShowsRegex() : base()
            {
                this.CategoryNames = new List<string>();
                this.ShowsBlockEndRegex = String.Empty;
                this.ShowsBlockStartRegex = String.Empty;
                this.ShowsNextPageRegex = String.Empty;
                this.ShowStartRegex = String.Empty;
                this.ShowThumbUrlRegex = String.Empty;
                this.ShowUrlAndTitleRegex = String.Empty;
                this.SkipFirstPage = false;
                this.HasShows = false;
                this.BaseUrl = String.Empty;
                this.SearchQueryUrl = String.Empty;
            }

            #endregion

            #region Properties

            public List<String> CategoryNames { get; set; }
            public String ShowsBlockStartRegex { get; set; }
            public String ShowsBlockEndRegex { get; set; }
            public String ShowStartRegex { get; set; }
            public String ShowThumbUrlRegex { get; set; }
            public String ShowUrlAndTitleRegex { get; set; }
            public String ShowsNextPageRegex { get; set; }
            public Boolean SkipFirstPage { get; set; }
            public Boolean HasShows { get; set; }
            public String BaseUrl { get; set; }
            public String SearchQueryUrl { get; set; }

            #endregion
        }

        private class CategoryLink : RssLink
        {
            public CategoryLink()
                : base()
            {
                this.BaseUrl = String.Empty;
            }

            public String BaseUrl { get; set; }
        }

        #region Private fields

        private static String baseUrl = "http://www.stream.cz";

        private static String dynamicCategoryStart = @"<ul id=""headMenu"">";
        private static String dynamicCategoryEnd = @"</ul>";
        private static String dynamicCategoryRegex = @"<a href=""(?<categoryUrl>[^""]+)"">(?<categoryTitle>[^<]+)</a>";

        private static String dynamicSubCategoryStartRegex = @"(<ul class=""uniMenu"">)|(<div id=""menu"">)";
        private static String dynamicSubCategoryEndRegex = @"(</ul>)|(<div id=""submenu_bg"">)";
        private static String dynamicSubCategoryRegex = @"(<li><a( )?(class=""active"")? href=""(?<subCategoryUrl>[^""]+)"">(?<subCategoryTitle>[^<]+)</a></li>)|(<a href=""(?<subCategoryUrl>[^""]+)"">(?<subCategoryTitle>[^<]+)</a>)";
        private static String dynamicSubCategoryNextPageRegex = @"<li class=""nextLink""><a href=""(?<categoryUrl>[^""]+)"">Další &raquo;</a></li>";
        private static String dynamicSubCategoryOrderRegex = @"<a  href=""(?<subCategoryUrl>[^""]+)"">Dle abecedy</a>";

        private static String doNotShowCategoryRegex = @"(Filmy)";
        private static String doNotShowSubCategoryRegex = @"(Úvod)|(Hudební akce)|(Vše)|(Česká televize)|(Kanály)";

        private static List<CategoryShowsRegex> categoriesAndShows = new List<CategoryShowsRegex>()
        {
            new CategoryShowsRegex()
            {
                CategoryNames = new List<string>() { "TOP 20", "Nejnovější klipy", "Pořady" },
                ShowsBlockStartRegex = @"",
                ShowsBlockEndRegex = @"",
                ShowStartRegex = @"",
                ShowThumbUrlRegex = @"",
                ShowUrlAndTitleRegex = @"",
                ShowsNextPageRegex = @"",
                HasShows = false,
                SkipFirstPage = false,
                BaseUrl = @"http://music.stream.cz",
                SearchQueryUrl = @"http://music.stream.cz/hledej?q={0}"
            },

            new CategoryShowsRegex()
            {
                CategoryNames = new List<string>() { "Stream", "TV Prima", "Óčko", "Public TV" },
                ShowsBlockStartRegex = @"<div class=""vertical670Content"">",
                ShowsBlockEndRegex = @"<div class=""vertical250Box"">",
                ShowStartRegex = @"<div class=""matrixThreeVideoList matrixThreeVideoListNumber[0-9]+"">",
                ShowThumbUrlRegex = @"style=""background: #000 url\('(?<showThumbUrl>[^']*)'\) no-repeat scroll 50% 50%;""",
                ShowUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""[^""]*"">(?<showTitle>[^<]*)</a>",
                ShowsNextPageRegex = @"<a href=""(?<url>[^""]*)"" class=""fakeButtonInline"">další&nbsp;&raquo;</a>",
                HasShows = true,
                SkipFirstPage = false,
                BaseUrl = @"http://www.stream.cz",
                SearchQueryUrl = @"http://www.stream.cz/?search_text={0}&a=search"
            },

            new CategoryShowsRegex()
            {
                CategoryNames = new List<string>(),
                ShowsBlockStartRegex = @"<div class=""vertical670Content"">",
                ShowsBlockEndRegex = @"<div class=""vertical250Box"">",
                ShowStartRegex = @"<div class=""matrixThreeVideoList matrixThreeVideoListNumber[0-9]+"">",
                ShowThumbUrlRegex = @"style=""background: #000 url\('(?<showThumbUrl>[^']*)'\) no-repeat scroll 50% 50%;""",
                ShowUrlAndTitleRegex = @"<a href=""(?<showUrl>[^""]*)"" title=""[^""]*"">(?<showTitle>[^<]*)</a>",
                ShowsNextPageRegex = @"<a href=""(?<url>[^""]*)"" class=""fakeButtonInline"">další&nbsp;&raquo;</a>",
                HasShows = false,
                SkipFirstPage = false,
                BaseUrl = @"http://www.stream.cz",
                SearchQueryUrl = @"http://www.stream.cz/?search_text={0}&a=search"
            }
        };

        private static List<ShowEpisodesRegex> showsAndEpisodes = new List<ShowEpisodesRegex>()
        {
            new ShowEpisodesRegex()
            {
                ShowNames = new List<string>() { "Search" },
                ShowEpisodesBlockStartRegex = @"<div class=""verticalBoxGenericContent"">",
                ShowEpisodesBlockEndRegex = @"<div id=""searchResultWorld"">",
                ShowEpisodeStartRegex = @"<div class=""videoList"">",
                ShowEpisodeThumbUrlRegex = @"style=""background: #000 url\('(?<showEpisodeThumbUrl>[^']*)'\) no-repeat scroll 50% 50%;",
                ShowEpisodeUrlAndTitleRegex = @"<a href=""(?<showEpisodeUrl>[^""]*)"" title=""[^""]*"">(?<showEpisodeTitle>[^<]*)</a>",
                ShowEpisodeLengthRegex = @"<span class=""videoListTime"">(?<showEpisodeLength>[^<]*)</span>",
                ShowEpisodeDescriptionRegex = @"<p>(?<showEpisodeDescription>[^<]*)</p>",
                ShowEpisodesNextPageRegex = @"<a href=""(?<url>[^""]*)"" class=""fakeButtonInline"">další&nbsp;&raquo;</a>",
                SkipFirstPage = false
            },

            new ShowEpisodesRegex()
            {
                 ShowNames = new List<string>(),
                 ShowEpisodesBlockStartRegex = @"(<div class=""vertical670Box"">)|(<div class=""videoListContent"">)|(<ul class=""item_list"">)|(<div id=""videa_kanalu_list"">)",
                 ShowEpisodesBlockEndRegex = @"(<div id=""js_fan_not_logged"")|(<div class=""vertical250Box"">)|(<div class=""qf_letter_list"">)|(<div id=""main_right"" class=""channel_right_top"">)",
                 ShowEpisodeStartRegex = @"(<div class=""matrixThreeVideoList matrixThreeVideoListNumber[0-9]*"">)|(<div class=""videoList"">)|(<li class=""clip "">)|(<li class=""show "">)|(<div class=""kanal_1video"">)",
                 ShowEpisodeThumbUrlRegex = @"(url\('(?<showEpisodeThumbUrl>[^']*)'\))|(src=""(?<showEpisodeThumbUrl>[^""]*)"")",
                 ShowEpisodeUrlAndTitleRegex = @"(<a href=""(?<showEpisodeUrl>[^""]*)"" title=""[^""]*"">(?<showEpisodeTitle>[^<]*)</a>)|(<a class=""title"" href=""(?<showEpisodeUrl>[^""]*)"">(?<showEpisodeTitle>[^<]*)</a>)|(<a class=""[^""]*"" href=""(?<showEpisodeUrl>[^""]*)"" title=""[^""]*"">(?<showEpisodeTitle>[^<]*))",
                 ShowEpisodeLengthRegex = @"<span class=""[^""]*"">(?<showEpisodeLength>[^<]*)</span>",
                 ShowEpisodesNextPageRegex = @"(<a href=""(?<url>[^""]*)"" class=""fakeButtonInline"">další&nbsp;&raquo;</a>)|(<a href=""(?<url>[^""]*)"">následující&nbsp;&raquo;</a>)",
                 ShowEpisodeDescriptionRegex = @"(<p>(?<showEpisodeDescription>[^<]*)</p>)|(<p class=""matrixDescription"">(?<showEpisodeDescription>[^<]*)</p>)",
                 SkipFirstPage = false
            }
        };

        private static String videoUrlFormat = @"http://cdn-dispatcher.stream.cz/?id={0}"; // add 'cdnId'

        private static String flashVarsStartRegex = @"(<param name=""flashvars"" value=)|(writeSWF)";
        private static String flashVarsEnd = @"/>";
        private static String idRegex = @"id=(?<id>[^&]+)";
        private static String cdnLqRegex = @"((cdnLQ)|(cdnID)){1}=(?<cdnLQ>[^&]+)";
        private static String cdnHqRegex = @"((cdnHQ)|(hdID)){1}=(?<cdnHQ>[^&]+)";

        private static String searchQueryUrl = @"http://www.stream.cz/?search_text={0}&a=search";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private CategoryLink currentCategory = new CategoryLink();

        #endregion

        #region Constructors

        public StreamCzUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
            {
                // find subcategory of main category
                List<CategoryLink> subCategories = this.GetSubCategories(((RssLink)parentCategory).Url);
                foreach (var subCategory in subCategories)
                {
                    subCategory.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(subCategory);
                }
                parentCategory.SubCategoriesDiscovered = true;
                return subCategories.Count;
            }
            else
            {
                // find shows
                CategoryShowsRegex categoryShowRegex = null;
                foreach (var categoryAndShow in StreamCzUtil.categoriesAndShows)
                {
                    if ((categoryAndShow.CategoryNames.Contains(parentCategory.Name)) || (categoryAndShow.CategoryNames.Count == 0))
                    {
                        categoryShowRegex = categoryAndShow;
                        break;
                    }
                }

                List<CategoryLink> shows = this.GetShows(((RssLink)parentCategory).Url, categoryShowRegex);
                foreach (var show in shows)
                {
                    show.ParentCategory = parentCategory;
                    parentCategory.SubCategories.Add(show);
                }
                parentCategory.SubCategoriesDiscovered = true;
                return shows.Count;
            }
        }

        private List<CategoryLink> GetShows(String subCategoryUrl, CategoryShowsRegex categoryShowRegex)
        {
            List<CategoryLink> shows = new List<CategoryLink>();

            String pageUrl = subCategoryUrl;

            while (!String.IsNullOrEmpty(pageUrl))
            {
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                Match categoryShowsStart = Regex.Match(baseWebData, categoryShowRegex.ShowsBlockStartRegex);
                if (categoryShowsStart.Success)
                {
                    baseWebData = baseWebData.Substring(categoryShowsStart.Index);
                    if (!String.IsNullOrEmpty(categoryShowRegex.ShowsBlockEndRegex))
                    {
                        Match categoryShowsEnd = Regex.Match(baseWebData, categoryShowRegex.ShowsBlockEndRegex);
                        if (categoryShowsEnd.Success)
                        {
                            baseWebData = baseWebData.Substring(0, categoryShowsEnd.Index);
                        }
                    }

                    MatchCollection matches = Regex.Matches(baseWebData, categoryShowRegex.ShowStartRegex);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        int start = matches[i].Index;
                        int end = (i < (matches.Count - 1)) ? matches[i + 1].Index : baseWebData.Length;

                        String showData = baseWebData.Substring(start, end - start);

                        String showThumb = String.Empty;
                        String showTitle = String.Empty;
                        String showUrl = String.Empty;

                        Match match = Regex.Match(showData, categoryShowRegex.ShowThumbUrlRegex);
                        if (match.Success)
                        {
                            showThumb = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, StreamCzUtil.baseUrl);
                            showData = showData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(showData, categoryShowRegex.ShowUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, StreamCzUtil.baseUrl);
                            showTitle = match.Groups["showTitle"].Value;
                            showData = showData.Substring(match.Index + match.Length);
                        }

                        if (!(String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl)))
                        {
                            shows.Add(new CategoryLink()
                            {
                                Name = showTitle,
                                Url = showUrl,
                                Thumb = showThumb,
                                HasSubCategories = false,
                                BaseUrl = categoryShowRegex.BaseUrl
                            });
                        }
                    }

                    Match nextPage = Regex.Match(baseWebData, categoryShowRegex.ShowsNextPageRegex);
                    pageUrl = (nextPage.Success) ? Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(nextPage.Groups["url"].Value), StreamCzUtil.baseUrl) : String.Empty;
                }
                else
                {
                    pageUrl = String.Empty;
                }
            }

            return shows;
        }

        private List<CategoryLink> GetSubCategories(String categoryUrl)
        {
            List<CategoryLink> subCategories = new List<CategoryLink>();
            String baseWebData = SiteUtilBase.GetWebData(categoryUrl);

            while (true)
            {
                Match start = Regex.Match(baseWebData, StreamCzUtil.dynamicSubCategoryStartRegex);
                if (start.Success)
                {
                    baseWebData = baseWebData.Substring(start.Index);
                    Match end = Regex.Match(baseWebData, StreamCzUtil.dynamicSubCategoryEndRegex);
                    if (end.Success)
                    {
                        String categoryData = baseWebData.Substring(0, end.Index);
                        Match nextPageMatch = Regex.Match(categoryData, StreamCzUtil.dynamicSubCategoryNextPageRegex);
                        if (nextPageMatch.Success)
                        {
                            String nextPageUrl = nextPageMatch.Groups["categoryUrl"].Value;
                            subCategories.AddRange(this.GetSubCategories(Utils.FormatAbsoluteUrl(nextPageUrl, StreamCzUtil.baseUrl)));
                        }
                        else
                        {
                            MatchCollection matches = Regex.Matches(categoryData, StreamCzUtil.dynamicSubCategoryRegex);
                            foreach (Match match in matches)
                            {
                                String title = match.Groups["subCategoryTitle"].Value.Trim();
                                if (!Regex.Match(title, StreamCzUtil.doNotShowSubCategoryRegex).Success)
                                {
                                    CategoryShowsRegex categoryShowRegex = null;
                                    foreach (var categoryAndShow in StreamCzUtil.categoriesAndShows)
                                    {
                                        if ((categoryAndShow.CategoryNames.Contains(title)) || (categoryAndShow.CategoryNames.Count == 0))
                                        {
                                            categoryShowRegex = categoryAndShow;
                                            break;
                                        }
                                    }

                                    String subCategoryUrl = Utils.FormatAbsoluteUrl(match.Groups["subCategoryUrl"].Value, categoryShowRegex.BaseUrl);
                                    String subCategoryWebData = SiteUtilBase.GetWebData(subCategoryUrl);
                                    Match order = Regex.Match(subCategoryWebData, StreamCzUtil.dynamicSubCategoryOrderRegex);
                                    if (order.Success)
                                    {
                                        subCategoryUrl = Utils.FormatAbsoluteUrl(order.Groups["subCategoryUrl"].Value, categoryShowRegex.BaseUrl);
                                    }

                                    subCategories.Add(new CategoryLink()
                                    {
                                        Name = title,
                                        Url = subCategoryUrl,
                                        HasSubCategories = categoryShowRegex.HasShows,
                                        SubCategories = new List<Category>(),
                                        BaseUrl = categoryShowRegex.BaseUrl
                                    });
                                }
                            }
                        }

                        baseWebData = baseWebData.Substring(end.Index + end.Length);
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

            return subCategories;
        }

        private Boolean AreThereSubCategories(String categoryUrl)
        {
            String baseWebData = SiteUtilBase.GetWebData(categoryUrl);

            while (true)
            {
                Match start = Regex.Match(baseWebData, StreamCzUtil.dynamicSubCategoryStartRegex);
                if (start.Success)
                {
                    baseWebData = baseWebData.Substring(start.Index);
                    Match end = Regex.Match(baseWebData, StreamCzUtil.dynamicSubCategoryEndRegex);
                    if (end.Success)
                    {
                        String categoryData = baseWebData.Substring(0, end.Index);
                        MatchCollection matches = Regex.Matches(categoryData, StreamCzUtil.dynamicSubCategoryRegex);
                        foreach (Match match in matches)
                        {
                            if (!Regex.Match(match.Groups["subCategoryTitle"].Value, StreamCzUtil.doNotShowSubCategoryRegex).Success)
                            {
                                return true;
                            }
                        }

                        baseWebData = baseWebData.Substring(end.Index + end.Length);
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

            return false;
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;
            String baseWebData = SiteUtilBase.GetWebData(StreamCzUtil.baseUrl);
            StringBuilder categoryData = new StringBuilder();
            int searchFrom = 0;
            while (true)
            {
                int start = baseWebData.IndexOf(StreamCzUtil.dynamicCategoryStart, searchFrom);
                if (start > 0)
                {
                    int end = baseWebData.IndexOf(StreamCzUtil.dynamicCategoryEnd, start);
                    if (end > 0)
                    {
                        categoryData.Append(baseWebData.Substring(start, end - start));
                        searchFrom = end;
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

            String categories = categoryData.ToString();
            MatchCollection matches = Regex.Matches(categories, StreamCzUtil.dynamicCategoryRegex);
            for (int i = 0; i < matches.Count; i++)
            {
                if (!Regex.Match(matches[i].Groups["categoryTitle"].Value, StreamCzUtil.doNotShowCategoryRegex).Success)
                {
                    Boolean subCategoriesExist = AreThereSubCategories(Utils.FormatAbsoluteUrl(matches[i].Groups["categoryUrl"].Value, StreamCzUtil.baseUrl));
                    RssLink category = new RssLink()
                    {
                        Name = matches[i].Groups["categoryTitle"].Value,
                        Url = Utils.FormatAbsoluteUrl(matches[i].Groups["categoryUrl"].Value, StreamCzUtil.baseUrl),
                        HasSubCategories = subCategoriesExist
                    };

                    if (subCategoriesExist)
                    {
                        category.SubCategories = new List<Category>();
                        category.SubCategoriesDiscovered = false;
                    }

                    dynamicCategoriesCount++;
                    this.Settings.Categories.Add(category);
                }
            }
            
            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl, ShowEpisodesRegex showEpisodesRegex)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = SiteUtilBase.GetWebData(pageUrl);

                Match showEpisodesStart = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesBlockStartRegex);
                if (showEpisodesStart.Success)
                {
                    baseWebData = baseWebData.Substring(showEpisodesStart.Index);

                    if (!String.IsNullOrEmpty(showEpisodesRegex.ShowEpisodesBlockEndRegex))
                    {
                        Match showEpisodesEnd = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesBlockEndRegex);
                        if (showEpisodesEnd.Success)
                        {
                            baseWebData = baseWebData.Substring(0, showEpisodesEnd.Index);
                        }
                    }

                    MatchCollection episodes = Regex.Matches(baseWebData, showEpisodesRegex.ShowEpisodeStartRegex);
                    for (int i = 0; i < episodes.Count; i++)
                    {
                        int start = episodes[i].Index;
                        int end = (i < (episodes.Count - 1)) ? episodes[i + 1].Index : baseWebData.Length;

                        String episodeData = baseWebData.Substring(start, end - start);

                        String showTitle = String.Empty;
                        String showThumbUrl = String.Empty;
                        String showUrl = String.Empty;
                        String showLength = String.Empty;
                        String showDescription = String.Empty;

                        Match match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeThumbUrlRegex);
                        if (match.Success)
                        {
                            showThumbUrl = match.Groups["showEpisodeThumbUrl"].Value;
                            episodeData = episodeData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeLengthRegex);
                        if (match.Success)
                        {
                            showLength = HttpUtility.HtmlDecode(match.Groups["showEpisodeLength"].Value);
                            episodeData = episodeData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeUrlAndTitleRegex);
                        if (match.Success)
                        {
                            showUrl = Utils.FormatAbsoluteUrl(HttpUtility.HtmlDecode(match.Groups["showEpisodeUrl"].Value), pageUrl);
                            showTitle = HttpUtility.HtmlDecode(match.Groups["showEpisodeTitle"].Value);
                            episodeData = episodeData.Substring(match.Index + match.Length);
                        }

                        match = Regex.Match(episodeData, showEpisodesRegex.ShowEpisodeDescriptionRegex);
                        if (match.Success)
                        {
                            showDescription = HttpUtility.HtmlDecode(match.Groups["showEpisodeDescription"].Value);
                            episodeData = episodeData.Substring(match.Index + match.Length);
                        }

                        if (!(String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl)))
                        {
                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Description = showDescription,
                                ImageUrl = showThumbUrl,
                                Length = showLength,
                                Title = showTitle,
                                VideoUrl = showUrl
                            };

                            pageVideos.Add(videoInfo);
                        }
                    }
                }

                Match nextPageMatch = Regex.Match(baseWebData, showEpisodesRegex.ShowEpisodesNextPageRegex);
                if (nextPageMatch.Success)
                {
                    String subUrl = HttpUtility.HtmlDecode(nextPageMatch.Groups["url"].Value);
                    this.nextPageUrl = Utils.FormatAbsoluteUrl(subUrl, pageUrl);
                }
                else
                {
                    this.nextPageUrl = String.Empty;
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category)
        {
            hasNextPage = false;
            String baseWebData = String.Empty;
            CategoryLink parentCategory = (CategoryLink)category;
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

            ShowEpisodesRegex showEpisodesRegex = null;
            foreach (var showAndEpisodes in StreamCzUtil.showsAndEpisodes)
            {
                if ((showAndEpisodes.ShowNames.Contains(this.currentCategory.Name)) || (showAndEpisodes.ShowNames.Count == 0))
                {
                    showEpisodesRegex = showAndEpisodes;
                    break;
                }
            }

            this.loadedEpisodes.AddRange(this.GetPageVideos(this.nextPageUrl, showEpisodesRegex));
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
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl);
            baseWebData = HttpUtility.HtmlDecode(baseWebData);

            Match flashVarsStart = Regex.Match(baseWebData, StreamCzUtil.flashVarsStartRegex);
            if (flashVarsStart.Success)
            {
                int end = baseWebData.IndexOf(StreamCzUtil.flashVarsEnd, flashVarsStart.Index);
                if (end > 0)
                {
                    baseWebData = baseWebData.Substring(flashVarsStart.Index, end - flashVarsStart.Index);

                    Match idMatch = Regex.Match(baseWebData, StreamCzUtil.idRegex);
                    Match cdnLqMatch = Regex.Match(baseWebData, StreamCzUtil.cdnLqRegex);
                    Match cdnHqMatch = Regex.Match(baseWebData, StreamCzUtil.cdnHqRegex);

                    String id = (idMatch.Success) ? idMatch.Groups["id"].Value : String.Empty;
                    String cdnLq = (cdnLqMatch.Success) ? cdnLqMatch.Groups["cdnLQ"].Value : String.Empty;
                    String cdnHq = (cdnHqMatch.Success) ? cdnHqMatch.Groups["cdnHQ"].Value : String.Empty;

                    if ((!String.IsNullOrEmpty(cdnLq)) && (!String.IsNullOrEmpty(cdnHq)))
                    {
                        // we got low and high quality
                        String lowQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(StreamCzUtil.videoUrlFormat, cdnLq));
                        String highQualityUrl = SiteUtilBase.GetRedirectedUrl(String.Format(StreamCzUtil.videoUrlFormat, cdnHq));

                        video.PlaybackOptions = new Dictionary<string, string>();
                        video.PlaybackOptions.Add("Low quality", lowQualityUrl);
                        video.PlaybackOptions.Add("High quality", highQualityUrl);
                    }
                    else if (!String.IsNullOrEmpty(cdnLq))
                    {
                        video.VideoUrl = SiteUtilBase.GetRedirectedUrl(String.Format(StreamCzUtil.videoUrlFormat, cdnLq));
                    }
                    else if (!String.IsNullOrEmpty(cdnHq))
                    {
                        video.VideoUrl = SiteUtilBase.GetRedirectedUrl(String.Format(StreamCzUtil.videoUrlFormat, cdnHq));
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

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<VideoInfo> videoList = this.getVideoList(new CategoryLink()
            {
                Name = "Search",
                Other = query,
                Url = String.Format(searchQueryUrl, query),
                BaseUrl = StreamCzUtil.baseUrl
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
