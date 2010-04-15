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

        public static string ZShareTrick(string Url)
        {
            string data = SiteUtilBase.GetWebData(Url);

            string tmpurl = "http://www.zshare.net/" + GetSubString(data, @"src=""http://www.zshare.net/", @"""");

            CookieContainer cc = new CookieContainer();
            data = SiteUtilBase.GetWebData(tmpurl, cc);
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

        public static string WiseVidTrick(string url)
        {
            byte[] tmp = Convert.FromBase64String(url);
            return Encoding.ASCII.GetString(tmp);
        }

        public static string YoutubeTrick(string url, VideoInfo video)
        {
            video.VideoUrl = url;
            video.PlaybackOptions = null;
            video.GetYouTubePlaybackOptions();
            return "";
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
