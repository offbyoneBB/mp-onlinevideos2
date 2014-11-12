using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2
{
    /// <summary>
    /// Represent class for RTMP urls for MediaPortal Url Source Splitter.
    /// All parameter values will be url encoded, so make sure you set them UrlDecoded!
    /// </summary>
    internal class RtmpUrl : SimpleUrl
    {
        #region Private fields

        private int openConnectionTimeout = RtmpUrl.DefaultRtmpOpenConnectionTimeout;
        private int openConnectionSleepTime = RtmpUrl.DefaultRtmpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = RtmpUrl.DefaultRtmpTotalReopenConnectionTimeout;
        private RtmpArbitraryDataCollection arbitraryData;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpUrl"/> class.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpUrl"/> class.
        /// </overloads>
        public RtmpUrl(String url)
            : this(new Uri(url))
        {
        }

        public RtmpUrl(string tcUrl, string hostname, int port)
            : this(new Uri((!string.IsNullOrEmpty(tcUrl) ? new Uri(tcUrl).Scheme : "rtmp") + "://" + hostname + (port > 0 ? ":" + port : "")))
        {
            this.TcUrl = tcUrl;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpUrl"/> class.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentException">
        /// <para>The protocol supplied by <paramref name="uri"/> is not supported.</para>
        /// </exception>
        public RtmpUrl(Uri uri)
            : base(uri)
        {
            if (!this.Uri.Scheme.StartsWith("rtmp", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }

            this.App = RtmpUrl.DefaultApp;
            this.TcUrl = RtmpUrl.DefaultTcUrl;
            this.PageUrl = RtmpUrl.DefaultPageUrl;
            this.SwfUrl = RtmpUrl.DefaultSwfUrl;
            this.FlashVersion = RtmpUrl.DefaultFlashVersion;
            this.PlayPath = RtmpUrl.DefaultPlayPath;
            this.Playlist = RtmpUrl.DefaultPlaylist;
            this.Live = RtmpUrl.DefaultLive;
            this.Subscribe = RtmpUrl.DefaultSubscribe;
            this.Start = RtmpUrl.DefaultStart;
            this.Stop = RtmpUrl.DefaultStop;
            this.BufferTime = RtmpUrl.DefaultBufferTime;
            this.Token = RtmpUrl.DefaultToken;
            this.Jtv = RtmpUrl.DefaultJtv;
            this.SwfVerify = RtmpUrl.DefaultSwfVerify;
            this.SwfAge = RtmpUrl.DefaultSwfAge;
            this.arbitraryData = new RtmpArbitraryDataCollection();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of application to connect to on the RTMP server.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If not <see langword="null"/> then overrides the app in the RTMP URL.
        /// Sometimes the librtmp URL parser cannot determine the app name automatically,
        /// so it must be given explicitly using this option.
        /// </para>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public String App { get; set; }

        /// <summary>
        /// Gets arbitray data collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is empty collection.
        /// </para>
        /// </remarks>
        public RtmpArbitraryDataCollection ArbitraryData
        {
            get { return this.arbitraryData; }
        }

        /// <summary>
        /// Gets or sets the URL of the target stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If is <see langword="null"/> then rtmp[t][e|s]://host[:port]/app is used.
        /// </para>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public String TcUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL of the web page in which the media was embedded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If is <see langword="null"/> then no value will be sent.
        /// </para>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public String PageUrl { get; set; }

        /// <summary>
        /// Gets or sets URL of the SWF player for the media.
        /// </summary>
        /// <remarks>
        /// <para>If is <see langword="null"/> then no value will be sent.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        public String SwfUrl { get; set; }

        /// <summary>
        /// Gets or sets the version of the Flash plugin used to run the SWF player.
        /// </summary>
        /// <remarks>
        /// <para>If is <see langword="null"/> then "WIN 10,0,32,18" is sent.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        public String FlashVersion { get; set; }

        /// <summary>
        /// Gets or sets the authentication string to be appended to the connect string.
        /// </summary>
        /// <remarks>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        public String Auth { get; set; }

        /// <summary>
        /// Gets or sets the playpath.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If not <see langword="null"/> then overrides the playpath parsed from the RTMP URL.
        /// Sometimes the librtmp URL parser cannot determine the correct playpath automatically,
        /// so it must be given explicitly using this option.
        /// </para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        public String PlayPath { get; set; }

        /// <summary>
        /// Gets or sets if set_playlist command have to be sent before play command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="true"/>, issue a set_playlist command before sending the play command.
        /// The playlist will just contain the current playpath.
        /// If the value is <see langword="false"/>, the set_playlist command will not be sent.
        /// </para>
        /// <para>The default value is <see langword="false"/>.</para>
        /// </remarks>
        public Boolean Playlist { get; set; }

        /// <summary>
        /// Specify that the media is a live stream.
        /// </summary>
        /// <remarks>
        /// <para>No resuming or seeking in live streams is possible.</para>
        /// <para>The default value is <see langword="false"/>.</para>
        /// </remarks>
        public Boolean Live { get; set; }

        /// <summary>
        /// Gets or sets the name of live stream to subscribe to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defaults to playpath.
        /// </para>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public String Subscribe { get; set; }

        /// <summary>
        /// Gets or sets the start into the stream.
        /// </summary>
        /// <remarks>
        /// <para>Start at seconds into the stream. Not valid for live streams.</para>
        /// <para>The default value is <see cref="uint"/>.<see cref="uint.MaxValue"/>, which means that value is not set.</para>
        /// </remarks>
        public uint Start { get; set; }

        /// <summary>
        /// Gets or sets the stop into the stream.
        /// </summary>
        /// <remarks>
        /// <para>Stop at seconds into the stream.</para>
        /// <para>The default value is <see cref="uint"/>.<see cref="uint.MaxValue"/>, which means that value is not set.</para>
        /// </remarks>
        public uint Stop { get; set; }

        /// <summary>
        /// Gets or sets the buffer time.
        /// </summary>
        /// <remarks>
        /// <para>Buffer time is in milliseconds.</para>
        /// <para>The default value is <see cref="RtmpUrl.DefaultBufferTime"/>.</para>
        /// </remarks>
        public uint BufferTime { get; set; }

        /// <summary>
        /// Gets or sets the key for SecureToken response.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used if the server requires SecureToken authentication.
        /// </para>
        /// <para>
        /// The default value is <see langword="null"/>.
        /// </para>
        /// </remarks>
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the JSON token used by legacy Justin.tv servers.
        /// </summary>
        /// <remarks>
        /// <para>JSON token used by legacy Justin.tv servers. Invokes NetStream.Authenticate.UsherToken.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        public String Jtv { get; set; }

        /// <summary>
        /// Gets or sets if the SWF player have to be retrieved from <see cref="RtmpUrl.SwfUrl"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the value is <see langword="true"/>, the SWF player is retrieved from the specified <see cref="RtmpUrl.SwfUrl"/>
        /// for performing SWF verification. The SWF hash and size (used in the verification step) are computed automatically.
        /// Also the SWF information is cached in a .swfinfo file in the user's home directory,
        /// so that it doesn't need to be retrieved and recalculated every time.
        /// The .swfinfo file records the SWF URL, the time it was fetched,
        /// the modification timestamp of the SWF file, its size, and its hash.
        /// By default, the cached info will be used for 30 days before re-checking. 
        /// </para>
        /// <para>
        /// The default value is <see cref="RtmpUrl.DefaultSwfVerify"/>.
        /// </para>
        /// </remarks>
        public Boolean SwfVerify { get; set; }

        /// <summary>
        /// Gets or sets how many days to use cached SWF info before re-checking.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Specify how many days to use the cached SWF info before re-checking.
        /// Use 0 to always check the SWF URL.
        /// Note that if the check shows that the SWF file has the same modification timestamp as before,
        /// it will not be retrieved again.
        /// </para>
        /// <para>
        /// The default value is <see cref="RtmpUrl.DefaultSwfAge"/>.
        /// </para>
        /// </remarks>
        public uint SwfAge { get; set; }

        /// <summary>
        /// Gets or sets the timeout to open RTMP url in milliseconds.
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
        /// Gets or sets the total timeout to open RTMP url in milliseconds.
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

            if (this.App != RtmpUrl.DefaultApp)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterApp, this.App));
            }

            if (this.BufferTime != RtmpUrl.DefaultBufferTime)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterBufferTime, this.BufferTime.ToString()));
            }

            if (this.FlashVersion != RtmpUrl.DefaultFlashVersion)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterFlashVer, this.FlashVersion));
            }

            if (this.Auth != RtmpUrl.DefaultAuth)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterAuth, this.Auth));
            }

            if (this.ArbitraryData.Count != 0)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterArbitraryData, this.ArbitraryData.ToString()));
            }

            if (this.Jtv != RtmpUrl.DefaultJtv)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterJtv, this.Jtv));
            }

            if (this.Live != RtmpUrl.DefaultLive)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterLive, this.Live ? "1" : "0"));
            }

            if (this.OpenConnectionTimeout != RtmpUrl.DefaultRtmpOpenConnectionTimeout)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterRtmpOpenConnectionTimeout, this.OpenConnectionTimeout.ToString()));
            }

            if (this.OpenConnectionSleepTime != RtmpUrl.DefaultRtmpOpenConnectionSleepTime)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterRtmpOpenConnectionSleepTime, this.OpenConnectionSleepTime.ToString()));
            }

            if (this.TotalReopenConnectionTimeout != RtmpUrl.DefaultRtmpTotalReopenConnectionTimeout)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterRtmpTotalReopenConnectionTimeout, this.TotalReopenConnectionTimeout.ToString()));
            }

            if (this.PageUrl != RtmpUrl.DefaultPageUrl)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterPageUrl, this.PageUrl));
            }

            if (this.Playlist != RtmpUrl.DefaultPlaylist)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterPlaylist, this.Playlist ? "1" : "0"));
            }

            if (this.PlayPath != RtmpUrl.DefaultPlayPath)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterPlayPath, this.PlayPath));
            }

            if (this.Start != RtmpUrl.DefaultStart)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterStart, this.Start.ToString()));
            }

            if (this.Stop != RtmpUrl.DefaultStop)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterStop, this.Stop.ToString()));
            }

            if (this.Subscribe != RtmpUrl.DefaultSubscribe)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterSubscribe, this.Subscribe));
            }

            if (this.SwfAge != RtmpUrl.DefaultSwfAge)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterSwfAge, this.SwfAge.ToString()));
            }

            if (this.SwfUrl != RtmpUrl.DefaultSwfUrl)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterSwfUrl, this.SwfUrl));
            }

            if (this.SwfVerify != RtmpUrl.DefaultSwfVerify)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterSwfVerify, this.SwfVerify ? "1" : "0"));
            }

            if (this.TcUrl != RtmpUrl.DefaultTcUrl)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterTcUrl, this.TcUrl));
            }

            if (this.Token != RtmpUrl.DefaultToken)
            {
                parameters.Add(new Parameter(RtmpUrl.ParameterToken, this.Token));
            }

            // return formatted connection string
            return base.ToString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        ///// <summary>
        ///// Parses back a string that was created via <see cref="ToString"/> into a <see cref="RtmpUrl"/> instance with all the parameters.
        ///// </summary>
        ///// <param name="url">The Url string to parse.</param>
        ///// <returns>A <see cref="RtmpUrl"/> instance with the parameters from the url.</returns>
        //public static RtmpUrl Parse(string url)
        //{
        //    if (!url.Contains(SimpleUrl.ParameterSeparator)) return new RtmpUrl(url);
        //    else
        //    {
        //        string[] url_split = url.Split(new string[] { SimpleUrl.ParameterSeparator }, StringSplitOptions.None);
        //        RtmpUrl rtmpUrl = new RtmpUrl(url_split[0]);
        //        string[] parameters = url_split[1].Split(new string[] { ParameterCollection.ParameterSeparator }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string parameter in parameters)
        //        {
        //            string[] param_value = parameter.Split(new string[] { Parameter.ParameterAssign }, StringSplitOptions.None);
        //            string paramName = param_value[0];
        //            string paramValue = System.Web.HttpUtility.UrlDecode(param_value[1]);
        //            if (paramName == RtmpUrl.ParameterApp) rtmpUrl.App = paramValue;
        //            else if (paramName == RtmpUrl.ParameterBufferTime) rtmpUrl.BufferTime = uint.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterFlashVer) rtmpUrl.FlashVersion = paramValue;
        //            else if (paramName == RtmpUrl.ParameterAuth) rtmpUrl.Auth = paramValue;
        //            else if (paramName == RtmpUrl.ParameterJtv) rtmpUrl.Jtv = paramValue;
        //            else if (paramName == RtmpUrl.ParameterLive) rtmpUrl.Live = paramValue == "1";
        //            else if (paramName == RtmpUrl.ParameterOpenConnectionMaximumAttempts) rtmpUrl.OpenConnectionMaximumAttempts = int.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterPageUrl) rtmpUrl.PageUrl = paramValue;
        //            else if (paramName == RtmpUrl.ParameterPlaylist) rtmpUrl.Playlist = paramValue == "1";
        //            else if (paramName == RtmpUrl.ParameterPlayPath) rtmpUrl.PlayPath = paramValue;
        //            else if (paramName == RtmpUrl.ParameterReceiveDataTimeout) rtmpUrl.ReceiveDataTimeout = int.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterStart) rtmpUrl.Start = uint.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterStop) rtmpUrl.Stop = uint.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterSubscribe) rtmpUrl.Subscribe = paramValue;
        //            else if (paramName == RtmpUrl.ParameterSwfAge) rtmpUrl.SwfAge = uint.Parse(paramValue);
        //            else if (paramName == RtmpUrl.ParameterSwfUrl) rtmpUrl.SwfUrl = paramValue;
        //            else if (paramName == RtmpUrl.ParameterSwfVerify) rtmpUrl.SwfVerify = paramValue == "1";
        //            else if (paramName == RtmpUrl.ParameterTcUrl) rtmpUrl.TcUrl = paramValue;
        //            else if (paramName == RtmpUrl.ParameterToken) rtmpUrl.Token = paramValue;
        //            else if (paramName == SimpleUrl.ParameterLogVerbosity) rtmpUrl.Verbosity = (LogVerbosity)Enum.Parse(typeof(LogVerbosity), paramValue);
        //            else if (paramName == SimpleUrl.ParameterLogMaxSize) rtmpUrl.MaximumLogSize = int.Parse(paramValue);
        //            else if (paramName == SimpleUrl.ParameterMaximumPlugins) rtmpUrl.MaximumPlugins = int.Parse(paramValue);
        //            else if (paramName == SimpleUrl.ParameterNetworkInterface) rtmpUrl.NetworkInterface = paramValue;
        //        }
        //        return rtmpUrl;
        //    }
        //}

        #endregion

        #region Constants

        // common parameters of RTMP protocol for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies open connection timeout in milliseconds.
        /// </summary>
        protected static readonly String ParameterRtmpOpenConnectionTimeout = "RtmpOpenConnectionTimeout";

        /// <summary>
        /// Specifies the time in milliseconds to sleep before opening connection.
        /// </summary>
        protected static readonly String ParameterRtmpOpenConnectionSleepTime = "RtmpOpenConnectionSleepTime";

        /// <summary>
        /// Specifies the total timeout to open RTMP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.
        /// </summary>
        protected static readonly String ParameterRtmpTotalReopenConnectionTimeout = "RtmpTotalReopenConnectionTimeout";

        // connection parameters of RTMP protocol

        protected static String ParameterApp = "RtmpApp";

        protected static String ParameterTcUrl = "RtmpTcUrl";

        protected static String ParameterPageUrl = "RtmpPageUrl";

        protected static String ParameterSwfUrl = "RtmpSwfUrl";

        protected static String ParameterFlashVer = "RtmpFlashVer";

        protected static String ParameterAuth = "RtmpAuth";

        protected static String ParameterArbitraryData = "RtmpArbitraryData";

        // session parameters of RTMP protocol

        protected static String ParameterPlayPath = "RtmpPlayPath";

        protected static String ParameterPlaylist = "RtmpPlaylist";

        protected static String ParameterLive = "RtmpLive";

        protected static String ParameterSubscribe = "RtmpSubscribe";

        protected static String ParameterStart = "RtmpStart";

        protected static String ParameterStop = "RtmpStop";

        protected static String ParameterBufferTime = "RtmpBuffer";

        // security parameters of RTMP protocol

        protected static String ParameterToken = "RtmpToken";

        protected static String ParameterJtv = "RtmpJtv";

        protected static String ParameterSwfVerify = "RtmpSwfVerify";

        protected static String ParameterSwfAge = "RtmpSwfAge";

        // default values for some parameters

        /// <summary>
        /// Default value for <see cref="ParameterRtmpOpenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultRtmpOpenConnectionTimeout = 20000;

        /// <summary>
        /// Default value for <see cref="ParameterRtmpOpenConnectionSleepTime"/>.
        /// </summary>
        public static readonly int DefaultRtmpOpenConnectionSleepTime = 0;

        /// <summary>
        /// Default value for <see cref="ParameterHttpTotalReopenConnectionTimeout"/>.
        /// </summary>
        public static readonly int DefaultRtmpTotalReopenConnectionTimeout = 60000;

        public static String DefaultApp = null;
        public static String DefaultTcUrl = null;
        public static String DefaultPageUrl = null;
        public static String DefaultSwfUrl = null;
        public static String DefaultFlashVersion = null;
        public static String DefaultAuth = null;
        public static String DefaultPlayPath = null;
        public const Boolean DefaultPlaylist = false;
        public const Boolean DefaultLive = false;
        public static String DefaultSubscribe = null;
        public const uint DefaultStart = uint.MaxValue;
        public const uint DefaultStop = uint.MaxValue;
        public const uint DefaultBufferTime = 30000;
        public static String DefaultToken = null;
        public static String DefaultJtv = null;
        public const Boolean DefaultSwfVerify = false;
        public const uint DefaultSwfAge = 0;

        #endregion
    }
}
