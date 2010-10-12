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
            string s = HttpUtility.UrlDecode(SiteUtilBase.GetRedirectedUrl(url));
            int p = s.IndexOf("file=");
            int q = s.IndexOf('&', p);
            if (q < 0) q = s.Length;
            s = s.Substring(p + 5, q - p - 5);
            string rss = SiteUtilBase.GetWebData(s);
            p = rss.IndexOf(@"enclosure url="""); p += 15;
            q = rss.IndexOf('"', p);
            return rss.Substring(p, q - p);
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

    public class GoogleVideo : HosterBase
    {
        public override string getHosterUrl()
        {
            return "video.google";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string result = HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl\x3d", @"\x26"));
            if (!String.IsNullOrEmpty(result))
                return result;
            return HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl=", @"&amp;"));
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
            //babylon 5
            string webData = SiteUtilBase.GetWebData(url);
            string iid = GetSubString(webData, @"var iid = ", "\n");
            url = @"http://v2.tudou.com/v?it=" + iid;

            XmlDocument doc = new XmlDocument();
            webData = SiteUtilBase.GetWebData(url);
            doc.LoadXml(webData);
            XmlNodeList nodes = doc.SelectNodes("//v/b/f");
            string largest = null;
            foreach (XmlNode node in nodes)
                if (largest == null || String.Compare(largest, node.InnerText) == -1)
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
            return GetSubString(webData, @"name=""src"" value=""", @"""");
        }
    }

    public class Vidbux : HosterBase
    {
        public override string getHosterUrl()
        {
            return "vidbux.com";
        }

        public override string getVideoUrls(string url)
        {
            string webData = SiteUtilBase.GetWebData(url);
            string packed = GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);
            string res = GetSubString(unpacked, @"'file','", @"'");
            videoType = VideoType.divx;
            if (!String.IsNullOrEmpty(res))
                return res;
            return GetSubString(unpacked, @"name=""src""value=""", @"""");
        }
    }

    public class Vidxden : HosterBase
    {
        public override string getHosterUrl()
        {
            return "vidxden.com";
        }

        public override string getVideoUrls(string url)
        {
            if (url.Contains("embed"))
            {
                string page = SiteUtilBase.GetWebData(url);
                url = Regex.Match(page, @"<div><a\shref=""(?<url>[^""]+)""").Groups["url"].Value;
            }

            string[] urlParts = url.Split('/');

            string postData = @"op=download1&usr_login=&id=" + urlParts[3] + "&fname=" + urlParts[4] + "&referer=&method_free=Free+Stream";
            string webData = SiteUtilBase.GetWebDataFromPost(url, postData);
            string packed = GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UnPack(packed);
            string res = GetSubString(unpacked, @"'file','", @"'");
            videoType = VideoType.divx;
            if (!String.IsNullOrEmpty(res))
                return res;
            return GetSubString(unpacked, @"name=""src""value=""", @"""");

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
            string webData = SiteUtilBase.GetWebData(url);
            // (with age confirm)
            url = @"http://www.wisevid.com/play?v=" + GetSubString(webData,
                @"play?v=", @"""");
            string tmp2 = SiteUtilBase.GetWebDataFromPost(url, "a=1");
            url = GetSubString(tmp2, "getF('", "'");
            byte[] tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }
    }


}
