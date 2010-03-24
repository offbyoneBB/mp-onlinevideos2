using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites
{
    public class KabouterWesleyUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string s = base.getUrl(video);
            return UrlTricks.YoutubeTrick(s, video);
        }
    }
}
