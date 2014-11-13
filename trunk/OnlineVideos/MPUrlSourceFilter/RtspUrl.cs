using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represent base class for RTSP urls for MediaPortal Url Source Splitter.
    /// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
    /// </summary>
    [Serializable]
    public class RtspUrl : SimpleUrl
    {
        #region Private fields

        private int multicastPreference;
        private int udpPreference;
        private int sameConnectionPreference;

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

            this.MulticastPreference = RtspUrl.DefaultMulticastPreference;
            this.UdpPreference = RtspUrl.DefaultUdpPreference;
            this.SameConnectionPreference = RtspUrl.DefaultSameConnectionTcpPreference;
            this.IgnorePayloadType = RtspUrl.DefaultIgnoreRtpPayloadType;
        }

        #endregion

        #region Properties

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
        #endregion

        #region Constants

        // common parameters of RTSP protocol for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies UDP multicast connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterMulticastPreference = "RtspMulticastPreference";

        /// <summary>
        /// Specifies UDP connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterUdpPreference = "RtspUdpPreference";

        /// <summary>
        /// Specifies same connection preference of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterSameConnectionTcpPreference = "RtspSameConnectionTcpPreference";

        /// <summary>
        /// Specifies ignore RTP payload type flag of RTSP protocol.
        /// </summary>
        protected static readonly String ParameterIgnoreRtpPayloadType = "RtspIgnoreRtpPayloadType";

        /// <summary>
        /// Default UDP multicast connection preference.
        /// </summary>
        public const int  DefaultMulticastPreference = 2;

        /// <summary>
        /// Default UDP connection preference.
        /// </summary>
        public const int  DefaultUdpPreference = 1;

        /// <summary>
        /// Default same connection preference.
        /// </summary>
        public const int  DefaultSameConnectionTcpPreference = 0;

        /// <summary>
        /// Default ignore payload type flag.
        /// </summary>
        public const Boolean DefaultIgnoreRtpPayloadType = false;

        #endregion
    }
}
