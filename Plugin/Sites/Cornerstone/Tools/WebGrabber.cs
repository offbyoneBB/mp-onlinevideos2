using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;


namespace OnlineVideos.Sites.Cornerstone.Tools
{

    public class WebGrabber {

        #region Private variables

      private static LogProvider logger = new LogProvider();
        private static int unsafeHeaderUserCount;
        private static object lockingObj;
        private string requestUrl;

        #endregion

        #region Ctor

        static WebGrabber() {
            unsafeHeaderUserCount = 0;
            lockingObj = new object();
        }

        public WebGrabber(string url) {
            requestUrl = url;
            request = (HttpWebRequest)WebRequest.Create(requestUrl);
        }

        public WebGrabber(Uri uri) {
            requestUrl = uri.OriginalString;
            request = (HttpWebRequest)WebRequest.Create(uri);
        }

        ~WebGrabber() {
            request = null;
            if (response != null) {
                response.Close();
                response = null;
            }
        }

        #endregion

        #region Public properties

        public HttpWebRequest Request {
            get { return request; }
        } private HttpWebRequest request;

        public HttpWebResponse Response {
            get { return response; }
        } private HttpWebResponse response;

        public Encoding Encoding {
            get { return encoding; }
            set { encoding = value; }
        } private Encoding encoding;

        public int MaxRetries {
            get { return maxRetries; }
            set { maxRetries = value; }
        } private int maxRetries = 3;

        public int Timeout {
            get { return timeout; }
            set { timeout = value; }
        } private int timeout = 5000;

        public int TimeoutIncrement {
            get { return timeoutIncrement; }
            set { timeoutIncrement = value; }
        } private int timeoutIncrement = 1000;

        public string UserAgent {
            get { return userAgent; }
            set { userAgent = value; }
        } private string userAgent = "Cornerstone/" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public string CookieHeader {
            get { return cookieHeader; }
            set { cookieHeader = value; }
        } private string cookieHeader;

        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        } private bool _debug = false;

        public bool AllowUnsafeHeader {
            get { return _allowUnsafeHeader; }
            set { _allowUnsafeHeader = value; }
        } private bool _allowUnsafeHeader = false;

        #endregion

        #region Public methods

        public bool GetResponse() {
            bool completed = false;
            int tryCount = 0;

            // enable unsafe header parsing if needed
            if (_allowUnsafeHeader) SetAllowUnsafeHeaderParsing(true);

            // setup some request properties
            request.Proxy = WebRequest.DefaultWebProxy;
            request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();

            while (!completed) {
                tryCount++;

                request.Timeout = timeout + (timeoutIncrement * tryCount);
                if (cookieHeader != null)
                    request.CookieContainer.SetCookies(request.RequestUri, cookieHeader);

                try {
                    response = (HttpWebResponse)request.GetResponse();
                    completed = true;
                }
                catch (WebException e) {

                    // Skip retry logic on protocol errors
                    if (e.Status == WebExceptionStatus.ProtocolError) {
                        HttpStatusCode statusCode = ((HttpWebResponse)e.Response).StatusCode;
                        switch (statusCode) {
                            // Currently the only exception is the service temporarily unavailable status
                            // So keep retrying when this is the case
                            case HttpStatusCode.ServiceUnavailable:
                                break;
                            // all other status codes mostly indicate problems that won't be
                            // solved within the retry period so fail these immediatly
                            default:
                                logger.Error(string.Format("Connection failed: URL={0}, Status={1}, Description={2}.", requestUrl, statusCode, ((HttpWebResponse)e.Response).StatusDescription));
                                return false;
                        }
                    }

                    // Return when hitting maximum retries.
                    if (tryCount == maxRetries) {
                        logger.Warn("Connection failed: Reached retry limit of " + maxRetries + ". URL=" + requestUrl);
                        return false;
                    }

                    // If we did not experience a timeout but some other error
                    // use the timeout value as a pause between retries
                    if (e.Status != WebExceptionStatus.Timeout) {
                        Thread.Sleep(timeout + (timeoutIncrement * tryCount));
                    }
                }
                catch (NotSupportedException e) {
                    logger.Error("Connection failed.", e);
                    return false;
                }
                catch (ProtocolViolationException e) {
                    logger.Error("Connection failed.", e);
                    return false;
                }
                catch (InvalidOperationException e) {
                    logger.Error("Connection failed.", e);
                    return false;
                }
                finally {
                    // disable unsafe header parsing if it was enabled
                    if (_allowUnsafeHeader) SetAllowUnsafeHeaderParsing(false);
                }
            }

            // persist the cookie header
            cookieHeader = request.CookieContainer.GetCookieHeader(request.RequestUri);

            // Debug
            if (_debug) logger.Debug(string.Format("GetResponse: URL={0}, UserAgent={1}, CookieHeader={2}", requestUrl, userAgent, cookieHeader));

            // disable unsafe header parsing if it was enabled
            if (_allowUnsafeHeader) SetAllowUnsafeHeaderParsing(false);

            return true;
        }

