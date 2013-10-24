using System;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Hoster
{
    public abstract class MyHosterBase : HosterBase
    {
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

        private string ToBase36(int i)
        {
            string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            string res = "";
            do
            {
                res += chars[i % 36];
                i = i / 36;
            } while (i > 0);
            return res;
        }

        private string ToBase(int c, int a)
        {
            string res = (c < a ? "" : ToBase(c / a, a)) + ((c % a) > 35 ? ((char)(c % a + 29)).ToString() : ToBase36(c % a));
            return res;
        }

        protected string Unpack(string p, int a, int c, string[] k, int e, string d)
        {
            for (int i = c - 1; i >= 0; i--)
                if (i < k.Length && !String.IsNullOrEmpty(k[i]))
                    p = Regex.Replace(p, @"\b" + ToBase(i, a) + @"\b", k[i]);
            return p;
        }


    }
}
