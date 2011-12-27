using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Web;
using System.Text;
using OnlineVideos.MPUrlSourceFilter;

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
        [Category("OnlineVideosConfiguration"), Description("playpath")]
        protected string playpath;
        [Category("OnlineVideosConfiguration"), Description("subscribepath")]
        protected string subscribepath;
        [Category("OnlineVideosConfiguration"), Description("pageurl")]
        protected string pageurl;
        [Category("OnlineVideosConfiguration"), Description("swfurl")]
        protected string swfurl;
        [Category("OnlineVideosConfiguration"), Description("swfVfy")]
        protected string swfVfy;
        [Category("OnlineVideosConfiguration"), Description("live")]
        protected string live;
        [Category("OnlineVideosConfiguration"), Description("auth")]
        protected string auth;
        [Category("OnlineVideosConfiguration"), Description("token")]
        protected string token;

        public override string getUrl(VideoInfo video)
        {

            string url;
            if (!String.IsNullOrEmpty(rtmpurl))
                url = rtmpurl;
            else
                url = base.getUrl(video);

            RtmpUrl theUrl = new RtmpUrl(url.Split('?')[0]);

            Uri uri = new Uri(url);
            NameValueCollection paramsHash = HttpUtility.ParseQueryString(uri.Query);

            string t;

            if ((t = GetValue(app, paramsHash["app"])) != null) theUrl.App = t;
            if ((t = GetValue(tcUrl, paramsHash["tcUrl"])) != null) theUrl.TcUrl = t;
            if ((t = GetValue(playpath, paramsHash["playpath"])) != null) theUrl.PlayPath = t;
            if ((t = GetValue(subscribepath, paramsHash["subscribepath"])) != null) theUrl.Subscribe = t;
            if ((t = GetValue(pageurl, paramsHash["pageurl"])) != null) theUrl.PageUrl = t;
            if ((t = GetValue(swfurl, paramsHash["swfurl"])) != null) theUrl.SwfUrl = t;
            if ((t = GetValue(swfVfy, paramsHash["swfVfy"])) != null) theUrl.SwfVerify = Boolean.Parse(t);
            if ((t = GetValue(live, paramsHash["live"])) != null) theUrl.Live = Boolean.Parse(t);
            if ((t = GetValue(token, paramsHash["token"])) != null) theUrl.Token = t;

            return theUrl.ToString();
        }

        private string GetValue(string configValue, string urlValue)
        {
            if (urlValue != null) return urlValue;
            else
                if (configValue != null) return configValue;
                else
                    return null;
        }
    }
}
