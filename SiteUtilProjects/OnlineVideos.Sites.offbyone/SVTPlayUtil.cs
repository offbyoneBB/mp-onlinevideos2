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

				if (category.Name == "Live")
				{
					return VideosForLiveCategory(htmlDoc.DocumentNode, url);
				}
				else if (category.ParentCategory.Name == "Live")
				{
					VideoInfo video = new VideoInfo();

					var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playVideoBox')]");
					video.ImageUrl = node.Element("a").Element("img").GetAttributeValue("data-imagename", "");

					node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playJsSchedule') and contains(@class,'svtTab-Active')]");
					node = node.Descendants("article").First();

					video.Title = node.GetAttributeValue("data-title", "");
					video.Description = node.GetAttributeValue("data-description", "");
					video.Length = node.GetAttributeValue("data-length", "");
					video.Airdate = node.Descendants("time").First().InnerText;

					video.VideoUrl = url + "?output=json";

					return new List<VideoInfo>() { video };
				}
				else
				{
					string tabName = category.Name == "Hela program" ? "episodes" : "clips";
					var containerDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'playBoxBody') and contains(@data-tabname, '" + tabName + "')]");
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

                var result = VideosForCurrentCategory(htmlDoc.DocumentNode, nextPageUrl);

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
            return null;
        }

        private List<VideoInfo> VideosForLiveCategory(HtmlAgilityPack.HtmlNode node, string url)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            foreach (var article in node.SelectSingleNode("//div[contains(@class, 'svtGridBlock')]").Descendants("article"))
            {
                string tag = article.Ancestors("div").Where(a => a.Element("h2") != null).Select(div => div.Element("h2").InnerText).FirstOrDefault();

                VideoInfo video = new VideoInfo();
                video.VideoUrl = article.Ancestors("a").Select(a => a.GetAttributeValue("href", "")).FirstOrDefault();
                if (!string.IsNullOrEmpty(video.VideoUrl))
                {
                    video.VideoUrl = HttpUtility.HtmlDecode(video.VideoUrl);
                    if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(url), video.VideoUrl).AbsoluteUri;

                    string playTime = article.Elements("div").Where(div => div.GetAttributeValue("class", "").Contains("playBroadcastTime")).Select(div => div.InnerText).FirstOrDefault();
                    if (playTime == null)
                        playTime = video.Description = article.Elements("span").Where(div => div.GetAttributeValue("class", "").Contains("playBroadcastTime")).Select(div => div.InnerText).FirstOrDefault();

                    playTime = Regex.Replace(playTime, @"\s+", "", RegexOptions.Multiline);

					bool live = article.Descendants("img").Where(img => img.GetAttributeValue("class", "") == "playBroadcastLiveIcon").Any();

                    var h5Node = article.Element("h5");
                    string playBroadcastTitle = article.Elements("div").Where(div => div.GetAttributeValue("class", "") == "playBroadcastTitle").Select(div => div.InnerText).FirstOrDefault();
                    video.Title = string.Format("{0} - {1}{3} - {2}", playTime, tag, h5Node != null ? h5Node.InnerText : playBroadcastTitle, live ? " LIVE" : "");

                    videoList.Add(video);
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

        public override int DiscoverSubCategories(Category parentCategory)
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
                            if (!string.IsNullOrEmpty(cat.Url))
                            {
                                if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;

                                cat.Name = HttpUtility.HtmlDecode((li.Descendants("h3").Select(h => h.InnerText).FirstOrDefault() ?? "").Trim().Replace('\n', ' '));

                                cat.Thumb = li.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                                if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(categoryUrl), cat.Thumb).AbsoluteUri;

                                cat.HasSubCategories = cat.Name != "Live";
                                cat.ParentCategory = parentCategory;
                                parentCategory.SubCategories.Add(cat);
                            }
                        }
                    }
                }
				else if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("kanaler"))
				{
					var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playChannelMenu')]");
					foreach (var li in node.Element("ul").Elements("li"))
					{
						RssLink cat = new RssLink();
						cat.Url = li.Element("a").GetAttributeValue("href", "");
						if (!string.IsNullOrEmpty(cat.Url) && !Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(categoryUrl), cat.Url).AbsoluteUri;

						var img = li.Element("a").Element("div").Element("img");
						cat.Name = img.GetAttributeValue("alt", "");

						cat.Thumb = img.GetAttributeValue("src", "");
						if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(categoryUrl), cat.Thumb).AbsoluteUri;

						cat.ParentCategory = parentCategory;
						parentCategory.SubCategories.Add(cat);
					}
				}
				else
				{
					var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'playBoxBody') and contains(@class,'svtTab-Active')]");
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
					url = url + "?hdcore=2.11.3&g=" + GetRandomChars(12);
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

		string GetRandomChars(int amount)
		{
			var random = new Random();
			var sb = new System.Text.StringBuilder(amount);
			for (int i = 0; i < amount;i++ ) sb.Append(System.Text.Encoding.ASCII.GetString(new byte[] { (byte)random.Next(65, 90) }));
			return sb.ToString();
		}
    }
}
