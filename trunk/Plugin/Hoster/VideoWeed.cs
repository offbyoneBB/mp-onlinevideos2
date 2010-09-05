using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Hoster
{
    public class VideoWeed : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Videoweed.com";
        }

        public override string getVideoUrls(string url)
        {
            return FlashProvider(url);
        }
    }
}
