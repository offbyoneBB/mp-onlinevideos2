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
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Removed warnings
* 
*/
#region Using directives

#define USE_TRACING

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.ComponentModel;

#endregion

/////////////////////////////////////////////////////////////////////
// <summary>contains GDataRequest, our thin wrapper class for request/response
// </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>constants for the authentication handler
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public static class GoogleAuthentication
    {
        ///  <summary>account prefix path </summary>
        public const string AccountPrefix = "/accounts";

        ///  <summary>protocol  </summary>
        public const string DefaultProtocol = "https";
        /// <summary>
        /// default authentication domain
        /// </summary>
        public const string DefaultDomain = "www.google.com";

        /// <summary>Google client authentication handler</summary>
        public const string UriHandler = "https://www.google.com/accounts/ClientLogin"; 
        /// <summary>Google client authentication email</summary>
        public const string Email = "Email";
        /// <summary>Google client authentication password</summary>
        public const string Password = "Passwd";
        /// <summary>Google client authentication source constant</summary>
        public const string Source = "source";
        /// <summary>Google client authentication default service constant</summary>
        public const string Service = "service";
        /// <summary>Google client authentication LSID</summary>
        public const string Lsid = "LSID";
        /// <summary>Google client authentication SSID</summary>
        public const string Ssid = "SSID";
        /// <summary>Google client authentication Token</summary>
        public const string AuthToken = "Auth"; 
        /// <summary>Google authSub authentication Token</summary>
        public const string AuthSubToken = "Token"; 
        /// <summary>Google client header</summary>
        public const string Header = "Authorization: GoogleLogin auth="; 
        /// <summary>Google method override header</summary>
        public const string Override = "X-HTTP-Method-Override"; 
        /// <summary>Google webkey identifier</summary>
        public const string WebKey = "X-Google-Key: key=";
        /// <summary>Google YouTube client identifier</summary>
        public const string YouTubeClientId = "X-GData-Client:";
        /// <summary>Google YouTube developer identifier</summary>
        public const string YouTubeDevKey = "X-GData-Key: key=";
        /// <summary>Google webkey identifier</summary>
        public const string AccountType = "accountType=";
        /// <summary>default value for the account type</summary>
        public const string AccountTypeDefault = "HOSTED_OR_GOOGLE";
        /// <summary>captcha url token</summary>
        public const string CaptchaAnswer = "logincaptcha";
        /// <summary>default value for the account type</summary>
        public const string CaptchaToken = "logintoken";
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequestFactory implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataGAuthRequestFactory : GDataRequestFactory, IVersionAware
    {
        /// <summary>
        ///  the header used to indicate version requests
        /// </summary>
        public const string GDataVersion = "GData-Version"; 

        private string gAuthToken;   // we want to remember the token here
        private string handler;      // so the handler is useroverridable, good for testing
        private string gService;         // the service we pass to Gaia for token creation
        private string applicationName;  // the application name we pass to Gaia and append to the user-agent
        private bool fMethodOverride;    // to override using post, or to use PUT/DELETE
        private int numberOfRetries;        // holds the number of retries the request will undertake
        private bool fStrictRedirect;       // indicates if redirects should be handled strictly
        private string accountType;         // indicates the accountType to use
        private string captchaAnswer;       // indicates the captcha Answer in a challenge
        private string captchaToken;        // indicates the captchaToken in a challenge

		private const int RetryCount = 3;	// default retry count for failed requests       
                                         

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
		public GDataGAuthRequestFactory(string service, string applicationName)
			: this(service, applicationName, RetryCount)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

		//////////////////////////////////////////////////////////////////////
		/// <summary>overloaded constructor</summary> 
		//////////////////////////////////////////////////////////////////////
		public GDataGAuthRequestFactory(string service, string applicationName, int numberOfRetries)
			: base(applicationName) {
			this.Service = service;
			this.ApplicationName = applicationName;
			this.NumberOfRetries = numberOfRetries;
		}
		/////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget)
        {
            return new GDataGAuthRequest(type, uriTarget, this); 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Get/Set accessor for gAuthToken</summary> 
        //////////////////////////////////////////////////////////////////////
        public string GAuthToken
        {
            get {return this.gAuthToken;}
            set {
                Tracing.TraceMsg("set token called with: " + value); 
                this.gAuthToken = value;
                }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>Get's an authentication token for the current credentials</summary> 
        //////////////////////////////////////////////////////////////////////
        public string QueryAuthToken(GDataCredentials gc)
        {
            GDataGAuthRequest request = new GDataGAuthRequest(GDataRequestType.Query, null, this);
            return request.QueryAuthToken(gc);
        }
        /////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string UserAgent, with GFE support</summary> 
        /// <remarks>GFE will enable gzip support ONLY for browser that have the string
        /// "gzip" in their user agent (IE or Mozilla), since lot of browsers have a
        /// broken gzip support.</remarks>
        ////////////////////////////////////////////////////////////////////////////////
        public override string UserAgent
        {
            get { return (base.UserAgent + (this.UseGZip ? " (gzip)" : "")); }
            set { base.UserAgent = value; }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Get/Set accessor for the application name</summary> 
        //////////////////////////////////////////////////////////////////////
        public string ApplicationName
        {
            get {return this.applicationName == null ? "" : this.applicationName;}
            set {this.applicationName = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>returns the service string</summary> 
        //////////////////////////////////////////////////////////////////////
        public string Service
        {
            get {return this.gService;}
            set {this.gService = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>Let's assume you are behind a corporate firewall that does not 
        /// allow all HTTP verbs (as you know, the atom protocol uses GET, 
        /// POST, PUT and DELETE). If you set MethodOverride to true,
        /// PUT and DELETE will be simulated using HTTP Post. It will
        /// add an X-Method-Override header to the request that 
        /// indicates the "real" method we wanted to send. 
        /// </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool MethodOverride
        {
            get {return this.fMethodOverride;}
            set {this.fMethodOverride = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>indicates if a redirect should be followed on not HTTPGet</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool StrictRedirect
        {
            get {return this.fStrictRedirect;}
            set {this.fStrictRedirect = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// property accessor to adjust how often a request of this factory should retry
        /// </summary>
        public int NumberOfRetries
        {
            get { return this.numberOfRetries; }
            set { this.numberOfRetries = value; }
        }

        /// <summary>
        /// property accessor to set the account type that is used during
        /// authentication. Defaults, if not set, to HOSTED_OR_GOOGLE
        /// </summary>
        public string AccountType
        {
            get { return this.accountType; }
            set { this.accountType = value; }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>property to hold the Answer for a challenge</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string CaptchaAnswer
        {
            get {return this.captchaAnswer;}
            set {this.captchaAnswer = value;}
        }
        // end of accessor public string CaptchaUrl

        //////////////////////////////////////////////////////////////////////
        /// <summary>property to hold the token for a challenge</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string CaptchaToken
        {
            get {return this.captchaToken;}
            set {this.captchaToken = value;}
        }
        // end of accessor public string CaptchaToken


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Handler</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Handler
        {
            get {

                return this.handler!=null ? this.handler : GoogleAuthentication.UriHandler; 
            }
            set {this.handler = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        private VersionInformation versionInfo = new VersionInformation();
        /// <summary>
        /// returns the major protocol version number this element 
        /// is working against. 
        /// </summary>
        /// <returns></returns>
        public int ProtocolMajor
        {
            get
            {
                return this.versionInfo.ProtocolMajor;
            }
            set
            {
                this.versionInfo.ProtocolMajor = value;
            }
        }

        /// <summary>
        /// returns the minor protocol version number this element 
        /// is working against. 
        /// </summary>
        /// <returns></returns>
        public int ProtocolMinor
        {
            get
            {
                return this.versionInfo.ProtocolMinor;
            }
            set
            {
                this.versionInfo.ProtocolMinor = value;
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequest implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataGAuthRequest : GDataRequest
    {
        /// <summary>holds the input in memory stream</summary> 
        private MemoryStream requestCopy;
        /// <summary>holds the factory instance</summary> 
        private GDataGAuthRequestFactory factory; 
        private AsyncData asyncData;
        private VersionInformation responseVersion;
        
        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        internal GDataGAuthRequest(GDataRequestType type, Uri uriTarget, GDataGAuthRequestFactory factory)  : base(type, uriTarget, factory as GDataRequestFactory)
        {
            // need to remember the factory, so that we can pass the new authtoken back there if need be
            this.factory = factory; 
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>returns the writable request stream</summary> 
        /// <returns> the stream to write into</returns>
        //////////////////////////////////////////////////////////////////////
        public override Stream GetRequestStream()
        {
            this.requestCopy = new MemoryStream(); 
            return this.requestCopy; 
        }
        /////////////////////////////////////////////////////////////////////////////

       //////////////////////////////////////////////////////////////////////
       /// <summary>Read only accessor for requestCopy</summary> 
       //////////////////////////////////////////////////////////////////////
       internal Stream RequestCopy
       {
           get {return this.requestCopy;}
       }
       /////////////////////////////////////////////////////////////////////////////
       

       internal AsyncData AsyncData
       {
           set 
           {
               this.asyncData = value;
           }
       }

        
        //////////////////////////////////////////////////////////////////////
        /// <summary>does the real disposition</summary> 
        /// <param name="disposing">indicates if dispose called it or finalize</param>
        //////////////////////////////////////////////////////////////////////
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); 
            if (this.disposed)
            {
                return;
            }
            if (disposing)
            {
                if (this.requestCopy != null)
                {
                    this.requestCopy.Close();
                    this.requestCopy = null;
                }
                this.disposed = true;
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>sets up the correct credentials for this call, pending 
        /// security scheme</summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void EnsureCredentials()
        {
            Tracing.Assert(this.Request!= null, "We should have a webrequest now"); 
            if (this.Request == null)
            {
                return; 
            }
            // if the token is NULL, we need to get a token. 
            if (this.factory.GAuthToken == null)
            {
                // we will take the standard credentials for that
                GDataCredentials gc = this.Credentials;
                Tracing.TraceMsg(gc == null ? "No Network credentials set" : "Network credentials found"); 
                if (gc != null)
                {
                    // only now we have something to do... 
                    this.factory.GAuthToken = QueryAuthToken(gc); 
                }
            }
            if (this.factory.GAuthToken != null && this.factory.GAuthToken.Length > 0)
            {
                // Tracing.Assert(this.factory.GAuthToken != null, "We should have a token now"); 
                Tracing.TraceMsg("Using auth token: " + this.factory.GAuthToken);
                string strHeader = GoogleAuthentication.Header + this.factory.GAuthToken;
                this.Request.Headers.Add(strHeader);
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// returns the version information that the response indicated
        /// can be NULL if used against a non versioned endpoint
        /// </summary>
        internal VersionInformation ResponseVersion
        {
            get
            {
                return this.responseVersion;
            }
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>sets the redirect to false after everything else
        /// is done </summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void EnsureWebRequest()
        {
            base.EnsureWebRequest(); 
            HttpWebRequest http = this.Request as HttpWebRequest; 
            if (http != null)
            {

                http.Headers.Remove(GDataGAuthRequestFactory.GDataVersion);
      
                // as we are doublebuffering due to redirect issues anyhow, 
                // disallow the default buffering
                http.AllowWriteStreamBuffering = false; 

                IVersionAware v = this.factory as IVersionAware;
                if (v != null)
                {
                    // need to add the version header to the request
                    http.Headers.Add(GDataGAuthRequestFactory.GDataVersion, v.ProtocolMajor.ToString() + "." + v.ProtocolMinor.ToString());
                }

                // we do not want this to autoredirect, our security header will be 
                // lost in that case
                http.AllowAutoRedirect = false;
                if (this.factory.MethodOverride && 
                    http.Method != HttpMethods.Get &&
                    http.Method != HttpMethods.Post)
                {
                    // remove it, if it is already there.
                    http.Headers.Remove(GoogleAuthentication.Override);

                    // cache the method, because Mono will complain if we try
                    // to open the request stream with a DELETE method.
                    string currentMethod = http.Method;

                    http.Headers.Add(GoogleAuthentication.Override, currentMethod);
                    http.Method = HttpMethods.Post;

                    // not put and delete, all is post
                    if (currentMethod == HttpMethods.Delete)
                    {
                        http.ContentLength = 0;
                        // .NET CF won't send the ContentLength parameter if no stream
                        // was opened. So open a dummy one, and close it right after.
                        Stream req = http.GetRequestStream(); 
                        req.Close(); 
                    }
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>goes to the Google auth service, and gets a new auth token</summary> 
        /// <returns>the auth token, or NULL if none received</returns>
        //////////////////////////////////////////////////////////////////////
        internal string QueryAuthToken(GDataCredentials gc)
        {
            Uri authHandler = null; 

            // need to copy this to a new object to avoid that people mix and match
            // the old (factory) and the new (requestsettings) and get screwed. So
            // copy the settings from the gc passed in and mix with the settings from the factory
            GDataCredentials gdc = new GDataCredentials(gc.Username, gc.getPassword());
            gdc.CaptchaToken = this.factory.CaptchaToken;
            gdc.CaptchaAnswer = this.factory.CaptchaAnswer;
            gdc.AccountType = gc.AccountType;

            try 
            {
                authHandler = new Uri(this.factory.Handler); 
            }
            catch
            {
                throw new GDataRequestException("Invalid authentication handler URI given"); 
            }                         

            return Utilities.QueryClientLoginToken(gdc,
                                        this.factory.Service,
                                        this.factory.ApplicationName,
                                        this.factory.KeepAlive,
                                        authHandler);
        }
        /////////////////////////////////////////////////////////////////////////////
    

        //////////////////////////////////////////////////////////////////////
        /// <summary>Executes the request and prepares the response stream. Also 
        /// does error checking</summary> 
        //////////////////////////////////////////////////////////////////////
        public override void Execute()
        {
            // call him the first time
            Execute(1); 
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>Executes the request and prepares the response stream. Also 
        /// does error checking</summary> 
        /// <param name="retryCounter">indicates the n-th time this is run</param>
        //////////////////////////////////////////////////////////////////////
        protected void Execute(int retryCounter)
        {
            Tracing.TraceCall("GoogleAuth: Execution called");
            try
            {
                CopyRequestData();
                base.Execute();
                if (this.Response is HttpWebResponse)
                {
                    HttpWebResponse response = this.Response as HttpWebResponse;
                    this.responseVersion = new VersionInformation(response.Headers[GDataGAuthRequestFactory.GDataVersion]);
                }
            }
            catch (GDataForbiddenException) 
            {
                Tracing.TraceMsg("need to reauthenticate, got a forbidden back");
                // do it again, once, reset AuthToken first and streams first
                Reset();
                this.factory.GAuthToken = null; 
                CopyRequestData();
                base.Execute();

            }
            catch (GDataRedirectException re)
            {
                // we got a redirect.
                Tracing.TraceMsg("Got a redirect to: " + re.Location);
                // only reset the base, the auth cookie is still valid
                // and cookies are stored in the factory
                if (this.factory.StrictRedirect)
                {
                    HttpWebRequest http = this.Request as HttpWebRequest; 
                    if (http != null)
                    {
                        // only redirect for GET, else throw
                        if (http.Method != HttpMethods.Get) 
                        {
                            throw; 
                        }
                    }
                }

                // verify that there is a non empty location string
                if (re.Location.Trim().Length == 0)
                {
                    throw;
                }

                Reset();
                this.TargetUri = new Uri(re.Location);
                CopyRequestData();
                base.Execute();
            }
            catch (GDataRequestException re)
            {
                if (((HttpWebResponse)re.Response).StatusCode != HttpStatusCode.InternalServerError)
                {
                    Tracing.TraceMsg("Not a server error. Possibly a Bad request or forbidden resource.");
                    Tracing.TraceMsg("We don't want to retry non 500 errors.");
                    throw;
                }
                if (retryCounter > this.factory.NumberOfRetries)
                {
                    Tracing.TraceMsg("Number of retries exceeded");
                    throw;
                }
                Tracing.TraceMsg("Let's retry this"); 
                // only reset the base, the auth cookie is still valid
                // and cookies are stored in the factory
                Reset();
                this.Execute(retryCounter + 1); 
            }
            catch (Exception e)
            {
                Tracing.TraceCall("*** EXCEPTION " + e.GetType().Name + " CAUGHT ***");
                throw; 
            }
            finally
            {
                if (this.requestCopy != null)
                {
                    this.requestCopy.Close();
                    this.requestCopy = null;
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>takes our copy of the stream, and puts it into the request stream</summary> 
        //////////////////////////////////////////////////////////////////////
        protected void CopyRequestData()
        {
            if (this.requestCopy != null)
            {
                // Since we don't use write buffering on the WebRequest object,
                // we need to ensure the Content-Length field is correctly set
                // to the length we want to set.
                EnsureWebRequest();
                this.Request.ContentLength = this.requestCopy.Length;
                // stream it into the real request stream
                Stream req = base.GetRequestStream();

                try
                {
                    const int size = 4096;
                    byte[] bytes = new byte[size];
                    int numBytes;

                    double oneLoop = 100;
                    if (requestCopy.Length > size)
                    {
                        oneLoop = (100 / ((double)this.requestCopy.Length / size));

                    }

                    // 3 lines of debug code
                    // this.requestCopy.Seek(0, SeekOrigin.Begin);

                    // StreamReader reader = new StreamReader( this.requestCopy );
                    // string text = reader.ReadToEnd();


                    this.requestCopy.Seek(0, SeekOrigin.Begin);

#if WindowsCE || PocketPC
#else
                    long bytesWritten = 0;
                    double current = 0;
#endif
                    while ((numBytes = this.requestCopy.Read(bytes, 0, size)) > 0)
                    {
                        req.Write(bytes, 0, numBytes);

#if WindowsCE || PocketPC
#else
                        bytesWritten += numBytes;
                        if (this.asyncData != null && this.asyncData.Delegate != null &&
                            this.asyncData.DataHandler != null)
                        {
                            AsyncOperationProgressEventArgs args;
                            args = new AsyncOperationProgressEventArgs(this.requestCopy.Length, 
                                            bytesWritten, (int)current, 
                                            this.Request.RequestUri,
                                            this.Request.Method,
                                            this.asyncData.UserData);
                            current += oneLoop;
                            if (!this.asyncData.DataHandler.SendProgressData(asyncData, args))
                                break;         
                        }
#endif
                    }
                }
                finally
                {
                    req.Close();
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////
    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
