using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class BeegUtil : SiteUtilBase
    {
        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            var json = GetWebData<JObject>("http://api.beeg.com/api/v5/index/main/0/pc");
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink() { Name = "latest", Url = "http://api.beeg.com/api/v5/index/main/0/pc" });
            foreach(var jTag in json["tags"]["popular"])
            {
                Settings.Categories.Add(new RssLink() { Name = jTag.ToString(), Url = string.Format("http://api.beeg.com/api/v5/index/tag/0/pc?tag={0}", jTag.ToString()) });
            }
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return VideosFromUrl(((RssLink)category).Url);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            var json = GetWebData<JObject>(video.VideoUrl);
            video.PlaybackOptions = new Dictionary<string,string>();
            var q720 = CreateVideoUrl(json["720p"]);
            if (q720 != null) video.PlaybackOptions.Add("720p", q720);
            var q480 = CreateVideoUrl(json["480p"]);
            if (q480 != null) video.PlaybackOptions.Add("480p", q480);
            var q240 = CreateVideoUrl(json["240p"]);
            if (q240 != null) video.PlaybackOptions.Add("240p", q240);
            return video.PlaybackOptions.FirstOrDefault().Value;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return VideosFromUrl(nextPageUrl);
        }

        private List<VideoInfo> VideosFromUrl(string url)
        {
            var result = new List<VideoInfo>();
            var json = GetWebData<JObject>(url);
            foreach (var jVideo in json["videos"])
            {
                result.Add(new VideoInfo()
                {
                    Title = jVideo.Value<string>("title"),
                    Description = jVideo.Value<string>("ps_name"),
                    Thumb = string.Format("http://img.beeg.com/236x177/{0}.jpg", jVideo.Value<string>("id")),
                    VideoUrl = string.Format("http://api2.beeg.com/api/v5/video/{0}", jVideo.Value<string>("id"))
                });
            }

            var currentPage = int.Parse(Regex.Match(url, @"/(\d+)/pc").Groups[1].Value);
            var pages = int.Parse(json.Value<string>("pages"));
            HasNextPage = currentPage + 1 < pages;
            nextPageUrl = url.Replace(string.Format("/{0}/pc", currentPage), string.Format("/{0}/pc", currentPage + 1));

            return result;
        }

        private string CreateVideoUrl(JToken json)
        {
            if (json == null) return null;

            var data = json.ToString();
            var key = Regex.Match(data, "key=(.*?)%2Cend=").Groups[1].Value;

            string o = DecryptKey(key);
            data = data.Replace(key, o);
            data = data.Replace("{DATA_MARKERS}", "data=pc");
            return string.Format("http:{0}", data);
        }

        private static string DecryptKey(string key)
        {
            var a = "5ShMcIQlssOd7zChAIOlmeTZDaUxULbJRnywYaiB";
            var e = HttpUtility.UrlDecode(key);
            string o = "";
            for (int n = 0; n < e.Length; n++)
            {
                var t = (char)(e[n] - a[n % a.Length] % 21);
                o += t;
            }
            var chunks = new List<string>();
            for (int i = o.Length - 3; i > -3; i -= 3)
            {
                int j = Math.Max(i, 0);
                int s = i < 0 ? i + 3 : 3;
                chunks.Add(o.Substring(j, s));
            }
            return string.Join("", chunks.ToArray());
        }
    }
}
