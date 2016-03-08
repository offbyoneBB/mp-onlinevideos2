using System;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class LiveLeakUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            string res = base.GetVideoUrl(video);
            if (!String.IsNullOrEmpty(res) && res.StartsWith("rtmp:"))
            {
                RtmpUrl theUrl = new RtmpUrl(res);
                int p = res.IndexOf('/', 9);
                int q = res.LastIndexOf('/');
                theUrl.App = res.Substring(p + 1, q - p);
                theUrl.PlayPath = "mp4:" + res.Substring(q + 1);
                return theUrl.ToString();
            }
            return res;
        }
    }
}
