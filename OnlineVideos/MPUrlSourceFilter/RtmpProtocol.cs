using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents supported RTMP protocols.
    /// </summary>
    [Serializable]
    internal enum RtmpProtocol
    {
        /// <summary>
        /// Standard RTMP protocol.
        /// </summary>
        RTMP,

        /// <summary>
        /// RTMP protocol over HTTP.
        /// </summary>
        RTMPT,

        /// <summary>
        /// RTMP protocol encrypted using Adobe's own security mechanism.
        /// </summary>
        RTMPE,

        /// <summary>
        /// RTMP protocol encrypted using Adobe's own security mechanism over HTTP.
        /// </summary>
        RTMPTE,

        /// <summary>
        /// RTMP protocol over a secure SSL connection using HTTP.
        /// </summary>
        RTMPS,

        /// <summary>
        /// RTMP protocol over a secure SSL connection using HTTPS.
        /// </summary>
        RTMPTS
    }
}
