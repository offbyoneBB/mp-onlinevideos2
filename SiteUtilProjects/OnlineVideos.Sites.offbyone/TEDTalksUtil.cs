using System.Collections.Generic;
using System.Linq;
using OnlineVideos._3rdParty.Newtonsoft.Json;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

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
                foreach (JProperty htmlStream in talkDetails["talks"][0]["nativeDownloads"])
                {
                    video.PlaybackOptions.Add(htmlStream.Name, htmlStream.Value.ToString());
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}
