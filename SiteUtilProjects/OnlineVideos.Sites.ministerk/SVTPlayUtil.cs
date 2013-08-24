using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class SVTPlayUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Kbps"), Description("Chose your desired bitrate that will be preselected.")]
        protected int preferredKbps = 1400;

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Split by Letter"), Description("Chose if you want a flat list of all shows or split by the first letter.")]
		protected bool splitByLetter = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Chose if you want to download available subtitles or not.")]
        protected bool retrieveSubtitles = false;
        
        [Category("OnlineVideosConfiguration")]
		protected string SwfUrl = "http://www.svtplay.se/public/swf/video/svtplayer-2012.28.swf";

        protected int currentVideosMaxPages = 0;
        protected string nextPageUrl = "";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.ToList().ForEach(c => c.HasSubCategories = true);
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            nextPageUrl = "";
            currentVideosMaxPages = 0;
            HasNextPage = false;

            string url = (category as RssLink).Url;
			string data = GetWebData(url);
			if (data.Length > 0)
			{
				var htmlDoc = new HtmlAgilityPack.HtmlDocument();
				htmlDoc.LoadHtml(data);

				if (category.ParentCategory.Name == "Live" && category.Name == "Livesändningar")
				{
					return VideosForLiveCategory(htmlDoc.DocumentNode, url);
				}
				else if (category.ParentCategory.Name == "Live")
				{
					VideoInfo video = new VideoInfo();

                    var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playJsSchedule') and contains(@class,'svtTab-Active')]");
                    node = node.Descendants("div").Where(d => d.GetAttributeValue("class", "").Contains("playJsSchedule-SelectedEntry")).First();

					video.Title = node.GetAttributeValue("data-title", "");
					video.Description = node.GetAttributeValue("data-description", "");
					video.Length = node.GetAttributeValue("data-length", "");
                    video.ImageUrl = node.GetAttributeValue("data-titlepage-poster", "");
					video.Airdate = node.Descendants("time").First().InnerText;

					video.VideoUrl = url + "?output=json";

					return new List<VideoInfo>() { video };
				}
                else if (category.ParentCategory.Name == "Öppet arkiv" || (category.ParentCategory.ParentCategory != null && category.ParentCategory.ParentCategory.Name == "Öppet arkiv"))
                {
                    var docNode = htmlDoc.DocumentNode;
                    FindNextPageUrlForOppetArkiv(docNode, url);
                    return VideosForOppetArkivCategory(docNode, url);
                }
				else
				{
                    string tabName = category.Name == "Hela program" ? "programpanel" : "klipppanel";
					var containerDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@id, '" + tabName + "')]");
					if (containerDiv != null)
					{
						var lastPageNode = containerDiv.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "playBoxContainer").FirstOrDefault();
						if (lastPageNode != null) lastPageNode = lastPageNode.Element("a");
						if (lastPageNode != null)
						{
							int maxPages = lastPageNode.GetAttributeValue("data-lastpage", 0);
							if (maxPages > 1)
							{
								currentVideosMaxPages = maxPages;
								nextPageUrl = HttpUtility.HtmlDecode(lastPageNode.GetAttributeValue("data-baseurl", "")) + lastPageNode.GetAttributeValue("data-name", "") + "=" + lastPageNode.GetAttributeValue("data-nextpage", "");
								if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute)) nextPageUrl = new Uri(new Uri(url), nextPageUrl).AbsoluteUri;
								HasNextPage = true;
							}
						}
						return VideosForCurrentCategory(containerDiv, url);
					}
				}
			}

            return null;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            string data = GetWebData(nextPageUrl);
            if (data.Length > 0)
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var docNode = htmlDoc.DocumentNode;
                if (nextPageUrl.Contains("www.oppetarkiv.se"))
                {
                    string currentUrl = nextPageUrl;
                    FindNextPageUrlForOppetArkiv(docNode, nextPageUrl);
                    return VideosForOppetArkivCategory(docNode, currentUrl);
                }
                else
                {
                    var result = VideosForCurrentCategory(docNode, nextPageUrl);

                    int currentPage = int.MaxValue;
                    int.TryParse(nextPageUrl.Substring(nextPageUrl.LastIndexOf('=') + 1), out currentPage);
                    if (currentVideosMaxPages > currentPage)
                    {
                        nextPageUrl = nextPageUrl.Replace("=" + currentPage, "=" + (currentPage + 1).ToString());
                    }
                    else
                    {
                        HasNextPage = false;
                        nextPageUrl = "";
                    }

                    return result;
                }
            }
            return null;
        }

        private List<VideoInfo> VideosForLiveCategory(HtmlAgilityPack.HtmlNode node, string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            var lis = node.Descendants("li");
            foreach (var li in lis)
            {
                if (!li.Descendants("article").Where(a => a.GetAttributeValue("class", "") == "playBroadcastEnded").Any())
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = li.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();

                    if (!string.IsNullOrEmpty(video.VideoUrl))
                    {
                        video.VideoUrl = HttpUtility.HtmlDecode(video.VideoUrl);
                        if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(url), video.VideoUrl).AbsoluteUri;
                        video.VideoUrl += "?output=json";

                        var titleDiv = li.SelectSingleNode(".//div[contains(@class,'playBroadcastTitle')]");

                        var title = HttpUtility.HtmlDecode((titleDiv != null ? titleDiv.InnerText : "").Trim().Replace('\n', ' '));
                        bool live = li.Descendants("img").Where(img => img.GetAttributeValue("class", "") == "playBroadcastLiveIcon").Any();
                        video.Title = (live ? "LIVE - " : "") + title;

                        video.Airdate = li.Descendants("time").Select(t => t.GetAttributeValue("datetime", "")).FirstOrDefault();
                        if (!string.IsNullOrEmpty(video.Airdate)) video.Airdate = DateTime.Parse(video.Airdate).ToString("g", OnlineVideoSettings.Instance.Locale);

                        videoList.Add(video);
                    }
                }
            }
            return videoList;
        }

        private List<VideoInfo> VideosForCurrentCategory(HtmlAgilityPack.HtmlNode node, string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            var articles = node.SelectNodes(".//article[contains(@class,'svtMediaBlock')]");
            if (articles != null)
            {
                foreach (var article in articles)
                {
                    VideoInfo video = new VideoInfo();
					video.VideoUrl = article.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(video.VideoUrl))
                    {
                        if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(url), video.VideoUrl).AbsoluteUri;

                        video.Title = HttpUtility.HtmlDecode((article.GetAttributeValue("data-title", "") ?? "").Trim().Replace('\n', ' '));
                        video.Description = HttpUtility.HtmlDecode((article.GetAttributeValue("data-description", "") ?? "").Trim().Replace('\n', ' '));

                        video.ImageUrl = article.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                        if (!string.IsNullOrEmpty(video.ImageUrl) && !Uri.IsWellFormedUriString(video.ImageUrl, System.UriKind.Absolute)) video.ImageUrl = new Uri(new Uri(url), video.ImageUrl).AbsoluteUri;

                        video.Airdate = article.Descendants("time").Select(t => t.GetAttributeValue("datetime", "")).FirstOrDefault();
                        if (!string.IsNullOrEmpty(video.Airdate)) video.Airdate = DateTime.Parse(video.Airdate).ToString("g", OnlineVideoSettings.Instance.Locale);

                        videoList.Add(video);
                    }
                }
            }
            return videoList;
        }

        private void FindNextPageUrlForOppetArkiv(HtmlAgilityPack.HtmlNode node, string url)
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
                    if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute)) nextPageUrl = new Uri(new Uri(url), nextPageUrl).AbsoluteUri;
                    HasNextPage = true;
                }
            }
        }

        private List<VideoInfo> VideosForOppetArkivCategory(HtmlAgilityPack.HtmlNode node, string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            var div = node.SelectSingleNode("//div[contains(@class,'svtGridBlock')]");
            foreach (var article in div.Elements("article"))
            {
                VideoInfo video = new VideoInfo();
                video.VideoUrl = article.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                if (!string.IsNullOrEmpty(video.VideoUrl))
                {
                    if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(url), video.VideoUrl).AbsoluteUri;

                    video.Title = HttpUtility.HtmlDecode((article.Descendants("a").Select(a => a.GetAttributeValue("title", "")).FirstOrDefault() ?? "").Trim().Replace('\n', ' '));
                    video.ImageUrl = article.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                    video.Airdate = article.Descendants("time").Select(t => t.GetAttributeValue("datetime", "")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(video.Airdate)) video.Airdate = DateTime.Parse(video.Airdate).ToString("g", OnlineVideoSettings.Instance.Locale);
                    videoList.Add(video);
                }
            }
            return videoList;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null && parentCategory.Name == "Live")
            {
                parentCategory.HasSubCategories = true;
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;

            }
            else
            {
                string categoryUrl = (parentCategory as RssLink).Url;
                string data = GetWebData(categoryUrl);
                if (!string.IsNullOrEmpty(data))
                {
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(data);
                    parentCategory.SubCategories = new List<Category>();
                    if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("program"))
                    {
                        var divs = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'playAlphabeticLetter')]");
                        if (divs != null)
                        {
                            foreach (var div in divs)
                            {
                                RssLink letterCategory = new RssLink()
                                {
                                    Name = HttpUtility.HtmlDecode(div.Element("h3").InnerText.Trim().Replace('\n', ' ')),
                                    HasSubCategories = true,
                                    ParentCategory = parentCategory,
                                    SubCategoriesDiscovered = true,
                                    SubCategories = new List<Category>()
                                };
                                var li_a_s = div.Element("ul").Descendants("li").Select(li => li.Element("a"));
                                if (li_a_s != null)
                                {
                                    foreach (var a in li_a_s)
                                    {
                                        RssLink cat = new RssLink();
                                        cat.Url = a.GetAttributeValue("href", "");
                                        if (!string.IsNullOrEmpty(cat.Url))
                                        {
                                            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;

                                            cat.Name = HttpUtility.HtmlDecode(a.InnerText.Trim().Replace('\n', ' '));

                                            cat.SubCategories = new List<Category>() { new RssLink() { Name = "Hela program", ParentCategory = cat, Url = cat.Url + "?pr=1" }, new RssLink() { Name = "Klipp", ParentCategory = cat, Url = cat.Url + "?kl=1" } };
                                            cat.HasSubCategories = true;
                                            cat.SubCategoriesDiscovered = true;

                                            if (splitByLetter)
                                            {
                                                letterCategory.SubCategories.Add(cat);
                                                cat.ParentCategory = letterCategory;
                                            }
                                            else
                                            {
                                                parentCategory.SubCategories.Add(cat);
                                                cat.ParentCategory = parentCategory;
                                            }
                                        }
                                    }
                                    if (splitByLetter && letterCategory.SubCategories.Count > 0)
                                    {
                                        letterCategory.EstimatedVideoCount = (uint)letterCategory.SubCategories.Count;
                                        parentCategory.SubCategories.Add(letterCategory);
                                    }
                                }
                            }
                        }
                    }
                    else if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("kategorier"))
                    {
                        var lis = htmlDoc.DocumentNode.SelectNodes("//li[contains(@class,'svtMediaBlock')]");
                        if (lis != null)
                        {
                            foreach (var li in lis)
                            {
                                RssLink cat = new RssLink();
                                cat.Url = li.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                                if (!string.IsNullOrEmpty(cat.Url) && !cat.Url.EndsWith("oppetarkiv"))
                                {
                                    if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;

                                    cat.Name = HttpUtility.HtmlDecode((li.Descendants("h3").Select(h => h.InnerText).FirstOrDefault() ?? "").Trim().Replace('\n', ' '));

                                    cat.Thumb = li.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(categoryUrl), cat.Thumb).AbsoluteUri;

                                    cat.HasSubCategories = true;
                                    cat.ParentCategory = parentCategory;
                                    parentCategory.SubCategories.Add(cat);
                                }
                            }
                        }
                    }
                    else if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("oppetarkiv"))
                    {
                        var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@role,'main')]");
                        foreach (var section in node.Elements("section"))
                        {
                            RssLink letterCategory = new RssLink()
                            {
                                Name = HttpUtility.HtmlDecode(section.Element("h2").Element("a").GetAttributeValue("id", "")),
                                HasSubCategories = true,
                                ParentCategory = parentCategory,
                                SubCategoriesDiscovered = true,
                                SubCategories = new List<Category>()
                            };
                            var li_a_s = section.Element("ul").Descendants("li").Select(li => li.Element("a"));
                            if (li_a_s != null)
                            {
                                foreach (var a in li_a_s)
                                {
                                    RssLink cat = new RssLink();
                                    cat.Url = a.GetAttributeValue("href", "");
                                    if (!string.IsNullOrEmpty(cat.Url))
                                    {
                                        cat.Url = HttpUtility.UrlDecode(cat.Url);
                                        cat.Url = HttpUtility.HtmlDecode(cat.Url); //Some urls come html encoded
                                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;
                                        cat.Url += "?sida=1&sort=tid_stigande";
                                        cat.Name = HttpUtility.HtmlDecode(a.InnerText.Trim().Replace('\n', ' '));
                                        cat.HasSubCategories = false;
                                        cat.SubCategoriesDiscovered = false;

                                        if (splitByLetter)
                                        {
                                            letterCategory.SubCategories.Add(cat);
                                            cat.ParentCategory = letterCategory;
                                        }
                                        else
                                        {
                                            parentCategory.SubCategories.Add(cat);
                                            cat.ParentCategory = parentCategory;
                                        }
                                    }
                                }
                                if (splitByLetter && letterCategory.SubCategories.Count > 0)
                                {
                                    letterCategory.EstimatedVideoCount = (uint)letterCategory.SubCategories.Count;
                                    parentCategory.SubCategories.Add(letterCategory);
                                }
                            }
                        }
                    }
                    else
                    {
                        var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playJsTabs playBox')]");
                        CategoriesFromArticles(node, parentCategory);

                        // categories are spread over pages - remember the last page on the parent category, so we know when to stop adding a NextPageCategory
                        var lastPageNode = node.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "playBoxContainer").FirstOrDefault();
                        if (lastPageNode != null) lastPageNode = lastPageNode.Element("a");
                        if (lastPageNode != null)
                        {
                            int maxPages = lastPageNode.GetAttributeValue("data-lastpage", 0);
                            if (maxPages > 1)
                            {
                                parentCategory.Other = maxPages;
                                string url = HttpUtility.HtmlDecode(lastPageNode.GetAttributeValue("data-baseurl", "")) + lastPageNode.GetAttributeValue("data-name", "") + "=" + lastPageNode.GetAttributeValue("data-nextpage", "");
                                if (!Uri.IsWellFormedUriString(url, System.UriKind.Absolute)) url = new Uri(new Uri(categoryUrl), url).AbsoluteUri;
                                parentCategory.SubCategories.Add(new NextPageCategory() { Url = url, ParentCategory = parentCategory });
                            }
                        }
                    }

                    parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)

                    return parentCategory.SubCategories.Count; // return the number of discovered categories
                }
            }
            return 0;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            string data = GetWebData(category.Url);
            if (!string.IsNullOrEmpty(data))
            {
                category.ParentCategory.SubCategories.Remove(category);

                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                int num = CategoriesFromArticles(htmlDoc.DocumentNode, category.ParentCategory);

                int currentPage = int.MaxValue;
                int.TryParse(category.Url.Substring(category.Url.LastIndexOf('=')+1), out currentPage);
                if ((int)category.ParentCategory.Other > currentPage)
                {
                    category.ParentCategory.SubCategories.Add(new NextPageCategory() { Url = category.Url.Replace("=" + currentPage, "="+(currentPage+1).ToString()), ParentCategory = category.ParentCategory });
                }
                return num;
            }
            return 0;
        }

        int CategoriesFromArticles(HtmlAgilityPack.HtmlNode node, Category parentCategory)
        {
            string categoryUrl = (parentCategory as RssLink).Url;
            int num = 0;
            var articles = node.Descendants("article");
            if (articles != null)
            {
                foreach (var article in articles)
                {
                    RssLink cat = new RssLink();
					cat.Url = article.Descendants("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(cat.Url))
                    {
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;

                        cat.Name = HttpUtility.HtmlDecode((article.GetAttributeValue("data-title", "") ?? "").Trim().Replace('\n', ' '));
                        cat.Description = HttpUtility.HtmlDecode((article.GetAttributeValue("data-description", "") ?? "").Trim().Replace('\n', ' '));

                        cat.Thumb = article.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                        if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(categoryUrl), cat.Thumb).AbsoluteUri;

						cat.SubCategories = new List<Category>() { new RssLink() { Name = "Hela program", ParentCategory = cat, Url = cat.Url + "?pr=1" }, new RssLink() { Name = "Klipp", ParentCategory = cat, Url = cat.Url + "?kl=1" } };
						cat.HasSubCategories = true;
						cat.SubCategoriesDiscovered = true;

                        cat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(cat);
                        num++;
                    }
                }
            }
            return num;
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string bestMatchUrl = "";
            List<String> result = new List<string>();

            string jsonUrl = video.VideoUrl.EndsWith("?output=json") ? video.VideoUrl : video.VideoUrl.Substring(0, video.VideoUrl.LastIndexOf("/")) + "?output=json";
            var json = GetWebData<Newtonsoft.Json.Linq.JObject>(jsonUrl);

            if (retrieveSubtitles)
            {
                try
                {
                    var subtitleReferences = json["video"]["subtitleReferences"].Where(sr => ((string)sr["url"] ?? "").EndsWith("srt"));
                    if (subtitleReferences != null)
                    {
                        foreach (var sr in subtitleReferences)
                        {
                            string url = (string)sr["url"];
                            if (!string.IsNullOrEmpty(url))
                            {
                                video.SubtitleText = CleanSubtitle(GetWebData(url));
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    Log.Error("{0}", "SVT play - Error retrieving subtitle");
                }
            }
            var sortedPlaybackOptions = json["video"]["videoReferences"].Where(vr => (string)vr["playerType"] != "ios" && (((string)vr["url"]).StartsWith("http") || ((string)vr["url"]).StartsWith("rtmp"))).OrderBy(vr => (int)vr["bitrate"]);
            foreach (var option in sortedPlaybackOptions)
            {
                string url = (string)option["url"];
				if (url.StartsWith("rtmp"))
				{
					url = new MPUrlSourceFilter.RtmpUrl(url.Replace("_definst_", "?slist=")) 
					{
						SwfVerify = true,
						SwfUrl = SwfUrl,
						Live = video.VideoUrl.Contains("/live")
					}.ToString();
				}
				else if (url.StartsWith("http://") && url.EndsWith(".f4m"))
				{
                    url = url + "?hdcore=2.11.3&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12);
				}
				else if (url.StartsWith("http://geoip.api"))
					url = HttpUtility.ParseQueryString(new Uri(url).Query)["vurl"];

                if ((int)option["bitrate"] <= preferredKbps) bestMatchUrl = url;

                video.PlaybackOptions.Add(string.Format("{0}:// | {1} kbps", url.Substring(0,4), option["bitrate"].ToString()), url);
            }
            
            if (bestMatchUrl != "") result.Add(bestMatchUrl);
            else result.Add(video.PlaybackOptions.Select(po => po.Value).FirstOrDefault());

            if (inPlaylist) video.PlaybackOptions = null;

            return result;
        }

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
    }
}
