using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxStage : HosterBase
    {
        public override string getHosterUrl()
        {
            return "DivxStage.eu";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Even newer:
                string newMethod = Dumbify(page);
                if (!String.IsNullOrEmpty(newMethod))
                    page = newMethod;

                //new method:
                Match m = Regex.Match(page, @"flashvars\.domain=""(?<domain>[^""]*)"";\s*flashvars\.file=""(?<file>[^""]*)"";\s*flashvars\.filekey=""(?<filekey>[^""]*)"";");
                if (m.Success)
                {
                    string fileKey = m.Groups["filekey"].Value.Replace(".", "%2E").Replace("-", "%2D");
                    string url2 = String.Format(@"{0}/api/player.api.php?key={1}&user=undefined&codes=1&pass=undefined&file={2}",
                        m.Groups["domain"].Value, fileKey, m.Groups["file"].Value);
                    page = SiteUtilBase.GetWebData(url2);
                    m = Regex.Match(page, @"url=(?<url>[^&]*)&");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }
                //divx
                string link = DivxProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    videoType = VideoType.divx;
                    return link;
                }
                //flv
                link = FlashProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    int index = link.IndexOf(".flv", StringComparison.CurrentCultureIgnoreCase);
                    if (index >= 0)
                        link = link.Substring(0, index + 4);

                    videoType = VideoType.flv;
                    return link;
                }
                //other
                Match n = Regex.Match(page, @"src""\svalue=""(?<url>.*?)""");
                if (n.Success)
                {
                    videoType = VideoType.unknown;
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }

        private string Dumbify(string webdata)
        {
            Match m = Regex.Match(webdata, @"\('(?<w>[^']{5,})','(?<i>[^']{5,})','(?<s>[^']{5,})'");
            while (m.Success)
            {
                webdata = Dumb(m.Groups["w"].Value, m.Groups["i"].Value, m.Groups["s"].Value);
                if (webdata.Contains("flashvars"))
                    return webdata;
                string s = Dumbify(webdata);
                if (!String.IsNullOrEmpty(s))
                    return s;

                m = m.NextMatch();
            }
            return null;
        }

        private int FromBase36(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)c - 0x30;
            if (c >= 'a' && c <= 'z')
                return (int)c - 0x60 + 9;
            return -1;

        }
        private int FromBase36(string s)
        {
            int result = 0;
            for (int i = 0; i < s.Length; i++)
                result = result * 36 + FromBase36(s[i]);
            return result;
        }

        private string Dumb(string w, string i, string s)
        {
            string arr1 = "";
            string arr2 = "";
            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            while (true)
            {
                if (v1 < 5) arr2 += w[v1];
                else
                    if (v1 < w.Length)
                        arr1 += w[v1];
                v1++;

                if (v2 < 5) arr2 += i[v2];
                else
                    if (v2 < i.Length)
                        arr1 += i[v2];
                v2++;
                if (v3 < 5) arr2 += s[v3];
                else
                    if (v3 < s.Length)
                        arr1 += s[v3];
                v3++;
                if (w.Length + i.Length + s.Length == arr1.Length + arr2.Length)
                    break;
            }

            v2 = 0;
            string res = "";
            for (v1 = 0; v1 < arr1.Length; v1 += 2)
            {
                int tt = -1;
                if (arr2[v2] % 2 == 1)
                    tt = 1;
                int b = FromBase36(arr1.Substring(v1, 2)) - tt;
                res += Convert.ToChar(b);
                v2++;
                if (v2 >= arr2.Length) v2 = 0;
            }
            return res;
        }


    }

    public class DivxStageNet : DivxStage
    {
        public override string getHosterUrl()
        {
            return "DivxStage.net";
        }
    }
}
