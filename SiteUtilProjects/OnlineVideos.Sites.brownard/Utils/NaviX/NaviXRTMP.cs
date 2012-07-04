using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXRTMP
    {
        public static MPUrlSourceFilter.RtmpUrl GetRTMPUrl(string naviXRTMPUrl)
        {
            Regex paramMatch = new Regex(@"\s+(tcUrl|app|playpath|swfUrl|pageUrl|swfVfy|live)\s*=\s*([^\s]*)");
            MatchCollection matches = paramMatch.Matches(naviXRTMPUrl);
            if (matches.Count < 1)
                return new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl);

            MPUrlSourceFilter.RtmpUrl url = new MPUrlSourceFilter.RtmpUrl(naviXRTMPUrl.Substring(0, matches[0].Index));
            foreach (Match m in matches)
            {
                string val = m.Groups[2].Value;
                switch (m.Groups[1].Value)
                {
                    case "tcUrl":
                        url.TcUrl = val;
                        break;
                    case "app":
                        url.App = val;
                        break;
                    case "playpath":
                        url.PlayPath = val;
                        break;
                    case "swfUrl":
                        url.SwfUrl = val;
                        break;
                    case "pageUrl":
                        url.PageUrl = val;
                        break;
                    case "swfVfy":
                        url.SwfUrl = val;
                        break;
                    case "live":
                        if (val == "1" || val.ToLower() == "true")
                            url.Live = true;
                        break;
                }
            }
            return url;
        }
    }
}
