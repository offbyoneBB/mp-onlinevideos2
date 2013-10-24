using System;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class MovShare : MyHosterBase
    {
        public override string getHosterUrl()
        {
            return "Movshare.net";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                return ParseData(page);
            }
            return String.Empty;
        }

        public string ParseData(string data)
        {
            string step1 = WiseCrack(data);
            Match m = Regex.Match(step1, @"\}\('(?<p>.+?)[^\\]',(?<a>[^,]*),(?<c>[^,]*),[^']*'(?<k>\|[^']*)'.split\('\|'\),(?<e>[^,]*),(?<d>[^\)]*)\)");
            if (m.Success)
            {
                data = Unpack(m.Groups["p"].Value, Int32.Parse(m.Groups["a"].Value), Int32.Parse(m.Groups["c"].Value), m.Groups["k"].Value.Split('|'),
                    Int32.Parse(m.Groups["e"].Value), m.Groups["d"].Value);
            }


            m = Regex.Match(data, @"flashvars\.file=""(?<fileid>[^""]*)"";\s*flashvars\.filekey=(?<filekey>[^;]*);");
            if (m.Success)
            {
                string fileKey = m.Groups["filekey"].Value;
                string fileId = m.Groups["fileid"].Value;
                while (m.Success && !fileKey.Contains("."))
                {
                    m = Regex.Match(data, String.Format(@"var\s{0}=(?<newval>[^;]*);", fileKey));
                    if (m.Success)
                        fileKey = m.Groups["newval"].Value;
                }
                fileKey = fileKey.Trim('"').Replace(".", "%2E").Replace("-", "%2D");
                data = SiteUtilBase.GetWebData(
                    String.Format("http://www.movshare.net/api/player.api.php?pass=undefined&codes=undefined&user=undefined&file={0}&key={1}",
                    fileId, fileKey));
                m = Regex.Match(data, @"url=(?<url>[^&]*)&");
                if (m.Success)
                    return m.Groups["url"].Value;
            }
            return String.Empty;
        }

    }
}
