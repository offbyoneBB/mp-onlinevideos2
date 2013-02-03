using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                Match n = Regex.Match(page, @"document.getElementById\('player[^=]*=(?<json>.*?);Player.checkRatio", RegexOptions.Singleline);
                if (n.Success)
                {
                    JToken jt = JObject.Parse(n.Groups["json"].Value) as JToken;
                    JToken video = jt["config"]["video"];
                    JToken request = jt["config"]["request"];
                    JArray files = video["files"]["h264"] as JArray;

                    string sig = request.Value<string>("signature");
                    string timestamp = request.Value<string>("timestamp");
                    string id = video.Value<string>("id");

                    foreach (JToken item in files)
                    {
                        string quality = item.Value<string>();
                        string vidUrl = String.Format(@"http://player.vimeo.com/play_redirect?clip_id={0}&sig={1}&time={2}&codecs=H264,VP8,VP6&quality={3}",
                            id, sig, timestamp, quality);
                        result.Add(quality, getRedirect(vidUrl));
                    }
                }
            }
            return result;
        }

        private string getRedirect(string url)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.AllowAutoRedirect = false;
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                request.Timeout = 15000;
                httpWebresponse = request.GetResponse() as HttpWebResponse;
                if (httpWebresponse == null) return url;
                return httpWebresponse.GetResponseHeader("Location");
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            if (httpWebresponse != null) httpWebresponse.Close();
            return url;
        }
    }
}
