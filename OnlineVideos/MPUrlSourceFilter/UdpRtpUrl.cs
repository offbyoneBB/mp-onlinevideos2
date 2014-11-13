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
        #endregion

        #region Methods
        #endregion

        #region Constants
        #endregion
    }

}
