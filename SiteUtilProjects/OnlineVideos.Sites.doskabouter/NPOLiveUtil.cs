using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class NPOLiveUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            string newToken = Doskabouter.Helpers.NPOHelper.GetToken(video.VideoUrl);
            if (!String.IsNullOrEmpty(newToken))
            {
                string webData = GetWebData(String.Format(@"http://ida.omroep.nl/aapi/?type=jsonp&stream={0}&token=", HttpUtility.UrlEncode(video.VideoUrl)) + newToken);
                JObject contentData = (JObject)JObject.Parse(webData);
                string url = contentData["stream"].Value<String>();
                webData = GetWebData(url);
                Match m = Regex.Match(webData, @"setSource\((?<url>[^\)]*)\)");
                if (!m.Success)
                    return String.Empty;

                string deJSONified = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(m.Groups["url"].Value);
                webData = GetWebData(deJSONified);
                m = Regex.Match(webData, @"\#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=(?<bw>[^,]*),CODECS=""[^""]*"",RESOLUTION=(?<reso>[^\s]*)\s*(?<url>.*)");
                video.PlaybackOptions = new Dictionary<string, string>();
                while (m.Success)
                {
                    string newurl = m.Groups["url"].Value;
                    url = FormatDecodeAbsolutifyUrl(deJSONified, newurl, "", UrlDecoding.None);
                    video.PlaybackOptions.Add(m.Groups["reso"].Value, url);
                    m = m.NextMatch();
                }
                return video.PlaybackOptions.Values.LastOrDefault<String>();
            }
            return String.Empty;
        }

    }
}
