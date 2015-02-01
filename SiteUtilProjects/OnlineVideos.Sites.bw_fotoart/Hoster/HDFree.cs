using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;


namespace OnlineVideos.Hoster
{
    public class HDFree : HosterBase
    {
        public override string getHosterUrl()
        {
            return "my-entertainment.biz";
        }

        private string requestFileInformation(string url, CookieContainer cc)
        {
            string webData = SiteUtilBase.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(webData))
            {
                if (!string.IsNullOrEmpty(getRegExData(@"(?<exists>This\sfile\sdoesn\'t\sexist,\sor\shas\sbeen\sremoved\s?\.)", webData, "exists")))
                    webData = string.Empty;
            }
            return webData;
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            CookieContainer cc = new CookieContainer();
            string webData1 = requestFileInformation("http://my-entertainment.biz/forum/content.php?r=3938", cc);
            string page = SiteUtilBase.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(page))
            {
                //Extract file url from HTML
                Match n = Regex.Match(page, @"file=(?<url>[^&]*)");
                string decodedUrl = HttpUtility.UrlDecode(n.Value);
                string str = decodedUrl.Remove(0, 5);
                 if (n.Success)
                {
                    videoType = VideoType.flv;
                    return str;
                }
            }
            return String.Empty;
        }
    }
}
