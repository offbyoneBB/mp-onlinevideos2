using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public abstract class MyHosterBase : HosterBase
    {
        protected string GetFromPost(string url, string page, bool skipsubmit = false, string[] extraValues = null, string[] ignoreValues = null)
        {
            List<string> values = new List<string>();

            Match m = Regex.Match(page, @"<input\s*type=""hidden""\s*name=""(?<name>[^\'""]+)""\s*value=""(?<value>[^\'""]+)""[^>]*>");
            while (m != null && m.Success)
            {
                AddToList(m.Groups, values, ignoreValues);
                m = m.NextMatch();
            }

            if (!skipsubmit)
            {
                m = Regex.Match(page, @"<input\s*type=""submit""\s*[^>]*name=""(?<name>[^""]*)""[^>]*value=""(?<value>[^""]*)""[^>]*>");
                if (m.Success)
                    AddToList(m.Groups, values, ignoreValues);
            }

            if (extraValues != null)
                values.AddRange(extraValues);
            if (values.Count > 0)
                page = WebCache.Instance.GetWebData(url, String.Join("&", values.ToArray()), forceUTF8: true);
            // Sometimes gorillavid returns "utf8" instead of "utf-8" as charset which crashes getwebdatafrompost, so force it to utf8

            return page;
        }

        private void AddToList(GroupCollection groups, List<string> values, string[] ignoreValues)
        {
            string key = groups["name"].Value;
            string value = groups["value"].Value;

            if (!String.IsNullOrEmpty(key) && !String.IsNullOrEmpty(value))
            {
                string valueToAdd = String.Format("{0}={1}", key, HttpUtility.UrlEncode(value));
                if (ignoreValues == null || Array.IndexOf(ignoreValues, valueToAdd) == -1)
                    values.Add(valueToAdd);
            }
        }

        protected string WiseCrack(string webdata)
        {
            Match m = Regex.Match(webdata, @"\('(?<w>[^']{5,})','(?<i>[^']{5,})','(?<s>[^']{5,})'");
            while (m.Success)
            {
                webdata = UnWise(m.Groups["w"].Value, m.Groups["i"].Value, m.Groups["s"].Value);
                if (webdata.Contains("flashvars"))
                    return webdata;
                string s = WiseCrack(webdata);
                if (!String.IsNullOrEmpty(s))
                    return s;

                m = m.NextMatch();
            }
            return null;
        }

        protected string FlashProvider(string page)
        {
            Match m = Regex.Match(page, @"flashvars\.domain=""(?<domain>[^""]*)"";\s*flashvars\.file=""(?<file>[^""]*)"";\s*flashvars\.filekey=""(?<filekey>[^""]*)"";");
            if (m.Success)
            {
                string fileKey = m.Groups["filekey"].Value.Replace(".", "%2E").Replace("-", "%2D");
                string url2 = String.Format(@"{0}/api/player.api.php?key={1}&user=undefined&codes=1&pass=undefined&file={2}",
                    m.Groups["domain"].Value, fileKey, m.Groups["file"].Value);
                page = WebCache.Instance.GetWebData(url2);
                m = Regex.Match(page, @"url=(?<url>[^&]*)&");
                if (m.Success)
                    return m.Groups["url"].Value;
            }
            else
            {
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"addVariable\(""file"",""(?<url>[^""]+)""\);");
                    if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                    n = Regex.Match(page, @"flashvars.file=""(?<url>[^""]+)"";");
                    if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                    n = Regex.Match(page, @"flashvars.{0,50}file\s*?(?:=|:)\s*?(?:\'|"")?(?<url>[^\'""]+)(?:\'|"")?", DefaultRegexOptions);
                    if (n.Success && Helpers.UriUtils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                }
            }
            return String.Empty;
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

        private string UnWise(string w, string i, string s)
        {
            StringBuilder arr1 = new StringBuilder();
            StringBuilder arr2 = new StringBuilder();
            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            int endLength = w.Length + i.Length + s.Length;

            while (endLength != arr1.Length + arr2.Length)
            {
                if (v1 < 5) arr2.Append(w[v1]);
                else
                    if (v1 < w.Length)
                        arr1.Append(w[v1]);
                v1++;

                if (v2 < 5) arr2.Append(i[v2]);
                else
                    if (v2 < i.Length)
                        arr1.Append(i[v2]);
                v2++;
                if (v3 < 5) arr2.Append(s[v3]);
                else
                    if (v3 < s.Length)
                        arr1.Append(s[v3]);
                v3++;
            }

            v2 = 0;
            string arr1S = arr1.ToString();
            string res = "";
            for (v1 = 0; v1 < arr1S.Length; v1 += 2)
            {
                int tt = -1;
                if (arr2[v2] % 2 == 1)
                    tt = 1;
                int b = FromBase36(arr1S.Substring(v1, 2)) - tt;
                res += Convert.ToChar(b);
                v2++;
                if (v2 >= arr2.Length) v2 = 0;
            }
            return res;
        }

        internal static RegexOptions DefaultRegexOptions = RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace;

        internal static string DivxProvider(string url, string webData = null)
        {
            string page = webData;
            if (webData == null)
                page = WebCache.Instance.GetWebData<string>(url);

            if (!string.IsNullOrEmpty(page))
            {
                Match n = Regex.Match(page, @"var\surl\s=\s'(?<url>[^']+)';");
                if (n.Success) return n.Groups["url"].Value;
                n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                if (n.Success) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }
}
