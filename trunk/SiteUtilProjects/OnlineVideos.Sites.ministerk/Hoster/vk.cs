using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class vk : HosterBase
    {
    
        public override string GetHosterUrl()
        {
            return "vk.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Regex rgx;
            Match m;
            if (url.Contains("vk.php?v="))
            {
                rgx = new Regex(@"v=(?<url>.*)");
                m = rgx.Match(url);
                if (m.Success)
                {
                    url = m.Groups["url"].Value;
                    url = HttpUtility.UrlDecode(url);
                }
            }
            string data = WebCache.Instance.GetWebData(HttpUtility.HtmlDecode(url)) ?? "";
            //location.href
            rgx = new Regex(@"location.href(?:[^""]*)""(?<url>[^""]*)");
            m = rgx.Match(data);
            if (m.Success)
            {
                string url2 = m.Groups["url"].Value.Replace("'", string.Empty).Replace("+", string.Empty);
                data = WebCache.Instance.GetWebData(HttpUtility.HtmlDecode(url2), referer: url) ?? "";
                m = rgx.Match(data);
                if (m.Success)
                {
                    string url3 = m.Groups["url"].Value.Replace("'", string.Empty).Replace("+", string.Empty);
                    data = WebCache.Instance.GetWebData(HttpUtility.HtmlDecode(url3), referer: url2) ?? "";
                }
            }
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

            rgx = new Regex(@"var vars = (.*)");
            m = rgx.Match(data);
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

        public override string GetVideoUrl(string url)
        {
            Dictionary<string, string> urls = GetPlaybackOptions(url);
            if (urls.Count > 0)
                return urls.First().Value;
            else
                return "";
        }
    }
}
