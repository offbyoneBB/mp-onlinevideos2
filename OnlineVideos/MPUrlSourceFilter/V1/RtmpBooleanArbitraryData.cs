using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Represents boolean RTMP arbitrary data.
    /// </summary>
    public class RtmpBooleanArbitraryData : RtmpArbitraryData
    {
        #region Private fields
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpBoolenArbitraryData"/> class with specified value.
        /// </summary>
        /// <param name="value">The specified boolean value.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpBoolenArbitraryData"/> class.
        /// </overloads>
        public RtmpBooleanArbitraryData(Boolean value)
            : this(RtmpArbitraryData.DefaultName, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpBoolenArbitraryData"/> class with specified value and name.
        /// </summary>
        /// <param name="name">The name of arbitrary data.</param>
        /// <param name="value">The specified boolean value.</param>
        public RtmpBooleanArbitraryData(String name, Boolean value)
            : base(RtmpArbitraryDataType.Boolean, name)
        {
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value of boolean arbitrary data type.
        /// </summary>
        public Boolean Value { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the boolean arbitrary data.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data.
        /// </returns>
        public override string ToString()
        {
            if (this.Name != RtmpArbitraryData.DefaultName)
            {
                return String.Format("conn=NB:{0}:{1}", this.Name, this.Value ? "1" : "0");
            }
            else
            {
                return String.Format("conn=B:{0}", this.Value ? "1" : "0");
            }
        }

        #endregion
    }
}
