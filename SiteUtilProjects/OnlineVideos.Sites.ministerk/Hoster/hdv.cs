using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class hdv : HosterBase, ISubtitle
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and full name), for example: english;danish")]
        protected string subtitleLanguages = "";

        private string subtitleText = null;

        public override string GetHosterUrl()
        {
            return "hdv.fun";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Match m = Regex.Match(url, @"/(?<imdbid>[^/]+)$");
            if (m.Success)
            {
                var data = GetWebData("https://eb2.srtaem.casa/l1", "imdb=" + m.Groups["imdbid"].Value);
                JArray json = JArray.Parse(data);
                string urlwithsubs = null;
                string firsturl = null;
                Dictionary<string, string> subs = new Dictionary<string, string>();
                foreach (var src in json)
                {
                    if (firsturl == null)
                    {
                        firsturl = src["src"][0].Value<String>("src");
                        if (!firsturl.StartsWith("http"))
                            firsturl = "https:" + firsturl;
                    }
                    if (!String.IsNullOrEmpty(subtitleLanguages) && urlwithsubs == null && src["sub"] is JObject)
                    {
                        urlwithsubs = src["src"][0].Value<String>("src");
                        if (!urlwithsubs.StartsWith("http"))
                            urlwithsubs = "https:" + urlwithsubs;

                        foreach (var sub in src["sub"].Children())
                        {
                            string lang = sub.First.Value<string>("lg");
                            if (!subs.ContainsKey(lang))
                                subs.Add(lang, sub.First.Value<string>("sub_id"));
                        }
                    }
                }
                if (subs.Count > 0)
                {
                    string subUrl = getSubUrl(subs, subtitleLanguages);
                    if (!String.IsNullOrEmpty(subUrl))
                    {
                        string subData = WebCache.Instance.GetWebData(subUrl);
                        subtitleText = Helpers.SubtitleUtils.Webvtt2SRT(subData);
                    }
                }

                string finalUrl = urlwithsubs != null ? urlwithsubs : firsturl;
                data = GetWebData(finalUrl);
                var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(data, finalUrl);
                if (res.Count == 1 && res.First().Key == @"0x0 (0 Kbps)")
                {
                    res.Clear();
                    res.Add("dummy", finalUrl);
                }
                return res;
            }
            return null;
        }
        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        private string getSubUrl(Dictionary<string, string> subs, string languages)
        {
            string[] langs = languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string lang in langs)
            {
                if (subs.ContainsKey(lang))
                    return "https://sub1.hdv.fun/vtt1/" + subs[lang] + ".vtt";
            }
            return null;
        }


        public string SubtitleText
        {
            get { return subtitleText; }
        }

    }
}
