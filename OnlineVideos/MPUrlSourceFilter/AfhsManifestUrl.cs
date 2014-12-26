using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents base class for Adobe Flash HTTP Streaming protocol described by manifest file.
    /// </summary>
    [Serializable]
    public class AfhsManifestUrl : HttpUrl
    {
        #region Private fields

        private String segmentFragmentUrlExtraParameters;

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
            this.SegmentFragmentUrlExtraParameters = AfhsManifestUrl.DefaultSegmentFragmentUrlExtraParameters;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets segment and fragment extra parameters attached to each segment and fragment URL. Segment and fragment extra parameters should start with '?'.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="ExtraParameters"/> is <see langword="null"/>.</para>
        /// </exception>
        public String SegmentFragmentUrlExtraParameters
        {
            get { return this.segmentFragmentUrlExtraParameters; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("SegmentFragmentUrlExtraParameters");
                }

                this.segmentFragmentUrlExtraParameters = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a string that can be given to the MediaPortal Url Source Splitter holding the url and all parameters.
        /// </summary>
        internal override string ToFilterString()
        {
            ParameterCollection parameters = new ParameterCollection();

            if (this.SegmentFragmentUrlExtraParameters != AfhsManifestUrl.DefaultSegmentFragmentUrlExtraParameters)
            {
                parameters.Add(new Parameter(AfhsManifestUrl.ParameterSegmentFragmentUrlExtraParameters, this.SegmentFragmentUrlExtraParameters));
            }

            // return formatted connection string
            return base.ToFilterString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
        }

        #endregion

        #region Constants

        // common parameters of AFHS protocol (based on manifest URL) for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies segment and fragment extra parameters added to each segment and fragment for AFHS protocol.
        /// </summary>
        protected static String ParameterSegmentFragmentUrlExtraParameters = "AfhsSegmentFragmentUrlExtraParameters";

        // default values for some parameters

        /// <summary>
        /// Default segment and fragment extra parameters for MediaPortal Url Source Splitter.
        /// </summary>
        /// <remarks>
        /// This value is <see cref="System.String.Empty"/>.
        /// </remarks>
        public static String DefaultSegmentFragmentUrlExtraParameters = String.Empty;

        #endregion
    }
}
