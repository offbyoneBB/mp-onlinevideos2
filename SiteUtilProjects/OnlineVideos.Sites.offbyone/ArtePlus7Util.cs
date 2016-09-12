using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
	public class ArtePlus7Util : SiteUtilBase
	{
		public enum VideoQuality { HD, MD, SD, LD };
		public enum Language { DE, FR };

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the preferred quality for the video to be played.")]
		VideoQuality videoQuality = VideoQuality.HD;
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Language", TranslationFieldName = "Language"), Description("Arte offers their programm in German and French.")]
		Language language = Language.DE;

		const string BASE_URL = "http://www.arte.tv/guide/{0}/plus7";
		string baseUrl;
        string nextPageUrl;

		public override void Initialize(SiteSettings siteSettings)
		{
			base.Initialize(siteSettings);

			baseUrl = string.Format(BASE_URL, language.ToString().ToLower());
		}

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Clear();

			// load homepage
			var doc = GetWebData<HtmlDocument>(baseUrl);
            var json = JsonFromScriptBlock(doc.DocumentNode, @"\(function\(\)\s*\{\s*var\s*element\s*=\s*React\.createElement\(HomePage,\s*(?<json>\{.*?})\);");

			// build categories for the themes
			Category categoriesCategory = new Category() { HasSubCategories = true, SubCategoriesDiscovered = true, Name = "Themen", SubCategories = new List<Category>() };
			foreach (var jCategory in json["categoriesVideos"] as JArray)
			{
				var categorySubNode = jCategory["category"];
				categoriesCategory.SubCategories.Add(new RssLink()
				{
					ParentCategory = categoriesCategory,
					EstimatedVideoCount = jCategory.Value<uint>("total_count"),
					Name = categorySubNode.Value<string>("name"),
					Description = categorySubNode.Value<string>("description"),
					Url = string.Format("{0}/videos?category={1}&page=1&limit=24&sort=newest", baseUrl, categorySubNode.Value<string>("code"))
				});
			}
			if (categoriesCategory.SubCategories.Count > 0) Settings.Categories.Add(categoriesCategory);

			// build categories for the shows
			Category showsCategory = new Category() { HasSubCategories = true, SubCategoriesDiscovered = true, Name = "Sendungen", SubCategories = new List<Category>() };
			foreach (var jCategory in json["clusters"] as JArray)
			{
				showsCategory.SubCategories.Add(new RssLink()
				{
					ParentCategory = showsCategory,
					Name = jCategory.Value<string>("title"),
					Description = jCategory.Value<string>("subtitle"),
					Url = string.Format("{0}/videos?cluster={1}&page=1&limit=24&sort=newest", baseUrl, jCategory.Value<string>("id"))
				});
			}
			if (showsCategory.SubCategories.Count > 0) Settings.Categories.Add(showsCategory);


			// build categories for the last 7 days
			Category dailyCategory = new Category() { HasSubCategories = true, SubCategoriesDiscovered = true, Name = "Letzte 7 Tage", SubCategories = new List<Category>() };
			for (int i = 0; i > -7; i--)
			{
				dailyCategory.SubCategories.Add(new RssLink()
				{
					ParentCategory = dailyCategory,
					Name = DateTime.Today.AddDays(i - 1).ToShortDateString(),
					Url = string.Format("{0}/videos?day={1}&page=1&limit=24&sort=newest", baseUrl, i)
				});
			}
			Settings.Categories.Add(dailyCategory);

			// build additional categories also found on homepage
			Settings.Categories.Add(new RssLink() { Name = "Neueste Videos", Url = string.Format("{0}/videos?page=1&limit=24&sort=newest", baseUrl) });
			Settings.Categories.Add(new RssLink() { Name = "Meistgesehen", Url = string.Format("{0}/videos?page=1&limit=24&sort=most_viewed", baseUrl) });
			Settings.Categories.Add(new RssLink() { Name = "Letzte Chance", Url = string.Format("{0}/videos?page=1&limit=24&sort=next_expiring", baseUrl) });

			Settings.DynamicCategoriesDiscovered = true;
			return Settings.Categories.Count;
		}

		public override List<VideoInfo> GetVideos(Category category)
		{
			return VideosFromJson(((RssLink)category).Url);
		}

		public override String GetVideoUrl(VideoInfo video)
		{
			video.PlaybackOptions = new Dictionary<string, string>();

			var doc = GetWebData<HtmlDocument>(video.VideoUrl);
            var vpDiv = doc.DocumentNode.Descendants("div").FirstOrDefault(s => !string.IsNullOrEmpty(s.GetAttributeValue("arte_vp_url_oembed", "")));

			if (vpDiv == null)
				throw new OnlineVideosException("Video nicht verfügbar!");

			var json = GetWebData<JObject>(vpDiv.GetAttributeValue("arte_vp_url_oembed", ""));
            HtmlDocument iframe = new HtmlAgilityPack.HtmlDocument();
            iframe.LoadHtml(json["html"].ToString());
            json = GetWebData<JObject>(HttpUtility.ParseQueryString(new Uri(iframe.DocumentNode.FirstChild.GetAttributeValue("src", "")).Query)["json_url"]);            

			foreach (var quality in json["videoJsonPlayer"]["VSR"])
			{
				string qualityName = string.Format("{0} | {1} | {2} ({3}x{4} - {5} kbps)",
					(quality.First.Value<string>("versionShortLibelle") ?? "").PadRight(3),
					quality.First.Value<string>("mediaType").PadRight(4),
					quality.First.Value<string>("quality"),
                    quality.First.Value<string>("width"),
                    quality.First.Value<string>("height"),
                    quality.First.Value<string>("bitrate"));

				if (quality.First.Value<string>("mediaType") == "rtmp")
				{
					if (!video.PlaybackOptions.ContainsKey(qualityName))
					{
						string host = quality.First.Value<string>("streamer");
						string file = quality.First.Value<string>("url");
						string playbackUrl = new MPUrlSourceFilter.RtmpUrl(host) { TcUrl = host, PlayPath = "mp4:" + file }.ToString();
						video.PlaybackOptions.Add(qualityName, playbackUrl);
					}
				}
				else if (quality.First.Value<string>("mediaType") == "mp4")
				{
					string file = quality.First.Value<string>("url");
					video.PlaybackOptions.Add(qualityName, file);
				}
				/*else if (quality.First.Value<string>("mediaType") == "hls")
				{
					string file = quality.First.Value<string>("url");
					video.PlaybackOptions.Add(qualityName, file);
					// todo -> resolve m3u8
				}*/
			}

			var bestOption = video.PlaybackOptions.FirstOrDefault(q => q.Key.Contains(videoQuality.ToString())).Value;
			if (string.IsNullOrEmpty(bestOption)) return video.PlaybackOptions.FirstOrDefault().Value;
			return bestOption;
		}

		private List<VideoInfo> VideosFromJson(string url)
		{
			var result = new List<VideoInfo>();
			var json = GetWebData<JObject>(url);
            foreach (JObject jVideo in json["videos"] ?? json["programs"])
			{
				var video = new VideoInfo()
				{
					Title = jVideo.Value<string>("title"),
					Description = jVideo.Value<string>("teaser"),
					Length = string.Format("{0} min", (int)Math.Round(TimeSpan.FromSeconds(jVideo.Value<int>("duration")).TotalMinutes)),
					Airdate = jVideo.Value<string>("scheduled_on"),
					VideoUrl = jVideo.Value<string>("url"),
					Thumb = ((JArray)jVideo["thumbnails"])[((JArray)jVideo["thumbnails"]).Count / 2].Value<string>("url")
				};
				var subtitle = jVideo.Value<string>("subtitle");
				if (!string.IsNullOrEmpty(subtitle)) video.Title += " - " + subtitle;
				result.Add(video);
			}

            HasNextPage = json.Value<bool>("has_more");
            if (HasNextPage)
            {
                var thisPageUri = new Uri(url);
                var queryParams = HttpUtility.ParseQueryString(thisPageUri.Query);
                queryParams["page"] = (int.Parse(queryParams["page"]) + 1).ToString();
                nextPageUrl = new Uri(thisPageUri, thisPageUri.GetLeftPart(UriPartial.Path) + "?" + queryParams).AbsoluteUri;
            }
            else
            {
                nextPageUrl = null;
            }

			return result;
		}

		private JObject JsonFromScriptBlock(HtmlNode node, string require)
		{
            var regex = new Regex(require, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);

            foreach(var scriptNode in node.Descendants("script"))
            {
                var match = regex.Match(scriptNode.InnerText);
                if (match.Success)
                {
                    string json = match.Groups["json"].Value;
                    return JObject.Parse(json);
                }
            }
            throw new OnlineVideosException("Site changed - Please report as broken!");
		}

        public override List<VideoInfo> GetNextPageVideos()
        {
            return VideosFromJson(nextPageUrl);
        }

		public override bool CanSearch { get { return true; } }

		public override List<SearchResultItem> Search(string query, string category = null)
		{
			return VideosFromJson(string.Format("http://www.arte.tv/guide/{0}/programs?q={1}&scope=plus7", language.ToString().ToLower(), HttpUtility.UrlEncode(query)))
				.ConvertAll<SearchResultItem>(v => (SearchResultItem)v);
		}
	}
}