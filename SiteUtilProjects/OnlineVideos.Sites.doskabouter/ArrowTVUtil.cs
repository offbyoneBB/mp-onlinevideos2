using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class ArrowTVUtil : GenericSiteUtil
    {
        private string userHash = null;
        private string lastId = null;

        public override List<VideoInfo> GetVideos(Category category)
        {
            nextPageAvailable = true;
            return GetVideoList();
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            nextPageAvailable = true;
            return GetVideoList();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            var data = GetWebData(video.VideoUrl);
            video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(data, video.VideoUrl, (x, y) => y.Bandwidth.CompareTo(x.Bandwidth), (x) => x.Width + "x" + x.Height);
            return video.GetPreferredUrl(true);
        }

        private List<VideoInfo> GetVideoList()
        {
            var videoList = new List<VideoInfo>();
            JObject webData;
            if (userHash == null)
                webData = GetWebData<JObject>(@"https://arrow.tv/rock/account/register", postData: @"{""campaign"":""""}");
            else
                webData = GetWebData<JObject>(@"https://arrow.tv/rock/account/loginbyhash", postData: @"{""userhash"":""" + userHash + @""",""campaign"":"""",""from"":" + lastId + "}");

            foreach (var item in webData["playlist"])
            {
                var info = item["info"];
                if (info["isMusic"].Value<bool>())
                {
                    VideoInfo vid = new VideoInfo()
                    {
                        Title = info.Value<string>("artist") + " - " + info.Value<string>("title"),
                        VideoUrl = item["sources"][0]["file"].Value<string>(),
                        Length = TimeUtils.TimeFromSeconds(info.Value<string>("duration"))

                    };
                    videoList.Add(vid);

                    lastId = info.Value<string>("id");
                }
            }
            userHash = webData["session"]["userhash"].Value<string>();
            return videoList;
        }
    }
}
