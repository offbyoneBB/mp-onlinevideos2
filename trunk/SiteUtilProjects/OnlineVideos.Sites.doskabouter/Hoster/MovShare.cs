using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class MovShare : HosterBase
    {
        private string HUMAN = "We need you to prove you're human";
        
        public override string getHosterUrl()
        {
            return "Movshare.net";
        }

        public override string getVideoUrls(string url)
        {
            //string page = SiteUtilBase.GetWebData(url.Substring(0, url.LastIndexOf("/")));
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                if (page.IndexOf(HUMAN, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    page = SiteUtilBase.GetWebDataFromPost(url, "ndl=1&submit.x=1&submit.y=1");
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
                    videoType = VideoType.flv;
                    return link;
                }
            }
            return String.Empty;
        }
    }
}
