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
    public class WebViewHelper : MarshalByRefObject, IAsyncDisposable
    {
        protected static WebViewHelper _instance = null;
        protected static readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);
        protected static Form _mainForm;

        public static WebViewHelper GetInstance(Form mainForm = null)
        {
            lock (_readyEvent)
            {
                if (_instance != null)
                    return _instance;

                _mainForm = mainForm;
                if (_mainForm != null)
                {
                    _mainForm.FormClosed += OnMainFormClosed;
                    _mainForm.Deactivate += OnMainFormDeactivate();
                }

                _readyEvent.Reset();
                Thread newThread = new Thread(CreateInstance);
                newThread.SetApartmentState(ApartmentState.STA);
                newThread.Start();

                _readyEvent.WaitOne();

                return _instance;
            }
        }

        private static EventHandler OnMainFormDeactivate()
        {
            return (s, e) =>
            {
                Log.Debug("MainForm got deactivated, activate it again");
                _mainForm.Activate();
            };
        }

        private static async void OnMainFormClosed(object sender, FormClosedEventArgs formClosedEventArgs)
        {
            Log.Debug("MainForm got closed, close WebView");
            await DisposeInstance().ConfigureAwait(false);
        }

        private static void CreateInstance()
        {
            _instance = new WebViewHelper();
            _readyEvent.Set();
            // Window blocks here
            Log.Debug("WebView: Run viewer form now");
            Application.Run(_instance._form);
            // ... until form gets closed
        }

        public static async Task DisposeInstance()
        {
            Log.Debug("WebView: DisposeInstance");
            WebViewHelper instance;
            lock (_readyEvent)
            {
                instance = _instance;
                _instance = null;
            }

            if (instance != null)
                await instance.DisposeAsync().ConfigureAwait(false);
        }

        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }

        public async ValueTask DisposeAsync()
        {
            Log.Debug("WebView: DisposeAsync");
            _form.Closed -= FormOnClosed;
            Log.Debug("WebView: Form.Close()");
            _form.Close();
            Log.Debug("WebView: Form.Close() done...");
        }


        //Only access this from the WebViewPlayer, as it can only be accessed from the main appdomain.
        public Microsoft.Web.WebView2.WinForms.WebView2 GetWebViewForPlayer { get { return _webView; } }

        private readonly Form _form;
        private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webView;
        private bool _navCompleted;
        private bool _isDebug;

        public Form SynchronizationContext
        {
            get { return _form; }
        }

        private WebViewHelper()
        {
            //should be created in main appdomain
            //if (Thread.CurrentThread.ManagedThreadId != 1)
            //{
            //    Log.Error("WebViewHelper creation not called on the MPMain thread");
            //}
            //else
            {
                Log.Debug("Creating WebViewHelper");

                _form = new Form();
                _form.FormBorderStyle = FormBorderStyle.None;
                _form.AutoScaleMode = AutoScaleMode.None;
                _form.TopMost = false;
                // Start hidden, some sites just do background work first
                if (!_isDebug)
                {
                    _form.Visible = false;
                    _form.Left = -1000;
                    _form.Top = -1000;
                    _form.Width = 1;
                    _form.Height = 1;
                }
                else
                {
                    _form.Size = new System.Drawing.Size(400, 300);
                    _form.Location = new Point(0, 0);
                }

                _form.Closed += FormOnClosed;

                if (_mainForm != null)
                {
                    _form.Shown += (sender, args) =>
                    {
                        Log.Debug("WebViewForm created, bring back MainForm to from and activate");
                        if (!_isDebug)
                            _form.Hide();
                        _mainForm.BringToFront();
                        _mainForm.Activate();
                    };
                }

                _webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                _webView.Name = "OV_Webview";
                _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                String cacheFolder = Path.Combine(Path.GetTempPath(), "WebViewplayer");
                _webView.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = cacheFolder };

                _webView.Dock = DockStyle.Fill;
                _form.Controls.Add(_webView);

                WaitForTaskCompleted(_webView.EnsureCoreWebView2Async());
            }
        }

        public void DebugMode(bool enabled)
        {
            _isDebug = true;
            if (!enabled)
                Hide();
            _form.Size = new System.Drawing.Size(400, 300);
            _form.Location = new Point(0, 0);
            Show();
        }

        public void Show()
        {
            Log.Debug("WebViewForm: Show");
            _form.TopMost = true;
            _form.Visible = true;
        }

        public void Hide()
        {
            Log.Debug("WebViewForm: Hide");
            _form.TopMost = false;
            _form.Visible = false;
        }

        private static async void FormOnClosed(object sender, EventArgs args)
        {
            Log.Debug("WebView: FormOnClosed");
            await DisposeInstance();
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                Log.Error("Error initializing webview: {0}", e.InitializationException.Message);
#if fulllogging
            _webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
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
            if (_webView.IsDisposed)
                return;
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
            if (_webView.IsDisposed)
                return default;
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
        public void SetUrlAndWait(string url)
        {
            Invoke(() =>
            {
                // Don't try to load same page
                var uri = new Uri(url);
                if (uri == _webView.Source)
                    return;
                _webView.Source = uri;
                _navCompleted = false;
                _webView.Source = uri;
                WaitUntilNavCompleted();
            });
        }
        public void ExecuteAndWait(string js, string waitIfResultIs)
        {
            Invoke(() =>
            {
                _navCompleted = false;
                var result = ExecuteFunc(js);
                if (result == waitIfResultIs)
                {
                    WaitUntilNavCompleted();
                }
            });
        }
        public string GetCurrentPageContent()
        {
            return Invoke(() =>
            {
                var tsk = _webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                WaitForTaskCompleted(tsk);
                string encoded = tsk.Result;
                return (String)Newtonsoft.Json.JsonConvert.DeserializeObject(encoded);
            });
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

                    try
                    {
                        _navCompleted = false;
                        //_webView.NavigationCompleted += Wv2_NavigationCompleted;

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
                    }
                    finally
                    {
                        //_webView.NavigationCompleted -= Wv2_NavigationCompleted;
                    }

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
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            DateTime start = DateTime.Now;
            do
            {
                Thread.Sleep(10);
                Application.DoEvents();
                if (DateTime.Now - start > timeout)
                {
                    Log.Warn("WebViewHelper: Timeout of request (10s)");
                    break;
                }
            }
            while (!_navCompleted);
        }

        private async void Wv2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
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

        public void DeleteAllCookies()
        {
            Invoke(() => _webView.CoreWebView2.CookieManager.DeleteAllCookies());
        }
    }
}
