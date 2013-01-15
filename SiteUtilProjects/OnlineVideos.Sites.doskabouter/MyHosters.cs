using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Net;
using System.Web;
using System.Threading;
using System.Xml;
using System.Linq;
using Newtonsoft.Json;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Hoster
{
    public class BlipTv : HosterBase
    {
        public override string getHosterUrl()
        {
            return "blip.tv";
        }

        public override string getVideoUrls(string url)
        {
            var result = getPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            string s = HttpUtility.UrlDecode(SiteUtilBase.GetRedirectedUrl(url));
            int p = s.IndexOf("file=");
            if (p > -1)
            {
                int q = s.IndexOf('&', p);
                if (q < 0) q = s.Length;
                s = s.Substring(p + 5, q - p - 5);
                string rss = SiteUtilBase.GetWebData(s);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(rss);
                var urlAttribute = xmlDoc.SelectSingleNode("//enclosure/@url");
                if (urlAttribute != null) res.Add("video", urlAttribute.Value);
            }
            else
            {

                string webData = SiteUtilBase.GetWebData(url);

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
        public override string getHosterUrl()
        {
            return "cinshare.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string tmp = GetSubString(webData, @"<iframe src=""", @"""");
            webData = SiteUtilBase.GetWebData(tmp);
            tmp = GetSubString(webData, @"<param name=""src"" value=""", @"""");
            return SiteUtilBase.GetRedirectedUrl(tmp);
        }
    }

    public class DailyMotion : HosterBase
    {
        public override string getHosterUrl()
        {
            return "dailymotion.com";
        }

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            CookieContainer cc = new CookieContainer();
            Cookie c = new Cookie();
            c.Name = "family_filter";
            c.Value = "off";
            c.Expires = DateTime.Now.AddHours(1);
            c.Domain = new Uri(url).Host;
            cc.Add(c);

            string webData = SiteUtilBase.GetWebData(url.Replace(@"/swf/", @"/video/"), cc);

            Dictionary<string, string> res = new Dictionary<string, string>();
            Match matchFileUrl = Regex.Match(webData, @"""stream_(?<n0>[^_]*(?:_[^_]*)?)_url"":\s*""(?<m0>[^""]*)""");
            while (matchFileUrl.Success)
            {
                string foundUrl = matchFileUrl.Groups["m0"].Value;
                foundUrl = foundUrl.Replace(@"\/", @"/");
                res.Add(matchFileUrl.Groups["n0"].Value, foundUrl);
                matchFileUrl = matchFileUrl.NextMatch();
            }
            return res;
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string resUrl = GetSubString(webData, @"""video"", """, @"""");
            if (String.IsNullOrEmpty(resUrl))
                resUrl = GetSubString(webData, @"""stream_url"":""", @"""");
            if (String.IsNullOrEmpty(resUrl) && !url.Contains("embed"))
            {
                string newUrl = url.Replace(@".com/", @".com/embed/");
                if (!newUrl.Equals(url)) //safety check to prevent infinite recursion
                    resUrl = getVideoUrls(newUrl);
            }
            return resUrl.Replace(@"\/", @"/");
        }
    }


    public class FiftySix : HosterBase
    {
        public override string getHosterUrl()
        {
            return "56.com";
        }

        public override string getVideoUrls(string url)
        {
            //Url=http://www.56.com/u90/v_MzYxNzA2MzE.html
            string id = GetSubString(url, "/v_", ".html");
            //http://stat.56.com/stat/flv.php?id=MzYxNzA2MzE&pct=1&user_id=&norand=1&gJsonId=1&gJson=VideoTimes&gJsonData=n&gJsonDoStr=oFlv.up_times(oJson.VideoTimes.data)
            string tmpUrl = @"http://stat.56.com/stat/flv.php?id=" + id + @"&pct=1&user_id=&norand=1&gJsonId=1&gJson=VideoTimes&gJsonData=n&gJsonDoStr=oFlv.up_times(oJson.VideoTimes.data)";
            CookieContainer cc = new CookieContainer();
            string webData = SiteUtilBase.GetWebData(tmpUrl, cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://stat.56.com"));
            string id2 = null;
            foreach (Cookie cook in ccol)
                id2 = cook.Value.TrimEnd('-');
            //http://vxml.56.com/json/36170631/?src=site
            webData = SiteUtilBase.GetWebData(@"http://vxml.56.com/json/" + id2 + "/?src=site");
            string fileUrl = GetSubString(webData, @"{""url"":""", @"""");
            return SiteUtilBase.GetRedirectedUrl(fileUrl);
        }
    }

    public class FileBox : HosterBase
    {
        public override string getHosterUrl()
        {
            return "filebox.com";
        }

        public override string getVideoUrls(string url)
        {
            string data = SiteUtilBase.GetWebData(url);
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
                    data = SiteUtilBase.GetWebDataFromPost(url, postdata);
                }

                Match n = Regex.Match(data, @"{url:\s*'(?<url>[^']*)',\sautoPlay");
                if (n.Success)
                    return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }

    public class FrogMovz : HosterBase
    {
        public override string getHosterUrl()
        {
            return "frogmovz.com";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed"))
            {
                string page = SiteUtilBase.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"'file=(?<url>[^']+)'");
                    if (n.Success)
                    {
                        videoType = VideoType.flv;
                        return n.Groups["url"].Value;
                    }
                }
            }
            else
            {
                string page = SiteUtilBase.GetWebData(url, cc);

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

                        string page2 = SiteUtilBase.GetWebDataFromPost(url, postdata, cc, url);

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
                                string unpacked = UnPack(packed);
                                videoType = VideoType.divx;
                                string res = GetSubString(unpacked, @"'file','", @"'");
                                if (!String.IsNullOrEmpty(res))
                                    resUrl = res;
                                else
                                    resUrl = GetSubString(unpacked, @"name=""src""value=""", @"""");
                            }
                            else
                                resUrl = GetSubString(page2, @"addVariable('file','", @"'");
                            resUrl = resUrl.Replace("[", "%5b").Replace("]", "%5d");
                            return resUrl;
                        }
                    }
                }
            }
            return String.Empty;
        }
    }

    public class GoogleVideo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "video.google";
        }

        public override string getVideoUrls(string url)
        {
            if (url.Contains("googleplayer.swf"))
                url = string.Format("http://video.google.de/videoplay?docid={0}", GetSubString(url, @"docid=", @"&"));

            string webData = SiteUtilBase.GetWebData(url);
            string result = HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl\x3d", @"\x26"));
            if (!String.IsNullOrEmpty(result))
                return result;
            return HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl=", @"&amp;"));
        }
    }

    public class KarambaVidz : FrogMovz
    {
        public override string getHosterUrl()
        {
            return "karambavidz.com";
        }
    }

    public class MySpace : HosterBase
    {
        public override string getHosterUrl()
        {
            return "myspace.com";
        }

        public override string getVideoUrls(string url)
        {
            string videoId = GetSubString(url, "videoid=", "&");
            string webData = SiteUtilBase.GetWebData(@"http://mediaservices.myspace.com/services/rss.ashx?videoID=" + videoId);
            string fileUrl = GetSubString(webData, @"RTMPE url=""", @"""");

            //return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfhash=a51d59f968ffb279f0a3c0bf398f2118b2cc811f04d86c940fd211193dee2013&swfsize=770329",
            return fileUrl.Replace("rtmp:", "rtmpe:");
        }
    }

    public class PlayerOmroep : HosterBase
    {

        private static readonly string[] sortedFormats = new string[] { "wmv", "mov", "wvc1" };
        private static readonly string[] sortedQualities = new string[] { "sb", "bb", "std" };

        public override string getHosterUrl()
        {
            return "player.omroep.nl";
        }

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            if (!(url.ToLowerInvariant().Contains(@"ugslplayer.xap")))
                return base.getPlaybackOptions(url);

            int aflID = Convert.ToInt32(url.Split('&')[0].Split('=')[1]);
            XmlDocument doc = new XmlDocument();
            string seqData = SiteUtilBase.GetWebData(@"http://pi.omroep.nl/info/security/");
            doc.LoadXml(seqData);
            string data = doc.SelectSingleNode("session/key").InnerText;
            byte[] tmp = Convert.FromBase64String(data);
            string[] secparts = Encoding.ASCII.GetString(tmp).Split('|');

            string prehash = aflID.ToString() + '|' + secparts[1];
            string hash = Utils.GetMD5Hash(prehash).ToUpperInvariant();
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

        public override string getVideoUrls(string url)
        {
            int aflID = Convert.ToInt32(url.Split('&')[0].Split('=')[1]);

            CookieContainer cc = new CookieContainer();
            string step1 = SiteUtilBase.GetWebData(url, cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://tmp.player.omroep.nl/"));
            CookieContainer newcc = new CookieContainer();
            foreach (Cookie c in ccol) newcc.Add(c);

            step1 = SiteUtilBase.GetWebData("http://player.omroep.nl/js/initialization.js.php?aflID=" + aflID.ToString(), newcc);
            if (!String.IsNullOrEmpty(step1))
            {
                int p = step1.IndexOf("securityCode = '");
                if (p != -1)
                {
                    step1 = step1.Remove(0, p + 16);
                    string sec = step1.Split('\'')[0];
                    string step2 = SiteUtilBase.GetWebData("http://player.omroep.nl/xml/metaplayer.xml.php?aflID=" + aflID.ToString() + "&md5=" + sec, newcc);
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
        public override string getHosterUrl()
        {
            return "playmyvid.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            url = GetSubString(webData, @"flv=", @"&");
            return @"http://www.playmyvid.com/files/videos/" + url;
        }
    }

    public class ShareRepo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "sharerepo.com";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            if (url.Contains("embed"))
            {
                string page = SiteUtilBase.GetWebData(url);
                if (!string.IsNullOrEmpty(page))
                {
                    Match n = Regex.Match(page, @"'file=(?<url>[^']+)'");
                    if (n.Success)
                    {
                        videoType = VideoType.flv;
                        return n.Groups["url"].Value;
                    }
                }
            }
            else
            {
                string page = SiteUtilBase.GetWebData(url, cc);

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

                        string page2 = SiteUtilBase.GetWebDataFromPost(url, postdata, cc, url);

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
                                string unpacked = UnPack(packed);
                                videoType = VideoType.divx;
                                string res = GetSubString(unpacked, @"'file','", @"'");
                                if (!String.IsNullOrEmpty(res))
                                    resUrl = res;
                                else
                                    resUrl = GetSubString(unpacked, @"name=""src""value=""", @"""");
                            }
                            else
                                resUrl = GetSubString(page2, @"addVariable('file','", @"'");
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
        public override string getHosterUrl()
        {
            return "smotri.com";
        }

        public override string getVideoUrls(string url)
        {
            string videoId = GetSubString(url, "?id=", null);

            string webData = SiteUtilBase.GetWebData(url);
            string postData = GetSubString(webData, @"so.addVariable('context',", @""");");
            postData = GetSubString(postData, @"""", null);
            postData = postData.Replace("_", "%5F");
            postData = postData.Replace(".", "%2E");
            postData = @"p%5Fid%5B1%5D=4&begun=1&video%5Furl=1&p%5Fid%5B0%5D=2&context=" +
                postData + @"&devid=LoadupFlashPlayer&ticket=" + videoId;

            webData = SiteUtilBase.GetWebDataFromPost(@"http://smotri.com/video/view/url/bot/", postData);
            //"{\"_is_loadup\":0,\"_vidURL\":\"http:\\/\\/file38.loadup.ru\\/4412949d467b8db09bd07eedc7127f57\\/4bd0b05a\\/9a\\/a1\\/c1ad0ea5c0e8268898d3449b9087.flv\",\"_imgURL\":\"http:\\/\\/frame2.loadup.ru\\/9a\\/a1\\/1191805.3.3.jpg\",\"botator_banner\":{\"4\":[{\"cnt_tot_max\":1120377,\"cnt_hour_max\":4500,\"clc_tot_max\":0,\"clc_hour_max\":0,\"cnt_uniq_day_max\":3,\"cnt_uniq_week_max\":0,\"cnt_uniq_month_max\":0,\"link_transitions\":\"http:\\/\\/smotri.com\\/botator\\/clickator\\/click\\/?sid=qm2fzb5ruwdcj1ig_12\",\"zero_pixel\":\"http:\\/\\/ad.adriver.ru\\/cgi-bin\\/rle.cgi?sid=1&bt=21&ad=226889&pid=440944&bid=817095&bn=817095&rnd=1702217828\",\"signature\":{\"set_sign\":\"top\",\"signature\":\"\",\"signature_color\":null},\"link\":\"http:\\/\\/pics.loadup.ru\\/content\\/smotri.com_400x300_reenc_2.flv\",\"link_show\":\"http:\\/\\/smotri.com\\/botator\\/logator\\/show\\/?sid=qm2fzb5ruwdcj1ig_12\",\"banner_type\":\"video_flv\",\"b_id\":12}]},\"trustKey\":\"79e566c96057ce2b6f6055a3fa34f744\",\"video_id\":\"v119180501e5\",\"_pass_protected\":0,\"begun_url_1\":\"http:\\/\\/flash.begun.ru\\/banner.jsp?pad_id=100582787&offset=0&limit=5&encoding=utf8&charset=utf8&keywords=\"}"
            return GetSubString(webData, @"_vidURL"":""", @"""").Replace(@"\/", "/");
        }
    }

    public class Stagevu : HosterBase
    {
        public override string getHosterUrl()
        {
            return "stagevu.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            url = GetSubString(webData, @"url[", @"';");
            return GetSubString(url, @"'", @"'");
        }
    }

    public class Tudou : HosterBase
    {
        public override string getHosterUrl()
        {
            return "tudou.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string iid = GetSubString(webData, @"var iid = ", "\n");
            //url = @"http://v2.tudou.com/v?it=" + iid;
            url = @"http://v2.tudou.com/v?vn=02&ui=0&refurl=" + HttpUtility.UrlEncode(url) + @"&it=" + iid + @"&pw=&noCache=13678&st=1%2C2&si=sp";
            //http://v2.tudou.com/v?vn=02&ui=0&refurl=http%3A%2F%2Fwww%2Etudou%2Ecom%2Fprograms%2Fview%2FXQ1dE6XJWnU&it=20391047&pw=&noCache=13678&st=1%2C2&si=sp
            XmlDocument doc = new XmlDocument();
            webData = SiteUtilBase.GetWebData(url);
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
        public override string getHosterUrl()
        {
            return "2gb-hosting.com";
        }

        public override string getVideoUrls(string url)
        {
            string postData = String.Empty;
            string webData = SiteUtilBase.GetWebData(url);
            string post = GetSubString(webData, @"<form>", @"</form>");
            Match m = Regex.Match(webData, @"<input\stype=""[^""]*""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
            while (m.Success)
            {
                if (!String.IsNullOrEmpty(postData))
                    postData += "&";
                postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                m = m.NextMatch();
            }
            webData = SiteUtilBase.GetWebDataFromPost(url, postData);
            string res = GetSubString(webData, @"embed", @">");
            res = GetSubString(res, @"src=""", @"""");
            return res;
        }
    }

    public class Ufliq : HosterBase
    {
        public override string getHosterUrl()
        {
            return "ufliq.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
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

            webData = SiteUtilBase.GetWebDataFromPost(url, postData);
            string packed = GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);
            return GetSubString(unpacked, @"'file','", @"'");
        }
    }

    public class Veehd : HosterBase
    {
        public override string getHosterUrl()
        {
            return "veehd.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string tmp = GetSubString(webData, @"$(""#playeriframe"").attr({src : """, @"""");
            webData = SiteUtilBase.GetWebData(@"http://veehd.com" + tmp);
            return HttpUtility.UrlDecode(GetSubString(webData, @"""url"":""", @""""));
        }
    }

    public class Vidbull : HosterBase
    {
        public override string getHosterUrl()
        {
            return "vidbull.com";
        }

        public override string getVideoUrls(string url)
        {
            string[] parts = url.Split('.');
            string id = parts[parts.Length - 2];
            if (!id.Contains("-"))
                //probably http://www.vidbull.com/t9i14x2l8s0v.html, so work to http://vidbull.com/embed-t9i14x2l8s0v-650x328.html
                //that way, there's no need for a delay+post
                parts[parts.Length - 2] = id.Replace("com/", "com/embed-") + "-650x328";
            url = String.Join(".", parts);

            string webData = SiteUtilBase.GetWebData(url);
            string sub = GetSubString(webData, "id='flvplayer'", null);
            string packed = GetSubString(sub, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);
            string res = GetSubString(unpacked, @"'file','", @"'");

            return res;
        }

    }

    public class Vureel : HosterBase
    {
        public override string getHosterUrl()
        {
            return "vureel.com";
        }

        public override string getVideoUrls(string url)
        {
            string s = SiteUtilBase.GetWebData(url);
            return GetSubString(s, @"Referral: ", " ");
        }
    }

    public class Wisevid : HosterBase
    {
        public override string getHosterUrl()
        {
            return "wisevid.com";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = SiteUtilBase.GetWebData(url, cc, @"http://www.wisevid.com/");

            CookieCollection ccol = cc.GetCookies(new Uri("http://www.wisevid.com/"));
            CookieContainer newcc = new CookieContainer();
            foreach (Cookie c in ccol) newcc.Add(c);
            // (with age confirm)
            url = @"http://www.wisevid.com/play?v=" + GetSubString(webData,
                @"play?v=", @"'");
            //string tmp2 = SiteUtilBase.GetWebDataFromPost(url, "a=1");
            string tmp2 = SiteUtilBase.GetWebData(url, newcc);
            url = GetSubString(tmp2, "getF('", "'");
            byte[] tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }
    }

    public class Xtshare : HosterBase
    {
        public override string getHosterUrl()
        {
            return "xtshare.com";
        }

        public override string getVideoUrls(string url)
        {
            CookieContainer cc = new CookieContainer();
            string webData = SiteUtilBase.GetWebData(url, cc);
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
                webData = SiteUtilBase.GetWebDataFromPost(url, "submit=I+am+human+now+let+me+watch+this+video", newcc);
            }
            string file = GetSubString(webData, "'file','", "'");
            string streamer = GetSubString(webData, "'streamer','", "'");
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


}
