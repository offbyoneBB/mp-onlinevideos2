using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Tubeload : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Tubeload.to";
        }

        public override string getVideoUrls(string url)
        {
            if (url.Contains("flv"))
                videoType = VideoType.flv;
            else
                videoType = VideoType.divx;
            return DivxProvider(url);
        }
    }
}
