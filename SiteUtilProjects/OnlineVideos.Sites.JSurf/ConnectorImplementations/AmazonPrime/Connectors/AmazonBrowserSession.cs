using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonBrowserSession : BrowserSessionBase
    {
        private Boolean _isLoggedIn;
        private long _lastLogin;
        private string _lastCulture;
        private string _userAgent = "Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko";

        public AmazonBrowserSession()
        {
            Log.Info("New Browser session");
            _isLoggedIn = false;
            _lastLogin = 0;
            _lastCulture = "";
            UserAgent = _userAgent;
        }

        public void Login(string username, string password, bool forceLogin = false)
        {
            string culture = Properties.Resources.Culture.ToString();
            long day = DateTime.Now.Ticks / TimeSpan.TicksPerDay;

            if (day > _lastLogin || culture != _lastCulture)
            {
                forceLogin = true;
            }

            if (!_isLoggedIn || forceLogin)
            {
                Log.Info("Login to Amazon " + Properties.Resources.AmazonRootUrl);
                _isLoggedIn = false;
                _lastCulture = culture;

                // Load the main page inside a WebBrowser control to retrieve all dynamically created cookies (session ids by JS)
                _cc = GetCookies(Properties.Resources.AmazonLoginUrl);

                var loginDoc = Load(Properties.Resources.AmazonLoginUrl);
                var formElements = new FormElementCollection(loginDoc);
                // There can appear different login pages, using other names for form / controls
                var loginForm = loginDoc.GetElementbyId("ap_signin_form") ?? loginDoc.DocumentNode.SelectNodes("//*[@name='signIn']").FirstOrDefault();
                if (loginForm == null)
                {
                    Log.Error("AmazonBrowserSession: Failed to get login form!");
                    return;
                }
                // Copy over all input elements
                foreach (var inputElement in loginForm.SelectNodes("//input"))
                {
                    var name = inputElement.Attributes["name"];
                    var value = inputElement.Attributes["value"];
                    if (name != null && value != null)
                        formElements[name.Value] = value.Value;
                }
                if (formElements.ContainsKey("email"))
                {
                    formElements["email"] = username;
                    formElements["password"] = password;
                    formElements["create"] = "0";
                }
                else
                {
                    // 2nd variant of login form
                    formElements["ap_email"] = username;
                    formElements["ap_password"] = password;
                }

                NameValueCollection headers = new NameValueCollection();
                headers["Accept"] = "text/html, application/xhtml+xml, image/jxr, */*";
                headers["Accept-Language"] = "de-DE";
                headers["Cache-Control"] = "no-cache";
                headers["User-Agent"] = UserAgent;

                string postUrl = loginForm.Attributes["action"].Value;
                Thread.Sleep(500);
                string login = GetWebData(postUrl, formElements.AssemblePostPayload(), _cc, Properties.Resources.AmazonLoginUrl, null, false, false, UserAgent, null, headers);
                // TODO add login check
                if (!login.Contains("nav_prime_member_btn"))
                {
                    _isLoggedIn = false;
                    Log.Info("Login at AP failed.");
                    return;
                }
                _isLoggedIn = true;
                _lastLogin = day;
                Log.Info("Login complete");
            }
        }

        /// <summary>
        /// Starts a new thread using STA apartment state (<see cref="ApartmentState.STA"/>). This is required for accessing some windows features like the clipboard.
        /// </summary>
        /// <param name="threadStart">Thread to start.</param>
        public static Thread RunSTAThreaded(ThreadStart threadStart)
        {
            Thread newThread = new Thread(threadStart);
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
            return newThread;
        }

        public CookieContainer GetCookies(string url)
        {
            ManualResetEvent ready = new ManualResetEvent(false);
            CookieContainer cc = null;
            RunSTAThreaded(
                () =>
                {
                    var wnd = new BrowserWindow { NavigateUrl = url };
                    Application.Run(wnd);
                    cc = wnd.CookieContainer;
                    ready.Set();
                });

            ready.WaitOne(10000);
            return cc;
        }
    }

    class BrowserWindow : Form
    {
        public string NavigateUrl { get; set; }
        public CookieContainer CookieContainer { get; private set; }

        public BrowserWindow()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Load += new EventHandler(Window_Load);
        }

        void Window_Load(object sender, EventArgs e)
        {
            WebBrowser wb = new WebBrowser { AllowNavigation = true };
            wb.DocumentCompleted += wb_DocumentCompleted;
            wb.Navigate(NavigateUrl);
        }

        void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser webBrowser = (WebBrowser)sender;
            if (webBrowser.ReadyState != WebBrowserReadyState.Complete)
                return;

            CookieContainer = GetCookieContainer(webBrowser);
            Close();
        }

        public CookieContainer GetCookieContainer(WebBrowser webBrowser)
        {
            CookieContainer container = new CookieContainer();
            // Will change: www.amazon.de to .amazon.de
            var domain = webBrowser.Url.Host.Replace("www", string.Empty);
            foreach (string cookie in webBrowser.Document.Cookie.Split(';'))
            {
                string name = cookie.Split('=')[0];
                string value = cookie.Substring(name.Length + 1);
                string path = "/";
                container.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
            }
            return container;
        }
    }
}
