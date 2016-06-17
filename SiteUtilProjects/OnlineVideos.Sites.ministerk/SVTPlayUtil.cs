using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Hoster;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class SvtPlayUtil : LatestVideosSiteUtilBase
    {

        #region Config

        public enum JaNej { Ja, Nej };
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Gruppera efter begynnelsebokstav"), Description("Välj om du vill gruppera programlistningar efter begynnelsebokstav eller inte.")]
        protected JaNej splitByLetter = JaNej.Nej;

        protected bool SplitByLetter
        {
            get { return splitByLetter == JaNej.Ja; }
        }


        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            Category program = new Category() { Name = "Program A-Ö", HasSubCategories = true };
            program.Other = (Func<List<Category>>)(() => GetProgramAOListingCategories(program));
            Settings.Categories.Add(program);

            Category kanaler = new Category() { Name = "Kanaler", HasSubCategories = false };
            List<VideoInfo> kanalerVideos = new List<VideoInfo>()
                {
                    new VideoInfo() {Title = "SVT1"},
                    new VideoInfo() {Title = "SVT2"},
                    new VideoInfo() {Title = "Barnkanalen"},
                    new VideoInfo() {Title = "SVT24"},
                    new VideoInfo() {Title = "Kunskapskanalen"}
                };
            kanalerVideos.ForEach(v =>
            {
                v.VideoUrl = string.Format("http://www.svtplay.se/kanaler/{0}", v.Title.ToLower());
            });
            kanaler.Other = kanalerVideos;
            Settings.Categories.Add(kanaler);

            RssLink popular = new RssLink() { Name = "Populärast just nu", Url = "http://www.svtplay.se/populara?embed=true&sida={0}", HasSubCategories = false };
            Settings.Categories.Add(popular);

            RssLink senaste = new RssLink() { Name = "Senaste program", Url = "http://www.svtplay.se/senaste?embed=true&sida={0}", HasSubCategories = false };
            Settings.Categories.Add(senaste);

            RssLink sistaChansen = new RssLink() { Name = "Sista chansen", Url = "http://www.svtplay.se/sista-chansen?embed=true&sida={0}", HasSubCategories = false };
            Settings.Categories.Add(sistaChansen);

            RssLink live = new RssLink() { Name = "Live", Url = "http://www.svtplay.se/live?embed=true&sida={0}", HasSubCategories = false };
            Settings.Categories.Add(live);

            Category genrer = new Category() { Name = "Genrer", HasSubCategories = true };
            genrer.Other = (Func<List<Category>>)(() => GetGenrerCategories(genrer));
            Settings.Categories.Add(genrer);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        private List<Category> GetGenrerCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            HtmlNode htmlNode = GetWebData<HtmlDocument>("http://www.svtplay.se/program").DocumentNode;
            HtmlNode div = htmlNode.SelectSingleNode("//div[contains(@class,'lp_clust')]");
            foreach (HtmlNode article in div.SelectNodes("article"))
            {
                RssLink cat = new RssLink();
                cat.Url = article.SelectSingleNode(".//a").GetAttributeValue("href", "");
                cat.Thumb = article.SelectSingleNode(".//img").GetAttributeValue("src", "");
                cat.Name = HttpUtility.HtmlDecode(article.SelectSingleNode(".//h2").InnerText.Trim());
                cat.ParentCategory = parentCategory;
                cat.HasSubCategories = true;
                if (cat.Url.Contains("genre/"))
                {
                    cat.Url = cat.Url.Replace("genre/", "").Replace("/", "");
                    cat.Other = (Func<List<Category>>)(() => GetTagCategories(cat));
                }
                else if (!cat.Url.Contains("oppetarkiv"))
                    cat.Other = (Func<List<Category>>)(() => GetGenreSubCategories(cat));
                else
                {
                    cat.Other = (Func<List<Category>>)(() => GetOppetArkivCategories(cat));
                    cat.Name = "Öppet arkiv";
                }
                cats.Add(cat);
            }
            Category allaGenrer = new Category() { Name = "Visa alla genrer", HasSubCategories = true, ParentCategory = parentCategory };
            allaGenrer.Other = (Func<List<Category>>)(() => GetTagsCategories(allaGenrer));
            cats.Add(allaGenrer);

            return cats;
        }

        private List<Category> GetOppetArkivCategories(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode htmlNode = GetWebData<HtmlDocument>("http://www.oppetarkiv.se/program").DocumentNode;
            HtmlNode div = htmlNode.Descendants("div").First(d => d.GetAttributeValue("role", "") == "main");
            foreach (HtmlNode alphaSec in div.SelectNodes("section"))
            {
                HtmlNodeCollection programs = alphaSec.SelectNodes("ul/li");
                if (programs != null)
                {
                    foreach (HtmlNode program in programs)
                    {
                        HtmlNode a = program.SelectSingleNode("a");
                        string url = "http://www.oppetarkiv.se" + a.GetAttributeValue("href", "") + "?sida={0}&sort=tid_stigande&embed=true";
                        RssLink programCat = new RssLink() { Name = HttpUtility.HtmlDecode(a.InnerText), Url = url, HasSubCategories = false, ParentCategory = parentCategory };
                        categories.Add(programCat);
                    }
                }
            }
            if (SplitByLetter) categories = SplitByLetterCategories(categories);
            return categories;
        }

        private List<Category> SplitByLetterCategories(List<Category> categories)
        {
            List<Category> alphaCats = new List<Category>();
            if (categories != null && categories.Count > 0)
            {
                Category parentCategory = categories.First().ParentCategory;
                string[] alphaz = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "Å", "Ä", "Ö" };
                foreach (string alpha in alphaz)
                {
                    Category cat = new Category() { Name = alpha, HasSubCategories = true, SubCategoriesDiscovered = true, ParentCategory = parentCategory, SubCategories = new List<Category>() };
                    cat.SubCategories.AddRange(categories.Where(c => c.Name.ToUpperInvariant().StartsWith(alpha)));
                    if (cat.SubCategories.Count > 0)
                    {
                        cat.SubCategories.ForEach(c => c.ParentCategory = cat);
                        alphaCats.Add(cat);
                    }
                }
                Category noCat = new Category() { Name = "0-9", HasSubCategories = true, SubCategoriesDiscovered = true, ParentCategory = parentCategory, SubCategories = new List<Category>() };
                noCat.SubCategories.AddRange(categories.Where(c => !alphaz.Contains(c.Name.ToUpperInvariant().Substring(0, 1))));
                if (noCat.SubCategories.Count > 0)
                {
                    noCat.SubCategories.ForEach(c => c.ParentCategory = noCat);
                    alphaCats.Add(noCat);
                }
            }
            return alphaCats;
        }

        private List<Category> GetGenreSubCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string url = (parentCategory as RssLink).Url;

            RssLink program = new RssLink() { Name = "Program A-Ö", Url = url, HasSubCategories = true, ParentCategory = parentCategory };
            program.Other = (Func<List<Category>>)(() => GetGenreProgramAOListingCategories(program));
            cats.Add(program);

            RssLink popular = new RssLink() { Name = "Populäraste", Url = "http://www.svtplay.se/ajax/" + url + "/populara?sida={0}", HasSubCategories = false, ParentCategory = parentCategory };
            cats.Add(popular);

            RssLink senaste = new RssLink() { Name = "Senaste", Url = "http://www.svtplay.se/ajax/" + url + "/senaste?sida={0}", HasSubCategories = false, ParentCategory = parentCategory };
            cats.Add(senaste);

            RssLink sista = new RssLink() { Name = "Sista chansen", Url = "http://www.svtplay.se/ajax/" + url + "/sista-chansen?sida={0}", HasSubCategories = false, ParentCategory = parentCategory };
            cats.Add(sista);

            RssLink klipp = new RssLink() { Name = "Klipp", Url = "http://www.svtplay.se/ajax/" + url + "/klipp?sida={0}", HasSubCategories = false, ParentCategory = parentCategory };
            cats.Add(klipp);

            RssLink live = new RssLink() { Name = "Live", Url = "http://www.svtplay.se/ajax/" + url + "/live?sida={0}", HasSubCategories = false, ParentCategory = parentCategory };
            cats.Add(live);

            return cats;
        }

        private RssLink DiscoverCategoryFromArticle(HtmlNode article)
        {
            RssLink cat = new RssLink();
            cat.Description = HttpUtility.HtmlDecode(article.GetAttributeValue("data-description", ""));
            HtmlNode a = article.Descendants("a").First();
            cat.Url = a.GetAttributeValue("href", "");
            IEnumerable<HtmlNode> imgs = a.Descendants("img");
            if (imgs != null && imgs.Count() > 0)
            {
                Uri uri = new Uri(new Uri("http://www.svtplay.se"), a.Descendants("img").First().GetAttributeValue("src", ""));
                cat.Thumb = uri.ToString();
            }
            HtmlNode fcap = a.SelectSingleNode(".//figcaption");
            if (fcap != null)
                cat.Name = HttpUtility.HtmlDecode(fcap.InnerText);
            else
                cat.Name = HttpUtility.HtmlDecode(article.GetAttributeValue("data-title", ""));
            cat.Name = (cat.Name ?? "").Trim();
            return cat;
        }

        private List<Category> GetGenreProgramAOListingCategories(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode htmlNode = GetWebData<HtmlDocument>("http://www.svtplay.se/" + (parentCategory as RssLink).Url + "?tab=titlar").DocumentNode;
            HtmlNode div = htmlNode.SelectSingleNode("//div[@id = 'playJs-alphabetic-list']");

            foreach (HtmlNode article in div.Descendants("article"))
            {
                RssLink subCat = DiscoverCategoryFromArticle(article);
                subCat.ParentCategory = parentCategory;
                if (subCat.Url.StartsWith("/video"))
                {
                    subCat.HasSubCategories = false;
                    List<VideoInfo> videos = new List<VideoInfo>();
                    VideoInfo video = new VideoInfo();
                    video.Title = subCat.Name;
                    video.Description = subCat.Description;
                    video.VideoUrl = "http://www.svtplay.se" + subCat.Url;
                    video.Thumb = subCat.Thumb;
                    videos.Add(video);
                    subCat.Other = videos;
                   
                }
                else
                {
                    subCat.HasSubCategories = true;
                    subCat.Other = (Func<List<Category>>)(() => GetProgramCategories(subCat));
                }
                categories.Add(subCat);
            }
            if (SplitByLetter) categories = SplitByLetterCategories(categories);
            return categories;
        }

        private List<Category> GetTagsCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            JObject json = GetWebData<JObject>("http://www.svtplay.se/api/cluster_page;cluster=sport");
            foreach (JToken tagGroups in json["allTags"])
            {
                foreach (JArray tagGroup in tagGroups)
                {
                    foreach (JArray a in tagGroup)
                    {
                        foreach (JToken item in a.Where(i => i["facet"].Value<string>() == "videoFacet"))
                        {
                            RssLink cat = new RssLink();
                            cat.ParentCategory = parentCategory;
                            cat.HasSubCategories = true;
                            cat.Name = item["name"].Value<string>();
                            cat.Url = item["term"].Value<string>();
                            cat.Other = (Func<List<Category>>)(() => GetTagCategories(cat));
                            cats.Add(cat);
                        }
                    }
                }
            }
            return cats;
        }

        private List<Category> GetTagCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            JObject json = GetWebData<JObject>("http://www.svtplay.se/api/cluster_page;cluster=" + (parentCategory as RssLink).Url);
            Category contents = new Category() { Name = "Program A-Ö", ParentCategory = parentCategory, HasSubCategories = true, SubCategoriesDiscovered = true };
            Category program = new Category() { Name = "Hela program", ParentCategory = parentCategory };
            List<VideoInfo> programs = new List<VideoInfo>();
            List<Category> categories = new List<Category>();
            foreach (JToken prog in json["contents"].Value<JArray>().Where(p => p["contentUrl"] != null && p["title"] != null))
            {
                if (prog["contentType"].Value<string>() == "titelsida")
                {
                    RssLink cat = new RssLink() { HasSubCategories = true, ParentCategory = contents };
                    cat.Name = prog["title"].Value<string>();
                    cat.Url = prog["contentUrl"].Value<string>();
                    if (prog["description"] != null)
                        cat.Description = prog["description"].Value<string>();
                    if (prog["thumbnailLarge"] != null)
                    {
                        cat.Thumb = prog["thumbnailLarge"].Value<string>();
                        cat.Thumb = cat.Thumb.StartsWith("//") ? ("http:" + cat.Thumb) : cat.Thumb;
                    }
                    cat.Other = (Func<List<Category>>)(() => GetProgramCategories(cat));
                    categories.Add(cat);
                }
                else
                {
                    VideoInfo video = new VideoInfo();
                    string programTitle = prog["programTitle"] != null ? prog["programTitle"].Value<string>() : "";
                    string title = prog["title"].Value<string>();
                    if (!programTitle.Contains(title))
                        video.Title = programTitle + " " + title;
                    else
                        video.Title = title;
                    video.VideoUrl = "http://www.svtplay.se" + prog["contentUrl"].Value<string>();
                    if (prog["description"] != null)
                        video.Description = prog["description"].Value<string>();
                    if (prog["thumbnailLarge"] != null)
                    {
                        video.Thumb = prog["thumbnailLarge"].Value<string>();
                        video.Thumb = video.Thumb.StartsWith("//") ? ("http:" + video.Thumb) : video.Thumb;
                    }
                    programs.Add(video);
                }
            }
            if (SplitByLetter) categories = SplitByLetterCategories(categories);
            contents.SubCategories = categories;
            if (categories != null && categories.Count > 0)
                cats.Add(contents);
            program.Other = programs;
            if (programs.Count > 0)
                cats.Add(program);

            Category clipsCat = new Category() { Name = "Klipp", ParentCategory = parentCategory };
            List<VideoInfo> clips = new List<VideoInfo>();
            foreach (JToken clip in json["clips"].Value<JArray>().Where(p => p["contentUrl"] != null && p["title"] != null))
            {
                VideoInfo video = new VideoInfo();
                video.Title = (clip["programTitle"] != null ? clip["programTitle"].Value<string>() + " " : "") + clip["title"].Value<string>();
                video.VideoUrl = "http://www.svtplay.se" + clip["contentUrl"].Value<string>();
                if (clip["description"] != null)
                    video.Description = clip["description"].Value<string>();
                if (clip["thumbnailLarge"] != null)
                {
                    video.Thumb = clip["thumbnailLarge"].Value<string>();
                    video.Thumb = video.Thumb.StartsWith("//") ? ("http:" + video.Thumb) : video.Thumb;
                }
                clips.Add(video);
            }
            clipsCat.Other = clips;

            if (clips.Count > 0)
                cats.Add(clipsCat);
            return cats;
        }

        private List<Category> GetProgramAOListingCategories(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            HtmlNode htmlNode = GetWebData<HtmlDocument>("http://www.svtplay.se/program").DocumentNode;
            IEnumerable<HtmlNode> alphabetList = htmlNode.Descendants("div").Where(d => d.GetAttributeValue("class", "").StartsWith("play_alphabetic-list"));
            foreach (HtmlNode alphaLi in alphabetList)
            {
                HtmlNodeCollection programs = alphaLi.SelectNodes("ul/li");
                if (programs != null)
                {
                    foreach (HtmlNode program in programs)
                    {
                        HtmlNode a = program.SelectSingleNode("a");
                        RssLink programCat = new RssLink() { Name = HttpUtility.HtmlDecode(a.InnerText), Url = a.GetAttributeValue("href", ""), HasSubCategories = true, ParentCategory = parentCategory };
                        programCat.Other = (Func<List<Category>>)(() => GetProgramCategories(programCat));
                        categories.Add(programCat);
                    }
                }
            }
            if (SplitByLetter) categories = SplitByLetterCategories(categories);
            return categories;
        }

        private List<Category> GetProgramCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            RssLink programs = new RssLink() { Name = "Hela program", ParentCategory = parentCategory, HasSubCategories = false, Url = "http://www.svtplay.se" + (parentCategory as RssLink).Url + "/hela-program?embed=true&sida={0}" };
            if (GetVideos(programs).Count > 0)
                cats.Add(programs);
            RssLink clips = new RssLink() { Name = "Klipp", ParentCategory = parentCategory, HasSubCategories = false, Url = "http://www.svtplay.se" + (parentCategory as RssLink).Url + "/klipp?embed=true&sida={0}" };
            if (GetVideos(clips).Count > 0)
                cats.Add(clips);
            return cats;
        }

        #endregion

        #region Videos

        private string currentVideosUrl = "{0}";
        private uint currentVideosPage = 0;
        private bool isOppetArkiv = false;
        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
            {
                HasNextPage = false;
                return category.Other as List<VideoInfo>;
            }
            currentVideosUrl = (category as RssLink).Url;
            currentVideosPage = 1;
            isOppetArkiv = currentVideosUrl.ToLower().Contains("oppetarkiv");
            return GetVideos();
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentVideosPage++;
            return GetVideos();
        }

        private List<VideoInfo> GetVideos(HtmlNode htmlNode)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (HtmlNode article in htmlNode.Descendants("article"))
            {
                VideoInfo video = new VideoInfo();
               // video.Length = HttpUtility.HtmlDecode(article.GetAttributeValue("data-length", ""));
               // video.Airdate = HttpUtility.HtmlDecode(article.GetAttributeValue("data-broadcasted", ""));
               // video.Description = HttpUtility.HtmlDecode(article.GetAttributeValue("data-description", ""));
                HtmlNode titleTextSpan = article.SelectSingleNode(".//h3");
                HtmlNode titleSubTextSpan = article.SelectSingleNode(".//p[contains(@class,'play_videolist-element__subtext')]");
                string title = titleTextSpan != null ? titleTextSpan.InnerText : "";
                string subText = titleSubTextSpan != null ? titleSubTextSpan.InnerText : "";
                title += " " + subText;
                title = Regex.Replace(title, "\\s", " ");
                title = Regex.Replace(title, "\\s+", " ");
                title = HttpUtility.HtmlDecode(title);
                //title = title.Replace("Längd: " + video.Length, "");
                video.Title = title;
                HtmlNode img = article.SelectSingleNode(".//img[contains(@class,'thumbnail-image')]");
                if (img != null)
                {
                    video.Thumb = img.GetAttributeValue("src", "");
                    if (video.Thumb.StartsWith("//"))
                        video.Thumb = "http:" + video.Thumb;
                }
                HtmlNode anchor = article.SelectSingleNode(".//a");
                video.VideoUrl = "http://www.svtplay.se" + anchor.GetAttributeValue("href", "");
                videos.Add(video);
            }
            return videos;
        }

        private List<VideoInfo> GetOppetArkivVideos(HtmlNode htmlNode)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            foreach (var article in htmlNode.Descendants("article"))
            {
                VideoInfo video = new VideoInfo();
                video.VideoUrl = article.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                Uri result;
                if (!Uri.TryCreate(video.VideoUrl, UriKind.Absolute, out result))
                    Uri.TryCreate(new Uri("http://www.oppetarkiv.se/"), video.VideoUrl, out result);
                video.VideoUrl = result.ToString();
                if (!string.IsNullOrEmpty(video.VideoUrl))
                {
                    HtmlNode h1 = article.SelectSingleNode(".//h1");
                    if (h1 != null)
                    {
                        video.Title = HttpUtility.HtmlDecode(h1.InnerText).Replace(" - ", " ").Trim() + " ";
                    }
                    video.Title += HttpUtility.HtmlDecode((article.Descendants("a").Select(a => a.GetAttributeValue("title", "")).FirstOrDefault() ?? "").Trim().Replace('\n', ' '));
                    video.Thumb = (article.SelectSingleNode(".//noscript/img") != null) ? article.SelectSingleNode(".//noscript/img").GetAttributeValue("src", "") : "";
                    if (video.Thumb.StartsWith("//")) video.Thumb = "http:" + video.Thumb;
                    video.Airdate = article.Descendants("time").Select(t => t.GetAttributeValue("datetime", "")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(video.Airdate)) video.Airdate = DateTime.Parse(video.Airdate).ToString("d", OnlineVideoSettings.Instance.Locale);
                    videoList.Add(video);
                }
            }
            return videoList;
        }

        private List<VideoInfo> GetVideos()
        {
            HtmlNode htmlNode = GetWebData<HtmlDocument>(string.Format(currentVideosUrl, currentVideosPage)).DocumentNode;
            List<VideoInfo> videos = isOppetArkiv ? GetOppetArkivVideos(htmlNode) : GetVideos(htmlNode);
            if (isOppetArkiv)
                HasNextPage = videos.Count > 0 && htmlNode.SelectNodes("//div[contains(@class,'svtoa_js-pagination-btn')]") != null && htmlNode.SelectNodes("//div[contains(@class,'svtoa_js-pagination-btn')]").Any();
            else
                HasNextPage = (!currentVideosUrl.Contains("/live?")) && videos.Count > 0 && htmlNode.SelectNodes("//div[contains(@class,'play_gridpage__pagination')]") != null && htmlNode.SelectNodes("//div[contains(@class,'play_gridpage__pagination')]").Any();
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            HosterBase svtPlay = HosterFactory.GetHoster("SVTPlay");
            string url = svtPlay.GetVideoUrl(video.VideoUrl);
            if (svtPlay is ISubtitle)
                video.SubtitleText = (svtPlay as ISubtitle).SubtitleText;
            return url;
        }

        #endregion

        #region GetFileNameForDownload

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            //Extension always .f4m
            ITrackingInfo ti = GetTrackingInfo(video);
            if (ti != null && ti.VideoKind == VideoKind.TvSeries)
            {
                return Helpers.FileUtils.GetSaveFilename(ti.Title) + ".S" + (ti.Season > 9 ? ti.Season.ToString() : "0" + ti.Season.ToString()) + "E" + (ti.Episode > 9 ? ti.Episode.ToString() : "0" + ti.Episode.ToString()) + ".f4m";
            }
            return Helpers.FileUtils.GetSaveFilename(video.Title) + ".f4m";
        }


        #endregion

        #region Tracking Info

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            Regex rgx = new Regex(@"(?<VideoKind>TvSeries)(?<Title>.*)? [Ss]äsong.*?(?<Season>\d+).*?[Aa]vsnitt.*?(?<Episode>\d+)");
            Match m = rgx.Match("TvSeries" + video.Title);
            ITrackingInfo ti = new TrackingInfo() { Regex = m };
            return ti;
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
            string data = GetWebData<string>("http://www.svtplay.se/sok?q=" + HttpUtility.UrlEncode(query));
            Regex rgx = new Regex(@"root\[""__svtplay""\] = (?<json>{.*?""plugins"":{}})");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                JObject json = JObject.Parse(m.Groups["json"].Value);
                JToken searchStore = json["context"]["dispatcher"]["stores"]["SearchStore"];
                JToken categories = searchStore["categories"];
                JToken titles = searchStore["titles"];
                JToken episodes = searchStore["episodes"];
                JToken clips = searchStore["clips"];
                JToken live = searchStore["live"];
                JToken openArchive = searchStore["openArchive"];

                if (categories != null && categories.Value<JArray>().Count() > 0)
                {
                    Category genrer = new Category() { Name = "Genrer", HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>()};
                    foreach (JToken cat in categories.Value<JArray>())
                    {
                        RssLink subCat = new RssLink();
                        subCat.HasSubCategories = true;
                        subCat.ParentCategory = genrer;
                        subCat.Name = cat["name"].Value<string>();
                        subCat.Thumb = cat["posterImageUrl"].Value<string>();
                        if (subCat.Thumb.StartsWith("/"))
                            subCat.Thumb = "http://www.svtplay.se" + subCat.Thumb;
                        if (cat["isTag"].Value<bool>())
                        {
                            subCat.Url = cat["urlPart"].Value<string>();
                            subCat.Other = (Func<List<Category>>)(() => GetTagCategories(subCat));
                        }
                        else
                        {
                            subCat.Url = cat["url"].Value<string>();
                            subCat.Other = (Func<List<Category>>)(() => GetGenreSubCategories(subCat));
                        }
                        genrer.SubCategories.Add(subCat);
                    }
                    results.Add(genrer);
                }
                if (titles != null && titles.Value<JArray>().Count() > 0)
                {
                    Category program = new Category() { Name = "Program", HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>() };
                    foreach (JToken cat in titles.Value<JArray>())
                    {
                        RssLink subCat = new RssLink();
                        subCat.HasSubCategories = true;
                        subCat.ParentCategory = program;
                        subCat.Name = cat["programTitle"].Value<string>();
                        subCat.Description = cat["description"].Value<string>();
                        subCat.Url = cat["contentUrl"].Value<string>();
                        subCat.Thumb = "http:" + cat["imageMedium"].Value<string>();
                        subCat.Other = (Func<List<Category>>)(() => GetProgramCategories(subCat));
                        program.SubCategories.Add(subCat);
                    }
                    results.Add(program);
                }
                if (episodes != null && episodes.Value<JArray>().Count() > 0)
                {
                    Category avsnitt = new Category() { Name = "Avsnitt", HasSubCategories = false };
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (JToken ep in episodes.Value<JArray>())
                    {
                        VideoInfo video = new VideoInfo();
                        string title = ep["programTitle"] != null ? ep["programTitle"].Value<string>() : "";
                        title += (ep["season"] != null && ep["season"].Value<int>() > 0 ? (" Säsong " + ep["season"].Value<int>() + " - ") : " ");
                        title += ep["title"] != null ? ep["title"].Value<string>() : "";
                        video.VideoUrl = "http://www.svtplay.se" + ep["contentUrl"].Value<string>();
                        video.Thumb = "http:" + ep["imageMedium"].Value<string>();
                        video.Title = title.Trim();
                        video.Description = ep["description"] != null ? ep["description"].Value<string>() : "";
                        videos.Add(video);
                    }
                    avsnitt.Other = videos;
                    results.Add(avsnitt);
                }
                if (live != null && live.Value<JArray>().Count() > 0)
                {
                    Category livesandningar = new Category() { Name = "Livesändningar", HasSubCategories = false };
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (JToken l in live.Value<JArray>())
                    {
                        VideoInfo video = new VideoInfo();
                        string title = l["programTitle"] != null ? l["programTitle"].Value<string>() : "";
                        title += " ";
                        title += l["title"] != null ? l["title"].Value<string>() : "";
                        video.VideoUrl = "http://www.svtplay.se" + l["contentUrl"].Value<string>();
                        video.Thumb = "http:" + l["imageMedium"].Value<string>();
                        video.Title = title.Trim();
                        video.Description = l["description"] != null ? l["description"].Value<string>() : "";
                        videos.Add(video);
                    }
                    livesandningar.Other = videos;
                    results.Add(livesandningar);
                }
                if (clips != null && clips.Value<JArray>().Count() > 0)
                {
                    Category klipp = new Category() { Name = "Klipp", HasSubCategories = false };
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (JToken clip in clips.Value<JArray>())
                    {
                        VideoInfo video = new VideoInfo();
                        string title = clip["title"] != null ? clip["title"].Value<string>() : "";
                        video.VideoUrl = "http://www.svtplay.se" + clip["contentUrl"].Value<string>();
                        video.Thumb = "http:" + clip["imageMedium"].Value<string>();
                        video.Title = title.Trim();
                        video.Description = clip["description"] != null ? clip["description"].Value<string>() : "";
                        videos.Add(video);
                    }
                    klipp.Other = videos;
                    results.Add(klipp);
                }
                if (openArchive != null && openArchive.Value<JArray>().Count() > 0)
                {
                    Category oppetArkiv = new Category() { Name = "Öppet arkiv", HasSubCategories = false };
                    List<VideoInfo> videos = new List<VideoInfo>();
                    foreach (JToken oa in openArchive.Value<JArray>())
                    {
                        VideoInfo video = new VideoInfo();
                        string title = oa["programTitle"] != null ? oa["programTitle"].Value<string>() : "";
                        title += " ";
                        title += oa["title"] != null ? oa["title"].Value<string>() : "";
                        video.VideoUrl = "http:" + oa["contentUrl"].Value<string>();
                        video.Thumb = "http:" + oa["imageMedium"].Value<string>();
                        video.Title = title.Trim();
                        video.Description = oa["description"] != null ? oa["description"].Value<string>() : "";
                        videos.Add(video);
                    }
                    oppetArkiv.Other = videos;
                    results.Add(oppetArkiv);
                }
            }
            return results;
        }
        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            HtmlNode htmlNode = GetWebData<HtmlDocument>("http://www.svtplay.se/senaste?embed=true&sida=1").DocumentNode;
            List<VideoInfo> videos = GetVideos(htmlNode);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        #endregion

    }
}