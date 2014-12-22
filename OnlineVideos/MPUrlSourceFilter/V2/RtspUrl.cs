using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.V2
{
    /// <summary>
    /// Represent base class for RTSP urls for MediaPortal Url Source Splitter.
    /// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
    /// </summary>
    internal class RtspUrl : SimpleUrl
    {
        #region Private fields

        private int multicastPreference = RtspUrl.DefaultRtspMulticastPreference;
        private int udpPreference = RtspUrl.DefaultRtspUdpPreference;
        private int sameConnectionPreference = RtspUrl.DefaultRtspSameConnectionTcpPreference;

        private int openConnectionTimeout = RtspUrl.DefaultRtspOpenConnectionTimeout;
        private int openConnectionSleepTime = RtspUrl.DefaultRtspOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = RtspUrl.DefaultRtspTotalReopenConnectionTimeout;

        private int clientPortMin = RtspUrl.DefaultRtspClientPortMin;
        private int clientPortMax = RtspUrl.DefaultRtspClientPortMax;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtspUrl"/> class.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtspUrl"/> class.
        /// </overloads>
        public RtspUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtspUrl"/> class.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentException">
        /// <para>The protocol supplied by <paramref name="uri"/> is not supported.</para>
        /// </exception>
        public RtspUrl(Uri uri)
            : base(uri)
        {
            if (this.Uri.Scheme != "rtsp")
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the timeout to open RTSP url in milliseconds.
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
        /// Gets or sets the total timeout to open RTSP url in milliseconds.
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
        /// Gets or sets the minimum client port for UDP connection.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="ClientPortMin"/> is lower than zero.</para>
        /// <para>The <see cref="ClientPortMin"/> is greater than 65535.</para>
        /// <para>The <see cref="ClientPortMin"/> is greater than <see cref="ClientPortMax"/>.</para>
        /// </exception>
        public int ClientPortMin
        {
            get { return this.clientPortMin; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be less than zero.");
                }
                if (value > 65535)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be greater than 65535.");
                }
                if (value >= this.ClientPortMax)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be greater than maximum client port.");
                }

                this.clientPortMin = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum client port for UDP connection.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="ClientPortMax"/> is lower than zero.</para>
        /// <para>The <see cref="ClientPortMax"/> is greater than 65535.</para>
        /// <para>The <see cref="ClientPortMax"/> is lower than <see cref="ClientPortMin"/>.</para>
        /// </exception>
        public int ClientPortMax
        {
            get { return this.clientPortMax; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be less than zero.");
                }
                if (value > 65535)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be greater than 65535.");
                }
                if (value <= this.ClientPortMin)
                {
                    throw new ArgumentOutOfRangeException("ClientPortMin", value, "Cannot be lower than minimum client port.");
                }

                this.clientPortMax = value;
            }
        }

        /// <summary>
        /// Specifies UDP multicast connection preference.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="MulticastPreference"/> is lower than zero.</para>
        /// </exception>
        public int MulticastPreference
        {
            get { return this.multicastPreference; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("MulticastPreference", value, "Cannot be lower than zero.");
                }

                this.multicastPreference = value;
            }
        }

        /// <summary>
        /// Specifies UDP connection preference.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="UdpPreference"/> is lower than zero.</para>
        /// </exception>
        public int UdpPreference
        {
            get { return this.udpPreference; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("UdpPreference", value, "Cannot be lower than zero.");
                }

                this.udpPreference = value;
            }
        }

        /// <summary>
        /// Specifies same connection preference.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="SameConnectionPreference"/> is lower than zero.</para>
        /// </exception>
        public int SameConnectionPreference
        {
            get { return this.sameConnectionPreference; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("SameConnectionPreference", value, "Cannot be lower than zero.");
                }

                this.sameConnectionPreference = value;
            }
        }

        /// <summary>
        /// Specifies ignore payload type flag.
        /// </summary>
        public Boolean IgnorePayloadType { get; set; }

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

            if (this.ClientPortMax != RtspUrl.DefaultRtspClientPortMax)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspClientPortMax, this.ClientPortMax.ToString()));
            }
            if (this.ClientPortMin != RtspUrl.DefaultRtspClientPortMin)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspClientPortMin, this.ClientPortMin.ToString()));
            }
            if (this.OpenConnectionTimeout != RtspUrl.DefaultRtspOpenConnectionTimeout)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspOpenConnectionTimeout, this.OpenConnectionTimeout.ToString()));
            }
            if (this.OpenConnectionSleepTime != RtspUrl.DefaultRtspOpenConnectionSleepTime)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspOpenConnectionSleepTime, this.OpenConnectionSleepTime.ToString()));
            }
            if (this.TotalReopenConnectionTimeout != RtspUrl.DefaultRtspTotalReopenConnectionTimeout)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspTotalReopenConnectionTimeout, this.TotalReopenConnectionTimeout.ToString()));
            }
            if (this.IgnorePayloadType != RtspUrl.DefaultRtspIgnoreRtpPayloadType)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspIgnoreRtpPayloadType, this.IgnorePayloadType.ToString()));
            }
            if (this.MulticastPreference != RtspUrl.DefaultRtspMulticastPreference)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspMulticastPreference, this.MulticastPreference.ToString()));
            }
            if (this.SameConnectionPreference != RtspUrl.DefaultRtspSameConnectionTcpPreference)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspSameConnectionTcpPreference, this.SameConnectionPreference.ToString()));
            }
            if (this.UdpPreference != RtspUrl.DefaultRtspUdpPreference)
            {
                parameters.Add(new Parameter(RtspUrl.ParameterRtspUdpPreference, this.UdpPreference.ToString()));
            }

            // return formatted connection string
            return base.ToString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Specifies open connection timeout in milliseconds.
        /// </summary>
        protected static readonly String ParameterRtspOpenConnectionTimeout = "RtspOpenConnectionTimeout";

        /// <summary>
        /// Specifies the time in milliseconds to sleep before opening connection.
        /// </summary>
        protected static readonly String ParameterRtspOpenConnectionSleepTime = "RtspOpenConnectionSleepTime";

        /// <summary>
        /// Specifies the total timeout to open RTSP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.
        /// </summary>
        protected static readonly String ParameterRtspTotalReopenConnectionTimeout = "RtspTotalReopenConnectionTimeout";

        /// <summary>
        /// Specifies UDP multicast connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterRtspMulticastPreference = "RtspMulticastPreference";

        /// <summary>
        /// Specifies UDP connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterRtspUdpPreference = "RtspUdpPreference";

        /// <summary>
        /// Specifies same connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterRtspSameConnectionTcpPreference = "RtspSameConnectionTcpPreference";

        /// <summary>
        /// Specifies ignore RTP payload type flag of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterRtspIgnoreRtpPayloadType = "RtspIgnoreRtpPayloadType";

        /// <summary>
        /// Specifies minimum client port for UDP connection.
        /// </summary>
        protected static readonly String ParameterRtspClientPortMin = "RtspClientPortMin";

        /// <summary>
        /// Specifies maximum client port for UDP connection.
        /// </summary>
        protected static readonly String ParameterRtspClientPortMax = "RtspClientPortMax";

        /// <summary>
        /// Default value for <see cref="ParameterRtspOpenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultRtspOpenConnectionTimeout = 20000;

        /// <summary>
        /// Default value for <see cref="ParameterRtspOpenConnectionSleepTime"/>.
        /// </summary>
        public static readonly int DefaultRtspOpenConnectionSleepTime = 0;

        /// <summary>
        /// Default value for <see cref="ParameterRtspTotalReopenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultRtspTotalReopenConnectionTimeout = 60000;

        /// <summary>
        /// Default UDP multicast connection preference.
        /// </summary>
        public static readonly int DefaultRtspMulticastPreference = 2;

        /// <summary>
        /// Default UDP connection preference.
        /// </summary>
        public static readonly int DefaultRtspUdpPreference = 1;

        /// <summary>
        /// Default same connection preference.
        /// </summary>
        public static readonly int DefaultRtspSameConnectionTcpPreference = 0;

        /// <summary>
        /// Default ignore payload type flag.
        /// </summary>
        public static readonly Boolean DefaultRtspIgnoreRtpPayloadType = false;

        /// <summary>
        /// Default minimum client port for UDP connection.
        /// </summary>
        public static readonly int DefaultRtspClientPortMin = 50000;

        /// <summary>
        /// Default maximum client port for UDP connection.
        /// </summary>
        public static readonly int DefaultRtspClientPortMax = 65535;

        #endregion
    }
}
