using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Duckload : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Duckload.com";
        }

        public override string getVideoUrls(string url)
        {
            if (url.Contains("flash"))
            {
                string page = SiteUtilBase.GetWebData(url +"?get=config");
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"video:\s(?<url>stream[^;]+);");
                    if (n.Success)
                    {
                        videoType = VideoType.flv;
                        return SiteUtilBase.GetRedirectedUrl("http://flash.duckload.com/video/player.swf?get=" + n.Groups["url"].Value + "&start=0");
                    }
                }
            }
            else
            {
                string page = SiteUtilBase.GetWebDataFromPost(url, "server=1&sn=" + System.Web.HttpUtility.UrlEncode("Stream Starten", Encoding.GetEncoding("latin1")));
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"video/divx""\ssrc=""(?<url>[^""]+)""");
                    if (n.Success)
                    {
                        videoType = VideoType.divx;
                        return n.Groups["url"].Value;
                    }
                }
            }
            return "";
        }
    }
}
