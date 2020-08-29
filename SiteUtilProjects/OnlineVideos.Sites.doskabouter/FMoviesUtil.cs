using System;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class FMoviesUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            var data = GetWebData(video.VideoUrl);
            Match m = Regex.Match(data, @"data-ts=""(?<ts>[^""]*)""");

            int p = video.VideoUrl.LastIndexOf('.');
            string id = video.VideoUrl.Substring(p + 1);
            string ts = "";
            if (m.Success)
                ts = "&ts=" + m.Groups["ts"].Value;

            data = GetWebData(@"https://mcloud2.to/key", referer: video.VideoUrl);
            m = Regex.Match(data, @"mcloudKey='(?<key>[^']*)'");
            string mccloud = "";
            if (m.Success)
                mccloud = "&mcloud=" + m.Groups["key"].Value;
            data = GetWebData(@"https://www11.fmovies.to/ajax/film/servers?id=" + id + "&_=840" + ts);
            m = Regex.Match(data, @"<a\sclass=\\""active\\""\sdata-id=\\""(?<id>[^\\]*)\\""\shref=\\""[^""]*"">");
            if (m.Success)
            {
                var m3 = Regex.Match(data, @"div\sclass=\\""server\srow\\""\sdata-type=\\""iframe\\""\sdata-id=\\""(?<server>[^\\]*)\\""");
                string server = "";
                if (m3.Success)
                    server = @"&server=" + m3.Groups["server"].Value;
                var jUrl = "https://www11.fmovies.to/ajax/episode/info?id=" + m.Groups["id"].Value + server + mccloud + "&_=888" + ts;
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
                string url2 = url.Replace("/embed/", "/info/");

                var data2 = GetWebData<JToken>(url2, referer: url);
                if (data2.Value<bool>("success"))
                {
                    var m3u8Url = data2["media"]?["sources"][0].Value<String>("file");
                    var m3u8Data = GetWebData(m3u8Url);
                    video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, m3u8Url, (x, y) => y.Bandwidth.CompareTo(x.Bandwidth), (x) => x.Width + "x" + x.Height);
                    string subUrl = jData.Value<string>("subtitle");
                    if (String.IsNullOrEmpty(subUrl))
                    {
                        var pars = HttpUtility.ParseQueryString(url);
                        subUrl = pars["sub.info"];
                        if (!String.IsNullOrEmpty(subUrl))
                        {
                            JToken subs = null;
                            try
                            {
                                subs = GetWebData<JToken>(subUrl);
                            }
                            catch (Exception e)
                            {
                                //in case of a 502
                                System.Threading.Thread.Sleep(1000);
                                subs = GetWebData<JToken>(subUrl);
                            }

                            subUrl = null;
                            if (subs != null)
                                foreach (var subItem in subs)
                                {
                                    if (subItem.Value<String>("label") == "English")
                                    {
                                        subUrl = subItem.Value<String>("file");
                                        break;
                                    }
                                }
                        }
                    }


                    if (!String.IsNullOrEmpty(subUrl))
                    {
                        Log.Debug("Found subtitles at " + subUrl);
                        video.SubtitleText = Helpers.SubtitleUtils.Webvtt2SRT(GetWebData(subUrl));
                    }

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
