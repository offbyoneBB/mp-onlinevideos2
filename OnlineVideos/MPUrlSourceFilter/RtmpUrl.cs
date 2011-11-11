using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter
{
	/// <summary>
	/// Represent base class for HTTP urls for MediaPortal Url Source Filter.
	/// All parameter values will be UrlEncoded, so make sure you set them UrlDecoded!
	/// </summary>
	public class RtmpUrl : SimpleUrl
	{
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
			: this(new Uri(!string.IsNullOrEmpty(tcUrl) ? new Uri(tcUrl).Scheme : "rtmp") + "://" + hostname + (port > 0 ? ":" + port : ""))
		{
			this.tcUrl = tcUrl;
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
            if (!Uri.Scheme.StartsWith("rtmp"))
            {
                throw new ArgumentException("The protocol is not supported.", "uri");
            }
		}

		#endregion

		#region Properties

		public string tcUrl { get; set; }
		public string app { get; set; }
		public string playpath { get; set; }
		public string subscribe { get; set; }
		public string pageUrl { get; set; }
		public string swfUrl { get; set; }
		public bool swfVfy { get; set; }
		public bool live { get; set; }
		public string token { get; set; }
		public string conn { get; set; }

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

			if (!string.IsNullOrEmpty(tcUrl)) parameters.Add(new Parameter("tcUrl", tcUrl));
			if (!string.IsNullOrEmpty(app)) parameters.Add(new Parameter("app", app));
			if (!string.IsNullOrEmpty(subscribe)) parameters.Add(new Parameter("subscribe", subscribe));
			if (!string.IsNullOrEmpty(pageUrl)) parameters.Add(new Parameter("pageUrl", pageUrl));
			if (!string.IsNullOrEmpty(swfUrl)) parameters.Add(new Parameter("swfUrl", swfUrl));
			if (swfVfy) parameters.Add(new Parameter("swfVfy", "1"));
			if (live) parameters.Add(new Parameter("live", "true"));
			if (!string.IsNullOrEmpty(token)) parameters.Add(new Parameter("token", token));
			if (!string.IsNullOrEmpty(conn)) parameters.Add(new Parameter("conn", conn));

			// return formatted connection string
			return base.ToString() + ParameterCollection.ParameterSeparator + parameters.FilterParameters;
		}

		#endregion
	}
}
