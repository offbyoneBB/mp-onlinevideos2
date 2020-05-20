using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;

namespace OnlineVideos.Sites.Amazon
{
    public partial class AmazonUtil : SiteUtilBase, IDisposable, IWebDriverSite
    {
        /// <summary>
        /// For browsing we use a "headless" instance (no window visible)
        /// </summary>
        private IWebDriver _driver;
        /// <summary>
        /// Driver used for real playback. Its Window is visible
        /// </summary>
        private IWebDriver _playbackDriver;
        /// <summary>
        /// The element inside player instance which handles key presses.
        /// </summary>
        private IWebElement _keyEventTarget;
        private Task _initTask;
        private Task _keyHandlerTask;
        private CancellationTokenSource _tokenSource;
        private IWebDriverKeyHandler _keyHandler;

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

        /// <summary>
        /// Defines the main navigation hierarchy.
        /// </summary>
        private readonly CategoryNode _categoryRoot = new CategoryNode
        {
            Category = null,
            SubCategories = new List<CategoryNode>
            {
                new CategoryNode {
                    Category = new Category { Name = "Watchlist", Other = "W" },
                    SubCategories = new List<CategoryNode>
                    {
                        new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "WS" }, Url = "https://www.amazon.de/gp/video/watchlist/tv/ref=dv_web_wtls_nr_bar_tv?show=24&sort=DATE_ADDED_DESC", UrlType = UrlType.SeriesCategory },
                        new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "WM" }, Url = "https://www.amazon.de/gp/video/watchlist/movies/ref=dv_web_wtls_nr_bar_mov?show=24&sort=DATE_ADDED_DESC", UrlType = UrlType.Movies },
                    }},
                new CategoryNode { Category = new Category {  Name = "Series", Other = "S", Thumb = "https://images-eu.ssl-images-amazon.com/images/S/atv-aps-images/encoded/STAR_TREK_PICARD/de_DE/COVER_ART/VINEYARD._UR1920,1080_RI_SX512_FMjpg_.jpg" } ,
                    SubCategories = new List<CategoryNode> {
                        new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "SWS" }, Url = "https://www.amazon.de/gp/video/watchlist/tv/ref=dv_web_wtls_nr_bar_tv?show=24&sort=DATE_ADDED_DESC", UrlType = UrlType.SeriesCategory },
                        new CategoryNode { Category = new Category { Name = "Recently Added", Other = "SRA" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031&s=date-desc-rank&dc&_encoding=UTF8&qid=1586595330&rnid=7448692031&ref=sr_st_date-desc-rank", UrlType = UrlType.SearchResults },
                        new CategoryNode { Category = new Category { Name = "Popular Series", Other = "SP" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031&s=popularity-rank&dc&_encoding=UTF8&qid=1586595304&rnid=7448692031&ref=sr_nr_p_n_ways_to_watch_1", UrlType = UrlType.SearchResults},
                        new CategoryNode { Category = new Category { Name = "Less than 30 days available", Other = "SL" }, Url = "https://www.amazon.de/s/ref=sr_nr_n_1?rh=n%3A9798874031%2Cn%3A!3010076031%2Cn%3A3015916031&bbn=9798874031&ie=UTF8", UrlType = UrlType.SearchResults},
                        new CategoryNode { Category = new Category { Name = "4K UHD", Other = "S4" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015916031%2Cp_n_ways_to_watch%3A7448695031%2Cp_n_video_quality%3A16184010031&s=featured-rank&dc&_encoding=UTF8&qid=1586595380&rnid=3010076031&ref=sr_st_featured-rank", UrlType = UrlType.SearchResults},
                    } },
                new CategoryNode { Category = new Category { Name = "Movies", Other = "M", Thumb = "https://images-eu.ssl-images-amazon.com/images/S/sgp-catalog-images/region_DE/disney-196960-Full-Image_GalleryCover-de-DE-1585331856342._UY500_UX667_RI_VsH2HPvUQOsKgBGkxruR0RTFVRlzMl7_TTW_SX512_.jpg"} ,
                    SubCategories = new List<CategoryNode> {
                        new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "MWM" }, Url = "https://www.amazon.de/gp/video/watchlist/movies/ref=dv_web_wtls_nr_bar_mov?show=24&sort=DATE_ADDED_DESC", UrlType = UrlType.SearchResults },
                        new CategoryNode { Category = new Category { Name = "Recently Added", Other = "MRA" }, Url = "https://www.amazon.de/s/ref=atv_sn_piv_cl1_mv_ra?_encoding=UTF8&rh=n%3A3010075031%2Cn%3A3356018031%2Cn%3A4190509031&sort=popularity-rank", UrlType = UrlType.SearchResults },
                        new CategoryNode { Category = new Category { Name = "Popular Movies", Other = "MP" }, Url = "https://www.amazon.de/s/ref=atv_sn_piv_cl1_mv_pl?_encoding=UTF8&rh=n%3A3010075031%2Cn%3A3356018031&sort=popularity-rank", UrlType = UrlType.SearchResults},
                        new CategoryNode { Category = new Category { Name = "Less than 30 days available", Other = "ML" }, Url = "https://www.amazon.de/s?i=instant-video&bbn=9798874031&rh=n%3A9798874031%2Cn%3A3015915031&dc&qid=1586595215&rnid=3010076031&ref=sr_nr_n_1", UrlType = UrlType.SearchResults},
                        new CategoryNode { Category = new Category { Name = "4K UHD", Other = "M4" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015915031%2Cp_n_ways_to_watch%3A7448695031%2Cp_n_video_quality%3A16184010031&s=date-desc-rank&dc&_encoding=UTF8&qid=1586593929&rnid=16184008031&ref=sr_nr_p_n_video_quality_2", UrlType = UrlType.SearchResults},
                        new CategoryNode { Category = new Category { Name = "Genres", Other = "MG" }, Url = "https://www.amazon.de/s?i=prime-instant-video&bbn=3279204031&rh=n%3A3279204031%2Cn%3A3015915031%2Cp_n_ways_to_watch%3A7448695031&s=featured-rank&dc&_encoding=UTF8&qid=1586595682&rnid=3010076031&ref=sr_nr_n_1", UrlType = UrlType.Genres},
                    } },
            }
        };

        /// <summary>
        /// Override the loading of main categories
        /// </summary>
        /// <returns></returns>
        public override int DiscoverDynamicCategories()
        {
            _initTask = InitBrowser();
            Settings.DynamicCategoriesDiscovered = true;
            BuildCategories(null, Settings.Categories);
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            BuildCategories(parentCategory, parentCategory.SubCategories);
            parentCategory.SubCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var videoInfos = new List<VideoInfo>();
            var node = FindInTree(_categoryRoot, category);
            if (node != null && !string.IsNullOrEmpty(node.Url))
            {
                videoInfos.AddRange(LoadVideosAsync(node.Url).Result);
                if (videoInfos.Count == 0)
                {
                    videoInfos.AddRange(LoadVideosFromSearchAsync(node.Url).Result);
                }
            }

            if (node == null)
            {
                LoadEpisodes(category, videoInfos);
                LoadUrlFilteredVideos(category, videoInfos);
            }
            return videoInfos;
        }

        private void LoadEpisodes(Category category, List<VideoInfo> videoInfos)
        {
            // Dynamic nodes, like series list
            string asin = category.Other as string;
            if (!string.IsNullOrEmpty(asin) && !asin.StartsWith("https://"))
            {
                string url = $"https://www.amazon.de/gp/video/detail/{asin}/ref=atv_wl_hom_c_unkc_1_1";
                videoInfos.AddRange(LoadEpisodesAsync(url).Result);
            }
        }
        private void LoadUrlFilteredVideos(Category category, List<VideoInfo> videoInfos)
        {
            // Dynamic nodes, like series list
            string url = category.Other as string;
            if (!string.IsNullOrEmpty(url) && url.StartsWith("https://"))
            {
                videoInfos.AddRange(LoadVideosFromSearchAsync(url).Result);
            }
        }

        /// <summary>
        /// Build the specified category list
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="categoriesToPopulate"></param>
        private void BuildCategories(Category parentCategory, IList<Category> categoriesToPopulate)
        {
            var node = FindInTree(_categoryRoot, parentCategory);
            if (node != null && (node.Category == null || !node.Category.SubCategoriesDiscovered))
            {
                foreach (CategoryNode categoryNode in node.SubCategories)
                {
                    categoryNode.Category.HasSubCategories = categoryNode.SubCategories?.Count > 0 || (!string.IsNullOrEmpty(categoryNode.Url) && (categoryNode.UrlType == UrlType.SeriesCategory || categoryNode.UrlType == UrlType.Genres));
                    categoryNode.Category.SubCategories = new List<Category>();
                    categoryNode.Category.ParentCategory = parentCategory;
                    categoriesToPopulate.Add(categoryNode.Category);
                }

                if (node.UrlType == UrlType.SeriesCategory)
                {
                    PrepareUrlAsync(node.Url).Wait();
                    var series = _driver.FindElements(By.CssSelector("[data-automation-id^=wl-item-]"));
                    foreach (IWebElement webElement in series)
                    {
                        var imgSrc = webElement.FindElement(By.TagName("img")).GetAttribute("src");
                        var asin = webElement.FindElement(By.Name("titleID")).GetAttribute("value");
                        var title = webElement.FindElement(By.TagName("h1")).GetAttribute("innerText");

                        categoriesToPopulate.Add(new Category
                        {
                            Name = title,
                            Description = asin,
                            Other = asin,
                            Thumb = imgSrc,
                            ParentCategory = parentCategory,
                            HasSubCategories = false,
                            SubCategories = new List<Category>()
                        });
                    }
                }

                if (node.UrlType == UrlType.Genres)
                {
                    PrepareUrlAsync(node.Url).Wait();
                    var genres = _driver.FindElements(By.CssSelector("[id^=p_n_theme_browse-bin\\/]"));
                    foreach (IWebElement webElement in genres)
                    {
                        var link = webElement.FindElement(By.TagName("a"));

                        var url = link.GetAttribute("href");
                        var title = link.GetAttribute("innerText");

                        categoriesToPopulate.Add(new Category
                        {
                            Name = title,
                            Other = url,
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
            var videos = _driver.FindElements(By.CssSelector("[data-automation-id^=wl-item-]"));
            foreach (IWebElement webElement in videos)
            {
                var imgSrc = webElement.FindElement(By.TagName("img")).GetAttribute("src");
                var asin = webElement.FindElement(By.Name("titleID")).GetAttribute("value");
                var title = webElement.FindElement(By.TagName("h1")).GetAttribute("innerText");

                videoInfos.Add(new VideoInfo { Title = title, Description = asin, VideoUrl = asin, Thumb = imgSrc });
            }
            return videoInfos;
        }

        private async Task<List<VideoInfo>> LoadVideosFromSearchAsync(string url)
        {
            List<VideoInfo> videoInfos = new List<VideoInfo>();
            await PrepareUrlAsync(url);
            var searchResults = _driver.FindElements(By.ClassName("s-result-list")).FirstOrDefault();
            if (searchResults != null)
            {
                var videos = _driver.FindElements(By.ClassName("s-result-item"));
                foreach (IWebElement webElement in videos)
                {
                    var imgSrc = webElement.FindElement(By.ClassName("s-image")).GetAttribute("src");
                    var asin = webElement.GetAttribute("data-asin");
                    var title = webElement.FindElement(By.TagName("h2")).GetAttribute("innerText");

                    videoInfos.Add(new VideoInfo { Title = title, Description = asin, VideoUrl = asin, Thumb = imgSrc });
                }
            }
            return videoInfos;
        }

        private async Task<List<VideoInfo>> LoadEpisodesAsync(string url)
        {
            await PrepareUrlAsync(url);
            List<VideoInfo> videoInfos = new List<VideoInfo>();
            var episodes = _driver.FindElements(By.CssSelector("[id^=av-ep-episodes-]"));
            foreach (IWebElement webElement in episodes)
            {
                var title = webElement.FindElement(By.ClassName("js-eplist-episode")).Text;
                var imgSrc = webElement.FindElement(By.TagName("img")).GetAttribute("src");
                var asin = webElement.FindElement(By.CssSelector("[id^=selector-]")).GetAttribute("id").Replace("selector-", "");

                videoInfos.Add(new VideoInfo { Title = title, Description = asin, VideoUrl = asin, Thumb = imgSrc });
            }
            return videoInfos;
        }

        #region Browser automation

        private Task InitBrowser()
        {
            // Load user's main page to init browser and do the login process
            return Task.Run(() => PrepareUrl("https://www.amazon.de/gp/video/mystuff/ref=atv_nb_mystuff"));
        }

        private async Task<bool> PrepareUrlAsync(string url)
        {
            if (_initTask != null) await _initTask;
            PrepareUrl(url);
            return true;
        }


        private void PrepareUrl(string url)
        {
            PrepareUrl(url, false, ref _driver);
        }

        private void PrepareUrl(string url, bool windowVisible, ref IWebDriver driver)
        {
            if (driver == null)
            {
                var firefoxOptions = new FirefoxOptions
                {
                    /* Note: IWebDriverSite is loaded in another AppDomain, but CodeBase points to original location  */
                    Profile = new FirefoxProfile(Path.Combine(Path.GetDirectoryName(typeof(IWebDriverSite).Assembly.CodeBase.Replace("file:///", "")), @"Profiles\Firefox.Automation"))
                };
                // For navigation we don't want the firefox window appearing
                if (!windowVisible)
                    firefoxOptions.AddArgument("-headless");
                var ffds = FirefoxDriverService.CreateDefaultService();
                ffds.HideCommandPromptWindow = true;
                driver = new FirefoxDriver(ffds, firefoxOptions);
                if (windowVisible)
                    driver.Manage().Window.FullScreen();
            }
            driver.Navigate().GoToUrl(url);

            // Check if login is needed, then we have a form for email input
            IWebElement loginField = driver.FindElements(By.Id("ap_email")).FirstOrDefault();
            if (loginField != null)
            {
                loginField.SendKeys(AmazonUsername);
                driver.FindElement(By.CssSelector(".a-button-inner > #continue")).Click();
                driver.FindElement(By.Id("ap_password")).SendKeys(AmazonPassword);
                driver.FindElement(By.Id("signInSubmit")).Click();
            }
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
            PrepareUrl(url, true, ref _playbackDriver);
            _keyEventTarget = _playbackDriver.FindElements(By.CssSelector(".webPlayerElement")).FirstOrDefault();
            return true;
        }

        private async Task ListenForKeysAsync(IWebDriver driver, CancellationToken token)
        {
            if (driver is IJavaScriptExecutor js)
            {
                var body = driver.FindElement(By.TagName("body"));
                js.ExecuteScript(@"var b=arguments[0];b.keyQueue=[];b.addEventListener('keyup', function onkeyup(key) { b.keyQueue.push(key.key); } );", body);
                bool doStop = false;
                do
                {
                    var keyFromBrowser = js.ExecuteScript("return arguments[0].keyQueue.shift();", body) as string;
                    await Task.Delay(50);
                    token.ThrowIfCancellationRequested();
                    if (_keyHandler != null && keyFromBrowser != null)
                    {
                        Log.Debug("WebDriver: Received key: {0}", keyFromBrowser);
                        if (keyFromBrowser == "MediaPlayPause" || keyFromBrowser == "MediaPause" ||
                            keyFromBrowser == "MediaPlay")
                        {
                            // Map to Space
                            HandleAction(" ");
                        }
                        else
                            doStop = _keyHandler.HandleKey(keyFromBrowser);
                    }
                } while (!doStop);
            }
        }

        public bool HandleAction(string keyOrAction)
        {
            Actions action = new Actions(_playbackDriver);
            action.SendKeys(_keyEventTarget, keyOrAction).Perform();
            return true;
        }

        public void SetKeyHandler(IWebDriverKeyHandler handler)
        {
            _keyHandler = handler;
            _tokenSource = new CancellationTokenSource();
            _keyHandlerTask = ListenForKeysAsync(_playbackDriver, _tokenSource.Token);
        }

        public bool Fullscreen()
        {
            _playbackDriver.Manage().Window.FullScreen();
            return true;
        }

        public bool SetWindowBoundaries(Point position, Size size)
        {
            _playbackDriver.Manage().Window.Position = position;
            _playbackDriver.Manage().Window.Size = size;
            return true;
        }

        public bool ShutDown()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;
            _keyHandlerTask = null;
            _playbackDriver?.Quit();
            _playbackDriver?.Dispose();
            _playbackDriver = null;
            return true;
        }

        public void Dispose()
        {
            ShutDown();
            _driver?.Quit();
            _driver?.Dispose();
            _initTask?.Dispose();
            _driver = null;
            _initTask = null;
        }

    }
}
