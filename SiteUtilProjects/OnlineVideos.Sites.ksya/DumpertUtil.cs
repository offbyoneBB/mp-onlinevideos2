using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class DumpertUtil : GenericSiteUtil
    {
        public override List<VideoInfo> GetVideos(Category category)
        {
            return parse(((RssLink)category).Url);
        }

        private  List<VideoInfo> parse(string url)
        {
            string[] parts = url.Split('/');
            int pageInd = parts.Length - 2;
            int pageNr = Int32.Parse(parts[pageInd]);
            parts[pageInd] = (++pageNr).ToString();

            nextPageUrl = string.Join("/", parts);

            var jsonData = GetWebData<JObject>(url);
            var items = jsonData.Value<JArray>("items");

            List<VideoInfo> videoList = new List<VideoInfo>();

            foreach (var item in items)
            {
                VideoInfo videoInfo = CreateVideoInfo();
                videoInfo.Title = item.Value<string>("title");
                var media = item.Value<JArray>("media");
                var variants = media[0].Value<JArray>("variants");
                if (variants[0].Value<string>("uri").StartsWith("youtube:"))
                {
                    videoInfo.Other = variants[0].Value<string>("uri").Split(':')[1];
                }
                else
                if (variants.Count > 1)
                {
                    Dictionary<string, string> vidUrls = new Dictionary<string, string>();
                    foreach (var variant in variants)
                        vidUrls.Add(variant.Value<string>("version"), variant.Value<string>("uri"));
                    videoInfo.PlaybackOptions = new Dictionary<string, string>();

                    //sort from high to low quality: 720p, tablet, flv, mobile
                    if (vidUrls.ContainsKey("720p"))
                    {
                        videoInfo.PlaybackOptions.Add("720p", vidUrls["720p"]);
                    }
                    if (vidUrls.ContainsKey("tablet"))
                    {
                        videoInfo.PlaybackOptions.Add("High", vidUrls["tablet"]);
                    }
                    if (vidUrls.ContainsKey("flv"))
                    {
                        videoInfo.PlaybackOptions.Add("Medium", vidUrls["flv"]);
                    }
                    if (vidUrls.ContainsKey("mobile"))
                    {
                        videoInfo.PlaybackOptions.Add("Low", vidUrls["mobile"]);
                    }
                }
                videoInfo.Thumb = item.Value<string>("thumbnail");
                videoInfo.Length = Helpers.TimeUtils.TimeFromSeconds(media[0].Value<string>("duration"));
                videoInfo.Airdate = item.Value<DateTime>("date").ToString("g");
                videoInfo.Description = item.Value<string>("description");
                videoList.Add(videoInfo);
            }
            nextPageAvailable = videoList.Count > 0;
            return videoList;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return parse(nextPageUrl);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string youtubeId = video.Other as string;
            if (!String.IsNullOrEmpty(youtubeId))
            {
                var hoster = Hoster.HosterFactory.GetHoster("Youtube");
                if (hoster != null)
                    video.PlaybackOptions = hoster.GetPlaybackOptions("https://www.youtube.com/watch?v=" + youtubeId);
                return video.GetPreferredUrl(false);
            }
            return video.GetPreferredUrl(true);
        }
    }
}
