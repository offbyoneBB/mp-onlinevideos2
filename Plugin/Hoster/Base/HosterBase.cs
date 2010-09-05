using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster.Base
{
    public abstract class HosterBase
    {
        public abstract string getVideoUrls(string url);
        public abstract string getHosterUrl();

        protected static string FlashProvider(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success) return n.Groups["url"].Value;
            }
            return "";
        }
    }
}
