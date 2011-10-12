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
    public class NRJ12Util : GenericSiteUtil
    {

        public override string getUrl(VideoInfo video)
        {
            string resultUrl = "";
            string webData = GetWebData(video.VideoUrl);
            string id = Regex.Match(webData, @"var\splaylist_cur_med\s=\s(?<id>[^;]*);").Groups["id"].Value;
            string playerInfo = GetWebData("http://www.nrj12.fr/player/newplayer?media=" + id);

            if (Regex.Match(playerInfo, @"<item\sid=""video""\stype=""String""\svalue=""(?<m0>[^""]*)""/>").Success)
            {                
                string url = Regex.Match(playerInfo, @"<item\sid=""video""\stype=""String""\svalue=""(?<m0>[^""]*)""/>").Groups["m0"].Value;
                resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                    string.Format("http://127.0.0.1/stream.flv?hostname={0}&app={1}&playpath={2}&port={3}&tcUrl={4}",
                                        "stream2.nrj.yacast.net",
                                        "nrj",
                                        "flv:" + url.Replace(".flv", ""),
                                        "443",
                                        "rtmp://stream2.nrj.yacast.net:1935/nrj"
                                    ));
            }
            if (Regex.Match(playerInfo, @"name=""movie"" value=""(?<m0>[^""]*)""").Success)
            {
                string url = Regex.Match(playerInfo, @"name=""movie"" value=""(?<m0>[^""]*)""").Groups["m0"].Value;
                string dailyData = GetWebData(url.Replace("/swf/", "/"));
                resultUrl = GetSubString(dailyData, @"""video"", """, @""""); 
                //Regex.Match(dailyData, @"so\.addVariable\(""video"",\s""(?<m0>[^""]*)""\)").Groups["m0"].Value;
                Log.Info("Result URL : " + resultUrl);                
            }
            return resultUrl;
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
