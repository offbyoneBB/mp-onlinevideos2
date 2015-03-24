using HtmlAgilityPack;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations
{
    public abstract class BrowserSessionBase
    {
        protected CookieContainer cc = new CookieContainer();
        protected string userAgent { get; set; } 

        public BrowserSessionBase()
        {
            userAgent = null;
        }

        /// <summary>
        /// Makes a HTTP GET request to the given URL
        /// </summary>
        public HtmlAgilityPack.HtmlDocument Load(string url)
        {
            var agilityDoc = new HtmlAgilityPack.HtmlDocument();
            string webData = GetWebData(url, (string)null, cc);
            agilityDoc.LoadHtml(webData);
            //Log.Info(url+" The doc:" + agilityDoc.DocumentNode.InnerHtml);
            return agilityDoc;
        }

        public string LoadAsStr(string url)
        {
            return GetWebData(url, (string)null, cc, null, null, false, false, userAgent);
        }

        public Newtonsoft.Json.Linq.JObject LoadAsJSON(string url)
        {
            return GetWebData<Newtonsoft.Json.Linq.JObject>(url, null, cc, null, null, false, false, userAgent );
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