using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Represents null RTMP arbitrary data.
    /// </summary>
    public class RtmpNullArbitraryData : RtmpArbitraryData
    {
        #region Private fields
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpNullArbitraryData"/> class.
        /// </summary>
        /// <param name="value">The specified object value.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpNullArbitraryData"/> class.
        /// </overloads>
        public RtmpNullArbitraryData()
            : this(RtmpArbitraryData.DefaultName)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpNullArbitraryData"/> class with specified name.
        /// </summary>
        /// <param name="name">The name of arbitrary data.</param>
        public RtmpNullArbitraryData(String name)
            : base(RtmpArbitraryDataType.Null, name)
        {
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the null arbitrary data.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data.
        /// </returns>
        public override string ToString()
        {
            if (this.Name != RtmpArbitraryData.DefaultName)
            {
                return String.Format("conn=NZ:{0}:", this.Name);
            }
            else
            {
                return String.Format("conn=Z:");
            }
        }

        #endregion
    }
}
