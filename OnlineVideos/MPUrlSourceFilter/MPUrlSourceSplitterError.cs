using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Enumeration of possible MediaPortal Url Source Splitter errors.
    /// </summary>
    public enum MPUrlSourceSplitterError
    {
        /// <summary>
        /// None error, success.
        /// </summary>
        None                                                                        = 0,

        /// <summary>
        /// Invalid configuration.
        /// </summary>
        InvalidConfiguration                                                        = -1,

        /// <summary>
        /// Url not specified.
        /// </summary>
        UrlNotSpecified                                                             = -2,

        /// <summary>
        /// Data from splitter to FFmpeg are bigger than requested.
        /// </summary>
        ReturnedDataLengthBiggerThanRequested                                       = -3,

        /// <summary>
        /// No more data available.
        /// </summary>
        NoMoreDataAvailable                                                         = -4,

        /// <summary>
        /// Requested data after end of stream.
        /// </summary>
        RequestedDataAfterTotalLength                                               = -5,

        /// <summary>
        /// Timeout occured.
        /// </summary>
        /// <remarks>
        /// Error code is same as VFW_E_TIMEOUT.
        /// </remarks>
        Timeout                                                                     = -2147220946,

        /// <summary>
        /// Demuxer worker requested to stop work.
        /// </summary>
        DemuxerWorkerStopRequest                                                    = -6,

        /// <summary>
        /// No data available, no data received.
        /// </summary>
        NoDataAvailable                                                             = -20,

        /// <summary>
        /// Parser still pending with result.
        /// </summary>
        ParserStillPending                                                          = -21,

        /// <summary>
        /// Stream is DRM protected.
        /// </summary>
        DrmProtected                                                                = -22,

        /// <summary>
        /// Unknown stream type.
        /// </summary>
        /// <remarks>
        /// Unknown stream type can occure when specific stream type have to be returned,
        /// but another stream type is actually returned. Typical example is Adobe Flash HTTP streaming protocol,
        /// where FLV stream type have to be returned.
        /// </remarks>
        UnknownStreamType                                                           = -23,

        /// <summary>
        /// Connection is lost, new connection cannot be opened.
        /// </summary>
        ConnectionLostCannotReopen                                                  = -24,

        /// <summary>
        /// No protocol loaded.
        /// </summary>
        NoProtocolLoaded                                                            = -30,

        /// <summary>
        /// Protocol cannot be determined for specified URL.
        /// </summary>
        NoActiveProtocol                                                            = -31,

        /// <summary>
        /// All data received, unknown stream type.
        /// </summary>
        DemuxerNotCreatedAllDataReceived                                            = -10,

        /// <summary>
        /// Cannot convert string.
        /// </summary>
        ConvertStringError                                                          = -11
    }
}
