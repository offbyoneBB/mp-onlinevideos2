using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Hoster
{
    public class NovaMov : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Novamov.com";
        }

        public override string getVideoUrls(string url)
        {
            videoType = VideoType.flv;
            return FlashProvider(url);
        }
    }
}
