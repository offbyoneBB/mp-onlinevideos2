using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using OnlineVideos.Hoster;
using System.Xml;
using System.Web;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class PutLocker : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "putlocker.com";
        }


        public override string GetVideoUrl(string url)
        {
            url = WebCache.Instance.GetRedirectedUrl(url);
            string webData = WebCache.Instance.GetWebData(url);
            if (string.IsNullOrEmpty(webData)) return string.Empty;

            Match m = Regex.Match(webData, @"<input\stype=""hidden""\sname=""confirm""\svalue=""(?<postdata>[^""]*)""", MyHosterBase.DefaultRegexOptions);

            if (!m.Success) return String.Empty;

            string postData = "confirm=" + HttpUtility.UrlEncode(m.Groups["postdata"].Value);
            string webDataLink = WebCache.Instance.GetWebData(url, postData);
            m = Regex.Match(webDataLink, @"<div\sid='fd_dl_drpdwn'>\s*<a\shref=""(?<url>[^""]*)""\starget=""_blank""\sid='top_external_download'\stitle='Download\sThis\sFile'>", MyHosterBase.DefaultRegexOptions);
            if (!m.Success) return String.Empty;
            return m.Groups["url"].Value;
        }
    }
}
