using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class ToonsTvUtil : BrightCoveUtil
    {
        private Dictionary<string, JToken> contentList;
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public override int DiscoverDynamicCategories()
        {
            string data = GetWebDataFromPost(@"https://cloud.rovio.com/identity/2.0/web/access", @"clientId=ChannelWeb&persistentGuid=ChannelWeb08bea391e0169d6d9f5e2f7f805a91bc1f89c16f");
            JToken jt = JObject.Parse(data) as JToken;
            string accessToken = jt["accessToken"].Value<string>();
            data = GetWebData(@"https://cloud.rovio.com/channel/1.2/content/videos?sw=1920&sh=1080&on=web&logoImgHeight=144&accessToken=" + accessToken);
            JToken alldata = JObject.Parse(data) as JToken;
            JArray categories = alldata["categories"] as JArray;

            contentList = new Dictionary<string, JToken>();
            foreach (JToken token in alldata["content"] as JArray)
            {
                string id = token["id"].Value<string>();
                if (!id.StartsWith("comingsoon"))
                    contentList.Add(id, token);
            }


            foreach (JToken category in categories)
                Settings.Categories.Add(GetSubcat(category));
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private Category GetSubcat(JToken cat)
        {
            RssLink res = new RssLink();
            res.Name = cat["title"].Value<string>();
            JToken descr = cat["description"];
            if (descr != null)
                res.Description = descr.Value<string>();
            JToken thumb = cat["thumbnailUrl"];
            if (thumb != null)
                res.Thumb = thumb.Value<string>();
            res.Other = cat["content"];

            return res;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            JArray videos = category.Other as JArray;
            List<VideoInfo> res = new List<VideoInfo>();

            foreach (JToken videoId in videos)
            {
                VideoInfo video = new VideoInfo();
                string id = videoId.Value<string>();
                if (contentList.ContainsKey(id))
                {
                    JToken vid = contentList[id];
                    video.Title = vid["title"].Value<string>();
                    JToken descr = vid["description"];
                    if (descr != null)
                        video.Description = descr.Value<string>();
                    JToken thumb = vid["thumbnailUrl"];
                    if (thumb != null)
                        video.ImageUrl = thumb.Value<string>();
                    video.Length = TimeSpan.FromSeconds(vid["length"].Value<int>() / 1000).ToString();
                    video.Airdate = epoch.AddSeconds(vid["publishedDate"].Value<double>() / 1000).ToString();
                    video.VideoUrl = baseUrl + "channels/" + category.Name + "/" + id;
                    video.Other = id;
                    res.Add(video);
                }
            }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            Match m = Regex.Match("exp=2575783636001 " + (string)video.Other, @"exp=(?<experienceId>[^\s]+)\s(?<contentId>[^$]+)$");

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, video.VideoUrl);

            string res = FillPlaybackOptions(video, renditions);
            int firstDiff = int.MaxValue;
            int lastDiff = int.MaxValue;
            if (video.PlaybackOptions != null)
            {
                foreach (KeyValuePair<string, string> kv in video.PlaybackOptions)
                {
                    string url = kv.Value;
                    int i = 0;
                    while (i < url.Length && i < res.Length && url[i] == res[i]) i++;
                    if (i < firstDiff)
                        firstDiff = i;

                    i = 0;
                    while (i < url.Length && i < res.Length && url[url.Length - 1 - i] == res[res.Length - i - 1]) i++;
                    if (i < lastDiff)
                        lastDiff = i;
                }

                video.PlaybackOptions = video.PlaybackOptions.ToDictionary(u => u.Key, u => format(firstDiff, lastDiff, u.Value));
            }
            if (firstDiff > 0)
                return format(firstDiff, lastDiff, res);
            else
                return res;
        }

        private string format(int firstDiff, int lastDiff, string url)
        {
            string res = url.Insert(url.Length - lastDiff, ",");
            res = res.Insert(firstDiff, ",");
            return res + ".csmil/bitrate=0?v=3.1.0&fp=WIN%2011,9,900,170&r=&g=";
        }
    }
}
