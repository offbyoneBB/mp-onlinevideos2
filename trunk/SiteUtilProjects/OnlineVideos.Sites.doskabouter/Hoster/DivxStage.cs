using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxStage : MyHosterBase
    {
        public override string getHosterUrl()
        {
            return "DivxStage.eu";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                //Even newer:
                string newMethod = WiseCrack(page);
                if (!String.IsNullOrEmpty(newMethod))
                    page = newMethod;

                //new method:
                Match m = Regex.Match(page, @"flashvars\.domain=""(?<domain>[^""]*)"";\s*flashvars\.file=""(?<file>[^""]*)"";\s*flashvars\.filekey=""(?<filekey>[^""]*)"";");
                if (m.Success)
                {
                    string fileKey = m.Groups["filekey"].Value.Replace(".", "%2E").Replace("-", "%2D");
                    string url2 = String.Format(@"{0}/api/player.api.php?key={1}&user=undefined&codes=1&pass=undefined&file={2}",
                        m.Groups["domain"].Value, fileKey, m.Groups["file"].Value);
                    page = SiteUtilBase.GetWebData(url2);
                    m = Regex.Match(page, @"url=(?<url>[^&]*)&");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }
                //divx
                string link = DivxProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    videoType = VideoType.divx;
                    return link;
                }
                //flv
                link = FlashProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    int index = link.IndexOf(".flv", StringComparison.CurrentCultureIgnoreCase);
                    if (index >= 0)
                        link = link.Substring(0, index + 4);

                    videoType = VideoType.flv;
                    return link;
                }
                //other
                Match n = Regex.Match(page, @"src""\svalue=""(?<url>.*?)""");
                if (n.Success)
                {
                    videoType = VideoType.unknown;
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }

    }

    public class DivxStageNet : DivxStage
    {
        public override string getHosterUrl()
        {
            return "DivxStage.net";
        }
    }
}
