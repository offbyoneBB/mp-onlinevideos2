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
    public class Loombo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Loombo.com";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed"))
            {
                string page = WebCache.Instance.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"'file=(?<url>[^']+)'");
                    if (n.Success)
                    {
                        return n.Groups["url"].Value;
                    }
                }
            }
            else
            {
                string page = WebCache.Instance.GetWebData(url, cookies: cc);

                if (!string.IsNullOrEmpty(page))
                {

                    string op = Regex.Match(page, @"<input\stype=""hidden""\sname=""op""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string id = Regex.Match(page, @"<input\stype=""hidden""\sname=""id""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string rand = Regex.Match(page, @"<input\stype=""hidden""\sname=""rand""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string referer = Regex.Match(page, @"<input\stype=""hidden""\sname=""referer""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string method_free = Regex.Match(page, @"<input\stype=""hidden""\sname=""method_free""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string method_premium = Regex.Match(page, @"<input\stype=""hidden""\sname=""method_premium""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;

                    string timeToWait = Regex.Match(page, @"<span\sid=""countdown"">(?<time>[^<]+)</span>").Groups["time"].Value;
                    if (Convert.ToInt32(timeToWait) < 10)
                    {
                        string postdata = "op=" + op +
                                          "&id=" + id +
                                          "&rand=" + rand +
                                          "&referer=" + referer +
                                          "&method_free=" + method_free +
                                          "&method_premium=" + method_premium +
                                          "&down_direct=1";

                        System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);

                        string page2 = WebCache.Instance.GetWebData(url, postdata, cc, url);

                        if (!string.IsNullOrEmpty(page2))
                        {
                            string packed = GetSubString(page2, @"return p}", @"</script>");
                            if (!String.IsNullOrEmpty(packed))
                            {
                                packed = packed.Replace(@"\'", @"'");
                                string unpacked = UnPack(packed);
                                string res = GetSubString(unpacked, @"'file','", @"'");
                                if (!String.IsNullOrEmpty(res))
                                    return res;
                                return GetSubString(unpacked, @"name=""src""value=""", @"""");
                            }
                            else
                            {
                                string res = GetSubString(page2, @"addVariable('file','", @"'");
                                if (String.IsNullOrEmpty(res))
                                {
                                    Match m = Regex.Match(page2, @"<!--\sDIRECT\sLINK\sDL-->\s*<span\sstyle=""[^""]*"">\s*<a\shref=""[^""]*"">(?<url>[^<]*)</a>\s*</span>");
                                    if (m.Success)
                                        res = m.Groups["url"].Value;
                                }
                                return res;
                            }
                        }
                    }
                }
            }
            return String.Empty;
        }

    }
}
