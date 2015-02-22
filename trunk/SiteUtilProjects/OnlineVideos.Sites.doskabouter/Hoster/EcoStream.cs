using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class EcoStream : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "ecostream.tv";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string fullUrl = url + "?ss=1";
            string webData = WebCache.Instance.GetWebData(fullUrl, "ss=1&sss=1", cc);
            Match m = Regex.Match(webData, @"var\st=setTimeout\(""lc\('(?<s>[^']*)','(?<k>[^']*)','(?<t>[^']*)','(?<key>[^']*)'\)"",[^\)]*\);");
            if (m.Success)
            {
                string newUrl = String.Format(@"http://www.ecostream.tv/lo/mq.php?s={0}&k={1}&t={2}&key={3}",
                    m.Groups["s"].Value, m.Groups["k"].Value, m.Groups["t"].Value, m.Groups["key"].Value);
                webData = WebCache.Instance.GetWebData(newUrl, "", cc, referer: fullUrl);
                m = Regex.Match(webData, @"<param\sname=""flashvars""\svalue=""file=(?<url>[^&]*)&[^>]*>");
                if (m.Success)
                {
                    newUrl = HttpUtility.UrlDecode(m.Groups["url"].Value);
                    if (!Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                    {
                        Uri uri = null;
                        if (Uri.TryCreate(new Uri(url), newUrl, out uri))
                        {
                            return WebCache.Instance.GetRedirectedUrl(uri.ToString() + "&start=0");
                        }
                    }
                }
            }
            return String.Empty;
        }
    }
}
