using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VideozBz : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "videoz.bz";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            try
            {
                string data = GetWebData(url);

                Regex rgx = new Regex(@"<source.*?src=""(?<url>[^""]*).*?res=""(?<res>\d+)");
                foreach (Match match in rgx.Matches(data))
                {
                    playbackOptions.Add(match.Groups["res"].Value, match.Groups["url"].Value);
                }
                playbackOptions = playbackOptions.OrderByDescending((p) =>
                {
                    int parsedRes = 0;
                    int.TryParse(p.Key, out parsedRes);
                    return parsedRes;
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch { }
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
