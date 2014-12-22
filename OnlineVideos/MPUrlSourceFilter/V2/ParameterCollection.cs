using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace OnlineVideos.MPUrlSourceFilter.V2
{
    /// <summary>
    /// Represents collection of parameters for MediaPortal Url Source Filter.
    /// </summary>
    internal class ParameterCollection : Collection<Parameter>
    {
        #region Private fields
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ParameterCollection"/> class.
        /// </summary>
        public ParameterCollection()
            : base()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets filter parameters.
        /// </summary>
        public virtual String FilterParameters
        {
            get
            {
                StringBuilder builder = new StringBuilder();

                foreach (var parameter in this)
                {
                    builder.AppendFormat((builder.Length == 0) ? "{0}" : "{1}{0}", parameter.FormatParameter(ParameterCollection.ParameterSeparator), ParameterCollection.ParameterSeparator);
                }

                return builder.ToString();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts a parameter into collection at specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The parameter to insert.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="item"/> is <see langword="null"/>.</para>
        /// </exception>
        protected override void InsertItem(int index, Parameter item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// Replaces the parameter at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the parameter to replace.param>
        /// <param name="item">The new value for the parameter at the specified index.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="item"/> is <see langword="null"/>.</para>
        /// </exception>
        protected override void SetItem(int index, Parameter item)
        {
            base.SetItem(index, item);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Specifies parameter separator for MediaPortal Url Source Filter.
        /// </summary>
        public static String ParameterSeparator = "&";

        #endregion
    }
}
