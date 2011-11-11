using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represent base class for all urls for MediaPortal Url Source Filter.
    /// </summary>
    public abstract class SimpleUrl
    {
        #region Private fields

        private Uri uri;
        private LogVerbosity verbosity = SimpleUrl.DefaultVerbosity;
        private String networkInterface = String.Empty;
        private int maximumLogSize = SimpleUrl.DefaultMaximumLogSize;
        private int maximumPlugins = SimpleUrl.DefaultMaximumPlugins;
        private int bufferingPercentage = SimpleUrl.DefaultBufferingPercentage;
        private int maximumBufferingSize = SimpleUrl.DefaultMaximumBufferingSize;

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
        /// Gets or sets the verbosity level of MediaPortal Url Source Filter.
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
        /// Gets or sets the network interface which MediaPortal Url Source Filter have to use.
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
        /// Gets or sets maximum plugins.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="BufferingPercentage"/> is less than zero.</para>
        /// <para>- or -</para>
        /// <para>The <see cref="BufferingPercentage"/> is higher than hundred.</para>
        /// </exception>
        public int BufferingPercentage
        {
            get { return this.bufferingPercentage; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("BufferingPercentage", value, "Value cannot be less than zero.");
                }

                if (value > 100)
                {
                    throw new ArgumentOutOfRangeException("BufferingPercentage", value, "Value cannot be higher than hundred.");
                }

                this.bufferingPercentage = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum plugins.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="MaximumBufferingSize"/> is less than zero.</para>
        /// </exception>
        public int MaximumBufferingSize
        {
            get { return this.maximumBufferingSize; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("MaximumBufferingSize", value, "Value cannot be less than zero.");
                }

                this.maximumBufferingSize = value;
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

            parameters.Add(new Parameter(SimpleUrl.ParameterUrl, this.Uri.ToString()));
            parameters.Add(new Parameter(SimpleUrl.ParameterLogVerbosity, ((int)this.Verbosity).ToString()));
            parameters.Add(new Parameter(SimpleUrl.ParameterMaximumBufferingSize, this.MaximumBufferingSize.ToString()));
            parameters.Add(new Parameter(SimpleUrl.ParameterMaximumLogSize, this.MaximumLogSize.ToString()));
            parameters.Add(new Parameter(SimpleUrl.ParameterMaximumPlugins, this.MaximumPlugins.ToString()));
            parameters.Add(new Parameter(SimpleUrl.ParameterBufferingPercentage, this.BufferingPercentage.ToString()));

            if (!String.IsNullOrEmpty(this.NetworkInterface))
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterNetworkInterface, this.NetworkInterface));
            }

            // return current URI and formatted connection string
            // MediaPortal Url Source Filter will ignore first part
            // first part is there, because OnlineVideos cannot work with not valid URIs
            return this.Uri.ToString() + SimpleUrl.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Special separator used to identify where starts parameters for MediaPortal Url Source Filter.
        /// </summary>
        public static String ParameterSeparator = "####";

        // common parameters for MediaPortal Url Source Filter

        /// <summary>
        /// Specifies network interface parameter name.
        /// </summary>
        protected static String ParameterNetworkInterface = "Interface";

        /// <summary>
        /// Specifies URL parameter name.
        /// </summary>
        protected static String ParameterUrl = "Url";

        /// <summary>
        /// Specifies maximum log size parameter name.
        /// </summary>
        protected static String ParameterMaximumLogSize = "MaxLogSize";

        /// <summary>
        /// Specifies log verbosity parameter name.
        /// </summary>
        protected static String ParameterLogVerbosity = "LogVerbosity";

        /// <summary>
        /// Specifies maximum plugins parameter name.
        /// </summary>
        protected static String ParameterMaximumPlugins = "MaxPlugins";

        /// <summary>
        /// Specifies buffering percentage parameter name.
        /// </summary>
        protected static String ParameterBufferingPercentage = "BufferingPercentage";

        /// <summary>
        /// Specifies maximum buffering size parameter name.
        /// </summary>
        protected static String ParameterMaximumBufferingSize = "MaxBufferingSize";

        // default values for some parameters

        /// <summary>
        /// Default verbosity of MediaPortal Url Sorce Filter.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="OnlineVideos.SourceFilter.LogVerbosity.Verbose"/>.
        /// </remarks>
        public const LogVerbosity DefaultVerbosity = LogVerbosity.Verbose;

        /// <summary>
        /// Default maximum log size of MediaPortal Url Source Filter.
        /// </summary>
        /// <remarks>
        /// The default values is 10 MB.
        /// </remarks>
        public const int DefaultMaximumLogSize = 10 * 1024 * 1024;

        /// <summary>
        /// Default maximum plugins for MediaPortal Url Source Filter.
        /// </summary>
        /// <remarks>
        /// The default value is 256.
        /// </remarks>
        public const int DefaultMaximumPlugins = 256;

        /// <summary>
        /// Default buffering percentage for MediaPortal Url Source Filter.
        /// </summary>
        /// <remarks>
        /// The default value is 2%.
        /// </remarks>
        public const int DefaultBufferingPercentage = 2;

        /// <summary>
        /// Default maximum buffering size for MediaPortal Url Source Filter.
        /// </summary>
        /// <remarks>
        /// The default value is 5 MB.
        /// </remarks>
        public const int DefaultMaximumBufferingSize = 5 * 1024 * 1024;

        #endregion
    }
}
