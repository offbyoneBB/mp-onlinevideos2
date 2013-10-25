using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class Vimeo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Vimeo";
        }

        public override string getVideoUrls(string url)
        {
            var result = getPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.First().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            Match u = Regex.Match(url, @"http://(?:www\.)?vimeo.com/moogaloop.swf\?clip_id=(?<url>[^&]*)&");
            if (u.Success)
                url = @"http://www.vimeo.com/" + u.Groups["url"].Value;
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"data-config-url=""(?<url>[^""]*)""");
                if (n.Success)
                {
                    page = SiteUtilBase.GetWebData(HttpUtility.HtmlDecode(n.Groups["url"].Value));
                    JToken jt = JObject.Parse(page) as JToken;
                    JToken video = jt["video"];
                    JToken request = jt["request"];
                    JObject files = request["files"]["h264"] as JObject;

                    string sig = request.Value<string>("signature");
                    string timestamp = request.Value<string>("timestamp");
                    string id = video.Value<string>("id");

                    foreach (KeyValuePair<string, JToken> item in files)
                    {
                        string quality = item.Key;
                        string vidUrl = item.Value.Value<string>("url");
                        result.Add(quality, vidUrl);
                    }
                }
            }
            return result;
        }

    }
}
