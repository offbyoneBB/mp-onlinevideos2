using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;


namespace OnlineVideos.Sites.bw_fotoart
{
    public class iStreamUtil : GenericSiteUtil
    {
        public class iStreamVideoInfo : VideoInfo
        {
            public iStreamUtil Util { get; set; }

            public override string GetPlaybackOptionUrl(string option)
            {
                return getPlaybackUrl(PlaybackOptions[option], Util);
            }

            public static string getPlaybackUrl(string playerUrl, iStreamUtil Util)
            {
                string data = GetWebData(playerUrl, Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);
                Match n = Regex.Match(data, @"URL=(?<url>[^""]*)");

                if (n.Groups[1].Value == "http://istream.ws" || n.Groups[1].Value == "")
                {
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(playerUrl);
                    string e = myRequest.Address.Query.Substring(3);

                    string encodeQuery = System.Uri.EscapeDataString(e);
                    string newplayerurl = "http://istream.ws" + myRequest.Address.LocalPath +"?m="+ encodeQuery;
                    string data2 = GetWebData(newplayerurl, Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);

                    Match m = Regex.Match(data2, @"URL=(?<url>[^""]*)");
                    playerUrl = m.Groups[1].Value;

                }
                else { playerUrl = n.Groups[1].Value; }

                WebRequest request = WebRequest.Create(playerUrl);
                WebResponse response = request.GetResponse();
                string url = response.ResponseUri.ToString();
                Uri uri = new Uri(url);
                foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                    if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                    {
                        Dictionary<string, string> options = hosterUtil.getPlaybackOptions(url);
                        if (options != null && options.Count > 0)
                        {
                            url = options.Last().Value;
                        }
                        break;
                    }
                return url;
            }
        }

      
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the url for a specific hoster")]
        protected string hosterUrlRegEx;

        public override VideoInfo CreateVideoInfo()
        {
            return new iStreamVideoInfo() { Util = this };
        }

        public override string getUrl(VideoInfo video)
        {
            string result = base.getUrl(video);
            if (video.PlaybackOptions == null && !string.IsNullOrEmpty(result))
                result = iStreamVideoInfo.getPlaybackUrl(result, this);
            return result;
        }
    }
}