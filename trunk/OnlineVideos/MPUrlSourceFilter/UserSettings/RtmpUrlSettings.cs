using System;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.UserSettings
{
    /// <summary>
    /// Represents class for RTMP url settings.
    /// </summary>
    [TypeConverter(typeof(ExpandableUserSettingObjectConverter<RtmpUrlSettings>))]
    [Serializable]
    public class RtmpUrlSettings : SimpleUrlSettings
    {
        #region Private fields

        private int openConnectionTimeout = OnlineVideoSettings.Instance.RtmpOpenConnectionTimeout;
        private int openConnectionSleepTime = OnlineVideoSettings.Instance.RtmpOpenConnectionSleepTime;
        private int totalReopenConnectionTimeout = OnlineVideoSettings.Instance.RtmpTotalReopenConnectionTimeout;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the timeout to open RTMP url in milliseconds.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="OpenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("The timeout to open RTMP url in milliseconds. It is applied to first opening of url.")]
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
        /// Gets or sets the total timeout to open RTMP url in milliseconds.
        /// </summary>
        /// <remarks>
        /// <para>It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="TotalReopenConnectionTimeout"/> is lower than zero.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("The total timeout to open RTMP url in milliseconds. It is applied when lost connection and trying to open new one. Filter will be trying to open connection until this timeout occurs. This parameter is ignored in case of live stream.")]
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
        /// Initializes a new instance of <see cref="RtmpUrlSettings" /> class.
        /// </summary>
        public RtmpUrlSettings()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpUrlSettings" /> class with specified RTMP url parameters.
        /// </summary>
        /// <param name="value">RTMP url parameters.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="value"/> is <see langword="null"/>.</para>
        /// </exception>
        public RtmpUrlSettings(String value)
            : base(value)
        {
            var parameters = GetParameters(value);

            var ovSettings = OnlineVideoSettings.Instance;

            this.OpenConnectionTimeout = GetValue(parameters, "OpenConnectionTimeout", ovSettings.RtmpOpenConnectionTimeout);
            this.OpenConnectionSleepTime = GetValue(parameters, "OpenConnectionSleepTime", ovSettings.RtmpOpenConnectionSleepTime);
            this.TotalReopenConnectionTimeout = GetValue(parameters, "TotalReopenConnectionTimeout", ovSettings.RtmpTotalReopenConnectionTimeout);
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
            builder.Append((this.OpenConnectionTimeout != OnlineVideoSettings.Instance.RtmpOpenConnectionTimeout) ? String.Format("OpenConnectionTimeout={0};", this.OpenConnectionTimeout) : String.Empty);
            builder.Append((this.OpenConnectionSleepTime != OnlineVideoSettings.Instance.RtmpOpenConnectionSleepTime) ? String.Format("OpenConnectionSleepTime={0};", this.OpenConnectionSleepTime) : String.Empty);
            builder.Append((this.TotalReopenConnectionTimeout != OnlineVideoSettings.Instance.RtmpTotalReopenConnectionTimeout) ? String.Format("TotalReopenConnectionTimeout={0};", this.TotalReopenConnectionTimeout) : String.Empty);

            return builder.ToString();
        }

        internal void Apply(RtmpUrl rtmpUrl)
        {
            base.Apply(rtmpUrl);

            rtmpUrl.OpenConnectionSleepTime = OpenConnectionSleepTime;
            rtmpUrl.OpenConnectionTimeout = OpenConnectionTimeout;
            rtmpUrl.TotalReopenConnectionTimeout = TotalReopenConnectionTimeout;
        }

        #endregion
    }
}
