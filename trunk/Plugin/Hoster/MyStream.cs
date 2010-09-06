using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class MyStream : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Mystream.to";
        }

        public override string getVideoUrls(string url)
        {
            if(url.Contains("flv"))
                videoType = VideoType.flv;
            else
                videoType = VideoType.divx;
            return DivxProvider(url);
        }
    }
}
