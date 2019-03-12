using System;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class NPOLiveUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType) 3072|SecurityProtocolType.Ssl3|SecurityProtocolType.Tls;

            string newToken = Doskabouter.Helpers.NPOHelper.GetToken(video.VideoUrl);
            if (!String.IsNullOrEmpty(newToken))
            {
                var jsonData = GetWebData<JObject>(video.VideoUrl + newToken);
                string url = jsonData["items"][0][0].Value<String>("url");
                string webData = GetWebData(url);
                Match m = Regex.Match(webData, @"setSource\((?<url>[^\)]*)\)");
                if (!m.Success)
                    return String.Empty;

                string deJSONified = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(m.Groups["url"].Value);
                webData = GetWebData(deJSONified);
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(webData, deJSONified);
                return video.GetPreferredUrl(false);
            }
            return String.Empty;
        }

    }
}
