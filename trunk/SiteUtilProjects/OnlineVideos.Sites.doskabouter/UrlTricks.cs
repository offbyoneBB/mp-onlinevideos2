using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Net;

namespace OnlineVideos.Sites
{
    public static class UrlTricks
    {
        private static String Decrypt(String str_hex, String str_key1, String str_key2)
        {
            // 1. Convert hexadecimal string to binary string
            //char[] chr_hex = str_hex.toCharArray();
            String str_bin = "";
            for (int i = 0; i < 32; i++)
            {
                int b = int.Parse(str_hex[i].ToString(), System.Globalization.NumberStyles.HexNumber);
                String temp1 = Convert.ToString(b, 2);
                while (temp1.Length < 4) temp1 = '0' + temp1;
                str_bin += temp1;
            }
            char[] chr_bin = str_bin.ToCharArray();

            // 2. Generate switch and XOR keys
            int key1 = int.Parse(str_key1);
            int key2 = int.Parse(str_key2);
            int[] key = new int[384];
            for (int i = 0; i < 384; i++)
            {
                key1 = (key1 * 11 + 77213) % 81371;
                key2 = (key2 * 17 + 92717) % 192811;
                key[i] = (key1 + key2) % 128;
            }

            // 3. Switch bits positions
            for (int i = 256; i >= 0; i--)
            {
                char temp3 = chr_bin[key[i]];
                chr_bin[key[i]] = chr_bin[i % 128];
                chr_bin[i % 128] = temp3;
            }

            // 4. XOR entire binary string
            for (int i = 0; i < 128; i++)
                chr_bin[i] = (char)(chr_bin[i] ^ key[i + 256] & 1);

            // 5. Convert binary string back to hexadecimal
            str_bin = new String(chr_bin);
            str_hex = "";
            for (int i = 0; i < 128; i += 4)
            {
                string binary = str_bin.Substring(i, 4);
                str_hex += Convert.ToByte(binary, 2).ToString("x");
            }
            return str_hex;
        }

        public static string MegaVideoTrick(string url)
        {
            XmlDocument doc = new XmlDocument();
            string s = url.Insert(25, "xml/videolink.php");
            s = SiteUtilBase.GetWebData(s);
            doc.LoadXml(s);
            XmlNode node = doc.SelectSingleNode("ROWS/ROW");
            string server = node.Attributes["s"].Value;
            string decrypted = Decrypt(node.Attributes["un"].Value, node.Attributes["k1"].Value, node.Attributes["k2"].Value);
            return String.Format("http://www{0}.megavideo.com/files/{1}/", server, decrypted);
        }

        public static string BlipTrick(string url)
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

