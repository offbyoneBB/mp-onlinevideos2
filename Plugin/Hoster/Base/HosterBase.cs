using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster.Base
{
    public enum VideoType
    {
        flv,
        divx,
        unknown
    }
    public abstract class HosterBase
    {
        protected VideoType videoType;

        public abstract string getVideoUrls(string url);
        public abstract string getHosterUrl();
        public virtual VideoType getVideoType() { return videoType; }

        protected static string FlashProvider(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
        protected static string DivxProvider(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"var\surl\s=\s'(?<url>[^']+)';");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }
}
