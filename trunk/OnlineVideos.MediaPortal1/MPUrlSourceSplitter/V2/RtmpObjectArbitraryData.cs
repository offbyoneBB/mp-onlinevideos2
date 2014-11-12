using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V2
{
    /// <summary>
    /// Represents object RTMP arbitrary data.
    /// </summary>
    internal class RtmpObjectArbitraryData : RtmpArbitraryData
    {
        #region Private fields

        private RtmpArbitraryDataCollection objects;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpObjectArbitraryData"/> class.
        /// </summary>
        /// <param name="value">The specified object value.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="RtmpObjectArbitraryData"/> class.
        /// </overloads>
        public RtmpObjectArbitraryData()
            : this(RtmpArbitraryData.DefaultName)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpObjectArbitraryData"/> class with specified name.
        /// </summary>
        /// <param name="name">The name of arbitrary data.</param>
        public RtmpObjectArbitraryData(String name)
            : base(RtmpArbitraryDataType.Object, name)
        {
            this.objects = new RtmpArbitraryDataCollection();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the objects in this object arbitrary data type.
        /// </summary>
        public RtmpArbitraryDataCollection Objects
        {
            get { return this.objects; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the object arbitrary data.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data.
        /// </returns>
        public override string ToString()
        {
            if (this.Name != RtmpArbitraryData.DefaultName)
            {
                if (String.IsNullOrEmpty(this.Objects.ToString()))
                {
                    return String.Format("conn=NO:{0}:1 conn=O:{0}:0", this.Name);
                }
                else
                {
                    return String.Format("conn=NO:{0}:1 {1} conn=NO:{0}:0", this.Name, this.Objects.ToString());
                }
            }
            else
            {
                if (String.IsNullOrEmpty(this.Objects.ToString()))
                {
                    return String.Format("conn=O:1 conn=O:0");
                }
                else
                {
                    return String.Format("conn=O:1 {0} conn=O:0", this.Objects.ToString());
                }
            }
        }

        #endregion
    }
}
