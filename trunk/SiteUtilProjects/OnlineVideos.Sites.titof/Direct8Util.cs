using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class Direct8Util : GenericSiteUtil
    {

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> listUrls = new List<string>();
            string webData = GetWebData(video.VideoUrl);
            string url = Regex.Match(webData, @"<script\stype=""text/javascript""\ssrc=""http://direct8\.hexaglobe\.com/player(?<url>[^""]*)""></script>").Groups["url"].Value;
            webData = GetWebData(@"http://direct8.hexaglobe.com/player" + url, referer: video.VideoUrl);
            string baseUrl = Regex.Match(webData, @"baseUrl:.*?'(?<url>[^']*)'").Groups["url"].Value;
            Match m = Regex.Match(webData, @"url\s:\s'(?<url>[^']*)'");
            while (m.Success)
            {
                listUrls.Add(baseUrl + m.Groups["url"].Value);
                m = m.NextMatch();
            }

            return listUrls;
        }

    }
}
