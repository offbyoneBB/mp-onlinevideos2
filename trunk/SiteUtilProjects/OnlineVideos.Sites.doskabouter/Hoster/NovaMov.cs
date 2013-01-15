using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class NovaMov : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Novamov.com";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);

            Match n = Regex.Match(page, @"flashvars.file=""(?<file>[^""]+)"";\s*flashvars.filekey=""(?<key>[^""]+)"";\s*flashvars.advURL=""[^""]*""");
            if (n.Success)
            {
                string tmpUrl = string.Format(@"http://www.novamov.com/api/player.api.php?key={0}&user=undefined&codes={1}&file={2}&pass=undefined",
                    HttpUtility.UrlEncode(n.Groups["key"].Value),
                    n.Groups["id"].Value,
                    n.Groups["file"].Value);
                page = SiteUtilBase.GetWebData(tmpUrl);
                n = Regex.Match(page, @"url=(?<url>.+?)&\w+=");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value))
                    return n.Groups["url"].Value;
            }

            videoType = VideoType.flv;
            return FlashProvider(url, page);
        }
    }
}
