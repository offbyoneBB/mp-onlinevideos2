using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Quickload : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Quickload.to";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"src=""(?<url>[^""]+)""\stype=""video/divx""");
                if (n.Success)
                {
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }
    }
}
