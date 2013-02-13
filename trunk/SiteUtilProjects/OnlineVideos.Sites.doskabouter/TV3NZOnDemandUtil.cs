using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class TV3NZOnDemandUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                if (cat.Name != "A-Z")
                    cat.HasSubCategories = false;
            }

            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            string res = base.getUrl(video);
            string[] urlParts = video.VideoUrl.Split('.');

            if (urlParts[1] == "four")
                res = res.Replace("@@", "c4");
            else
                res = res.Replace("@@", "tv3");

            video.PlaybackOptions = new Dictionary<string, string>();
            string[] bitRates = { "330K", "700K" };
            foreach (string bitRate in bitRates)
            {

                RtmpUrl rtmpUrl = new RtmpUrl(res + bitRate)
                {
                    SwfVerify = true,
                    SwfUrl = @"http://static.mediaworks.co.nz/video/3.9/videoPlayer3.9.swf"
                };

                video.PlaybackOptions.Add(bitRate, rtmpUrl.ToString());
                //rtmpe://nzcontent.mediaworks.co.nz/tv3/_definst_/mp4:/transfer/07022011/HW031459_700K -W http://static.mediaworks.co.nz/video/3.9/videoPlayer3.9.swf
                //rtmpe://nzcontent.mediaworks.co.nz/c4/_definst_/mp4:/transfer/07022011/HW031459_700K -W http://static.mediaworks.co.nz/video/3.9/videoPlayer3.9.swf
            }
            return video.PlaybackOptions[bitRates[1]];
        }
    }
}
