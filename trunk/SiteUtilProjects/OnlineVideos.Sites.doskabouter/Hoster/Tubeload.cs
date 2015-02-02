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
        public override string GetHosterUrl()
        {
            return "Tubeload.to";
        }

        public override string GetVideoUrl(string url)
        {
            return DivxProvider(url);
        }
    }
}
