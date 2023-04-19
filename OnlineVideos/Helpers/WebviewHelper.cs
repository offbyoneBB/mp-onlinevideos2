//#define fulllogging
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace OnlineVideos.Helpers
{
    public class WebViewHelper : MarshalByRefObject
    {
        protected static WebViewHelper _Instance = null;
        public static WebViewHelper Instance
        {
            get
            {
                return _Instance ?? (_Instance = new WebViewHelper());
            }
        }

        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }

        public static void Dispose()
        {
            if (_Instance != null)
            {
                _Instance.webView.Dispose();
            }
            _Instance = null;
        }


        //Only access this from the WebViewPlayer, as it can only be accessed from the main appdomain.
        public WebView2 GetWebViewForPlayer { get { return webView; } }
        private WebView2 webView;

        private WebViewHelper()
        {
            //should be created in main appdomain
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Log.Error("WebViewHelper creation not called on the MPMain thread");
            }
            else
            {
                Log.Debug("Creating WebViewHelper");
                webView = new WebView2();
                webView.Name = "OV_Webview";
                webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                String cacheFolder = Path.Combine(Path.GetTempPath(), "WebViewplayer");
                webView.CreationProperties = new CoreWebView2CreationProperties() { UserDataFolder = cacheFolder };
                waitForTaskCompleted(webView.EnsureCoreWebView2Async());
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                Log.Error("Error initializing webview: {0}", e.InitializationException.Message);
#if fulllogging
            webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
#endif
        }

#if fulllogging
        private void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            Log.Debug("Response for: "+e.Request.Uri);
            Log.Debug("Headers");
            foreach (var key in e.Request.Headers)
                Log.Debug(key.Key + ":" + key.Value);
        }
#endif

        public void SetEnabled(bool enabled)
        {
            var d = (Action)delegate
            {
                webView.Enabled = enabled;
            };
            if (webView.InvokeRequired)
                webView.Invoke(d);
            else
                d.Invoke();
        }

        public void SetUrl(string url)
        {
            webView.Source = new Uri(url);
        }

        public string GetUrl()
        {
            return webView.Source.AbsoluteUri;
        }


        public void Execute(string js)
        {
            if (webView.InvokeRequired)
            {
                var get = (Action)delegate { exec(js); };
                webView.Invoke(get);
            }
            else
                exec(js);
        }

        public string ExecuteFunc(string js)
        {
            var get = (Func<string>)delegate
            {
                var rr = webView.ExecuteScriptAsync(js);
                waitForTaskCompleted(rr);
                return rr.Result;
            };
            if (webView.InvokeRequired)
            {
                return (string)webView.Invoke(get);
            }
            else
            {
                return (string)get.Invoke();
            }
        }

        public string GetHtml(string url, string postData = null, string referer = null, NameValueCollection headers = null, bool blockOtherRequests = true)
        {
            var d = (Func<string>)delegate
            {
                Log.Debug("GetHtml-{1}: '{0}'", url, postData != null ? "POST" : "GET");
                if (webView.Source.ToString() != url)
                {


                    var eventHandler = new EventHandler<CoreWebView2WebResourceRequestedEventArgs>(
                        (sender, e) => CoreWebView2_WebResourceRequested(e, url, referer, headers, blockOtherRequests)
                        );
                    webView.CoreWebView2.WebResourceRequested += eventHandler;
                    webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                    if (postData == null)
                    {
                        webView.Source = new Uri(url);
                    }
                    else
                    {
                        byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(postData);
                        MemoryStream stream = new MemoryStream(byteArray);
                        var request = webView.CoreWebView2.Environment.CreateWebResourceRequest(url,
                                      "POST", stream, "Content-Type: application/x-www-form-urlencoded");
                        webView.CoreWebView2.NavigateWithWebResourceRequest(request);
                    }
                    WaitUntilNavCompleted();
                    webView.CoreWebView2.WebResourceRequested -= eventHandler;
                    webView.CoreWebView2.RemoveWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                }
                try
                {
                    var tsk = webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                    waitForTaskCompleted(tsk);
                    string encoded = tsk.Result;
                    return (String)Newtonsoft.Json.JsonConvert.DeserializeObject(encoded);
                }
                catch (Exception e)
                {
                    Log.Error("Error getting html: " + e.Message);
                    return null;
                }
            };
            if (webView.InvokeRequired)
                return (string)webView.Invoke(d);
            else
            {
                Log.Error("GetHtml should not be called from main thread");//will get into infinite loop
                return null;
            }
        }

        private void CoreWebView2_WebResourceRequested(CoreWebView2WebResourceRequestedEventArgs e, string url, string referer, NameValueCollection headers,
            bool blockOtherRequests)
        {
            if (headers != null)
                foreach (var key in headers.AllKeys)
                {
                    e.Request.Headers.SetHeader(key, headers[key]);
                }

            if (referer != null)
                e.Request.Headers.SetHeader("Referer", referer);
#if fulllogging
            Log.Debug("Request:" + e.Request.Method + " " + url);
            Log.Debug("Headers");
            foreach (var key in e.Request.Headers)
                Log.Debug(key.Key + ":" + key.Value);
#endif
            if (blockOtherRequests && e.Request.Uri != url)
            {
                e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "Not found", null);
            }
        }

        private bool navCompleted;
        public void WaitUntilNavCompleted()
        {
            navCompleted = false;
            webView.NavigationCompleted += Wv2_NavigationCompleted;
            do
            {
                Application.DoEvents();
            }
            while (!navCompleted);
        }

        private void Wv2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
