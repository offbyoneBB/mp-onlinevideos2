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
        public override string getHosterUrl()
        {
            return "Loombo.com";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed"))
            {
                string page = SiteUtilBase.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"'file=(?<url>[^']+)'");
                    if (n.Success)
                    {
                        videoType = VideoType.flv;
                        return n.Groups["url"].Value;
                    }
                }
            }
            else
            {
                string page = SiteUtilBase.GetWebData(url, cc);

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

                        string page2 = SiteUtilBase.GetWebDataFromPost(url, postdata, cc, url);

                        if (!string.IsNullOrEmpty(page2))
                        {
                            string packed = GetSubString(page2, @"return p}", @"</script>");
                            packed = packed.Replace(@"\'", @"'");
                            string unpacked = UnPack(packed);
                            videoType = VideoType.divx;
                            string res = GetSubString(unpacked, @"'file','", @"'");
                            if (!String.IsNullOrEmpty(res))
                                return res;
                            return GetSubString(unpacked, @"name=""src""value=""", @"""");
                        }
                    }
                }
            }
            return "";
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        public static string UnPack(string packed)
        {
            string res;
            int p = packed.IndexOf('|');
            if (p < 0) return null;
            p = packed.LastIndexOf('\'', p);

            string pattern = packed.Substring(0, p - 1);

            string[] pars = packed.Substring(p).TrimStart('\'').Split('|');
            for (int i = 0; i < pars.Length; i++)
                if (String.IsNullOrEmpty(pars[i]))
                    if (i < 10)
                        pars[i] = i.ToString();
                    else
                        if (i < 36)
                            pars[i] = ((char)(i + 0x61 - 10)).ToString();
                        else
                            pars[i] = (i - 26).ToString();
            res = String.Empty;
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                int ind = -1;
                if (Char.IsDigit(c))
                {
                    if (i + 1 < pattern.Length && Char.IsDigit(pattern[i + 1]))
                    {
                        ind = int.Parse(pattern.Substring(i, 2)) + 26;
                        i++;
                    }
                    else
                        ind = int.Parse(pattern.Substring(i, 1));
                }
                else
                    if (Char.IsLower(c))
                        ind = ((int)c) - 0x61 + 10;

                if (ind == -1 || ind >= pars.Length)
                    res += c;
                else
                    res += pars[ind];
            }
            return res;
        }

    }
}
