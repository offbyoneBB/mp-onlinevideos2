using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class EcoStream : HosterBase
    {
        public override string getHosterUrl()
        {
            return "ecostream.tv";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string fullUrl = url + "?ss=1";
            string webData = SiteUtilBase.GetWebDataFromPost(fullUrl, "ss=1&sss=1", cc);
            Match m = Regex.Match(webData, @"var\st=setTimeout\(""lc\('(?<s>[^']*)','(?<k>[^']*)','(?<t>[^']*)','(?<key>[^']*)'\)"",[^\)]*\);");
            if (m.Success)
            {
                string newUrl = String.Format(@"http://www.ecostream.tv/lo/mq.php?s={0}&k={1}&t={2}&key={3}",
                    m.Groups["s"].Value, m.Groups["k"].Value, m.Groups["t"].Value, m.Groups["key"].Value);
                webData = SiteUtilBase.GetWebDataFromPost(newUrl, "", cc, referer: fullUrl);
                m = Regex.Match(webData, @"<param\sname=""flashvars""\svalue=""file=(?<url>[^&]*)&[^>]*>");
                if (m.Success)
                {
                    newUrl = HttpUtility.UrlDecode(m.Groups["url"].Value);
                    if (!Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                    {
                        Uri uri = null;
                        if (Uri.TryCreate(new Uri(url), newUrl, out uri))
                        {
                            return SiteUtilBase.GetRedirectedUrl(uri.ToString() + "&start=0");
                        }
                    }
                }
            }
            return String.Empty;
        }
    }
}
