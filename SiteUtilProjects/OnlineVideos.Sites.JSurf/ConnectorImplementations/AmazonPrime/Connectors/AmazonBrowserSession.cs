using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.JSurf.Extensions;
using OnlineVideos.Sites.JSurf.Properties;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonBrowserSession : BrowserSessionBase
    {
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
                _cc = GetCookies(Resources.AmazonLoginUrl);

                var loginDoc = Load(Resources.AmazonLoginUrl);
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
                //Thread.Sleep(500);
                string login = GetWebData(postUrl, formElements.AssemblePostPayload(), _cc, Resources.AmazonLoginUrl, null, false, false, UserAgent, null, headers);

                var reCustomer = new Regex("\"customerID\":\"([^\"]*)\"");
                var customerMatch = reCustomer.Match(login);
                if (customerMatch.Groups.Count > 1)
                {
                    _customerId = customerMatch.Groups[1].Value;
                    _isLoggedIn = true;
                    _lastLogin = day;
                }
                Log.Info("Login complete");
            }
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
            Regex reCleanUrl = new Regex("(/d.*/).*/(video)");
            streamUrl = reCleanUrl.Replace(streamUrl, "$1$2");

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
            if (!string.IsNullOrEmpty(query))
                query = "&IncludeAll=T&AID=T&" + query;

            string deviceTypeId;
            if (!TypeIDs.TryGetValue(pgMode, out deviceTypeId))
                deviceTypeId = TypeIDs["All"];

            pgMode = pgMode.Split('_').FirstOrDefault();

            if (!pgMode.Contains("/"))
                pgMode = "catalog/" + pgMode;

            string parameter = string.Format("{0}&deviceID={1}&format=json&version={2}&formatVersion=3&marketplaceId={3}", deviceTypeId, _deviceId, version, Resources.AmazonMarketId);
            if (!string.IsNullOrEmpty(siteId))
                parameter += "&id=" + siteId;

            string jsondata = GetWebData(string.Format("{0}/cdp/{1}?{2}{3}", Resources.AmazonATVUrl, pgMode, parameter, query), cookies: useCookie, cache: false, userAgent: UserAgent);
            if (string.IsNullOrEmpty(jsondata))
                return null; // false?

            var response = JObject.Parse(jsondata);

            if (response.SelectToken("message.statusCode").ToString() != "SUCCESS")
            {
                Log.Warn("Error Code: " + (string)response.SelectToken("message.body.code"));
                return string.Empty;
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
            if (!string.IsNullOrEmpty(data))
            {
                string error = Regex.Match(data, "{[^\"]*\"errorCode[^}]*}").Value;
                if (!string.IsNullOrEmpty(error))
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
