using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        protected override List<VideoInfo> Parse(string url, string data)
        {
            var res = base.Parse(url, data);
            foreach (var vid in res)
                vid.Thumb = ApplyUrlDecoding(vid.Thumb, UrlDecoding.HtmlDecode).Replace("https://", "http://");
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
                    video.PlaybackOptions.Add(htmlStream.Name, htmlStream.Value.ToString());
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}