        public string GetString() {
            // If encoding was not set manually try to detect it
            if (encoding == null) {
                try {
                    // Try to get the encoding using the characterset
                    encoding = Encoding.GetEncoding(response.CharacterSet);
                }
                catch (Exception e) {
                    // If this fails default to the system's default encoding
                    logger.Debug("Encoding could not be determined, using default.", e);
                    encoding = Encoding.Default;
                }
            }

            // Debug
            if (_debug) logger.Debug("GetString: Encoding={2}", encoding.EncodingName);

            // Converts the stream to a string
            try {
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, encoding, true);
                string data = reader.ReadToEnd();
                reader.Close();
                stream.Close();
                response.Close();

                // return the string data
                return data;
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                // There was an error reading the stream
                // todo: might have to retry
                logger.ErrorException("Error while trying to read stream data: ", e);
            }

            // return nothing.
            return null;
        }

        public XmlNodeList GetXML() {
            return GetXML(null);
        }

        public XmlNodeList GetXML(string rootNode) {
            string data = GetString();
            
            // if there's no data return nothing
            if (String.IsNullOrEmpty(data))
                return null;

            XmlDocument xml = new XmlDocument();

            // attempts to convert data into an XmlDocument
            try {
                xml.LoadXml(data);
            }
            catch (XmlException e) {
                logger.ErrorException("XML Parse error: URL=" + requestUrl, e);
                return null;
            }
            
            // get the document root
            XmlElement xmlRoot = xml.DocumentElement;
            if (xmlRoot == null)
                return null;

            // if a root node name is given check for it
            // return null when the root name doesn't match
            if (rootNode != null && xmlRoot.Name != rootNode)
                return null;

            // return the node list
            return xmlRoot.ChildNodes;

        }

        #endregion

        #region Private methods

        //Method to change the AllowUnsafeHeaderParsing property of HttpWebRequest.
        private bool SetAllowUnsafeHeaderParsing(bool setState) {
            try {
                lock (lockingObj) {
                    // update our counter of the number of requests needing 
                    // unsafe header processing
                    if (setState == true) unsafeHeaderUserCount++;
                    else unsafeHeaderUserCount--;

                    // if there was already a request using unsafe heaser processing, we
                    // dont need to take any action.
                    if (unsafeHeaderUserCount > 1)
                        return true;

                    // if the request tried to turn off unsafe header processing but it is
                    // still needed by another request, we should wait.
                    if (unsafeHeaderUserCount >= 1 && setState == false)
                        return true;

                    //Get the assembly that contains the internal class
                    Assembly aNetAssembly = Assembly.GetAssembly(typeof(System.Net.Configuration.SettingsSection));
                    if (aNetAssembly == null)
                        return false;

                    //Use the assembly in order to get the internal type for the internal class
                    Type aSettingsType = aNetAssembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                    if (aSettingsType == null)
                        return false;

                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created allready the property will create it for us.
                    object anInstance = aSettingsType.InvokeMember("Section",
                                                                    BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic,
                                                                    null, null, new object[] { });
                    if (anInstance == null)
                        return false;

                    //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                    FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (aUseUnsafeHeaderParsing == null)
                        return false;

                    // and finally set our setting
                    aUseUnsafeHeaderParsing.SetValue(anInstance, setState);
                    return true;
                }

            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Unsafe header parsing setting change failed.");
                return false;
            }
        }

        #endregion
    }
}
