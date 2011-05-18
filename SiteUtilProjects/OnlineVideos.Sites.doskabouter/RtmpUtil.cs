using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Web;
using System.Text;

namespace OnlineVideos.Sites
{
    public class RtmpUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("rtmpurl")]
        protected string rtmpurl;
        [Category("OnlineVideosConfiguration"), Description("app")]
        protected string app;
        [Category("OnlineVideosConfiguration"), Description("tcUrl")]
        protected string tcUrl;
        [Category("OnlineVideosConfiguration"), Description("hostname")]
        protected string hostname;
        [Category("OnlineVideosConfiguration"), Description("port")]
        protected string port;
        [Category("OnlineVideosConfiguration"), Description("playpath")]
        protected string playpath;
        [Category("OnlineVideosConfiguration"), Description("subscribepath")]
        protected string subscribepath;
        [Category("OnlineVideosConfiguration"), Description("pageurl")]
        protected string pageurl;
        [Category("OnlineVideosConfiguration"), Description("swfurl")]
        protected string swfurl;
        [Category("OnlineVideosConfiguration"), Description("swfsize")]
        protected string swfsize;
        [Category("OnlineVideosConfiguration"), Description("swfhash")]
        protected string swfhash;
        [Category("OnlineVideosConfiguration"), Description("swfVfy")]
        protected string swfVfy;
        [Category("OnlineVideosConfiguration"), Description("live")]
        protected string live;
        [Category("OnlineVideosConfiguration"), Description("auth")]
        protected string auth;
        [Category("OnlineVideosConfiguration"), Description("token")]
        protected string token;
        [Category("OnlineVideosConfiguration"), Description("")]
        protected string conn;

        public override string getUrl(VideoInfo video)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("http://127.0.0.1/stream.flv?");

            string url;
            if (!String.IsNullOrEmpty(rtmpurl))
                url = rtmpurl;
            else
                url = base.getUrl(video);
            Uri uri = new Uri(url);
            NameValueCollection paramsHash = HttpUtility.ParseQueryString(uri.Query);

            UriBuilder ub = new UriBuilder(uri);
            ub.Query = String.Empty;

            sb.Append("rtmpurl=" + HttpUtility.UrlEncode(ub.ToString()));

            AddValue("app", app, paramsHash["app"], sb);
            AddValue("tcUrl", tcUrl, paramsHash["tcUrl"], sb);
            AddValue("hostname", hostname, paramsHash["hostname"], sb);
            AddValue("port", port, paramsHash["port"], sb);
            AddValue("playpath", playpath, paramsHash["playpath"], sb);
            AddValue("subscribepath", subscribepath, paramsHash["subscribepath"], sb);
            AddValue("pageurl", pageurl, paramsHash["pageurl"], sb);
            AddValue("swfurl", swfurl, paramsHash["swfurl"], sb);
            AddValue("swfsize", swfsize, paramsHash["swfsize"], sb);
            AddValue("swfhash", swfhash, paramsHash["swfhash"], sb);
            AddValue("swfVfy", swfVfy, paramsHash["swfVfy"], sb);
            AddValue("live", live, paramsHash["live"], sb);
            AddValue("auth", auth, paramsHash["auth"], sb);
            AddValue("token", token, paramsHash["token"], sb);
            AddValue("conn", conn, paramsHash["conn"], sb);


            return ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, sb.ToString());
        }

        private void AddValue(string name, string configValue, string urlValue, StringBuilder sb)
        {
            if (urlValue != null) sb.Append("&" + name + "=" + HttpUtility.UrlEncode(urlValue));
            else
                if (configValue != null) sb.Append("&" + name + "=" + HttpUtility.UrlEncode(configValue));
        }
    }
}
