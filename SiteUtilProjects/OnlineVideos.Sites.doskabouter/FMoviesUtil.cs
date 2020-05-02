using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class FMoviesUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            int p = video.VideoUrl.LastIndexOf('.');
            string id = video.VideoUrl.Substring(p + 1);
            var data = GetWebData(@"https://mcloud2.to/key",referer: video.VideoUrl);
            var m = Regex.Match(data, @"mcloudKey='(?<key>[^']*)'");
            string mccloud = "";
            if (m.Success)
                mccloud = "&mcloud=" + m.Groups["key"].Value;
            data = GetWebData(@"https://fmovies.to/ajax/film/servers/" + id);
            m = Regex.Match(data, @"<a\sclass=\\""active\\""\sdata-id=\\""(?<id>[^\\]*)\\""\shref=\\""[^""]*"">");
            if (m.Success)
            {
                var jUrl = "https://fmovies.to/ajax/episode/info?id=" + m.Groups["id"].Value + mccloud;
                JObject jData;
                try
                {
                    jData = GetWebData<JObject>(jUrl);
                }
                catch (Exception e)
                {
                    //in case of a 503
                    System.Threading.Thread.Sleep(1000);
                    jData = GetWebData<JObject>(jUrl);
                }
                string url = jData.Value<string>("target");
                string data2 = GetWebData(url, referer: video.VideoUrl);
                var m2 = Regex.Match(data2, @"var\smediaSources\s=\s\[{""file"":""(?<url>[^""]*)""}];");
                if (m2.Success)
                {
                    var m3u8Data = GetWebData(m2.Groups["url"].Value);
                    video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, m2.Groups["url"].Value, (x, y) => y.Bandwidth.CompareTo(x.Bandwidth), (x) => x.Width + "x" + x.Height);
                    string subUrl = jData.Value<string>("subtitle");
                    if (!String.IsNullOrEmpty(subUrl))
                        video.SubtitleText = Helpers.SubtitleUtils.Webvtt2SRT(GetWebData(subUrl));

                    return video.GetPreferredUrl(true);
                }
            }
            return null;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return new TrackingInfo { VideoKind = VideoKind.Movie, Title = video.Title }; ;
        }
    }
}
