using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class DivxStage : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "DivxStage.eu";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
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
                    return link;
                }

                //divx
                link = DivxProvider(url, page);
                if (!string.IsNullOrEmpty(link))
                {
                    return link;
                }

                //other
                Match n = Regex.Match(page, @"src""\svalue=""(?<url>.*?)""");
                if (n.Success)
                {
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }

    }

    public class DivxStageNet : DivxStage
    {
        public override string GetHosterUrl()
        {
            return "DivxStage.net";
        }
    }

    public class DivxStageTo : DivxStage
    {
        public override string GetHosterUrl()
        {
            return "DivxStage.to";
        }
    }
}
