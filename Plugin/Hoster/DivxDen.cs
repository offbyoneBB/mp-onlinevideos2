using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxDen : HosterBase
    {
        public override string getHosterUrl()
        {
            return "DivxDen.com";
        }

        public override string getVideoUrls(string url)
        {
            if (url.Contains("embed"))
            {
                string page = SiteUtilBase.GetWebData(url);
                url = Regex.Match(page, @"<div><a\shref=""(?<url>[^""]+)""").Groups["url"].Value;
            }

            string[] urlParts = url.Split('/');

            string postData = @"op=download1&usr_login=&id=" + urlParts[3] + "&fname=" + urlParts[4] + "&referer=&method_free=Free+Stream";
            string webData = GenericSiteUtil.GetWebDataFromPost(url, postData);
            string packed = GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);
            string res = GetSubString(unpacked, @"'file','", @"'");
            videoType = VideoType.divx;
            if (!String.IsNullOrEmpty(res))
                return res;
            return GetSubString(unpacked, @"name=""src""value=""", @"""");
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
