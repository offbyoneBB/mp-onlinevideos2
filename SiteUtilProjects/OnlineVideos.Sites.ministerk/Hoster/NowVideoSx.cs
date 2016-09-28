using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class NowVideoSx : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "nowvideo.sx";
        }

        public override Dictionary<string,string> GetPlaybackOptions(string url)
        {
            string data = GetWebData<string>(url);
            Regex rgx = new Regex(@"src=""(?<u>[^""]*)[^>]*video/mp4");
            Dictionary<string, string> pbos = new Dictionary<string, string>();
            int i = 1;
            foreach(Match m in rgx.Matches(data))
            {
                pbos.Add(i.ToString(), m.Groups["u"].Value);
                i++;
            }
            return pbos;
        }

        public override string GetVideoUrl(string url)
        {
            Dictionary<string, string> pbos = GetPlaybackOptions(url);
            if (pbos.Count == 0)
                return "";
            else
                return pbos.First().Value;
        }
    }
}
