using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class DailyMotionUtil : SiteUtilBase
    {
        string api_base_url = "https://api.dailymotion.com";
        string api_channel_list_url = "/channels?sort=popular";
        string api_channel_videos_url = "/channel/{0}/videos?fields=created_time,description%2Cduration%2Cembed_url%2Cthumbnail_240_url%2Ctitle&limit=50&page={1}";
        string api_channel_videos_search_url = "/channel/{0}/videos?fields=created_time,description%2Cduration%2Cembed_url%2Cthumbnail_240_url%2Ctitle&limit=50&search={1}&page={2}";
        string api_video_search_url = "/videos?fields=created_time,description%2Cduration%2Cembed_url%2Cthumbnail_240_url%2Ctitle&limit=50&search={0}&page={1}";

        int current_videos_page = 1;
        string current_videos_url = "";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            foreach (JObject jChannel in GetWebData<JObject>(api_base_url + api_channel_list_url)["list"])
            {
                Settings.Categories.Add(new RssLink() 
                { 
                    Name = jChannel.Value<string>("name"),
                    Description = jChannel.Value<string>("description"),
                    Url = jChannel.Value<string>("id")
                });
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return VideosFromJson(api_base_url + string.Format(api_channel_videos_url, (category as RssLink).Url, 1));
        }

        public override bool CanSearch { get { return true; } }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            return Settings.Categories.ToDictionary(c => c.Name, c => ((RssLink)c).Url);
        }

        public override List<VideoInfo> Search(string query)
        {
            return VideosFromJson(api_base_url + string.Format(api_video_search_url, HttpUtility.UrlEncode(query), 1));
        }

        public override List<VideoInfo> Search(string query, string category)
        {
            return VideosFromJson(api_base_url + string.Format(api_channel_videos_search_url, category, HttpUtility.UrlEncode(query), 1));
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return VideosFromJson(current_videos_url.Replace("&page=" + current_videos_page, "&page=" + (current_videos_page + 1).ToString()));
        }

        public override string getUrl(VideoInfo video)
        {
            var json = JObject.Parse(Regex.Match(GetWebData(video.VideoUrl), "var info = (?<json>{.*),").Groups["json"].Value);

            video.PlaybackOptions = new Dictionary<string,string>();
            
            var url = json.Value<string>("stream_h264_ld_url");
            if (!string.IsNullOrEmpty(url))
                video.PlaybackOptions.Add("Low - 320x240", url);
            
            url = json.Value<string>("stream_h264_url");
            if (!string.IsNullOrEmpty(url))
                video.PlaybackOptions.Add("Normal - 512x384", url);

            url = json.Value<string>("stream_h264_hq_url");
            if (!string.IsNullOrEmpty(url))
                video.PlaybackOptions.Add("High - 848x480", url);

            url = json.Value<string>("stream_h264_hd_url");
            if (!string.IsNullOrEmpty(url))
                video.PlaybackOptions.Add("HD - 1280x720", url);

            url = json.Value<string>("stream_h264_hd1080_url");
            if (!string.IsNullOrEmpty(url))
                video.PlaybackOptions.Add("Full HD - 1920x1080", url);

            return video.PlaybackOptions.Last().Value;
        }

        List<VideoInfo> VideosFromJson(string url)
        {
            var result = new List<VideoInfo>();
            
            JObject json = GetWebData<JObject>(url);
            
            HasNextPage = json.Value<bool>("has_more");
            current_videos_page = json.Value<int>("page");
            current_videos_url = url;

            foreach (JObject jVideo in json["list"])
            {
                result.Add(new VideoInfo()
                {
                    Title = jVideo.Value<string>("title"),
                    Description = jVideo.Value<string>("description"),
                    Airdate = Utils.UNIXTimeToDateTime(jVideo.Value<double>("created_time")).ToString("g", OnlineVideoSettings.Instance.Locale),
                    Length = jVideo.Value<string>("duration"),
                    ImageUrl = jVideo.Value<string>("thumbnail_240_url"),
                    VideoUrl = jVideo.Value<string>("embed_url")
                });
            }
            return result;
        }
    }
}
