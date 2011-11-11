using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Speedload : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Speedload.to";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"src=""(?<url>[^""]+)""\smovietitle");
                if (n.Success)
                {
                    string referer = n.Groups["url"].Value;
                    string link = SiteUtilBase.GetRedirectedUrl(referer,url);
                    if (referer.CompareTo(link) != 0)
                    {
						var resultUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(link);
						resultUrl.Referer = referer;
						return resultUrl.ToString();
                    }
                }
            }
            return String.Empty;
        }
    }
}
