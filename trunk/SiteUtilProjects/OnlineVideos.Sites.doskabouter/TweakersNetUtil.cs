using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.IO;

namespace OnlineVideos.Sites
{
    public class TweakersNetUtil : GenericSiteUtil
    {
        CookieContainer cc;

        public override int DiscoverDynamicCategories()
        {
            cc = new CookieContainer();
            string data = GetWebData(baseUrl, cookies: cc);
            Match m = Regex.Match(data, @"<input\stype=""hidden""\sname=""tweakers_token""\svalue=""(?<tweakerstoken>[^""]+)"">");
            if (m.Success)
            {
                string postData = @"decision=accept&returnTo=http%3A%2F%2Ftweakers.net%2F&tweakers_token=" +
                    HttpUtility.UrlEncode(m.Groups["tweakerstoken"].Value);

                cc = MyGetWebData(@"https://secure.tweakers.net/my.tnet/cookies/", postData, cc);
            }
            return base.DiscoverDynamicCategories();
        }

        private CookieContainer MyGetWebData(string url, string postData, CookieContainer cc)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats so we can say we accept them
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content
            request.CookieContainer = cc;

            request.Accept = "*/*";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;

            byte[] data = Encoding.UTF8.GetBytes(postData);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            request.AllowAutoRedirect = false;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) { };//just to get the cookies

            CookieContainer result = new CookieContainer();
            foreach (Cookie cookie in cc.GetCookies(new Uri("http://tmp.tweakers.net")))
            {
                cookie.Domain = new Uri(baseUrl).Host;
                result.Add(cookie);
            }
            return result;
        }


        protected override CookieContainer GetCookie()
        {
            return cc;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string res = base.GetVideoUrl(video);
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                return video.PlaybackOptions.Last().Value;
            return res;
        }
    }
}