        public static string PlayerOmroepTrick(string Url)
        {
            int aflID = Convert.ToInt32(Url.Split('=')[1]);
            CookieContainer cc = new CookieContainer();
            string step1 = SiteUtilBase.GetWebData(Url, cc);
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
                            return final.InnerText;

                    }
                }

            }
            return null;
        }

        public static string DivxDenTrick(string Url)
        {
            //http://www.watch-series.com/open_link.php?vari=Y2BmaGVt werkt niet!
            // laad: http://www.divxden.com/l9qqwqjnto2g/qfn-smpsns2102.flv.html
            // dan een post naar dat adres. zoek naar addVariable, en daar staat dan:
            // addVariable|http|com|divxden|addParam|player|true|s4||flvplayer|write|autostart||type|jpg|l9qqwqjnto2g|00000||image|flv|smpsns2102|qfn|rlua5vaty4yeu53c7vqdgsluthlsjvgdhrqhq7i|182|file|opaque|wmode|always|allowscriptaccess|allowfullscreen|318|640|swf|www|SWFObject|new|var'.split('|')))
            //            |http|com|divxden|addParam|player|true|s6||flvplayer|write|autostart||type|jpg|p5ao2i4gqhir|00001||image|flv|net|dome|The_Simpsons_2002_tv|rpua5vapzmqfimt2unutmtrprqn6eiimzcaffhq|182|file|opaque|wmode|always|allowscriptaccess|allowfullscreen|318|640|swf|www|SWFObject|new|var"
            //voor http://s4.divxden.com:182/d/rlua5vaty4yeu53c7vqdgsluthlsjvgdhrqhq7i/qfn-smpsns2102.flv

            string[] urlParts = Url.Split('/');

            string postData = @"op=download1&usr_login=&id=" + urlParts[3] + "&fname=" + urlParts[4] + "&referer=&method_free=Free+Stream";
            string webData = MySiteUtil.GetWebDataFromPost(Url, postData);
            string packed = GetSubString(webData, @"return p}", @"</script>");
            packed = packed.Replace(@"\'", @"'");
            string unpacked = UrlTricks.UnPack(packed);
            string res = GetSubString(unpacked, @"'file','", @"'");
            if (!String.IsNullOrEmpty(res))
                return res;
            return GetSubString(unpacked, @"name=""src""value=""", @"""");
        }

        public static string SmotriTrick(string Url)
        {
            string videoId = GetSubString(Url, "?id=", null);


            string webData = SiteUtilBase.GetWebData(Url);
            string postData = GetSubString(webData, @"so.addVariable('context',", @""");");
            postData = GetSubString(postData, @"""", null);
            postData = postData.Replace("_", "%5F");
            postData = postData.Replace(".", "%2E");
            postData = @"p%5Fid%5B1%5D=4&begun=1&video%5Furl=1&p%5Fid%5B0%5D=2&context=" +
                postData + @"&devid=LoadupFlashPlayer&ticket=" + videoId;

            webData = MySiteUtil.GetWebDataFromPost(@"http://smotri.com/video/view/url/bot/", postData);
            //"{\"_is_loadup\":0,\"_vidURL\":\"http:\\/\\/file38.loadup.ru\\/4412949d467b8db09bd07eedc7127f57\\/4bd0b05a\\/9a\\/a1\\/c1ad0ea5c0e8268898d3449b9087.flv\",\"_imgURL\":\"http:\\/\\/frame2.loadup.ru\\/9a\\/a1\\/1191805.3.3.jpg\",\"botator_banner\":{\"4\":[{\"cnt_tot_max\":1120377,\"cnt_hour_max\":4500,\"clc_tot_max\":0,\"clc_hour_max\":0,\"cnt_uniq_day_max\":3,\"cnt_uniq_week_max\":0,\"cnt_uniq_month_max\":0,\"link_transitions\":\"http:\\/\\/smotri.com\\/botator\\/clickator\\/click\\/?sid=qm2fzb5ruwdcj1ig_12\",\"zero_pixel\":\"http:\\/\\/ad.adriver.ru\\/cgi-bin\\/rle.cgi?sid=1&bt=21&ad=226889&pid=440944&bid=817095&bn=817095&rnd=1702217828\",\"signature\":{\"set_sign\":\"top\",\"signature\":\"\",\"signature_color\":null},\"link\":\"http:\\/\\/pics.loadup.ru\\/content\\/smotri.com_400x300_reenc_2.flv\",\"link_show\":\"http:\\/\\/smotri.com\\/botator\\/logator\\/show\\/?sid=qm2fzb5ruwdcj1ig_12\",\"banner_type\":\"video_flv\",\"b_id\":12}]},\"trustKey\":\"79e566c96057ce2b6f6055a3fa34f744\",\"video_id\":\"v119180501e5\",\"_pass_protected\":0,\"begun_url_1\":\"http:\\/\\/flash.begun.ru\\/banner.jsp?pad_id=100582787&offset=0&limit=5&encoding=utf8&charset=utf8&keywords=\"}"
            return GetSubString(webData, @"_vidURL"":""", @"""").Replace(@"\/", "/");
        }

        public static string MovShareTrick(string Url)
        {
            string webData = MySiteUtil.GetWebDataFromPost(Url, "submit.x=119&submit.y=19");
            return GetSubString(webData, @"<param name=""src"" value=""", @"""");
        }

        public static string ZShareTrick(string Url)
        {
            CookieContainer cc = new CookieContainer();
            string data = SiteUtilBase.GetWebData(Url, cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://tmp.zshare.net"));
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

            return turl;
        }

        public static string TudouTrick(string Url, ISimpleRequestHandler reqHandler)
        {
            XmlDocument doc = new XmlDocument();
            string webData = SiteUtilBase.GetWebData(Url);
            doc.LoadXml(webData);
            XmlNodeList nodes = doc.SelectNodes("//v/b/f");
            string largest = null;
            foreach (XmlNode node in nodes)
                if (largest == null || String.Compare(largest, node.InnerText) == -1)
                    largest = node.InnerText;
            return ReverseProxy.GetProxyUri(reqHandler, largest);
        }

        public static string WiseVidTrick(string url)
        {
            byte[] tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }

        public static string FiftySixComTrick(string Url)
        {
            //Url=http://www.56.com/u90/v_MzYxNzA2MzE.html
            string id = GetSubString(Url, "/v_", ".html");
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

        public static string LiveVideoTrick(string Url)
        {
            return null;
        }

        public static string VureelTrick(string Url)
        {
            string s = SiteUtilBase.GetWebData(Url);
            return GetSubString(s, @"Referral: ", " ");

            /*
            s = @"D{kzqx|(|""xmE*|m!|7ri~i{kzqx|*F~iz({9(E(vm([_NWjrmk|0*p||xB77{|i|qk6~}zmmt6kwu7~zmkgxti""mz6{n*4*xt""*4*><:*4*;A8*4*A*4*+NNNNNN*1C{96illXiziu0*uwlm*4*|ziv{xizmv|*1C{96illXiziu0*ittw[kzqx|Ikkm{{*4(*iti""{*1C{96illXiziu0*ittwn}tt{kzmmv*4*|z}m*1C{96illXiziu0*nti{p~iz{*4*nqtmEp||xB77{=6~}zmmt6kwu7tmmkpqvoq{qttmoit7@l;=;9;jmmA>nA>k;@j==nk;?>=k<?@=7<jlj=<ki7989=96nt~.{|zm|kpqvoEm!ik|nq|.|""xmE~qlmw.t|i{6umlqiqlE989=9.{sqvEp||xB77{|i|qk6~}zmmt6kwu7kwum|6{n*1C{96zq|m0*umlqi{xikm*1CD7{kzqx|F";
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            byte[]newb=new byte[bytes.Length];
            for (int i=0;i<bytes.Length;i++)
            {
                int n = bytes[i];
                if (n < 128) { n = n - 8; if (n < 32) { n = 127 + (n - 32); } }
                newb[i] = (byte)n;
            }
            string t = Encoding.ASCII.GetString(newb);
             */
        }

        public static string YoutubeTrick(string url, VideoInfo video)
        {
            video.VideoUrl = url;
            video.PlaybackOptions = null;
            video.GetYouTubePlaybackOptions();
            return url;
        }

        public static string GoogleCaTrick(string Url)
        {
            string webData = SiteUtilBase.GetWebData(Url);
            string result = HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl\x3d", @"\x26"));
            if (!String.IsNullOrEmpty(result))
                return result;
            return HttpUtility.UrlDecode(GetSubString(webData, @"videoUrl=", @"&amp;"));
        }

        public static string MyspaceTrick(string Url)
        {
            string videoId = GetSubString(Url, "videoid=", "&");
            string webData = SiteUtilBase.GetWebData(@"http://mediaservices.myspace.com/services/rss.ashx?videoID=" + videoId);
            string fileUrl = GetSubString(webData, @"RTMPE url=""", @"""");

            //return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&swfhash=a51d59f968ffb279f0a3c0bf398f2118b2cc811f04d86c940fd211193dee2013&swfsize=770329",
            return fileUrl.Replace("rtmp:", "rtmpe:");
        }

        public static string UnPack(string packed)
        {
            string res;
            int p = packed.IndexOf('|');
            if (p < 0) return null;
            p = packed.LastIndexOf('\'', p);

            string pattern = packed.Substring(0, p - 1);

            string[] pars = packed.Substring(p).TrimStart('\'').Split('|');
            for (int i = 0; i < pars.Length; i++)
                if (String.IsNullOrEmpty(pars[i]))
                    if (i < 10)
                        pars[i] = i.ToString();
                    else
                        if (i < 36)
                            pars[i] = ((char)(i + 0x61 - 10)).ToString();
                        else
                            pars[i] = (i - 26).ToString();
            res = String.Empty;
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                int ind = -1;
                if (Char.IsDigit(c))
                {
                    if (i + 1 < pattern.Length && Char.IsDigit(pattern[i + 1]))
                    {
                        ind = int.Parse(pattern.Substring(i, 2)) + 26;
                        i++;
                    }
                    else
                        ind = int.Parse(pattern.Substring(i, 1));
                }
                else
                    if (Char.IsLower(c))
                        ind = ((int)c) - 0x61 + 10;

                if (ind == -1 || ind >= pars.Length)
                    res += c;
                else
                    res += pars[ind];
            }
            return res;
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        // use MySiteUtil until everybody has 0.21 or higher, there the needed methods of SiteUtilBase are public. 
        // After that: Remove MySiteUtil and replace references with SiteUtilBase
        private class MySiteUtil : SiteUtilBase
        {
            public override List<VideoInfo> getVideoList(Category category)
            {
                throw new NotImplementedException();
            }
            public override string getUrl(VideoInfo video)
            {
                return base.getUrl(video);
            }

            public static new string GetWebDataFromPost(string url, string postData)
            {
                return SiteUtilBase.GetWebDataFromPost(url, postData);
            }

        }

    }
}
