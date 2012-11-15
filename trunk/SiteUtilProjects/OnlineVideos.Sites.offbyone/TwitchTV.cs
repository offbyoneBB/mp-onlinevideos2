using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Globalization;

namespace OnlineVideos.Sites
{
	public class TwitchTV : SiteUtilBase
	{
		string baseApiUrl = "https://api.twitch.tv/kraken";
		string gamesUrl = "/games/top?limit=100";
		string streamsUrl = "/streams?limit&game={0}";
		string searchUrl = "/search/streams?limit=100&query={0}";
		string metaInfoUrl = "http://usher.twitch.tv/find/{0}.xml?type=any";

		string nextPageUrl;

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Clear();
	
			var games = GetWebData<JObject>(baseApiUrl + gamesUrl);
			foreach (var game in from game in games["top"] select game)
			{
				Settings.Categories.Add(CategoryFromJsonGameObject(game));
			}
			Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;

			string nextCategoriesPageUrl = games["_links"].Value<string>("next");
			if (!string.IsNullOrEmpty(nextCategoriesPageUrl))
			{
				Settings.Categories.Add(new NextPageCategory() { Url = nextCategoriesPageUrl });
			}

			return Settings.Categories.Count - 1;
		}

		public override int DiscoverNextPageCategories(NextPageCategory category)
		{
			Settings.Categories.Remove(category);
			var games = GetWebData<JObject>(category.Url);
			foreach (var game in from game in games["top"] select game)
			{
				Settings.Categories.Add(CategoryFromJsonGameObject(game));
			}

			string nextCategoriesPageUrl = games["_links"].Value<string>("next");
			if (!string.IsNullOrEmpty(nextCategoriesPageUrl))
			{
				Settings.Categories.Add(new NextPageCategory() { Url = nextCategoriesPageUrl });
			}

			return Settings.Categories.Count - 1;
		}

		public override List<VideoInfo> getVideoList(Category category)
		{
			return VideosFromApiUrl(baseApiUrl + string.Format(streamsUrl, HttpUtility.UrlEncode(category.Name)));
		}

		public override List<VideoInfo> getNextPageVideos()
		{
			return VideosFromApiUrl(nextPageUrl);
		}

		public override bool CanSearch
		{
			get { return true; }
		}

		public override List<ISearchResultItem> DoSearch(string query)
		{
			return VideosFromApiUrl(baseApiUrl + string.Format(searchUrl, HttpUtility.UrlEncode(query))).ConvertAll<ISearchResultItem>(i => i as ISearchResultItem);
		}

		public override string getUrl(OnlineVideos.VideoInfo video)
		{
			video.PlaybackOptions = new Dictionary<string, string>();
			var metaDocText = GetWebData(string.Format(metaInfoUrl, video.VideoUrl));
			var fixedDocText = Regex.Replace(metaDocText, @"(?<start></?)(?<digit>\d)", @"${start}_${digit}");
			var doc = XDocument.Parse(fixedDocText);
			foreach (var quali in doc.Element("nodes").Elements())
			{
				video.PlaybackOptions.Add(
					string.Format("{0:F0} kbps | {1}p", double.Parse(quali.Element("bitrate").Value, CultureInfo.InvariantCulture), quali.Element("video_height").Value),
					new MPUrlSourceFilter.RtmpUrl(quali.Element("connect").Value) { Live= true, Subscribe = quali.Element("play").Value, Jtv = quali.Element("token").Value }.ToString());
			}
			return video.PlaybackOptions.Select(p => p.Value).FirstOrDefault();
		}

		List<VideoInfo> VideosFromApiUrl(string url)
		{
			nextPageUrl = string.Empty;
			HasNextPage = false;

			List<VideoInfo> result = new List<VideoInfo>();

			var streams = GetWebData<JObject>(url);
			foreach (var stream in from stream in streams["streams"] select stream)
			{
				result.Add(VideoFromJsonStreamObject(stream));
			}

			nextPageUrl = streams["_links"].Value<string>("next");
			if (!string.IsNullOrEmpty(nextPageUrl)) HasNextPage = true;

			return result;
		}

		Category CategoryFromJsonGameObject(JToken game)
		{
			return new Category()
			{
				Name = game["game"].Value<string>("name"),
				Thumb = game["game"]["box"].Value<string>("medium"),
				Description = string.Format("Channels: {0} / Viewers: {1}", game.Value<string>("channels"), game.Value<string>("viewers"))
			};
		}

		VideoInfo VideoFromJsonStreamObject(JToken stream)
		{
			return new VideoInfo()
			{
				Title = (stream["channel"].Value<string>("status") ?? stream["channel"].Value<string>("display_name")).Replace('\n', ' ' ),
				ImageUrl = stream.Value<string>("preview"),
				Description = string.Format("{0} Viewers on {1}", stream.Value<string>("viewers"), stream["channel"].Value<string>("name")),
				Airdate = stream["channel"].Value<DateTime>("created_at").ToString("g", OnlineVideoSettings.Instance.Locale),
				VideoUrl = stream["channel"].Value<string>("name")
			};
		}
	}
}
