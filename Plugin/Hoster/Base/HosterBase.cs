using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster.Base
{
    public enum VideoType
    {
        flv,
        divx,
        unknown
    }
    public abstract class HosterBase
    {
        protected VideoType videoType;

        public virtual Dictionary<string, string> getPlaybackOptions(string url)
        {
            return new Dictionary<string, string>() { { GetType().Name, getVideoUrls(url) } };
        }
        public abstract string getVideoUrls(string url);
        public abstract string getHosterUrl();
        public virtual VideoType getVideoType() { return videoType; }

        protected static string FlashProvider(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
        protected static string DivxProvider(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"var\surl\s=\s'(?<url>[^']+)';");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
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

        protected static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
