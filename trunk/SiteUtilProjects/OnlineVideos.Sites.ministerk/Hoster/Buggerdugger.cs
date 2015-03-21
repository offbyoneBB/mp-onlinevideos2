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
    public class Buggerdugger : HosterBase
    {

        private string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;
                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }
                return ascii;
            }
            catch { }
            return string.Empty;
        }

        public override string GetHosterUrl()
        {
            return "buggerdugger.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            if (url.Contains("/video_ext.php?"))
            {
                Regex rgx = new Regex(@"oid=(?<oid>\d+).*?[^o]id=(?<id>\d+).*?hash=(?<hash>[0-9a-f]*)");
                Match m = rgx.Match(url);
                if (m.Success)
                {
                    string format = @"https://api.vk.com/method/video.getEmbed?oid={0}&video_id={1}&embed_hash={2}&callback=callbackFunc";
                    string vkUrl = string.Format(format, m.Groups["oid"].Value, m.Groups["id"].Value, m.Groups["hash"].Value);
                    playbackOptions = HosterFactory.GetHoster("vk").GetPlaybackOptions(vkUrl);
                }
            }
            else
            {
                string data = GetWebData(url);
                Regex rgx = new Regex(@"{""file"":""(?<url>[^""]*).*?label"":""(?<res>[^""]*)");
                foreach (Match m in rgx.Matches(data))
                {
                    string u = m.Groups["url"].Value;
                    u = u.Replace(@"\x", string.Empty);
                    u = ConvertHex(u);
                    string r = m.Groups["res"].Value;
                    playbackOptions.Add(r, u);
                }
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
