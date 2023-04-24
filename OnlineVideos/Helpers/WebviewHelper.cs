//#define fulllogging
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnlineVideos.Helpers
{
    public class WebViewHelper : MarshalByRefObject, IDisposable
    {
        protected static WebViewHelper _instance = null;
        protected static readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);

        public static WebViewHelper GetInstance(Form mainForm = null)
        {
            lock (_readyEvent)
            {
                if (_instance != null)
                    return _instance;

                _readyEvent.Reset();
                Thread newThread = new Thread(CreateInstance);
                newThread.SetApartmentState(ApartmentState.STA);
                newThread.Start();

                // Get the current main form of application
                if (mainForm != null)
                    mainForm.FormClosed += OnMainFormClosed;

                _readyEvent.WaitOne();
                return _instance;
            }
        }

        private static void OnMainFormClosed(object sender, FormClosedEventArgs formClosedEventArgs)
        {
            DisposeInstance();
        }

        private static void CreateInstance()
        {
            _instance = new WebViewHelper();
            _readyEvent.Set();
            // Window blocks here
            Application.Run(_instance._form);
            // ... until form gets closed
        }

        public static void DisposeInstance()
        {
            lock (_readyEvent)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }

        public void Dispose()
        {
            _form.Closed -= FormOnClosed;
            _form.Close();
        }


        //Only access this from the WebViewPlayer, as it can only be accessed from the main appdomain.
        public WebView2 GetWebViewForPlayer { get { return _webView; } }

        private readonly Form _form;
        private readonly WebView2 _webView;
        private bool _navCompleted;

        private WebViewHelper()
        {
            //should be created in main appdomain
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                Log.Error("WebViewHelper creation not called on the MPMain thread");
            }
            //else
            {
                Log.Debug("Creating WebViewHelper");

                _form = new Form();
                _form.FormBorderStyle = FormBorderStyle.SizableToolWindow;
                _form.AutoScaleMode = AutoScaleMode.None;
                _form.TopMost = true;
                _form.Width = 400;
                _form.Height = 300;
                _form.Closed += FormOnClosed;

                _webView = new WebView2();
                _webView.Name = "OV_Webview";
                _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                String cacheFolder = Path.Combine(Path.GetTempPath(), "WebViewplayer");
                _webView.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = cacheFolder };

                _webView.Dock = DockStyle.Fill;
                _form.Controls.Add(_webView);

                WaitForTaskCompleted(_webView.EnsureCoreWebView2Async());
            }
        }

        private static void FormOnClosed(object sender, EventArgs args)
        {
            DisposeInstance();
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

        private void Invoke(Action action)
        {
            if (_webView.InvokeRequired)
            {
                IAsyncResult iar = _webView.BeginInvoke(action);
                iar.AsyncWaitHandle.WaitOne();
            }
            else
            {
                action.Invoke();
            }
        }

        private TE Invoke<TE>(Func<TE> func)
        {
            if (_webView.InvokeRequired)
            {
                IAsyncResult iar = _webView.BeginInvoke(func);
                iar.AsyncWaitHandle.WaitOne();
                return (TE)_webView.EndInvoke(iar);
            }
            else
            {
                return func.Invoke();
            }
        }

        public void SetEnabled(bool enabled)
        {
            Invoke(() => _webView.Enabled = enabled);
        }

        public void SetUrl(string url)
        {
            Invoke(() => _webView.Source = new Uri(url));
        }

        public string GetUrl()
        {
            return Invoke(() => _webView.Source.AbsoluteUri);
        }

        public void Execute(string js)
        {
            Invoke(() => Exec(js));
        }

        public string ExecuteFunc(string js)
        {
            return Invoke(() =>
            {
                var rr = _webView.ExecuteScriptAsync(js);
                WaitForTaskCompleted(rr);
                return rr.Result;
            });
        }

        public string GetHtml(string url, string postData = null, string referer = null, NameValueCollection headers = null, bool blockOtherRequests = true)
        {
            var d = (Func<string>)delegate
            {
                Log.Debug("GetHtml-{1}: '{0}'", url, postData != null ? "POST" : "GET");
                if (_webView.Source.ToString() != url)
                {
                    var eventHandler = new EventHandler<CoreWebView2WebResourceRequestedEventArgs>(
                        (sender, e) => CoreWebView2_WebResourceRequested(e, url, referer, headers, blockOtherRequests)
                        );
                    _webView.CoreWebView2.WebResourceRequested += eventHandler;
                    _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                    if (postData == null)
                    {
                        _webView.Source = new Uri(url);
                    }
                    else
                    {
                        byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(postData);
                        MemoryStream stream = new MemoryStream(byteArray);
                        var request = _webView.CoreWebView2.Environment.CreateWebResourceRequest(url,
                                      "POST", stream, "Content-Type: application/x-www-form-urlencoded");
                        _webView.CoreWebView2.NavigateWithWebResourceRequest(request);
                    }
                    WaitUntilNavCompleted();
                    _webView.CoreWebView2.WebResourceRequested -= eventHandler;
                    _webView.CoreWebView2.RemoveWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                }
                try
                {
                    var tsk = _webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                    WaitForTaskCompleted(tsk);
                    string encoded = tsk.Result;
                    return (String)Newtonsoft.Json.JsonConvert.DeserializeObject(encoded);
                }
                catch (Exception e)
                {
                    Log.Error("Error getting html: " + e.Message);
                    return null;
                }
            };

            if (_webView.InvokeRequired)
                return (string)_webView.Invoke(d);
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
                e.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "Not found", null);
            }
        }

        public void WaitUntilNavCompleted()
        {
            _navCompleted = false;
            _webView.NavigationCompleted += Wv2_NavigationCompleted;
            do
            {
                Application.DoEvents();
            }
            while (!_navCompleted);
        }

        private void Wv2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
