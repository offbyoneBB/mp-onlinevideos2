using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class VideoWeed : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Videoweed.es";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                Match m = Regex.Match(page, @"flashvars\.file=""(?<fileid>[^""]*)"";\s*flashvars\.filekey=""(?<filekey>[^""]*)"";");
                if (m.Success)
                {
                    page = SiteUtilBase.GetWebData(
                        String.Format("http://www.videoweed.es/api/player.api.php?pass=undefined&codes=undefined&user=undefined&file={0}&key={1}",
                        m.Groups["fileid"].Value, HttpUtility.UrlEncode(m.Groups["filekey"].Value)));
                    m = Regex.Match(page, @"url=(?<url>[^&]*)&");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }
            }
            return String.Empty;
        }
    }
}
