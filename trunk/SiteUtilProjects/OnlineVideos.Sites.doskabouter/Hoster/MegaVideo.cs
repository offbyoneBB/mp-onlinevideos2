using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using System.Xml;
using OnlineVideos.Sites;

namespace OnlineVideos.Hoster
{
    public class MegaVideo : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Megavideo.com";
        }

        public override string GetVideoUrl(string url)
        {
            XmlDocument doc = new XmlDocument();
            string id = string.Empty;

            //www.megavideo.com/v/XXXXXXXX
            if (url.Contains("/v/"))
                id = url.Substring(url.LastIndexOf("/") + 1, 8);
            else
                //www.megavideo.com?v=XXXXXXXX
                id = url.Substring(url.LastIndexOf("/") + 1, 11);

            if (id.StartsWith("?v=")) id = id.Substring(3, 8);
            if (id.StartsWith("?d="))
            {
                string webData = WebCache.Instance.GetWebData(url);
                string regEx = @"flashvars.v = ""(?<url>[^""]+)"";";
                Match m = Regex.Match(webData, regEx);
                if (m.Success)
                    id = m.Groups["url"].Value;
                else
                    return String.Empty;
            }

            string s = "http://www.megavideo.com/xml/videolink.php?v=" + id;
            s = WebCache.Instance.GetWebData(s);
            if (!s.Contains("This video has been removed due to infringement"))
            {
                doc.LoadXml(s);
                XmlNode node = doc.SelectSingleNode("ROWS/ROW");
                string server = node.Attributes["s"].Value;
                string decrypted = Decrypt(node.Attributes["un"].Value, node.Attributes["k1"].Value, node.Attributes["k2"].Value);
                return String.Format("http://www{0}.megavideo.com/files/{1}/", server, decrypted);
            }
            else return String.Empty;
        }

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
    }
}
