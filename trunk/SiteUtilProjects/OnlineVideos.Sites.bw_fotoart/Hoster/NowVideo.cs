using System;

using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class NowVideo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "NowVideo.eu";
        }

        public override string GetVideoUrl(string url)
        {
            //Get HTML from url
            string page = WebCache.Instance.GetWebData(url);


            //Grab hidden value file & filekey
            string fileValue = Helpers.StringUtils.GetSubString(page, @"flashvars.file=""", @""";");
            string filekeyValue = Helpers.StringUtils.GetSubString(page, @"flashvars.filekey=""", @""";");

            //create url
            string url2 = "http://www.nowvideo.eu/api/player.api.php?key=" + filekeyValue + "&pass=undefined&file=" + fileValue + "&user=undefined&codes=1";

            //Get HTML from url
            string weData = WebCache.Instance.GetWebData(url2);

            //Grab url from html
            string res = Helpers.StringUtils.GetSubString(weData, @"url=", @"&");

            if (!String.IsNullOrEmpty(res))
            {
                return res;
            }
            return String.Empty;
        }
    }
}
