using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Web;

namespace OnlineVideos.Sites
{
	public class rbbUtil : SiteUtilBase, IChoice
	{
		string categoriesUrl = @"http://mediathek.rbb-online.de/rbb/servlet/ajax-cache/9869914/view=list/index.html";
		string videosUrl = @"http://mediathek.rbb-online.de/rbb/servlet/ajax-cache/9869702/view=list/{0}";
		string mediaItemRegex = "mediaCollection.addMediaStream\\(\\d, \\d, \"(?<server>[^\"]*)\", \"(?<path>[^\"]*)\", \"(?<provider>[^\"]*)\"\\)";
		string searchUrl = @"http://mediathek.rbb-online.de/suche?s={0}&inhalt=all&view=list&avfilter=video";

		string nextPageUrl;

		public override int DiscoverDynamicCategories()
		{
			var html = GetWebData<HtmlDocument>(categoriesUrl);
			Settings.Categories.Clear();
			var ol = html.DocumentNode.Descendants("ol").Where(o => o.GetAttributeValue("class", "") == "mt-view-level_1").First();
			foreach (var li in ol.Elements("li"))
			{
				var a = li.Descendants("a").FirstOrDefault();
				if (a != null)
				{
					var url = a.GetAttributeValue("href", "");
					url = string.Format(videosUrl, url.Substring(url.LastIndexOf("?") + 1));

					RssLink category = new RssLink() { Url = url, Name = a.InnerText.Trim() };

					var img = li.Descendants("img").FirstOrDefault();
					if (img != null) category.Thumb = "http://mediathek.rbb-online.de" + img.GetAttributeValue("src", "");

					var span = li.Descendants("span").FirstOrDefault();
					if (span != null) category.EstimatedVideoCount = uint.Parse(span.InnerText.Substring(0, span.InnerText.IndexOf(' ')));

					Settings.Categories.Add(category);
				}
			}
			Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
			return Settings.Categories.Count;
		}

		public override bool CanSearch { get { return true; } }

		public override List<SearchResultItem> Search(string query, string category = null)
		{
			return getVideos(string.Format(searchUrl, HttpUtility.UrlEncode(query)), true, true).ConvertAll<SearchResultItem>(i => i as SearchResultItem);
		}

		public override List<VideoInfo> GetVideos(Category category)
		{
			return getVideos((category as RssLink).Url, true);
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			return getVideos(nextPageUrl, false);
		}

		List<VideoInfo> getVideos(string url, bool findOuterOL, bool fromSearch = false)
		{
			HasNextPage = false;

			var result = new List<VideoInfo>();
			var html = GetWebData<HtmlDocument>(url);

			var ol = findOuterOL ? html.DocumentNode.Descendants("ol").First() : html.DocumentNode;
			foreach (var li in ol.Elements("li"))
			{
				if (fromSearch)
				{
					var video = new RbbVideoInfo() { Title = li.Descendants("h3").First().InnerText.Trim(), HasDetails = false };
					FillVideoInfoFromLI(li, video, true);
					if (!string.IsNullOrEmpty(video.VideoUrl))
						result.Add(video);
				}
				else
				{
					var sub_ol = li.Elements("ol").FirstOrDefault();
					if (sub_ol != null)
					{
						var video = new RbbVideoInfo() { Title = li.Descendants("h3").First().InnerText.Trim() };

						var sub_lis = sub_ol.Elements("li");

						if (sub_lis.Count() == 1) // Sendung hat nur einen Beitrag
						{
							video.HasDetails = false;

							FillVideoInfoFromLI(li, video, true);

							if (!string.IsNullOrEmpty(video.VideoUrl))
								result.Add(video);
						}
						else // Sendung hat einzelne Beiträge
						{
							//video.ImageUrl = category.Thumb;
							foreach (var sub_li in sub_lis)
							{
								var subVideo = new VideoInfo() { Title = video.Title };
								FillVideoInfoFromLI(sub_li, subVideo, false);
								subVideo.Length = subVideo.Length.Replace("min", "").Trim();
								if (!string.IsNullOrEmpty(subVideo.VideoUrl))
									video.Children.Add(subVideo);
							}
							if (video.Children.Count > 0)
								result.Add(video);
						}
					}
				}
			}

			var nextPageLink = html.DocumentNode.Descendants("a").Where(a => a.InnerText == "Weiter").FirstOrDefault();
			if (nextPageLink != null)
			{
				HasNextPage = true;
				nextPageUrl = "http://mediathek.rbb-online.de" + nextPageLink.GetAttributeValue("href", "");
			}

			return result;
		}

		private static void FillVideoInfoFromLI(HtmlNode li, VideoInfo video, bool appendTitle)
		{
			var img = li.Descendants("img").FirstOrDefault();
			if (img != null) video.Thumb = "http://mediathek.rbb-online.de" + img.GetAttributeValue("src", "");

			var h3 = li.Descendants("h3").LastOrDefault();
			if (h3 != null)
			{
				var link = h3.Descendants("a").FirstOrDefault();
				if (link != null)
					video.VideoUrl = "http://mediathek.rbb-online.de" + link.GetAttributeValue("href", "");

				video.Title += " - " + h3.InnerText.Trim();
				video.Title2 = h3.InnerText.Trim();
			}

			var span = li.Descendants("span").Where(s => s.GetAttributeValue("class", "") == "mt-airtime").FirstOrDefault();
			if (span != null)
			{
				var firstSpace = span.InnerText.IndexOf(' ');
				if (firstSpace < 0)
					video.Airdate = span.InnerText;
				else
				{
					video.Airdate = span.InnerText.Substring(0, firstSpace);
					video.Length = span.InnerText.Substring(firstSpace + 1);
				}
			}
		}

		public List<VideoInfo> GetVideoChoices(VideoInfo video)
		{
			return (video as RbbVideoInfo).Children;
		}

		public override string GetVideoUrl(VideoInfo video)
		{
			//video.PlaybackOptions = new Dictionary<string, string>();

			var html = GetWebData(video.VideoUrl);
			var match = Regex.Match(html, mediaItemRegex);
			while (match.Success)
			{
				var server = match.Groups["server"].Value;
				var path = match.Groups["path"].Value;

				if (string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(path))
					return path;
					//video.PlaybackOptions.Add((video.PlaybackOptions.Count + 1).ToString(), path);

				//if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(path) && server.StartsWith("rtmp"))
					//video.PlaybackOptions.Add((video.PlaybackOptions.Count + 1).ToString(), new MPUrlSourceFilter.RtmpUrl(server) { PlayPath = path }.ToString());

				match = match.NextMatch();
			}

			throw new OnlineVideosException("No MP4 Url found! Report to Creator.");
		}
	}

	public class RbbVideoInfo : VideoInfo
	{
		public RbbVideoInfo() 
		{
			Children = new List<VideoInfo>();
		}

		public List<VideoInfo> Children { get; set; }
	}
}
