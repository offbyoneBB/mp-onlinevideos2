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
namespace Google.GData.Client {
    public class AsyncSendData : AsyncData, IAsyncEntryData {
        private AtomEntry entry;
        private GDataRequestType type;
        private string contentType;
        private string slugHeader;

        private AsyncSendData(AsyncDataHandler handler, Uri uriToUse, AtomEntry entry, AtomFeed feed,
            SendOrPostCallback callback, object userData, bool parseFeed)
            : base(uriToUse, null, userData, callback, parseFeed) {
            this.DataHandler = handler;
            this.entry = entry;
            this.Feed = feed;
        }

        public AsyncSendData(AsyncDataHandler handler, Uri uriToUse, AtomEntry entry, SendOrPostCallback callback, object userData)
            : this(handler, uriToUse, entry, null, callback, userData, false) {
        }

        public AsyncSendData(AsyncDataHandler handler, AtomEntry entry, SendOrPostCallback callback, object userData)
            : this(handler, null, entry, null, callback, userData, false) {
        }

        public AsyncSendData(AsyncDataHandler handler, Uri uriToUse, AtomFeed feed, SendOrPostCallback callback, object userData)
            : this(handler, uriToUse, null, feed, callback, userData, false) {
        }

        public AsyncSendData(AsyncDataHandler handler, Uri uriToUse, Stream stream, GDataRequestType type,
            string contentType, string slugHeader, SendOrPostCallback callback, object userData, bool parseFeed)
            : this(handler, uriToUse, null, null, callback, userData, parseFeed) {
            this.DataStream = stream;
            this.type = type;
            this.contentType = contentType;
            this.slugHeader = slugHeader;
        }

        public AtomEntry Entry {
            get {
                return this.entry;
            }
            set {
                this.entry = value;
            }
        }

        public string ContentType {
            get {
                return this.contentType;
            }
        }

        public string SlugHeader {
            get {
                return this.slugHeader;
            }
        }

        public GDataRequestType Type {
            get {
                return this.type;
            }
        }
    }

    public class AsyncDeleteData : AsyncData, IAsyncEntryData {
        private readonly AtomEntry _entry;
        private readonly bool _permanentDelete;

        public AsyncDeleteData(AtomEntry entry, bool permanentDelete, object userData, SendOrPostCallback callback)
            : base(null, userData, callback) {
            _entry = entry;
            _permanentDelete = permanentDelete;
        }

        public bool PermanentDelete {
            get {
                return _permanentDelete;
            }
        }

        public AtomEntry Entry {
            get {
                return _entry;
            }
        }
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>async functionality of the Service implementation
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public partial class Service : AsyncDataHandler, IService, IVersionAware {

        private delegate void WorkerSendEventHandler(AsyncSendData data, AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate);

        private delegate void WorkerDeleteEventHandler(AsyncDeleteData data, AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate);

        /// <summary>
        /// the basic interface as an async version. This call will return directly
        /// and you need to rely on the events fired to figure out what happened.
        /// </summary>
        /// <param name="queryUri">the Uri to Query</param>
        /// <param name="ifModifiedSince">The ifmodifiedsince date, use DateTime.MinValue if you want everything</param>
        /// <param name="userData">The userData token. this must be unique if you make several async requests at once</param>
        /// <returns>nothing</returns>
        public void QueryFeedAync(Uri queryUri, DateTime ifModifiedSince, Object userData) {
            this.QueryAsync(queryUri, ifModifiedSince, true, userData);
        }

        /// <summary>
        /// the basic interface as an async version. This call will return directly
        /// and you need to rely on the events fired to figure out what happened.
        /// this version does not parse the response from the webserver but 
        /// provides it to you in the event
        /// </summary>
        /// <param name="queryUri">the Uri to Query</param>
        /// <param name="ifModifiedSince">The ifmodifiedsince date, use DateTime.MinValue if you want everything</param>
        /// <param name="userData">The userData token. this must be unique if you make several async requests at once</param>
        /// <returns>nothing</returns>
        public void QueryStreamAync(Uri queryUri, DateTime ifModifiedSince, Object userData) {
            this.QueryAsync(queryUri, ifModifiedSince, false, userData);
        }

        /// <summary>
        /// the basic interface as an async version. This call will return directly
        /// and you need to rely on the events fired to figure out what happened.
        /// </summary>
        /// <param name="queryUri">the Uri to Query</param>
        /// <param name="ifModifiedSince">The ifmodifiedsince date, use DateTime.MinValue if you want everything</param>
        /// <param name="doParse">if true, returns a feed, else a stream</param>
        /// <param name="userData">The userData token. this must be unique if you make several async requests at once</param>
        /// <returns>nothing</returns>
        private void QueryAsync(Uri queryUri, DateTime ifModifiedSince, bool doParse, Object userData) {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userData);
            AsyncQueryData data = new AsyncQueryData(queryUri, ifModifiedSince, doParse, asyncOp, userData, this.ProgressReportDelegate);

            AddUserDataToDictionary(userData, asyncOp);

            // Start the asynchronous operation.
            WorkerQueryEventHandler workerDelegate = new WorkerQueryEventHandler(AsyncQueryWorker);
            workerDelegate.BeginInvoke(
                data,
                asyncOp,
                this.CompletionMethodDelegate,
                null,
                null);
        }

