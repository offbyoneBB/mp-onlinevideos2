using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Twitch API docs can be found here: https://dev.twitch.tv/docs/v5/
    /// </summary>
    public class TwitchTVUtil : SiteUtilBase
    {
        string baseApiUrl = "https://api.twitch.tv/kraken";
        string gamesUrl = "/games/top?limit=100";
        string streamsUrl = "/streams?game={0}&limit=100";
        string searchUrl = "/search/streams?query={0}&limit=25";
        string tokenUrl = "https://api.twitch.tv/api/channels/{0}/access_token";
        string playlistUrl = "http://usher.justin.tv/api/channel/hls/{0}.m3u8?allow_source=true&player=twitchweb&token={1}&segment_preference=2&sig={2}";

        string nextPageUrl;

        private NameValueCollection customHeader;

        public override int DiscoverDynamicCategories()
        {
            customHeader = new NameValueCollection();
            customHeader.Add("Client-ID", "hjmel8nfh3miwa8kwknofidpswbj45i");
            customHeader.Add("Accept", "application/vnd.twitchtv.v5+json");
            Settings.Categories.Clear();

            var games = GetWebData<JObject>(baseApiUrl + gamesUrl, headers: customHeader);
            foreach (var game in from game in games["top"] select game)
            {
                Settings.Categories.Add(CategoryFromJsonGameObject(game));
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;

            string nextCategoriesPageUrl = getNextPageUrl(baseApiUrl + gamesUrl, games.Value<int>("_total"));
            if (!string.IsNullOrEmpty(nextCategoriesPageUrl))
            {
                Settings.Categories.Add(new NextPageCategory() { Url = nextCategoriesPageUrl });
            }

            return Settings.Categories.Count - 1;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Settings.Categories.Remove(category);
            var games = GetWebData<JObject>(category.Url, headers: customHeader);
            foreach (var game in from game in games["top"] select game)
            {
                Settings.Categories.Add(CategoryFromJsonGameObject(game));
            }

            string nextCategoriesPageUrl = getNextPageUrl(category.Url, games.Value<int>("total"));
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

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            return VideosFromApiUrl(baseApiUrl + string.Format(searchUrl, HttpUtility.UrlEncode(query))).ConvertAll<SearchResultItem>(i => i as SearchResultItem);
        }

        public override string GetVideoUrl(OnlineVideos.VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            var tokenDataJson = GetWebData<JToken>(string.Format(tokenUrl, video.VideoUrl), headers: customHeader);
            var token = tokenDataJson["token"];
            var sig = tokenDataJson["sig"];
            string hlsPlaylistUrl = string.Format(playlistUrl, video.VideoUrl, HttpUtility.UrlEncode(token.ToString()), sig);
            var m3u8Data = GetWebData(hlsPlaylistUrl);
            video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, hlsPlaylistUrl);
            return video.GetPreferredUrl(false);
        }

        List<VideoInfo> VideosFromApiUrl(string url)
        {

            List<VideoInfo> result = new List<VideoInfo>();

            var streams = GetWebData<JObject>(url, headers: customHeader);
            foreach (var stream in from stream in streams["streams"] select stream)
            {
                result.Add(VideoFromJsonStreamObject(stream));
            }

            nextPageUrl = getNextPageUrl(url, streams.Value<int>("_total"));
            HasNextPage = (!string.IsNullOrEmpty(nextPageUrl));
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
                Title = (stream["channel"].Value<string>("status") ?? stream["channel"].Value<string>("display_name")).Replace('\n', ' '),
                Thumb = stream["preview"].Value<string>("large"),
                Description = string.Format("{0} Viewers on {1}", stream.Value<string>("viewers"), stream["channel"].Value<string>("name")),
                Airdate = stream["channel"].Value<DateTime>("created_at").ToString("g", OnlineVideoSettings.Instance.Locale),
                VideoUrl = stream["channel"].Value<string>("name")
            };
        }

        private string getNextPageUrl(string url, int total)
        {
            var uri = new Uri(url);
            var urlParams = HttpUtility.ParseQueryString(uri.Query);
            int offset = 0;
            int limit = 0;
            if (urlParams["offset"] != null)
                int.TryParse(urlParams["offset"], out offset);
            if (urlParams["limit"] != null)
                int.TryParse(urlParams["limit"], out limit);
            if (total > offset + limit)
            {
                urlParams["offset"] = (offset + limit).ToString();
                return uri.GetLeftPart(UriPartial.Path) + '?' + urlParams;
            }
            return String.Empty;
        }
    }
}
