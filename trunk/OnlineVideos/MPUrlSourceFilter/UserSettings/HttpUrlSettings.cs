using System;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.UserSettings
{
    /// <summary>
    /// Represents class for HTTP url settings.
    /// </summary>
    [TypeConverter(typeof(ExpandableUserSettingObjectConverter<HttpUrlSettings>))]
    [Serializable]
    public class HttpUrlSettings : SimpleUrlSettings
    {
        #region Private fields

        private int openConnectionTimeout = OnlineVideoSettings.Instance.HttpOpenConnectionTimeout;
        private int openConnectionSleepTime = OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the timeout to open HTTP url in milliseconds.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("The timeout to open HTTP url in milliseconds. It is applied to first opening of url.")]
        [NotifyParentProperty(true)]
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
        [Category("OnlineVideosUserConfiguration"), Description("The time in milliseconds to sleep before opening connection.")]
        [NotifyParentProperty(true)]
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
        /// Gets or sets the total timeout to open HTTP url in milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="TotalReopenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("The total timeout to open HTTP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.")]
        [NotifyParentProperty(true)]
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

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrlSettings" /> class.
        /// </summary>
        public HttpUrlSettings()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HttpUrlSettings" /> class with specified HTTP url parameters.
        /// </summary>
        /// <param name="value">HTTP url parameters.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="value"/> is <see langword="null"/>.</para>
        /// </exception>
        public HttpUrlSettings(String value)
            : base(value)
        {
            var parameters = GetParameters(value);

            var ovSettings = OnlineVideoSettings.Instance;

            this.OpenConnectionTimeout = GetValue(parameters, "OpenConnectionTimeout", ovSettings.HttpOpenConnectionTimeout);
            this.OpenConnectionSleepTime = GetValue(parameters, "OpenConnectionSleepTime", ovSettings.HttpOpenConnectionSleepTime);
            this.TotalReopenConnectionTimeout = GetValue(parameters, "TotalReopenConnectionTimeout", ovSettings.HttpTotalReopenConnectionTimeout);
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
            StringBuilder builder = new StringBuilder();

            builder.Append(base.ToString());
            builder.Append((this.OpenConnectionTimeout != OnlineVideoSettings.Instance.HttpOpenConnectionTimeout) ? String.Format("OpenConnectionTimeout={0};", this.OpenConnectionTimeout) : String.Empty);
            builder.Append((this.OpenConnectionSleepTime != OnlineVideoSettings.Instance.HttpOpenConnectionSleepTime) ? String.Format("OpenConnectionSleepTime={0};", this.OpenConnectionSleepTime) : String.Empty);
            builder.Append((this.TotalReopenConnectionTimeout != OnlineVideoSettings.Instance.HttpTotalReopenConnectionTimeout) ? String.Format("TotalReopenConnectionTimeout={0};", this.TotalReopenConnectionTimeout) : String.Empty);

            return builder.ToString();
        }

        internal void Apply(HttpUrl httpUrl)
        {
            base.Apply(httpUrl);

            httpUrl.OpenConnectionSleepTime = OpenConnectionSleepTime;
            httpUrl.OpenConnectionTimeout = OpenConnectionTimeout;
            httpUrl.TotalReopenConnectionTimeout = TotalReopenConnectionTimeout;
        }

        #endregion
    }
}
