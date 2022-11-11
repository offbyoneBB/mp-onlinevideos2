using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Threading;
using System.Xml;
using System.Linq;
using OnlineVideos.MPUrlSourceFilter;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{

    public class AuroraVid : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "auroravid.to";
        }

        public override string GetVideoUrl(string url)
        {
            url = WebCache.Instance.GetRedirectedUrl(url);
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                page = GetFromPost(url, page, true);
                Match m = Regex.Match(page, @"<source\s*src=""(?<url>[^""]*)""");
                if (m.Success)
                    return m.Groups["url"].Value;
            }
            return null;
        }
    }


    public class BlipTv : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "blip.tv";
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            string s = HttpUtility.UrlDecode(WebCache.Instance.GetRedirectedUrl(url));
            int p = s.IndexOf("file=");
            if (p > -1)
            {
                int q = s.IndexOf('&', p);
                if (q < 0) q = s.Length;
                s = s.Substring(p + 5, q - p - 5);
                string rss = WebCache.Instance.GetWebData(s);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(rss);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("a", "http://search.yahoo.com/mrss/");
                XmlNodeList nodes = xmlDoc.SelectNodes("//a:group/a:content", nsmgr);
                foreach (XmlNode node in nodes)
                {
                    string videoUrl = node.Attributes["url"].Value;
                    string w = node.Attributes["width"].Value;
                    string h = node.Attributes["height"].Value;
                    if (!String.IsNullOrEmpty(w) && !String.IsNullOrEmpty(h) && !String.IsNullOrEmpty(videoUrl))
                    {
                        try
                        {
                            res.Add(String.Format("{0}x{1}", w, h), videoUrl);
                        }
                        catch { }
                    }

                }
            }
            else
            {

                string webData = WebCache.Instance.GetWebData(url);

                Match matchFileUrl = Regex.Match(webData, @"data-blip(?<n0>[^=]*)=""(?<m0>[^""]*)""");
                while (matchFileUrl.Success)
                {
                    string foundUrl = matchFileUrl.Groups["m0"].Value;
                    res.Add(matchFileUrl.Groups["n0"].Value, foundUrl);
                    matchFileUrl = matchFileUrl.NextMatch();
                }
            }

            return res;
        }
    }

    public class Cinshare : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "cinshare.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            string tmp = Helpers.StringUtils.GetSubString(webData, @"<iframe src=""", @"""");
            webData = WebCache.Instance.GetWebData(tmp);
            tmp = Helpers.StringUtils.GetSubString(webData, @"<param name=""src"" value=""", @"""");
            return WebCache.Instance.GetRedirectedUrl(tmp);
        }
    }

    public class ClipWatching : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "clipwatching.com";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            var m = Regex.Match(data, @"src:\s""(?<url>[^""]*)"",\stype:\s""application/x-mpegURL""");
            if (m.Success)
            {
                data = GetWebData(m.Groups["url"].Value);
                var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(data, m.Groups["url"].Value);
                return res.FirstOrDefault().Value;
            }
            if (data.IndexOf("The file you were looking for could not be found, sorry for any inconvenience.") >= 0)
                throw new OnlineVideosException("The file you were looking for could not be found, sorry for any inconvenience.");

            return null;
        }
    }

    public class CloudTime : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "cloudtime.to";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                page = GetFromPost(url, page, true);
                Match m = Regex.Match(page, @"<source\ssrc=""(?<url>[^""]*)""\stype='video/mp4'>");
                if (!m.Success)
                    m = Regex.Match(page, @"<a\shref=""(?<url>[^""]*)""\starget=""""\sclass=""btn\sdwlBtn""");
                if (m.Success)
                {
                    string result = m.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(result, UriKind.Absolute))
                    {
                        Uri uri = null;
                        if (Uri.TryCreate(new Uri(url), result, out uri))
                            result = uri.ToString();
                        else
                            result = string.Empty;
                    }
                    return result;
                }
            }
            return null;
        }
    }

    public class DailyMotion : HosterBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Proxy (requires custom proxytool running)")]
        string customProxy = null;
        public override string GetHosterUrl()
        {
            return "dailymotion.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            var m = Regex.Match(url, @"/video/(?<videoid>.*)$");
            if (m.Success)
                url = @"https://www.dailymotion.com/player/metadata/video/" + m.Groups["videoid"].Value;

            url = Sites.doskabouter.Helpers.CustomProxyHelper.GetProxyUrl(url, customProxy);

            string webData = WebCache.Instance.GetWebData(url);

            Dictionary<string, string> res = new Dictionary<string, string>();
            Match matchFileUrl = Regex.Match(webData, @"""qualities"":{""auto"":\[{""type"":""application\\/x-mpegURL"",""url"":""(?<url>[^""]+)""}");
            if (matchFileUrl.Success)
            {
                string foundUrl = matchFileUrl.Groups["url"].Value;
                foundUrl = Sites.doskabouter.Helpers.CustomProxyHelper.GetProxyUrl(foundUrl.Replace(@"\/", @"/"), customProxy);
                var m3u8Data = GetWebData(foundUrl);
                return Helpers.HlsPlaylistParser.GetPlaybackOptions(m3u8Data, foundUrl);
            }
            return res;
        }

        public override string GetVideoUrl(string url)
        {
            var res = GetPlaybackOptions(url);
            return res.FirstOrDefault().Key;
        }
    }

    public class Doodso : Dood
    {
        public override string GetHosterUrl()
        {
            return "dood.so";
        }
    }

    public class Doodla : Dood
    {
        public override string GetHosterUrl()
        {
            return "dood.la";
        }
    }

    public class Dood : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "dood.to";
        }

        public override string GetVideoUrl(string url)
        {
            UriBuilder ub = new UriBuilder(url.Replace("/d/", "/e/"));
            ub.Host = "dood.so";
            url = ub.Uri.ToString();
            var data = GetWebData(url);
            var m = Regex.Match(data, @"\$\.get\('(?<url>/pass[^']*)'");
            if (m.Success)
            {
                var tmpUrl = m.Groups["url"].Value;
                if (!Uri.IsWellFormedUriString(tmpUrl, UriKind.Absolute))
                {
                    Uri uri = null;
                    if (Uri.TryCreate(new Uri(url), tmpUrl, out uri))
                    {
                        tmpUrl = uri.ToString();
                    }
                }
                data = GetWebData(tmpUrl, referer: url);
                int i = tmpUrl.LastIndexOf('/');
                TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                var newurl = data + "SQPPQsSXKD?token=" + tmpUrl.Substring(i + 1) + "&expiry=" + Math.Truncate(span.TotalSeconds);
                HttpUrl finalUrl = new HttpUrl(newurl);
                finalUrl.Referer = "https://dood.so";
                return finalUrl.ToString();
            }
            m = Regex.Match(data, @"<title>(?<Title>[^<]*)</title>");
            if (m.Success)
                throw new OnlineVideosException(m.Groups["Title"].Value);
            return null;
        }
    }

    public class DoodWatch : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "dood.watch";
        }

        public override string GetVideoUrl(string url)
        {
            url = url.Replace("/d/", "/e/");
            var data = GetWebData(url);
            var m = Regex.Match(data, @"\$\.get\('(?<url>/pass[^']*)'");
            if (m.Success)
            {
                var tmpUrl = m.Groups["url"].Value;
                if (!Uri.IsWellFormedUriString(tmpUrl, UriKind.Absolute))
                {
                    Uri uri = null;
                    if (Uri.TryCreate(new Uri(url), tmpUrl, out uri))
                    {
                        tmpUrl = uri.ToString();
                    }
                }
                data = GetWebData(tmpUrl, referer: url);
                int i = tmpUrl.LastIndexOf('/');
                TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                var newurl = data + "SQPPQsSXKD?token=" + tmpUrl.Substring(i + 1) + "&expiry=" + Math.Truncate(span.TotalMilliseconds);
                HttpUrl finalUrl = new HttpUrl(newurl);
                finalUrl.Referer = "https://dood.watch";
                return finalUrl.ToString();
            }
            return null;
        }
    }

    public class EnterVIdeo : HosterBase, ISubtitle
    {
        string subUrl;

        public string SubtitleText
        {
            get
            {
                if (!String.IsNullOrEmpty(subUrl))
                {
                    var data = GetWebData(subUrl);
                    if (data.StartsWith(@"WEBVTT"))
                        data = Helpers.SubtitleUtils.Webvtt2SRT(data);

                    return data;
                }
                else
                    return null;
            }
        }

        public override string GetHosterUrl()
        {
            return "entervideo.net";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            subUrl = Helpers.StringUtils.GetSubString(data, @"<track kind=""captions"" src=""", @"""");
            var vidUrl = Helpers.StringUtils.GetSubString(data, @"<source src=""", @"""");
            var httpUrl = new HttpUrl(vidUrl);
            httpUrl.Referer = url;
            return httpUrl.ToString();
        }
    }

    public class FiftySix : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "56.com";
        }

        public override string GetVideoUrl(string url)
        {
            //Url=http://www.56.com/u90/v_MzYxNzA2MzE.html
            string id = Helpers.StringUtils.GetSubString(url, "/v_", ".html");
            //http://stat.56.com/stat/flv.php?id=MzYxNzA2MzE&pct=1&user_id=&norand=1&gJsonId=1&gJson=VideoTimes&gJsonData=n&gJsonDoStr=oFlv.up_times(oJson.VideoTimes.data)
            string tmpUrl = @"http://stat.56.com/stat/flv.php?id=" + id + @"&pct=1&user_id=&norand=1&gJsonId=1&gJson=VideoTimes&gJsonData=n&gJsonDoStr=oFlv.up_times(oJson.VideoTimes.data)";
            CookieContainer cc = new CookieContainer();
            string webData = WebCache.Instance.GetWebData(tmpUrl, cookies: cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://stat.56.com"));
            string id2 = null;
            foreach (Cookie cook in ccol)
                id2 = cook.Value.TrimEnd('-');
            //http://vxml.56.com/json/36170631/?src=site
            webData = WebCache.Instance.GetWebData(@"http://vxml.56.com/json/" + id2 + "/?src=site");
            string fileUrl = Helpers.StringUtils.GetSubString(webData, @"{""url"":""", @"""");
            return WebCache.Instance.GetRedirectedUrl(fileUrl);
        }
    }

    public class FileBox : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "filebox.com";
        }

        public override string GetVideoUrl(string url)
        {
            string data = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(data))
            {
                string op = Regex.Match(data, @"<input\stype=""hidden""\sname=""op""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;
                string id = Regex.Match(data, @"<input\stype=""hidden""\sname=""id""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;
                string rand = Regex.Match(data, @"<input\stype=""hidden""\sname=""rand""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;
                string referer = Regex.Match(data, @"<input\stype=""hidden""\sname=""referer""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;
                string method_free = Regex.Match(data, @"<input\stype=""hidden""\sname=""method_free""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;
                string method_premium = Regex.Match(data, @"<input\stype=""hidden""\sname=""method_premium""\svalue=""(?<value>[^""]+)""\s*>").Groups["value"].Value;

                string timeToWait = Regex.Match(data, @"<span\sid=""countdown_str"">[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
                if (Convert.ToInt32(timeToWait) < 10)
                {
                    string postdata = "op=" + op +
                                      "&id=" + id +
                                      "&rand=" + rand +
                                      "&referer=" + referer +
                                      "&method_free=" + method_free +
                                      "&method_premium=" + method_premium +
                                      "&down_direct=1";
                    //op=download2&id=benm0xrsl2s2&rand=6kuq6s4slrihwlq55ar5b7hukpvomewfy6i645i&referer=http%3A%2F%2Fwatchseries.eu%2Fopen%2Fcale%2F5764571%2Fidepisod%2F167374.html&method_free=&method_premium=&down_direct=1
                    System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);
                    data = WebCache.Instance.GetWebData(url, postdata);
                }

                Match n = Regex.Match(data, @"{url:\s*'(?<url>[^']*)',\sautoPlay");
                if (n.Success)
                    return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }

    public class FileNuke : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "filenuke.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            webData = GetFromPost(url, webData);
            Match m = Regex.Match(webData, @"var\slnk1\s=\s'(?<url>[^']*)'");
            if (m.Success)
            {
                HttpUrl httpUrl = new HttpUrl(m.Groups["url"].Value);
                httpUrl.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                return httpUrl.ToString();
            }
            return String.Empty;
        }
    }


    public class FrogMovz : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "frogmovz.com";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed"))
            {
                string page = WebCache.Instance.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"'file=(?<url>[^']+)'");
                    if (n.Success)
                    {
                        return n.Groups["url"].Value;
                    }
                }
            }
            else
            {
                string page = WebCache.Instance.GetWebData(url, cookies: cc);

                if (!string.IsNullOrEmpty(page))
                {

                    string op = Regex.Match(page, @"<input\stype=""hidden""\sname=""op""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;
                    string id = Regex.Match(page, @"<input\stype=""hidden""\sname=""id""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;
                    string rand = Regex.Match(page, @"<input\stype=""hidden""\sname=""rand""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;
                    string referer = Regex.Match(page, @"<input\stype=""hidden""\sname=""referer""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;
                    string method_free = Regex.Match(page, @"<input\stype=""hidden""\sname=""method_free""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;
                    string method_premium = Regex.Match(page, @"<input\stype=""hidden""\sname=""method_premium""\svalue=""(?<value>[^""]+)""\s/>").Groups["value"].Value;

                    string timeToWait = Regex.Match(page, @"<span\sid=""countdown_str"">[^>]*>[^>]*>[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
                    if (Convert.ToInt32(timeToWait) < 10)
                    {
                        string postdata = "op=" + op +
                                          "&id=" + id +
                                          "&rand=" + rand +
                                          "&referer=" + referer +
                                          "&method_free=" + method_free +
                                          "&method_premium=" + method_premium +
                                          "&down_direct=1";

                        System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);

                        string page2 = WebCache.Instance.GetWebData(url, postdata, cc, url);

                        if (!string.IsNullOrEmpty(page2))
                        {
                            string packed = null;
                            int i = page2.LastIndexOf(@"return p}");
                            if (i >= 0)
                            {
                                int j = page2.IndexOf(@"</script>", i);
                                if (j >= 0)
                                    packed = page2.Substring(i + 9, j - i - 9);
                            }
                            string resUrl;
                            if (!String.IsNullOrEmpty(packed))
                            {
                                packed = packed.Replace(@"\'", @"'");
                                string unpacked = Helpers.StringUtils.UnPack(packed);
                                string res = Helpers.StringUtils.GetSubString(unpacked, @"'file','", @"'");
                                if (!String.IsNullOrEmpty(res))
                                    resUrl = res;
                                else
                                    resUrl = Helpers.StringUtils.GetSubString(unpacked, @"name=""src""value=""", @"""");
                            }
                            else
                                resUrl = Helpers.StringUtils.GetSubString(page2, @"addVariable('file','", @"'");
                            resUrl = resUrl.Replace("[", "%5b").Replace("]", "%5d");
                            return resUrl;
                        }
                    }
                }
            }
            return String.Empty;
        }
    }

    public class Gomoplayer : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "gomoplayer.com";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            string packed = Helpers.StringUtils.GetSubString(data, @"return p}", @"</script>");
            if (!String.IsNullOrEmpty(packed))
            {
                packed = packed.Replace(@"\'", @"'");
                string unpacked = Helpers.StringUtils.UnPack(packed);
                var resUrl = Helpers.StringUtils.GetSubString(unpacked, @"file:""", @"""");
                if (resUrl.EndsWith(".m3u8"))
                {
                    var m3u8Data = GetWebData(resUrl);
                    var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(m3u8Data, resUrl);
                    var finalUrl = res.LastOrDefault().Value;
                    return finalUrl;
                }
                else
                    return resUrl;
            }
            return null;
        }
    }

    public class GoogleVideo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "video.google";
        }

        public override string GetVideoUrl(string url)
        {
            if (url.Contains("googleplayer.swf"))
                url = string.Format("http://video.google.de/videoplay?docid={0}", Helpers.StringUtils.GetSubString(url, @"docid=", @"&"));

            string webData = WebCache.Instance.GetWebData(url);
            string result = HttpUtility.UrlDecode(Helpers.StringUtils.GetSubString(webData, @"videoUrl\x3d", @"\x26"));
            if (!String.IsNullOrEmpty(result))
                return result;
            return HttpUtility.UrlDecode(Helpers.StringUtils.GetSubString(webData, @"videoUrl=", @"&amp;"));
        }
    }

    public class KarambaVidz : FrogMovz
    {
        public override string GetHosterUrl()
        {
            return "karambavidz.com";
        }
    }

    public class Mightyupload : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "mightyupload";
        }

        public override string GetVideoUrl(string url)
        {
            string data = WebCache.Instance.GetWebData(url);
            if (!string.IsNullOrEmpty(data))
            {
                var m2 = Regex.Match(data, @"return\sp(?<data>.*?)</script", DefaultRegexOptions);
                if (m2.Success)
                {
                    var res = Helpers.StringUtils.UnPack(m2.Groups["data"].Value.Replace(@"\'", @"'"));
                    m2 = Regex.Match(res, @"{file:""(?<url>[^""]*)"",flashplayer");
                }
                if (m2.Success)
                    return m2.Groups["url"].Value;
            }
            return url;
        }
    }

    public class Mixdrop : MyHosterBase
    {

        public override string GetHosterUrl()
        {
            return "mixdrop.co";
        }

        public override string GetVideoUrl(string url)
        {
            if (!url.Contains("/e/"))
            {
                var data2 = GetWebData(url);
                Match m2 = Regex.Match(data2, @"<iframe\swidth=""[^%]*%""\sheight=""[^""]*""\ssrc=""(?<url>[^""]*)""");
                if (m2.Success)
                {
                    var newUrl = m2.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                    {
                        Uri uri = null;
                        if (Uri.TryCreate(new Uri(url), newUrl, out uri))
                        {
                            newUrl = uri.ToString();
                        }
                        else
                        {
                            newUrl = string.Empty;
                        }
                    }
                    url = newUrl;
                }
                else
                {
                    m2 = Regex.Match(data2, @"<h2>(?<line1>[^<]*)</h2>\s*<p>(?<line2>[^<]*)</p>");
                    if (m2.Success)
                        throw new OnlineVideosException(m2.Groups["line1"].Value + "\n" + m2.Groups["line2"].Value);
                }
            }
            var data = GetWebData(url);
            string packed = Helpers.StringUtils.GetSubString(data, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);
            Match m = Regex.Match(unpacked, @"MDCore\.wurl=""(?<url>[^""]*)""");
            if (m.Success)
            {
                var finalUrl = m.Groups["url"].Value;
                if (!finalUrl.StartsWith("http"))
                    finalUrl = "https:" + finalUrl;
                return finalUrl;
            }
            return String.Empty;
        }
    }


    public class MovDivX : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "movdivx.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            webData = GetFromPost(url, webData);
            if (!string.IsNullOrEmpty(webData))
            {
                string packed = null;
                int i = webData.LastIndexOf(@"return p}");
                if (i >= 0)
                {
                    int j = webData.IndexOf(@"</script>", i);
                    if (j >= 0)
                        packed = webData.Substring(i + 9, j - i - 9);
                }
                if (!String.IsNullOrEmpty(packed))
                {
                    packed = packed.Replace(@"\'", @"'");
                    string unpacked = Helpers.StringUtils.UnPack(packed);
                    return Helpers.StringUtils.GetSubString(unpacked, @"name=""src""value=""", @"""");
                }
                return String.Empty;
            }

            Match m = Regex.Match(webData, @"var\slnk1\s=\s'(?<url>[^']*)'");
            if (m.Success)
            {
                HttpUrl httpUrl = new HttpUrl(m.Groups["url"].Value);
                httpUrl.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                return httpUrl.ToString();
            }
            return String.Empty;
        }
    }

    public class MySpace : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "myspace.com";
        }

        public override string GetVideoUrl(string url)
        {
            string videoId = Helpers.StringUtils.GetSubString(url, "videoid=", "&");
            string webData = WebCache.Instance.GetWebData(@"http://mediaservices.myspace.com/services/rss.ashx?videoID=" + videoId);
            string fileUrl = Helpers.StringUtils.GetSubString(webData, @"RTMPE url=""", @"""");

            //return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfhash=a51d59f968ffb279f0a3c0bf398f2118b2cc811f04d86c940fd211193dee2013&swfsize=770329",
            return fileUrl.Replace("rtmp:", "rtmpe:");
        }
    }

    public class OnlyStream : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "onlystream.tv";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            Match m = Regex.Match(data, @"{file:""(?<url>[^""]*mp4)""");
            if (m.Success)
                return m.Groups["url"].Value;

            return null;
        }
    }

    public class PlayerOmroep : HosterBase
    {

        private static readonly string[] sortedFormats = new string[] { "wmv", "mov", "wvc1" };
        private static readonly string[] sortedQualities = new string[] { "sb", "bb", "std" };

        public override string GetHosterUrl()
        {
            return "player.omroep.nl";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            if (!(url.ToLowerInvariant().Contains(@"ugslplayer.xap")))
                return base.GetPlaybackOptions(url);

            int aflID = Convert.ToInt32(url.Split('&')[0].Split('=')[1]);
            XmlDocument doc = new XmlDocument();
            string seqData = WebCache.Instance.GetWebData(@"http://pi.omroep.nl/info/security/");
            doc.LoadXml(seqData);
            string data = doc.SelectSingleNode("session/key").InnerText;
            byte[] tmp = Convert.FromBase64String(data);
            string[] secparts = Encoding.ASCII.GetString(tmp).Split('|');

            string prehash = aflID.ToString() + '|' + secparts[1];
            string hash = Helpers.EncryptionUtils.GetMD5Hash(prehash).ToUpperInvariant();
            string url2 = String.Format(@"http://pi.omroep.nl/info/stream/aflevering/{0}/{1}", aflID, hash);
            doc.Load(url2);

            List<KeyValuePair<KeyValuePair<string, string>, string>> playbackOptions =
                new List<KeyValuePair<KeyValuePair<string, string>, string>>();

            foreach (XmlNode node in doc.SelectNodes("streams/stream"))
            {
                string quality = node.Attributes["compressie_kwaliteit"].Value;
                string format = node.Attributes["compressie_formaat"].Value;
                string streamUrl = node.SelectSingleNode("streamurl").InnerText.Trim();
                if (format == "mov") streamUrl += ".mp4"; //so file will be played by internal player
                //else
                //  streamUrl = SiteUtilBase.ParseASX(streamUrl)[0];

                if (!String.IsNullOrEmpty(streamUrl) && Uri.IsWellFormedUriString(streamUrl, System.UriKind.Absolute))
                {
                    KeyValuePair<string, string> q = new KeyValuePair<string, string>(quality, format);
                    playbackOptions.Add(new KeyValuePair<KeyValuePair<string, string>, string>(q, streamUrl));
                }
            }
            playbackOptions.Sort(Compare);

            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (KeyValuePair<KeyValuePair<string, string>, string> kv in playbackOptions)
                res.Add(kv.Key.Value + ' ' + kv.Key.Key, kv.Value);
            return res;
        }

        public override string GetVideoUrl(string url)
        {
            int aflID = Convert.ToInt32(url.Split('&')[0].Split('=')[1]);

            CookieContainer cc = new CookieContainer();
            string step1 = WebCache.Instance.GetWebData(url, cookies: cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://tmp.player.omroep.nl/"));
            CookieContainer newcc = new CookieContainer();
            foreach (Cookie c in ccol) newcc.Add(c);

            step1 = WebCache.Instance.GetWebData("http://player.omroep.nl/js/initialization.js.php?aflID=" + aflID.ToString(), cookies: newcc);
            if (!String.IsNullOrEmpty(step1))
            {
                int p = step1.IndexOf("securityCode = '");
                if (p != -1)
                {
                    step1 = step1.Remove(0, p + 16);
                    string sec = step1.Split('\'')[0];
                    string step2 = WebCache.Instance.GetWebData("http://player.omroep.nl/xml/metaplayer.xml.php?aflID=" + aflID.ToString() + "&md5=" + sec, cookies: newcc);
                    if (!String.IsNullOrEmpty(step2))
                    {
                        XmlDocument tdoc = new XmlDocument();
                        tdoc.LoadXml(step2);
                        XmlNode final = tdoc.SelectSingleNode("/media_export_player/aflevering/streams/stream[@compressie_kwaliteit='bb' and @compressie_formaat='wmv']");
                        if (final != null)
                            return final.InnerText.Trim();

                    }
                }

            }
            return null;
        }

        public int Compare(KeyValuePair<KeyValuePair<string, string>, string> a, KeyValuePair<KeyValuePair<string, string>, string> b)
        {
            int res = Array.IndexOf(sortedQualities, a.Key.Key).CompareTo(Array.IndexOf(sortedQualities, b.Key.Key));
            if (res != 0)
                return res;
            return Array.IndexOf(sortedFormats, a.Key.Value).CompareTo(Array.IndexOf(sortedFormats, b.Key.Value));
        }
    }

    public class Playmyvid : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "playmyvid.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            url = Helpers.StringUtils.GetSubString(webData, @"flv=", @"&");
            return @"http://www.playmyvid.com/files/videos/" + url;
        }
    }
    public class SharedSx : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "shared.sx";
        }

        public override string GetVideoUrl(string url)
        {
            string page = WebCache.Instance.GetWebData(url);
            Match n = Regex.Match(page, @"<source\ssrc=""(?<url>[^""]*)""");
            if (!n.Success)
                n = Regex.Match(page, @"<div\sclass=""stream-content""\sdata-url=""(?<url>[^""]*)""");
            if (n.Success)
                return n.Groups["url"].Value;
            return String.Empty;
        }
    }

    public class ShareRepo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "sharerepo.com";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed") || url.Contains(@"/f/"))
            {
                string page = WebCache.Instance.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"file:\s'(?<url>[^']*)'");
                    if (n.Success)
                    {
                        HttpUrl httpurl = new HttpUrl(n.Groups["url"].Value);
                        httpurl.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                        return httpurl.ToString();
                    }
                }
            }
            else
            {
                string page = WebCache.Instance.GetWebData(url, cookies: cc);

                if (!string.IsNullOrEmpty(page))
                {

                    string op = Regex.Match(page, @"<input\stype=""hidden""\sname=""op""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string id = Regex.Match(page, @"<input\stype=""hidden""\sname=""id""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string rand = Regex.Match(page, @"<input\stype=""hidden""\sname=""rand""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string referer = Regex.Match(page, @"<input\stype=""hidden""\sname=""referer""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    string method_free = Regex.Match(page, @"<input\stype=""submit""\sname=""method_free""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;
                    //string method_premium = Regex.Match(page, @"<input\stype=""submit""\sname=""method_premium""\svalue=""(?<value>[^""]+)"">").Groups["value"].Value;

                    //string timeToWait = Regex.Match(page, @"<span\sid=""countdown_str"">[^>]*>[^>]*>[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
                    //if (Convert.ToInt32(timeToWait) < 10)
                    {
                        string postdata = "op=" + op +
                                          "&id=" + id +
                                          "&rand=" + rand +
                                          "&referer=" + referer +
                                          "&method_free=" + HttpUtility.UrlEncode(method_free) +
                                          //"&method_premium=" + method_premium +
                                          "&down_direct=1";

                        //System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);

                        string page2 = WebCache.Instance.GetWebData(url, postdata, cc, url);

                        if (!string.IsNullOrEmpty(page2))
                        {
                            string packed = null;
                            int i = page2.LastIndexOf(@"return p}");
                            if (i >= 0)
                            {
                                int j = page2.IndexOf(@"</script>", i);
                                if (j >= 0)
                                    packed = page2.Substring(i + 9, j - i - 9);
                            }
                            string resUrl;
                            if (!String.IsNullOrEmpty(packed))
                            {
                                packed = packed.Replace(@"\'", @"'");
                                string unpacked = Helpers.StringUtils.UnPack(packed);
                                string res = Helpers.StringUtils.GetSubString(unpacked, @"'file','", @"'");
                                if (!String.IsNullOrEmpty(res))
                                    resUrl = res;
                                else
                                    resUrl = Helpers.StringUtils.GetSubString(unpacked, @"name=""src""value=""", @"""");
                            }
                            else
                                resUrl = Helpers.StringUtils.GetSubString(page2, @"addVariable('file','", @"'");
                            resUrl = resUrl.Replace("[", "%5b").Replace("]", "%5d");
                            return resUrl;
                        }
                    }
                }
            }
            return String.Empty;
        }
    }

    public class Smotri : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "smotri.com";
        }

        public override string GetVideoUrl(string url)
        {
            string videoId = Helpers.StringUtils.GetSubString(url, "?id=", null);

            string webData = WebCache.Instance.GetWebData(url);
            string postData = Helpers.StringUtils.GetSubString(webData, @"so.addVariable('context',", @""");");
            postData = Helpers.StringUtils.GetSubString(postData, @"""", null);
            postData = postData.Replace("_", "%5F");
            postData = postData.Replace(".", "%2E");
            postData = @"p%5Fid%5B1%5D=4&begun=1&video%5Furl=1&p%5Fid%5B0%5D=2&context=" +
                postData + @"&devid=LoadupFlashPlayer&ticket=" + videoId;

            webData = WebCache.Instance.GetWebData(@"http://smotri.com/video/view/url/bot/", postData);
            //"{\"_is_loadup\":0,\"_vidURL\":\"http:\\/\\/file38.loadup.ru\\/4412949d467b8db09bd07eedc7127f57\\/4bd0b05a\\/9a\\/a1\\/c1ad0ea5c0e8268898d3449b9087.flv\",\"_imgURL\":\"http:\\/\\/frame2.loadup.ru\\/9a\\/a1\\/1191805.3.3.jpg\",\"botator_banner\":{\"4\":[{\"cnt_tot_max\":1120377,\"cnt_hour_max\":4500,\"clc_tot_max\":0,\"clc_hour_max\":0,\"cnt_uniq_day_max\":3,\"cnt_uniq_week_max\":0,\"cnt_uniq_month_max\":0,\"link_transitions\":\"http:\\/\\/smotri.com\\/botator\\/clickator\\/click\\/?sid=qm2fzb5ruwdcj1ig_12\",\"zero_pixel\":\"http:\\/\\/ad.adriver.ru\\/cgi-bin\\/rle.cgi?sid=1&bt=21&ad=226889&pid=440944&bid=817095&bn=817095&rnd=1702217828\",\"signature\":{\"set_sign\":\"top\",\"signature\":\"\",\"signature_color\":null},\"link\":\"http:\\/\\/pics.loadup.ru\\/content\\/smotri.com_400x300_reenc_2.flv\",\"link_show\":\"http:\\/\\/smotri.com\\/botator\\/logator\\/show\\/?sid=qm2fzb5ruwdcj1ig_12\",\"banner_type\":\"video_flv\",\"b_id\":12}]},\"trustKey\":\"79e566c96057ce2b6f6055a3fa34f744\",\"video_id\":\"v119180501e5\",\"_pass_protected\":0,\"begun_url_1\":\"http:\\/\\/flash.begun.ru\\/banner.jsp?pad_id=100582787&offset=0&limit=5&encoding=utf8&charset=utf8&keywords=\"}"
            return Helpers.StringUtils.GetSubString(webData, @"_vidURL"":""", @"""").Replace(@"\/", "/");
        }
    }

    public class Stagevu : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "stagevu.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            url = Helpers.StringUtils.GetSubString(webData, @"url[", @"';");
            return Helpers.StringUtils.GetSubString(url, @"'", @"'");
        }
    }

    public class Streamango : MyHosterBase
    {

        const string key = "=/+9876543210zyxwvutsrqponmlkjihgfedcbaZYXWVUTSRQPONMLKJIHGFEDCBA";

        public override string GetHosterUrl()
        {
            return "streamango.com";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            Match m = Regex.Match(data, @"type:""video/mp4"",src:d\('(?<encrypted>[^']*)',(?<mask>\d+)\)");
            if (m.Success)
            {
                var enc = m.Groups["encrypted"].Value;
                int mask = int.Parse(m.Groups["mask"].Value);
                enc = Regex.Replace(enc, @"[^A-Za-z0-9\+\/\=]", "");
                int idx = 0;
                StringBuilder res = new StringBuilder();
                res.Append("http:");
                while (idx < enc.Length)
                {
                    int a = key.IndexOf(enc.Substring(idx++, 1));
                    int b = key.IndexOf(enc.Substring(idx++, 1));
                    int c = key.IndexOf(enc.Substring(idx++, 1));
                    int d = key.IndexOf(enc.Substring(idx++, 1));
                    int s1 = ((a << 2) | (b >> 4)) ^ mask;
                    res.Append((char)s1);
                    int s2 = ((b & 0xf) << 0x4) | (c >> 0x2);
                    if (c != 0x40)
                    {
                        res.Append((char)s2);
                    }
                    int s3 = ((c & 0x3) << 0x6) | d;
                    if (d != 0x40)
                    {
                        res.Append((char)s3);
                    }
                }
                return res.ToString();

            }
            Match nofile = Regex.Match(data, @"<p class=""lead"">(?<message>[^>]*)</p>");
            TestForError(nofile);

            return null;
        }
    }

    public class StreamCloud : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "streamcloud.eu";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);

            string timeToWait = Regex.Match(webData, @"var\s*count\s*=\s*(?<time>[^;]+);").Groups["time"].Value;
            if (Convert.ToInt32(timeToWait) <= 10)
                System.Threading.Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);

            webData = GetFromPost(url, webData);
            Match m = Regex.Match(webData, @"file:\s*""(?<url>[^""]*)""");
            if (m.Success)
                return m.Groups["url"].Value;
            return String.Empty;
        }
    }

    public class StreamIn : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "streamin.to";
        }

        public override string GetVideoUrl(string url)
        {
            string webdata = WebCache.Instance.GetWebData(url);
            string timeToWait = Regex.Match(webdata, @"<span\sid=""countdown_str"">[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
            if (String.IsNullOrEmpty(timeToWait) && webdata.Length < 50)
                throw new OnlineVideosException(webdata);
            int time;
            if (Int32.TryParse(timeToWait, out time) && time < 10)
                System.Threading.Thread.Sleep(time * 1001);
            webdata = GetFromPost(url, webdata, false, null, new[] { "imhuman=+" });

            string file = Helpers.StringUtils.GetSubString(webdata, @"file: """, @"""");
            string streamer = Helpers.StringUtils.GetSubString(webdata, @"streamer: """, @"""");
            if (String.IsNullOrEmpty(streamer))
            {
                string packed = Helpers.StringUtils.GetSubString(webdata, @"return p}", @"</script>");
                packed = packed.Replace(@"\'", @"'");
                string unpacked = Helpers.StringUtils.UnPack(packed);

                Match m = Regex.Match(unpacked, @"file:\s*""(?<url>[^""]*)""");
                if (m.Success)
                    return m.Groups["url"].Value;

            }
            Match nofile = Regex.Match(webdata, @"<title>File\sRemoved</title>\s*<b>(?<message>[^<]*)</b>");
            TestForError(nofile, streamer);

            RtmpUrl rtmpUrl = new RtmpUrl(streamer)
            {
                PlayPath = file
            };
            return rtmpUrl.ToString();

        }
    }

    public class Streamzz : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "streamzz.to";
        }

        public string MyGetRedirectedUrl(string url, string referer, CookieContainer cc, NameValueCollection headers)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return url;
                foreach (var headerName in headers.AllKeys)
                {
                    switch (headerName.ToLowerInvariant())
                    {
                        case "accept":
                            request.Accept = headers[headerName];
                            break;
                        case "user-agent":
                            request.UserAgent = headers[headerName];
                            break;
                        case "referer":
                            request.Referer = headers[headerName];
                            break;
                        default:
                            request.Headers.Set(headerName, headers[headerName]);
                            break;
                    }
                }

                request.AllowAutoRedirect = true;
                request.CookieContainer = cc;
                request.Timeout = 15000;
                if (!string.IsNullOrEmpty(referer)) request.Referer = referer;
                var result = request.BeginGetResponse((ar) => request.Abort(), null);
                while (!result.IsCompleted) Thread.Sleep(10);
                httpWebresponse = request.EndGetResponse(result) as HttpWebResponse;
                if (httpWebresponse == null) return url;
                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                    return url;
                else
                    return httpWebresponse.ResponseUri.OriginalString;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            finally
            {
                if (httpWebresponse != null)
                {
                    try
                    {
                        httpWebresponse.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());
                    }
                }
            }
            return url;
        }

        public override string GetVideoUrl(string url)
        {
            const string uagent = @"Mozilla/5.0 (Windows NT 6.1; rv:90.0) Gecko/20100101 Firefox/90.0";
            CookieContainer cc = new CookieContainer();
            NameValueCollection headers = new NameValueCollection();
            headers.Set("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,* /*;q=0.8");
            headers.Set("Accept-Language", "en-US,en;q=0.5");
            headers.Set("Accept-Encoding", "gzip, deflate, br");
            var newUrl = MyGetRedirectedUrl(url, null, cc, headers);

            var data = GetWebData(newUrl, cookies: cc, userAgent: uagent);
            headers.Set("Accept", "*/*");

            Match m = Regex.Match(data, @"history\.pushState\(stateObj,\s*""[^""]*"",\s""(?<url>[^""]*)""\);");
            string referer = null;
            if (m.Success)
            {
                referer = m.Groups["url"].Value;
                if (!Uri.IsWellFormedUriString(referer, UriKind.Absolute))
                {
                    Uri uri = null;
                    if (Uri.TryCreate(new Uri(url), referer, out uri))
                        referer = uri.ToString();
                }
            }

            m = Regex.Match(data, @"<script\stype='text/javascript'\ssrc='[^']*?(?<ppu_main>[^\./']*)\.js'></script>");
            cc.Add(new Cookie("ppu_main_" + m.Groups["ppu_main"].Value, "1", "", @".streamzz.to"));
            GetWebData(@"https://cnt.streamzz.to/count.php?xyz=1", referer: referer, cookies: cc, userAgent: uagent, headers: headers);

            m = Regex.Match(data, @"return\sp(?<pack>[^<]*)</script");
            var l = new List<String>();
            string url1 = null;
            while (m.Success && url1 == null)
            {
                var unpacked = Helpers.StringUtils.UnPack(m.Groups["pack"].Value);
                if (unpacked != null)
                {
                    Match m2 = Regex.Match(unpacked, @"src:\\'(?<p1>https://[^/]+)/getl1nk'\.split");
                    if (m2.Success)
                        url1 = m2.Groups["p1"].Value;
                }
                m = m.NextMatch();
            }
            m = Regex.Match(newUrl, @"/x(?<code>.*)");
            if (m.Success)
            {
                url1 = url1 + "/getlink-" + m.Groups["code"].Value + ".dll";
                var finalUrl = WebCache.Instance.GetRedirectedUrl(url1, @"https://streamzz.to/");
                return finalUrl;
            }
            return null;
        }
    }
    public class TheFile : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "thefile.me";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);

            webData = GetFromPost(url, webData, false, new[] { "method_free=Free+Download" }, new[] { "op=login" });
            webData = Helpers.StringUtils.GetSubString(webData, @"id=""player_code""", "</html>");
            string packed = Helpers.StringUtils.GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);

            Match m = Regex.Match(unpacked, @"file:\s*""(?<url>[^""]*)""");
            if (m.Success)
                return m.Groups["url"].Value;
            return String.Empty;
        }
    }

    public class TheVideo : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "thevideo.me";
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            using (var certignorer = new CertificateIgnorer())
            {
                url = WebCache.Instance.GetRedirectedUrl(url);
            }

            url = url.Replace(@"https://vev.io/", @"https://vev.io/api/serve/video/");
            var jsonData = GetWebData<JToken>(url, "{}");
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (JProperty q in jsonData["qualities"])
            {
                result.Add(q.Name, q.Value.ToString());
            }
            return result;
        }
    }

    public class Tudou : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "tudou.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            string iid = Helpers.StringUtils.GetSubString(webData, @"var iid = ", "\n");
            //url = @"http://v2.tudou.com/v?it=" + iid;
            url = @"http://v2.tudou.com/v?vn=02&ui=0&refurl=" + HttpUtility.UrlEncode(url) + @"&it=" + iid + @"&pw=&noCache=13678&st=1%2C2&si=sp";
            //http://v2.tudou.com/v?vn=02&ui=0&refurl=http%3A%2F%2Fwww%2Etudou%2Ecom%2Fprograms%2Fview%2FXQ1dE6XJWnU&it=20391047&pw=&noCache=13678&st=1%2C2&si=sp
            XmlDocument doc = new XmlDocument();
            webData = WebCache.Instance.GetWebData(url);
            doc.LoadXml(webData);
            XmlNodeList nodes = doc.SelectNodes("//v/f");
            string largest = null;
            // not quite sure which of the nodes gives a valid result, but the one that does, gives a time-out in mediaportal
            foreach (XmlNode node in nodes)
                if (largest == null)// || String.Compare(largest, node.InnerText) == -1)
                    largest = node.InnerText;

            return largest;
        }
    }

    public class TwoGBHosting : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "2gb-hosting.com";
        }

        public override string GetVideoUrl(string url)
        {
            string postData = String.Empty;
            string webData = WebCache.Instance.GetWebData(url);
            string post = Helpers.StringUtils.GetSubString(webData, @"<form>", @"</form>");
            Match m = Regex.Match(webData, @"<input\stype=""[^""]*""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
            while (m.Success)
            {
                if (!String.IsNullOrEmpty(postData))
                    postData += "&";
                postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                m = m.NextMatch();
            }
            webData = WebCache.Instance.GetWebData(url, postData);
            string res = Helpers.StringUtils.GetSubString(webData, @"embed", @">");
            res = Helpers.StringUtils.GetSubString(res, @"src=""", @"""");
            return res;
        }
    }

    public class Ufliq : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "ufliq.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            string postData = String.Empty;
            Match m = Regex.Match(webData, @"<input\stype=""hidden""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
            while (m.Success)
            {
                if (!String.IsNullOrEmpty(postData))
                    postData += "&";
                postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                m = m.NextMatch();
            }
            if (String.IsNullOrEmpty(postData))
                return null;

            Thread.Sleep(5000);

            webData = WebCache.Instance.GetWebData(url, postData);
            string packed = Helpers.StringUtils.GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);
            return Helpers.StringUtils.GetSubString(unpacked, @"'file','", @"'");
        }
    }


    public class Upstream : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "upstream.to";
        }

        public override string GetVideoUrl(string url)
        {
            string data = GetWebData(url);
            string packed = Helpers.StringUtils.GetSubString(data, @"return p}", @"</script>");
            if (!String.IsNullOrEmpty(packed))
            {
                packed = packed.Replace(@"\'", @"'");
                string unpacked = Helpers.StringUtils.UnPack(packed);
                var resUrl = Helpers.StringUtils.GetSubString(unpacked, @"file:""", @"""");
                if (resUrl.EndsWith(".m3u8"))
                {
                    var m3u8Data = GetWebData(resUrl, referer: "https://upstream.to/");
                    var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(m3u8Data, resUrl);
                    var finalUrl = res.LastOrDefault().Value;
                    HttpUrl ress = new HttpUrl(finalUrl);
                    ress.Referer = "https://upstream.to/";
                    return ress.ToString();
                }
                else
                    return resUrl;
            }
            return null;
        }
    }

    public class Veehd : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "veehd.com";
        }

        public override string GetVideoUrl(string url)
        {
            string webData = WebCache.Instance.GetWebData(url);
            Match nofile = Regex.Match(webData, @"<b[^>]*>>(?<message>[^<]*)</b>");
            string tmp = Helpers.StringUtils.GetSubString(webData, @"$(""#playeriframe"").attr({src : """, @"""");
            webData = WebCache.Instance.GetWebData(@"http://veehd.com" + tmp);
            string res = HttpUtility.UrlDecode(Helpers.StringUtils.GetSubString(webData, @"""url"":""", @""""));
            TestForError(nofile, res);
            return res;
        }
    }

    public class VeryStream : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "verystream.com";
        }
        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            var match = Regex.Match(data, @"<p\sstyle=""""\s*class=""""\s*id=""videolink"">(?<url>[^<]*)<");
            if (match.Success)
                return new Uri(new Uri(url), match.Groups["url"].Value).AbsoluteUri.Replace("/e/", "/gettoken/");
            match = Regex.Match(data, @"<h3>(?<message>[^<]*)<");
            TestForError(match);
            return null;
        }
    }

    public class Vidbull : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidbull.com";
        }

        public override string GetVideoUrl(string url)
        {
            Match m = Regex.Match(url, @"vidbull.com/(?<id>[^\.]*)");
            if (m.Success)
            {
                string id = m.Groups["id"].Value;

                if (!id.Contains("-"))
                    id = "embed-" + id + "-640x360.html";
                if (!id.EndsWith(".html"))
                    id = id + ".html";
                url = @"http://vidbull.com/" + id;
            };

            string webData = WebCache.Instance.GetWebData(url);
            string sub = Helpers.StringUtils.GetSubString(webData, "id='flvplayer'", null);
            string packed = Helpers.StringUtils.GetSubString(sub, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);
            string res = Helpers.StringUtils.GetSubString(unpacked, @"{file:""", @"""");
            if (res.StartsWith(@"http://"))
                return res;

            RijndaelManaged rijndael = new RijndaelManaged();
            rijndael.Key = HexToBytes("a949376e37b369f17bc7d3c7a04c5721");
            rijndael.Mode = CipherMode.ECB;
            rijndael.Padding = PaddingMode.Zeros;
            ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            using (MemoryStream msDecrypt = new MemoryStream(HexToBytes(res)))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        res = srDecrypt.ReadToEnd();
                        int p = res.IndexOf('\0');
                        if (p >= 0)
                            return res.Substring(0, p - 1);
                        m = Regex.Match(webData, @"<span[^>]*>(?<message>[^<]*)</span>");
                        TestForError(m);
                        return String.Empty;
                    }
                }
            }
        }

        private byte[] HexToBytes(string hexString)
        {
            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return HexAsBytes;
        }

    }

    public class Vidoza : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidoza.net";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            var m = Regex.Match(data, @"{\ssrc:\s""(?<url>[^""]*)"",\stype:\s""video/mp4""");
            if (m.Success)
                return m.Groups["url"].Value;
            Match nofile = Regex.Match(data, @"<h3 class=""text-center"">(?<message>[^<]*)</h3>");
            TestForError(nofile);
            return null;
        }
    }

    public class VidTo : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidto.me";
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.First().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            string webData = WebCache.Instance.GetWebData(url);

            string timeToWait = Regex.Match(webData, @"<span\sid=""countdown_str"">[^>]*>(?<time>[^<]+)</span>").Groups["time"].Value;
            if (!String.IsNullOrEmpty(timeToWait) && Convert.ToInt32(timeToWait) < 10)
                Thread.Sleep(Convert.ToInt32(timeToWait) * 1001);

            webData = GetFromPost(url, webData);

            var m = Regex.Match(webData, @"{file:""(?<m0>[^""]*)"",label:""(?<n0>[^""]*)""}");
            while (m.Success)
            {
                res.Add(m.Groups["n0"].Value, m.Groups["m0"].Value);
                m = m.NextMatch();
            }
            return res;
        }
    }

    public class VidLox : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidlox.tv";
        }

        public override string GetVideoUrl(string url)
        {
            string s;
            using (var certIgnorer = new CertificateIgnorer())
            {
                s = WebCache.Instance.GetWebData(url);
            }
            Match nofile = Regex.Match(s, @"<p class=""text-center"">(?<message>[^<]*)</p>");
            s = Helpers.StringUtils.GetSubString(s, "sources: [", "]");
            var m = Regex.Match(s, @"""(?<url>[^""]*)""");
            string theUrl = null;
            if (!m.Success)
                TestForError(nofile);
            while (m.Success)
            {
                theUrl = m.Groups["url"].Value;
                if (theUrl.EndsWith("mp4"))
                    return theUrl;
                m = m.NextMatch();
            }
            return theUrl;
        }
    }

    public class VidLoxMe : VidLox
    {
        public override string GetHosterUrl()
        {
            return "vidlox.me";
        }

        public override string GetVideoUrl(string url)
        {
            string s = WebCache.Instance.GetWebData(url);
            s = Helpers.StringUtils.GetSubString(s, "sources: [", "]");
            var m = Regex.Match(s, @"""(?<url>[^""]*)""");
            string theUrl = null;
            while (m.Success)
            {
                theUrl = m.Groups["url"].Value;
                if (theUrl.EndsWith("mp4"))
                    return theUrl;
                m = m.NextMatch();
            }
            return theUrl;
        }
    }

    public class VidNode : HosterBase, ISubtitle
    {
        private string subUrl;

        public override string GetHosterUrl()
        {
            return "vidnode.net";
        }

        public override string GetVideoUrl(string url)
        {
            Match m = Regex.Match(url, @"&amp;sub=(?<subid>[^&]*)&");
            if (m.Success)
                subUrl = @"https://vidnode.net/player/sub/index.php?id=" + m.Groups["subid"].Value;
            else
                subUrl = null;

            url = url.Replace(@"http://", @"https://");
            var data = GetWebData(url, referer: "http://google.com");
            m = Regex.Match(data, @"sources:\s*\[{file:\s*['""](?<url>[^'""]*)['""]\s*,\s*label");
            if (m.Success)
            {
                return m.Groups["url"].Value;
            }
            return null;
        }

        string ISubtitle.SubtitleText
        {
            get
            {
                if (!String.IsNullOrEmpty(subUrl))
                {
                    var data = GetWebData(subUrl);
                    if (data.StartsWith(@"WEBVTT"))
                        data = Helpers.SubtitleUtils.Webvtt2SRT(data);

                    return data;
                }
                else
                    return null;
            }
        }
    }

    public class VidTodo : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vidtodo.com";
        }

        public override string GetVideoUrl(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.AllowAutoRedirect = false;
            request.Timeout = 15000;
            request.Referer = url;
            request.Method = "HEAD";
            HttpWebResponse httpWebresponse = (HttpWebResponse)request.GetResponse();
            string newUrl = httpWebresponse.Headers.Get("Location");

            var data = GetWebData(newUrl, referer: url);
            string packed = Helpers.StringUtils.GetSubString(data, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = Helpers.StringUtils.UnPack(packed);
            string res = Helpers.StringUtils.GetSubString(unpacked, @"file:""", @"""");
            if (!String.IsNullOrEmpty(res))
                return res;
            res = Helpers.StringUtils.GetSubString(unpacked, @"src:""", @"""");
            if (!String.IsNullOrEmpty(res))
                return res;
            Match noFile = Regex.Match(data, @"<div\sid=""container"">\s*<b>(?<message>[^<]*)<", DefaultRegexOptions);
            TestForError(noFile);
            return null;
        }
    }

    public class Vidzi : HosterBase  //copied from ministerk, there it's called VidziTv and therefore could not be found by a regular gethoster call
    {
        public override string GetHosterUrl()
        {
            return "vidzi.tv";
        }

        public override string GetVideoUrl(string url)
        {
            if (!url.Contains("embed-"))
            {
                url = url.Replace("vidzi.tv/", "vidzi.tv/embed-");
            }
            if (!url.EndsWith(".html"))
            {
                url += ".html";
            }
            string data = GetWebData<string>(url);
            if (data.Contains("File was deleted or expired."))
                throw new OnlineVideosException("File was deleted or expired.");
            string packed = Helpers.StringUtils.GetSubString(data, @"return p}", @"</script>");
            if (!String.IsNullOrEmpty(packed))
            {
                string unpacked = Helpers.StringUtils.UnPack(packed.Replace(@"\'", @"'"));
                var rgx = new Regex(@"file:""(?<url>[^""]*?.mp4[^""]*)");
                var m = rgx.Match(unpacked);
                if (m.Success)
                {
                    return m.Groups["url"].Value;
                }
            }
            return "";
        }
    }

    public class Voe : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "voe.sx";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            Match m = Regex.Match(data, @"""hls"":\s""(?<url>[^""]*)""");
            if (m.Success)
            {
                var data2 = GetWebData(m.Groups["url"].Value);
                var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(data2, m.Groups["url"].Value);
                return res.FirstOrDefault().Value;
            }
            return null;
        }
    }

    public class Vshare : MyHosterBase
    {
        public override string GetHosterUrl()
        {
            return "vshare.eu";
        }

        public override string GetVideoUrl(string url)
        {
            url = url.Replace(@"http://", @"https://");
            string data = WebCache.Instance.GetWebData(url);
            var m = Regex.Match(data, @"\$\('\#play'\)\.attr\('disabled',\strue\)\s*\.attr\('value',\s'(?<time>\d+)'\)");
            if (m.Success)
                Thread.Sleep(Convert.ToInt32(m.Groups["time"].Value) * 1001);
            Match noFile = Regex.Match(data, @"<h1\sclass=""alt\slightbg\slh42"">(?<message>[^<]+)</h1>");

            data = GetFromPost(url, data);

            Match n = Regex.Match(data, @"<source\ssrc=""(?<url>[^""]*)""\stype=""video/mp4"">");
            if (n.Success)
                return n.Groups["url"].Value;
            TestForError(noFile);
            return null;
        }
    }


    public class Vureel : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "vureel.com";
        }

        public override string GetVideoUrl(string url)
        {
            string s = WebCache.Instance.GetWebData(url);
            return Helpers.StringUtils.GetSubString(s, @"Referral: ", " ");
        }
    }

    public class Wisevid : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "wisevid.com";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = WebCache.Instance.GetWebData(url, cookies: cc, referer: @"http://www.wisevid.com/");

            CookieCollection ccol = cc.GetCookies(new Uri("http://www.wisevid.com/"));
            CookieContainer newcc = new CookieContainer();
            foreach (Cookie c in ccol) newcc.Add(c);
            // (with age confirm)
            url = @"http://www.wisevid.com/play?v=" + Helpers.StringUtils.GetSubString(webData,
                @"play?v=", @"'");
            //string tmp2 = SiteUtilBase.GetWebDataFromPost(url, "a=1");
            string tmp2 = WebCache.Instance.GetWebData(url, cookies: newcc);
            url = Helpers.StringUtils.GetSubString(tmp2, "getF('", "'");
            byte[] tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }
    }

    public class Xtshare : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "xtshare.com";
        }

        public override string GetVideoUrl(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = WebCache.Instance.GetWebData(url, cookies: cc);
            if (url.Contains("humancheck.php"))
            {

                CookieCollection ccol = cc.GetCookies(new Uri("http://xtshare.com/humancheck.php"));
                CookieContainer newcc = new CookieContainer();
                foreach (Cookie c in ccol)
                {
                    c.Path = String.Empty;
                    newcc.Add(c);
                }
                url = url.Replace("humancheck", "toshare");
                webData = WebCache.Instance.GetWebData(url, "submit=I+am+human+now+let+me+watch+this+video", newcc);
            }
            string file = Helpers.StringUtils.GetSubString(webData, "'file','", "'");
            string streamer = Helpers.StringUtils.GetSubString(webData, "'streamer','", "'");
            // not tested, couldn't find a video hosted by xtshare
            RtmpUrl rtmpUrl = new RtmpUrl(streamer + file)
            {
                SwfVerify = true,
                SwfUrl = @"http://xtshare.com/player.swf",
                PageUrl = url,
                TcUrl = streamer
            };
            return rtmpUrl.ToString();
        }
    }

    public class Wolfstream : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "wolfstream.tv";
        }

        public override string GetVideoUrl(string url)
        {
            var data = GetWebData(url);
            Match m = Regex.Match(data, @"{file:""(?<url>[^""]*)""");
            if (m.Success)
            {
                var data2 = GetWebData(m.Groups["url"].Value);
                var res = Helpers.HlsPlaylistParser.GetPlaybackOptions(data2, m.Groups["url"].Value);
                return res.FirstOrDefault().Value;
            }
            return null;
        }
    }

}
