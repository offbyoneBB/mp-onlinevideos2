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
    }
}
