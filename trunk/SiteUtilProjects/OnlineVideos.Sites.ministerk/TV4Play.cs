using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase
	{
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;

        protected string playbackMetadataUrl = "http://prima.tv4play.se/api/web/asset/{0}/play";
        protected string playbackSwfUrl = "http://www.tv4play.se/flash/tv4play_sa.swf";

        protected string nextPageUrl = "";

        public override int DiscoverDynamicCategories()
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(GetWebData(baseUrl));
            var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@id = 'program-categories']");
            Settings.Categories.Clear();
            foreach (var li in ul.Elements("li"))
            {
                var a = li.Element("a");
                RssLink category = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(a.InnerText),
                    Url = GetUrlForCategory(a.GetAttributeValue("href", "")),
                    SubCategories = new List<Category>(),
                    HasSubCategories = true
                };
                // do not use subcategories as there are only very few or zero items below each
                /*var innerUl = li.Element("ul");
                if (innerUl != null)
                {
                    foreach (var innerLi in innerUl.Elements("li"))
                    {
                        var innerA = innerLi.Element("a");
                        category.SubCategories.Add(new RssLink()
                        {
                            Name = HttpUtility.HtmlDecode(innerA.InnerText),
                            Url = GetUrlForCategory(innerA.GetAttributeValue("href", "")),
                            ParentCategory = category,
                            SubCategories = new List<Category>(),
                            HasSubCategories = true
                        });
                    }
                    category.SubCategoriesDiscovered = true;
                    category.EstimatedVideoCount = (uint)category.SubCategories.Count;
                }*/
                Settings.Categories.Add(category);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        string GetUrlForCategory(string category)
        {
            category = HttpUtility.HtmlDecode(category);
            Uri uri = null;
            if (!Uri.IsWellFormedUriString(category, UriKind.Absolute))
            {
                // workaround for .net bug when combining uri with a query only
                if (category.StartsWith("?"))
                {
                    uri = new UriBuilder(baseUrl) { Query = category.Substring(1) }.Uri;
                }
                else
                {
                    if (Uri.TryCreate(new Uri(baseUrl), category, out uri))
                    {
                        category = uri.ToString();
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            // use : &is_geo_restricted=false to hide geoblocked
            return uri.ToString() + (string.IsNullOrEmpty(uri.Query) ? "?" : "&") + "page=100&is_premium=false&content-type=senaste";
        }

		public override int DiscoverSubCategories(Category parentCategory)
		{
			string data = GetWebData((parentCategory as RssLink).Url);
			parentCategory.SubCategories = new List<Category>();
			if (!string.IsNullOrEmpty(data))
			{
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'row js-show-more-content']");
                if (ul != null)
                {
                    foreach (var li in ul.Elements("li"))
                    {
                        RssLink cat = new RssLink();
                        cat.Url = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").GetAttributeValue("href", ""));
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                        cat.Name = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").InnerText);
                        cat.Description = HttpUtility.HtmlDecode(li.Element("div").Element("p").InnerText);
                        cat.Thumb = HttpUtility.ParseQueryString(new Uri(HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(li.Element("p").Element("a").Element("img").GetAttributeValue("src", "")))).Query)["source"];
                        parentCategory.SubCategories.Add(cat);
                        cat.ParentCategory = parentCategory;
                    }
                }
                (parentCategory as RssLink).EstimatedVideoCount = (uint)parentCategory.SubCategories.Count;
			}

			parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
			return parentCategory.SubCategories.Count;
		}

		public override List<VideoInfo> getNextPageVideos()
		{
			return getVideoList(new RssLink() { Url = nextPageUrl });
		}

		public override List<VideoInfo> getVideoList(Category category)
		{
			List<VideoInfo> videos = new List<VideoInfo>();
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(GetWebData(((RssLink)category).Url));
            var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'row js-show-more-content']");
            foreach (var li in ul.Elements("li"))
            {
				VideoInfo video = new VideoInfo();
                video.Title = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").InnerText);
                video.Airdate = HttpUtility.HtmlDecode(li.Element("div").Element("div").Element("p").InnerText);
                video.ImageUrl = HttpUtility.ParseQueryString(new Uri(HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(li.Element("p").Element("a").Element("img").GetAttributeValue("src", "")))).Query)["source"];
                video.VideoUrl = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").GetAttributeValue("href", ""));
                if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(baseUrl), video.VideoUrl).AbsoluteUri;
                video.Description = HttpUtility.HtmlDecode(li.Element("div").Elements("p").First(p => p.GetAttributeValue("class", "") == "video-description").InnerText);
				videos.Add(video);
			}
            
			HasNextPage = false;
            nextPageUrl = "";
            var nextPageLink = htmlDoc.DocumentNode.SelectSingleNode("//a[@class = 'js-show-more btn secondary full']");
            if (nextPageLink != null)
            {
                nextPageUrl = HttpUtility.HtmlDecode(nextPageLink.GetAttributeValue("data-more-from", ""));
                if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute)) nextPageUrl = new Uri(new Uri(((RssLink)category).Url), nextPageUrl).AbsoluteUri;
                HasNextPage = true;
            }

			return videos;
		}

		public override string getUrl(VideoInfo video)
		{
			string result = string.Empty;
			video.PlaybackOptions = new Dictionary<string, string>();
			XmlDocument xDoc = GetWebData<XmlDocument>(string.Format(playbackMetadataUrl, HttpUtility.ParseQueryString(new Uri(video.VideoUrl).Query)["video_id"]));
			var errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
			if (errorElements != null && errorElements.Count > 0)
			{
				throw new OnlineVideosException(((XmlElement)errorElements[0]).GetAttribute("content"));
			}
			else
			{
				List<KeyValuePair<int, string>> urls = new List<KeyValuePair<int, string>>();
                foreach (XmlElement videoElem in xDoc.SelectNodes("//items/item"))
				{
                    if (videoElem.GetElementsByTagName("scheme")[0].InnerText.ToLower().StartsWith("rtmp"))
                    {
                        urls.Add(new KeyValuePair<int, string>(
                            int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                            new MPUrlSourceFilter.RtmpUrl(videoElem.GetElementsByTagName("base")[0].InnerText) 
                            {
                                PlayPath = videoElem.GetElementsByTagName("url")[0].InnerText.Replace(".mp4", ""),
                                SwfUrl = playbackSwfUrl,
                                SwfVerify = true
                            }.ToString()));
                    }
				}
				foreach(var item in urls.OrderBy(u => u.Key))
				{
					video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
					result = item.Value;
				}
				return result;
			}
		}
	}
}
