using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DataUp : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Dataup.to";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success)
                {
                    return n.Groups["url"].Value;
                }
                else
                {
                    n = Regex.Match(page, @"addVariable\('file','(?<url>[^']+)'");
                    if (n.Success)
                    {
                        return n.Groups["url"].Value;
                    }
                }
            }
            return String.Empty;
        }
    }
}
