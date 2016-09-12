using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OnlineVideos.MPUrlSourceFilter.Http;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represent base class for HTTP urls for MediaPortal Url Source Splitter.
    /// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
    /// </summary>
    [Serializable]
    public class HttpUrl : SimpleUrl
    {
        #region Private fields

        private String referer = HttpUrl.DefaultHttpReferer;
        private String userAgent = HttpUrl.DefaultHttpUserAgent;
        Version version = HttpUrl.DefaultHttpVersion;
        private CookieCollection cookies = new CookieCollection();
        private bool ignoreContentLength = HttpUrl.DefaultHttpIgnoreContentLength;
        private int openConnectionTimeout = HttpUrl.DefaultHttpOpenConnectionTimeout;
        private int openConnectionSleepTime = HttpUrl.DefaultHttpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = HttpUrl.DefaultHttpTotalReopenConnectionTimeout;
        private HttpHeaderCollection customHeaders;

        private String serverUserName = HttpUrl.DefaultHttpServerUserName;
        private String serverPassword = HttpUrl.DefaultHttpServerPassword;

        private String proxyServer = HttpUrl.DefaultHttpProxyServer;
        private int proxyServerPort = HttpUrl.DefaultHttpProxyServerPort;
        private String proxyServerUserName = HttpUrl.DefaultHttpProxyServerUserName;
        private String proxyServerPassword = HttpUrl.DefaultHttpProxyServerPassword;
        private ProxyServerType proxyServerType = HttpUrl.DefaultHttpProxyServerType;

        private String streamFileName = HttpUrl.DefaultStreamFileName;

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
            if (this.Uri.Scheme != "http" && this.Uri.Scheme != "https")
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }

            this.cookies = new CookieCollection();
            this.customHeaders = new HttpHeaderCollection();

            this.Version = null;
            this.IgnoreContentLength = false;
            this.ServerAuthenticate = false;
            this.ProxyServerAuthenticate = false;
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
        /// Gets or sets the timeout to open HTTP url in milliseconds.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        public int OpenConnectionTimeout
        {
            get { return this.openConnectionTimeout; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("OpenConnectionTimeout", value, "Cannot be less than zero.");
                }

                this.openConnectionTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the time in milliseconds to sleep before opening connection.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionSleepTime"/> is lower than zero.</para>
        /// </exception>
        public int OpenConnectionSleepTime
        {
            get { return this.openConnectionSleepTime; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("OpenConnectionSleepTime", value, "Cannot be less than zero.");
                }

                this.openConnectionSleepTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the total timeout to open HTTP url in milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="TotalReopenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        public int TotalReopenConnectionTimeout
        {
            get { return this.totalReopenConnectionTimeout; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("TotalReopenConnectionTimeout", value, "Cannot be less than zero.");
                }

                this.totalReopenConnectionTimeout = value;
            }
        }

        /// <summary>
        /// Gets custom headers collection.
        /// </summary>
        public HttpHeaderCollection CustomHeaders
        {
            get { return this.customHeaders; }
        }

        /// <summary>
        /// Specifies if filter has to authenticate against remote server.
        /// </summary>
        public Boolean ServerAuthenticate { get; set; }

        /// <summary>
        /// Gets or sets the remote server user name.
        /// </summary>
        public String ServerUserName
        {
            get { return this.serverUserName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ServerUserName");
                }

                this.serverUserName = value;
            }
        }

        /// <summary>
        /// Gets or sets the remote server password.
        /// </summary>
        public String ServerPassword
        {
            get { return this.serverPassword; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ServerPassword");
                }

                this.serverPassword = value;
            }
        }

        /// <summary>
        /// Specifies if filter has to authenticate against proxy server.
        /// </summary>
        public Boolean ProxyServerAuthenticate { get; set; }

        /// <summary>
        /// Gets or sets the proxy server.
        /// </summary>
        public String ProxyServer
        {
            get { return this.proxyServer; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ProxyServer");
                }

                this.proxyServer = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy server port.
        /// </summary>
        public int ProxyServerPort
        {
            get { return this.proxyServerPort; }
            set
            {
                if ((value < 0) || (value > 65535))
                {
                    throw new ArgumentOutOfRangeException("ProxyServerPort", value, "Must be greater than or equal to zero and lower than 65536.");
                }

                this.proxyServerPort = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy server user name.
        /// </summary>
        public String ProxyServerUserName
        {
            get { return this.proxyServerUserName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ProxyServerUserName");
                }

                this.proxyServerUserName = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy server password.
        /// </summary>
        public String ProxyServerPassword
        {
            get { return this.proxyServerPassword; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ProxyServerPassword");
                }

                this.proxyServerPassword = value;
            }
        }

        /// <summary>
        /// Gets or sets the proxy server type.
        /// </summary>
        public ProxyServerType ProxyServerType
        {
            get { return this.proxyServerType; }
            set
            {
                switch (value)
                {
                    case ProxyServerType.None:
                    case ProxyServerType.HTTP:
                    case ProxyServerType.HTTP_1_0:
                    case ProxyServerType.SOCKS4:
                    case ProxyServerType.SOCKS5:
                    case ProxyServerType.SOCKS4A:
                    case ProxyServerType.SOCKS5_HOSTNAME:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("ProxyServerType", value, "The proxy server type value is unknown.");
                }

                this.proxyServerType = value;
            }
        }

        /// <summary>
        /// Gets or sets the stream file name. Stream file name have to be used when FFmpeg cannot autodetect stream format.
        /// </summary>
        public String StreamFileName
        {
            get { return this.streamFileName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("StreamFileName");
                }

                this.streamFileName = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a string that can be given to the MediaPortal Url Source Splitter holding the url and all parameters.
        /// </summary>
        internal override string ToFilterString()
        {
            ParameterCollection parameters = new ParameterCollection();

            if (this.IgnoreContentLength != HttpUrl.DefaultHttpIgnoreContentLength)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpIgnoreContentLength, this.IgnoreContentLength ? "1" : "0"));
            }
            if (this.OpenConnectionTimeout != HttpUrl.DefaultHttpOpenConnectionTimeout)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpOpenConnectionTimeout, this.OpenConnectionTimeout.ToString()));
            }
            if (this.OpenConnectionSleepTime != HttpUrl.DefaultHttpOpenConnectionSleepTime)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpOpenConnectionSleepTime, this.OpenConnectionSleepTime.ToString()));
            }
            if (this.TotalReopenConnectionTimeout != HttpUrl.DefaultHttpTotalReopenConnectionTimeout)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpTotalReopenConnectionTimeout, this.TotalReopenConnectionTimeout.ToString()));
            }
            if (String.CompareOrdinal(this.Referer, HttpUrl.DefaultHttpReferer) != 0)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpReferer, this.Referer.ToString()));
            }
            if (String.CompareOrdinal(this.UserAgent, HttpUrl.DefaultHttpUserAgent) != 0)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpUserAgent, this.UserAgent.ToString()));
            }

            if (this.Version == HttpVersion.Version10)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpVersion, HttpUrl.HttpVersionForce10.ToString()));
            }
            else if (this.Version == HttpVersion.Version11)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpVersion, HttpUrl.HttpVersionForce11.ToString()));
            }

            if (this.Cookies.Count > 0)
            {
                CookieContainer container = new CookieContainer(this.Cookies.Count);
                foreach (Cookie cookie in this.Cookies)
                {
                    container.Add(this.Uri, cookie);
                }
                parameters.Add(new Parameter(HttpUrl.ParameterHttpCookie, container.GetCookieHeader(this.Uri)));
            }

            if (this.CustomHeaders.Count > 0)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpHeadersCount, this.CustomHeaders.Count.ToString()));

                for (int i = 0; i < this.CustomHeaders.Count; i++)
                {
                    HttpHeader header = this.CustomHeaders[i];

                    parameters.Add(new Parameter(String.Format(HttpUrl.ParameterHttpHeaderFormatName, i), header.Name));
                    parameters.Add(new Parameter(String.Format(HttpUrl.ParameterHttpHeaderFormatValue, i), header.Value));
                    
                }
            }

            if (this.ServerAuthenticate)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpServerAuthenticate, "1"));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpServerUserName, this.ServerUserName));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpServerPassword, this.ServerPassword));
            }

            if (this.ProxyServerAuthenticate)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServerAuthenticate, "1"));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServer, this.ProxyServer));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServerPort, this.ProxyServerPort.ToString()));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServerUserName, this.ProxyServerUserName));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServerPassword, this.ProxyServerPassword));
                parameters.Add(new Parameter(HttpUrl.ParameterHttpProxyServerType, ((int)this.ProxyServerType).ToString()));
            }

            if (String.CompareOrdinal(this.StreamFileName, HttpUrl.DefaultStreamFileName) != 0)
            {
                parameters.Add(new Parameter(HttpUrl.ParameterHttpStreamFileName, this.StreamFileName.ToString()));
            }

            // return formatted connection string
            return base.ToFilterString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        internal override void ApplySettings(Sites.SiteUtilBase siteUtil)
        {
            siteUtil.HttpSettings.Apply(this);
        }

        #endregion

        #region Constants

        /* parameters */

        /// <summary>
        /// Specifies open connection timeout in milliseconds.
        /// </summary>
        protected static readonly String ParameterHttpOpenConnectionTimeout = "HttpOpenConnectionTimeout";

        /// <summary>
        /// Specifies the time in milliseconds to sleep before opening connection.
        /// </summary>
        protected static readonly String ParameterHttpOpenConnectionSleepTime = "HttpOpenConnectionSleepTime";

        /// <summary>
        /// Specifies the total timeout to open HTTP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.
        /// </summary>
        protected static readonly String ParameterHttpTotalReopenConnectionTimeout = "HttpTotalReopenConnectionTimeout";

        /// <summary>
        /// Specifies the value of referer HTTP header to send to remote server.
        /// </summary>
        protected static readonly String ParameterHttpReferer = "HttpReferer";

        /// <summary>
        /// Specifies the value of user agent HTTP header to send to remote server.
        /// </summary>
        protected static readonly String ParameterHttpUserAgent = "HttpUserAgent";

        /// <summary>
        /// Specifies the value of cookie HTTP header to send to remote server.
        /// </summary>
        protected static readonly String ParameterHttpCookie = "HttpCookie";

        /// <summary>
        /// Forces to use specific HTTP protocol version
        /// </summary>
        protected static readonly String ParameterHttpVersion = "HttpVersion";

        /// <summary>
        /// Specifies that version of HTTP protocol is not specified.
        /// </summary>
        protected static readonly int HttpVersionNone = 0;

        /// <summary>
        /// Forces to use HTTP version 1.0.
        /// </summary>
        protected static readonly int HttpVersionForce10 = 1;

        /// <summary>
        /// Forces to use HTTP version 1.1.
        /// </summary>
        protected static readonly int HttpVersionForce11 = 2;

        /// <summary>
        /// Specifies if content length HTTP header have to be ignored (e.g. because server reports bad content length).
        /// </summary>
        protected static readonly String ParameterHttpIgnoreContentLength = "HttpIgnoreContentLength";

        /// <summary>
        /// Specifies if seeking is supported by specifying range HTTP header in request.
        /// </summary>
        protected static readonly String ParameterHttpSeekingSupported = "HttpSeekingSupported";

        /// <summary>
        /// Enables or disables automatic detection of seeking support.
        /// </summary>
        protected static readonly String ParameterHttpSeekingSupportDetection = "HttpSeekingSupportDetection";

        /// <summary>
        /// Specifies count of custom HTTP headers.
        /// </summary>
        protected static readonly String ParameterHttpHeadersCount = "HttpHeadersCount";

        /// <summary>
        /// Parameter name format for custom HTTP header.
        /// </summary>
        protected static readonly String ParameterHttpHeaderFormatName = "HttpHeaderName{0:D8}";

        /// <summary>
        /// Parameter value format for custom HTTP header.
        /// </summary>
        protected static readonly String ParameterHttpHeaderFormatValue = "HttpHeaderValue{0:D8}";

        /// <summary>
        /// Specifies if filter has to authenticate against remote server.
        /// </summary>
        protected static readonly String ParameterHttpServerAuthenticate = "HttpServerAuthenticate";

        /// <summary>
        /// Specifies the value of remote server user name to authenticate.
        /// </summary>
        protected static readonly String ParameterHttpServerUserName = "HttpServerUserName";

        /// <summary>
        /// Specifies the value of remote server password to authenticate.
        /// </summary>
        protected static readonly String ParameterHttpServerPassword = "HttpServerPassword";

        /// <summary>
        /// Specifies if filter has to authenticate against proxy server.
        /// </summary>
        protected static readonly String ParameterHttpProxyServerAuthenticate = "HttpProxyServerAuthenticate";

        /// <summary>
        /// Specifies the value of proxy server.
        /// </summary>
        protected static readonly String ParameterHttpProxyServer = "HttpProxyServer";

        /// <summary>
        /// Specifies the value of proxy server port.
        /// </summary>
        protected static readonly String ParameterHttpProxyServerPort = "HttpProxyServerPort";

        /// <summary>
        /// Specifies the value of remote server user name to authenticate.
        /// </summary>
        protected static readonly String ParameterHttpProxyServerUserName = "HttpProxyServerUserName";

        /// <summary>
        /// Specifies the value of remote server password to authenticate.
        /// </summary>
        protected static readonly String ParameterHttpProxyServerPassword = "HttpProxyServerPassword";

        /// <summary>
        /// Specifies the value of proxy server type.
        /// </summary>
        protected static readonly String ParameterHttpProxyServerType = "HttpProxyServerType";

        /// <summary>
        /// Specifies the value of stream file name.
        /// </summary>
        protected static readonly String ParameterHttpStreamFileName = "StreamFileName";

        /* default values */

        /// Default value for <see cref="ParameterHttpOpenConnectionTimeout"/>.
        /// </summary>
        public const int DefaultHttpOpenConnectionTimeout = 20000;

        /// <summary>
        /// Default value for <see cref="ParameterHttpOpenConnectionSleepTime"/>.
        /// </summary>
        public const int DefaultHttpOpenConnectionSleepTime = 0;

        /// <summary>
        /// Default value for <see cref="ParameterHttpTotalReopenConnectionTimeout"/>.
        /// </summary>
        public const int DefaultHttpTotalReopenConnectionTimeout = 60000;

        /// <summary>
        /// Default value for <see cref="ParameterHttpReferer"/>.
        /// </summary>
        public const String DefaultHttpReferer = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpUserAgent"/>.
        /// </summary>
        public const String DefaultHttpUserAgent = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpCookie"/>.
        /// </summary>
        public const String DefaultHttpCookie = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpVersion"/>.
        /// </summary>
        public const Version DefaultHttpVersion = null;

        /// <summary>
        /// Default value for <see cref="ParameterHttpIgnoreContentLength"/>.
        /// </summary>
        public const Boolean DefaultHttpIgnoreContentLength = false;

        /// <summary>
        /// Default value for <see cref="ParameterHttpSeekingSupported"/>.
        /// </summary>
        public const Boolean DefaultHttpSeekingSupported = false;

        /// <summary>
        /// Default value for <see cref="ParameterHttpSeekingSupportDetection"/>.
        /// </summary>
        public const Boolean DefaultHttpSeekingSupportDetection = true;

        /// <summary>
        /// Default value for <see cref="ParameterHttpHeadersCount"/>.
        /// </summary>
        public const int DefaultHttpHeadersCount = 0;

        /// <summary>
        /// Default value for <see cref="ParameterHttpServerAuthenticate"/>.
        /// </summary>
        public const Boolean DefaultHttpServerAuthenticate = false;

        /// <summary>
        /// Default value for <see cref="ParameterHttpServerUserName"/>.
        /// </summary>
        public const String DefaultHttpServerUserName = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpServerPassword"/>.
        /// </summary>
        public const String DefaultHttpServerPassword = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServerAuthenticate"/>.
        /// </summary>
        public const Boolean DefaultHttpProxyServerAuthenticate = false;

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServer"/>.
        /// </summary>
        public const String DefaultHttpProxyServer = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServerPort"/>.
        /// </summary>
        public const int DefaultHttpProxyServerPort = 1080;

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServerUserName"/>.
        /// </summary>
        public const String DefaultHttpProxyServerUserName = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServerPassword"/>.
        /// </summary>
        public const String DefaultHttpProxyServerPassword = "";

        /// <summary>
        /// Default value for <see cref="ParameterHttpProxyServerType"/>.
        /// </summary>
        public const ProxyServerType DefaultHttpProxyServerType = ProxyServerType.HTTP;

        /// <summary>
        /// Default value for <see cref="StreamFileName"/>.
        /// </summary>
        public const String DefaultStreamFileName = "";

        #endregion
    }
}
