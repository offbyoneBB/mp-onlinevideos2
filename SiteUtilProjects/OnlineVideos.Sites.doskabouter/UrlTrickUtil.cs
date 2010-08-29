using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites
{
    public class UrlTrickUtil : GenericSiteUtil
    {
        public override List<String> getMultipleVideoUrls(VideoInfo video)
        {
            List<string> newUrls;
            string s = base.getUrl(video);
            if (UrlTricks.getMultipleVideoUrlsFromAll(s, video, out newUrls))
                return newUrls;
            return null;
        }


    }
}
