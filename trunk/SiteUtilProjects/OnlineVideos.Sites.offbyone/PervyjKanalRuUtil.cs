using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class PervyjKanalRuUtil : SiteUtilBase
    {
		[Category("OnlineVideosConfiguration")]
        string liveUrl = "http://stream.1tv.ru/live";
		[Category("OnlineVideosConfiguration")]
        string livePlaylistRegex = @"var\s+playlist\s*=\s*'(?<url>[^']+)'";
		[Category("OnlineVideosConfiguration")]
        string liveScheduleUrl = "http://stream.1tv.ru/schedule_10h.js";
		[Category("OnlineVideosConfiguration")]
		string videoArchivUrl = "http://www.1tv.ru/videoarchiver/";
		[Category("OnlineVideosConfiguration")]
		string allVideosRegex = @"<a\s+href=""(?<url>[^""]+)""[^>]*>Все\s+[^<]+</a>";
		[Category("OnlineVideosConfiguration")]
		string downloadFileRegex = @"'file'\s*:\s*'(?<url>[^']+)'";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
			// add one Category for the Live Stream
			Settings.Categories.Add(new Category() { Name = "Прямой эфир" });
			// get all categories from the video archive
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(GetWebData(videoArchivUrl));
			var ul = htmlDoc.DocumentNode.SelectSingleNode("//div[@class = 'title' and text() = 'Каталог видеоархива']").NextSibling.NextSibling;
			GetVideoArchivCategoryHierarchy(ul, null);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

		void GetVideoArchivCategoryHierarchy(HtmlNode ul, Category parentCategory)
		{
			if (ul == null) return;

			foreach (var li in ul.ChildNodes.Where(n => n.Name == "li"))
			{
				var a = li.Element("a");
				var span = li.Element("span");

				Category newCat = (a != null) ?
					new RssLink() { Url = HttpUtility.UrlDecode(a.GetAttributeValue("href", "")), Name = a.InnerText.Trim(), ParentCategory = parentCategory } :
					new Category() { Name = span.InnerText.Trim(), ParentCategory = parentCategory, SubCategories = new List<Category>(), HasSubCategories = true, SubCategoriesDiscovered = true };

				if (parentCategory == null)
					Settings.Categories.Add(newCat);
				else
					parentCategory.SubCategories.Add(newCat);

				foreach(var subUl in li.Elements("ul"))
					GetVideoArchivCategoryHierarchy(subUl, newCat);
			}
		}

        public override List<VideoInfo> GetVideos(Category category)
        {
			if (category is RssLink)
			{
				string url = (category as RssLink).Url;
				if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) url = new Uri(new Uri(videoArchivUrl), url).AbsoluteUri;
				string allVideosUrl = HttpUtility.UrlDecode(Regex.Match(GetWebData(url), allVideosRegex).Groups["url"].Value);
				if (!Uri.IsWellFormedUriString(allVideosUrl, UriKind.Absolute)) allVideosUrl = new Uri(new Uri(videoArchivUrl), allVideosUrl).AbsoluteUri;
				return GetVideosFromArchivePage(allVideosUrl);
			}
			else // handle Live Category differently
			{
				return GetVideoListForLiveStreaming();
			}
        }

		private List<VideoInfo> GetVideoListForLiveStreaming()
		{
			// try to get infos for the currently playing and next show
			var titles = new List<string>();
			try
			{
				var jsonData = GetWebData<JToken>(liveScheduleUrl);
				var channel = ((JArray)jsonData)[0]["channel"];
				uint current_timestamp = channel.First(j => j["current_timestamp"] != null)["current_timestamp"].Value<uint>("value");
				var schedule = channel.First(j => j["schedule"] != null)["schedule"];

				var currentShow = schedule.FirstOrDefault(i => i["issue"]["begin"].Value<uint>("value") < current_timestamp && i["issue"]["end"].Value<uint>("value") > current_timestamp);
				if (currentShow != null)
				{
					titles.Add(string.Format(OnlineVideoSettings.Instance.Locale, "Сейчас: {0} ({1:t}-{2:t}) - Онлайн: {3}",
						currentShow["issue"]["0"]["title"]["value"].Value<string>(),
                        Helpers.TimeUtils.UNIXTimeToDateTime(currentShow["issue"]["begin"].Value<uint>("value")),
                        Helpers.TimeUtils.UNIXTimeToDateTime(currentShow["issue"]["end"].Value<uint>("value")),
						currentShow["issue"]["online"].Value<string>("value") == "no" ? "нет" : "да"));
				}
				var nextShow = schedule.FirstOrDefault(i => i["issue"]["begin"].Value<uint>("value") >= current_timestamp);
				if (nextShow != null)
				{
					titles.Add(string.Format(OnlineVideoSettings.Instance.Locale, "Далее: {0} ({1:t}-{2:t}) - Онлайн: {3}",
						nextShow["issue"]["0"]["title"]["value"].Value<string>(),
                        Helpers.TimeUtils.UNIXTimeToDateTime(nextShow["issue"]["begin"].Value<uint>("value")),
                        Helpers.TimeUtils.UNIXTimeToDateTime(nextShow["issue"]["end"].Value<uint>("value")),
						nextShow["issue"]["online"].Value<string>("value") == "no" ? "нет" : "да"));
				}
			}
			catch (Exception ex)
			{
				Log.Info("Error getting current show infos: {0}", ex.ToString());
			}
			if (titles.Count == 0) titles.Add("Прямой эфир");
			var result = new List<VideoInfo>();
			foreach (var item in titles)
			{
				result.Add(new VideoInfo() { Title = item, VideoUrl = liveUrl, Description = "Прямой эфир" });
			}
			return result;
		}

		string nextPageUrl;

		List<VideoInfo> GetVideosFromArchivePage(string url)
		{
			HasNextPage = false;
			nextPageUrl = "";

			var videos = new List<VideoInfo>();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(GetWebData(url));
			var div = htmlDoc.DocumentNode.SelectSingleNode("//div[@id = 'list_abc_search']");

			var innerDiv = div.Elements("div").FirstOrDefault(d => d.GetAttributeValue("id", "") == "list_video");
			if (innerDiv != null) div = innerDiv;

			foreach(var ul in div.Elements("ul"))
			foreach (var li in ul.Elements("li"))
			{
				VideoInfo video = new VideoInfo();
				var img = li.Descendants("img").FirstOrDefault();
				if (img != null) video.Thumb = img.GetAttributeValue("src", "");
				video.Airdate = li.SelectSingleNode("div[@class = 'date']").InnerText;
				video.Title = HttpUtility.HtmlDecode(li.SelectSingleNode("div[@class = 'txt']").InnerText);
				video.VideoUrl =  HttpUtility.HtmlDecode(li.Descendants("a").FirstOrDefault().GetAttributeValue("href", ""));
				if (!Uri.IsWellFormedUriString(video.VideoUrl, UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(url), video.VideoUrl).AbsoluteUri;
				videos.Add(video);
			}

			var pagingDiv = div.SelectSingleNode("div[@class='all_pagination']/div[@class='all_pages']");
			if (pagingDiv != null)
			{
				var nextPageLink = pagingDiv.Descendants("a").FirstOrDefault(a => a.InnerText.Contains("&gt;"));
				if (nextPageLink != null)
				{
					HasNextPage = true;
					nextPageUrl = HttpUtility.HtmlDecode(nextPageLink.GetAttributeValue("href", ""));
					if (!Uri.IsWellFormedUriString(nextPageUrl, UriKind.Absolute)) nextPageUrl = new Uri(new Uri(url), nextPageUrl).AbsoluteUri;
				}
			}

			return videos;
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			return GetVideosFromArchivePage(nextPageUrl);
		}

        public override string GetVideoUrl(VideoInfo video)
        {
			string data = GetWebData(video.VideoUrl);
			if (video.Description == "Прямой эфир") // live
			{
				// find the url for playlist xml
				string url = HttpUtility.UrlDecode(Regex.Match(data, livePlaylistRegex).Groups["url"].Value);
				if (!Uri.IsWellFormedUriString(url, System.UriKind.Absolute)) url = new Uri(new Uri(liveUrl), url).AbsoluteUri;
				// get the playlist xml
				var playlistXml = GetWebData<System.Xml.XmlDocument>(url);
				string stream_url = playlistXml.SelectSingleNode("//url").InnerText;
				return stream_url;
			}
			else // videoarchiv
			{
				var htmlDoc = new HtmlDocument();
				htmlDoc.LoadHtml(data);
				var div = htmlDoc.DocumentNode.SelectSingleNode("//div[@id = 'flashvideoportal_1']");
				var js = div.NextSibling.NextSibling.InnerText;
				string url = Regex.Match(js, downloadFileRegex).Groups["url"].Value;
				return url;
			}
        }
    }
}
