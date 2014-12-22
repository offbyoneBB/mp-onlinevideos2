using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace OnlineVideos.MPUrlSourceFilter.V2
{
    /// <summary>
    /// Represents collection of RTMP protocol arbitrary data.
    /// </summary>
    internal class RtmpArbitraryDataCollection : Collection<RtmpArbitraryData>
    {
        #region Private fields
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="RtmpArbitraryDataCollection"/> class.
        /// </summary>
        public RtmpArbitraryDataCollection()
            : base()
        {
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        /// <summary>
        /// Inserts a arbitrary data into collection at specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The arbitrary data to insert.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="item"/> is <see langword="null"/>.</para>
        /// </exception>
        protected override void InsertItem(int index, RtmpArbitraryData item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// Replaces the arbitrary data at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the parameter to replace.param>
        /// <param name="item">The new value for the arbitrary data at the specified index.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="item"/> is <see langword="null"/>.</para>
        /// </exception>
        protected override void SetItem(int index, RtmpArbitraryData item)
        {
            base.SetItem(index, item);
        }

        /// <summary>
        /// Gets canonical string representation for the arbitrary data collection.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the canonical representation of the this arbitrary data collection.
        /// </returns>
        /// <remarks>
        /// Returns empty string ("") if no arbitrary data is in collection.
        /// </remarks>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var arbitraryData in this)
            {
                builder.AppendFormat((builder.Length == 0) ? "{0}" : "{1}{0}", arbitraryData.ToString(), RtmpArbitraryDataCollection.ArbitraryDataSeparator);
            }

            return builder.ToString();
        }

        #endregion

        #region Constants

        /// <summary>
        /// Specifies arbitrary data separator.
        /// </summary>
        public static String ArbitraryDataSeparator = " ";

        #endregion
    }
}
