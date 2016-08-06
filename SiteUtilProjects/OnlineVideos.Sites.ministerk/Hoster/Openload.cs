using Jurassic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Openload : HosterBase
    {

        private string GetRgbValues(MemoryStream stream)
        {
            string rgb = "";
            using (Bitmap bmp = new Bitmap(stream))
            {
                List<byte> rgbValues = new List<byte>();
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        Color clr = bmp.GetPixel(x, y);
                        rgbValues.Add(clr.R);
                        rgbValues.Add(clr.G);
                        rgbValues.Add(clr.B);
                    }
                }
                rgb = System.Text.Encoding.ASCII.GetString(rgbValues.ToArray());
            }
            return rgb;
        }



        public override string GetHosterUrl()
        {
            return "openload.co";
        }

        public override string GetVideoUrl(string url)
        {
            string data = GetWebData<string>(url);
            Regex rgx = new Regex(@"<img id=""linkimg"" src=""data:image\/png;base64,(?<img>[^""]*)"">");
            Match m = rgx.Match(data);
            if (!m.Success)
                return "";
            string base64String = m.Groups["img"].Value;
            if (string.IsNullOrWhiteSpace(base64String))
                return "";
            byte[] img = Convert.FromBase64String(base64String);

            using (MemoryStream stream = new MemoryStream(img, 0, img.Length))
            {
                string pixelstring = GetRgbValues(stream);

                List<List<string>> imageTabs = new List<List<string>>();
                int i = -1;
                int j = 0;
                for (int idx = 0; idx < pixelstring.Length; idx++)
                {
                    char cString = pixelstring[idx];
                    if (cString.Equals('\0'))
                        break;
                    if (idx % (12 * 20) == 0)
                    {
                        imageTabs.Add(new List<string>());
                        i += 1;
                        j = -1;
                    }
                    if (idx % (20) == 0)
                    {
                        imageTabs[i].Add("");
                        j += 1;
                    }
                    imageTabs[i][j] += cString;
                }
                 
                string numbers = GetWebData("https://openload.co/assets/js/obfuscator/n.js", referer: url);
                Regex regex = new Regex(@"['""](?<n>[^""^']+?)['""]");
                Match nMatch = regex.Match(numbers);
                if (!nMatch.Success)
                    return "";
                string signStr = nMatch.Groups["n"].Value;

                List<List<string>> signTabs = new List<List<string>>();
                i = -1;
                j = 0;
                for (int idx = 0; idx < signStr.Length; idx++)
                {
                    char cString = signStr[idx];
                    if (cString.Equals('\0'))
                        break;
                    if (idx % (11 * 26) == 0)
                    {
                        signTabs.Add(new List<string>());
                        i += 1;
                        j = -1;
                    }
                    if (idx % (26) == 0)
                    {
                        signTabs[i].Add("");
                        j += 1;
                    }
                    signTabs[i][j] += cString;
                }

                List<string> linkData = new List<string>();
                int ldindex = -1;
                foreach (int ci in new List<int>() { 2, 3, 5, 7 })
                {
                    ldindex++;
                    linkData.Add("");
                    double tmp = 99.0; //'c'
                    for (int cj = 0; cj < signTabs[ci].Count; cj++)
                    {
                        for (int ck = 0; ck < signTabs[ci][cj].Length; ck++)
                        {
                            if (tmp > 122)
                                tmp = 98.0;//'b'
                            if (signTabs[ci][cj][ck] == (char)(int)Math.Floor(tmp))
                            {
                                if (linkData[ldindex].Length > cj)
                                    continue;
                                tmp += 2.5;
                                if (ck < imageTabs[ci][cj].Length)
                                {
                                    linkData[ldindex] += (imageTabs[ci][cj][ck]);
                                }
                            }
                        }
                    }
                }
                url = "https://openload.co/stream/" + linkData[3] + "~" + linkData[1] + "~" + linkData[2] + "~" + linkData[0] + "?mime=true";
                return url;
            }
        }
    }

    public class OpenloadIo : Openload
    {
        public override string GetHosterUrl()
        {
            return "openload.io";
        }

    }
}
