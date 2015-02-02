using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class NovaMov : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "Novamov.com";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            return ParseData(page);
        }

        private string ParseData(string webData)
        {
            string step1 = WiseCrack(webData);

            string link = HttpUtility.UrlDecode(FlashProvider(step1));
            if (!String.IsNullOrEmpty(link))
            {
                return link;
            }

            return String.Empty;

        }
    }
}
