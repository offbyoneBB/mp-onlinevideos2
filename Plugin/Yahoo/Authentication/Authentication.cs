using System;
using System.Text;
using System.Diagnostics;
using OnlineVideos.Properties;

namespace Yahoo
{
    #region Public enums

    /// <summary>
    /// Defines error codes for the the <see cref="Yahoo.AuthenticationException">AuthenticationException</see> class.
    /// </summary>
    [Serializable]
    public enum AuthenticationErrorCode
    {
        /// <summary>
        /// A network error occurred.
        /// </summary>
        NetworkError = -2,

        /// <summary>
        /// An error occurred while parsing data or invalid input encountered. See the exception Message for details.
        /// </summary>
        Other = -1,

        /// <summary>
        /// The Yahoo! authentication system returned an unknown error code.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The token is expired. The user must reauthorize the application. Direct the user to the Yahoo! login screen and retrieve a new token.
        /// </summary>
        TokenExpired = 1000,

        /// <summary>
        /// The token is invalid. Verify that you are using the token that was returned for the user when he or she last logged in.
        /// </summary>
        TokenInvalid = 2001,

        /// <summary>
        /// The application's request used plain HTTP. Authenticated calls require HTTPS.
        /// </summary>
        HttpsRequired = 2002,

        /// <summary>
        /// The signature (sig) is invalid. Verify that you are generating the signature correctly.
        /// </summary>
        SignatureInvalid = 2003,

        /// <summary>
        /// The timestamp (ts) is invalid. The timestamp may be out of sync with the Yahoo! login server clocks by at most ±600 seconds. To avoid clock skew issues, run ntpd.
        /// </summary>
        TimestampInvalid = 2004,

        /// <summary>
        /// The application ID (appid) is invalid. Verify that you are using your application's proper application ID.
        /// </summary>
        ApplicationIdInvalid = 3000,

        /// <summary>
        /// The Yahoo! login server is temporarily unable to service your request. Try again later.
        /// </summary>
        ServiceUnavailable = 9000
    }

    #endregion

    /// <summary>
    /// Authenticates users using Yahoo!'s Browser-Based Authentication and maintains user credentials and signed in state.
    /// <seealso cref="Yahoo.AuthenticationException"/>
	/// </summary>
	/// <remarks>
	/// <p>Users must be requested to give your application permission to access their data. This is done by redirecting the
	/// user's browser to the user sign in page, which can be retrieved with <see cref="GetUserLogOnAddress()"/>, 
	/// where they will need to sign into their Yahoo! account, approve access by your application 
	/// and accept the required legal terms. Once this is complete, the user's browser will be redirected to
	/// your endpoint address with a "token" that represents the user and is valid for two weeks. You should store this cookie
	/// along with information on how long the token is valid to avoid unnecessary redirects to the user sign in page.</p>
	///  
	/// <p>Once the user has signed in and you have a valid token from a previous call you can skip directly to
	/// updating the user's credentials. Calling <see cref="UpdateCredentials">UpdateCredentials()</see> will retrieve the WSSID and cookie 
	/// that are needed for authenticated web service calls. You should check <see cref="IsCredentialed">IsCredentialed</see> to see
	/// if you have current credentials before calling <c>UpdateCredentials()</c> or making authenticated web service calls.
	/// This step should be totally transparent to the end user.</p>
	/// 
	/// <p>To make authenticated web service calls, you can use <see cref="GetAuthenticatedServiceDataSet">GetAuthenticatedServiceDataSet()</see>, 
	/// <see cref="GetAuthenticatedServiceStream">GetAuthenticatedServiceStream()</see>, 
	/// <see cref="GetAuthenticatedServiceString">GetAuthenticatedServiceString()</see> or manually call a web service. 
	/// To call authenticated web services you will need to construct the URL, sign it and attach the cookies to the request.</p>
	/// </remarks>
	/// <example>To use the Authentication class you need to instantiate it with your application Id and shared secret you
	/// obtained when you registered your application.
	/// 
	/// The following example creates a new Authentication object in Global.asax
	/// for each new user session.
	/// <code>
	/// protected void Session_Start(Object sender, EventArgs e)
	///	{
	///		Yahoo.Authentication auth = new Yahoo.Authentication("MyApplicationId", "MySharedSecret");
	///		Session["Auth"] = auth;
	/// }
	/// </code>
	/// To sign in the user, redirect their browser to the user sign in page.
	/// <code>
	/// Response.Redirect(auth.GetUserLogOnAddress().ToString())
	/// </code>
	/// Once the user has successfully signed in and agreed to the permissions and terms they will be redirected to your end point page.
	/// Here you need to make sure the call is valid and coming from Yahoo! and save the token.
	/// <code>
	/// if(Request.QueryString["token"] != null &amp;&amp; Request.QueryString["token"].Length > 0)
	/// {	
	///		if(auth.IsValidSignedUrl(Request.Url) == true)
	///		{
	///			// Given token to Authentication class
	///			auth.Token = Request.QueryString["token"];
	///
	///			// TODO: Save the token and its timeout date in a secure persistent store.
	///
	///			// Attempt to retrieve user credentials
	///			auth.UpdateCredentials();
	///		}
	///		else
	///		{
	///			// TODO: Show error
	///		}
	///	}
	/// </code>
	/// Once you have the user credentials you can start making authenticated web service calls.
	/// <code>
	/// if(auth.IsCredentialed == true)
	/// {
	///		System.Data.DataSet dsServices;
	///		dsServices = auth.GetAuthenticatedServiceDataSet(new System.Uri("http://photos.yahooapis.com/V1.0/listServices"));
	///		
	///		// TODO: Work your magic with the returned data.
	/// }
	/// else
	/// {
	///		// TODO: If the token has expired, redirect to user sign in page.
	///		// TODO: If the token should be valid, call UpdateCredentials() and try again.
	/// }
	/// </code>
	/// </example>
	public class Authentication
    {

