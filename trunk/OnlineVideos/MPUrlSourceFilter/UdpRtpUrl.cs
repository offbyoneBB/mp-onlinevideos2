using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represent base class for UDP or RTP urls for MediaPortal Url Source Splitter.
    /// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
    /// </summary>
    [Serializable]
    public class UdpRtpUrl : SimpleUrl
    {
        #region Private fields

        private int receiveDataCheckInterval = UdpRtpUrl.DefaultUdpReceiveDataCheckInterval;
        private int openConnectionTimeout = UdpRtpUrl.DefaultUdpOpenConnectionTimeout;
        private int openConnectionSleepTime = UdpRtpUrl.DefaultUdpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = UdpRtpUrl.DefaultUdpTotalReopenConnectionTimeout;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="UdpRtpUrl"/> class.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="UdpRtpUrl"/> class.
        /// </overloads>
        public UdpRtpUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UdpRtpUrl"/> class.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentException">
        /// <para>The protocol supplied by <paramref name="uri"/> is not supported.</para>
        /// </exception>
        public UdpRtpUrl(Uri uri)
            : base(uri)
        {
            if ((this.Uri.Scheme != "udp") && (this.Uri.Scheme != "rtp"))
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the timeout to open UDP or RTP url in milliseconds.
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
        /// Gets or sets the total timeout to open UDP or RTP url in milliseconds.
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
        /// Gets or sets the receive data check interval.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="ReceiveDataCheckInterval"/> is lower than zero.</para>
        /// </exception>
        public int ReceiveDataCheckInterval
        {
            get { return this.receiveDataCheckInterval; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("ReceiveDataCheckInterval", value, "Cannot be less than zero.");
                }

                this.receiveDataCheckInterval = value;
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

            if (this.ReceiveDataCheckInterval != UdpRtpUrl.DefaultUdpReceiveDataCheckInterval)
            {
                parameters.Add(new Parameter(UdpRtpUrl.ParameterUdpReceiveDataCheckInterval, this.ReceiveDataCheckInterval.ToString()));
            }
            if (this.OpenConnectionTimeout != UdpRtpUrl.DefaultUdpOpenConnectionTimeout)
            {
                parameters.Add(new Parameter(UdpRtpUrl.ParameterUdpOpenConnectionTimeout, this.OpenConnectionTimeout.ToString()));
            }
            if (this.OpenConnectionSleepTime != UdpRtpUrl.DefaultUdpOpenConnectionSleepTime)
            {
                parameters.Add(new Parameter(UdpRtpUrl.ParameterUdpOpenConnectionSleepTime, this.OpenConnectionSleepTime.ToString()));
            }
            if (this.TotalReopenConnectionTimeout != UdpRtpUrl.DefaultUdpTotalReopenConnectionTimeout)
            {
                parameters.Add(new Parameter(UdpRtpUrl.ParameterUdpTotalReopenConnectionTimeout, this.TotalReopenConnectionTimeout.ToString()));
            }

            // return formatted connection string
            return base.ToFilterString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        internal override void ApplySettings(Sites.SiteUtilBase siteUtil)
        {
            siteUtil.UdpRtpSettings.Apply(this);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Specifies open connection timeout in milliseconds.
        /// </summary>
        protected static readonly String ParameterUdpOpenConnectionTimeout = "UdpOpenConnectionTimeout";

        /// <summary>
        /// Specifies the time in milliseconds to sleep before opening connection.
        /// </summary>
        protected static readonly String ParameterUdpOpenConnectionSleepTime = "UdpOpenConnectionSleepTime";

        /// <summary>
        /// Specifies the total timeout to open UDP or RTP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.
        /// </summary>
        protected static readonly String ParameterUdpTotalReopenConnectionTimeout = "UdpTotalReopenConnectionTimeout";

        /// <summary>
        /// Specifies receive data check interval.
        /// </summary>
        protected static readonly String ParameterUdpReceiveDataCheckInterval = "UdpReceiveDataCheckInterval";

        /// <summary>
        /// Default value for <see cref="ParameterUdpOpenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultUdpOpenConnectionTimeout = 2000;

        /// <summary>
        /// Default value for <see cref="ParameterUdpOpenConnectionSleepTime"/>.
        /// </summary>
        public static readonly int DefaultUdpOpenConnectionSleepTime = 0;

        /// <summary>
        /// Default value for <see cref="ParameterUdpTotalReopenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultUdpTotalReopenConnectionTimeout = 60000;

        /// <summary>
        /// Default receive data check interval.
        /// </summary>
        public static readonly int DefaultUdpReceiveDataCheckInterval = 500;

        #endregion
    }
}
