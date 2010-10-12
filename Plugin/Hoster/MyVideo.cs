using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using System.Net;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class MyVideo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "MyVideo.de";
        }

        public override string getVideoUrls(string url)
        {
            string newUrl = SiteUtilBase.GetRedirectedUrl(url);
            newUrl = HttpUtility.UrlDecode(newUrl);
            Match n = Regex.Match(newUrl, @"V=(?<url>[^&]+)&");
            if (n.Success)
            {
                videoType = VideoType.flv;
                return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }
}
