using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class ZinWa : HosterBase
    {
        public override string getHosterUrl()
        {
            return "zinwa.com";
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            string page = SiteUtilBase.GetWebData(url);

            //Extract iframe url from HTML
            Match n = Regex.Match(page, @"<IFRAME SRC=""(?<url>[^""]*)""[^""]*>");
            
            if (!string.IsNullOrEmpty(page))
            {
                //Get HTML from iframe url
                string webData = SiteUtilBase.GetWebData(n.Groups["url"].Value);
                string file = GetSubString(webData, @"file: """, @"""");

                if (!string.IsNullOrEmpty(file))
                {
                    videoType = VideoType.flv;
                    return file;
                }
                return String.Empty;
            }
            return String.Empty;
        }
    }
}
