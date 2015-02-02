using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Net;

namespace OnlineVideos.Hoster
{
    public class Loaded : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Loaded.it";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string first = WebCache.Instance.GetWebData(url, cookies: cc);
            string code = Regex.Match(first, @"name=""code""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
            string second = WebCache.Instance.GetWebData(url, "code=" + code, cc, url);
            Match n = Regex.Match(second, @"url:\s'(?<url>.*/get/[^']+)'");
            if (n.Success)
            {
                return n.Groups["url"].Value;
            }
            else
            {
                string hash = Regex.Match(second, @"name=""hash""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
                string hostname = Regex.Match(second, @"name=""hostname""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
                string filename = Regex.Match(second, @"name=""filename""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;

				if (!string.IsNullOrEmpty(hash) && !string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(filename))
				{
					var resultUrl = new OnlineVideos.MPUrlSourceFilter.HttpUrl("http://" + hostname + "/get/" + hash + "/" + filename);
					resultUrl.Referer = url;
					return resultUrl.ToString();
				}
            }
            return String.Empty;
        }
    }
}
