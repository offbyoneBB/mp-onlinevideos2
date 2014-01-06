using System;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class GorillaVid : MyHosterBase
    {
        public override string getHosterUrl()
        {
            return "GorillaVid.in";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                string sWaitTime = getRegExData(@"Wait\s(?:<(.|\n)*?>)?(?<waittime>\d*?)(?:<(.|\n)*?>)?\sseconds", page, "waittime");
                int iWaitTime = 5;
                if (!string.IsNullOrEmpty(sWaitTime))
                {
                    if (!int.TryParse(sWaitTime, out iWaitTime))
                        iWaitTime = 5;
                }

                page = GetFromPost(url, page, true);

                //file:\s*'(?<Title>[^"]*)',
                Match n = Regex.Match(page, @"file:\s*'(?<url>[^']*)'");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"file:\s*""(?<url>[^""]*)""");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }

    public class GorillaVidCom : GorillaVid
    {
        public override string getHosterUrl()
        {
            return "GorillaVid.com";
        }

        public override string getVideoUrls(string url)
        {
            url = SiteUtilBase.GetRedirectedUrl(url);
            return base.getVideoUrls(url);
        }
    }

}
