using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations
{
    public abstract class BrowserSessionBase
    {
        protected CookieContainer _cc = new CookieContainer();
        protected string UserAgent { get; set; }

        protected BrowserSessionBase()
        {
            UserAgent = null;
        }

        /// <summary>
        /// Makes a HTTP GET request to the given URL
        /// </summary>
        public HtmlAgilityPack.HtmlDocument Load(string url)
        {
            var agilityDoc = new HtmlAgilityPack.HtmlDocument();
            string webData = GetWebData(url, null, _cc);
            agilityDoc.LoadHtml(webData);
            //Log.Info(url+" The doc:" + agilityDoc.DocumentNode.InnerHtml);
            return agilityDoc;
        }

        public string LoadAsStr(string url)
        {
            return GetWebData(url, null, _cc, null, null, false, false, UserAgent);
        }

        public Newtonsoft.Json.Linq.JObject LoadAsJSON(string url)
        {
            return GetWebData<Newtonsoft.Json.Linq.JObject>(url, null, _cc, null, null, false, false, UserAgent);
        }

        protected T GetWebData<T>(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            return WebCache.Instance.GetWebData<T>(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
        }

        protected string GetWebData(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            return WebCache.Instance.GetWebData(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
        }
    }

}