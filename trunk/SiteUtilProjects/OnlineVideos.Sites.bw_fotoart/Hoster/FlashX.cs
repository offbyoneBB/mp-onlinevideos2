using System;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class FlashX : HosterBase
    {
        public override string getHosterUrl()
        {
            return "flashx.tv";
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Extract url from HTML
                Match n = Regex.Match(page, @"<span\sclass=""auto-style6"">\s*<a\shref=""(?<url>[^""]*)""[^>]*>");
                if (n.Success)
                {
                    //Extract url from HTML
                    string webData = SiteUtilBase.GetWebData(n.Groups["url"].Value);
                    Match m = Regex.Match(webData, @"config=(?<url>[^""]*)""");
                    string webData2 = SiteUtilBase.GetWebData(m.Groups["url"].Value);

                    //Grab link from xml page
                    string file = GetSubString(webData2, @"<file>", @"</file>");

                    if (!string.IsNullOrEmpty(file))
                    {
                        videoType = VideoType.flv;
                        return file;	
                    }
                }
                return String.Empty;
            }
            return String.Empty;
        }
    }
}
