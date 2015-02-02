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
    public class Freeload : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Freeload.to";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"type=""video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success)
                {
					var resultUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl(n.Groups["url"].Value);
					resultUrl.Referer = url;
					return resultUrl.ToString();
                }
            }
            return String.Empty;
        }
    }
}