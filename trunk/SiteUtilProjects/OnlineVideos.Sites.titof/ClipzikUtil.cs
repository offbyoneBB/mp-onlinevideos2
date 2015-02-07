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
    public class ClipzikUtil : GenericSiteUtil
    {

        public override string GetVideoUrl(VideoInfo video)
        {            
            string webData = GetWebData(video.VideoUrl);
            if (webData.Contains(@"youtube.com"))
            {
                string id = Regex.Match(webData, @"http://www\.youtube\.com/v/(?<url>[^&|""]*)").Groups["url"].Value;
                return Hoster.HosterFactory.GetHoster("Youtube").GetVideoUrl(id);
            }
            else
            {
                string url = Regex.Match(webData, @"<embed\ssrc=""(?<m0>[^""]*)""").Groups["m0"].Value;
                string dailyData = GetWebData(url.Replace("/swf/", "/video/"));
                return GetSubString(dailyData, @"""video"", """, @""""); 
            }
            
        }

        protected string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}
