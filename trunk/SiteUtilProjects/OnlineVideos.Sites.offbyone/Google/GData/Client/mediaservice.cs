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
using System.Xml;
using System.IO;
using System.Net;

#endregion

/////////////////////////////////////////////////////////////////////
// <summary>contains Service, the base interface that 
//   allows to query a service for different feeds
//  </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

   
    //////////////////////////////////////////////////////////////////////
    /// <summary>MediaService implementation. Adds the ability to send MimeMultipart
    /// message (used for Piasa/YouTube etc
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class MediaService : Service
    {
        //////////////////////////////////////////////////////////////////////
        private const string MimeBoundary = "END_OF_PART";
        private const string MimeContentType = "multipart/related; boundary=\"" + MimeBoundary + "\"";
        

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor, sets the default GDataRequest</summary> 
        //////////////////////////////////////////////////////////////////////
        public MediaService(string applicationName) : base(applicationName)
        {
        }
        /////////////////////////////////////////////////////////////////////////////
 

        //////////////////////////////////////////////////////////////////////
        /// <summary>this will trigger the creation of an authenticating service</summary> 
        //////////////////////////////////////////////////////////////////////
        public MediaService(string service, string applicationName) : base(service, applicationName)
        {
        }
        /////////////////////////////////////////////////////////////////////////////
 

        //////////////////////////////////////////////////////////////////////
        /// <summary>Inserts an AtomBase entry against a Uri. The overloaded
        /// version here will check if this is an AbstractEntry and if it has
        /// a media property set. If so, it will create a mime multipart envelope</summary> 
        /// <param name="feedUri">the uri for the feed this object should be posted against</param> 
        /// <param name="baseEntry">the entry to be inserted</param> 
        /// <param name="type">the type of request to create</param> 
        /// <param name="data">the async data payload</param>
        /// <returns> the response as a stream</returns>
        //////////////////////////////////////////////////////////////////////
        internal override Stream EntrySend(Uri feedUri, AtomBase baseEntry, GDataRequestType type, AsyncSendData data)
        {
            if (feedUri == null)
            {
                throw new ArgumentNullException("feedUri"); 
            }
            Tracing.Assert(baseEntry != null, "baseEntry should not be null");
            if (baseEntry == null)
            {
                throw new ArgumentNullException("baseEntry"); 
            }

            AbstractEntry entry = baseEntry as AbstractEntry;
            // if the entry is not an abstractentry or if no media is set, do the default
            if (entry == null || entry.MediaSource == null)
            {
                return base.EntrySend(feedUri, baseEntry, type, data);
            }

            Stream outputStream = null;
            Stream inputStream=null;
            try
            {
                IGDataRequest request = this.RequestFactory.CreateRequest(type,feedUri);
                request.Credentials = this.Credentials;
    
                GDataRequest r = request as GDataRequest;
    
                if (r != null) 
                {
                    r.ContentType = MediaService.MimeContentType;
                    r.Slug = entry.MediaSource.Name;
    
                    GDataRequestFactory f = this.RequestFactory as GDataRequestFactory;
                    if (f != null)
                    {
                        f.CustomHeaders.Add("MIME-version: 1.0");
                    }
                }

                if (data != null)
                {
                    GDataGAuthRequest gr = request as GDataGAuthRequest;
                    if (gr != null)
                    {
                        gr.AsyncData = data;
                    }
                }

    
                outputStream = request.GetRequestStream();
                inputStream = entry.MediaSource.GetDataStream();
                StreamWriter w = new StreamWriter(outputStream);

                w.WriteLine("Media multipart posting");
                CreateBoundary(w, GDataRequestFactory.DefaultContentType);
                baseEntry.SaveToXml(outputStream);
                w.WriteLine();
                CreateBoundary(w, entry.MediaSource.ContentType);
                WriteInputStreamToRequest(inputStream, outputStream);
                w.WriteLine();
                w.WriteLine("--" + MediaService.MimeBoundary + "--");
                w.Flush();
                request.Execute();
                outputStream.Close();
                outputStream = null;
                return request.GetResponseStream();
            }
            catch (Exception)
            {
                throw; 
            }
            finally
            {
                if (outputStream != null)
                {
                    outputStream.Close();
                }
                if (inputStream != null)
                {
                    inputStream.Close();
                }
            }
        }
    
        /// <summary>
        /// creates the MIME boundary string
        /// </summary>
        /// <param name="w">stream to write to</param>
        /// <param name="contentType">content type to use</param>
        protected void CreateBoundary(StreamWriter w, string contentType)
        {
            w.WriteLine("--" + MediaService.MimeBoundary);
            w.WriteLine("Content-Type: " + contentType);
            if (contentType != GDataRequestFactory.DefaultContentType)
            {
                w.WriteLine("Content-Transfer-Encoding: binary");
            }
            w.WriteLine();
            w.Flush();
        }

    }
    /////////////////////////////////////////////////////////////////////////////
} 
/////////////////////////////////////////////////////////////////////////////
