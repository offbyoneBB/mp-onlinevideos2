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

using System;
using System.IO;
using System.Net;

#endregion

/////////////////////////////////////////////////////////////////////
// <summary>contains GDataLogingRequest
//  </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequestFactory implmentation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataLoggingRequestFactory : GDataGAuthRequestFactory
    {
        /// <summary>holds the filename for the input request</summary> 
        private string strInput;
        /// <summary>holds the filename for the output response</summary> 
        private string strOutput; 
        /// <summary>holds the filename for the combined logger</summary> 
        private string strCombined; 

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string RequestFileName</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string RequestFileName
        {
            get {return this.strInput;}
            set {this.strInput = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ResponseFileName</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string ResponseFileName
        {
            get {return this.strOutput;}
            set {this.strOutput = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string CombinedLogFileName</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string CombinedLogFileName
        {
            get {return this.strCombined;}
            set {this.strCombined = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public GDataLoggingRequestFactory(string service, string applicationName) : base(service, applicationName)
        {
            this.strInput = "GDatarequest.xml";
            this.strOutput = "GDataresponse.xml"; 
            this.strCombined = "GDatatraffic.log"; 

        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget)
        {
            return new GDataLoggingRequest(type, uriTarget, this, this.strInput, this.strOutput, this.strCombined);
        }
        /////////////////////////////////////////////////////////////////////////////



    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>base GDataRequest implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GDataLoggingRequest : GDataGAuthRequest, IDisposable
    {
        /// <summary>holds the filename for the input request</summary> 
        private string strInput;
        /// <summary>holds the filename for the output response</summary> 
        private string strOutput; 
        /// <summary>holds the filename for the combined logger</summary> 
        private string strCombined; 
       
        private MemoryStream memoryStream;
        


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        internal GDataLoggingRequest(GDataRequestType type, Uri uriTarget, GDataGAuthRequestFactory factory, string strInputFileName, string strOutputFileName, string strCombinedLogFileName) : base(type, uriTarget, factory)
        {
            this.strInput = strInputFileName;
            this.strOutput = strOutputFileName;
            this.strCombined = strCombinedLogFileName;
        }
        /////////////////////////////////////////////////////////////////////////////

        
        //////////////////////////////////////////////////////////////////////
        /// <summary>does the real disposition</summary> 
        /// <param name="disposing">indicates if dispose called it or finalize</param>
        //////////////////////////////////////////////////////////////////////
        protected override void Dispose(bool disposing)
        {
            try
            {
                this.memoryStream.Close();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>Executes the request and prepares the response stream. Also 
        /// does error checking</summary> 
        //////////////////////////////////////////////////////////////////////
        public override void Execute()
        {
            try
            {
                base.Execute(); 

            } catch (GDataRequestException re)
            {
                Tracing.TraceMsg("Got into exception handling for base.execute"); 
                HttpWebResponse response = re.Response as HttpWebResponse;

                // save the response to the log
                StreamWriter w = new StreamWriter(this.strOutput); 
                StreamWriter x = new StreamWriter(this.strCombined, true, System.Text.Encoding.UTF8, 512);


                if (response != null)
                {
                    SaveHeaders(false, response.Headers, null, response.ResponseUri, w);
                    SaveHeaders(false, response.Headers, null, response.ResponseUri, x);

                    Stream req = response.GetResponseStream(); 
                    SaveStream(req, x, w); 
                }
                w.Close();
                x.Close();
                throw; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////





        //////////////////////////////////////////////////////////////////////
        /// <summary>Log's the request object if overridden in subclass</summary>
        /// <param name="request">the request to log</param> 
        //////////////////////////////////////////////////////////////////////
        protected override void LogRequest(WebRequest request) 
        {

            // save the response to the log
            StreamWriter w = new StreamWriter(this.strInput); 
            StreamWriter x = new StreamWriter(this.strCombined, true, System.Text.Encoding.UTF8, 512);

            HttpWebRequest r = request as HttpWebRequest;

            if (r != null)
            {
                SaveHeaders(true, r.Headers, r.Method, r.RequestUri, w);
                SaveHeaders(true, r.Headers, r.Method, r.RequestUri, x);
            }
            if (this.RequestCopy != null)
            {
                this.RequestCopy.Seek(0, SeekOrigin.Begin); 
                SaveStream(this.RequestCopy, w, x);
                this.RequestCopy.Seek(0, SeekOrigin.Begin); 
            }
            w.Close();
            x.Close();
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Log's the response object if overridden in subclass</summary>
        /// <param name="response">the response to log</param> 
        //////////////////////////////////////////////////////////////////////
        protected override void LogResponse(WebResponse response) 
        {
            // save the response to the log
            StreamWriter w = new StreamWriter(this.strOutput); 
            StreamWriter x = new StreamWriter(this.strCombined, true, System.Text.Encoding.UTF8, 512);

            HttpWebResponse r = response as HttpWebResponse;
            if (r != null)
            {

                SaveHeaders(false, r.Headers, r.Method, r.ResponseUri, w);
                SaveHeaders(false, r.Headers, r.Method, r.ResponseUri, x);
            }

            Stream result = this.GetResponseStream();

            if (result != null)
            {
                result.Seek(0, SeekOrigin.Begin); 
                SaveStream(result, w, x);
                result.Seek(0, SeekOrigin.Begin); 
            }
            w.Close();
            x.Close();

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>resets the object's state</summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void Reset()
        {
            base.Reset();
            this.memoryStream.Close();
            this.memoryStream = null;
        }
        /////////////////////////////////////////////////////////////////////////////


  
        //////////////////////////////////////////////////////////////////////
        /// <summary>private void SaveStream()</summary> 
        /// <param name="stream">the stream to save </param>
        /// <param name="outOne">the first stream to save into </param>
        /// <param name="outCombined">the combined stream to save into</param>
        //////////////////////////////////////////////////////////////////////
        private static void SaveStream(Stream stream, StreamWriter outOne, StreamWriter outCombined)
        {
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);

                String line;

                while ((line = reader.ReadLine())!= null)
                {
                    outOne.WriteLine(line);
                    outCombined.WriteLine(line);
                }
                outOne.Close();
                outCombined.WriteLine();
                outCombined.Close(); 

            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>private void SaveStream()</summary> 
        /// <param name="isRequest">indicates wether this is a request or a response log</param>
        /// <param name="headers"> the webheader collection to save</param>
        /// <param name="method"> indicates the HTTP method used</param>
        /// <param name="target">the target URI of the request</param>
        /// <param name="outputStream">the stream to save to</param>
        //////////////////////////////////////////////////////////////////////
        private static void SaveHeaders(bool isRequest, WebHeaderCollection headers, String method, Uri target, StreamWriter outputStream)
        {
            if (outputStream != null && headers != null)
            {
                if (isRequest)
                {
                    outputStream.WriteLine("Request at: " + DateTime.Now);
                } 
                else 
                {
                    outputStream.WriteLine("Response received at: " + DateTime.Now);
                }

                if (method != null)
                {
                    outputStream.WriteLine(method + " to: " + target.ToString());
                }
                foreach (String key in headers.AllKeys)
                {
                    outputStream.WriteLine("Header: " + key + ":" + headers[key]);
                }
                outputStream.Flush();
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>gets the readable response stream. In the logger, we need to
        /// copy the response to be able to log it. So we return a memory stream</summary> 
        /// <returns> the response stream</returns>
        //////////////////////////////////////////////////////////////////////
        public override Stream GetResponseStream()
        {
            if (this.memoryStream == null) 
            {
                Stream   req = base.GetResponseStream();
                if (req != null)
                {
                    this.memoryStream = new MemoryStream(); 
                    const int size = 4096;
                    byte[] bytes = new byte[4096];
                    int numBytes;

                    while ((numBytes = req.Read(bytes, 0, size)) > 0)
                    {
                        this.memoryStream.Write(bytes, 0, numBytes);
                    }
                    this.memoryStream.Seek(0, SeekOrigin.Begin);
                }
            }
            return this.memoryStream; 
        }
        /////////////////////////////////////////////////////////////////////////////

    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
