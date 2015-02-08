using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Helpers
{
    public static class StringUtils
    {
        public static string ToFriendlyCase(string PascalString)
        {
            return Regex.Replace(PascalString, "(?!^)([A-Z])", " $1");
        }

        public static string ReplaceEscapedUnicodeCharacter(string input)
        {
            return Regex.Replace(input, @"(?:\\|%)[uU]([0-9A-Fa-f]{4})",
                match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
        }

        public static string GetRandomLetters(int amount)
        {
            var random = new Random();
            var sb = new StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(Encoding.ASCII.GetString(new byte[] { (byte)random.Next('A', 'Z') }));
            return sb.ToString();
        }

        public static string[] Tokenize(string text, bool dropToken, params string[] tokens)
        {
            if (tokens.Length > 0)
            {

                string regex = @"([";
                foreach (string s in tokens)
                    regex += s;
                regex += "])";
                Regex RE = new Regex(regex);
                if (dropToken)
                {
                    string output = RE.Replace(text, " ");
                    return (new Regex(@"\s").Split(output));
                }
                else
                    return (RE.Split(text));
            }
            return null;
        }

        public static string PlainTextFromHtml(string input)
        {
            string result = input;
            if (!string.IsNullOrEmpty(result))
            {
                // decode HTML escape character
                result = System.Web.HttpUtility.HtmlDecode(result);

                // Replace &nbsp; with space
                result = Regex.Replace(result, @"&nbsp;", " ", RegexOptions.Multiline);

                // Remove double spaces
                result = Regex.Replace(result, @"  +", "", RegexOptions.Multiline);

                // Replace <br/> with \n
                result = Regex.Replace(result, @"< *br */*>", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove remaining HTML tags                
                result = Regex.Replace(result, @"<[^>]*>", "", RegexOptions.Multiline);

                // Replace multiple newlines with just one
                result = Regex.Replace(result, @"(\r?\n)+", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove whitespace at the beginning and end
                result = result.Trim();
            }
            return result;
        }

        public static string GetSubString(string s, string start, string until)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        public static string GetRegExData(string regex, string data, string group)
        {
            string result = string.Empty;
            Match m = Regex.Match(data, regex);
            if (m.Success)
                result = m.Groups[group].Value;
            return result == null ? string.Empty : result;
        }

        private static string GetVal(string num, string[] pars)
        {
            int n = 0;
            for (int i = 0; i < num.Length; i++)
            {
                n = n * 36;
                char c = num[i];
                if (Char.IsDigit(c))
                    n += ((int)c) - 0x30;
                else
                    n += ((int)c) - 0x61 + 10;
            }
            if (n < 0 || n >= pars.Length)
                return n.ToString();

            return pars[n];
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
            string num = "";
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (Char.IsDigit(c) || Char.IsLower(c))
                    num += c;
                else
                {
                    if (num.Length > 0)
                    {
                        res += GetVal(num, pars);
                        num = "";
                    }
                    res += c;
                }
            }
            if (num.Length > 0)
                res += GetVal(num, pars);

            return res;
        }

        private static string ToBase36(int i)
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

        private static string ToBase(int c, int a)
        {
            string res = (c < a ? "" : ToBase(c / a, a)) + ((c % a) > 35 ? ((char)(c % a + 29)).ToString() : ToBase36(c % a));
            return res;
        }

        public static string Unpack(string p, int a, int c, string[] k, int e, string d)
        {
            for (int i = c - 1; i >= 0; i--)
                if (i < k.Length && !String.IsNullOrEmpty(k[i]))
                    p = Regex.Replace(p, @"\b" + ToBase(i, a) + @"\b", k[i]);
            return p;
        }

    }
}
