using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Net;

namespace OnlineVideos.Hoster
{
    public class Loaded : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Loaded.it";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string first = SiteUtilBase.GetWebData(url, cc);
            string code = Regex.Match(first, @"name=""code""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
            string second = SiteUtilBase.GetWebDataFromPost(url, "code=" + code, cc, url);
            Match n = Regex.Match(second, @"url:\s'(?<url>.*/get/[^']+)'");
            if (n.Success)
            {
                videoType = VideoType.flv;
                return n.Groups["url"].Value;
            }
            /*else
            {
                string code2 = Regex.Match(first, @"name=""code""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
                string hash = Regex.Match(first, @"name=""hash""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
                string hostname = Regex.Match(first, @"name=""hostname""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;
                string filename = Regex.Match(first, @"name=""filename""\svalue=""(?<value>[^""]+)""").Groups["value"].Value;

                string postdata = "code=" + code2 +
                  "&hash=" + hash +
                  "&hostname=" + hostname +
                  "&filename=" + filename;

                string third = SiteUtilBase.GetWebDataFromPost("http://loaded.it/modules/getfile.php", postdata, cc, url);
                n = Regex.Match(third, @"url:\s'(?<url>.*get/[^']+)'");
                if (n.Success)
                {
                    videoType = VideoType.flv;
                    return n.Groups["url"].Value;
                }
            }*/
            return String.Empty;
        }
    }
}
