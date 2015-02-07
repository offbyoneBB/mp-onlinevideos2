using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Net;
using System.Collections;
using System.Reflection;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class DreamfilmUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable ddos protection workaround"), Description("If nothing works, try this! For faster site set this to false.")]
        protected bool removeDdosProtection = false;

        protected const string TV = "TV";
        protected const string FILM = "FILM";

        // ddos prevention-prevention
        protected const string formula = @"a\.value = (\d+)\+(\d+)\*(\d+)";
        protected CookieContainer cc = new CookieContainer();
        protected const string ddosUrl = "http://dreamfilm.se{0}?jschl_vc={1}&jschl_answer={2}";

        #region MyGetWebData

        //Stolen from http://stackoverflow.com/questions/1047669/cookiecontainer-bug
        // Wildcarded domains not sent with request... i.e. ".dreamfilm.se"
        private void BugFix_CookieDomain(CookieContainer cookieContainer)
        {
            System.Type _ContainerType = typeof(CookieContainer);
            Hashtable table = (Hashtable)_ContainerType.InvokeMember("m_domainTable",
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.GetField |
                                       System.Reflection.BindingFlags.Instance,
                                       null,
                                       cookieContainer,
                                       new object[] { });
            ArrayList keys = new ArrayList(table.Keys);
            foreach (string keyObj in keys)
            {
                string key = (keyObj as string);
                if (key[0] == '.')
                {
                    string newKey = key.Remove(0, 1);
                    table[newKey] = table[keyObj];
                }
            }
        }

        protected string GetWebDataWithDdosRemoval(string url, string postData = null)
        {
            if (removeDdosProtection)
            {
                NameValueCollection headers = new NameValueCollection();
                headers.Add("Accept", "*/*"); // accept any content type
                headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
                headers.Add("Referer", url);

                BugFix_CookieDomain(cc);
                // No caching... if url results in ddos protection page.
                string data = GetWebData(url, postData, cc, null, null, false, false, null, null, headers, false);
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var form = htmlDoc.DocumentNode.SelectSingleNode("//form[@id = 'challenge-form']");
                if (form != null)
                {
                    //Need to bypass CloudFlare ddos protection, calculate answer on challenge and keep cookies
                    string action = form.GetAttributeValue("action", "");
                    string jschlVc = htmlDoc.DocumentNode.SelectSingleNode("//input[@name = 'jschl_vc']").GetAttributeValue("value", "");
                    string a = "0";
                    string b = "0";
                    string c = "0";
                    Regex rgx = new Regex(formula);
                    Match m = rgx.Match(data);
                    if (m.Success)
                    {
                        a = m.Groups[1].Value;
                        b = m.Groups[2].Value;
                        c = m.Groups[3].Value;
                    }
                    int answer = (int.Parse(a) + int.Parse(b) * int.Parse(c)) + 12;

                    var challengeUrl = string.Format(ddosUrl, action, jschlVc, answer);
                    BugFix_CookieDomain(cc);
                    data = GetWebData(challengeUrl, postData, cc, null, null, false, false, null, null, headers, false);
                }
                return data;
            }
            else
            {
                if (postData != null)
                    return GetWebData(url, postData);
                return GetWebData(url);
            }
        }

        #endregion
        #region SettingsCategories

        public override int DiscoverDynamicCategories()
        {

            Settings.Categories.ToList().ForEach((c) =>
            {
                c.HasSubCategories = true;
                if (c.Name == "TV-Serier")
                {
                    c.SubCategoriesDiscovered = true;
                    c.SubCategories.ForEach(child => child.HasSubCategories = true);
                }
                if (c.Name == "Topplista")
                {
                    c.SubCategoriesDiscovered = true;
                    c.SubCategories.ForEach(child => child.HasSubCategories = true);
                }
            });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        #endregion

        #region SubCategories

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Name == "Lista A-Ö")
            {
                string data = GetWebDataWithDdosRemoval((parentCategory as RssLink).Url);
                if (!string.IsNullOrEmpty(data))
                {
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(data);
                    foreach (var a in htmlDoc.DocumentNode.SelectNodes("//div[@class = 'l']/a"))
                    {
                        RssLink cat = new RssLink()
                        {
                            Name = a.InnerText.Replace("\r", string.Empty).Trim(),
                            HasSubCategories = true,
                            Other = TV,
                            ParentCategory = parentCategory,
                            Url = a.GetAttributeValue("href", "")
                        };
                        parentCategory.SubCategories.Add(cat);
                    }
                }
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                return parentCategory.SubCategories.Count;
            }
            else if (parentCategory.Name == "Filmer")
            {
                if (parentCategory.ParentCategory != null && parentCategory.ParentCategory.Name == "Topplista")
                {
                    string data = GetWebDataWithDdosRemoval((parentCategory as RssLink).Url);
                    if (!string.IsNullOrEmpty(data))
                    {
                        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                        htmlDoc.LoadHtml(data);

                        foreach (var a in htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'btn btn-info')]"))
                        {
                            RssLink cat = new RssLink()
                            {
                                Name = a.InnerText.Replace("\r", string.Empty).Trim(),
                                HasSubCategories = true,
                                Other = FILM,
                                ParentCategory = parentCategory,
                                Url = a.GetAttributeValue("href", "")
                            };
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                    parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                    return parentCategory.SubCategories.Count;
                }
                return DoDiscoverFilmSubCategories(parentCategory);
            }
            else
            {
                return DoDiscoverSubCategories(parentCategory);
            }
        }

        public override int DiscoverNextPageCategories(NextPageCategory nextPagecategory)
        {
            nextPagecategory.ParentCategory.SubCategories.Remove(nextPagecategory);
            Category category = new RssLink()
            {
                SubCategories = new List<Category>(),
                Url = nextPagecategory.Url
            };

            string nextPage = "";
            PopulateSubCategories(ref category, out nextPage);
            if (category.SubCategories != null)
                nextPagecategory.ParentCategory.SubCategories.AddRange(category.SubCategories);
            if (nextPage != "")
                nextPagecategory.ParentCategory.SubCategories.Add(new NextPageCategory() { ParentCategory = nextPagecategory.ParentCategory, Url = nextPage, SubCategories = new List<Category>() });

            return category.SubCategories.Count;
        }

        private int DoDiscoverSubCategories(Category parentCategory)
        {
            string nextPage = "";
            PopulateSubCategories(ref parentCategory, out nextPage);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            if (nextPage != "")
                parentCategory.SubCategories.Add(new NextPageCategory() { ParentCategory = parentCategory, Url = nextPage });

            return parentCategory.SubCategories.Count;
        }

        private int DoDiscoverFilmSubCategories(Category parentCategory)
        {
            string categoryUrl = (parentCategory as RssLink).Url;
            RssLink cat = new RssLink()
            {
                Name = "Alla",
                HasSubCategories = true,
                Other = FILM,
                ParentCategory = parentCategory,
                Url = categoryUrl

            };
            parentCategory.SubCategories.Add(cat);
            string data = GetWebDataWithDdosRemoval(categoryUrl);
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var ul = htmlDoc.DocumentNode.SelectSingleNode("//div[@class = 'well sidebar-nav']/ul");
                if (ul != null)
                {
                    foreach (var a in ul.SelectNodes("li/a").Where(a => a.GetAttributeValue("href", "") != ""))
                    {
                        cat = new RssLink()
                        {
                            Name = a.InnerText.Replace("\r", string.Empty).Trim(),
                            HasSubCategories = true,
                            Other = FILM,
                            ParentCategory = parentCategory,
                            Url = a.GetAttributeValue("href", "")
                        };
                        parentCategory.SubCategories.Add(cat);
                    }
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        private void PopulateSubCategories(ref Category parentCategory, out string nextPage)
        {
            nextPage = "";
            string categoryUrl = (parentCategory as RssLink).Url;

            string data = GetWebDataWithDdosRemoval(categoryUrl);
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                if (!categoryUrl.StartsWith("http://dreamfilm.se/search/"))
                {
                    var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, ' movie')]");
                    if (divs != null)
                    {
                        foreach (var div in divs)
                        {
                            RssLink cat = new RssLink();
                            var imageDiv = div.SelectSingleNode(".//div[@class = 'panel-body']");
                            cat.Url = imageDiv.SelectSingleNode("a").GetAttributeValue("href", "");
                            var image = imageDiv.SelectSingleNode("a/img").GetAttributeValue("src", "");
                            if (string.IsNullOrEmpty(image))
                                image = imageDiv.SelectSingleNode("a/img").GetAttributeValue("data-cfsrc", "");
                            if (!removeDdosProtection)
                                cat.Thumb = string.IsNullOrEmpty(image) ? "" : (image.StartsWith("http") ? image : string.Format("http://dreamfilm.se/{0}", image));
                            else
                                cat.Thumb = (string.IsNullOrEmpty(image) || !image.StartsWith("http")) ? "" : image;
                            cat.Name = Regex.Replace(div.GetAttributeValue("title",""), @"S[0-9]+E[0-9]+", string.Empty);
                            cat.Name = Regex.Replace(cat.Name, @"\s+", " ").Trim();
                            cat.HasSubCategories = false;
                            cat.Other = cat.Url.Contains("/movies") ? FILM : TV;
                            /*foreach (var textnode in div.SelectSingleNode("div/div").SelectNodes("text()"))
                            {
                                cat.Description += textnode.InnerText;
                            }
                            cat.Description = Regex.Replace(cat.Description, @"\s+", " ").Trim();
                             */
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                }
                else
                {
                    var searchdata = htmlDoc.DocumentNode.SelectSingleNode("//div[@id = 'searchdata']");
                    if (searchdata != null)
                    {
                        foreach (var a in searchdata.SelectNodes("ul/a") ?? new HtmlAgilityPack.HtmlNodeCollection(searchdata))
                        {
                            RssLink cat = new RssLink();
                            cat.Url = a.GetAttributeValue("href", "");
                            var image = a.SelectSingleNode("li/div/img").GetAttributeValue("src", "");
                            if (string.IsNullOrEmpty(image))
                                image = a.SelectSingleNode("li/div/img").GetAttributeValue("data-cfsrc", "");
                            if (!removeDdosProtection)
                                cat.Thumb = string.IsNullOrEmpty(image) ? "" : (image.StartsWith("http") ? image : string.Format("http://dreamfilm.se/{0}", image));
                            else
                                cat.Thumb = (string.IsNullOrEmpty(image) || !image.StartsWith("http")) ? "" : image;
                            cat.Name = Regex.Replace(a.SelectSingleNode("li/div/h4").InnerText, @"\s+", " ").Trim();
                            cat.HasSubCategories = false;
                            cat.Other = cat.Url.Contains("/movies") ? FILM : TV;
                            foreach (var textnode in a.SelectSingleNode("li/div").SelectNodes("text()"))
                            {
                                cat.Description += textnode.InnerText;
                            }
                            cat.Description = Regex.Replace(cat.Description, @"\s+", " ").Trim();
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                        }
                    }
                }
                var buttons = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, 'btn')]");
                if (buttons != null)
                {
                    var nextButton = buttons.FirstOrDefault(b => !b.GetAttributeValue("class", "").Contains("disabled") && b.InnerText != null && b.InnerText.Contains("Nästa"));
                    if (nextButton != null)
                        nextPage = nextButton.GetAttributeValue("href", "");
                }
            }
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            Category parentCategory = new RssLink()
            {
                SubCategories = new List<Category>(),
                Url = string.Format("http://dreamfilm.se/search/?q={0}", HttpUtility.UrlEncode(query))
            };
            string nextPage = "";
            PopulateSubCategories(ref parentCategory, out nextPage);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            if (nextPage != "")
                parentCategory.SubCategories.Add(new NextPageCategory() { ParentCategory = parentCategory, Url = nextPage });

            foreach (Category c in parentCategory.SubCategories)
                results.Add(c);

            return results;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string categoryUrl = (category as RssLink).Url;
            string data = GetWebDataWithDdosRemoval(categoryUrl);
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var div = htmlDoc.DocumentNode.SelectSingleNode("//div[@class = 'tabbable']");
                var lis = div.SelectSingleNode("ul").SelectNodes("li");
                string type = category.Other as string;
                if (type.Equals(FILM))
                {
                    ITrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.Movie, Title = category.Name };
                    Regex rgx = new Regex(@"http://www.imdb.com/title/(tt\d{7})/");
                    Match m = rgx.Match(data);
                    if (m.Success)
                    {
                        ti.ID_IMDB = m.Groups[1].Value;
                    }
                    rgx = new Regex(@"([^\(]*)\((\d{4})\)");
                    m = rgx.Match(category.Name);
                    uint y = 0;
                    if (m.Success)
                    {
                        ti.Title = m.Groups[1].Value;
                        uint.TryParse (m.Groups[2].Value,out y);
                        ti.Year = y;
                    }
                    string tabName;
                    string movieId;
                    HtmlAgilityPack.HtmlNode a;
                    HtmlAgilityPack.HtmlNode movieFrame;
                    VideoInfo video;
                    foreach (var li in lis)
                    {
                        a = li.SelectSingleNode("a");
                        tabName = a.InnerText.Replace("\r", string.Empty).Trim();
                        movieId = a.GetAttributeValue("href", "#iBetThisIdDoesNotExistOnPage").Replace("#", string.Empty);
                        movieFrame = div.SelectSingleNode(string.Format(".//div[@id = '{0}']/iframe", movieId));
                        video = new VideoInfo();
                        video.Other = ti;
                        video.VideoUrl = movieFrame.GetAttributeValue("src", "");
                        video.Title = string.Format("{0} [{1}]", category.Name, tabName);
                        video.Thumb = category.Thumb;
                        video.Description = category.Description;
                        videos.Add(video);
                    }
                }
                else
                {
                    List<string> seasons = new List<string>();
                    HtmlAgilityPack.HtmlNode a;
                    HtmlAgilityPack.HtmlNode seasonNode;
                    HtmlAgilityPack.HtmlNodeCollection seasonEpisodeNodes;
                    string seasonName;
                    string seasonId;
                    VideoInfo video;
                    Regex rgx = new Regex(@"([^\(]*)\((\d{4})\)");
                    Match m = rgx.Match(category.Name);
                    uint year = 0;
                    string title = category.Name;
                    if (m.Success)
                    {
                        title = m.Groups[1].Value;
                        uint.TryParse(m.Groups[2].Value, out year);
                    }
                    foreach (var li in lis)
                    {
                        a = li.SelectSingleNode("a");
                        seasonName = a.InnerText.Replace("\r", string.Empty).Replace("Säsong ", "S").Trim();
                        seasonId = a.GetAttributeValue("href", "#iBetThisIdDoesNotExistOnPage").Replace("#", string.Empty);
                        seasonNode = div.SelectSingleNode(string.Format(".//div[@id = '{0}']", seasonId));
                        seasonEpisodeNodes = seasonNode.SelectNodes(".//a[@class = 'showmovie epSelect']");
                        if (seasonEpisodeNodes != null)
                        {
                            foreach (var seasonEpisodeNode in seasonEpisodeNodes)
                            {
                                video = new VideoInfo();
                                video.VideoUrl = seasonEpisodeNode.GetAttributeValue("rel", "");
                                video.Title = (category.Name + "." + seasonName + seasonEpisodeNode.InnerText.Replace("Avsnitt ", "E")).Replace("\r", string.Empty).Trim();
                                video.Thumb = category.Thumb;
                                video.Description = category.Description;

                                ITrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.TvSeries, Title = title, Year = year };
                                rgx = new Regex(@"\.S(\d+)E(\d+)");
                                m = rgx.Match(video.Title);
                                uint s,e = 0;
                                if (m.Success)
                                {
                                    uint.TryParse(m.Groups[1].Value, out s);
                                    ti.Season = s;
                                    uint.TryParse(m.Groups[2].Value, out e);
                                    ti.Episode = e;
                                }
                                video.Other = ti;
                                videos.Add(video);
                            }
                        }
                    }
                }
            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string bestUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            string iframeUrl = video.VideoUrl;
            if ((video.Other as TrackingInfo).VideoKind == VideoKind.TvSeries)
            {
                string data = GetWebDataWithDdosRemoval("http://dreamfilm.se/CMS/modules/series/ajax.php", string.Format("action=showmovie&id={0}", video.VideoUrl));
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                iframeUrl = htmlDoc.DocumentNode.SelectSingleNode("//iframe").GetAttributeValue("src", "");
            }
            if (!string.IsNullOrEmpty(iframeUrl))
            {
                video.PlaybackOptions = Hoster.HosterFactory.GetHoster("vk").GetPlaybackOptions(iframeUrl);
                if (video.PlaybackOptions.Count > 0)
                    bestUrl = video.PlaybackOptions.First().Value;
            }

            if (inPlaylist) video.PlaybackOptions.Clear();
            return new List<string>() { bestUrl };
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return video.Other as TrackingInfo;
        }
        #endregion
    }
}
