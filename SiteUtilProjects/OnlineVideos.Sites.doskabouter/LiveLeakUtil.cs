using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class LiveLeakUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            string res = base.GetVideoUrl(video);
            if (res.Contains(@"www.liveleak.com/ll_embed"))
            {
                var data = GetWebData(res);
                video.PlaybackOptions = new Dictionary<string, string>();
                Match m = Regex.Match(data, @"<source\ssrc=""(?<url>[^""]*)""\s(?:default\s)?res=""[^""]*""\slabel=""(?<label>[^""]*)""\stype=""video/mp4"">");
                while (m.Success)
                {
                    video.PlaybackOptions.Add(m.Groups["label"].Value, m.Groups["url"].Value);
                    m = m.NextMatch();
                }
                return video.PlaybackOptions.First().Value;
            }
            return res;
        }
    }
}
