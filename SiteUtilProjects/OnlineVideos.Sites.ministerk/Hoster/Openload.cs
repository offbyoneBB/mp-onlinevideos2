﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Openload : HosterBase
    {

        public override string GetHosterUrl()
        {
            return "openload.co";
        }

        public override string GetVideoUrl(string url)
        {

            string data = GetWebData<string>(url);
            Regex rgx = new Regex(@"<div style=""display:none;"">[\s\r\n]*<span id=""[^""]*"">(?<enc>.+?)</span>");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string enc = HttpUtility.HtmlDecode(m.Groups["enc"].Value);
                string chars = "";
                int last = (int)enc[enc.Length - 1];
                int eCount = enc.Count();
                for (int i = 0; i < eCount; i++)
                {
                    int j = (int)enc[i];
                    if (j == last)
                        j -= 1;
                    else if (j == last - 1)
                        j += 1;
                    if (j >= 33 && j <= 126)
                        j = ((j + 14) % 94) + 33;
                    chars += (char)(j + (i == eCount - 1 ? 2 : 0));
                }
                url = "https://openload.co/stream/" + chars + "?mime=true";
                return url;
            }
            return "";
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