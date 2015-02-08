using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class Vimeo : HosterBase, ISubtitle
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 3166-2?), for example: en;de")]
        protected string subtitleLanguages = "";

        private string subtitleText = null;

        public override string GetHosterUrl()
        {
            return "Vimeo";
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.First().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleText = null;
            Dictionary<string, string> result = new Dictionary<string, string>();
            Match u = Regex.Match(url, @"http://(?:www\.)?vimeo.com/moogaloop.swf\?clip_id=(?<url>[^&]*)&");
            if (!u.Success)
                u = Regex.Match(url, @"http://player.vimeo.com/video/(?<url>\d+)");
            if (u.Success)
                url = @"http://www.vimeo.com/" + u.Groups["url"].Value;

            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"data-config-url=""(?<url>[^""]*)""");
                if (n.Success)
                {
                    page = WebCache.Instance.GetWebData(HttpUtility.HtmlDecode(n.Groups["url"].Value));
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

                    if (!String.IsNullOrEmpty(subtitleLanguages))
                    {
                        string data = WebCache.Instance.GetWebData(getSubUrl(request["text_tracks"] as JArray, subtitleLanguages));
                        subtitleText = Helpers.SubtitleUtils.Webvtt2SRT(data);
                    }
                }
            }
            return result;
        }

        private string getSubUrl(JArray textTracks, string languages)
        {
            if (textTracks != null)
            {
                string[] langs = languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string lang in langs)
                    foreach (JToken textTrack in textTracks)
                        if (lang == textTrack.Value<string>("lang"))
                            return @"http:" + textTrack.Value<string>("direct_url");
            }
            return null;
        }

        string ISubtitle.SubtitleText
        {
            get
            {
                return subtitleText;
            }
        }
    }

}

