using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Twitch API docs can be found here: https://github.com/justintv/Twitch-API
	/// </summary>
    public class TwitchTVUtil : SiteUtilBase
	{
		string baseApiUrl = "https://api.twitch.tv/kraken";
		string gamesUrl = "/games/top?limit=100";
		string featuredStreamsUrl = "/streams/featured?limit=100";
		string streamsUrl = "/streams?limit=100&game={0}";
		string searchUrl = "/search/streams?limit=100&query={0}";
		string tokenUrl = "http://api.twitch.tv/api/channels/{0}/access_token";
		string playlistUrl = "http://usher.twitch.tv/select/{0}.m3u8?nauthsig={1}&nauth={2}&allow_source=true";
		string swfUrl = "http://www-cdn.jtvnw.net/widgets/live_site_player.reecf0cca00fdb5cb6edc8e227c91702545504613.swf";
		string pageUrlBase = "http://de.twitch.tv/";
		string m3u8Regex = @"#EXT-X-STREAM-INF:PROGRAM-ID=\d,BANDWIDTH=(?<bitrate>\d+),(RESOLUTION=\d+x\d+,)?VIDEO=""(?<quality>[^""]+)""\n(?<url>.*)";

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

		public override List<VideoInfo> GetVideos(Category category)
		{
			return VideosFromApiUrl(baseApiUrl + string.Format(streamsUrl, HttpUtility.UrlEncode(category.Name)));
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			return VideosFromApiUrl(nextPageUrl);
		}

		public override bool CanSearch
		{
			get { return true; }
		}

		public override List<ISearchResultItem> Search(string query, string category = null)
		{
			return VideosFromApiUrl(baseApiUrl + string.Format(searchUrl, HttpUtility.UrlEncode(query))).ConvertAll<ISearchResultItem>(i => i as ISearchResultItem);
		}

		public override string GetVideoUrl(OnlineVideos.VideoInfo video)
		{
			video.PlaybackOptions = new Dictionary<string, string>();

			var tokenDataJson = GetWebData<JToken>(string.Format(tokenUrl, video.VideoUrl));
			var token= tokenDataJson["token"];
			var sig = tokenDataJson["sig"];
			var m3u8Data = GetWebData(string.Format(playlistUrl, video.VideoUrl, sig, token));
			foreach (Match match in Regex.Matches(m3u8Data, m3u8Regex))
			{
				video.PlaybackOptions.Add(
					string.Format("{0} - {1} kbps", match.Groups["quality"].Value, int.Parse(match.Groups["bitrate"].Value) / 1000),
					match.Groups["url"].Value);
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
                ImageUrl = stream["preview"].Value<string>("large"),
				Description = string.Format("{0} Viewers on {1}", stream.Value<string>("viewers"), stream["channel"].Value<string>("name")),
				Airdate = stream["channel"].Value<DateTime>("created_at").ToString("g", OnlineVideoSettings.Instance.Locale),
				VideoUrl = stream["channel"].Value<string>("name")
			};
		}
	}
}