        #region Public constants 

        /// <summary>
        /// Maximum difference in seconds the timestamp is allowed to be off.
        /// </summary>
        public const int MaximumClockSkew = 600;

        #endregion

        #region Private fields

        private Uri _wsUserLogOnUrl = new System.Uri("https://api.login.yahoo.com/WSLogin/V1/wslogin");
		private Uri _wsTokenLogOnUrl = new System.Uri("https://api.login.yahoo.com/WSLogin/V1/wspwtoken_login");

		private string _applicationId = "";
		private string _sharedSecret = "";

		private string _cookies = "";
		private string _wssId = "";
		private DateTime _validUntil = DateTime.MinValue;
		private string _token = "";
		private System.Net.IWebProxy _proxy = null;
        private int _httpTimeout = 10 * 1000;
		private string _userAgent = "BBAuth .NET";

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="applicationId">Application ID.</param>
		/// <param name="sharedSecret">Shared secret.</param>
        /// <exception cref="ArgumentNullException">ApplicationId or sharedSecret is null (Nothing in VB) or an empty string.</exception>
		public Authentication(string applicationId, string sharedSecret)
		{
            if (applicationId == null || applicationId.Length == 0)
            {
                throw new ArgumentNullException("applicationId");
            }
            else if (sharedSecret == null || sharedSecret.Length == 0)
            {
                throw new ArgumentNullException("sharedSecret");
            }

            _applicationId = applicationId;
			_sharedSecret = sharedSecret;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets the application ID.
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public string ApplicationId
		{
			get { return _applicationId; }
			set
            {
                if (value != null && value.Length != 0)
                {
                    _applicationId = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
		}

		/// <summary>
		/// Gets or sets the authentication cookie(s).
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public string Cookies
		{
			get { return _cookies; }
            set
            {
                if (value != null && value.Length != 0)
                {
                    _cookies = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
		}

		/// <summary>
		/// Gets or sets the user credential fetch timeout in milliseconds.
		/// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than zero and is not System.Threading.Timeout.Infinite.</exception>
        public int HttpTimeout
		{
			get { return _httpTimeout; }
			set
            {
                if (value < 0 && value != System.Threading.Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException("value", Resources.ErrorTimeoutOutOfRange);
                }
                else
                {
                    _httpTimeout = value;
                }
            }
		}

		/// <summary>
		/// Gets a value describing whether we should have valid authentication details required to make web service calls. 
		/// You should check this before trying to call <see cref="UpdateCredentials"/> to avoid unnecessary token login calls.
		/// </summary>
		/// <returns>A value determining whether the Token, WssId and Cookies are valid and ValidUntil has not passed.</returns>
		public bool IsCredentialed
		{
			get 
			{
				bool result = false;

				if (_token.Length != 0 && _wssId.Length != 0 && _cookies.Length != 0 && DateTime.Now < _validUntil)
				{
					result = true;
				}

				return result;
			}
		}

		/// <summary>
		/// Gets or sets the proxy used by web requests.
		/// </summary>
		public System.Net.IWebProxy Proxy
		{
			get { return _proxy; }
			set { _proxy = value; }
		}

		/// <summary>
		/// Gets or sets the shared secret.
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public string SharedSecret
		{
			get { return _sharedSecret; }
			set
            {
                if (value != null && value.Length != 0)
                {
                    _sharedSecret = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
		}

		/// <summary>
		/// Gets or sets the token returned by the Yahoo! login server.
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB).</exception>
        public string Token
		{
			get { return _token; }
			set
            {
                if (value != null)
                {
                    // We allow empty strings
                    _token = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
		}

		/// <summary>
		/// Gets or sets the user agent string to use during web requests.
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public string UserAgent
		{
			get { return _userAgent; }
			set
            {
                if (value != null && value.Length != 0)
                {
                    _userAgent = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
		}

		/// <summary>
		/// Gets or sets the date and time when the cookie and WSSID will no longer be valid.
		/// </summary>
        public DateTime ValidUntil
		{
			get { return _validUntil; }
            set { _validUntil = value; }
        }

		/// <summary>
		/// Gets or sets the WSSID.
		/// </summary>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public string WssId
		{
			get { return _wssId; }
            set
            {
                if (value != null && value.Length != 0)
                {
                    _wssId = value;
                }
                else
                {
                    throw new ArgumentNullException("value");
                }
            }
        }

		#endregion

        #region Public static methods

        /// <summary>
        /// Creates an MD5 hash of the input string as a 32 character hexadecimal string.
        /// </summary>
        /// <param name="input">Text to generate has for.</param>
        /// <returns>Hash as 32 character hexadecimal string.</returns>
        public static string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5Hasher;
            byte[] data;
            int count;
            StringBuilder result;

            md5Hasher = System.Security.Cryptography.MD5.Create();
            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.
            result = new StringBuilder();
            for (count = 0; count < data.Length; count++)
            {
                result.Append(data[count].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns the number of seconds since January 1st, 1970 for the current date and time.
        /// </summary>
        /// <returns>Number of seconds since January 1st, 1970.</returns>
        public static long GetUnixTime()
        {
            return GetUnixTime(DateTime.UtcNow);
        }

        /// <summary>
        /// Returns the number of seconds since January 1st, 1970 from the given date and time.
        /// </summary>
        /// <param name="dateTimeUtc">Date and time to convert into Unix time in UTC.</param>
        /// <returns>Number of seconds since January 1st, 1970 has elapsed since the given date and time.</returns>
        public static long GetUnixTime(DateTime dateTimeUtc)
        {
            TimeSpan timestamp;

            // Get seconds since Jannuary 1, 1970 GMT
            timestamp = dateTimeUtc - new DateTime(1970, 1, 1);

            return (long)Math.Floor(timestamp.TotalSeconds);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Makes an authenticated web service call that returns XML data and returns the result as a dataset.
        /// </summary>
        /// <param name="wsAddress">Address of web service with parameters to call.</param>
        /// <returns></returns>
        public System.Data.DataSet GetAuthenticatedServiceDataSet(System.Uri wsAddress)
        {
            System.Data.DataSet result = null;

            using (System.IO.Stream dataStream = GetAuthenticatedServiceStream(wsAddress))
            {
                // Read result into a dataset
                result = new System.Data.DataSet();
                result.Locale = System.Globalization.CultureInfo.InvariantCulture;
                result.ReadXml(dataStream);
            }

            return result;
        }

        /// <summary>
        /// Makes an authenticated web service call and returns resulting stream. You must close the stream once you are done with it.
        /// </summary>
        /// <param name="wsAddress">Address of web service with parameters to call.</param>
        /// <returns></returns>
        /// <exception cref="AuthenticationException">The <see cref="Token"/>, <see cref="Cookies"/> or <see cref="WssId"/> is not set or then authentication system returned an error.</exception>
        public System.IO.Stream GetAuthenticatedServiceStream(System.Uri wsAddress)
        {
            System.UriBuilder fullAddress;

            if (this._token.Length == 0)
            {
                throw new AuthenticationException(Resources.ErrorTokenNotSet, AuthenticationErrorCode.TokenInvalid);
            }
            else if (this._cookies.Length == 0)
            {
                throw new AuthenticationException(Resources.ErrorCookieNotSet, AuthenticationErrorCode.Other);
            }
            else if (this._wssId.Length == 0)
            {
                throw new AuthenticationException(Resources.ErrorWssIdNotSet, AuthenticationErrorCode.Other);
            }

			// Check if user is credentialed
			if(this.IsCredentialed == false)
			{
				// Attempt to update credentials
				this.UpdateCredentials();
			}

            fullAddress = new UriBuilder(wsAddress);
            fullAddress.Query = AppendQuery(fullAddress.Query, "appid=" + System.Web.HttpUtility.UrlEncode(_applicationId)
                            + "&WSSID=" + System.Web.HttpUtility.UrlEncode(_wssId));

            // Make an authenticated call the given web service
            return GetServiceStream(fullAddress.Uri, true);
        }

        /// <summary>
        /// Makes an authenticated web service call and returns result as a string.
        /// </summary>
        /// <param name="wsAddress">Address of web service with parameters to call.</param>
        /// <returns></returns>
        public string GetAuthenticatedServiceString(System.Uri wsAddress)
        {
            string result = "";

            using (System.IO.Stream dataStream = GetAuthenticatedServiceStream(wsAddress))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(dataStream))
                {
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }

		/// <summary>
		/// Makes an authenticated web service call that returns XML data and returns the result as an XmlDocument.
		/// </summary>
		/// <param name="wsAddress">Address of web service with parameters to call.</param>
		/// <returns></returns>
		public System.Xml.XmlDocument GetAuthenticatedServiceXmlDocument(System.Uri wsAddress)
		{
			System.Xml.XmlDocument result = null;

			using (System.IO.Stream dataStream = GetAuthenticatedServiceStream(wsAddress))
			{
				// Read result into a XmlDocument
				result = new System.Xml.XmlDocument();
				result.Load(dataStream);
			}

			return result;
		}

		/// <summary>
		/// Makes an authenticated web service call that returns XML data and returns the result as an XPathDocument.
		/// </summary>
		/// <param name="wsAddress">Address of web service with parameters to call.</param>
		/// <returns></returns>
		public System.Xml.XPath.XPathDocument GetAuthenticatedServiceXPathDocument(System.Uri wsAddress)
		{
			System.Xml.XPath.XPathDocument result = null;

			using (System.IO.Stream dataStream = GetAuthenticatedServiceStream(wsAddress))
			{
				// Read result into a XPathDocument
				result = new System.Xml.XPath.XPathDocument(dataStream);
			}

			return result;
		}

		/// <summary>
        /// Gets the address used to retrieve the user's authentication credentials.
        /// This is the second step in the browser authentication process.
        /// </summary>
        /// <returns>Signed address used to retrieve user's credentials (WssId and Cookies).</returns>
        /// <exception cref="AuthenticationException">The <see cref="Token"/> is not set.</exception>
        public Uri GetTokenLogOnAddress()
        {
            string appParam = "";
            System.UriBuilder result;

            if (this._token.Length == 0)
            {
                throw new AuthenticationException(Resources.ErrorTokenNotSet, AuthenticationErrorCode.TokenInvalid);
            }

            result = new UriBuilder(_wsTokenLogOnUrl);
            result.Query = AppendQuery(result.Query, "appid=" + System.Web.HttpUtility.UrlEncode(_applicationId)
                             + "&token=" + System.Web.HttpUtility.UrlEncode(_token) + appParam);

            return SignUrl(result.Uri);
        }

        /// <summary>
        /// Gets the address the user's browser should be redirected to for login. 
        /// This is the first step in the browser authentication process.
        /// </summary>
        /// <returns></returns>
        public Uri GetUserLogOnAddress()
        {
            return GetUserLogOnAddress("");
        }

        /// <summary>
        /// Gets the address the user's browser should be redirected to for login. 
        /// This is the first step in the browser authentication process.
        /// </summary>
        /// <param name="applicationData">Optional data string, typically a session id, 
        /// that Yahoo will transfer to the target application upon successful authentication.</param>
        /// <returns></returns>
        public Uri GetUserLogOnAddress(string applicationData)
        {
            string appParam = "";
            System.UriBuilder result;

            if (applicationData != null && applicationData.Length != 0)
            {
                appParam = "&appdata=" + System.Web.HttpUtility.UrlEncode(applicationData);
            }

            result = new UriBuilder(_wsUserLogOnUrl);
            result.Query = AppendQuery(result.Query, "appid=" + System.Web.HttpUtility.UrlEncode(_applicationId)
                             + appParam);

            return SignUrl(result.Uri);
        }

        /// <summary>
        /// Gets a web service address with the required authentication information. 
        /// Note that you will still need to set the cookie when doing the actual call.
        /// </summary>
        /// <returns>Returns the given web service address appended with the application Id and WSSID.</returns>
        /// <exception cref="ArgumentNullException">The value specified is null (Nothing in VB) or an empty string.</exception>
        public Uri GetWebServiceAddress(Uri webService)
        {
            System.UriBuilder result;

            if (webService == null)
            {
                throw new ArgumentNullException("webService");
            }

            result = new UriBuilder(webService);
            result.Query = AppendQuery(result.Query, "appid=" + System.Web.HttpUtility.UrlEncode(_applicationId)
                             + "WSSID=" + System.Web.HttpUtility.UrlEncode(_wssId));

            return result.Uri;
        }

        /// <summary>
		/// Verifies that a given signed URL is valid using the stored shared secret.
		/// </summary>
		/// <param name="url">URL to verify.</param>
        /// <returns>True if validation succeeded, false otherwise.</returns>
		public bool IsValidSignedUrl(Uri url)
		{
			return IsValidSignedUrl(url, this._sharedSecret);
		}

		/// <summary>
		/// Verifies that a given signed URL is valid.
		/// </summary>
		/// <param name="url">URL to verify.</param>
		/// <param name="secret">Shared secret used to sign the URL.</param>
		/// <returns>True if validation succeeded, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">The specified url is null (Nothing in VB).</exception>
        public bool IsValidSignedUrl(Uri url, string secret)
		{
			long timestamp = 0;
			string ts;

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

			// Get the timestamp on the url
			ts = GetQueryItemValue(url.Query, "ts");
			
			if (ts.Length != 0)
			{
				try
                {
                    timestamp = long.Parse(ts, System.Globalization.CultureInfo.InvariantCulture);
                } 
				catch(FormatException) {}
                catch (OverflowException) { }
            } // ts.Length != 0

            return IsValidSignedUrl(url, secret, timestamp);
		}

		/// <summary>
		/// Verifies that a given signed URL is valid.
		/// </summary>
		/// <param name="url">URL to verify.</param>
		/// <param name="secret">Shared secret used to sign the URL.</param>
        /// <param name="timestamp">Timestamp to use for signing. You should always use the current time in order to achieve a valid check.</param>
        /// <returns>True if validation succeeded, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">The specified url or secret is null (Nothing in VB) or an empty string.</exception>
        public bool IsValidSignedUrl(Uri url, string secret, long timestamp)
		{
			string query;
			UriBuilder relativeUrl;
			bool result = false;

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            else if (secret == null || secret.Length == 0)
            {
                throw new ArgumentNullException("secret");
            }

			// Get the path and query string
			query = url.Query;

			// Remove the "sig" key and its value
			query = RemoveQueryItem(query, "sig");

			// Verify that the timestamp on the URL is +/- 300 seconds of the current time
            if (Math.Abs(GetUnixTime() - timestamp) <= MaximumClockSkew)
			{

				// Hash the cleaned URL and match against given signature
				relativeUrl = new UriBuilder(url);

				// Replace query without signature
				relativeUrl.Query = query;

				if (string.Compare(GetMD5Hash(relativeUrl.Uri.PathAndQuery + secret), GetQueryItemValue(url.Query, "sig"), true, System.Globalization.CultureInfo.InvariantCulture) == 0)
				{
					result = true;
				}

			} // if timestamp difference <= 300

			return result;
		}

        /// <summary>
		/// Signs the given URL using the stored shared secret.
		/// </summary>
		/// <param name="url">URL with querystring to sign.</param>
		/// <returns></returns>
		public Uri SignUrl(Uri url)
		{
			return SignUrl(url, _sharedSecret);
		}

		/// <summary>
		/// Signs the given URL with the given secret.
		/// </summary>
		/// <param name="url">URL with querystring to sign.</param>
		/// <param name="secret">Secret to use for signing.</param>
		/// <returns>Signed URL.</returns>
		public Uri SignUrl(Uri url, string secret)
		{
            return SignUrl(url, secret, GetUnixTime());
		}

        /// <summary>
        /// Signs the given URL with the given secret.
        /// </summary>
        /// <param name="url">URL with querystring to sign.</param>
        /// <param name="secret">Secret to use for signing.</param>
        /// <param name="timestamp">Timestamp to use for signing.</param>
        /// <returns>Signed URL.</returns>
        /// <exception cref="ArgumentNullException">The specified url or secret is null (Nothing in VB) or an empty string.</exception>
        public Uri SignUrl(Uri url, string secret, long timestamp)
        {
            UriBuilder result;

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            else if (secret == null || secret.Length == 0)
            {
                throw new ArgumentNullException("secret");
            }

            // Copy given URI to result and append timestamp
            result = new UriBuilder(url);

            result.Query = AppendQuery(result.Query, "ts=" + string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:g}", timestamp));

            // Append signature to result
            result.Query = AppendQuery(result.Query, "sig=" + GetMD5Hash(result.Uri.PathAndQuery + secret));

            return result.Uri;
        }

        /// <summary>
        /// Attempts to get or update the user authentication credentials that are required for web service calls.
        /// </summary>
        /// <exception cref="AuthenticationException">The <see cref="Token"/> is not set or the authentication system returned an error. Check the ErrorCode property for details.</exception>
        public void UpdateCredentials()
		{
			System.Xml.XPath.XPathDocument xpDoc;
			System.Xml.XPath.XPathNavigator navigator;
			System.Xml.XPath.XPathNavigator node;

            if(this._token.Length == 0)
            {
                throw new AuthenticationException(Resources.ErrorTokenNotSet, AuthenticationErrorCode.TokenInvalid);
            }

			// Make a call to the authentication service to retrieve the WSSID and cookie
			try
			{
                // Clear current authentication details
                _wssId = "";
                _cookies = "";

                using (System.IO.Stream dataStream = GetServiceStream(GetTokenLogOnAddress(),false))
                {
					xpDoc = new System.Xml.XPath.XPathDocument(dataStream);
                }

				// Attempt to select "/BBAuthTokenLoginResponse/Success"
				navigator = xpDoc.CreateNavigator();

				node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Success");

                if (node != null)
                {
					// Get cookies
					node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Success/Cookie");
					if (node != null) { _cookies = node.Value; }
					if (_cookies.Length != 0)
					{
						_cookies = _cookies.Replace(";", ",");
					}
					else
					{
						// Raise custom error
						throw new AuthenticationException(Resources.ErrorCookieParse, AuthenticationErrorCode.Other);
					}

					// Get WSSID
					node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Success/WSSID");
					if (node != null)
					{
						_wssId = node.Value;
					}
					else
					{
						// Raise custom error
						throw new AuthenticationException(Resources.ErrorWssIdNotSet, AuthenticationErrorCode.Other);
					}

					// Get timeout value of the cookie and WSSID
					int timeout = 0;
					node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Success/Timeout");
					if (node != null)
					{
						try
						{
							// With .NET 2.0 we could use node.ValueAsInt
							timeout = int.Parse(node.Value, System.Globalization.CultureInfo.InvariantCulture);
						}
						catch (FormatException)
						{
							// Raise custom error
							throw new AuthenticationException(Resources.ErrorTimeoutParse, AuthenticationErrorCode.Other);
						}
						catch (OverflowException)
						{
							// Raise custom error
							throw new AuthenticationException(Resources.ErrorTimeoutParse, AuthenticationErrorCode.Other);
						}
						catch (InvalidCastException)
						{
							// Raise custom error
							throw new AuthenticationException(Resources.ErrorTimeoutParse, AuthenticationErrorCode.Other);
						}
					}
					else
					{
						// Raise custom error
						throw new AuthenticationException(Resources.ErrorTimeoutParse, AuthenticationErrorCode.Other);
					}
					_validUntil = DateTime.Now.AddSeconds(timeout);
				}
                else
                {
					AuthenticationErrorCode errorCode = AuthenticationErrorCode.Unknown;
					string errorCodeText = "";
					string errorDesc = Resources.ErrorUnknown;

					node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Error");

					if (node != null)
					{
						// Get error code
						node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Error/ErrorCode");
						if (node != null) { errorCodeText = node.Value; }

						if (errorCodeText.Length != 0)
						{
							try
							{
								if (Enum.IsDefined(typeof(AuthenticationErrorCode), int.Parse(errorCodeText, System.Globalization.CultureInfo.InvariantCulture)) == true)
								{
									errorCode = (AuthenticationErrorCode)Enum.Parse(typeof(AuthenticationErrorCode), errorCodeText, true);
								}
							}
							catch (ArgumentException) { }
							catch (FormatException) { }
						}

						// Get error description
						node = SelectSingleNode(navigator, "/BBAuthTokenLoginResponse/Error/ErrorDescription");
						if (node != null) { errorDesc = node.Value; }

					}

                    // Raise error
                    throw new AuthenticationException(errorDesc, errorCode);
                }
            }
            catch(System.Net.WebException wex)
            {
                // Try to retrieve more information about the network error
                string errorDesc = Resources.ErrorNetwork;
                System.Net.HttpWebResponse errorResponse = null;

                try
                {
                    errorResponse = wex.Response as System.Net.HttpWebResponse;
                    if (errorResponse != null)
                    {
                        errorDesc = errorResponse.StatusDescription + " (" + ((int)errorResponse.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                    }
                }
                catch { }
                finally
                {
                    if (errorResponse != null)
                    {
                        errorResponse.Close();
                    }
                }

                // Raise error
                throw new AuthenticationException(errorDesc, wex, AuthenticationErrorCode.NetworkError);
            }
		}

		#endregion

        #region Virtual methods

        /// <summary>
        /// Makes a web request to the given address.
        /// </summary>
        /// <param name="wsAddress">Address to make web request to.</param>
        /// <param name="setCookies">Sets whether the internally stored cookies should be sent with the request.</param>
        /// <returns></returns>
        public virtual System.IO.Stream GetServiceStream(System.Uri wsAddress, bool setCookies)
        {
            System.Net.HttpWebRequest request = null;
            System.Net.HttpWebResponse response = null;
            System.IO.Stream result = null;

            if (wsAddress == null)
            {
                throw new ArgumentNullException("wsAddress");
            }

            // Create and initialize the web request
            request = System.Net.WebRequest.Create(wsAddress) as System.Net.HttpWebRequest;

            request.UserAgent = _userAgent;
            request.KeepAlive = false;
            request.Timeout = _httpTimeout;
            if (setCookies == true)
            {
                if (_cookies.Length != 0)
                {
                    request.CookieContainer = new System.Net.CookieContainer();
                    request.CookieContainer.SetCookies(new System.Uri(wsAddress.AbsoluteUri), _cookies);
                }
            }
            if (_proxy != null) { request.Proxy = _proxy; }

            response = request.GetResponse() as System.Net.HttpWebResponse;

            if (request.HaveResponse == true && response != null)
            {
				result = response.GetResponseStream();
            }

            return result;
        }

        #endregion

        #region Private methods

        /// <summary>
		/// Appends a query to a query string.
		/// </summary>
		/// <param name="currentQuery">Current query string or null.</param>
		/// <param name="newQuery">Query to append.</param>
		/// <returns>Combined query string.</returns>
		/// <remarks>The resulting query string does not contain the leading question mark.</remarks>
		private static string AppendQuery(string currentQuery, string newQuery)
		{
			string result;

			if(currentQuery != null && currentQuery.Length > 1) {
				// Remove the starting ?
				if (currentQuery.Substring(0, 1) == "?")
				{
					result = currentQuery.Substring(1) + "&" + newQuery;
				}
				else
				{
					result = currentQuery + "&" + newQuery;
				}
			}
			else {
				result = newQuery;
			}

			return result;
		}

		/// <summary>
		/// Gets the value from the given query string for the given key.
		/// </summary>
		/// <param name="query">Query string to search.</param>
		/// <param name="key">Key of the item to find.</param>
		/// <returns>Value of the key/value pair or empty string if key isn't found.</returns>
		private static string GetQueryItemValue(string query, string key)
		{
			string[] items;
			string[] itemPair;
			int count;
			string result = "";

			if (query != null && query.Length > 1)
			{
				// Remove the starting ?
				if (query.Substring(0, 1) == "?")
				{
					query = query.Substring(1);
				}

				// Split the given query string with &
				items = query.Split(new char[] { '&' });

				// Loop all items and search for the requested item
				for(count=0; count<items.Length; count++)
				{
					// Get the key/value pair
					itemPair = items[count].Split(new char[] { '=' });

					// Compare the key with the requested item
					if(string.Compare(itemPair[0], key, true, System.Globalization.CultureInfo.InvariantCulture) == 0)
					{
						if (itemPair.Length >= 1)
						{
							// Extract value
							result = itemPair[1];
						}
						break;
					} // if key=query

				} // for items

			} // If query != null

			return result;
		}

		/// <summary>
		/// Removes the requested key/value pair from the given query string.
		/// </summary>
		/// <param name="query">Query string to modify.</param>
		/// <param name="key">Key of the key/value pair to remove.</param>
		/// <returns>Query string without the requested key/value pair.</returns>
		/// <remarks>The resulting query string does not contain the leading question mark.</remarks>
		private static string RemoveQueryItem(string query, string key)
		{
			string[] items;
			string[] itemPair;
			string value;
			int count;
			string result = "";

			if (query != null && query.Length > 1)
			{

				// Remove the starting ?
				if (query.Substring(0, 1) == "?")
				{
					query = query.Substring(1);
				}

				// Split the given query string with &
				items = query.Split(new char[] { '&' });

				// Loop all items and search for the requested item
				for (count = 0; count < items.Length; count++)
				{
					// Get the key/value pair
					itemPair = items[count].Split(new char[] { '=' });

					// Compare the key with the requested item
					if (string.Compare(itemPair[0], key, true, System.Globalization.CultureInfo.InvariantCulture) != 0)
					{
                        value = "";
                        // Check if we had a value or not
						if (itemPair.Length >= 1)
						{
							value = itemPair[1];
						}

						// Wasn't the key to remove, add this key/value pair to result
						result = AppendQuery(result, itemPair[0] + "=" + value);
					} // if key==query

				} // for items

			} // If query != null

			return result;
		}

		/// <summary>
		/// Selects a single node in the XPathNavigator using the specified XPath query. Needed for .NET 1.1 compatibility,
		/// .NET has this method built-in.
		/// </summary>
		/// <param name="navigator">XPathNavigator.</param>
		/// <param name="expression">A String representing an XPath expression.</param>
		/// <returns></returns>
		private System.Xml.XPath.XPathNavigator SelectSingleNode(System.Xml.XPath.XPathNavigator navigator, string expression)
		{
			System.Xml.XPath.XPathNodeIterator iterator = navigator.Select(expression);
			if(iterator.MoveNext() == true)
			{
				return iterator.Current;
			}
			else
			{
				return null;
			}
		}

		#endregion

	}
}
