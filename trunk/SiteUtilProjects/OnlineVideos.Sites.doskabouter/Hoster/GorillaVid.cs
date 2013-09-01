using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class GorillaVid : HosterBase
    {
        public override string getHosterUrl()
        {
            return "GorillaVid.in";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                string sWaitTime = getRegExData(@"Wait\s(?:<(.|\n)*?>)?(?<waittime>\d*?)(?:<(.|\n)*?>)?\sseconds", page, "waittime");
                int iWaitTime = 5;
                if (!string.IsNullOrEmpty(sWaitTime))
                {
                    if (!int.TryParse(sWaitTime, out iWaitTime))
                        iWaitTime = 5;
                }

                //Thread.Sleep(iWaitTime * 1001);

                //Dictionary<string, string> post = new Dictionary<string, string>();
                string postData = string.Empty;

                Match m = Regex.Match(page, @"<input\s*?type=""hidden""\s*?name=""(?<name>[^\'""]+)""\s*?value=""(?<value>[^\'""]+)"".*?>");
                while (m != null && m.Success)
                {
                    string key = m.Groups["name"].Value;
                    string value = m.Groups["value"].Value;
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        postData = postData + string.Format("{0}={1}&", key, HttpUtility.UrlEncode(value));

                        //if (post.ContainsKey(key))
                        //    post[key] = value;
                        //else
                        //    post.Add(key, value);
                    }
                    m = m.NextMatch();
                }

                if (!string.IsNullOrEmpty(postData))
                {
                    postData.Remove(postData.Length - 1, 1);
                    //postData = HttpUtility.UrlEncode(postData);
                    //postData = postData.Replace(' ', '+');

                    if (!string.IsNullOrEmpty(postData))
                        page = SiteUtilBase.GetWebDataFromPost(url, postData);
                }
                //file:\s*'(?<Title>[^"]*)',
                Match n = Regex.Match(page, @"file:\s*'(?<url>[^']*)'");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
                n = Regex.Match(page, @"file:\s*""(?<url>[^""]*)""");
                if (n.Success && Utils.IsValidUri(n.Groups["url"].Value)) return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }

    public class GorillaVidCom : GorillaVid
    {
        public override string getHosterUrl()
        {
            return "GorillaVid.com";
        }

        public override string getVideoUrls(string url)
        {
            url = SiteUtilBase.GetRedirectedUrl(url);
            return base.getVideoUrls(url);
        }
    }

}
