using System;
using System.Net;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class BitShare : HosterBase
    {
        public override string getHosterUrl()
        {
            return "bitshare.com";
        }

        private string requestFileInformation(string url, CookieContainer cc)
        {
            string webData = SiteUtilBase.GetWebData(url, cc);
            if (!string.IsNullOrEmpty(webData))
            {
                if (!string.IsNullOrEmpty(getRegExData(@"(?<exists>This\sfile\sdoesn\'t\sexist,\sor\shas\sbeen\sremoved\s?\.)", webData, "exists")))
                    webData = string.Empty;
            }
            return webData;
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = requestFileInformation(url, cc);
            if (string.IsNullOrEmpty(webData)) return string.Empty;
            
            //Grab ajaxid
            string ajaxdl = GetSubString(webData, @"var ajaxdl = """, @""""); 
            //Grab Post url
            string posturl = GetSubString(webData, @"url: """, @""""); 

            //Send Postdata (simulates a button click)
             string postData = @"request=generateID&ajaxid="+ajaxdl;
             string page = GenericSiteUtil.GetWebDataFromPost(url, postData, cc);

            //Grab file url from html
            Match n = Regex.Match(page, @"scaling:\s'fit',\s*url:\s'(?<url>[^']*)'");

            if (n.Success)
            {
                videoType = VideoType.flv;
                return n.Groups["url"].Value;
            }
			return String.Empty;
        }
    }
}