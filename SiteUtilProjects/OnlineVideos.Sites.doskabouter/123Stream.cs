using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class One23Stream : GenericSiteUtil
    {
        public override List<VideoInfo> GetVideos(Category category)
        {
            var res = base.GetVideos(category);
            res = res.OrderBy(v => v.Title).ToList();
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            var data = GetWebData(video.VideoUrl, referer: baseUrl);

            var Match = Regex.Match(data, @"""file"":\s*(?<funcname>[^\(]*)\(");
            if (Match.Success)
            {
                Match = Regex.Match(data, @"function\s" + Match.Groups["funcname"].Value + @"\(\)\s{\s*return\(\[(?<list>[^]]*)]\.join\(""""\)\s*\+\s*(?<func2>[^\.]*)\.");
            }
            string s = null;
            if (Match.Success)
            {
                s = Match.Groups["list"].Value;
                s = "http:" + s.Replace(@"\/", "/").Replace(@"""", "").Replace(",", "");
                Match = Regex.Match(data, @"var\s*" + Match.Groups["func2"].Value + @"\s*=\s*\[(?<val>[^]]*)];");
            }
            if (Match.Success)
            {
                s = s + Match.Groups["val"].Value.Replace(@"""", "").Replace(",", "");
                Match = Regex.Match(data, @"\+\sdocument\.getElementById\(""(?<docid>[^""]*)""\)\.innerHTML");
            }
            if (Match.Success)
            {
                Match = Regex.Match(data, "id=" + Match.Groups["docid"].Value + ">(?<val>[^<]*)</");
            }
            if (Match.Success)
            {
                s = s + Match.Groups["val"].Value;
                var ddd = GetWebData(s).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string rest = ddd[ddd.Length - 1];
                int i = s.IndexOf("playlist.m3u8?");
                if (i > 0)
                    return s.Substring(0, i) + rest;
                return s;
            }
            return null;
        }
    }
}
