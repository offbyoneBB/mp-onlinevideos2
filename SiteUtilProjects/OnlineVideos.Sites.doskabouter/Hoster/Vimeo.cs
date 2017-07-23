using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            var result = new SortedList<int, Tuple<string, string>>();
            Match u = Regex.Match(url, @"https?://(?:www\.)?vimeo.com/moogaloop.swf\?clip_id=(?<url>[^&]*)&");
            if (!u.Success)
                u = Regex.Match(url, @"https?://player.vimeo.com/video/(?<url>\d+)");
            if (u.Success)
                url = @"http://www.vimeo.com/" + u.Groups["url"].Value;

            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"""config_url"":(?<url>[^,]*),");
                if (n.Success)
                {
                    string deJSONified = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(n.Groups["url"].Value);
                    page = WebCache.Instance.GetWebData(deJSONified);
                    JToken jt = JObject.Parse(page) as JToken;
                    JToken video = jt["video"];
                    JToken request = jt["request"];
                    var files = request["files"]["progressive"] as JArray;

                    foreach (JToken item in files)
                    {
                        string quality = item.Value<string>("quality");
                        string vidUrl = item.Value<string>("url");

                        int i = 0;
                        while (i < quality.Length && quality[i] >= '0' && quality[i] <= '9') i++;
                        int q;
                        if (i <= 0 || !int.TryParse(quality.Substring(0, i - 1), out q)) q = 0;
                        result.Add(q, new Tuple<string, string>(quality, vidUrl));
                    }

                    if (!String.IsNullOrEmpty(subtitleLanguages))
                    {
                        string subUrl = getSubUrl(request["text_tracks"] as JArray, subtitleLanguages);
                        if (!String.IsNullOrEmpty(subUrl))
                        {
                            string data = WebCache.Instance.GetWebData(subUrl);
                            subtitleText = Helpers.SubtitleUtils.Webvtt2SRT(data);
                        }
                    }
                }
            }
            return result.ToDictionary(v => v.Value.Item1, v => v.Value.Item2);
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

