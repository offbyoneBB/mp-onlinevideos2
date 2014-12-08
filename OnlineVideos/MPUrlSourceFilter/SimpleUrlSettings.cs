using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

namespace OnlineVideos.MPUrlSourceFilter
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
        public Boolean DumpProtocolInputData { get; set; }

        /// <summary>
        /// Specifies if protocol have to dump output data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if protocol have to dump output data.")]
        public Boolean DumpProtocolOutputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump input data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if parser have to dump input data.")]
        public Boolean DumpParserInputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump output data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if parser have to dump output data.")]
        public Boolean DumpParserOutputData { get; set; }

        /// <summary>
        /// Specifies if output pin(s) have to dump data.
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Specifies if output pin(s) have to dump data.")]
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
            Hashtable parameters = SimpleUrlSettings.GetParameters(value);

            this.NetworkInterface = SimpleUrlSettings.GetValue(parameters, "NetworkInterface", OnlineVideoSettings.NetworkInterfaceSystemDefault);

            this.DumpProtocolInputData = (String.Compare(SimpleUrlSettings.GetValue(parameters, "DumpProtocolInputData", "0"), "1") == 0);
            this.DumpProtocolOutputData = (String.Compare(SimpleUrlSettings.GetValue(parameters, "DumpProtocolOutputData", "0"), "1") == 0);
            this.DumpParserInputData = (String.Compare(SimpleUrlSettings.GetValue(parameters, "DumpParserInputData", "0"), "1") == 0);
            this.DumpParserOutputData = (String.Compare(SimpleUrlSettings.GetValue(parameters, "DumpParserOutputData", "0"), "1") == 0);
            this.DumpOutputPinData = (String.Compare(SimpleUrlSettings.GetValue(parameters, "DumpOutputPinData", "0"), "1") == 0);
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

        protected static Hashtable GetParameters(String value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Hashtable parameters = new Hashtable();

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

        protected static String GetValue(Hashtable parameters, String name, String defaultValue)
        {
            if (parameters.Contains(name))
            {
                return (String)parameters[name];
            }
            else
            {
                return defaultValue;
            }
        }

        #endregion
    }
}
