using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2
{
    /// <summary>
    /// Represent base class for all urls for MediaPortal Url Source Splitter.
    /// </summary>
    internal abstract class SimpleUrl
    {
        #region Private fields

        private Uri uri;
        private LogVerbosity verbosity = SimpleUrl.DefaultVerbosity;
        private String networkInterface = String.Empty;
        private String cacheFolder = String.Empty;
        private int maximumLogSize = SimpleUrl.DefaultLogMaximumSize;
        private int maximumPlugins = SimpleUrl.DefaultMaximumPlugins;
        private Boolean liveStream = SimpleUrl.DefaultLiveStream;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class with specified URL.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class.
        /// </overloads>
        public SimpleUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class with specified uniform resource identifier.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="uri"/> is <see langword="null"/>.</para>
        /// </exception>
        public SimpleUrl(Uri uri)
        {
            this.Uri = uri;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the uniform resource identifier.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="Uri"/> is <see langword="null"/>.</para>
        /// </exception>
        public Uri Uri
        {
            get { return this.uri; }
            protected set
            {
                // check if value is not null
                if (value == null)
                {
                    throw new ArgumentNullException("Uri");
                }

                this.uri = value;
            }
        }

        /// <summary>
        /// Gets or sets the verbosity level of MediaPortal Url Source Splitter.
        /// </summary>
        public LogVerbosity Verbosity
        {
            get { return this.verbosity; }
            set
            {
                this.verbosity = value;
            }
        }

        /// <summary>
        /// Gets or sets the network interface which MediaPortal Url Source Splitter have to use.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="NetworkInterface"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <remarks>
        /// The network interface have to be name of network interface card. The empty string ("") means the default system network interface.
        /// </remarks>
        public String NetworkInterface
        {
            get { return this.networkInterface; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NetworkInterface");
                }

                this.networkInterface = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum log size.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="MaximumLogSize"/> is less than zero.</para>
        /// </exception>
        public int MaximumLogSize
        {
            get { return this.maximumLogSize; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("MaximumLogSize", value, "Value cannot be less than zero.");
                }

                this.maximumLogSize = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum plugins.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="MaximumPlugins"/> is less than zero.</para>
        /// </exception>
        public int MaximumPlugins
        {
            get { return this.maximumPlugins; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("MaximumPlugins", value, "Value cannot be less than zero.");
                }

                this.maximumPlugins = value;
            }
        }

        /// <summary>
        /// Gets or sets the cache folder which MediaPortal Url Source Splitter have to use.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="CacheFolder"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <remarks>
        /// The empty string ("") means the default cache folder.
        /// </remarks>
        public String CacheFolder
        {
            get { return this.cacheFolder; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("CacheFolder");
                }

                this.cacheFolder = value;
            }
        }

        /// <summary>
        /// Gets or sets the live stream flag.
        /// </summary>
        /// <remarks>
        /// Specifying live stream flag optimize some parts of MediaPortal Url Source Splitter. In case of live stream are ignored timeouts, cache file is not needed (except the case of downloading = recording).
        /// </remarks>
        public Boolean LiveStream
        {
            get { return this.liveStream; }
            set { this.liveStream = value; }
        }

        /// <summary>
        /// Specifies if protocol have to dump input data.
        /// </summary>
        public Boolean DumpProtocolInputData { get; set; }

        /// <summary>
        /// Specifies if protocol have to dump output data.
        /// </summary>
        public Boolean DumpProtocolOutputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump input data.
        /// </summary>
        public Boolean DumpParserInputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump output data.
        /// </summary>
        public Boolean DumpParserOutputData { get; set; }

        /// <summary>
        /// Specifies if output pin(s) have to dump data.
        /// </summary>
        public Boolean DumpOutputPinData { get; set; }

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

            parameters.Add(new Parameter(SimpleUrl.ParameterUrl, this.Uri.ToString()));
            if (this.Verbosity != DefaultVerbosity)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterLogVerbosity, ((int)this.Verbosity).ToString()));
            }
            if (this.MaximumLogSize != SimpleUrl.DefaultLogMaximumSize)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterLogMaxSize, this.MaximumLogSize.ToString()));
            }
            if (this.MaximumPlugins != DefaultMaximumPlugins)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterMaximumPlugins, this.MaximumPlugins.ToString()));
            }
            if (!String.IsNullOrEmpty(this.NetworkInterface))
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterNetworkInterface, this.NetworkInterface));
            }
            if (!String.IsNullOrEmpty(this.CacheFolder))
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterCacheFolder, this.CacheFolder));
            }
            if (this.LiveStream != SimpleUrl.DefaultLiveStream)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterLiveStream, this.LiveStream ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpProtocolInputData != SimpleUrl.DefaultDumpProtocolInputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpProtocolInputData, this.DumpProtocolInputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpProtocolOutputData != SimpleUrl.DefaultDumpProtocolOutputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpProtocolOutputData, this.DumpProtocolOutputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpParserInputData != SimpleUrl.DefaultDumpParserInputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpParserInputData, this.DumpParserInputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpParserOutputData != SimpleUrl.DefaultDumpParserOutputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpParserOutputData, this.DumpParserOutputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpOutputPinData != SimpleUrl.DefaultDumpOutputPinData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpOutputPinData, this.DumpOutputPinData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }

            // return current URI and formatted connection string
            // MediaPortal Url Source Splitter will ignore first part
            // first part is there, because OnlineVideos cannot work with not valid URIs

            return this.Uri.Scheme + "://" + this.Uri.Host + SimpleUrl.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Special separator used to identify where starts parameters for MediaPortal Url Source Splitter.
        /// </summary>
        public static readonly String ParameterSeparator = "####";

        // common parameters for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies network interface parameter name.
        /// </summary>
        protected static readonly String ParameterNetworkInterface = "Interface";

        /// <summary>
        /// Specifies URL parameter name.
        /// </summary>
        protected static readonly String ParameterUrl = "Url";

        /// <summary>
        /// Specifies maximum log size parameter name.
        /// </summary>
        protected static readonly String ParameterLogMaxSize = "LogMaxSize";

        /// <summary>
        /// Specifies log verbosity parameter name.
        /// </summary>
        protected static readonly String ParameterLogVerbosity = "LogVerbosity";

        /// <summary>
        /// Specifies maximum plugins parameter name.
        /// </summary>
        protected static readonly String ParameterMaximumPlugins = "MaxPlugins";

        /// <summary>
        /// Specifies cache folder parameter name.
        /// </summary>
        protected static readonly String ParameterCacheFolder = "CacheFolder";

        /// <summary>
        /// Specifies live stream flag parameter name.
        /// </summary>
        protected static readonly String ParameterLiveStream = "LiveStream";

        /// <summary>
        /// Specifies parameter name for protocol input data.
        /// </summary>
        protected static readonly String ParameterDumpProtocolInputData = "DumpProtocolInputData";

        /// <summary>
        /// Specifies parameter name for protocol output data.
        /// </summary>
        protected static readonly String ParameterDumpProtocolOutputData = "DumpProtocolOutputData";

        /// <summary>
        /// Specifies parameter name for parser input data.
        /// </summary>
        protected static readonly String ParameterDumpParserInputData = "DumpParserInputData";

        /// <summary>
        /// Specifies parameter name for parser output data.
        /// </summary>
        protected static readonly String ParameterDumpParserOutputData = "DumpParserOutputData";

        /// <summary>
        /// Specifies parameter name for output pin data.
        /// </summary>
        protected static readonly String ParameterDumpOutputPinData = "DumpOutputPinData";

        // default values for some parameters

        /// <summary>
        /// Specifies <see langword="true"/> value for filter parameter value.
        /// </summary>
        public static readonly String DefaultTrue = "1";

        /// <summary>
        /// Specifies <see langword="false"/> value for filter parameter value.
        /// </summary>
        public static readonly String DefaultFalse = "0";

        /// <summary>
        /// Default verbosity of MediaPortal Url Sorce Splitter.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2.LogVerbosity.Verbose"/>.
        /// </remarks>
        public const LogVerbosity DefaultVerbosity = LogVerbosity.Verbose;

        /// <summary>
        /// Default maximum log size of MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// The default values is 10 MB.
        /// </remarks>
        public const int DefaultLogMaximumSize = 10 * 1024 * 1024;

        /// <summary>
        /// Default maximum plugins for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// The default value is 256.
        /// </remarks>
        public const int DefaultMaximumPlugins = 256;

        /// <summary>
        /// Default value of live stream flag.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>.
        /// </remarks>
        public const Boolean DefaultLiveStream = false;

        /// <summary>
        /// Default value for parameter for protocol input data parameter.
        /// </summary>
        public const Boolean DefaultDumpProtocolInputData = false;

        /// <summary>
        /// Default value for protocol output data parameter.
        /// </summary>
        public const Boolean DefaultDumpProtocolOutputData = false;

        /// <summary>
        /// Default value for parser input data parameter.
        /// </summary>
        public const Boolean DefaultDumpParserInputData = false;

        /// <summary>
        /// Default value for parser output data parameter.
        /// </summary>
        public const Boolean DefaultDumpParserOutputData = false;

        /// <summary>
        /// Default value for output pin data parameter.
        /// </summary>
        public const Boolean DefaultDumpOutputPinData = false;

        #endregion
    }
}
