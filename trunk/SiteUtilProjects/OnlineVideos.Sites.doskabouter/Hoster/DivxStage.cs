using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxStage : HosterBase
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
