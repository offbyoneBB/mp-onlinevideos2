using System;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites.Doskabouter.Helpers
{
    internal class NPOHelper
    {
        public static string GetToken(string url)
        {
            string webData = WebCache.Instance.GetWebData(@"http://ida.omroep.nl/npoplayer/i.js?s=" + HttpUtility.UrlEncode(url));
            string result = String.Empty;

            Match m = Regex.Match(webData, @"token\s*=\s*""(?<token>[^""]*)""");
            if (m.Success)
            {
                int first = -1;
                int second = -1;
                string token = m.Groups["token"].Value;
                for (int i = 5; i < token.Length - 4; i++)
                    if (Char.IsDigit(token[i]))
                    {
                        if (first == -1)
                            first = i;
                        else
                            if (second == -1)
                                second = i;
                    }
                if (first == -1) first = 12;
                if (second == -1) second = 13;
                char[] newToken = token.ToCharArray();
                newToken[first] = token[second];
                newToken[second] = token[first];
                return new String(newToken);
            }
            return null;
        }

    }
}
