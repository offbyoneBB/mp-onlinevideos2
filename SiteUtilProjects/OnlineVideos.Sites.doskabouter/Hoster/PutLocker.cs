using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using OnlineVideos.Hoster.Base;
using System.Xml;
using System.Web;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class PutLocker : HosterBase
    {
        public override string getHosterUrl()
        {
            return "putlocker.com";
        }


        public override string getVideoUrls(string url)
        {
            url = SiteUtilBase.GetRedirectedUrl(url);
            string webData = SiteUtilBase.GetWebData(url);
            if (string.IsNullOrEmpty(webData)) return string.Empty;

            Match m = Regex.Match(webData, @"<input\stype=""hidden""\sname=""confirm""\svalue=""(?<postdata>[^""]*)""", defaultRegexOptions);

            if (!m.Success) return String.Empty;

            string postData = "confirm=" + HttpUtility.UrlEncode(m.Groups["postdata"].Value);
            string webDataLink = SiteUtilBase.GetWebDataFromPost(url, postData);
            m = Regex.Match(webDataLink, @"<div\sid='fd_dl_drpdwn'>\s*<a\shref=""(?<url>[^""]*)""\starget=""_blank""\sid='top_external_download'\stitle='Download\sThis\sFile'>", defaultRegexOptions);
            if (!m.Success) return String.Empty;
            return m.Groups["url"].Value;
        }
    }
}
