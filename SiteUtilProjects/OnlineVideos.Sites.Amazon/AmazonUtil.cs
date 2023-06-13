using Newtonsoft.Json;
using OnlineVideos.CrossDomain;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace OnlineVideos.Sites.Amazon
{
    public partial class AmazonUtil : SiteUtilBase, IDisposable, IWebViewHTMLMediaElement
    {
        private Task _initTask;
        private Task _keyHandlerTask;
        private CancellationTokenSource _tokenSource;
        //private IWebDriverKeyHandler _keyHandler;

        private WebViewHelper _wvh;

        #region Configuration

        /// <summary>
        /// Username required for some web automation
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Login"), Description("Website user name"), PasswordPropertyText(true)]
        string AmazonUsername;

        /// <summary>
        /// Password required for some web automation
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Website password"), PasswordPropertyText(true)]
        string AmazonPassword;

        #endregion

        ~AmazonUtil()
        {
            Dispose();
        }

        public void SetWebviewHelper(WebViewHelper webViewHelper)
        {
            _wvh = webViewHelper;
        }

        public void StartPlayback()
        {
            _wvh.Execute("document.querySelector(\".dv-dp-node-playback >a\").click()");
        }

        private const string _homeUrl = "https://www.amazon.de/gp/video/storefront/";

        /// <summary>
        /// Defines the main navigation hierarchy.
        /// </summary>
        private readonly CategoryNode _categoryRoot = new CategoryNode
        {
            Category = null,
            SubCategories = new List<CategoryNode>
            {
                new CategoryNode { Category = new Category { Name = "Home Content", Other = "H" }, Url = _homeUrl, UrlType = UrlType.Home, ParseRegex = reSeries },
                new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "WS" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/tv/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.SeriesCategory, ParseRegex = reSeries },
                new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "WM" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/movie/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.Movies, ParseRegex = reSeries },
                new CategoryNode { Category = new Category { Name = "Genres", Other = "MG" }, Url = "https://www.amazon.de/gp/video/categories", UrlType = UrlType.Genres},

                //new CategoryNode {
                //    Category = new Category { Name = "Watchlist", Other = "W" },
                //    SubCategories = new List<CategoryNode>
                //    {
                //        new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "WS" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/tv/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.SeriesCategory, ParseRegex = reSeries },
                //        new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "WM" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/movie/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.Movies, ParseRegex = reSeries },
                //    }},
                //new CategoryNode { Category = new Category {  Name = "Series", Other = "S", Thumb = "https://m.media-amazon.com/images/S/pv-target-images/d38224028da665637203c2fa88a310b63e331638d8b529b3a03d60ed568256b2._UR1920,1080_SX720_FMjpg_.jpg" } ,
                //    SubCategories = new List<CategoryNode> {
                //        new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "SWS" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/tv/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.SeriesCategory, ParseRegex = reSeries },
                //        new CategoryNode { Category = new Category { Name = "Recently Added", Other = "SRA" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031&s=date-desc-rank&dc&_encoding=UTF8&qid=1586595330&rnid=7448692031&ref=sr_st_date-desc-rank", UrlType = UrlType.SearchResults },
                //        new CategoryNode { Category = new Category { Name = "Popular Series", Other = "SP" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031&s=popularity-rank&dc&_encoding=UTF8&qid=1586595304&rnid=7448692031&ref=sr_nr_p_n_ways_to_watch_1", UrlType = UrlType.SearchResults},
                //        new CategoryNode { Category = new Category { Name = "Less than 30 days available", Other = "SL" }, Url = "https://www.amazon.de/s/ref=sr_nr_n_1?rh=n%3A9798874031%2Cn%3A!3010076031%2Cn%3A3015916031&bbn=9798874031&ie=UTF8", UrlType = UrlType.SearchResults},
                //        new CategoryNode { Category = new Category { Name = "4K UHD", Other = "S4" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031%2Cp_n_video_quality%3A16184010031&s=featured-rank&dc&_encoding=UTF8&qid=1586595380&rnid=3010076031&ref=sr_st_featured-rank", UrlType = UrlType.SearchResults},
                //    } },
                //new CategoryNode { Category = new Category { Name = "Movies", Other = "M", Thumb = "https://m.media-amazon.com/images/S/pv-target-images/17ac72f32d352fd80d432d410d8595b77cb1001787603ebc3f622253ae8a516d._UR1920,1080_SX720_FMjpg_.jpg"} ,
                //    SubCategories = new List<CategoryNode> {
                //        new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "MWM" }, Url = "https://www.amazon.de/gp/video/mystuff/watchlist/movie/ref=atv_mys_wl_tab?sort=DATE_ADDED_DESC", UrlType = UrlType.Movies, ParseRegex = reSeries },
                //        new CategoryNode { Category = new Category { Name = "Recently Added", Other = "MRA" }, Url = "https://www.amazon.de/s/ref=atv_sn_piv_cl1_mv_ra?_encoding=UTF8&rh=n%3A3010075031%2Cn%3A3356018031%2Cn%3A4190509031&sort=popularity-rank", UrlType = UrlType.SearchResults },
                //        new CategoryNode { Category = new Category { Name = "Popular Movies", Other = "MP" }, Url = "https://www.amazon.de/s/ref=atv_sn_piv_cl1_mv_pl?_encoding=UTF8&rh=n%3A3010075031%2Cn%3A3356018031&sort=popularity-rank", UrlType = UrlType.SearchResults},
                //        new CategoryNode { Category = new Category { Name = "Less than 30 days available", Other = "ML" }, Url = "https://www.amazon.de/s?i=instant-video&bbn=9798874031&rh=n%3A9798874031%2Cn%3A3015915031&dc&qid=1586595215&rnid=3010076031&ref=sr_nr_n_1", UrlType = UrlType.SearchResults},
                //        new CategoryNode { Category = new Category { Name = "4K UHD", Other = "M4" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015915031%2Cp_n_ways_to_watch%3A7448695031%2Cp_n_video_quality%3A16184010031&s=date-desc-rank&dc&_encoding=UTF8&qid=1586593929&rnid=16184008031&ref=sr_nr_p_n_video_quality_2", UrlType = UrlType.SearchResults},
                //        new CategoryNode { Category = new Category { Name = "Genres", Other = "MG" }, Url = "https://www.amazon.de/gp/video/categories", UrlType = UrlType.Genres},
                //    } },
            }
        };

        /// <summary>
        /// Override the loading of main categories
        /// </summary>
        /// <returns></returns>
        public override int DiscoverDynamicCategories()
        {
            _initTask = InitBrowserAsync();
            Settings.DynamicCategoriesDiscovered = true;
            BuildCategoriesAsync(null, Settings.Categories).Wait();
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            BuildCategoriesAsync(parentCategory, parentCategory.SubCategories).Wait();
            parentCategory.SubCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var videoInfos = new List<VideoInfo>();
            var node = FindInTree(_categoryRoot, category);
            string pageUrl = node?.Url ?? category.Other as string;
            var isHomeContainer = pageUrl != null && pageUrl.StartsWith("HOMEIDX");
            var homeIdx = isHomeContainer ? int.Parse(pageUrl.Replace("HOMEIDX", "")) : -1;
            if (isHomeContainer && _homeContents != null)
            {
                int idx = -1;
                foreach (var container in _homeContents.props.containers)
                {
                    idx++;
                    if (idx != homeIdx)
                        continue;

                    var items = container.entities;
                    foreach (var item in items)
                    {
                        // Filter "Prime" (or other entitlements) only
                        if (!item.filterEntitled)
                            continue;

                        string rating = item.customerReviews != null ? $"| {item.customerReviews.countFormatted} ({item.customerReviews.count})" : "";

                        videoInfos.Add(new VideoInfo
                        {
                            Title = item.displayTitle.Decode(),
                            Description = string.Format("{0} | {1} | {2} {3}\r\n{4} | {5}\r\n{6}",
                                item.releaseYear,
                                item.categorizedGenres.primaryGenre,
                                item.contentMaturityRating.title,
                                rating,
                                item.entityType,
                                item.entitlements.FirstOrDefault(),
                                item.synopsis.Decode()),
                            Thumb = item.images.cover?.url ?? item.images.hero?.url,
                            VideoUrl = "https://www.amazon.de" + item.link.url
                        });
                    }
                    return videoInfos;
                }
            }

            if (pageUrl != null && pageUrl.StartsWith("https://"))
            {
                PrepareUrlAsync(pageUrl).Wait();
                var content = _wvh.GetCurrentPageContent();

                var jsonMatches = reJsonContent.Matches(content);
                foreach (Match jsonMatch in jsonMatches)
                {
                    //var genreContents = ParseJsonContentGenreItems(jsonMatch.Value);
                    var genreContents = ParseJsonContentHome(jsonMatch.Value);
                    if (genreContents != null)
                    {
                        int idx = -1;
                        foreach (var container in genreContents.props.containers)
                        {
                            idx++;
                            if (isHomeContainer && idx != homeIdx)
                                continue;

                            var items = container.entities;
                            foreach (var item in items)
                            {
                                // Filter "Prime" only
                                if (!item.filterEntitled)
                                    continue;

                                string rating = item.customerReviews != null ? $"| {item.customerReviews.countFormatted} ({item.customerReviews.count})" : "";

                                videoInfos.Add(new VideoInfo
                                {
                                    Title = item.displayTitle.Decode(),
                                    Description = string.Format("{0} | {1} | {2} {3}\r\n{4} | {5}\r\n{6}",
                                        item.releaseYear,
                                        item.categorizedGenres.primaryGenre,
                                        item.contentMaturityRating.title,
                                        rating,
                                        item.entityType,
                                        item.entitlements.FirstOrDefault(),
                                        item.synopsis.Decode()),
                                    Thumb = item.images.cover?.url ?? item.images.hero?.url,
                                    VideoUrl = "https://www.amazon.de" + item.link.url
                                });
                            }
                        }

                        return videoInfos;
                    }

                    var videoItemDetails = ParseJsonContent(content);
                    if (videoItemDetails?.props?.content?.baseOutput?.containers != null)
                    {
                        var items = videoItemDetails.props.content.baseOutput.containers.First().entities;
                        foreach (var item in items)
                        {
                            videoInfos.Add(new VideoInfo
                            {
                                Title = item.title.Decode(),
                                Description = string.Format("{0} | {1} | {2} | {3} ({4}) \r\n{5}",
                                    item.releaseYear,
                                    item.categorizedGenres.primaryGenre,
                                    item.contentMaturityRating.title,
                                    item.customerReviews.countFormatted,
                                    item.customerReviews.count,
                                    item.synopsis.Decode()),
                                Thumb = item.images.cover.url,
                                VideoUrl = "https://www.amazon.de" + item.link.url
                            });
                        }

                        return videoInfos;
                    }

                    if (node != null && node.ParseRegex != null)
                    {
                        var matches = node.ParseRegex.Matches(content);
                        foreach (Match match in matches)
                        {
                            var imgSrc = match.Groups["image"].Value;
                            var url = "https://www.amazon.de" + match.Groups["detail"].Value;
                            //var asin = await (await webElement.QuerySelectorAsync("[name=titleID]"))?.GetAttributeAsync("value");
                            var title = match.Groups["title"].Value;
                            var description = match.Groups["role"].Value;
                            //var rating = webElement.TryFindElement(By.ClassName("tst-hover-maturity-rating"))?.GetAttribute("innerText");
                            //var description = !string.IsNullOrEmpty(rating) ? desc + "\r\n" + rating : desc;

                            videoInfos.Add(new VideoInfo
                            {
                                Title = title,
                                Description = description,
                                Other = url,
                                Thumb = imgSrc,
                                VideoUrl = url
                            });
                        }
                    }
                }
            }

            if (node == null)
            {
                // Page is already loaded, try to parse episodes
                videoInfos.AddRange(LoadEpisodesAsync().Result);
            }
            return videoInfos;
        }

        private static Regex reSeries = new Regex(
            @"<article[^>]+?title=.(?<title>[^""]+?)""[^>]+?type=.(?<type>[^""]+?)"".+?<a href=.(?<detail>[^""]+?\/detail\/[^""]+?)""[^>]+?>(?<role>[^<]+?)<.+?<img[^>]+?src=.(?<image>[^""]+?)"".+?\/article>",
            RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Regex reJsonContent = new Regex("<script type=\"text\\/template\">(?<props>{\"props\":[^<]*)<\\/script>", RegexOptions.Compiled);
        private HomeContent.Root _homeContents;


        private HomeContent.Root ParseJsonContentHome(string content)
        {
            var jsonMatches = reJsonContent.Matches(content);
            foreach (Match jsonMatch in jsonMatches)
            {
                try
                {
                    var contentHome = JsonConvert.DeserializeObject<HomeContent.Root>(jsonMatch.Groups["props"].Value);
                    if (contentHome?.props?.containers != null && contentHome.props.containers.Exists(c => c.containerType == "StandardCarousel"))
                    {
                        return contentHome;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            return null;
        }

        private GenreList.Items.Root ParseJsonContentGenreItems(string content)
        {
            var jsonMatches = reJsonContent.Matches(content);
            foreach (Match jsonMatch in jsonMatches)
            {
                try
                {
                    var genreDetails = JsonConvert.DeserializeObject<GenreList.Items.Root>(jsonMatch.Groups["props"].Value);
                    if (genreDetails?.props?.containers != null && genreDetails.props.containers.Exists(c => c.containerType == "StandardCarousel"))
                    {
                        return genreDetails;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            return null;
        }

        private GenreList.Root ParseJsonContentGenres(string content)
        {
            var jsonMatches = reJsonContent.Matches(content);
            foreach (Match jsonMatch in jsonMatches)
            {
                try
                {
                    var genreDetails = JsonConvert.DeserializeObject<GenreList.Root>(jsonMatch.Groups["props"].Value);
                    if (genreDetails?.props?.containers != null)
                    {
                        return genreDetails;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            return null;
        }

        private Root ParseJsonContent(string content)
        {
            var jsonMatches = reJsonContent.Matches(content);
            foreach (Match jsonMatch in jsonMatches)
            {
                try
                {
                    Root videoItemDetails = JsonConvert.DeserializeObject<Root>(jsonMatch.Groups["props"].Value);
                    if (videoItemDetails != null && videoItemDetails.props != null && videoItemDetails.props.content != null)
                    {
                        return videoItemDetails;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            return null;
        }

        private EpisodeList.Root ParseJsonContentEpisodes(string content)
        {
            var jsonMatches = reJsonContent.Matches(content);
            foreach (Match jsonMatch in jsonMatches)
            {
                try
                {
                    var json = jsonMatch.Groups["props"].Value;
                    if (!json.Contains("\"episodeNumber\":"))
                        continue;

                    EpisodeList.Root videoItemDetails = JsonConvert.DeserializeObject<EpisodeList.Root>(json);
                    if (videoItemDetails?.props?.state?.detail != null)
                    {
                        return videoItemDetails;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// Build the specified category list
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="categoriesToPopulate"></param>
        private async Task BuildCategoriesAsync(Category parentCategory, IList<Category> categoriesToPopulate)
        {
            var node = FindInTree(_categoryRoot, parentCategory);
            if (node != null && (node.Category == null || !node.Category.SubCategoriesDiscovered))
            {
                foreach (CategoryNode categoryNode in node.SubCategories)
                {
                    categoryNode.Category.HasSubCategories = categoryNode.SubCategories?.Count > 0 || (!string.IsNullOrEmpty(categoryNode.Url) && (categoryNode.UrlType == UrlType.Home || categoryNode.UrlType == UrlType.SeriesCategory || categoryNode.UrlType == UrlType.Genres));
                    categoryNode.Category.SubCategories = new List<Category>();
                    categoryNode.Category.ParentCategory = parentCategory;
                    categoriesToPopulate.Add(categoryNode.Category);
                }

                if (node.UrlType == UrlType.Home)
                {
                    await PrepareUrlAsync(node.Url);
                    var content = _wvh.GetCurrentPageContent();

                    _homeContents = ParseJsonContentHome(content);
                    if (_homeContents != null)
                    {
                        int i = 0;
                        foreach (var container in _homeContents.props.containers)
                        {
                            categoriesToPopulate.Add(new Category
                            {
                                Name = container.title,
                                ParentCategory = parentCategory,
                                HasSubCategories = container.entities.Any(e => e.entityType == null), /* categories have null type */
                                SubCategories = new List<Category>(),
                                Other = "HOMEIDX" + (i++)
                            });
                        }

                    }
                }

                if (node.UrlType == UrlType.SeriesCategory || node.UrlType == UrlType.Movies)
                {
                    await PrepareUrlAsync(node.Url);
                    var content = _wvh.GetCurrentPageContent();

                    var videoItemDetails = ParseJsonContent(content);
                    if (videoItemDetails?.props?.content?.baseOutput?.containers != null)
                    {
                        var items = videoItemDetails.props.content.baseOutput.containers.FirstOrDefault().entities;
                        foreach (var item in items)
                        {
                            categoriesToPopulate.Add(new Category
                            {
                                Name = item.title.Decode(),
                                Description = string.Format("{0} | {1} | {2} | {3} ({4})\r\n\r\n{5}",
                                    item.releaseYear,
                                    item.categorizedGenres.primaryGenre,
                                    item.contentMaturityRating.title,
                                    item.customerReviews.countFormatted,
                                    item.customerReviews.count,
                                    item.synopsis.Decode()),
                                Other = "https://www.amazon.de" + item.link.url,
                                Thumb = item.images.cover.url,
                                ParentCategory = parentCategory,
                                HasSubCategories = false,
                                SubCategories = new List<Category>()
                            });
                        }
                    }
                }

                if (node.UrlType == UrlType.Genres)
                {
                    await PrepareUrlAsync(node.Url);
                    var content = _wvh.GetCurrentPageContent();

                    var videoItemDetails = ParseJsonContentGenres(content);
                    if (videoItemDetails?.props?.containers != null)
                    {
                        var items = videoItemDetails.props.containers.FirstOrDefault().entities;
                        foreach (var item in items)
                        {
                            categoriesToPopulate.Add(new Category
                            {
                                Name = item.displayTitle.Decode(),
                                Other = "https://www.amazon.de" + item.link.url,
                                Thumb = item.images.cover.url,
                                ParentCategory = parentCategory,
                                HasSubCategories = false,
                                SubCategories = new List<Category>()
                            });
                        }
                    }
                }
            }

            // No main navigation node, check if we want to load home content categories
            var pageUrl = parentCategory?.Other as string;
            var isHomeContainer = pageUrl != null && pageUrl.StartsWith("HOMEIDX");
            var homeIdx = isHomeContainer ? int.Parse(pageUrl.Replace("HOMEIDX", "")) : -1;
            if (isHomeContainer && _homeContents != null)
            {
                int idx = -1;
                foreach (var container in _homeContents.props.containers)
                {
                    idx++;
                    if (idx != homeIdx)
                        continue;

                    var items = container.entities;
                    foreach (var item in items)
                    {
                        // Filter "Prime" only, but leave categories and genres included (they don't have an entityType)
                        if (item.entityType != null)
                            continue;

                        categoriesToPopulate.Add(new Category
                        {
                            Name = item.title.Decode(),
                            Other = "https://www.amazon.de" + item.link?.url,
                            Thumb = item.images?.cover?.url,
                            ParentCategory = parentCategory,
                            HasSubCategories = false,
                            SubCategories = new List<Category>()
                        });
                    }
                }
            }

        }

        private async Task<List<VideoInfo>> LoadVideosAsync(string url)
        {
            List<VideoInfo> videoInfos = new List<VideoInfo>();
            await PrepareUrlAsync(url);
            //var videos = await _wvh.DevTools.QuerySelectorAllAsync("[data-automation-id^=wl-item-]");
            //var videos = _driver.FindElements(By.CssSelector("[data-automation-id^=wl-item-]"));
            Regex reDuration = new Regex(@"(\d) Std. (\d+) Min.");
            //foreach (HtmlElement webElement in videos)
            //{
            //    var imgSrc = await (await webElement.QuerySelectorAsync("img")).GetAttributeAsync("src");
            //    var asin = await (await webElement.QuerySelectorAsync("[name=titleID]"))?.GetAttributeAsync("value");
            //    //var title = webElement.TryFindElement(By.TagName("h1"))?.GetAttribute("innerText");
            //    //var desc = webElement.TryFindElement(By.ClassName("tst-hover-synopsis"))?.GetAttribute("innerText");
            //    //var rating = webElement.TryFindElement(By.ClassName("tst-hover-maturity-rating"))?.GetAttribute("innerText");
            //    //var fullContent = webElement.GetAttribute("innerHTML");
            //    //var match = reDuration.Match(fullContent);
            //    //var duration = match?.Value;

            //    //var resume = webElement.TryFindElement(By.CssSelector("a[data-resume-time]"))?.GetAttribute("data-resume-time"); // in seconds
            //    double resumePercent = 0d;
            //    //if (match.Success && int.TryParse(resume, out int resumeSeconds))
            //    //{
            //    //    int totalTime = (int.Parse(match.Groups[1].Value) * 60 + int.Parse(match.Groups[2].Value)) * 60; // also in seconds
            //    //    resumePercent = (double)resumeSeconds / totalTime * 100;
            //    //}

            //    //var description = !string.IsNullOrEmpty(rating) ? desc + "\r\n" + rating : desc;

            //    // Playback progress
            //    ExtendedProperties extendedProperties = new ExtendedProperties { VideoProperties = { ["Progress"] = string.Format("{0:0}%", resumePercent) } };

            //    //videoInfos.Add(new VideoInfo { Title = title, Description = description, VideoUrl = asin, Thumb = imgSrc, Length = duration, Other = extendedProperties });
            //}
            return videoInfos;
        }

        private async Task<List<VideoInfo>> LoadEpisodesAsync()
        {
            List<VideoInfo> videoInfos = new List<VideoInfo>();
            var content = _wvh.GetCurrentPageContent();
            var v = ParseJsonContentEpisodes(content);
            if (v != null)
            {
                var items = v.props.state.detail.detail;
                foreach (var kvp in items)
                {
                    var item = kvp.Value;
                    //var progress = webElement.TryFindElement(By.CssSelector("span[aria-valuenow]"))?.GetAttribute("aria-valuenow");
                    //int.TryParse(progress, out int progressPercent);
                    //// Playback progress
                    //ExtendedProperties extendedProperties = new ExtendedProperties { VideoProperties = { ["Progress"] = string.Format("{0:0}%", progressPercent) } };

                    var videoInfo = new VideoInfo
                    {
                        Title = item.title.Decode(),
                        Description = string.Format("Season {0} Episode {1} | {2} | {3}\r\n{4}",
                            item.seasonNumber,
                            item.episodeNumber,
                            item.releaseDate,
                            item.ratingBadge.description,
                            item.synopsis.Decode()),
                        VideoUrl = $"https://www.amazon.de/gp/video/detail/{kvp.Key}/ref=stream_prime_hd_ep?autoplay=1&t=0",
                        Thumb = item.images?.packshot,
                        Airdate = item.releaseDate,
                        Length = TimeSpan.FromSeconds(item.duration).ToString(),
                        //Other = extendedProperties
                    };

                    videoInfos.Add(videoInfo);
                }
            }

            return videoInfos;
        }

        #region Browser automation

        private async Task InitBrowserAsync()
        {
            _wvh.DebugMode(true);
            bool clear = false;
            if (clear)
            {
                _wvh.DeleteAllCookies();
            }
            // Load user's main page to init browser and do the login process
            _wvh.SetUrlAndWait("https://www.amazon.de/gp/video/mystuff");
            string fn = "(function(id,val) {{ var e=document.getElementById(id); if (e) {{ e.value=val; document.querySelector('input[type=submit]').click(); return true; }} return false; }})('{0}', '{1}'); ";
            var call1 = string.Format(fn, "ap_email", AmazonUsername);
            _wvh.ExecuteAndWait(call1, "true");
            var call2 = string.Format(fn, "ap_password", AmazonPassword);
            _wvh.ExecuteAndWait(call2, "true");
        }

        private async Task PrepareUrlAsync(string url, bool windowVisible = false)
        {
            _wvh.SetUrlAndWait(url);
        }

        #endregion

        #region Private helper methods

        private CategoryNode FindInTree(CategoryNode node, Category findCategory)
        {
            if (node.Category?.Other == findCategory?.Other)
                return node;
            // Current level first
            foreach (CategoryNode categoryNode in node.SubCategories)
            {
                if (categoryNode.Category?.Other == findCategory?.Other)
                    return categoryNode;
            }
            // Then all sub-levels
            foreach (CategoryNode categoryNode in node.SubCategories)
            {
                var subNode = FindInTree(categoryNode, findCategory);
                if (subNode != null)
                    return subNode;
            }
            return null;
        }

        #endregion

        public bool StartPlayback(string playbackUrl)
        {
            string url = $"https://www.amazon.de/gp/video/detail/{playbackUrl}/ref=stream_prime_hd_ep?autoplay=1&t=0";
            //PrepareUrl(url, true, ref _playbackDriver);
            //_keyEventTarget = _playbackDriver.FindElements(By.CssSelector(".webPlayerElement")).FirstOrDefault();
            return true;
        }

        //private async Task ListenForKeysAsync(IWebDriver driver, CancellationToken token)
        //{
        //    if (driver is IJavaScriptExecutor js)
        //    {
        //        var body = driver.FindElement(By.TagName("body"));
        //        js.ExecuteScript(@"var b=arguments[0];b.keyQueue=[];b.addEventListener('keyup', function onkeyup(key) { b.keyQueue.push(key.key); } );", body);
        //        bool doStop = false;
        //        do
        //        {
        //            var keyFromBrowser = js.ExecuteScript("return arguments[0].keyQueue.shift();", body) as string;
        //            await Task.Delay(50);
        //            token.ThrowIfCancellationRequested();
        //            //if (_keyHandler != null && keyFromBrowser != null)
        //            //{
        //            //    if (keyFromBrowser == "MediaPlayPause" || keyFromBrowser == "MediaPause" ||
        //            //        keyFromBrowser == "MediaPlay")
        //            //    {
        //            //        // Map to Space
        //            //        HandleAction(" ");
        //            //    }
        //            //    else
        //            //        doStop = _keyHandler.HandleKey(keyFromBrowser);
        //            //}
        //        } while (!doStop);
        //    }
        //}

        //public bool HandleAction(string keyOrAction)
        //{
        //    Actions action = new Actions(_playbackDriver);
        //    action.SendKeys(_keyEventTarget, keyOrAction).Perform();
        //    return true;
        //}

        //public void SetKeyHandler(IWebDriverKeyHandler handler)
        //{
        //    _keyHandler = handler;
        //    _tokenSource = new CancellationTokenSource();
        //    _keyHandlerTask = ListenForKeysAsync(_playbackDriver, _tokenSource.Token);
        //}

        //public bool Fullscreen()
        //{
        //    _playbackDriver.Manage().Window.FullScreen();
        //    return true;
        //}

        //public bool SetWindowBoundaries(Point position, Size size)
        //{
        //    _playbackDriver.Manage().Window.Position = position;
        //    _playbackDriver.Manage().Window.Size = size;
        //    return true;
        //}

        public bool ShutDown()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;
            _keyHandlerTask = null;
            //_playbackDriver?.Quit();
            //_playbackDriver?.Dispose();
            //_playbackDriver = null;
            return true;
        }

        public void Dispose()
        {
            ShutDown();
            //_driver?.Quit();
            //_driver?.Dispose();
            _initTask?.Dispose();
            //_driver = null;
            _initTask = null;
        }

        public string VideoElementSelector { get; } = "document.getElementsByTagName('video')[0]";
    }

    public static class ParserExtensions
    {
        public static string Decode(this string input)
        {
            return HttpUtility.HtmlDecode(input);
        }
    }
    //public static class WebDriverExtensions
    //{
    //    public static IWebElement TryFindElement(this IWebElement element, By by)
    //    {
    //        return element.FindElements(by).FirstOrDefault();
    //    }
    //}
}
