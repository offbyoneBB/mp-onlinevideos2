using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Net;

namespace OnlineVideos.Hoster
{
    public class ShareHoster : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Sharehoster.com";
        }

        public override string getVideoUrls(string url)
        {

            string code = url.Replace("http://www.sharehoster.com/wait/", "");
            CookieContainer cc = new CookieContainer();
            string page = SiteUtilBase.GetWebData("http://www.sharehoster.com/flowplayer/config.php?movie=" + code, cc);
            Match n = Regex.Match(page, @"'url':\s'(?<url>.*/video/[^']+)'");
            if (n.Success)
            {
                videoType = VideoType.flv;
                return n.Groups["url"].Value;
            }
            return "";
        }
    }
}