#if fulllogging
            Log.Debug("Navigation complete, id=" + e.NavigationId + " " + e.IsSuccess.ToString() + " " + e.HttpStatusCode.ToString());
#endif
            navCompleted = true;
            if (!e.IsSuccess)
            {
                Log.Debug("Error navigating, result: {0}", e.HttpStatusCode);
            }
        }

        public Dictionary<String, Cookie> GetCookies(string url)
        {
            var d = (Func<Dictionary<String, Cookie>>)delegate
            {
                try
                {
                    var tsk = webView.CoreWebView2.CookieManager.GetCookiesAsync(url);
                    waitForTaskCompleted(tsk);
                    return tsk.Result.ToDictionary(x => x.Name, x => x.ToSystemNetCookie());
                }
                catch (Exception e)
                {
                    Log.Error("Error getting cookies for " + url + ": " + e.Message);
                }
                return null;

            };
            if (webView.InvokeRequired)
                return (Dictionary<String, Cookie>)webView.Invoke(d);
            else
            {
                Log.Error("GetCookies should not be called from main thread");//will get into infinite loop
                return null;
            }
        }

        public void SetCookie(Cookie cookie)
        {
            var d = (Action)delegate
            {
                var cook = webView.CoreWebView2.CookieManager.CreateCookieWithSystemNetCookie(cookie);
                webView.CoreWebView2.CookieManager.AddOrUpdateCookie(cook);
            };
            if (webView.InvokeRequired)
                webView.Invoke(d);
            else
                d.Invoke();
        }

        /// <summary>
        /// Returns a Point on screen according to fraction of width and height
        /// so (0,0) will return the top left coordinate of the screen, (1,1) bottom right and (0.5,0.5) in the middle
        /// Result is relative to Mediaportal Mainform
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <returns></returns>
        public Point GetPointOnScreen(PointF relativePosition)
        {
            return new Point(Convert.ToInt32(webView.Location.X + webView.Size.Width * relativePosition.X),
                Convert.ToInt32(webView.Location.Y + webView.Size.Height * relativePosition.Y));
        }

        /// <summary>
        /// Sends a left-click to the webview at position p
        /// </summary>
        /// <param name="p">Position of the click, relative to Mediaportal Mainform</param>
        public void SendClick(Point p)
        {
            webView.Enabled = true;
            webView.Focus();
            Application.DoEvents();
            Cursor.Position = new Point(webView.Parent.Location.X + p.X, webView.Parent.Location.Y + p.Y);
            CursorHelper.DoLeftMouseClick();
            webView.Parent.Focus();
            webView.Enabled = false;
        }



        private void waitForTaskCompleted(Task t)
        {
            do
            {
                Application.DoEvents();
            }
            while (!t.IsCompleted);
        }

        private async void exec(string js)
        {
            await webView.ExecuteScriptAsync(js);
        }
    }
}
