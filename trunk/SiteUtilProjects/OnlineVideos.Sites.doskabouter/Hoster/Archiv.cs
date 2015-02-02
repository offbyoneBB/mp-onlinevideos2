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
    public class Archiv : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Archiv.to";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"embed src=""(?<url>[^""]+)""");
                if (n.Success)
                {
                    return Regex.Match(HttpUtility.UrlDecode(n.Groups["url"].Value), @"file=(?<url>[^&]+)&").Groups["url"].Value;
                }
                else
                {
                    n = Regex.Match(page, @"href=""(?<url>[^""]+)"">Download");
                    if (n.Success)
                    {
						var resultUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(n.Groups["url"].Value);
						resultUrl.Referer = url;
						return resultUrl.ToString();
                    }
                }
            }
            return String.Empty;
        }
    }
}
