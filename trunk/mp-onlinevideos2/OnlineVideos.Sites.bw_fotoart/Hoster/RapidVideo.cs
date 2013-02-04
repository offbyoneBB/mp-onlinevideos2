using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class RapidVideo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "rapidvideo.com";
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Extract file url from HTML
                Match n = Regex.Match(page, @"file=(?<url>[^&]*)");
                if (n.Success)
                {
                    videoType = VideoType.divx;
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }
    }
}
