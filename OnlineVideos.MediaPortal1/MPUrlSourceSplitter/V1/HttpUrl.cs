using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Represent base class for HTTP urls for MediaPortal Url Source Splitter.
	/// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
    /// </summary>
    public class HttpUrl : SimpleUrl
    {
        #region Private fields

        private String referer;
        private String userAgent;
        Version version;
        private CookieCollection cookies;
        private bool ignoreContentLength;
        private int receiveDataTimeout = HttpUrl.DefaultReceiveDataTimeout;
        private int openConnectionMaximumAttempts = HttpUrl.DefaultOpenConnectionMaximumAttempts;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrl"/> class.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="HttpUrl"/> class.
        /// </overloads>
        public HttpUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrl"/> class.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentException">
        /// <para>The protocol supplied by <paramref name="uri"/> is not supported.</para>
        /// </exception>
        public HttpUrl(Uri uri)
            : base(uri)
        {
            if (this.Uri.Scheme != "http")
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }

            this.cookies = new CookieCollection();

            this.Referer = String.Empty;
            this.UserAgent = String.Empty;
            this.Version = null;
            this.IgnoreContentLength = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets referer HTTP header.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="Referer"/> is <see langword="null"/>.</para>
        /// </exception>
        public String Referer
        {
            get { return this.referer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Referer");
                }

                this.referer = value;
            }
        }

        /// <summary>
        /// Gets or sets user agent HTTP header.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="UserAgent"/> is <see langword="null"/>.</para>
        /// </exception>
        public String UserAgent
        {
            get { return this.userAgent; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("UserAgent");
                }

                this.userAgent = value;
            }
        }

        /// <summary>
        /// Gets or sets HTTP version.
        /// </summary>
        /// <remarks>
        /// If <see cref="Version"/> is <see langword="null"/>, than version supported by remote server is used.
        /// </remarks>
        public Version Version
        {
            get { return this.version; }
            set
            {
                this.version = value;
            }
        }

        /// <summary>
        /// Gets or sets ignore content length flag.
        /// </summary>
        /// <remarks>
        /// This is useful to set for Apache 1.x (and similar servers) which will report incorrect content length for files over 2 gigabytes.
        /// </remarks>
        public Boolean IgnoreContentLength
        {
            get { return this.ignoreContentLength; }
            set
            {
                this.ignoreContentLength = value;
            }
        }

        /// <summary>
        /// Gets collection of cookies.
        /// </summary>
        public CookieCollection Cookies
        {
            get { return this.cookies; }
        }

        /// <summary>
        /// Gets or sets received data timeout.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="ReceiveDataTimeout"/> is less than zero.</para>
        /// </exception>
        /// <remarks>
        /// The value is in milliseconds.
        /// </remarks>
        public int ReceiveDataTimeout
        {
            get { return this.receiveDataTimeout; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("ReceiveDataTimeout", value, "Cannot be less than zero.");
                }

                this.receiveDataTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum attempts of opening connection to remote server.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionMaximumAttempts"/> is less than zero.</para>
        /// </exception>
        public int OpenConnectionMaximumAttempts
        {
            get { return this.openConnectionMaximumAttempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("OpenConnectionMaximumAttempts", value, "Cannot be less than zero.");
                }

                this.openConnectionMaximumAttempts = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the specified instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the unescaped canonical representation of the this instance.
        /// </returns>
        public override string ToString()
        {
            ParameterCollection parameters = new ParameterCollection();

			if (this.IgnoreContentLength != DefaultIgnoreContentLength)
			{
				parameters.Add(new Parameter(HttpUrl.ParameterIgnoreContentLength, this.IgnoreContentLength ? "1" : "0"));
			}
			if (this.OpenConnectionMaximumAttempts != DefaultOpenConnectionMaximumAttempts)
			{
				parameters.Add(new Parameter(HttpUrl.ParameterOpenConnectionMaximumAttempts, this.OpenConnectionMaximumAttempts.ToString()));
			}
			if (this.ReceiveDataTimeout != DefaultReceiveDataTimeout)
			{
				parameters.Add(new Parameter(HttpUrl.ParameterReceiveDataTimeout, this.ReceiveDataTimeout.ToString()));
			}
            if (!String.IsNullOrEmpty(this.Referer))
            {
                parameters.Add(new Parameter(HttpUrl.ParameterReferer, this.Referer.ToString()));
            }
            if (!String.IsNullOrEmpty(this.UserAgent))
            {
                parameters.Add(new Parameter(HttpUrl.ParameterUserAgent, this.UserAgent.ToString()));
            }

            if (this.Version == HttpVersion.Version10)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterVersion, HttpUrl.HttpVersionForce10.ToString()));
            }
            else if (this.Version == HttpVersion.Version11)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterVersion, HttpUrl.HttpVersionForce11.ToString()));
            }

            if (this.Cookies.Count > 0)
            {
                CookieContainer container = new CookieContainer(this.Cookies.Count);
                foreach (Cookie cookie in this.Cookies)
                {
                    container.Add(this.Uri, cookie);
                }
                parameters.Add(new Parameter(HttpUrl.ParameterCookie, container.GetCookieHeader(this.Uri)));
            }

            // return formatted connection string
            return base.ToString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        // common parameters of HTTP protocol for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies receive data timeout for HTTP protocol.
        /// </summary>
        protected static String ParameterReceiveDataTimeout = "HttpReceiveDataTimeout";

        /// <summary>
        /// Specifies how many times should MediaPortal Url Source Splitter try to receive data from remote server.
        /// </summary>
        protected static String ParameterOpenConnectionMaximumAttempts = "HttpOpenConnectionMaximumAttempts";

        /// <summary>
        /// Specifies referer HTTP header sent to remote server.
        /// </summary>
        protected static String ParameterReferer = "HttpReferer";

        /// <summary>
        /// Specifies user agent HTTP header sent to remote server.
        /// </summary>
        protected static String ParameterUserAgent = "HttpUserAgent";

        /// <summary>
        /// Specifies cookies sent to remote server.
        /// </summary>
        protected static String ParameterCookie = "HttpCookie";

        /// <summary>
        /// Specifies version of HTTP protocol to use.
        /// </summary>
        protected static String ParameterVersion = "HttpVersion";

        /// <summary>
        /// Specifies if content length should be ignored.
        /// </summary>
        protected static String ParameterIgnoreContentLength = "HttpIgnoreContentLength";

        /// <summary>
        /// Specifies that version of HTTP protocol is not specified.
        /// </summary>
        protected const int HttpVersionNone = 0;

        /// <summary>
        /// Forces to use HTTP version 1.0.
        /// </summary>
        protected const int HttpVersionForce10 = 1;

        /// <summary>
        /// Forces to use HTTP version 1.1.
        /// </summary>
        protected const int HttpVersionForce11 = 2;

        // default values for some parameters

        /// <summary>
        /// Default receive data timeout of MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// The value is in milliseconds. The default value is 20000.
        /// </remarks>
        public const int DefaultReceiveDataTimeout = 20000;

        /// <summary>
        /// Default maximum of open connection attempts of MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// The default value is 3.
        /// </remarks>
        public const int DefaultOpenConnectionMaximumAttempts = 3;

        /// <summary>
        /// Default referer for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This values is <see cref="System.String.Empty"/>.
        /// </remarks>
        public static String DefaultReferer = String.Empty;

        /// <summary>
        /// Default user agent for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This values is <see cref="System.String.Empty"/>.
        /// </remarks>
        public static String DefaultUserAgent = String.Empty;

        /// <summary>
        /// Default HTTP version for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This value is <see langword="null"/>.
        /// </remarks>
        public static HttpVersion DefaultVersion = null;

        /// <summary>
        /// Default ignore content length flag for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This values if <see langword="false"/>.
        /// </remarks>
        public static Boolean DefaultIgnoreContentLength = false;

        #endregion
    }
}
