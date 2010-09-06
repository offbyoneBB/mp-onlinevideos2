using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class MovShare : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Movshare.net";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url.Substring(0, url.LastIndexOf("/")));
            if (!string.IsNullOrEmpty(page))
            {
                //flv
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success)
                {
                    videoType = VideoType.flv;
                    return n.Groups["url"].Value;
                }
                //divx
                n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success)
                {
                    videoType = VideoType.divx;
                    return n.Groups["url"].Value;
                }
            }
            return "";
        }
    }
}
