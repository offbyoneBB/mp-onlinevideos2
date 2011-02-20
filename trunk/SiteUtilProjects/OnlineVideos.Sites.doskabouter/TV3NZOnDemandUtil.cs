using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TV3NZOnDemandUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string res = base.getUrl(video);
            video.PlaybackOptions = new Dictionary<string, string>();
            string[] bitRates = { "330K", "700K" };
            foreach (string bitRate in bitRates)
            {
                Log.Debug("tv3 " + res + bitRate);

                string url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                HttpUtility.UrlEncode(res + bitRate),
                HttpUtility.UrlEncode(@"http://static.mediaworks.co.nz/video/3.9/videoPlayer3.9.swf")));
                video.PlaybackOptions.Add(bitRate, url);
                Log.Debug("tv3 add " + url);
                //rtmpe://nzcontent.mediaworks.co.nz/tv3/_definst_/mp4:/transfer/07022011/HW031459_700K -W http://static.mediaworks.co.nz/video/3.9/videoPlayer3.9.swf
            }
            return video.PlaybackOptions[bitRates[0]];
        }
    }
}
