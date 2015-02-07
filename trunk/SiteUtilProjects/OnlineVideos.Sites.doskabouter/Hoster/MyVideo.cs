using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
using System.Net;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class MyVideo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "MyVideo.de";
        }

        public override string GetVideoUrl(string url)
        {
            string newUrl = WebCache.Instance.GetRedirectedUrl(url);
            newUrl = HttpUtility.UrlDecode(newUrl);
            Match n = Regex.Match(newUrl, @"V=(?<url>[^&]+)&");
            if (n.Success)
            {
                return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }
}
