using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Xtream : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Xtream.to";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"<param\sname=""src""\svalue=""(?<url>[^""]+)""");
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
