using System.Text.RegularExpressions;
using System.Net;

namespace OnlineVideos.Sites.Doskabouter.Helpers
{
    internal class NPOHelper
    {
        public static string GetToken(string url, WebProxy proxy = null)
        {
            string webData = WebCache.Instance.GetWebData(@"http://ida.omroep.nl/app.php/auth", proxy: proxy);

            Match m = Regex.Match(webData, @"{""token"":""(?<token>[^""]*)""}");
            if (m.Success)
                return m.Groups["token"].Value;
            return null;
        }

    }
}
