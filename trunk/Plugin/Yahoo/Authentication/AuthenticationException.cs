using System;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Yahoo
{
    /// <summary>
    /// The exception that is thrown when an error occurs while accessing user credentials or authenticated web services.
    /// </summary>
    [Serializable]
    public class AuthenticationException : System.Exception
    {
        #region Private fields

        private Yahoo.AuthenticationErrorCode _errorCode;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class.
        /// </summary>
        public AuthenticationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with the specified error message.
        /// </summary>
        /// <param name="message">The text of the error message.</param>
        public AuthenticationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with the specified error message and error code.
        /// </summary>
        /// <param name="message">The text of the error message.</param>
        /// <param name="errorCode">A nested exception.</param>
        public AuthenticationException(string message, Yahoo.AuthenticationErrorCode errorCode)
            : base(message)
        {
            _errorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with the specified error message and nested exception.
        /// </summary>
        /// <param name="message">The text of the error message.</param>
        /// <param name="innerException">A nested exception.</param>
        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with the specified error message, nested exception and error code.
        /// </summary>
        /// <param name="message">The text of the error message.</param>
        /// <param name="innerException">A nested exception.</param>
        /// <param name="errorCode">The <see cref="Yahoo.AuthenticationErrorCode">AuthenticationErrorCode</see>.</param>
        public AuthenticationException(string message, Exception innerException, Yahoo.AuthenticationErrorCode errorCode)
            : base(message, innerException)
        {
            _errorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class from the specified SerializationInfo and StreamingContext instances.
        /// </summary>
        /// <param name="info">A SerializationInfo that contains the information required to serialize the new AuthenticationException.</param>
        /// <param name="context">A StreamingContext that contains the source of the serialized stream that is associated with the new AuthenticationException.</param>
        protected AuthenticationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _errorCode = (Yahoo.AuthenticationErrorCode)info.GetInt32("ErrorCode");
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the <see cref="Yahoo.AuthenticationErrorCode">AuthenticationErrorCode</see> returned.
        /// </summary>
        public Yahoo.AuthenticationErrorCode ErrorCode
        {
            get { return _errorCode; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// This method supports the .NET Framework infrastructure and is not intended to be used directly from your code. 
        /// Populates a SerializationInfo instance with the data needed to serialize the AuthenticationException. 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
            info.AddValue("ErrorCode", (int)_errorCode);
        }

        #endregion
    }
}
