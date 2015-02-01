using System;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class EventCasterUtil : GenericSiteUtil
    {

        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
            video.Airdate = matchGroups["date"].Value + ' ' + matchGroups["time"].Value;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);

            Match m = regEx_FileUrl.Match(webData);
            if (m.Success)
            {
                string newUrl = m.Groups["m0"].Value + "?authenticity_token=" + HttpUtility.UrlEncode(m.Groups["m1"].Value);
                webData = GetWebData(newUrl);
                m = Regex.Match(webData, @"url:\s\\""(?<url>[^\\]*)\\[^{]*{[^{]*{\\n\s*url:\s\\""[^""]*"",\\n\s*netConnectionUrl:\s\\""(?<netConnectionUrl>[^\\]*)\\""", defaultRegexOptions);
                if (m.Success)
                {
                    string ncUrl = m.Groups["netConnectionUrl"].Value;
                    string[] parts = ncUrl.Split('/');
                    string playPath = parts[4];
                    bool live = webData.Contains("live = true");
                    if (live)
                    {
                        int p = playPath.IndexOf("stream");
                        if (p >= 0) playPath = playPath.Insert(p, "/");
                    }
                    RtmpUrl result = new RtmpUrl(ncUrl)
                    {
                        PlayPath = m.Groups["url"].Value,
                        App = String.Format("{0}/{1}{2}", parts[3], parts[4], m.Groups["m1"].Value),
                        Live = live
                    };
                    return result.ToString();
                }
            }
            return String.Empty;

        }
    }
}
