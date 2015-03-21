using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
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
            string data = WebCache.Instance.GetWebData(url);
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

            Regex rgx = new Regex(@"""url(?<res>\d+)?"":""(?<url>[^""]*)");
            foreach (Match m in rgx.Matches(data))
            {
                string u = m.Groups["url"].Value.Replace(@"\/",@"/");
                string r = m.Groups["res"].Value;
                playbackOptions.Add(r, u);
            }
            playbackOptions = playbackOptions.OrderByDescending((p) =>
            {
                string resKey = p.Key;
                int parsedRes = 0;
                int.TryParse(resKey, out parsedRes);
                return parsedRes;
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

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
