using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class NdrUtil : SiteUtilBase
    {
		const string baseUrl = "http://www.ndr.de";
		const string category_list_url = "http://www.ndr.de/mediathek/dropdown101-extapponly.html";
		const string search_url = "http://www.ndr.de/mediathek/mediatheksuche101.html?search_video=true&query={0}";

		string nextPageUrl = "";

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Clear();
			HtmlDocument htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(GetWebData(category_list_url));
			var list = htmlDoc.DocumentNode.Descendants("ul").First(ul => ul.GetAttributeValue("class", "") == "m_az_key");
			foreach (var h5 in list.Descendants("h5"))
			{
				var a = h5.Element("a");
				Settings.Categories.Add(new RssLink() { Name = a.InnerText, Url = baseUrl + a.GetAttributeValue("href", "").Replace("103", "105") });
			}
			Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
			return Settings.Categories.Count;
		}

		public override List<VideoInfo> GetVideos(Category category)
		{
			HasNextPage = false;
			var result = new List<VideoInfo>();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(GetWebData((category as RssLink).Url));
			foreach (var item in htmlDoc.DocumentNode.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "m_teaser"))
			{
				string videoUrl = item.Descendants("a").First().GetAttributeValue("href", "");
				if (!string.IsNullOrEmpty(videoUrl))
				{
					if (!Uri.IsWellFormedUriString(videoUrl, UriKind.Absolute))
						videoUrl = new Uri(new Uri((category as RssLink).Url), videoUrl).AbsoluteUri;
					var length = item.Descendants("span").FirstOrDefault(s => s.GetAttributeValue("class", "") == "runtime");
					var video = new VideoInfo()
					{
						Title = item.Descendants("h4").First().InnerText,
						Description = item.Descendants("p").First().FirstChild.InnerText,
						VideoUrl = videoUrl,
						Thumb = baseUrl + item.Descendants("img").First().GetAttributeValue("src", ""),
						Length = length != null ? length.InnerText : "",
						Airdate = HttpUtility.HtmlDecode(item.Descendants("div").First(d => d.GetAttributeValue("class", "") == "subline").InnerText)
					};
					video.Airdate = video.Airdate.Substring(video.Airdate.LastIndexOf('-') + 1).Trim();
					result.Add(video);
				}
			}
			var weiterSpan = htmlDoc.DocumentNode.Descendants("span").FirstOrDefault(s => s.InnerText == "weiter");
			if (weiterSpan != null && weiterSpan.ParentNode.Name == "a")
			{
				nextPageUrl = weiterSpan.ParentNode.GetAttributeValue("href", "");
				if (!Uri.IsWellFormedUriString(nextPageUrl, UriKind.Absolute))
					nextPageUrl = new Uri(new Uri((category as RssLink).Url), nextPageUrl).AbsoluteUri;
				HasNextPage = true;
			}
			return result;
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			return GetVideos(new RssLink() { Url = nextPageUrl });
		}

        public override String GetVideoUrl(VideoInfo video)
        {
			video.PlaybackOptions = new Dictionary<string, string>();
			string data = GetWebData(video.VideoUrl);
			var match = Regex.Match(data, @"playlist:\s*(?<playlist>\[.*?\])", RegexOptions.Singleline);
			if (match.Success)
			{
				var json = JsonConvert.DeserializeObject<JArray>(match.Groups["playlist"].Value);
				foreach (var item in json.First.Children())
				{
					string url = item.First.Value<string>("src");
					if (!string.IsNullOrEmpty(url))
					{
						if (url.EndsWith("f4m"))
							video.PlaybackOptions.Add("HD", url = url + "?hdcore=2.8.2&g=AAAAAAAAAAAA");
						else if (url.EndsWith("mp4"))
							video.PlaybackOptions.Add("SD", url);
					}
				}
			}
			else
			{
				throw new OnlineVideosException("Keinen Stream gefunden.");
			}
			return video.PlaybackOptions.Select(d => d.Value).FirstOrDefault();
        }

		public override bool CanSearch { get { return true; } }

		public override List<SearchResultItem> Search(string query, string category = null)
		{
			return GetVideos(new RssLink() { Url = string.Format(search_url, query) }).ConvertAll<SearchResultItem>(v => (SearchResultItem)v);
		}
    }
}