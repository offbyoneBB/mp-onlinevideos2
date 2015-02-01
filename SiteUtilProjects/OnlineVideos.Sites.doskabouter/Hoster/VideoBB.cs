using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OnlineVideos.Hoster.Base;
using System.Xml;
using System.Web;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class VideoBB : HosterBase
    {
        public override string getHosterUrl()
        {
            return "videobb.com";
        }

        private string requestFileInformation(string url, CookieContainer cc)
        {
            string webData = SiteUtilBase.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(webData))
            {
                if (!string.IsNullOrEmpty(getRegExData(@"(?<exists>This\video\swas\seither\sdeleted\sby\sthe\suser\sor\sin\sbreach\sof\sa\scopyright\sholder|Video\sis\snot\savailable)", webData, "exists")))
                    webData = string.Empty;
            }
            return webData;
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = requestFileInformation(url, cc);
            if (string.IsNullOrEmpty(webData)) return string.Empty;

            string setting = getRegExData(@"<param value=""setting=(?<setting>[^""]+)""", webData, "setting");
            if (string.IsNullOrEmpty(setting)) return string.Empty;
            byte[] temp = Convert.FromBase64String(setting);
            setting = Encoding.ASCII.GetString(temp);

            webData = SiteUtilBase.GetWebData(setting, cookies: cc);
            if (string.IsNullOrEmpty(webData)) return string.Empty;

            string dlLink = getRegExData(@"""res"":\[.*?\{""d"":(?:true|false),""\w+"":""\w+"",""u"":""(?<url>[^""]+)""[^\}]*\}\]", webData, "url");
            if (string.IsNullOrEmpty(dlLink)) return string.Empty;
            temp = Convert.FromBase64String(dlLink);
            dlLink = Encoding.ASCII.GetString(temp);

            return dlLink;
        }

        /* OLD METHOD
        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            url = GetSubString(webData, @"""setting=", @"""");
            byte[] tmp = Convert.FromBase64String(url);
            url = Encoding.ASCII.GetString(tmp);

            webData = SiteUtilBase.GetWebData(url);
            url = GetSubString(webData, @"""token1"":""", @"""");
            tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }
        */
    }
}
