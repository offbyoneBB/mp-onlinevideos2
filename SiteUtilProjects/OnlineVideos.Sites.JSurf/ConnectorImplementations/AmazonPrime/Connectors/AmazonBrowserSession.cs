using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Extensions;
using OnlineVideos.Sites.JSurf.Extensions;
using OnlineVideos.Sites.JSurf.Properties;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonBrowserSession : BrowserSessionBase
    {
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);

        private Boolean _isLoggedIn;
        private long _lastLogin;
        private string _lastCulture;
        private string _userAgent = "Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko";

        private string _deviceId;
        private string _customerId;

        public AmazonBrowserSession()
        {
            Log.Info("New Browser session");
            _isLoggedIn = false;
            _lastLogin = 0;
            _lastCulture = "";
            UserAgent = _userAgent;

            _deviceId = _userAgent.GenId();
        }

        private static string CookieCacheFile => Path.Combine(Path.GetTempPath(), typeof(AmazonBrowserSession).GUID.ToString());

        internal static CookieContainer GetSavedCookies()
        {
            var cacheFile = CookieCacheFile;
            if (File.Exists(cacheFile))
                return File.ReadAllText(cacheFile).Deserialize();
            return null;
        }
        internal static void SaveCookies(CookieContainer cookieContainer)
        {
            if (cookieContainer != null)
                File.WriteAllText(CookieCacheFile, cookieContainer.Serialize());
        }

        internal static void ApplySavedCookies(CookieContainer cookieContainer, string url)
        {
            if (cookieContainer != null && cookieContainer.Count > 0)
            {
                var uri = new Uri(url);
                var cookie = cookieContainer.GetCookieHeader(uri);
                InternetSetCookie(uri.Host, null, cookie.ToString());
            }
        }

        public void Login(string username, string password, bool forceLogin = false)
        {
            string culture = Resources.Culture.ToString();
            long day = DateTime.Now.Ticks / TimeSpan.TicksPerDay;

            if (day > _lastLogin || culture != _lastCulture)
            {
                forceLogin = true;
            }

            if (!_isLoggedIn || forceLogin)
            {
                Log.Info("Login to Amazon " + Resources.AmazonRootUrl);
                _isLoggedIn = false;
                _lastCulture = culture;

                // Load the main page inside a WebBrowser control to retrieve all dynamically created cookies (session ids by JS)
                string afterLoginPage = String.Empty;
                _cc = GetSavedCookies();
                if (_cc != null)
                {
                    // Test if cookies are still valid
                    _cc = GetPage(Resources.AmazonRootUrl, out afterLoginPage);
                    if (FindCustomerId(afterLoginPage, day)) return;
                }

                _cc = GetPageAndCookies(Resources.AmazonLoginUrl, username, password, false, out afterLoginPage);
                SaveCookies(_cc);

                //var loginDoc = Load(Resources.AmazonLoginUrl);

                //string firstCorrectPostUrl = null;
                //for (var retry = 0; retry < 3; retry++)
                //{
                //    if (retry > 0) Thread.Sleep(retry * 1000);
                //    // There can appear different login pages, using other names for form / controls
                //    var loginForm = loginDoc.GetElementbyId("ap_signin_form") ??
                //                    loginDoc.DocumentNode.SelectNodes("//*[@name='signIn']").FirstOrDefault();
                //    if (loginForm == null)
                //    {
                //        Log.Error("AmazonBrowserSession: Failed to get login form!");
                //        return;
                //    }
                //    var formElements = new FormElementCollection(loginForm);

                //    // Copy over all input elements
                //    foreach (var inputElement in loginForm.SelectNodes("//input"))
                //    {
                //        var name = inputElement.Attributes["name"];
                //        var value = inputElement.Attributes["value"];
                //        if (name != null && value != null)
                //            formElements[name.Value] = value.Value;
                //    }
                //    if (formElements.ContainsKey("email"))
                //    {
                //        formElements["email"] = username;
                //        formElements["password"] = password;
                //        formElements["create"] = "0";
                //    }
                //    else
                //    {
                //        // 2nd variant of login form
                //        formElements["ap_email"] = username;
                //        formElements["ap_password"] = password;
                //    }

                //    NameValueCollection headers = new NameValueCollection();
                //    headers["Accept"] = "text/html, application/xhtml+xml, image/jxr, */*";
                //    headers["Accept-Language"] = "de-DE";
                //    headers["Cache-Control"] = "no-cache";
                //    headers["User-Agent"] = UserAgent;

                //    string postUrl = loginForm.Attributes["action"].Value;
                //    // &#x2F;
                //    if (postUrl.Contains("&#x"))
                //        postUrl = WebUtility.HtmlDecode(postUrl);

                //    if (postUrl.StartsWith("https://"))
                //        firstCorrectPostUrl = postUrl;
                //    else
                //        postUrl = firstCorrectPostUrl;

                //    if (postUrl == null)
                //        break;

                //    Thread.Sleep(500);
                //    string login = GetWebData(postUrl, formElements.AssemblePostPayload(), _cc, Resources.AmazonLoginUrl, null, false, false, UserAgent, null, headers);

                if (FindCustomerId(afterLoginPage, day)) return;
                //if (!_isLoggedIn)
                //{
                //    // Login not finished yet, i.e. because the new code sending happens and a new login form appears
                //    loginDoc.LoadHtml(login);
                //}
                //}
                Log.Info("Login complete");
            }
        }

        private bool FindCustomerId(string afterLoginPage, long day)
        {
            var reCustomer = new[]
            {
                new Regex("\"customerID\":\"([^\"]*)\""),
                new Regex("custId=([^&]*)")
            };
            foreach (Regex regex in reCustomer)
            {
                var customerMatch = regex.Match(afterLoginPage);
                if (customerMatch.Groups.Count > 1)
                {
                    _customerId = customerMatch.Groups[1].Value;
                    _isLoggedIn = true;
                    _lastLogin = day;
                    return true;
                }
            }

            return false;
        }

        public bool GetInputStreamProperties(string asin, out string streamUrl, out string licUrl, out Dictionary<string, string> additionalTags)
        {
            //var content = GetATVDataJSON("GetASINDetails", "ASINList=" + asin);
            //if (content == null)
            //{
            //    streamUrl = licUrl = null;
            //    return false;
            //}

            string vMT = "Feature";
            Dictionary<string, string> values = GetFlashVars(asin);

            var urldata = GetUrldata("catalog/GetPlaybackResources", values, extra: true, vMT: vMT, opt: "&titleDecorationScheme=primary-content");
            JObject playbackData = JObject.Parse(urldata.Data);

            streamUrl = playbackData.SelectToken("audioVideoUrls.avCdnUrlSets[0].avUrlInfoList[0].url").Value<string>(); // Cloudfront

            // Fix for missing DD+ audio streams, remove DeviceTypeID(?) part in url
            Regex reCleanUrl1 = new Regex("~");
            Regex reCleanUrl2 = new Regex("/[1-9][$].*?/");
            var cleaned = reCleanUrl1.Replace(streamUrl, "");
            if (String.Equals(cleaned, streamUrl))
                cleaned = reCleanUrl2.Replace(streamUrl, "/");

            streamUrl = cleaned;

            additionalTags = new Dictionary<string, string>();
            for (int s = 0; ; s++)
            {
                //{"displayName":"German (Germany)","format":"DFXP","index":"0","languageCode":"de-DE","subtype":"dialog","type":"subtitle","url":"http://....dfxp","videoMaterialType":"Feature"},
                var subtitleNode = playbackData.SelectToken("subtitleUrls[" + s + "]");
                if (subtitleNode == null)
                    break;

                string subtitlePath;
                string lang = subtitleNode.SelectToken("languageCode").Value<string>();
                string displayName = subtitleNode.SelectToken("displayName").Value<string>();
                string value = subtitleNode.SelectToken("url").Value<string>();
                string fakeFilename;
                if (DownloadAndConvertSubtitle(asin, displayName, value, out fakeFilename, out subtitlePath))
                {
                    additionalTags["subtitle_" + lang] = subtitlePath;
                    additionalTags["fakefilename"] = fakeFilename;
                }
            }

            licUrl = GetUrldata("catalog/GetPlaybackResources", values, extra: true, vMT: vMT, dRes: "Widevine2License", retURL: true).Url;
            return streamUrl != null && licUrl != null;
        }

        private bool DownloadAndConvertSubtitle(string asin, string displayName, string url, out string fakeFilename, out string subtitlePath)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), asin);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                fakeFilename = Path.Combine(path, asin + ".mkv");
                subtitlePath = Path.Combine(path, asin + "." + displayName + ".srt");

                // Use cached file if already exists
                if (File.Exists(subtitlePath))
                    return true;

                string doc = GetWebData(url, cookies: _cc, userAgent: UserAgent);
                Regex re = new Regex("<tt:p begin=\"(?<begin>[^\\\"]*)\" end=\"(?<end>[^\\\"]*)\">(?<text>.*)<\\/tt:p>", RegexOptions.Multiline);
                var matches = re.Matches(doc);
                int num = 1;

                using (var srt = new FileStream(subtitlePath, FileMode.Create))
                using (var sw = new StreamWriter(srt))
                    foreach (Match match in matches)
                    {
                        string begin = match.Groups["begin"].Value;
                        string end = match.Groups["end"].Value;
                        string text = match.Groups["text"].Value.Replace("<tt:br/>", "\r\n").Replace("<tt:br>", "\r\n").Replace("</tt:br>", "");
                        sw.Write("{0}\n{1} --> {2}\n{3}\n\n", num++, begin, end, text);
                    }
                return true;
            }
            catch (Exception)
            {
                subtitlePath = fakeFilename = null;
                return false;
            }
        }

        private Dictionary<string, string> GetFlashVars(string asin)
        {
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"asin", asin},
                {"deviceTypeID", "AOAGZA014O5RE"},
                {"userAgent", UserAgent},
                {"customerId", _customerId}
              };

            var r = new Random((int)DateTime.Now.Ticks);
            string rand = "onWebToken_" + r.Next(0, 484);

            var pltoken = GetWebData(Resources.AmazonRootUrl + "/gp/video/streaming/player-token.json?callback=" + rand, cookies: _cc, cache: false, userAgent: UserAgent);

            var reToken = new Regex("\"token\":\"([^\"]*)\"");
            var matchToken = reToken.Match(pltoken);
            if (matchToken.Groups.Count > 1)
                values["token"] = matchToken.Groups[1].Value;

            return values;
        }

        protected Dictionary<string, string> TypeIDs = new Dictionary<string, string>
        {
                {"All", "firmware=fmw:17-app:2.0.45.1210&deviceTypeID=A2M4YX06LWP8WI"},
                {"GetCategoryList_ftv", "firmware=fmw:17-app:2.0.45.1210&deviceTypeID=A12GXV8XMS007S"}
            };

        protected JToken GetATVDataJSON(string pgMode, string query = "", int version = 2, CookieContainer useCookie = null, string siteId = null)
        {
            if (query.Contains("?"))
                query = query.Split('?')[1];
            if (!String.IsNullOrEmpty(query))
                query = "&IncludeAll=T&AID=T&" + query;

            string deviceTypeId;
            if (!TypeIDs.TryGetValue(pgMode, out deviceTypeId))
                deviceTypeId = TypeIDs["All"];

            pgMode = pgMode.Split('_').FirstOrDefault();

            if (!pgMode.Contains("/"))
                pgMode = "catalog/" + pgMode;

            string parameter = String.Format("{0}&deviceID={1}&format=json&version={2}&formatVersion=3&marketplaceId={3}", deviceTypeId, _deviceId, version, Resources.AmazonMarketId);
            if (!String.IsNullOrEmpty(siteId))
                parameter += "&id=" + siteId;

            string jsondata = GetWebData(String.Format("{0}/cdp/{1}?{2}{3}", Resources.AmazonATVUrl, pgMode, parameter, query), cookies: useCookie, cache: false, userAgent: UserAgent);
            if (String.IsNullOrEmpty(jsondata))
                return null; // false?

            var response = JObject.Parse(jsondata);

            if (response.SelectToken("message.statusCode").ToString() != "SUCCESS")
            {
                Log.Warn("Error Code: " + (string)response.SelectToken("message.body.code"));
                return String.Empty;
            }
            return response.SelectToken("message.body");
        }

        public struct Result
        {
            public string Url;
            public bool Success;
            public string Data;
        }

        public Result GetUrldata(string mode, Dictionary<string, string> values, string retformat = "json", string devicetypeid = null, int version = 1, string firmware = "1", string opt = "",
          bool extra = false, CookieContainer useCookie = null, bool retURL = false, string vMT = "Feature", string dRes = "AudioVideoUrls%2CSubtitleUrls")
        {
            if (devicetypeid == null)
                devicetypeid = values["deviceTypeID"];
            string url = Resources.AmazonATVUrl + "/cdp/" + mode
                         + "?asin=" + values["asin"]
                         + "&deviceTypeID=" + devicetypeid
                         + "&firmware=" + firmware
                         + "&customerID=" + values["customerId"]
                         + "&deviceID=" + _deviceId
                         + "&marketplaceID=" + Resources.AmazonMarketId
                         + "&token=" + values["token"]
                         + "&format=" + retformat
                         + "&version=" + version
                         + opt;
            if (extra)
                url += "&resourceUsage=ImmediateConsumption&consumptionType=Streaming&deviceDrmOverride=CENC"
                       + "&deviceStreamingTechnologyOverride=DASH&deviceProtocolOverride=Http&audioTrackId=all"
                       + "&videoMaterialType=" + vMT
                       + "&desiredResources=" + dRes;
            if (retURL)
                return new Result { Url = url, Success = true };

            string data = GetWebData(url, cookies: useCookie, userAgent: UserAgent, referer: Resources.AmazonRootUrl, cache: false);
            if (!String.IsNullOrEmpty(data))
            {
                string error = Regex.Match(data, "{[^\"]*\"errorCode[^}]*}").Value;
                if (!String.IsNullOrEmpty(error))
                    return new Result { Success = false, Data = error };

                return new Result { Success = true, Data = data };
            }
            return new Result { Success = false, Data = "HTTP Error" };
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

        public CookieContainer GetPage(string url, out string afterLoginPage)
        {
            return GetPageAndCookies(url, null, null, true, out afterLoginPage);
        }

        public CookieContainer GetPageAndCookies(string url, string username, string password, bool noFormSubmit, out string afterLoginPage, CookieContainer cc = null)
        {
            ManualResetEvent ready = new ManualResetEvent(false);
            string pageContent = null;
            RunSTAThreaded(
                () =>
                {
                    var wnd = new BrowserWindow { Username = username, Password = password, NavigateUrl = url, CookieContainer = cc, NoFormSubmit = noFormSubmit };
                    Application.Run(wnd);
                    cc = wnd.CookieContainer;
                    pageContent = wnd.AfterLoginPage;
                    ready.Set();
                });

            ready.WaitOne(30000);
            afterLoginPage = pageContent;
            return cc;
        }
    }

    class BrowserWindow : Form
    {
        /// <summary>
        /// If <c>>true</c>, no form will be submitted. This can be used to return only the loaded page content.
        /// </summary>
        public bool NoFormSubmit { get; set; }
        public string NavigateUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AfterLoginPage { get; private set; }

        public CookieContainer CookieContainer { get; set; }

        public BrowserWindow()
        {
            ShowInTaskbar = true;
#if DEBUG
            WindowState = FormWindowState.Normal;
#else
            WindowState = FormWindowState.Minimized;
#endif

            Load += new EventHandler(Window_Load);
        }

        void Window_Load(object sender, EventArgs e)
        {
            WebBrowser wb = new WebBrowser
            {
                AllowNavigation = true,
                ScriptErrorsSuppressed = true,
                ScrollBarsEnabled = true,
                Dock = DockStyle.Fill,
            };

            AmazonBrowserSession.ApplySavedCookies(CookieContainer, NavigateUrl);
            Controls.Add(wb);
            wb.DocumentCompleted += wb_DocumentCompleted;
            wb.Navigate(NavigateUrl);
        }

        void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser webBrowser = (WebBrowser)sender;
            if (webBrowser.ReadyState != WebBrowserReadyState.Complete)
                return;

            if (!NoFormSubmit)
            {
                foreach (HtmlElement form in webBrowser.Document.Forms)
                {
                    if (form.Id == "ap_signin_form" || form.Name == "signIn")
                    {
                        if (HasCaptcha(form))
                        {
                            // Wait for interactive input
                            FillFormUserDetails(form, false);
                            Size = new Size(500, 800);
                            WindowState = FormWindowState.Normal;
                            CenterToParent();
                            BringToFront();
                            Activate();
                            return;
                        }

                        FillFormUserDetails(form, true);
                        return; // Next round
                    }
                }
            }

            // No login form anymore, we are in.
            CookieContainer = GetCookieContainer(webBrowser);
            AfterLoginPage = webBrowser.Document.Body.OuterHtml;
            Close();
        }

        private bool HasCaptcha(HtmlElement form)
        {
            return FindRecursive(form, (e) => e.Id == "use_image_captcha") != null;
        }

        private void FillFormUserDetails(HtmlElement form, bool submit)
        {
            Func<HtmlElement, bool> findEmail = e => e.Id == "email" || e.Id == "ap_email" || e.Id == "ap-claim";
            Func<HtmlElement, bool> findPasswd = e => e.Id == "password" || e.Id == "ap_password";
            Func<HtmlElement, bool> findRemember = e => e.Id == "rememberMe";

            var emailElem = FindRecursive(form, findEmail);
            var passwordElem = FindRecursive(form, findPasswd);
            var rememberElem = FindRecursive(form, findRemember);
            if (emailElem != null)
                emailElem.SetAttribute("value", Username);
            if (passwordElem != null)
                passwordElem.SetAttribute("value", Password);
            if (rememberElem != null)
                rememberElem.SetAttribute("checked", "true");
            if (submit)
                form.InvokeMember("submit");
        }

        private HtmlElement FindRecursive(HtmlElement elem, Func<HtmlElement, bool> filter)
        {
            var foundElem = elem.Children.Cast<HtmlElement>().FirstOrDefault(filter);
            if (foundElem != null)
                return foundElem;
            foreach (HtmlElement subElement in elem.Children)
            {
                foundElem = FindRecursive(subElement, filter);
                if (foundElem != null)
                    return foundElem;
            }
            return null;
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
