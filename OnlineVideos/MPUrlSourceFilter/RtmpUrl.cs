using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MPUrlSourceFilter
{
	/// <summary>
    /// Represent class for RTMP urls for MediaPortal Url Source Splitter.
	/// All parameter values will be url encoded, so make sure you set them UrlDecoded!
	/// </summary>
    [Serializable]
	public class RtmpUrl : SimpleUrl
	{
        #region Private fields

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
        [Category("librtmp"), Description("Name of application to connect to on the RTMP server. Sometimes the librtmp URL parser cannot determine the app name automatically, so it must be given explicitly using this option.")]
        public String App { get; set; }

        /// <summary>
        /// Gets arbitray data collection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default value is empty collection.
        /// </para>
        /// </remarks>
        [Category("librtmp"), Description("Arbitrary Data appended to the connect packet.")]
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
        [Category("librtmp"), Description("The URL of the target stream. If not set, rtmp[t][e|s]://host[:port]/app is used.")]
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
        [Category("librtmp"), Description("The URL of the web page in which the media was embedded.")]
        public String PageUrl { get; set; }

        /// <summary>
        /// Gets or sets URL of the SWF player for the media.
        /// </summary>
        /// <remarks>
        /// <para>If is <see langword="null"/> then no value will be sent.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        [Category("librtmp"), Description("URL of the SWF player for the media. Used for SWF Verification.")]
        public String SwfUrl { get; set; }

        /// <summary>
        /// Gets or sets the version of the Flash plugin used to run the SWF player.
        /// </summary>
        /// <remarks>
        /// <para>If is <see langword="null"/> then "WIN 10,0,32,18" is sent.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        [Category("librtmp"), Description("The version of the Flash plugin used to run the SWF player. Default is 'WIN 10,0,32,18'")]
        public String FlashVersion { get; set; }

        /// <summary>
        /// Gets or sets the authentication string to be appended to the connect string.
        /// </summary>
        /// <remarks>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        [Category("librtmp"), Description("Authentication string to be appended to the connect string.")]
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
        [Category("librtmp"), Description("Overrides playpath that is parsed from RTMP URL, as sometimes the librtmp URL parser cannot determine the correct playpath automatically.")]
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
        [Category("librtmp"), DefaultValue(false), Description("When true the set_playlist command has to be sent before the play command.")]
        public Boolean Playlist { get; set; }

        /// <summary>
        /// Specify that the media is a live stream.
        /// </summary>
        /// <remarks>
        /// <para>No resuming or seeking in live streams is possible.</para>
        /// <para>The default value is <see langword="false"/>.</para>
        /// </remarks>
        [Category("librtmp"), DefaultValue(false), Description("Has to be set to true when the media is a live stream.")]
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
        [Category("librtmp"), Description("Name of live stream to subscribe to - defaults to playpath.")]
        public String Subscribe { get; set; }

        /// <summary>
        /// Gets or sets the start into the stream.
        /// </summary>
        /// <remarks>
        /// <para>Start at seconds into the stream. Not valid for live streams.</para>
        /// <para>The default value is <see cref="uint"/>.<see cref="uint.MaxValue"/>, which means that value is not set.</para>
        /// </remarks>
        [Category("librtmp"), DefaultValue(uint.MaxValue), Description("Start at seconds into the stream. Not valid for live streams.")]
        public uint Start { get; set; }

        /// <summary>
        /// Gets or sets the stop into the stream.
        /// </summary>
        /// <remarks>
        /// <para>Stop at seconds into the stream.</para>
        /// <para>The default value is <see cref="uint"/>.<see cref="uint.MaxValue"/>, which means that value is not set.</para>
        /// </remarks>
        [Category("librtmp"), DefaultValue(uint.MaxValue), Description("Stop at seconds into the stream.")]
        public uint Stop { get; set; }

        /// <summary>
        /// Gets or sets the buffer time.
        /// </summary>
        /// <remarks>
        /// <para>Buffer time is in milliseconds.</para>
        /// <para>The default value is <see cref="RtmpUrl.DefaultBufferTime"/>.</para>
        /// </remarks>
        [Category("librtmp"), DefaultValue(DefaultBufferTime), Description("milliseconds to buffer")]
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
        [Category("librtmp"), Description("The key for SecureToken response if the server requires SecureToken authentication.")]
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets the JSON token used by legacy Justin.tv servers.
        /// </summary>
        /// <remarks>
        /// <para>JSON token used by legacy Justin.tv servers. Invokes NetStream.Authenticate.UsherToken.</para>
        /// <para>The default value is <see langword="null"/>.</para>
        /// </remarks>
        [Category("librtmp"), Description("The JSON token used by legacy Justin.tv servers. Invokes NetStream.Authenticate.UsherToken.")]
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
        [Category("librtmp"), DefaultValue(DefaultSwfVerify), Description("If true, the SWF player is retrieved from the specified SwfUrl for performing SWF verification. The SWF hash and size (used in the verification step) are computed automatically.")]
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
        [Category("librtmp"), DefaultValue(DefaultSwfAge), Description("Specify how many days to use the cached SWF info before re-checking.")]
        public uint SwfAge { get; set; }

		#endregion

		#region Methods

        /// <summary>
        /// Parses back a string that was created via <see cref="ToString"/> into a <see cref="RtmpUrl"/> instance with all the parameters.
        /// </summary>
        /// <param name="url">The Url string to parse.</param>
        /// <returns>A <see cref="RtmpUrl"/> instance with the parameters from the url.</returns>
        public static RtmpUrl Parse(string url)
        {
            //if (!url.Contains(SimpleUrl.ParameterSeparator)) return new RtmpUrl(url);
            //else
            //{
            //    string[] url_split = url.Split(new string[] {SimpleUrl.ParameterSeparator}, StringSplitOptions.None);
            //    RtmpUrl rtmpUrl = new RtmpUrl(url_split[0]);
            //    string[] parameters = url_split[1].Split(new string[] { ParameterCollection.ParameterSeparator }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (string parameter in parameters)
            //    {
            ////        string[] param_value = parameter.Split(new string[] { Parameter.ParameterAssign }, StringSplitOptions.None);
            ////        string paramName = param_value[0];
            ////        string paramValue = System.Web.HttpUtility.UrlDecode(param_value[1]);
            ////        if (paramName == RtmpUrl.ParameterApp) rtmpUrl.App = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterBufferTime) rtmpUrl.BufferTime = uint.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterFlashVer) rtmpUrl.FlashVersion = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterAuth) rtmpUrl.Auth = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterJtv) rtmpUrl.Jtv = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterLive) rtmpUrl.Live = paramValue == "1";
            ////        else if (paramName == RtmpUrl.ParameterOpenConnectionMaximumAttempts) rtmpUrl.OpenConnectionMaximumAttempts = int.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterPageUrl) rtmpUrl.PageUrl = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterPlaylist) rtmpUrl.Playlist = paramValue == "1";
            ////        else if (paramName == RtmpUrl.ParameterPlayPath) rtmpUrl.PlayPath = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterReceiveDataTimeout) rtmpUrl.ReceiveDataTimeout = int.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterStart) rtmpUrl.Start = uint.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterStop) rtmpUrl.Stop = uint.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterSubscribe) rtmpUrl.Subscribe = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterSwfAge) rtmpUrl.SwfAge = uint.Parse(paramValue);
            ////        else if (paramName == RtmpUrl.ParameterSwfUrl) rtmpUrl.SwfUrl = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterSwfVerify) rtmpUrl.SwfVerify = paramValue == "1";
            ////        else if (paramName == RtmpUrl.ParameterTcUrl) rtmpUrl.TcUrl = paramValue;
            ////        else if (paramName == RtmpUrl.ParameterToken) rtmpUrl.Token = paramValue;
            ////        else if (paramName == SimpleUrl.ParameterLogVerbosity) rtmpUrl.Verbosity = (LogVerbosity)Enum.Parse(typeof(LogVerbosity), paramValue);
            ////        else if (paramName == SimpleUrl.ParameterMaximumLogSize) rtmpUrl.MaximumLogSize = int.Parse(paramValue);
            ////        else if (paramName == SimpleUrl.ParameterMaximumPlugins) rtmpUrl.MaximumPlugins = int.Parse(paramValue);
            ////        else if (paramName == SimpleUrl.ParameterNetworkInterface) rtmpUrl.NetworkInterface = paramValue;
            //    }
            //    return rtmpUrl;
            //}

            return null;
        }

		#endregion

        #region Constants

        // common parameters of RTMP protocol for MediaPortal Url Source Splitter

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
