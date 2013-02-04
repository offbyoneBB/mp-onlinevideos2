using System;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class NowVideo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "NowVideo.eu";
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            string page = SiteUtilBase.GetWebData(url);


            //Grab hidden value file & filekey
            string fileValue = GetSubString(page, @"flashvars.file=""", @""";");
            string filekeyValue = GetSubString(page, @"flashvars.filekey=""", @""";");

            //create url
            string url2 = "http://www.nowvideo.eu/api/player.api.php?key=" + filekeyValue + "&pass=undefined&file=" + fileValue + "&user=undefined&codes=1";

            //Get HTML from url
            string weData = SiteUtilBase.GetWebData(url2);

            //Grab url from html
            string res = GetSubString(weData, @"url=", @"&");

            if (!String.IsNullOrEmpty(res))
            {
                videoType = VideoType.flv;
                return res;
            }
            return String.Empty;
        }
    }
}
