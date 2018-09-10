using System;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class FlashX : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "flashx.tv";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                System.Threading.Thread.Sleep(8500);//experimental
                page = GetFromPost(@"https://www.flashx.co/dl?playitnow", page);
                //Extract url from HTML
                Match n = Regex.Match(page, @"player\.updateSrc\(\[\s*{src:\s'(?<url>[^']*)'");
                if (n.Success)
                    return n.Groups["url"].Value;
                return String.Empty;
            }
            return String.Empty;
        }
    }
}
