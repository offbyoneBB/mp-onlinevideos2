using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MPUrlSourceFilter.Http
{
    /// <summary>
    /// Represents class for HTTP header.
    /// </summary>
    [Serializable]
    public class HttpHeader
    {
        #region Private fields

        private String name;
        private String value;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="HttpHeader"/> class.
        /// </summary>
        /// <param name="name">The name of HTTP header.</param>
        /// <param name="value">The value of HTTP header.</param>
        public HttpHeader(String name, String value)
        {
            this.Name = name;
            this.Value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of HTTP header.
        /// </summary>
        public String Name
        {
            get { return this.name; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Name");
                }

                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Cannot be empty string", "Name", null);
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of HTTP header.
        /// </summary>
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
    }
}
