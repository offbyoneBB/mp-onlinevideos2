/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
#region Using directives

#define USE_TRACING
#define USE_LOGGING

using System;
using System.Xml; 
using System.Net;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.IO;
using System.Text; 

#endregion


//////////////////////////////////////////////////////////////////////
// <summary>custom exceptions</summary> 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>standard exception class to be used inside the query object
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [Serializable]
    public class LoggedException : Exception
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor so that FxCop does not complain</summary> 
        //////////////////////////////////////////////////////////////////////
        public LoggedException()
        {
            
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        /// <param name="msg">msg for the exception</param>
        //////////////////////////////////////////////////////////////////////
        public LoggedException(string msg) : base(msg)
        {
            LoggedException.EnsureLogging();
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        /// <param name="msg">msg for the exception</param>
        /// <param name="exception">inner exception</param>
        //////////////////////////////////////////////////////////////////////
        public LoggedException(string msg, Exception exception) : base(msg,exception)
        {
            LoggedException.EnsureLogging();
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and maybe for future use</summary> 
        protected LoggedException(SerializationInfo info,  StreamingContext context) : base(info, context)
        {
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>protected void EnsureLogging()</summary> 
        //////////////////////////////////////////////////////////////////////
        [Conditional("USE_LOGGING")] protected static void EnsureLogging()
        {
         }
        /////////////////////////////////////////////////////////////////////////////

    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>standard exception class to be used inside the query object
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [Serializable]
    public class ClientQueryException : LoggedException
    {
        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor so that FxCop does not complain</summary> 
        //////////////////////////////////////////////////////////////////////
        public ClientQueryException()
        {

        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        /// <param name="msg">msg for the exception</param>
        //////////////////////////////////////////////////////////////////////
        public ClientQueryException(string msg) : base(msg)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and for future use</summary> 
        public ClientQueryException(string msg, Exception innerException) : base(msg, innerException)
        {
        }

        /// <summary>here to please FxCop and maybe for future use</summary> 
        protected ClientQueryException(SerializationInfo info,  StreamingContext context) : base(info, context)
        {
        }
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>standard exception class to be used inside the feed object
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [Serializable]
    public class ClientFeedException : LoggedException
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor so that FxCop does not complain</summary> 
        //////////////////////////////////////////////////////////////////////
        public ClientFeedException()
        {

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        /// <param name="msg">msg for the exception</param>
        //////////////////////////////////////////////////////////////////////
        public ClientFeedException(string msg) : base(msg)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and for future use</summary> 
        public ClientFeedException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and maybe for future use</summary> 
        protected ClientFeedException(SerializationInfo info,  StreamingContext context) : base(info, context)
        {
        }
    }
    /////////////////////////////////////////////////////////////////////////////

      //////////////////////////////////////////////////////////////////////
    /// <summary>standard exception class to be used inside the feed object
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [Serializable]
    public class GDataBatchRequestException : LoggedException
    {
        private AtomFeed batchResult;

        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataBatchRequestException()
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor so that FxCop does not complain</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataBatchRequestException(AtomFeed batchResult)
        {
            this.batchResult = batchResult;
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the BatchResult Feed that contains the problem
        /// </summary>
        public AtomFeed BatchResult
        {
            get { return this.batchResult; }
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>standard overloaded constructor</summary> 
        /// <param name="msg">msg for the exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchRequestException(string msg) : base(msg)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and for future use</summary> 
        public GDataBatchRequestException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and maybe for future use</summary> 
        protected GDataBatchRequestException(SerializationInfo info,  StreamingContext context) : base(info, context)
        {
        }
    }
    /////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////
    /// <summary>standard exception class to be used inside GDataRequest
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [Serializable]
    public class GDataRequestException : LoggedException
    {

        /// <summary>holds the webresponse object</summary> 
        protected WebResponse webResponse;
        /// <summary>cache to hold the responseText in an error scenario</summary>
        protected string      responseText;

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor so that FxCop does not complain</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataRequestException()
        {

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for response</summary> 
        //////////////////////////////////////////////////////////////////////
        public WebResponse Response
        {
            get {return this.webResponse;}
        }

        /// <summary>
        /// this uses the webresponse object to get at the
        /// stream send back from the server.
        /// </summary>
        /// <returns>the error message</returns>
        protected string ReadResponseString()
        {
            if (this.webResponse == null)
                return (null);

            Stream responseStream = this.webResponse.GetResponseStream();

			for (int i = 0; i < this.webResponse.Headers.Count; ++i) {
				string headerVal = this.webResponse.Headers[i].ToLower();
				if (headerVal.Contains("gzip")) {
					responseStream = new System.IO.Compression.GZipStream(responseStream,
						System.IO.Compression.CompressionMode.Decompress);
					break;
				}
				if (headerVal.Contains("deflate")) {
					responseStream = new System.IO.Compression.DeflateStream(responseStream,
						System.IO.Compression.CompressionMode.Decompress);
					break;
				}
			}

			if (responseStream == null)
				return (null);

            StreamReader reader = new StreamReader(responseStream);
            return (reader.ReadToEnd());
        }

        /// <summary>
        /// this is the error message returned by the server
        /// </summary>
        public string ResponseString
        {
            get
            {
                if (this.responseText == null)
                    this.responseText = ReadResponseString();
                return (this.responseText);
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>public GDataRequestException(WebException e)</summary> 
        /// <param name="msg"> the exception message as a string</param>
        /// <param name="exception"> the inner exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataRequestException(string msg, Exception exception) : base(msg, exception)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public GDataRequestException(WebException e)</summary> 
        /// <param name="msg"> the exception message as a string</param>
        //////////////////////////////////////////////////////////////////////
        public GDataRequestException(string msg) : base(msg)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public GDataRequestException(WebException e)</summary> 
        /// <param name="msg"> the exception message as a string</param>
        /// <param name="exception"> the inner exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataRequestException(string msg, WebException exception) : base(msg, exception)
        {
            if (exception != null)
            {
                this.webResponse = exception.Response;    
            }
            
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>public GDataRequestException(WebException e)</summary> 
        /// <param name="msg"> the exception message as a string</param>
        /// <param name="response"> the webresponse object that caused the exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataRequestException(string msg, WebResponse response) : base(msg)
        {
            this.webResponse = response;
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>here to please FxCop and maybe for future use</summary> 
        protected GDataRequestException(SerializationInfo info,  StreamingContext context) : base(info, context)
        {
        }

        /// <summary>overridden to make FxCop happy and future use</summary> 
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData( SerializationInfo info,  StreamingContext context ) 
        {
            base.GetObjectData( info, context );
        }
    }
    /////////////////////////////////////////////////////////////////////////////

    //////////////////////////////////////////////////////////////////////
    /// <summary>exception class thrown when we encounter an access denied
    /// (HttpSTatusCode.Forbidden) when accessing a server
    /// </summary> 
    //////////////////////////////////////////////////////////////////////    
    public class GDataForbiddenException : GDataRequestException
    {
        //////////////////////////////////////////////////////////////////////
        /// <summary>constructs a forbidden exception</summary> 
        /// <param name="msg"> the exception message as a string</param>
        /// <param name="response"> the webresponse object that caused the exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataForbiddenException(string msg, WebResponse response) : base(msg)
        {
            this.webResponse = response;
        }

    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>exception class thrown when we encounter a redirect
    /// (302 and 307) when accessing a server
    /// </summary> 
    //////////////////////////////////////////////////////////////////////    
    public class GDataRedirectException : GDataRequestException
    {
        private string redirectLocation; 
        //////////////////////////////////////////////////////////////////////
        /// <summary>constructs a redirect execption</summary> 
        /// <param name="msg"> the exception message as a string</param>
        /// <param name="response"> the webresponse object that caused the exception</param>
        //////////////////////////////////////////////////////////////////////
        public GDataRedirectException(string msg, WebResponse response) : base(msg)
        {
            this.webResponse = response;
            if (response != null && response.Headers != null)
            {
                this.redirectLocation = response.Headers["Location"]; 
            }
            
        }


        /// <summary>
        /// returns the location header of the webresponse object
        /// which should be the location we should redirect to
        /// </summary>
        public string Location 
        {
            get 
            {
                return this.redirectLocation != null ? this.redirectLocation : "";
            }
        }

    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>exception class thrown when we encounter a not-modified
    /// response (HttpStatusCode.NotModified) when accessing a server
    /// </summary> 
    //////////////////////////////////////////////////////////////////////    
    public class GDataNotModifiedException : GDataRequestException
    {
      //////////////////////////////////////////////////////////////////////
      /// <summary>constructs a not modified exception</summary> 
      /// <param name="msg"> the exception message as a string</param>
      /// <param name="response"> the webresponse object that caused the exception</param>
      //////////////////////////////////////////////////////////////////////
      public GDataNotModifiedException(string msg, WebResponse response)
        : base(msg)
      {
        this.webResponse = response;
      }

    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>exception class is thrown when you tried 
    ///      to modified/update a resource and the server detected a version 
    ///        conflict.
    ///  </summary> 
    //////////////////////////////////////////////////////////////////////    
    public class GDataVersionConflictException : GDataRequestException
    {
      //////////////////////////////////////////////////////////////////////
      /// <summary>constructs a version conflict exeception</summary> 
      /// <param name="msg"> the exception message as a string</param>
      /// <param name="response"> the webresponse object that caused the exception</param>
      //////////////////////////////////////////////////////////////////////
      public GDataVersionConflictException(string msg, WebResponse response)
        : base(msg)
      {
        this.webResponse = response;
      }

    }
} /////////////////////////////////////////////////////////////////////////////
