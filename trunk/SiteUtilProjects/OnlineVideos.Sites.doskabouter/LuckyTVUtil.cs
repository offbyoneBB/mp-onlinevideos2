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
    public class LuckyTVUtil : GenericSiteUtil, IRequestHandler
    {
        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            ReverseProxy.AddHandler(this);

        }

        public override string getUrl(VideoInfo video)
        {
            string url = base.getUrl(video);
            return ReverseProxy.GetProxyUri(this, url);
        }

        public bool DetectInvalidPackageHeader()
        {
            return false;
        }

        public void HandleRequest(string url, HTTPServerRequest request, HTTPServerResponse response)
        {
            HttpWebRequest proxyRequest = WebRequest.Create(url) as HttpWebRequest;

            if (proxyRequest == null)
            {
                response.Status = HTTPServerResponse.HTTPStatus.HTTP_NOT_FOUND;
                response.Send().Close();
            }
            else
            {

                HttpWebResponse siteResponse = proxyRequest.GetResponse() as HttpWebResponse;
                // copy headers
                foreach (string aKey in siteResponse.Headers.AllKeys) response.Set(aKey, siteResponse.Headers.Get(aKey));
                // restream data
                Stream responseStream = response.Send();
                Stream siteResponseStream = siteResponse.GetResponseStream();
                int read = 0;
                while (read < siteResponse.ContentLength)
                {
                    byte[] data = new byte[1024];
                    int fetched = siteResponseStream.Read(data, 0, 1024);
                    read += fetched;
                    responseStream.Write(data, 0, fetched);
                    if (fetched == 0 || read >= siteResponse.ContentLength) break;
                }
                responseStream.Flush();
                responseStream.Close();
            }
        }
    }
}
