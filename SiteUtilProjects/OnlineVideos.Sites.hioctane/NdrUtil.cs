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
        const string category_list_url = "http://www.ndr.de/mediathek/sendungen_a-z/index.html";
        const string search_url = "http://www.ndr.de/suche10.html?query={0}&search_mediathek=1&sort_by=date&range=unlimited&results_per_page=50#";

		string nextPageUrl = "";

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Clear();
			HtmlDocument htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(GetWebData(category_list_url));
            var section = htmlDoc.DocumentNode.Descendants("section").FirstOrDefault(s => s.GetAttributeValue("class", "") =="columnedlist");
			foreach (var li in section.Descendants("li"))
			{
				var a = li.Element("a");
				Settings.Categories.Add(new RssLink() { Name = a.InnerText, Url = baseUrl + a.GetAttributeValue("href", "") });
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
			foreach (var item in htmlDoc.DocumentNode.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
			{
				string videoUrl = item.Descendants("a").First().GetAttributeValue("href", "");
				if (!string.IsNullOrEmpty(videoUrl))
				{
					if (!Uri.IsWellFormedUriString(videoUrl, UriKind.Absolute))
						videoUrl = new Uri(new Uri((category as RssLink).Url), videoUrl).AbsoluteUri;
                    var length = item.Descendants("div").FirstOrDefault(s => s.GetAttributeValue("class", "") == "textpadding");
					var video = new VideoInfo()
					{
						Title = item.Descendants("h2").First().InnerText.Trim(),
						Description = item.Descendants("p").First().FirstChild.InnerText.Trim(),
						VideoUrl = videoUrl,
						Thumb = baseUrl + item.Descendants("img").First().GetAttributeValue("src", ""),
						Length = length != null ? length.InnerText.Trim() : "",
					};
                    var airDateNode = item.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "subline");
                    if (airDateNode != null) video.Airdate = HttpUtility.HtmlDecode(airDateNode.InnerText);
					result.Add(video);
				}
			}
			var weiterLink = htmlDoc.DocumentNode.Descendants("a").FirstOrDefault(s => s.GetAttributeValue("title", "") == "weiter");
			if (weiterLink != null)
			{
				nextPageUrl = weiterLink.GetAttributeValue("href", "");
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
            var match = Regex.Match(data, @"playlist\s*:\s*\[(?<inner>\s*
{
            [^{}]*
            (
                        (
                                    (?<Open>{)
                                    [^{}]*
                        )+
                        (
                                    (?<Close-Open>})
                                    [^{}]*
                        )+
            )*
            (?(Open)(?!))
})", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
			if (match.Success)
			{
                var json = JsonConvert.DeserializeObject<JObject>(match.Groups["inner"].Value.Replace("\"","'").Replace("' || '", ""));
				foreach (var item in json.Children())
				{
                    string url = (item as JProperty).Value.Value<string>("src");
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