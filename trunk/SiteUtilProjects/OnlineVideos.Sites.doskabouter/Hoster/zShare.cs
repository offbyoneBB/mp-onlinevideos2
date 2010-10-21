using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using System.Net;
using OnlineVideos.Sites;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class zShare : HosterBase
    {
        public override string getHosterUrl()
        {
            return "zShare.net";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string data = SiteUtilBase.GetWebData(HttpUtility.HtmlDecode(url), cc);
            if (!data.Contains(@"name=""flashvars"" value=""") && data.Contains(@"<iframe src="""))
            {
                string tmp = GetSubString(data, @"<iframe src=""", @"""");
                cc = new CookieContainer();
                data = SiteUtilBase.GetWebData(HttpUtility.HtmlDecode(tmp), cc);
            }

            CookieCollection ccol = cc.GetCookies(new Uri("http://tmp.zshare.net"));
            if (data.Contains(@"name=""flashvars"" value="""))
            {
                data = GetSubString(data, @"name=""flashvars"" value=""", "&player");
                Dictionary<string, string> dic = new Dictionary<string, string>();
                string[] tmp = data.Split('&');
                foreach (string s in tmp)
                {
                    string[] t = s.Split('=');
                    dic[t[0]] = t[1];

                }
                string hash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(dic["filename"] + "tr35MaX25P7aY3R", "MD5").ToLower();
                string turl = @"http://" + dic["serverid"] + ".zshare.net/stream/" + dic["hash"] + '/' + dic["fileid"] + '/' +
                    dic["datetime"] + '/' + dic["filename"] + '/' + hash + '/' + dic["hnic"];
                foreach (Cookie cook in ccol)
                    CookieHelper.SetIECookie(String.Format("http://{0}.zshare.net", dic["serverid"]), cook);

                videoType = VideoType.flv;
                return turl;
            }
            return String.Empty;
        }

    }
}
