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

namespace OnlineVideos.Sites
{
    public class DreamfilmUtil : SiteUtilBase
    {
        private int currentCategoryPage = 0;
        private string currentSearch = "";

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

        protected string GetWebDataWithDdosRemoval(string url,string postData = null)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            headers.Add("Referer", url);

            BugFix_CookieDomain(cc);
            // No caching... if url results in ddos protection page.
            string data = GetWebData(url, postData, headers, cc, null, false, false, null, false);
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(data);
            var form = htmlDoc.DocumentNode.SelectSingleNode("//form[@id = 'challenge-form']");
            if (form != null)
            {
                //Need to bypass CloudFlare ddos protection, calculate answer on challenge and keep cookies
                string action = form.GetAttributeValue("action","");
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
                data = GetWebData(challengeUrl, postData, headers, cc, null, false, false, null, false);
            }
            return data;
        }

        #endregion
        #region SettingsCategories

        public override int DiscoverDynamicCategories()
        {

            Settings.Categories.ToList().ForEach(c => c.HasSubCategories = true);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        #endregion

        #region SubCategories
 
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            currentCategoryPage = 0;
            if (parentCategory.Name == "Filmer")
                return DoDiscoverFilmSubCategories(parentCategory);
            else
            {
                return DoDiscoverSubCategories(parentCategory);
            }
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            currentCategoryPage++;
            return DoDiscoverSubCategories(category.ParentCategory);
        }

        private int DoDiscoverSubCategories(Category parentCategory)
        {
            var foundSoFar = parentCategory.SubCategories.Count;
            PopulateSubCategories(ref parentCategory);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count - foundSoFar > 0;
            if (parentCategory.SubCategoriesDiscovered)
                parentCategory.SubCategories.Add(new NextPageCategory() { ParentCategory = parentCategory });

            return parentCategory.SubCategories.Count - foundSoFar;
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

        private void PopulateSubCategories(ref Category parentCategory)
        {
            string categoryUrl;
            bool  notSearch = (parentCategory is RssLink) && !string.IsNullOrEmpty((parentCategory as RssLink).Url);
            if (notSearch)
                categoryUrl = (parentCategory as RssLink).Url;
            else
                categoryUrl = string.Format("http://dreamfilm.se/search/?q={0}", currentSearch);

            string data = GetWebDataWithDdosRemoval(string.Format("{0}{1}page={2}", categoryUrl, categoryUrl.Contains("?") ? "&" : "?", currentCategoryPage));
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                if (notSearch)
                {
                    var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'galery info')]");
                    if (divs != null)
                    {
                        foreach (var div in divs)
                        {
                            RssLink cat = new RssLink();
                            var imageDiv = div.SelectSingleNode("div[@class = 'image-galery']");
                            cat.Url = imageDiv.SelectSingleNode("a").GetAttributeValue("href", "");
                            var image = imageDiv.SelectSingleNode("a/img").GetAttributeValue("src", "");
                            //do not get thumb if source dreamfilm due to ddos protection
                            cat.Thumb = (string.IsNullOrEmpty(image) || !image.StartsWith("http")) ? "" : image;
                            cat.Name = Regex.Replace(div.SelectSingleNode("div/div/h4").InnerText, @"S[0-9]+E[0-9]+", string.Empty);
                            cat.Name = Regex.Replace(cat.Name, @"\s+", " ").Trim();
                            cat.HasSubCategories = false;
                            cat.Other = cat.Url.Contains("/movies") ? FILM : TV;
                            foreach (var textnode in div.SelectSingleNode("div/div").SelectNodes("text()"))
                            {
                                cat.Description += textnode.InnerText;
                            }
                            cat.Description = Regex.Replace(cat.Description, @"\s+", " ").Trim();
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
                            //do not get thumb if source dreamfilm due to ddos protection
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

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            currentSearch = HttpUtility.UrlEncode(query);
            Category parentCategory = new Category()
            {
                SubCategories = new List<Category>()
            };
            currentCategoryPage = 0;
            PopulateSubCategories(ref parentCategory);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            if (parentCategory.SubCategoriesDiscovered)
                parentCategory.SubCategories.Add(new NextPageCategory() { ParentCategory = parentCategory });

            foreach (Category c in parentCategory.SubCategories)
                results.Add(c);

            return results;
        }
        
        #endregion

        #region Videos
        
        public override List<VideoInfo> getVideoList(Category category)
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
                        video.Other = FILM;
                        video.VideoUrl = movieFrame.GetAttributeValue("src", "");
                        video.Title = string.Format("{0} [{1}]", category.Name, tabName);
                        video.ImageUrl = category.Thumb;
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
                                video.Other = TV;
                                video.VideoUrl = seasonEpisodeNode.GetAttributeValue("href", "");
                                video.Title = (category.Name + "." + seasonName + seasonEpisodeNode.InnerText.Replace("Avsnitt ", "E")).Replace("\r", string.Empty).Trim();
                                video.ImageUrl = category.Thumb;
                                video.Description = category.Description;
                                videos.Add(video);
                            }
                        }
                    }
                }
            }
            return videos;
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string bestUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            string iframeUrl = video.VideoUrl;
            string type = (string)video.Other;
            if (type.Equals(TV))
            {
                string data = GetWebDataWithDdosRemoval("http://dreamfilm.se/CMS/modules/series/ajax.php", string.Format("action=showmovie&id={0}", video.VideoUrl));
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                iframeUrl = htmlDoc.DocumentNode.SelectSingleNode("//iframe").GetAttributeValue("src", "");
            }
            if (!string.IsNullOrEmpty(iframeUrl))
            {
                string frameData = GetWebData(iframeUrl) ?? "";
                Regex rgx = new Regex(@"var vars = (.*)");
                Match m = rgx.Match(frameData);
                if (m.Success)
                {
                    var json = JObject.Parse(m.Groups[1].Value);
                    rgx = new Regex(@"url([0-9]+)");
                    int res;
                    int max = 0;
                    string url;
                    foreach (JToken token in json.Descendants())
                    {
                        JProperty property = token as JProperty;
                        if (property != null)
                        {
                            m = rgx.Match(property.Name);
                            if (m.Success)
                            {
                                res = int.Parse(m.Groups[1].Value);
                                url = (string)json[string.Format("url{0}", res)];
                                video.PlaybackOptions.Add(string.Format("{0}p",res),url);
                                if (max < res)
                                {
                                    max = res;
                                    bestUrl = url;
                                }
                            }
                        }
                    }
                }
            }
            if (inPlaylist) video.PlaybackOptions.Clear();
            return new List<string>() { bestUrl };
        }
        #endregion
    }
}
