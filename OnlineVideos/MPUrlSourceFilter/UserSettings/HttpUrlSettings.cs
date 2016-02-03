using System;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.UserSettings
{
    /// <summary>
    /// Represents class for HTTP url settings.
    /// </summary>
    [TypeConverter(typeof(ExpandableUserSettingObjectConverter<HttpUrlSettings>))]
    [Serializable]
    public class HttpUrlSettings : SimpleUrlSettings
    {
        #region Private fields

        private int openConnectionTimeout = OnlineVideoSettings.Instance.HttpOpenConnectionTimeout;
        private int openConnectionSleepTime = OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout;

        private String serverUserName = HttpUrl.DefaultHttpServerUserName;
        private String serverPassword = HttpUrl.DefaultHttpServerPassword;

        private String proxyServer = HttpUrl.DefaultHttpProxyServer;
        private int proxyServerPort = HttpUrl.DefaultHttpProxyServerPort;
        private String proxyServerUserName = HttpUrl.DefaultHttpProxyServerUserName;
        private String proxyServerPassword = HttpUrl.DefaultHttpProxyServerPassword;
        private ProxyServerType proxyServerType = HttpUrl.DefaultHttpProxyServerType;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the timeout to open HTTP url in milliseconds.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("The timeout to open HTTP url in milliseconds. It is applied to first opening of url.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The time in milliseconds to sleep before opening connection.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The total timeout to open HTTP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.")]
        [NotifyParentProperty(true)]
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
        /// Specifies if filter has to authenticate against remote server.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if filter has to authenticate against remote server.")]
        [NotifyParentProperty(true)]
        public Boolean ServerAuthenticate { get; set; }

        /// <summary>
        /// Gets or sets the remote server user name.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("The remote server user name.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The remote server password.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if filter has to authenticate against proxy server.")]
        [NotifyParentProperty(true)]
        public Boolean ProxyServerAuthenticate { get; set; }

        /// <summary>
        /// Gets or sets the proxy server.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("The proxy server.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The proxy server port.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The proxy server user name.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The proxy server password.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The proxy server type.")]
        [NotifyParentProperty(true)]
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

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrlSettings" /> class.
        /// </summary>
        public HttpUrlSettings()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrlSettings" /> class with specified HTTP url parameters.
        /// </summary>
        /// <param name="value">HTTP url parameters.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="value"/> is <see langword="null"/>.</para>
        /// </exception>
        public HttpUrlSettings(String value)
            : base(value)
        {
            this.ServerAuthenticate = HttpUrl.DefaultHttpServerAuthenticate;
            this.ProxyServerAuthenticate = HttpUrl.DefaultHttpProxyServerAuthenticate;

            var parameters = GetParameters(value);

            var ovSettings = OnlineVideoSettings.Instance;

            this.OpenConnectionTimeout = GetValue(parameters, "OpenConnectionTimeout", ovSettings.HttpOpenConnectionTimeout);
            this.OpenConnectionSleepTime = GetValue(parameters, "OpenConnectionSleepTime", ovSettings.HttpOpenConnectionSleepTime);
            this.TotalReopenConnectionTimeout = GetValue(parameters, "TotalReopenConnectionTimeout", ovSettings.HttpTotalReopenConnectionTimeout);

            this.ServerAuthenticate = GetValue(parameters, "ServerAuthenticate", ovSettings.HttpServerAuthenticate);
            this.ServerUserName = GetValue(parameters, "ServerUserName", ovSettings.HttpServerUserName);
            this.ServerPassword = GetValue(parameters, "ServerPassword", ovSettings.HttpServerPassword);

            this.ProxyServerAuthenticate = GetValue(parameters, "ProxyServerAuthenticate", ovSettings.HttpProxyServerAuthenticate);
            this.ProxyServer = GetValue(parameters, "ProxyServer", ovSettings.HttpProxyServer);
            this.ProxyServerPort = GetValue(parameters, "ProxyServerPort", ovSettings.HttpProxyServerPort);
            this.ProxyServerUserName = GetValue(parameters, "ProxyServerUserName", ovSettings.HttpProxyServerUserName);
            this.ProxyServerPassword = GetValue(parameters, "ProxyServerPassword", ovSettings.HttpProxyServerPassword);
            this.ProxyServerType = (ProxyServerType)GetValue(parameters, "ProxyServerType", (int)ovSettings.HttpProxyServerType);
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
            StringBuilder builder = new StringBuilder();

            builder.Append(base.ToString());
            builder.Append((this.OpenConnectionTimeout != OnlineVideoSettings.Instance.HttpOpenConnectionTimeout) ? String.Format("OpenConnectionTimeout={0};", this.OpenConnectionTimeout) : String.Empty);
            builder.Append((this.OpenConnectionSleepTime != OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime) ? String.Format("OpenConnectionSleepTime={0};", this.OpenConnectionSleepTime) : String.Empty);
            builder.Append((this.TotalReopenConnectionTimeout != OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout) ? String.Format("TotalReopenConnectionTimeout={0};", this.TotalReopenConnectionTimeout) : String.Empty);

            builder.Append((this.ServerAuthenticate != OnlineVideoSettings.Instance.HttpServerAuthenticate) ? String.Format("ServerAuthenticate={0};", this.ServerAuthenticate) : String.Empty);
            builder.Append((this.ServerUserName != OnlineVideoSettings.Instance.HttpServerUserName) ? String.Format("ServerUserName={0};", this.ServerUserName) : String.Empty);
            builder.Append((this.ServerPassword != OnlineVideoSettings.Instance.HttpServerPassword) ? String.Format("ServerPassword={0};", this.ServerPassword) : String.Empty);

            builder.Append((this.ProxyServerAuthenticate != OnlineVideoSettings.Instance.HttpProxyServerAuthenticate) ? String.Format("ProxyServerAuthenticate={0};", this.ProxyServerAuthenticate) : String.Empty);
            builder.Append((this.ProxyServer != OnlineVideoSettings.Instance.HttpProxyServer) ? String.Format("ProxyServer={0};", this.ProxyServer) : String.Empty);
            builder.Append((this.ProxyServerPort != OnlineVideoSettings.Instance.HttpProxyServerPort) ? String.Format("ProxyServerPort={0};", this.ProxyServerPort) : String.Empty);
            builder.Append((this.ProxyServerUserName != OnlineVideoSettings.Instance.HttpProxyServerUserName) ? String.Format("ProxyServerUserName={0};", this.ProxyServerUserName) : String.Empty);
            builder.Append((this.ProxyServerPassword != OnlineVideoSettings.Instance.HttpProxyServerPassword) ? String.Format("ProxyServerPassword={0};", this.ProxyServerPassword) : String.Empty);
            builder.Append((this.ProxyServerType != OnlineVideoSettings.Instance.HttpProxyServerType) ? String.Format("ProxyServerType={0};", (int)this.ProxyServerType) : String.Empty);

            return builder.ToString();
        }

        internal void Apply(HttpUrl httpUrl)
        {
            base.Apply(httpUrl);

            httpUrl.OpenConnectionSleepTime = this.OpenConnectionSleepTime;
            httpUrl.OpenConnectionTimeout = this.OpenConnectionTimeout;
            httpUrl.TotalReopenConnectionTimeout = this.TotalReopenConnectionTimeout;

            httpUrl.ServerAuthenticate = this.ServerAuthenticate;
            httpUrl.ServerUserName = this.ServerUserName;
            httpUrl.ServerPassword = this.ServerPassword;

            httpUrl.ProxyServerAuthenticate = this.ProxyServerAuthenticate;
            httpUrl.ProxyServer = this.ProxyServer;
            httpUrl.ProxyServerPort = this.ProxyServerPort;
            httpUrl.ProxyServerUserName = this.ProxyServerUserName;
            httpUrl.ProxyServerPassword = this.ProxyServerPassword;
            httpUrl.ProxyServerType = this.ProxyServerType;
        }

        #endregion
    }
}
