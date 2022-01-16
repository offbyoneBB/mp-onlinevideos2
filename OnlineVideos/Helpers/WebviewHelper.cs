using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
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

        public static void Dispose()
        {
            if (_Instance != null)
            {
                _Instance.webView.Dispose();
            }
            _Instance = null;
        }


        //Only access this from the WebViewPlayer, as it can only be accessed from the main appdomain.
        public WebView2 GetWebViewForPlayer;
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
        }

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
                var rr = execfunc(js);
                waitForTaskCompleted(rr);
                return rr.Result;
            };
            if (webView.InvokeRequired)
            {
                return (string)webView.Invoke(get);
            }
            else
                return (string)get();
        }

        public string GetHtml(string url)
        {
            var d = (Func<string>)delegate
            {
                if (webView.Source.ToString() != url)
                {
                    webView.Source = new Uri(url);
                    WaitUntilNavCompleted();
                }
                string encoded = doc();
                return (String)Newtonsoft.Json.JsonConvert.DeserializeObject(encoded);
            };
            if (webView.InvokeRequired)
                return (string)webView.Invoke(d);
            else
                return d.Invoke();
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
            if (e.IsSuccess)
            {
                navCompleted = true;
            }
        }

        public List<Cookie> GetCookies(string url)
        {
            var d = (Func<List<Cookie>>)delegate
            {
                var rr = getCookies(url);
                waitForTaskCompleted(rr);
                return rr.Result;
            };
            if (webView.InvokeRequired)
                return (List<Cookie>)webView.Invoke(d);
            else
                return d.Invoke();
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

        private string doc()
        {
            var rr = getDoc();
            waitForTaskCompleted(rr);
            return rr.Result;
        }

        private async Task<List<Cookie>> getCookies(string url)
        {
            List<CoreWebView2Cookie> res = null;
            try
            {
                res = await webView.CoreWebView2.CookieManager.GetCookiesAsync(url);
            }
            catch (Exception e)
            {
                Log.Error("Error getting cookies for " + url + ": " + e.Message);
            }
            return res.ConvertAll(x => x.ToSystemNetCookie());
        }

        private void waitForTaskCompleted(Task t)
        {
            do
            {
                Application.DoEvents();
            }
            while (!t.IsCompleted);
        }

        private async Task<String> getDoc()
        {
            try
            {
                return await webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            }
            catch (Exception e)
            {
                Log.Error("Error getting html: " + e.Message);
                return null;
            }
        }

        private async Task<string> execfunc(string js)
        {
            return await webView.ExecuteScriptAsync(js);
        }

        private async void exec(string js)
        {
            await webView.ExecuteScriptAsync(js);
        }
    }
}
