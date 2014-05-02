using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class vk : HosterBase
    {
        public override string getHosterUrl()
        {
            return "vk.com";
        }

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            string data = Sites.SiteUtilBase.GetWebData(url) ?? "";
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

            Regex rgx = new Regex(@"var vars = (.*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                var json = JObject.Parse(m.Groups[1].Value);
                rgx = new Regex(@"url([0-9]+)");
                int res;
                foreach (JToken token in json.Descendants())
                {
                    JProperty property = token as JProperty;
                    if (property != null)
                    {
                        m = rgx.Match(property.Name);
                        if (m.Success)
                        {
                            res = int.Parse(m.Groups[1].Value);
                            url = ((string)json[string.Format("url{0}", res)]).Replace("https://","http://");
                            pairs.Add(new KeyValuePair<string, string>(string.Format("{0}p", res), url));
                        }
                    }
                }
            }
            var sorted = from pair in pairs
                         orderby pair.Key descending
                         select pair;
            foreach (var pair in sorted)
            {
                playbackOptions.Add(pair.Key, pair.Value);
            }
            return playbackOptions;
        }

        public override string getVideoUrls(string url)
        {
            Dictionary<string, string> urls = getPlaybackOptions(url);
            if (urls.Count > 0)
                return urls.First().Value;
            else
                return "";
        }
    }
}
