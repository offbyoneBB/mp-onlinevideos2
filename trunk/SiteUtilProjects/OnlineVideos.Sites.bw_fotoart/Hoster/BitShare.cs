using System;
using System.Net;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class BitShare : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "bitshare.com";
        }

        private string requestFileInformation(string url, CookieContainer cc)
        {
            string webData = WebCache.Instance.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(webData))
            {
                if (!string.IsNullOrEmpty(Helpers.StringUtils.GetRegExData(@"(?<exists>This\sfile\sdoesn\'t\sexist,\sor\shas\sbeen\sremoved\s?\.)", webData, "exists")))
                    webData = string.Empty;
            }
            return webData;
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = requestFileInformation(url, cc);
            if (string.IsNullOrEmpty(webData)) return string.Empty;
            
            //Grab ajaxid
            string ajaxdl = Helpers.StringUtils.GetSubString(webData, @"var ajaxdl = """, @""""); 
            //Grab Post url
            string posturl = Helpers.StringUtils.GetSubString(webData, @"url: """, @""""); 

            //Send Postdata (simulates a button click)
             string postData = @"request=generateID&ajaxid="+ajaxdl;
             string page = WebCache.Instance.GetWebData(url, postData, cc);

            //Grab file url from html
            Match n = Regex.Match(page, @"scaling:\s'fit',\s*url:\s'(?<url>[^']*)'");

            if (n.Success)
            {
                return n.Groups["url"].Value;
            }
			return String.Empty;
        }
    }
}