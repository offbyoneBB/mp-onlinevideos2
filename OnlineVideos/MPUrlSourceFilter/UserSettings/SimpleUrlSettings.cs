using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.UserSettings
{
    /// <summary>
    /// Represents base abstract class for simple url settings.
    /// </summary>
    [Serializable]
    public abstract class SimpleUrlSettings
    {
        #region Private fields

        private String networkInterface;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the network interface.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="NetworkInterface"/> is <see langword="null"/>.</para>
        /// </exception>
        [Category("OnlineVideosUserConfiguration"), Description("Preferred network interface.")]
        [TypeConverter(typeof(NetworkInterfaceConverter))]
        [NotifyParentProperty(true)]
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
        /// Specifies if protocol have to dump input data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if protocol have to dump input data.")]
        [NotifyParentProperty(true)]
        public Boolean DumpProtocolInputData { get; set; }

        /// <summary>
        /// Specifies if protocol have to dump output data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if protocol have to dump output data.")]
        [NotifyParentProperty(true)]
        public Boolean DumpProtocolOutputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump input data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if parser have to dump input data.")]
        [NotifyParentProperty(true)]
        public Boolean DumpParserInputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump output data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if parser have to dump output data.")]
        [NotifyParentProperty(true)]
        public Boolean DumpParserOutputData { get; set; }

        /// <summary>
        /// Specifies if output pin(s) have to dump data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if output pin(s) have to dump data.")]
        [NotifyParentProperty(true)]
        public Boolean DumpOutputPinData { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrlSettings" /> class.
        /// </summary>
        protected SimpleUrlSettings()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrlSettings" /> class with specified simple url parameters.
        /// </summary>
        /// <param name="value">Simple url parameters.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="value"/> is <see langword="null"/>.</para>
        /// </exception>
        protected SimpleUrlSettings(String value)
        {
            var parameters = GetParameters(value);

            this.NetworkInterface = GetValue(parameters, "NetworkInterface", OnlineVideoSettings.NetworkInterfaceSystemDefault);

            this.DumpProtocolInputData = (String.Compare(GetValue(parameters, "DumpProtocolInputData", "0"), "1") == 0);
            this.DumpProtocolOutputData = (String.Compare(GetValue(parameters, "DumpProtocolOutputData", "0"), "1") == 0);
            this.DumpParserInputData = (String.Compare(GetValue(parameters, "DumpParserInputData", "0"), "1") == 0);
            this.DumpParserOutputData = (String.Compare(GetValue(parameters, "DumpParserOutputData", "0"), "1") == 0);
            this.DumpOutputPinData = (String.Compare(GetValue(parameters, "DumpOutputPinData", "0"), "1") == 0);
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

            builder.Append((String.CompareOrdinal(this.NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? String.Format("NetworkInterface={0};", this.NetworkInterface) : String.Empty);

            builder.Append(this.DumpProtocolInputData ? "DumpProtocolInputData=1;" : String.Empty);
            builder.Append(this.DumpProtocolOutputData ? "DumpProtocolOutputData=1;" : String.Empty);
            builder.Append(this.DumpParserInputData ? "DumpParserInputData=1;" : String.Empty);
            builder.Append(this.DumpParserOutputData ? "DumpParserOutputData=1;" : String.Empty);
            builder.Append(this.DumpOutputPinData ? "DumpOutputPinData=1;" : String.Empty);
            
            return builder.ToString();
        }

        internal void Apply(SimpleUrl simpleUrl)
        {
            simpleUrl.NetworkInterface = (String.CompareOrdinal(NetworkInterface, OnlineVideoSettings.NetworkInterfaceSystemDefault) != 0) ? NetworkInterface : String.Empty;
            simpleUrl.DumpProtocolInputData = DumpProtocolInputData;
            simpleUrl.DumpProtocolOutputData = DumpProtocolOutputData;
            simpleUrl.DumpParserInputData = DumpParserInputData;
            simpleUrl.DumpParserOutputData = DumpParserOutputData;
            simpleUrl.DumpOutputPinData = DumpOutputPinData;
        }

        protected static Dictionary<string,string> GetParameters(String value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var parameters = new Dictionary<string, string>();

            foreach (var parameter in value.Split(new String[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                String[] parameterNameValue = parameter.Split(new String[] { "=" }, StringSplitOptions.None);
                if (parameterNameValue.Length == 1)
                {
                    parameters.Add(parameterNameValue[0], String.Empty);
                }
                else
                {
                    parameters.Add(parameterNameValue[0], parameterNameValue[1]);
                }
            }

            return parameters;
        }

        protected static T GetValue<T>(Dictionary<string, string> parameters, String name, T defaultValue)
        {
            if (parameters.ContainsKey(name))
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)parameters[name];
                else if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(parameters[name]);
                else
                    throw new Exception(string.Format("Unsupported type <{0}> for conversion!", typeof(T).FullName));
            }
            else
            {
                return defaultValue;
            }
        }

        #endregion
    }
}
