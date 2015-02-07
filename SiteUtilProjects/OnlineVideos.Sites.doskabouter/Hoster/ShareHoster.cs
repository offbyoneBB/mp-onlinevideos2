using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Net;

namespace OnlineVideos.Hoster
{
    public class ShareHoster : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Sharehoster.com";
        }

        public override string GetVideoUrl(string url)
        {
            url = url.Replace("vid", "wait");
            CookieContainer cc = new CookieContainer();
            string page = WebCache.Instance.GetWebData(url, cookies: cc);
            string file = url.Substring(url.LastIndexOf(@"/") + 1);
            string wait = Regex.Match(page, @"name=""wait""\svalue=""(?<wait>[^""]+)""").Groups["wait"].Value;
            string postdata = string.Format("continue=Fortfahren&open=show_wait&file={0}&wait={1}", file, wait);
            page = WebCache.Instance.GetWebData("http://www.sharehoster.com/vid/" + file, postdata, cc, url);

            Match n = Regex.Match(page, @"name=""stream""\svalue=""(?<url>[^""]+)""");
            if (n.Success)
            {
                return n.Groups["url"].Value;
            }
            else
            {
                page = WebCache.Instance.GetWebData("http://www.sharehoster.com/flowplayer/config.php?movie=" + file, cookies: cc);
                n = Regex.Match(page, @"'url':\s'(?<url>.*/video/[^']+)'");
                if (n.Success)
                {
                    return n.Groups["url"].Value;
                }
            }

            return String.Empty;
        }
    }
}
