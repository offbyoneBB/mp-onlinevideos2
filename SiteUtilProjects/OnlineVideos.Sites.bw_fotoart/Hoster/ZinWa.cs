using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class ZinWa : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "zinwa.com";
        }

        public override string GetVideoUrl(string url)
        {
            //Get HTML from url
            string page = WebCache.Instance.GetWebData(url);

            //Extract iframe url from HTML
            Match n = Regex.Match(page, @"<IFRAME SRC=""(?<url>[^""]*)""[^""]*>");
            
            if (!string.IsNullOrEmpty(page))
            {
                //Get HTML from iframe url
                string webData = WebCache.Instance.GetWebData(n.Groups["url"].Value);
                string file = Helpers.StringUtils.GetSubString(webData, @"file: """, @"""");

                if (!string.IsNullOrEmpty(file))
                {
                    return file;
                }
                return String.Empty;
            }
            return String.Empty;
        }
    }
}
