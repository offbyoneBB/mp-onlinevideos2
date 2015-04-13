using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Videomega : HosterBase, IReferer
    {
        private string refererUrl = null;

        public override string GetHosterUrl()
        {
            return "videomega.tv";
        }

        public override string GetVideoUrl(string url)
        {
            if (url.ToLower().Contains("/cdn.php"))
            {
                string refUrl = RefererUrl;
                //Clear referer
                RefererUrl = null;
                string data = GetWebData(url, referer: refUrl, cache: false, userAgent: "Apple-iPhone/701.341");
                Regex rgx = new Regex(@"<source.*?src=""(?<url>[^""]*)");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    MPUrlSourceFilter.HttpUrl httpUrl = new MPUrlSourceFilter.HttpUrl(m.Groups["url"].Value);
                    httpUrl.Referer = url;
                    httpUrl.UserAgent = "Apple-iPhone/701.341";
                    return httpUrl.ToString();
                }
            }
            else
            {
                //doskabouter impl
                int p = url.IndexOf('?');
                string url2 = url.Insert(p, "iframe.php");
                string webData = WebCache.Instance.GetWebData(url2);
                Match m = Regex.Match(webData, @"document\.write\(unescape\(""(?<data>[^""]*)""\)\);");
                if (m.Success)
                {
                    string data = HttpUtility.UrlDecode(m.Groups["data"].Value);
                    m = Regex.Match(data, @"file:\s""(?<url>[^""]*)"",");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }
            }
            return String.Empty;
        }

        public string RefererUrl
        {
            get
            {
                return refererUrl;
            }
            set
            {
                refererUrl = value;
            }
        }
    }
}
