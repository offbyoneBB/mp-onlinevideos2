using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            Match m = Regex.Match(data, @"\$\.post\('(?<url>[^']*)',");
            Match m2 = Regex.Match(data, @"'(?<key>[^']*)':'(?<value>[^']*)'");
            if (m.Success)
            {
                StringBuilder sb = new StringBuilder();
                while (m2.Success)
                {
                    if (sb.Length != 0)
                        sb.Append('&');
                    sb.Append(m2.Groups["key"].Value);
                    sb.Append('=');
                    sb.Append(m2.Groups["value"].Value);
                    m2 = m2.NextMatch();
                }

                data = GetWebData("https://api.hdv.fun" + m.Groups["url"].Value, sb.ToString());
                JArray json = JArray.Parse(data);
                string urlwithsubs = null;
                string firsturl = null;
                Dictionary<string, string> subs = new Dictionary<string, string>();
                foreach (var src in json)
                {
                    if (firsturl == null)
                        firsturl = src["src"][0].Value<String>("src");
                    if (!String.IsNullOrEmpty(subtitleLanguages) && urlwithsubs == null && src["sub"] is JObject)
                    {
                        urlwithsubs = src["src"][0].Value<String>("src");
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
                return urlwithsubs != null ? urlwithsubs : firsturl;
            }
            return null;
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
