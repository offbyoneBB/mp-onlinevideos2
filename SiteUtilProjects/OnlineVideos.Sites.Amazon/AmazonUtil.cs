using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
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
                        new CategoryNode { Category = new Category { Name = "Series Watchlist", Other = "WS" }, CategoryUrl = "https://www.amazon.de/gp/video/watchlist/tv/ref=dv_web_wtls_nr_bar_tv?show=24&sort=DATE_ADDED_DESC"},
                        new CategoryNode { Category = new Category { Name = "Movies Watchlist", Other = "WM" }, Url = "https://www.amazon.de/gp/video/watchlist/movies/ref=dv_web_wtls_nr_bar_mov?show=24&sort=DATE_ADDED_DESC"},
                    }},
                new CategoryNode { Category = new Category {  Name = "Series", Other = "S" } },
                new CategoryNode { Category = new Category { Name = "Movies", Other = "M" } },
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
            }

            if (node == null)
            {
                LoadEpisodes(category, videoInfos);
            }
            return videoInfos;
        }

        private void LoadEpisodes(Category category, List<VideoInfo> videoInfos)
        {
            // Dynamic nodes, like series list
            if (!string.IsNullOrEmpty(category.Other as string))
            {
                string url = $"https://www.amazon.de/gp/video/detail/{category.Other}/ref=atv_wl_hom_c_unkc_1_1";
                videoInfos.AddRange(LoadEpisodesAsync(url).Result);
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
                    categoryNode.Category.HasSubCategories = categoryNode.SubCategories?.Count > 0 || !string.IsNullOrEmpty(categoryNode.CategoryUrl);
                    categoryNode.Category.SubCategories = new List<Category>();
                    categoriesToPopulate.Add(categoryNode.Category);
                }

                if (!string.IsNullOrEmpty(node.CategoryUrl))
                {
                    PrepareUrlAsync(node.CategoryUrl).Wait();
                    var series = _driver.FindElements(By.CssSelector("[data-automation-id^=wl-item-]"));
                    foreach (IWebElement webElement in series)
                    {
                        var imgSrc = webElement.FindElement(By.TagName("img")).GetAttribute("src");
                        var asin = webElement.FindElement(By.Name("titleID")).GetAttribute("value");
                        var title = webElement.FindElement(By.TagName("h1")).GetAttribute("innerText");

                        categoriesToPopulate.Add(new Category { Name = title, Description = asin, Other = asin, Thumb = imgSrc, HasSubCategories = false, SubCategories = new List<Category>() });
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
                    Profile = new FirefoxProfile(@"C:\Users\morpheus\AppData\Roaming\Mozilla\Firefox\Profiles\Amazon.Automation")
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

        public bool HandleAction(string keyOrAction)
        {
            Actions action = new Actions(_playbackDriver);
            action.SendKeys(_keyEventTarget, keyOrAction).Perform();
            return true;
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
