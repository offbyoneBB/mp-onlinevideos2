using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2
{
    /// <summary>
    /// Represents class for creating URL objects from specified string.
    /// </summary>
    internal static class UrlFactory
    {
        #region Private fields
        #endregion

        #region Constructors

        static UrlFactory()
        {
            UrlFactory.SupportedProtocols = new Hashtable();

            UrlFactory.SupportedProtocols.Add("HTTP", "HTTP");
            UrlFactory.SupportedProtocols.Add("HTTPS", "HTTP");

            UrlFactory.SupportedProtocols.Add("RTMP", "RTMP");
            UrlFactory.SupportedProtocols.Add("RTMPT", "RTMP");
            UrlFactory.SupportedProtocols.Add("RTMPE", "RTMP");
            UrlFactory.SupportedProtocols.Add("RTMPTE", "RTMP");
            UrlFactory.SupportedProtocols.Add("RTMPS", "RTMP");
            UrlFactory.SupportedProtocols.Add("RTMPTS", "RTMP");

            UrlFactory.SupportedProtocols.Add("RTSP", "RTSP");

            UrlFactory.SupportedProtocols.Add("UDP", "UDP");
            UrlFactory.SupportedProtocols.Add("RTP", "UDP");
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        public static SimpleUrl CreateUrl(String url)
        {
            // no special form of URL
            // in this case check URI scheme

            Uri uri = new Uri(url);
            String scheme = (String)UrlFactory.SupportedProtocols[uri.Scheme.ToUpperInvariant()];

            switch (scheme)
            {
                case "HTTP":
                    return new HttpUrl(url);
                case "RTSP":
                    return new RtspUrl(url);
                case "RTMP":
                    return new RtmpUrl(url);
                case "UDP":
                    return new UdpRtpUrl(url);
                default:
                    return null;
            }
        }

        #endregion

        #region Constants

        public static readonly Hashtable SupportedProtocols = null;

        #endregion
    }
}
