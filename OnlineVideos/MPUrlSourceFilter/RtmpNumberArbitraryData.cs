using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents number RTMP arbitrary data.
    /// </summary>
    [Serializable]
    public class RtmpNumberArbitraryData : RtmpArbitraryData
    {
        #region Private fields
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpNumberArbitraryData"/> class with specified value.
        /// </summary>
        /// <param name="value">The specified number value.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpNumberArbitraryData"/> class.
        /// </overloads>
        public RtmpNumberArbitraryData(double value)
            : this(RtmpArbitraryData.DefaultName, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpNumberArbitraryData"/> class with specified value and name.
        /// </summary>
        /// <param name="name">The name of arbitrary data.</param>
        /// <param name="value">The specified number value.</param>
        public RtmpNumberArbitraryData(String name, double value)
            : base(RtmpArbitraryDataType.Number, name)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value of number arbitrary data type.
        /// </summary>
        public double Value { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the number arbitrary data.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data.
        /// </returns>
        public override string ToString()
        {
            if (this.Name != RtmpArbitraryData.DefaultName)
            {
                return String.Format("conn=NN:{0}:{1}", this.Name, this.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                return String.Format("conn=N:{0}", this.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        #endregion
    }
}
