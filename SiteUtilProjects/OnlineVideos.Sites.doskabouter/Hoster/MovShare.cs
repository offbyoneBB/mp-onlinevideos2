using System;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class MovShare : WholeCloud
    {
        public override string GetHosterUrl()
        {
            return "Movshare.net";
        }

        public override string GetVideoUrl(string url)
        {
            url = WebCache.Instance.GetRedirectedUrl(url);
            return base.GetVideoUrl(url);
        }

        protected string ParseData(string data)
        {
            string step1 = WiseCrack(data);
            if (String.IsNullOrEmpty(step1))
                step1 = data;
            Match m = Regex.Match(step1, @"\}\('(?<p>.+?)[^\\]',(?<a>[^,]*),(?<c>[^,]*),[^']*'(?<k>\|[^']*)'.split\('\|'\),(?<e>[^,]*),(?<d>[^\)]*)\)");
            if (m.Success)
            {
                data = Helpers.StringUtils.Unpack(m.Groups["p"].Value, Int32.Parse(m.Groups["a"].Value), Int32.Parse(m.Groups["c"].Value), m.Groups["k"].Value.Split('|'),
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
                data = WebCache.Instance.GetWebData(
                    String.Format("http://www.movshare.net/api/player.api.php?pass=undefined&codes=undefined&user=undefined&file={0}&key={1}",
                    fileId, fileKey));
                m = Regex.Match(data, @"url=(?<url>[^&]*)&");
                if (m.Success)
                    return m.Groups["url"].Value;
            }
            return String.Empty;
        }

    }

    public class WholeCloud : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "wholecloud.net";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                page = GetFromPost(url, page);
                Match m = Regex.Match(page, @"flashvars\.file=""(?<file>[^""]*)"";\s*flashvars\.filekey=""(?<filekey>[^""]*)"";\s*flashvars\.advURL=""[^""]*"";\s*if\s\(typeof\sv728x90\s!==\s'undefined'\)\s{\s*flashvars\.cid=""(?<cid>[^""]*)""");
                if (m.Success)
                {
                    url = String.Format(@"http://www.wholecloud.net/api/player.api.php?key={0}&cid3=wholecloud%2Enet&file={1}&user=undefined&numOfErrors=0&cid2=undefined&pass=undefined&cid={2}",
                   HttpUtility.UrlEncode(m.Groups["filekey"].Value), m.Groups["file"].Value, m.Groups["cid"].Value);
                    page = GetWebData(url);
                    m = Regex.Match(page, @"url=(?<url>.*)");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }

                //0:88%2E159%2E164%2E124%2Dba90bb83524f4fb8d65a9692923c5d54
                //1:wholecloud%2Enet
                //2:696178b4b52f6
                //3:1
            }

            return String.Empty;
        }


    }
}