        /// <summary>
        ///  worker method for the query case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncQueryWorker(AsyncQueryData data, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate) {
            try {
                long contentLength;
                using (var responseStream = Query(data.UriToUse, data.Modified, null, out contentLength)) {
                    HandleResponseStream(data, responseStream, contentLength);
                }
            } catch (Exception e) {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        /// updates the entry asynchronous, you need to supply a valid and unique
        /// token. Events will be send to the async delegates you setup on the service
        /// object
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <returns></returns>
        public void UpdateAsync(AtomEntry entry, Object userData) {
            AsyncSendData data = new AsyncSendData(this, entry, this.ProgressReportDelegate, userData);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncUpdateWorker);
            this.AsyncStarter(data, workerDelegate, userData);
        }

        /// <summary>
        /// inserts the entry asynchronous, you need to supply a valid and unique
        /// token. Events will be send to the async delegates you setup on the service
        /// object
        /// </summary>
        /// <param name="feedUri">the target feed the entry get's inserted into</param>
        /// <param name="entry"></param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <returns></returns>
        public void InsertAsync(Uri feedUri, AtomEntry entry, Object userData) {
            AsyncSendData data = new AsyncSendData(this, feedUri, entry, this.ProgressReportDelegate, userData);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncInsertWorker);
            this.AsyncStarter(data, workerDelegate, userData);
        }

