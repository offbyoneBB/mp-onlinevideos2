using System;
using System.Text.RegularExpressions;
using OnlineVideos.MPUrlSourceFilter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class iLiveToUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            Match m = Regex.Match(webData, fileUrlRegEx, defaultRegexOptions);
            if (m.Success)
            {
                Match tokenMatch = Regex.Match(webData, @"getJSON\(""(?<url>[^""]*)"",\sfunction", defaultRegexOptions);
                if (tokenMatch.Success)
                {
                    string tokenData = GetWebData(tokenMatch.Groups["url"].Value, referer: video.VideoUrl);
                    JToken token = JToken.Parse(tokenData);

                    RtmpUrl result = new RtmpUrl(deJSON(m.Groups["rtmpurl"].Value));
                    result.PageUrl = video.VideoUrl;
                    result.SwfUrl = deJSON(m.Groups["swfurl"].Value);
                    result.PlayPath = deJSON(m.Groups["playpath"].Value);
                    result.App = "edge/_definst_/?" + deJSON(m.Groups["app"].Value);
                    result.Token = token.Value<string>("token");
                    return result.ToString();
                }
            }
            return String.Empty;
        }

        private string deJSON(string url)
        {
            try
            {
                string deJSONified = JsonConvert.DeserializeObject<string>('"' + url + '"');
                if (!string.IsNullOrEmpty(deJSONified)) return deJSONified;
            }
            catch { }
            return url;
        }
    }
}
