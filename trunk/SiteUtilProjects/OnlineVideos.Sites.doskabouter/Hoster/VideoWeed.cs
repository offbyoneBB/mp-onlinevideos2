using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class VideoWeed : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "Videoweed.es";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                string link = FlashProvider(page);
                return link;
            }
            return String.Empty;
        }
    }
}
