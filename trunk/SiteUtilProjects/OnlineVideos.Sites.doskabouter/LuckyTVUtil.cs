using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using HybridDSP.Net.HTTP;
using System.IO;

namespace OnlineVideos.Sites
{
    public class LuckyTVUtil : GenericSiteUtil, ISimpleRequestHandler
    {
        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            ReverseProxy.Instance.AddHandler(this);
        }

        public override string getUrl(VideoInfo video)
        {
            string url = base.getUrl(video);
            return ReverseProxy.Instance.GetProxyUri(this, url);
        }

        public void UpdateRequest(HttpWebRequest request)
        {
            return;
        }
    }
}
