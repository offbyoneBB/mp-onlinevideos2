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
        public override string GetHosterUrl()
        {
            return "Mystream.to";
        }

        public override string GetVideoUrl(string url)
        {
            return DivxProvider(url);
        }
    }
}
