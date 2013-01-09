using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents base class for Adobe Flash HTTP Streaming protocol described by manifest file.
    /// </summary>
    public class AfhsManifestUrl : HttpUrl
    {
        #region Private fields

        private String extraParameters;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="AfhsManifestUrl"/> class.
        /// </summary>
        /// <param name="url">The URL of manifest to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="AfhsManifestUrl"/> class.
        /// </overloads>
        public AfhsManifestUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="AfhsManifestUrl"/> class.
        /// </summary>
        /// <param name="uri">The uniform resource identifier with manifest URL.</param>
        /// <exception cref="ArgumentException">
        /// <para>The protocol supplied by <paramref name="uri"/> is not supported.</para>
        /// </exception>
        public AfhsManifestUrl(Uri uri)
            : base(uri)
        {
            this.ExtraParameters = AfhsManifestUrl.DefaultExtraParameters;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets extra parameters attached to each segment and fragment URL. Extra parameters should start with '?'.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="ExtraParameters"/> is <see langword="null"/>.</para>
        /// </exception>
        public String ExtraParameters
        {
            get { return this.extraParameters; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ExtraParameters");
                }

                this.extraParameters = value;
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

            if (this.ExtraParameters != DefaultExtraParameters)
            {
                parameters.Add(new Parameter(AfhsManifestUrl.ParameterExtraParameters, this.ExtraParameters));
            }

            // return formatted connection string
            return base.ToString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        // common parameters of AFHS protocol (based on manifest URL) for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies extra parameters added to each segment and fragment for AFHS protocol.
        /// </summary>
        protected static String ParameterExtraParameters = "AfhsExtraParameters";

        // default values for some parameters

        /// <summary>
        /// Default extra parameters for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This value is <see cref="System.String.Empty"/>.
        /// </remarks>
        public static String DefaultExtraParameters = String.Empty;

        #endregion
    }
}