        /// <summary>
        /// takes a given feed, and does a batch post of that feed
        /// against the batchUri parameter. If that one is NULL 
        /// it will try to use the batch link URI in the feed
        /// </summary>
        /// <param name="feed">the feed to post</param>
        /// <param name="batchUri">the URI to user</param>
        /// <param name="userData">the userdata identifying this request</param>
        /// <returns></returns>
        public void BatchAsync(AtomFeed feed, Uri batchUri, Object userData) {
            AsyncSendData data = new AsyncSendData(this, batchUri, feed, this.ProgressReportDelegate, userData);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncBatchWorker);
            this.AsyncStarter(data, workerDelegate, userData);
        }

        /// <summary>
        /// this is a helper function for to send binary data asyncronous to a resource
        /// The async returned object will contain the output Feed
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <returns></returns>
        public void StreamSendFeedAsync(Uri targetUri,
            Stream inputStream,
            GDataRequestType type,
            string contentType,
            string slugHeader,
            object userData) {
            StreamSendAsync(targetUri, inputStream, type, contentType, slugHeader, userData, true);
        }

        /// <summary>
        /// this is a helper function for to send binary data asyncronous to a resource
        /// The async returned object will contain the output stream
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <returns></returns>
        public void StreamSendStreamAsync(Uri targetUri,
            Stream inputStream,
            GDataRequestType type,
            string contentType,
            string slugHeader,
            object userData) {
            StreamSendAsync(targetUri, inputStream, type, contentType, slugHeader, userData, false);
        }

        /// <summary>
        /// this is a helper function for to send binary data asyncronous to a resource
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="inputStream"></param>
        /// <param name="type"></param>
        /// <param name="contentType">the contenttype to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="slugHeader">the slugHeader to use in the request, if NULL is passed, factory default is used</param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <param name="parseFeed">indicates if the async operation should try to parse the server returned stream, or just return the stream</param>
        /// <returns></returns>
        private void StreamSendAsync(Uri targetUri,
            Stream inputStream,
            GDataRequestType type,
            string contentType,
            string slugHeader,
            object userData,
            bool parseFeed) {
            AsyncSendData data = new AsyncSendData(this, targetUri, inputStream, type, contentType, slugHeader,
                this.ProgressReportDelegate, userData, parseFeed);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncStreamSendWorker);
            this.AsyncStarter(data, workerDelegate, userData);
        }

        /// <summary>
        /// handles the response stream
        /// copies it into the memory stream, or parses it into a feed.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="responseStream"></param>
        /// <param name="contentLength"></param>
        /// <returns></returns>
        protected override void HandleResponseStream(AsyncData data, Stream responseStream, long contentLength) {
            if (data.ParseFeed) {
                data.Feed = CreateAndParseFeed(responseStream, data.UriToUse);
                data.DataStream = null;
            } else {
                base.HandleResponseStream(data, responseStream, contentLength);
            }
        }

        /// <summary>
        /// worker method for the update case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncUpdateWorker(AsyncSendData data,
            AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate) {
            try {
                data.Entry = this.Update(data.Entry, data);
            } catch (Exception e) {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        /// worker method for the Insert case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncInsertWorker(AsyncSendData data,
            AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate) {
            try {
                data.Entry = this.Insert(data.UriToUse, data.Entry, data);
            } catch (Exception e) {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        ///  worker method for the batch case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncBatchWorker(AsyncSendData data,
            AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate) {
            try {
                data.Feed = this.Batch(data.Feed, data.UriToUse, data);
            } catch (Exception e) {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        /// worker method for the direct stream send
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncStreamSendWorker(AsyncSendData data, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate) {
            try {
                using (var responseStream = StreamSend(data.UriToUse, data.DataStream, data.Type, data.ContentType, data.SlugHeader, null, data)) {
                    HandleResponseStream(data, responseStream, -1);
                }
            } catch (Exception e) {
                data.Exception = e;
            }

            completionMethodDelegate(data);
        }

        /// <summary>
        /// starts the async job
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        /// <param name="workerDelegate"></param>
        /// <returns></returns>
        private void AsyncStarter(AsyncSendData data, WorkerSendEventHandler workerDelegate, Object userData) {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userData);
            data.Operation = asyncOp;

            AddUserDataToDictionary(userData, asyncOp);

            // Start the asynchronous operation.
            workerDelegate.BeginInvoke(
                data,
                asyncOp,
                this.CompletionMethodDelegate,
                null,
                null);
        }

        public void DeleteAsync(AtomEntry entry, bool permanentDelete, Object userData) {
            AsyncDeleteData data = new AsyncDeleteData(entry, permanentDelete, userData, ProgressReportDelegate);
            AsyncStarter(data, AsyncDeleteWorker, userData);
        }

        private void AsyncStarter(AsyncDeleteData data, WorkerDeleteEventHandler workerDelegate, Object userData) {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userData);
            data.Operation = asyncOp;
            AddUserDataToDictionary(userData, asyncOp);

            // Start the asynchronous operation.
            workerDelegate.BeginInvoke(data, asyncOp, CompletionMethodDelegate, null, null);
        }

        private void AsyncDeleteWorker(AsyncDeleteData data, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate) {
            try {
                Delete(data.Entry, data.PermanentDelete);
            } catch (Exception e) {
                data.Exception = e;
            }

            completionMethodDelegate(data);
        }
    }
}
