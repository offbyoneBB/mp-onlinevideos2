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
using System.Threading;
using System.ComponentModel;
using System.Collections.Specialized;



#endregion

/////////////////////////////////////////////////////////////////////
// <summary>contains Service, the base interface that 
//   allows to query a service for different feeds
//  </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    /// <summary>
    /// EventArgument class for service level events during parsing
    /// </summary>
    public class ServiceEventArgs : EventArgs
    {
        private AtomFeed feedObject;
        private IService service;
        private Uri uri;

        /// <summary>
        /// constructor. Takes the URI and the service this event applies to
        /// </summary>
        /// <param name="uri">URI currently executed</param>
        /// <param name="service">service object doing the execution</param>
        public ServiceEventArgs(Uri uri, IService service) 
        {
            this.service = service;
            this.uri = uri;
        }
    
        /// <summary>the feed to be created. If this is NULL, a service 
        /// will create a DEFAULT atomfeed</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomFeed Feed
        {
            get {return this.feedObject;}
            set {this.feedObject = value;}
        }
        ////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>the service to be used for the feed to be created. </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public IService Service
        {
            get {return this.service;}
        }
        ////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>the Uri to be used</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public Uri Uri
        {
            get {return this.uri;}
        }
        ////////////////////////////////////////////////////////////////////////
   }



    /// <summary>Delegate declaration for the feed creation in a service</summary> 
    public delegate void ServiceEventHandler(object sender, ServiceEventArgs e);


   
    //////////////////////////////////////////////////////////////////////
    /// <summary>base Service implementation
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public partial class Service : IService, IVersionAware
    {
        /// <summary>holds the credential information</summary> 
        private GDataCredentials credentials; 
        /// <summary>the GDatarequest to use</summary> 
        private IGDataRequestFactory factory;
        /// <summary>holds the hooks for the eventing in the feedparser</summary> 
        public event FeedParserEventHandler NewAtomEntry;
        /// <summary>eventhandler, when the parser finds a new extension element-> mirrored from underlying parser</summary> 
        public event ExtensionElementEventHandler NewExtensionElement;
        /// <summary>eventhandler, when the service needs to create a new feed</summary> 
        public event ServiceEventHandler NewFeed;

        private string serviceID;


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor, sets the default GDataRequest</summary> 
        //////////////////////////////////////////////////////////////////////
        public Service() : base()
        {
            this.RequestFactory = new GDataRequestFactory(this.GetType().Name);
            InitVersionInformation();
        }
        /////////////////////////////////////////////////////////////////////////////
 

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor, sets the default GDataRequest</summary> 
        //////////////////////////////////////////////////////////////////////
        public Service(string applicationName) : base()
        {
            this.RequestFactory = new GDataRequestFactory(applicationName == null ? 
                                                            this.GetType().Name : 
                                                            applicationName);
            InitVersionInformation();
        }
        /////////////////////////////////////////////////////////////////////////////
 

        //////////////////////////////////////////////////////////////////////
        /// <summary>this will trigger the creation of an authenticating service</summary> 
        //////////////////////////////////////////////////////////////////////
        public Service(string service, string applicationName) : base()
        {
            this.RequestFactory = new GDataGAuthRequestFactory(service, applicationName);
            this.serviceID = service; 
            InitVersionInformation();
        }
        /////////////////////////////////////////////////////////////////////////////
 

        /// <summary>
        /// this returns the string that the services uses to identify the google service to use
        /// when authentication with Google is required. Examples are "cl" for calendar, e.g. 
        /// </summary>
        /// <returns></returns>
        public string ServiceIdentifier
        {
            get
            {
                return this.serviceID;
            }
        }
  
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
                this.versionInfo.ProtocolMajor  = value;
                PropagateVersionInfo();
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
                this.versionInfo.ProtocolMinor  = value;
                PropagateVersionInfo();
            }
        }
        
        /// <summary>
        /// by default all services now use version 2 for the protocol.
        /// this needs to be overridden by a service to specify otherwise. 
        /// </summary>
        /// <returns></returns>
        protected virtual void InitVersionInformation()
        {
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public IGDataRequest Request</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public IGDataRequestFactory RequestFactory
        {
            get { return this.factory; }
            set { this.factory = value; OnRequestFactoryChanged(); }
        }

        /// <summary>
        /// notifier if someone changes the requestfactory of the service. 
        /// This will cause the service to set the versionnumber on the 
        /// request factory to it's own
        /// </summary>
        public virtual void OnRequestFactoryChanged() 
        {
            PropagateVersionInfo();
        }

        private void PropagateVersionInfo()
        {
            IVersionAware v = this.factory as IVersionAware;
            if (v != null)
            {
                v.ProtocolMajor = this.ProtocolMajor;
                v.ProtocolMinor = this.ProtocolMinor;
            }
            return; 
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public ICredentials Credentials</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataCredentials Credentials
        {
            get {return this.credentials;}
            set 
            {
                this.credentials = value; 
                // if we get new credentials, make sure we invalidate the old authtoken
                SetAuthenticationToken(value == null ? null : value.ClientToken);
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// if the service is using a Google Request Factory it will use that 
        /// assuming credentials are set to retrieve the authentication token
        /// for those credentials
        /// 
        /// Note that this only works for ClientLogin, not for any other type of authentication
        /// </summary>
        /// <returns>string</returns>
        [Obsolete("the name is confusing. Do not use this, use QueryClientLoginToken instead")]
        public string QueryAuthenticationToken() 
        {
            if (this.Credentials != null)
            {
                GDataGAuthRequestFactory factory = this.factory as GDataGAuthRequestFactory;
                if (factory != null)
                {
                    return factory.QueryAuthToken(this.Credentials);
                }
            }
            return null;
        }

        /// <summary>
        /// if the service is using a Google Request Factory it will use that 
        /// assuming credentials are set to retrieve the authentication token
        /// for those credentials
        /// 
        /// Note that this only works for ClientLogin, not for any other type of authentication
        /// </summary>
        /// <returns>string</returns>
        public string QueryClientLoginToken()
        {
            if (this.Credentials != null)
            {
                GDataGAuthRequestFactory factory = this.factory as GDataGAuthRequestFactory;
                if (factory != null)
                {
                    return factory.QueryAuthToken(this.Credentials);
                }
            }
            return null;
        }

        /// <summary>
        /// if the service is using a Google Request Factory it will set the passed 
        /// in token to the factory. NET CF does not support authsubtokens here
        /// </summary>
        /// <returns>string</returns>
        public void SetAuthenticationToken(string token) 
        {
            GDataGAuthRequestFactory factory = this.factory as GDataGAuthRequestFactory;
            if (factory != null)
            {
                factory.GAuthToken = token;
            }
#if WindowsCE || PocketPC
#else
            else 
            {
                GAuthSubRequestFactory f = this.factory as GAuthSubRequestFactory;
                if (f != null)
                {
                    f.Token = token;
                }
            }
#endif
        }

        /// <summary>
        /// Sets the credentials of the user to authenticate requests
        /// to the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void setUserCredentials(String username, String password)
        {
            this.Credentials = new GDataCredentials(username, password);
        }



   
        //////////////////////////////////////////////////////////////////////
        /// <summary>the basic interface. Take a URI and just get it</summary> 
        /// <param name="queryUri">the URI to execute</param>
        /// <returns> a webresponse object</returns>
        //////////////////////////////////////////////////////////////////////
        public Stream Query(Uri queryUri)
        {
          return Query(queryUri, DateTime.MinValue);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the basic interface. Take a URI and just get it</summary> 
        /// <param name="queryUri">the URI to execute</param>
        /// <param name="ifModifiedSince">used to set a precondition date that 
        /// indicates the feed should be returned only if it has been modified 
        /// after the specified date. A value of DateTime.MinValue indicates no 
        /// precondition.</param>
        /// <returns> a webresponse object</returns>
        //////////////////////////////////////////////////////////////////////
        public Stream Query(Uri queryUri, DateTime ifModifiedSince)
        {
            long l;
            return this.Query(queryUri, ifModifiedSince, null, out l);
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>the basic interface. Take a URI and just get it</summary> 
        /// <param name="queryUri">the URI to execute</param>
        /// <param name="etag">used to set a precondition etag that 
        /// indicates the feed should be returned only if it has been modified </param>
        /// <returns> a webresponse object</returns>
        //////////////////////////////////////////////////////////////////////
        public Stream Query(Uri queryUri, string etag)
        {
            long l;
            return this.Query(queryUri, DateTime.MinValue, etag, out l);
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>the basic interface. Take a URI and just get it</summary> 
        /// <param name="queryUri">the URI to execute</param>
        /// <param name="ifModifiedSince">used to set a precondition date that 
        /// indicates the feed should be returned only if it has been modified 
        /// after the specified date. A value of DateTime.MinValue indicates no 
        /// precondition.</param>
        /// <param name="etag">used to set a precondition etag that 
        /// indicates the feed should be returned only if it has been modified </param>
        /// <param name="contentLength">returns the content length of the response</param>
        /// <returns> a webresponse object</returns>
        //////////////////////////////////////////////////////////////////////
        private Stream Query(Uri queryUri, DateTime ifModifiedSince, string etag, out long contentLength)
        {
            Tracing.TraceCall("Enter");
            if (queryUri == null)
            {
              throw new System.ArgumentNullException("queryUri");
            }

            contentLength = -1; 

            IGDataRequest request = this.RequestFactory.CreateRequest(GDataRequestType.Query, queryUri);
            request.Credentials = this.Credentials;
            request.IfModifiedSince = ifModifiedSince;

            if (etag != null)
            {
                ISupportsEtag ise = request as ISupportsEtag;
                if (ise != null)
                {
                    ise.Etag = etag; 
                }
            }

            try
            {
              request.Execute();
            }
            catch (Exception)
            {
              // Prevent connection leaks
              if (request.GetResponseStream() != null)
                request.GetResponseStream().Close();

              throw;
            }
            
            // return the response
            GDataGAuthRequest gr = request as GDataGAuthRequest;
            if (gr != null)
            {
                 contentLength = gr.ContentLength;
            }
            
            Tracing.TraceCall("Exit");
            return new GDataReturnStream(request);
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Returns a single Atom entry based upon its unique URI.
        /// </summary>
        /// <param name="entryUri">The URI of the Atom entry.</param>
        /// <returns>AtomEntry representing the entry.</returns>
        public AtomEntry Get(string entryUri)
        {
            FeedQuery query = new FeedQuery(entryUri);
            AtomFeed resultFeed = Query(query);
            return resultFeed.Entries[0];
        }


   
        //////////////////////////////////////////////////////////////////////
        /// <summary>executes the query and returns an AtomFeed object tree</summary> 
        /// <param name="feedQuery">the query parameters as a FeedQuery object </param>
        /// <returns>AtomFeed object tree</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomFeed Query(FeedQuery feedQuery)
        {
            AtomFeed feed = null;
            Tracing.TraceCall("Enter");

            if (feedQuery == null)
            {
                throw new System.ArgumentNullException("feedQuery", "The query argument MUST not be null");
            }
            // Create a new request to the Uri in the query object...    
            Uri targetUri = null;

            try
            {
                targetUri = feedQuery.Uri;

            }
            catch (System.UriFormatException)
            {
                throw new System.ArgumentException("The query argument MUST contain a valid Uri", "feedQuery");
            }

            Tracing.TraceInfo("Service:Query - about to query");

            Stream responseStream = null;

            if (feedQuery.Etag != null)
            {
                responseStream = Query(targetUri, feedQuery.Etag);
            }
            else
            {
                responseStream = Query(targetUri, feedQuery.ModifiedSince);
            }

            Tracing.TraceInfo("Service:Query - query done");
            if (responseStream != null)
            {
                feed = CreateAndParseFeed(responseStream, feedQuery.Uri);
            }
            Tracing.TraceCall("Exit");
            return feed;
 
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>executes the query and returns an AtomFeed object tree</summary> 
        /// <param name="feedQuery">the query parameters as a FeedQuery object </param>
        /// <param name="ifModifiedSince">used to set a precondition date that 
        /// indicates the feed should be returned only if it has been modified 
        /// after the specified date. A value of null indicates no 
        /// precondition.</param>
        /// <returns>AtomFeed object tree</returns>
        //////////////////////////////////////////////////////////////////////
        [Obsolete("FeedQuery has a modifiedSince property, use that instead")] 
        public AtomFeed Query(FeedQuery feedQuery, DateTime ifModifiedSince)
        {
            feedQuery.ModifiedSince = ifModifiedSince;
            return Query(feedQuery);
        }
        /////////////////////////////////////////////////////////////////////////////





        //////////////////////////////////////////////////////////////////////
        /// <summary>object QueryOpenSearchRssDescription()</summary> 
        /// <param name="serviceUri">the service to ask for an OpenSearchRss Description</param> 
        /// <returns> a webresponse object</returns>
        //////////////////////////////////////////////////////////////////////
        public Stream QueryOpenSearchRssDescription(Uri serviceUri)
        {
            if (serviceUri == null)
            {
                throw new System.ArgumentNullException("serviceUri");
            }
            IGDataRequest request = this.RequestFactory.CreateRequest(GDataRequestType.Query, serviceUri);
            request.Credentials = this.Credentials;
            request.Execute();
            // return the response
            return request.GetResponseStream();
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>WebResponse Update(Uri updateUri, Stream entryStream, ICredentials credentials)</summary> 
        /// <param name="entry">the old entry to update</param> 
        /// <returns> the new Entry, as returned from the server</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomEntry Update(AtomEntry entry)
        {
            return this.Update(entry, null); 
        }
        /////////////////////////////////////////////////////////////////////////////




        //////////////////////////////////////////////////////////////////////
        /// <summary>templated type safe verion of the interface</summary> 
        /// <param name="entry">the old entry to update</param> 
        /// <returns> the new Entry, as returned from the server</returns>
        //////////////////////////////////////////////////////////////////////
        public TEntry  Update<TEntry>(TEntry entry) where TEntry : AtomEntry
        {
            return this.Update(entry, null) as TEntry;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>WebResponse Update(Uri updateUri, Stream entryStream, ICredentials credentials)</summary> 
        /// <param name="entry">the old entry to update</param> 
        /// <param name="data">the async data block used</param> 
        /// <returns> the new Entry, as returned from the server</returns>
        //////////////////////////////////////////////////////////////////////
        private AtomEntry Update(AtomEntry entry, AsyncSendData data)
        {
            Tracing.Assert(entry != null, "entry should not be null");
            if (entry == null)
            {
                throw new ArgumentNullException("entry"); 
            }

            if (entry.ReadOnly)
            {
                throw new GDataRequestException("Can not update a read-only entry"); 
            }


            Uri target = new Uri(entry.EditUri.ToString());

            Stream returnStream = EntrySend(target, entry, GDataRequestType.Update, data);
            return CreateAndParseEntry(returnStream, target);
        }
        /////////////////////////////////////////////////////////////////////////////

   

        //////////////////////////////////////////////////////////////////////
        /// <summary>public WebResponse Insert(Uri insertUri, Stream entryStream, ICredentials credentials)</summary> 
        /// <param name="feed">the feed this entry should be inserted into</param> 
        /// <param name="entry">the entry to be inserted</param> 
        /// <returns> the inserted entry</returns>
        //////////////////////////////////////////////////////////////////////
        AtomEntry IService.Insert(AtomFeed feed, AtomEntry entry)
        {

            Tracing.Assert(feed != null, "feed should not be null");
            if (feed == null)
            {
                throw new ArgumentNullException("feed"); 
            }
            Tracing.Assert(entry != null, "entry should not be null");
            if (entry == null)
            {
                throw new ArgumentNullException("entry"); 
            }

            if (feed.ReadOnly)
            {
                throw new GDataRequestException("Can not update a read-only feed"); 
            }

            Tracing.TraceMsg("Post URI is: " + feed.Post); 
            Uri target = new Uri(feed.Post); 
            return Insert(target, entry);
        }
        /////////////////////////////////////////////////////////////////////////////
        


     
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// templated type safe version of Insert
        /// </summary>
        /// <typeparam name="TEntry"></typeparam>
        /// <param name="feed"></param>
        /// <param name="entry"></param>
        /// <returns> the new Entry, as returned from the server</returns>
        public TEntry Insert<TEntry>(AtomFeed feed, TEntry entry) where TEntry : AtomEntry
        {
            IService s = this as IService; 
            return s.Insert(feed, entry) as TEntry;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>templated type safe verion of the interface</summary> 
        /// <typeparam name="TEntry"></typeparam>
        /// <param name="feedUri"></param>
        /// <param name="entry">the old entry to update</param> 
        /// <returns> the new Entry, as returned from the server</returns> 
        public TEntry Insert<TEntry>(Uri feedUri, TEntry entry) where TEntry : AtomEntry
        {
            return this.Insert(feedUri, entry, null) as TEntry;
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// internal Insert version to avoid recursion in the template versions
        /// </summary>
        /// <param name="feedUri"></param>
        /// <param name="newEntry"></param>
        /// <returns></returns>
        protected AtomEntry internalInsert(Uri feedUri, AtomEntry newEntry)
        {
            return this.Insert(feedUri, newEntry, null);
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>public WebResponse Insert(Uri insertUri, Stream entryStream, ICredentials credentials)</summary> 
        /// <param name="feedUri">the uri for the feed this entry should be inserted into</param> 
        /// <param name="newEntry">the entry to be inserted</param> 
        /// <param name="data">the data used for an async request</param>
        /// <returns> the inserted entry</returns>
        //////////////////////////////////////////////////////////////////////
        private AtomEntry Insert(Uri feedUri, AtomEntry newEntry, AsyncSendData data)
        {
            Tracing.Assert(feedUri != null, "feedUri should not be null");
            if (feedUri == null)
            {
                throw new ArgumentNullException("feedUri"); 
            }
            Tracing.Assert(newEntry != null, "newEntry should not be null");
            if (newEntry == null)
            {
                throw new ArgumentNullException("newEntry"); 
            }
            this.versionInfo.ImprintVersion(newEntry);

            Stream returnStream = EntrySend(feedUri, newEntry, GDataRequestType.Insert, data);
            return CreateAndParseEntry(returnStream, feedUri);
        }
        /////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// simple update for media resources
        /// </summary>
        /// <param name="uriTarget"></param>
        /// <param name="input">the stream to send</param>
        /// <param name="contentType"></param>
        /// <param name="slugHeader">the value for the slug header, indicating filenaming</param>
        /// <returns>AtomEntry</returns>
        public AtomEntry Update(Uri uriTarget, Stream input, string contentType, string slugHeader)
        {
            Stream returnStream = StreamSend(uriTarget, input, GDataRequestType.Update, contentType, slugHeader);
            return CreateAndParseEntry(returnStream, uriTarget);
        }   

        /// <summary>
        /// Simple insert for media resources
        /// </summary>
        /// <param name="uriTarget"></param>
        /// <param name="input"></param>
        /// <param name="contentType"></param>
        /// <param name="slugHeader">the value for the slug header, indicating filenaming</param>
        /// <returns>AtomEntry</returns>
        public AtomEntry Insert(Uri uriTarget, Stream input, string contentType, string slugHeader)
        {
            Stream returnStream = StreamSend(uriTarget, input, GDataRequestType.Insert, contentType, slugHeader);
            return CreateAndParseEntry(returnStream, uriTarget);
        }


        /// <summary>
        /// used to create a feed based on a stream
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="uriToUse"></param>
        /// <returns></returns>
        internal AtomFeed CreateAndParseFeed(Stream inputStream, Uri uriToUse)
        {
            AtomFeed returnFeed = null;

            if (inputStream != null)
            {
                returnFeed = CreateFeed(uriToUse);
                this.versionInfo.ImprintVersion(returnFeed);
                try
                {
                    returnFeed.NewAtomEntry += new FeedParserEventHandler(this.OnParsedNewEntry); 
                    returnFeed.NewExtensionElement += new ExtensionElementEventHandler(this.OnNewExtensionElement);
                    returnFeed.Parse(inputStream, AlternativeFormat.Atom);
                }
                finally
                {
                    inputStream.Close(); 
                }
            }

            return returnFeed; 
        }

        internal AtomEntry CreateAndParseEntry(Stream inputStream, Uri uriTarget)
        {
            AtomFeed returnFeed = CreateAndParseFeed(inputStream, uriTarget);
            AtomEntry entry=null; 
            // there should be ONE entry echoed back. 
            if (returnFeed != null && returnFeed.Entries.Count > 0)
            {
                entry = returnFeed.Entries[0];
                if (entry != null)
                {
                    entry.Service = this;
                    entry.setFeed(null);
                }
            }
            return entry; 
        }
   


        //////////////////////////////////////////////////////////////////////
        /// <summary>Inserts an AtomBase entry against a Uri</summary> 
        /// <param name="feedUri">the uri for the feed this object should be posted against</param> 
        /// <param name="baseEntry">the entry to be inserted</param> 
        /// <param name="type">the type of request to create</param> 
        /// <returns> the response as a stream</returns>
        //////////////////////////////////////////////////////////////////////
        public Stream EntrySend(Uri feedUri, AtomEntry baseEntry, GDataRequestType type)
        {
            return this.EntrySend(feedUri, baseEntry, type, null); 
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Inserts an AtomBase entry against a Uri</summary> 
        /// <param name="feedUri">the uri for the feed this object should be posted against</param> 
        /// <param name="baseEntry">the entry to be inserted</param> 
        /// <param name="type">the type of request to create</param> 
        /// <param name="data">the async data payload</param>
        /// <returns> the response as a stream</returns>
        //////////////////////////////////////////////////////////////////////
        internal virtual Stream EntrySend(Uri feedUri, AtomBase baseEntry, GDataRequestType type, AsyncSendData data)
        {
            Tracing.Assert(feedUri != null, "feedUri should not be null");
            if (feedUri == null)
            {
                throw new ArgumentNullException("feedUri"); 
            }
            Tracing.Assert(baseEntry != null, "baseEntry should not be null");
            if (baseEntry == null)
            {
                throw new ArgumentNullException("baseEntry"); 
            }
            this.versionInfo.ImprintVersion(baseEntry);

            IGDataRequest request = this.RequestFactory.CreateRequest(type,feedUri);
            request.Credentials = this.Credentials;

            ISupportsEtag eTarget = request as ISupportsEtag;
            ISupportsEtag eSource = baseEntry as ISupportsEtag;
            if (eTarget != null && eSource != null)
            {
                eTarget.Etag = eSource.Etag;
            }
            if (data != null)
            {
                GDataGAuthRequest gr = request as GDataGAuthRequest;
                if (gr != null)
                {
                    gr.AsyncData = data;
                }
            }

            Stream outputStream = request.GetRequestStream();

            baseEntry.SaveToXml(outputStream);
            request.Execute();

            outputStream.Close();
            return request.GetResponseStream();
        }



        /// <summary>
        /// this is a helper function for external utilities. It is not worth
        /// running the other insert/saves through here, as this would involve
        /// double buffering/copying of the bytes
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="payload"></param>
        /// <param name="type"></param>
        /// <returns>Stream</returns>
        
        public Stream StringSend(Uri targetUri, String payload, GDataRequestType type)
        {
            Tracing.Assert(targetUri != null, "targetUri should not be null");
            if (targetUri == null)
            {
                throw new ArgumentNullException("targetUri"); 
            }
            Tracing.Assert(payload != null, "payload should not be null");
            if (payload == null)
            {
                throw new ArgumentNullException("payload"); 
            }

            IGDataRequest request = this.RequestFactory.CreateRequest(type,targetUri);
            request.Credentials = this.Credentials;

            Stream outputStream = request.GetRequestStream();

            StreamWriter w = new StreamWriter(outputStream);
            w.Write(payload);
            w.Flush();
       
            request.Execute();

            w.Close();
            return request.GetResponseStream();
        }



        /// <summary>
        /// this is a helper function for to send binary data to a resource
        /// it is not worth running the other insert/saves through here, as this would involve
        /// double buffering/copying of the bytes
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <returns>Stream</returns>
        public Stream StreamSend(Uri targetUri, 
                                 Stream inputStream, 
                                 GDataRequestType type, 
                                 string contentType,
                                 string slugHeader)
        {

            return StreamSend(targetUri, inputStream, type, contentType, slugHeader, null, null);
        }

        /// <summary>
        /// this is a helper function for to send binary data to a resource
        /// it is not worth running the other insert/saves through here, as this would involve
        /// double buffering/copying of the bytes
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="etag">The http etag to pass into the request</param>
        /// <returns>Stream</returns>
        public Stream StreamSend(Uri targetUri, 
                                 Stream inputStream, 
                                 GDataRequestType type, 
                                 string contentType,
                                 string slugHeader,
                                 string etag)
        {

            return StreamSend(targetUri, inputStream, type, contentType, slugHeader, etag, null);
        }


        /// <summary>
        /// this is a helper function for to send binary data to a resource
        /// it is not worth running the other insert/saves through here, as this would involve
        /// double buffering/copying of the bytes
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="etag">The http etag to pass into the request</param>
        /// <param name="data">The async data needed for notifications</param>
        /// <returns>Stream from the server response. You should close this stream explicitly.</returns>
        private Stream StreamSend(Uri targetUri, 
                                 Stream inputStream, 
                                 GDataRequestType type, 
                                 string contentType,
                                 string slugHeader,
                                 string etag,
                                 AsyncSendData data)
        {
            Tracing.Assert(targetUri != null, "targetUri should not be null");
            if (targetUri == null)
            {
                throw new ArgumentNullException("targetUri"); 
            }
            if (inputStream == null)
            {
                Tracing.Assert(inputStream != null, "payload should not be null");
                throw new ArgumentNullException("inputStream"); 
            }
            if (type != GDataRequestType.Insert && type != GDataRequestType.Update)
            {
                Tracing.Assert(type != GDataRequestType.Insert && type != GDataRequestType.Update,"type needs to be insert or update");
                throw new ArgumentNullException("type"); 
            }


            IGDataRequest request = this.RequestFactory.CreateRequest(type,targetUri);
            request.Credentials = this.Credentials;

            if (data != null)
            {
                GDataGAuthRequest gr = request as GDataGAuthRequest;
                if (gr != null)
                {
                    gr.AsyncData = data;
                }
            }

            // set the contenttype of the request
            if (contentType != null)
            {
                GDataRequest r = request as GDataRequest;
                if (r != null)
                {
                    r.ContentType = contentType;
                }
            }

            if (slugHeader != null)
            {
                GDataRequest r = request as GDataRequest;
                if (r != null)
                {
                    r.Slug = slugHeader;
                }
            }

            if (etag != null)
            {
                ISupportsEtag ise = request as ISupportsEtag;
                if (ise != null)
                {
                    ise.Etag = etag;
                }
            }

            Stream outputStream = request.GetRequestStream();

            WriteInputStreamToRequest(inputStream, outputStream);

            request.Execute();
            outputStream.Close();
            return new GDataReturnStream(request);
        }

        /// <summary>
        /// write the current stream to an output stream
        /// this is primarily used to write data to the 
        /// request stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        protected void WriteInputStreamToRequest(Stream input, Stream output)
        {
            BinaryWriter w = new BinaryWriter(output);
            const int size = 4096;
            byte[] bytes = new byte[4096];
            int numBytes;

            while((numBytes = input.Read(bytes, 0, size)) > 0)
            {
                w.Write(bytes, 0, numBytes);
            }
            w.Flush();
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>creates a new feed instance to be returned by
        /// Batch(), Query() and other operations
        ///
        /// Subclasses can supply their own feed implementation by
        /// overriding this method.
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        protected virtual AtomFeed CreateFeed(Uri uriToUse)
        {
            ServiceEventArgs args = null;
            AtomFeed feed = null;

            if (this.NewFeed != null)
            {
                args = new ServiceEventArgs(uriToUse, this);
                this.NewFeed(this, args);
            }

            if (args != null)
            {
                feed = args.Feed;
            }

            if (feed == null)
            {
                feed = new AtomFeed(uriToUse, this);
            }

            return feed;
        }


        /// <summary>
        /// takes a given feed, and does a batch post of that feed
        /// against the batchUri parameter. If that one is NULL 
        /// it will try to use the batch link URI in the feed
        /// </summary>
        /// <param name="feed">the feed to post</param>
        /// <param name="batchUri">the URI to user</param>
        /// <returns>the returned AtomFeed</returns>
        public AtomFeed Batch(AtomFeed feed, Uri batchUri) 
        {
            return Batch(feed, batchUri, null);
        }
        //////////////////////////////////////////////////////////////////////



        /// <summary>
        /// takes a given feed, and does a batch post of that feed
        /// against the batchUri parameter. If that one is NULL 
        /// it will try to use the batch link URI in the feed
        /// </summary>
        /// <param name="feed">the feed to post</param>
        /// <param name="batchUri">the URI to user</param>
        /// <param name="data">The async data payload</param>
        /// <returns>the returned AtomFeed</returns>
        private AtomFeed Batch(AtomFeed feed, Uri batchUri, AsyncSendData data) 
        {
            Uri uriToUse = batchUri;
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }

            if (uriToUse == null)
            {
                uriToUse = feed.Batch == null ? null : new Uri(feed.Batch); 
            }

            if (uriToUse == null)
            {
                throw new ArgumentNullException("batchUri"); 
            }

            Tracing.Assert(feed != null, "feed should not be null");
            if (feed == null)
            {
                throw new ArgumentNullException("feed"); 
            }

            if (feed.BatchData == null) 
            {
                // setting this will make the feed output the namespace, instead of each entry
                feed.BatchData = new GDataBatchFeedData(); 
            }  
            Stream returnStream = EntrySend(uriToUse, feed, GDataRequestType.Batch, data);
            return CreateAndParseFeed(returnStream, uriToUse);;  
        }
        //////////////////////////////////////////////////////////////////////



    
        //////////////////////////////////////////////////////////////////////
        /// <summary>deletes an Atom entry object</summary> 
        /// <param name="entry"> </param>
        //////////////////////////////////////////////////////////////////////
        public void Delete(AtomEntry entry)
        {
            Delete(entry, false);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>deletes an Atom entry object</summary> 
        /// <param name="entry">The entry to be deleted </param>
        /// <param name="permanentDelete">Should the entry be archived or not</param>
        //////////////////////////////////////////////////////////////////////
        public void Delete(AtomEntry entry, bool permanentDelete)
        {
            Tracing.Assert(entry != null, "entry should not be null");
            string eTag = null;

            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.ReadOnly)
            {
                throw new GDataRequestException("Can not update a read-only entry");
            }

            Tracing.Assert(entry.EditUri != null, "Entry should have a valid edit URI");

            ISupportsEtag eSource = entry as ISupportsEtag;

            if (eSource != null)
            {
                eTag = eSource.Etag;
            }

            if (entry.EditUri != null)
            {
                var uriString = entry.EditUri.Content + (permanentDelete ? "?delete=true" : string.Empty);
                Delete(new Uri(uriString), eTag);
            }
            else
            {
                throw new GDataRequestException("Invalid Entry object (no edit uri) to call Delete on");
            }
        }

        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        ///<summary>Deletes an Atom entry when given a Uri</summary>
        ///<param name="uriTarget">The target Uri to call http delete against</param>
        /////////////////////////////////////////////////////////////////////
        public void Delete(Uri uriTarget)
        {
            Delete(uriTarget, null);
        }   
        //////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        ///<summary>Deletes an Atom entry when given a Uri</summary>
        ///<param name="uriTarget">The target Uri to call http delete against</param>
        ///<param name="eTag">The eTag of the item to delete. This parameter is used for strong
        /// concurrency support in protocol version 2 and up</param>
        /////////////////////////////////////////////////////////////////////
        public void Delete(Uri uriTarget, string eTag)
        {
            Tracing.Assert(uriTarget != null, "uri should not be null");
            if (uriTarget == null)
            {
                throw new ArgumentNullException("uriTarget");
            }

            Tracing.TraceMsg("Deleting entry: " + uriTarget.ToString());
            IGDataRequest request = RequestFactory.CreateRequest(GDataRequestType.Delete, uriTarget);

            ISupportsEtag eTarget = request as ISupportsEtag;
            if (eTarget != null && eTag != null)
            {
                eTarget.Etag = eTag;
            }

            request.Credentials = Credentials;
            request.Execute();
            IDisposable disp = request as IDisposable;
            disp.Dispose();
        }

        //////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>eventchaining. We catch this by the baseFeedParsers, which 
        /// would not do anything with the gathered data. We pass the event up
        /// to the user</summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feedentry</param> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        protected void OnParsedNewEntry(object sender, FeedParserEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e"); 
            }
            if (this.NewAtomEntry != null)
            {
                // just forward it upstream, if hooked
                Tracing.TraceMsg("\t calling event dispatcher"); 
                this.NewAtomEntry(this, e);
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>eventchaining. We catch this by the baseFeedParsers, which 
        /// would not do anything with the gathered data. We pass the event up
        /// to the user, and if he did not dicscard it, we add the entry to our
        /// collection</summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feedentry</param> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        protected void OnNewExtensionElement(object sender, ExtensionElementEventArgs e)
        {
            // by default, if our event chain is not hooked, the underlying parser will add it
            Tracing.TraceCall("received new extension element notification");
            Tracing.Assert(e != null, "e should not be null");
            if (e == null)
            {
                throw new ArgumentNullException("e"); 
            }
            if (this.NewExtensionElement != null)
            {
                Tracing.TraceMsg("\t calling event dispatcher"); 
                this.NewExtensionElement(this, e);
            }
        }
        /////////////////////////////////////////////////////////////////////////////


    }
    /////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// used to cover a return stream and add some additional data to it. 
    /// </summary>
    public class GDataReturnStream : Stream, ISupportsEtag
    {
        private string etag;
        private Stream innerStream;

        /// <summary>
        /// default constructor based on a gdatarequest object
        /// </summary>
        /// <param name="r"></param>
        public GDataReturnStream(IGDataRequest r)
        {
            this.innerStream = r.GetResponseStream();
            ISupportsEtag ise = r as ISupportsEtag;
            if (ise != null)
            {
                this.etag = ise.Etag;
            }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override bool CanRead
        {
            get { return this.innerStream.CanRead; }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override bool CanSeek
        {
            get { return this.innerStream.CanSeek; }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override bool CanTimeout
        {
            get { return this.innerStream.CanTimeout;}
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override void Close()
        {
            this.innerStream.Close();
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override bool CanWrite
        {
            get { return this.innerStream.CanWrite; }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override long Length
        {
            get { return this.innerStream.Length; }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override long Position
        {
            get
            {
                return this.innerStream.Position;
            }
            set
            {
                this.innerStream.Position = value;
            }
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        public override void Flush()
        {
            this.innerStream.Flush();
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.innerStream.Seek(offset, origin);
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            this.innerStream.SetLength(value);
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"/>
        /// <param name="offset"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count);           
        }

        /// <summary>
        /// default override, delegates to the real stream
        /// </summary>
        /// <param name="buffer"/>
        /// <param name="count"/>
        /// <param name="offset"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// implements the etag interface
        /// </summary>
        public string Etag
        {
            get { return this.etag; }
            set { this.etag = value; }
        }
    }
} 
/////////////////////////////////////////////////////////////////////////////
