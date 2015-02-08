using System;
using System.Collections.Generic;
using System.Text;

namespace RssToolkit.Rss
{
    /// <summary>
    /// Rss Aggregation Event Arguments
    /// </summary>
    public class RssAggregationEventArgs : EventArgs
    {
        private Exception _exception;
        private string _message;
        private RssSeverityType _severityType;

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception
        {
            get 
            { 
                return _exception; 
            }

            set 
            { 
                _exception = value; 
            }
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message
        {
            get
            { 
                return _message; 
            }

            set 
            { 
                _message = value; 
            }
        }

        /// <summary>
        /// Gets or sets the type of the severity.
        /// </summary>
        /// <value>The type of the severity.</value>
        public RssSeverityType SeverityType
        {
            get 
            { 
                return _severityType; 
            }

            set 
            { 
                _severityType = value; 
            }
        }
    }
}
