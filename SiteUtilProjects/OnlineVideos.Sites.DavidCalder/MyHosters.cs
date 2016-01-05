using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster;
using OnlineVideos.Sites;
using System.Net;

namespace OnlineVideos.Sites.DavidCalder
{
    public class Uploadc : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "uploadc";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string postData = String.Empty;
            string[] parts = url.Split(new[] { "/" }, StringSplitOptions.None);
            string Referer = "http://uploadc.com/player/6.6/jwplayer.flash.swf";

            string data = WebCache.Instance.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");

                while (match.Success)
                {
                    if (!String.IsNullOrEmpty(postData))
                        postData += "&";
                    postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                    match = match.NextMatch();
                }
                data = WebCache.Instance.GetWebData(url, postData, cc, url);
                System.Threading.Thread.Sleep(Convert.ToInt32(3) * 1001);
                data = WebCache.Instance.GetWebData(url, postData, cc, url);

                Match n1 = Regex.Match(data, @"""sources""\s:\s\[\s*{\s*""file""\s:\s""(?<url>[^""]+)"",\s*""default""\s:\strue,\s*""label""\s:\s""720""\s*}\s*\]");
                if (n1.Success)
                    return WebCache.Instance.GetRedirectedUrl(n1.Groups["url"].Value, Referer);

                Match n = Regex.Match(data, @"s1.addVariable\('file','(?<url>[^']*)'\)");
                if (n.Success)

                    return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }

    public class Megavideoz : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "megavideoz";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            Match n = Regex.Match(webData, @"<meta\sproperty='og:url'\scontent='(?<url>[^']+)'/>");
            if (n.Success)
            {
                webData = WebCache.Instance.GetWebData(n.Groups["url"].Value);
                return n.Groups["url"].Value;
            }
            else return string.Empty;
        }
    }

    public class Allmyvideos : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "allmyvideos";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string data = WebCache.Instance.GetWebData(url, cookies: cc);

            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");
                if (match.Success)
                {
                    string postData = String.Empty;
                    while (match.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                        match = match.NextMatch();
                    }
                    data = WebCache.Instance.GetWebData(url, postData, cc, url);
                }

                MatchCollection matches = Regex.Matches(data, @"{\s*""file""\s*:\s""(?<url>[^""]+)"",\s*""default""\s:\s[^,]*,\s*""label""\s:\s""[^""]*""\s*}");
                if (matches.Count != 0)
                {
                    return matches[matches.Count - 1].Groups["url"].Value;
                }

                //<p id="content">File was deleted</p>
                Match noFile = Regex.Match(data, @"<p\sid=""content"">(?<msg>[^<]*)</p>");
                if (noFile.Success)
                    Log.Info("Hoster Result : " + noFile.Groups["msg"].Value);
            }
            return url;
        }
    }

    public class Vidspot : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidspot";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string data = WebCache.Instance.GetWebData(url, cookies: cc);

            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");

                if (match.Success)
                {
                    string postData = string.Empty;
                    while (match.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                        match = match.NextMatch();
                    }
                    data = WebCache.Instance.GetWebData(url, postData, cc, url);
                }

                MatchCollection matches = Regex.Matches(data, @"{\s*""file""\s*:\s""(?<url>[^""]+)"",\s*""default""\s:\s[^,]*,\s*""label""\s:\s""[^""]*""\s*}");
                if (matches.Count != 0)
                {
                    return matches[matches.Count - 1].Groups["url"].Value;
                }

                //<p id="content">File was deleted</p>
                Match noFile = Regex.Match(data, @"<p\sid=""content"">(?<msg>[^<]*)</p>");
                if (noFile.Success)
                    Log.Info("Hoster Result : " + noFile.Groups["msg"].Value);
            }
            return String.Empty;
        }
    }

    public class Mightyupload : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "mightyupload";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();

            string data = WebCache.Instance.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<IFRAME\sSRC=""(?<url>[^""]*)""");
                if (match.Success)
                    data = WebCache.Instance.GetWebData(match.Groups["url"].Value);
                Match n = Regex.Match(data, @"file:\s'(?<url>[^']+)'");
                if (n.Success)
                    return WebCache.Instance.GetRedirectedUrl(n.Groups["url"].Value);

            }
            return url;
        }
    }

    public class Vshare : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "vshare.eu";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string postData = String.Empty;
            string Referer = "http://vshare.eu/player/6.6/jwplayer.flash.swf";

            string data = WebCache.Instance.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");

                while (match.Success)
                {
                    if (!String.IsNullOrEmpty(postData))
                        postData += "&";
                    postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                    match = match.NextMatch();
                }
                postData += "&referer=&method_free=Continue";

                data = WebCache.Instance.GetWebData(url, postData, cc, url);
                System.Threading.Thread.Sleep(Convert.ToInt32(3) * 1001);
                data = WebCache.Instance.GetWebData(url, postData, cc, url);

                Match n = Regex.Match(data, @"config:{file:'(?<url>[^']+)','provider':'http'}");
                if (n.Success)

                    return WebCache.Instance.GetRedirectedUrl(n.Groups["url"].Value, Referer);

            }
            return url;
        }
    }

    public class Played : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "played";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string postData = String.Empty;
            string Referer = "http://played.to/player/6.6/jwplayer.flash.swf";

            string data = WebCache.Instance.GetWebData(url, cookies: cc);
            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");

                while (match.Success)
                {
                    if (!String.IsNullOrEmpty(postData))
                        postData += "&";
                    postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                    match = match.NextMatch();
                }
                postData += "&imhuman=Continue+to+Video";

                data = WebCache.Instance.GetWebData(url, postData, cc, url);
                System.Threading.Thread.Sleep(1 * 1001);
                Match n = Regex.Match(data, @"{\s*file:\s*""(?<url>[^""]*)""");
                if (n.Success)
                    return WebCache.Instance.GetRedirectedUrl(n.Groups["url"].Value, Referer);
            }
            return url;
        }
    }

    public class Vodlocker : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "vodlocker";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string data = WebCache.Instance.GetWebData(url, cookies: cc);

            if (!string.IsNullOrEmpty(data))
            {
                Match match = Regex.Match(data, @"<input\stype=""hidden""\sname=""(?<name>[^""]+)""\svalue=""(?<value>[^""]+)"">");
                if (match.Success)
                {
                    string postData = String.Empty;
                    while (match.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += match.Groups["name"].Value + "=" + match.Groups["value"].Value;
                        match = match.NextMatch();
                    }

                    string timeToWait = Regex.Match(data, @"<span\sid=""countdown_str"">[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
                    if (Convert.ToInt32(timeToWait) < 10)
                        System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);
                    postData = postData.Replace("op=search&op=download1&", "op=download1&usr_login=&");
                    postData = postData.Insert(postData.IndexOf("&hash="), "&referer=" + "http://played.to/player/6.6/jwplayer.flash.swf");
                    postData += "&imhuman=Proceed+to+video";

                    data = WebCache.Instance.GetWebData(url, postData, cc, url);
                }

                //setup({  file:  "http://fsd3.vodlocker.com:8777/oscebol4cc4pcnokakccn54v5u7hzw4y5gymapaeqahyfctwojj4jvjut4/v.mp4"
                Match n = Regex.Match(data, @"setup\({\s*file:\s*""(?<url>[^""]+)""");
                if (n.Success)
                    return n.Groups["url"].Value;

                        //<p id="content">File was deleted</p>
                Match noFile = Regex.Match(data, @"<span[^>]*>(?<msg>[^<]*)</span>");
                if (noFile.Success)
                    throw new OnlineVideosException(noFile.Groups["msg"].Value);

            }
            return url;
        }
    }
}
