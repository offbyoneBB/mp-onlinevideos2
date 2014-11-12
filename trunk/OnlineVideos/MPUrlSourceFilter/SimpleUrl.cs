using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represent base class for all urls for MediaPortal Url Source Splitter.
    /// </summary>
    [Serializable]
    public abstract class SimpleUrl
    {
        #region Private fields

        private Uri uri;
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
        [Category("General"), Description("The uniform resource identifier for this instance.")]
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
        /// Gets or sets the live stream flag.
        /// </summary>
        /// <remarks>
        /// Specifying live stream flag optimize some parts of MediaPortal Url Source Splitter. In case of live stream are ignored timeouts, cache file is not needed (except the case of downloading = recording).
        /// </remarks>
        [Category("MPUrlSourceSplitter"), DefaultValue(false), Description("Specifies if stream is live or not.")]
        public Boolean LiveStream
        {
            get { return this.liveStream; }
            set { this.liveStream = value; }
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
            BinaryFormatter serializer = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, this);
                return this.Uri.Scheme + "://" + this.Uri.Host + SimpleUrl.ParameterSeparator + Convert.ToBase64String(stream.ToArray());
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Special separator used to identify where starts parameters for MediaPortal Url Source Splitter.
        /// </summary>
        public static String ParameterSeparator = "####";

        // common parameters for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies URL parameter name.
        /// </summary>
        protected static String ParameterUrl = "Url";

        /// <summary>
        /// Specifies live stream flag parameter name.
        /// </summary>
        protected static String ParameterLiveStream = "LiveStream";

        // default values for some parameters

        /// <summary>
        /// Default value of live stream flag.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>.
        /// </remarks>
        public const Boolean DefaultLiveStream = false;

        #endregion
    }
}
