using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class TelewizjadaUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Enable adult streams")]
        bool enableAdult = false;

        public override List<VideoInfo> GetVideos(Category category)
        {
            var data = GetWebData<JObject>(((RssLink)category).Url);
            List<VideoInfo> res = new List<VideoInfo>();
            foreach (JToken channel in data["channels"] as JArray)
            {
                VideoInfo v = new VideoInfo()
                {
                    Title = channel.Value<string>("displayName"),
                    VideoUrl = channel.Value<string>("url"),
                    Thumb = FormatDecodeAbsolutifyUrl(baseUrl, channel.Value<string>("bigThumb"), "{0}", UrlDecoding.None),
                    Other = channel.Value<string>("id")
                };
                if (enableAdult || channel.Value<int>("isAdult") == 0)
                    res.Add(v);
            }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            CookieContainer cc = new CookieContainer();
            string postData = "url=" + HttpUtility.UrlEncode(video.VideoUrl);
            string webData = GetWebData(@"http://www.telewizjada.net/set_cookie.php", postData, cc);
            var jData = GetWebData<JObject>(@"http://www.telewizjada.net/get_channel_url.php", "cid=" + video.Other.ToString(), cc);
            var webData2 = jData.Value<string>("url");
            string m3u = GetWebData(webData2, null, cc);
            Match m = Regex.Match(m3u, @"(?<ch>chu.*)");
            if (m.Success)
            {
                HttpUrl httpUrl = new HttpUrl(FormatDecodeAbsolutifyUrl(webData2, m.Groups["ch"].Value, "{0}", UrlDecoding.None));
                httpUrl.LiveStream = true;
                foreach (Cookie cookie in cc.GetCookies(new Uri(@"http://www.telewizjada.net")))
                    httpUrl.Cookies.Add(cookie);
                return httpUrl.ToString();
            }
            return String.Empty;
        }
    }
}
