using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class SVTPlayUtil : SiteUtilBase
    {
        public enum JaNej { Ja, Nej };
        protected const string _oppetArkiv = "Öppet arkiv";
        protected const string _programA_O = "Program A-Ö";
        protected string nextPageUrl = "";

        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string oppetArkivListUrl;
        [Category("OnlineVideosConfiguration"), Description("hdcore value for manifest-urls")]
        protected string hdcore;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Hämta undertexter"), Description("Välj om du vill hämta eventuella undertexter")]
        protected JaNej retrieveSubtitles = JaNej.Ja;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Gruppera efter begynnelsebokstav"), Description("Välj om du vill gruppera programlistningar efter begynnelsebokstav eller inte.")]
        protected JaNej splitByLetter = JaNej.Ja;

        protected bool RetrieveSubtitles
        {
            get { return retrieveSubtitles == JaNej.Ja; }
        }

        protected bool SplitByLetter
        {
            get { return splitByLetter == JaNej.Ja; }
        }

        #region category

        private RssLink DiscoverCategoryFromArticle(HtmlNode article)
        {
            RssLink cat = new RssLink();
            cat.Description = HttpUtility.HtmlDecode(article.GetAttributeValue("data-description", ""));
            HtmlNode a = article.Descendants("a").First();
            Uri uri = new Uri(new Uri(baseUrl), a.GetAttributeValue("href", ""));
            cat.Url = uri.ToString();
            uri = new Uri(new Uri(baseUrl), a.Descendants("img").First().GetAttributeValue("src", ""));
            cat.Thumb = uri.ToString();
            HtmlNode h2 = a.SelectSingleNode("span/h2");
            if (h2 != null)
                cat.Name = HttpUtility.HtmlDecode(h2.InnerText);
            else
                cat.Name = HttpUtility.HtmlDecode(article.GetAttributeValue("data-title", ""));
            if (cat.Name.ToLower().Contains("oppetarkiv"))
            {
                cat.Name = _oppetArkiv;
                cat.Url = oppetArkivListUrl;
            }
            return cat;
        }

        private List<Category> DiscoverProgramAOCategories(HtmlNode htmlNode, Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode div = htmlNode.Descendants("div").First(d => d.GetAttributeValue("class", "") == "play_alphabetic-list-titles");
            foreach (HtmlNode alphaLi in div.SelectNodes("ul/li"))
            {
                Category alphaCat = new Category() { Name = HttpUtility.HtmlDecode(alphaLi.SelectSingleNode("div/h3").InnerText), SubCategories = new List<Category>(), HasSubCategories = true, ParentCategory = parentCategory };
                HtmlNodeCollection programs = alphaLi.SelectNodes("div/ul/li");
                if (programs != null)
                {
                    foreach (HtmlNode program in programs)
                    {
                        HtmlNode a = program.SelectSingleNode("a");
                        Uri uri = new Uri(new Uri(baseUrl), a.GetAttributeValue("href", ""));
                        RssLink programCat = new RssLink() { Name = HttpUtility.HtmlDecode(a.InnerText), Url = uri.ToString(), HasSubCategories = true, ParentCategory = SplitByLetter ? alphaCat : parentCategory };
                        if (SplitByLetter)
                            alphaCat.SubCategories.Add(programCat);
                        else
                            categories.Add(programCat);
                    }
                }
                if (SplitByLetter && alphaCat.SubCategories.Count > 0)
                {
                    alphaCat.SubCategoriesDiscovered = true;
                    categories.Add(alphaCat);
                }
            }
            return categories;
        }

        private List<Category> DiscoverOppetArkivCategories(HtmlNode htmlNode, Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode div = htmlNode.Descendants("div").First(d => d.GetAttributeValue("role", "") == "main");
            foreach (HtmlNode alphaSec in div.SelectNodes("section"))
            {
                Category alphaCat = new Category() { Name = HttpUtility.HtmlDecode(alphaSec.SelectSingleNode("h2/a").InnerText), SubCategories = new List<Category>(), HasSubCategories = true, ParentCategory = parentCategory };
                HtmlNodeCollection programs = alphaSec.SelectNodes("ul/li");
                if (programs != null)
                {
                    foreach (HtmlNode program in programs)
                    {
                        HtmlNode a = program.SelectSingleNode("a");
                        Uri uri = new Uri(new Uri(baseUrl), a.GetAttributeValue("href", ""));
                        RssLink programCat = new RssLink() { Name = HttpUtility.HtmlDecode(a.InnerText), Url = uri.ToString(), HasSubCategories = false, ParentCategory = SplitByLetter ? alphaCat : parentCategory };
                        if (SplitByLetter)
                            alphaCat.SubCategories.Add(programCat);
                        else
                            categories.Add(programCat);
                    }
                }
                if (SplitByLetter && alphaCat.SubCategories.Count > 0)
                {
                    alphaCat.SubCategoriesDiscovered = true;
                    categories.Add(alphaCat);
                }
            }
            return categories;
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.First(c => c.Name == _programA_O).HasSubCategories = true;
            HtmlNode htmlNode = GetWebData<HtmlDocument>(baseUrl).DocumentNode;
            foreach (HtmlNode section in htmlNode.Descendants("section").Where(n => n.GetAttributeValue("class", "").Contains("play_js-hovered-list")))
            {
                HtmlNode div = section.SelectSingleNode("div");
                RssLink cat = new RssLink();
                cat.Name = div.SelectSingleNode("div/h1").InnerText;
                cat.HasSubCategories = div.GetAttributeValue("id", "") == "categories";
                if (cat.HasSubCategories)
                {
                    cat.SubCategories = new List<Category>();
                    foreach (HtmlNode article in section.Descendants("article"))
                    {
                        Category subCat = DiscoverCategoryFromArticle(article);
                        subCat.HasSubCategories = true;
                        subCat.ParentCategory = cat;
                        cat.SubCategories.Add(subCat);
                    }
                    cat.SubCategoriesDiscovered = cat.SubCategories.Count > 0;
                }
                else
                {
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (HtmlNode article in section.Descendants("article"))
                    {
                        videos.Add(getVideoFromArticle(article));
                    }
                    cat.Other = videos;
                    cat.EstimatedVideoCount = (uint)videos.Count;
                }
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 1;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode htmlNode = GetWebData<HtmlDocument>((parentCategory as RssLink).Url).DocumentNode;
            if (parentCategory.Name == _programA_O)
            {
                categories = DiscoverProgramAOCategories(htmlNode, parentCategory);
            }
            else if (parentCategory.Name == _oppetArkiv)
            {
                categories = DiscoverOppetArkivCategories(htmlNode, parentCategory);
            }
            else
            {
                IEnumerable<HtmlNode> categoryNodes = htmlNode.Descendants("li").Where(li => li.GetAttributeValue("class", "").Contains("play_category-tab"));
                if (categoryNodes == null || categoryNodes.Count() == 0)
                    categoryNodes = htmlNode.Descendants("li").Where(li => li.GetAttributeValue("class", "").Contains("play_list__item"));

                foreach (HtmlNode categoryNode in categoryNodes)
                {
                    string ariaControls = categoryNode.SelectSingleNode("a").GetAttributeValue("aria-controls", "");
                    HtmlNode div = htmlNode.SelectSingleNode("//div[@id = '" + ariaControls + "']");

                    RssLink category = new RssLink() { Name = categoryNode.SelectSingleNode("a/span").InnerText, ParentCategory = parentCategory };
                    if (category.Name == _programA_O)
                    {
                        IEnumerable<HtmlNode> alphaDivs = div.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "play_alphabetic-letter playx_clearfix");
                        bool split = SplitByLetter && alphaDivs != null && alphaDivs.Count() > 0;
                        category.HasSubCategories = true;
                        category.SubCategories = new List<Category>();
                        if (split)
                        {
                            foreach (HtmlNode alphaDiv in alphaDivs)
                            {
                                Category alphaCat = new Category() { Name = HttpUtility.HtmlDecode(alphaDiv.SelectSingleNode("h3").InnerText), ParentCategory = category, HasSubCategories = true, SubCategories = new List<Category>() };
                                foreach (HtmlNode article in alphaDiv.Descendants("article"))
                                {
                                    RssLink subCat = DiscoverCategoryFromArticle(article);
                                    subCat.HasSubCategories = true;
                                    subCat.ParentCategory = alphaCat;
                                    alphaCat.SubCategories.Add(subCat);
                                }
                                alphaCat.SubCategoriesDiscovered = alphaCat.SubCategories.Count > 0;
                                category.SubCategories.Add(alphaCat);
                            }

                        }
                        else
                        {
                            foreach (HtmlNode article in div.Descendants("article"))
                            {
                                RssLink subCat = DiscoverCategoryFromArticle(article);
                                subCat.HasSubCategories = true;
                                subCat.ParentCategory = category;
                                category.SubCategories.Add(subCat);
                            }
                        }
                        category.SubCategoriesDiscovered = category.SubCategories.Count > 0;
                    }
                    else
                    {
                        category.HasSubCategories = false;
                        List<VideoInfo> videos = new List<VideoInfo>();
                        foreach (HtmlNode article in div.Descendants("article"))
                        {
                            videos.Add(getVideoFromArticle(article));
                        }
                        category.Other = videos;
                        category.EstimatedVideoCount = (uint)videos.Count;
                    }
                    categories.Add(category);
                }
            }
            parentCategory.SubCategories = categories;
            parentCategory.SubCategoriesDiscovered = categories.Count > 0;
            return categories.Count;
        }

        #endregion

        #region video

        private VideoInfo getVideoFromArticle(HtmlNode article)
        {
            VideoInfo video = new VideoInfo();
            HtmlNode playLinkSubNode = article.Descendants("span").FirstOrDefault(s => s.GetAttributeValue("class", "") == "play-link-sub");
            string title = article.GetAttributeValue("data-title", "");
            if (playLinkSubNode != null)
            {
                string playLinkSub = playLinkSubNode.InnerText.Trim().Replace('\n', ' ');
                if (playLinkSub != "" && !title.Contains(playLinkSub))
                {
                    title = playLinkSub + " - " + title;
                }
            }
            video.Title = title;
            video.Description = article.GetAttributeValue("data-description", "");
            video.Airdate = HttpUtility.HtmlDecode(article.GetAttributeValue("data-broadcasted", ""));
            video.Length = article.GetAttributeValue("data-length", "");
            HtmlNode a = article.SelectSingleNode("div/a");
            Uri uri = new Uri(new Uri(baseUrl), a.GetAttributeValue("href", ""));
            video.VideoUrl = uri.ToString();
            video.ImageUrl = a.SelectSingleNode("div/img").GetAttributeValue("data-imagename", "");
            video.CleanDescriptionAndTitle();
            return video;
        }

        private List<VideoInfo> getOppetArkivVideoList(HtmlAgilityPack.HtmlNode node)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            var div = node.SelectSingleNode("//div[contains(@class,'svtGridBlock')]");
            foreach (var article in div.Elements("article"))
            {
                VideoInfo video = new VideoInfo();
                video.VideoUrl = article.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                if (!string.IsNullOrEmpty(video.VideoUrl))
                {
                    video.Title = HttpUtility.HtmlDecode((article.Descendants("a").Select(a => a.GetAttributeValue("title", "")).FirstOrDefault() ?? "").Trim().Replace('\n', ' '));
                    video.ImageUrl = article.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                    video.Airdate = article.Descendants("time").Select(t => t.GetAttributeValue("datetime", "")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(video.Airdate)) video.Airdate = DateTime.Parse(video.Airdate).ToString("d", OnlineVideoSettings.Instance.Locale);
                    videoList.Add(video);
                }
            }
            return videoList;
        }

        private void getNextPageVideosUrl(HtmlAgilityPack.HtmlNode node)
        {
            HasNextPage = false;
            nextPageUrl = "";
            var a_o_buttons = node.SelectNodes("//a[contains(@class, 'svtoa-button')]");
            if (a_o_buttons != null)
            {
                var a_o_next_button = a_o_buttons.Where(a => (a.InnerText ?? "").Contains("Visa fler")).FirstOrDefault();
                if (a_o_next_button != null)
                {
                    nextPageUrl = a_o_next_button.GetAttributeValue("href", "");
                    nextPageUrl = HttpUtility.UrlDecode(nextPageUrl);
                    nextPageUrl = HttpUtility.HtmlDecode(nextPageUrl); //Some urls come html encoded
                    HasNextPage = true;
                }
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            HasNextPage = false;
            if (!string.IsNullOrEmpty(nextPageUrl))
            {
                HtmlNode htmlNode = GetWebData<HtmlDocument>(nextPageUrl).DocumentNode;
                getNextPageVideosUrl(htmlNode);
                return getOppetArkivVideoList(htmlNode);
            }
            return new List<VideoInfo>();
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category.Other is List<VideoInfo>)
            {
                return category.Other as List<VideoInfo>;
            }
            else
            {
                var htmlNode = GetWebData<HtmlDocument>((category as RssLink).Url).DocumentNode;
                getNextPageVideosUrl(htmlNode);
                return getOppetArkivVideoList(htmlNode);
            }
        }

        public override string getUrl(VideoInfo video)
        {
            string url = "";
            JToken videoToken = GetWebData<JObject>(video.VideoUrl + "?output=json")["video"];
            if (RetrieveSubtitles)
            {
                try
                {
                    var subtitleReferences = videoToken["subtitleReferences"].Where(sr => ((string)sr["url"] ?? "").EndsWith("srt"));
                    if (subtitleReferences != null && subtitleReferences.Count() > 0)
                    {
                        url = (string)subtitleReferences.First()["url"];
                        if (!string.IsNullOrEmpty(url))
                        {
                            video.SubtitleText = CleanSubtitle(GetWebData(url));
                        }
                    }
                }
                catch { }
            }
            JToken videoReference = videoToken["videoReferences"].FirstOrDefault(vr => (string)vr["playerType"] == "flash" && !string.IsNullOrEmpty((string)vr["url"]));
            if (videoReference == null)
            {
                url = "";
            }
            else
            {
                Boolean live = false;
                JValue liveVal = (JValue)videoToken["live"];
                if (liveVal != null)
                    live = liveVal.Value<bool>();
                url = (string)videoReference["url"] + "?hdcore=" + hdcore + "&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12);
                url = new MPUrlSourceFilter.AfhsManifestUrl(url)
                {
                    LiveStream = live,
                    Referer = video.VideoUrl,

                    ReceiveDataTimeout = live ? MPUrlSourceFilter.AfhsManifestUrl.DefaultReceiveDataTimeout * 2 : MPUrlSourceFilter.AfhsManifestUrl.DefaultReceiveDataTimeout
                }.ToString();
            }
            return url;
        }

        #endregion

        #region search

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
            string[] subcats = { "search-categories", "search-titles", "" };
            HtmlNode htmlNode = GetWebData<HtmlDocument>(baseUrl + "sok?q=" + HttpUtility.UrlEncode(query)).DocumentNode;
            foreach (HtmlNode section in htmlNode.Descendants("section").Where(n => n.GetAttributeValue("class", "").Contains("play_js-hovered-list")))
            {
                HtmlNode div = section.SelectSingleNode("div");
                RssLink cat = new RssLink();
                cat.Name = div.SelectSingleNode("div/h1").InnerText;
                if (cat.Name.ToLower().Contains("oppetarkiv"))
                    cat.Name = _oppetArkiv;
                cat.HasSubCategories = subcats.Any(c => c == div.GetAttributeValue("id", ""));
                if (cat.HasSubCategories)
                {
                    cat.SubCategories = new List<Category>();
                    foreach (HtmlNode article in section.Descendants("article"))
                    {
                        Category subCat = DiscoverCategoryFromArticle(article);
                        subCat.HasSubCategories = true;
                        subCat.ParentCategory = cat;
                        cat.SubCategories.Add(subCat);
                    }
                    cat.SubCategoriesDiscovered = cat.SubCategories.Count > 0;
                }
                else
                {
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (HtmlNode article in section.Descendants("article"))
                    {
                        videos.Add(getVideoFromArticle(article));
                    }
                    cat.Other = videos;
                    cat.EstimatedVideoCount = (uint)videos.Count;
                }
                results.Add(cat);
            }
            return results;
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            //Extension always .f4m
            return OnlineVideos.Utils.GetSaveFilename(video.Title) + ".f4m";
        }

        #endregion

        #region subtitle

        string CleanSubtitle(string subtitle)
        {
            // For some reason the time codes in the subtitles from Öppet arkiv starts @ 10 hours. replacing first number in the
            // hour position with 0. Hope and pray there will not be any shows with 10+ h playtime...
            // Remove all trailing stuff, ie in 00:45:21.960 --> 00:45:25.400 A:end L:82%
            Regex rgx = new Regex(@"\d(\d:\d\d:\d\d\.\d\d\d)\s*-->\s*\d(\d:\d\d:\d\d\.\d\d\d).*$", RegexOptions.Multiline);
            subtitle = rgx.Replace(subtitle, new MatchEvaluator((Match m) =>
            {
                return "0" + m.Groups[1].Value + " --> 0" + m.Groups[2].Value + "\r";
            }));

            // Removes color codes, ie <36>....</36>. Can't use XDocument to remove all tags since <36> is invalid xml (parse throws)
            // I haven't noticed any other tags or strange WebSrt stuff, so keeping it simple. 
            rgx = new Regex(@"</{0,1}\d\d>");
            return rgx.Replace(subtitle, string.Empty);
        }

        #endregion
    }
}
