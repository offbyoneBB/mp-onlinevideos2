using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.V2
{
    /// <summary>
    /// Represents string RTMP arbitrary data.
    /// </summary>
    internal class RtmpStringArbitraryData : RtmpArbitraryData
    {
        #region Private fields

        private String value;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpStringArbitraryData"/> class with specified value.
        /// </summary>
        /// <param name="value">The specified string value.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpStringArbitraryData"/> class.
        /// </overloads>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="value"/> is <see langword="null"/>.</para>
        /// </exception>
        public RtmpStringArbitraryData(String value)
            : this(RtmpArbitraryData.DefaultName, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpStringArbitraryData"/> class with specified value and name.
        /// </summary>
        /// <param name="name">The name of arbitrary data.</param>
        /// <param name="value">The specified string value.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="value"/> is <see langword="null"/>.</para>
        /// </exception>
        public RtmpStringArbitraryData(String name, String value)
            : base(RtmpArbitraryDataType.String, name)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value of number arbitrary data type.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="Value"/> is <see langword="null"/>.</para>
        /// </exception>
        public String Value
        {
            get { return this.value; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Value");
                }

                this.value = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Encodes string value to be correct for MediaPortal Url Source Filter.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the eencoded string value this arbitrary data.
        /// </returns>
        protected virtual String EncodeValue()
        {
            return this.Value.Replace("\\", "\\5c").Replace(" ", "\\20");
        }

        /// <summary>
        /// Gets canonical string representation for the string arbitrary data.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data.
        /// </returns>
        public override string ToString()
        {
            if (this.Name != RtmpArbitraryData.DefaultName)
            {
                return String.Format("conn=NS:{0}:{1}", this.Name, this.EncodeValue());
            }
            else
            {
                return String.Format("conn=S:{0}", this.EncodeValue());
            }
        }

        #endregion
    }
}
