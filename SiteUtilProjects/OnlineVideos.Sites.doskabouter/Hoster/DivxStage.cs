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
                string link = FlashProvider(page);
                if (!String.IsNullOrEmpty(link))
                {
                    videoType = VideoType.flv;
                    return link;
                }

                //divx
                link = DivxProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    videoType = VideoType.divx;
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