#if fulllogging
            Log.Debug("Navigation complete, id=" + e.NavigationId + " " + e.IsSuccess.ToString() + " " + e.HttpStatusCode.ToString());
#endif
            _navCompleted = true;
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
                    var tsk = _webView.CoreWebView2.CookieManager.GetCookiesAsync(url);
                    WaitForTaskCompleted(tsk);
                    return tsk.Result.ToDictionary(x => x.Name, x => x.ToSystemNetCookie());
                }
                catch (Exception e)
                {
                    Log.Error("Error getting cookies for " + url + ": " + e.Message);
                }
                return null;

            };
            if (_webView.InvokeRequired)
                return (Dictionary<String, Cookie>)_webView.Invoke(d);
            else
            {
                Log.Error("GetCookies should not be called from main thread");//will get into infinite loop
                return null;
            }
        }

        public void SetCookie(Cookie cookie)
        {
            Invoke(() =>
            {
                var cook = _webView.CoreWebView2.CookieManager.CreateCookieWithSystemNetCookie(cookie);
                _webView.CoreWebView2.CookieManager.AddOrUpdateCookie(cook);
            });
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
            return new Point(Convert.ToInt32(_webView.Location.X + _webView.Size.Width * relativePosition.X),
                Convert.ToInt32(_webView.Location.Y + _webView.Size.Height * relativePosition.Y));
        }

        /// <summary>
        /// Sends a left-click to the webview at position p
        /// </summary>
        /// <param name="p">Position of the click, relative to Mediaportal Mainform</param>
        public void SendClick(Point p)
        {
            _webView.Enabled = true;
            _webView.Focus();
            Application.DoEvents();
            Cursor.Position = new Point(_webView.Parent.Location.X + p.X, _webView.Parent.Location.Y + p.Y);
            CursorHelper.DoLeftMouseClick();
            _webView.Parent.Focus();
            _webView.Enabled = false;
        }

        private void WaitForTaskCompleted(Task t)
        {
            do
            {
                Application.DoEvents();
            }
            while (!t.IsCompleted);
        }

        private async void Exec(string js)
        {
            await _webView.ExecuteScriptAsync(js);
        }
    }
}
