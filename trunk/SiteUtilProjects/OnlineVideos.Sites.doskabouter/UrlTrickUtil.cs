using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites
{
    public class UrlTrickUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string newUrl;
            string s = base.getUrl(video);
            if (UrlTricks.GetUrlFromAll(s, video, out newUrl))
                return newUrl;
            return null;
        }
    }
}
