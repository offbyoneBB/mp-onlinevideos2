using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnlineVideos.Hoster;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class TEDTalksUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            foreach (var cat in Settings.Categories)
                cat.Other = true;
            int res = base.DiscoverDynamicCategories();
            foreach (var cat in Settings.Categories)
                cat.HasSubCategories = (true.Equals(cat.Other));
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = GetWebData(video.VideoUrl);
            var talkDetailsMatch = regEx_FileUrl.Match(data);
            if (talkDetailsMatch.Success)
            {
                JObject talkDetails = JsonConvert.DeserializeObject(talkDetailsMatch.Groups["json"].Value) as JObject;
                foreach (JProperty htmlStream in talkDetails["__INITIAL_DATA__"]["talks"][0]["downloads"]["nativeDownloads"])
                {
                    HttpUrl httpUrl = new HttpUrl(htmlStream.Value.ToString()) { UserAgent = OnlineVideoSettings.Instance.UserAgent };
                    video.PlaybackOptions.Add(htmlStream.Name, httpUrl.ToString());
                }
                if (video.PlaybackOptions.Count == 0)
                {
                    string url = talkDetails["__INITIAL_DATA__"]["talks"][0]["player_talks"][0]["resources"]["hls"]["stream"].ToString();
                    if (string.IsNullOrEmpty(url))
                    {
                        var external = talkDetails["__INITIAL_DATA__"]["talks"][0]["player_talks"][0]["external"];
                        if (external.Value<string>("service") == "YouTube")
                        {
                            HosterBase hoster = HosterFactory.GetHoster(external.Value<string>("service"));
                            video.PlaybackOptions = hoster.GetPlaybackOptions(@"https://www.youtube.com/watch?v=" + external.Value<string>("code"));
                        }
                    }
                    else
                    {
                        var m3u8Data = GetWebData(url);
                        var options = Helpers.HlsPlaylistParser.GetPlaybackOptions(m3u8Data, url);
                        foreach (var key in options.Keys)
                        {
                            HttpUrl httpUrl = new HttpUrl(options[key]) { UserAgent = OnlineVideoSettings.Instance.UserAgent };
                            video.PlaybackOptions.Add(key, httpUrl.ToString());
                        }
                        return video.GetPreferredUrl(false);
                    }
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}
