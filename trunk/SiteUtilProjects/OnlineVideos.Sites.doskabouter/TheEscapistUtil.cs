using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TheEscapistUtil : GenericSiteUtil
    {
        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
            string title2 = matchGroups["Title2"].Value;
            if (!String.IsNullOrEmpty(title2))
                video.Title += " " + title2;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            Match m = Regex.Match(data, @"imsVideo.play\({(?<preUrl>[^}]*)}", defaultRegexOptions);
            if (m.Success)
            {
                string preUrl = @"http://www.escapistmagazine.com/videos/vidconfig.php?" + m.Groups["preUrl"].Value.Replace("\"", "").Replace(':', '=').Replace(',', '&').Replace(";", "%3B");
                m = Regex.Match(m.Groups["preUrl"].Value, @"""hash"":""(?<hash>[^""]*)""");
                string s = GetWebData(preUrl);
                byte[] a = new byte[s.Length / 2];
                byte[] e = Encoding.ASCII.GetBytes(m.Groups["hash"].Value);
                for (int i = 0; i < s.Length / 2; i++)
                    a[i] = (byte)(Convert.ToByte(s.Substring(i * 2, 2), 16) ^ e[i % e.Length]);

                var json = JObject.Parse(Encoding.ASCII.GetString(a));
                var vids = json["files"]["videos"];
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (var vid in vids)
                    video.PlaybackOptions.Add(vid.Value<string>("type") + ' ' + vid.Value<string>("res"), vid.Value<string>("src"));
            }

            string resultUrl;
            if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
            else
                resultUrl = video.PlaybackOptions.Last().Value;
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
            return resultUrl;
        }
    }
}
