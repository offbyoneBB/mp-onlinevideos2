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
using System.IO.Compression;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Threading;
using System.Collections.Specialized;

#endregion


/////////////////////////////////////////////////////////////////////
// <summary>contains support classes to handle async up/download and 
// delegation. Used for the resumable upload and the service object
//  </summary>
////////////////////////////////////////////////////////////////////
namespace Google.GData.Client {
    /// <summary>Delegate declaration for the operation completed event in a service</summary> 
    public delegate void AsyncOperationCompletedEventHandler(object sender, AsyncOperationCompletedEventArgs e);

    /// <summary>Delegate declaration for the operation progress update event in a service</summary> 
    public delegate void AsyncOperationProgressEventHandler(object sender, AsyncOperationProgressEventArgs e);

    /// <summary>
    /// EventArgument class for async events, this one is used
    /// when the operation is completed.
    /// </summary>
    public class AsyncOperationCompletedEventArgs : AsyncCompletedEventArgs {
        private AtomFeed feedObject;
        private Stream stream;
        private AtomEntry entryObject;

        /// <summary>
        /// constructor. takes the async data blob
        /// </summary>
        /// <param name="data">async data to constructor</param>
        internal AsyncOperationCompletedEventArgs(AsyncData data)
            : base(data.Exception, false, data.UserData) {
            feedObject = data.Feed;
            stream = data.DataStream;

            IAsyncEntryData entryData = data as IAsyncEntryData;
            if (entryData != null) {
                entryObject = entryData.Entry;
            }
        }

        internal AsyncOperationCompletedEventArgs(AsyncData data, bool cancelled)
            : base(data.Exception, cancelled, data.UserData) {
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the feed that was created. If NULL, a stream or entry was returned</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomFeed Feed {
            get { return this.feedObject; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the entry that was created. If NULL, a stream or feed was returned</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomEntry Entry {
            get { return this.entryObject; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the stream that was created. If NULL, a feed or entry was returned</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public Stream ResponseStream {
            get { return this.stream; }
        }
    }

    /// <summary>
    /// EventArgument class for async operation progress reports
    /// </summary>
    public class AsyncOperationProgressEventArgs : ProgressChangedEventArgs {
        private long completeSize;
        private long currentPosition;
        private Uri uri;
        private string httpVerb;

        /// <summary>
        /// constructor. Takes the URI and the service this event applies to
        /// </summary>
        /// <param name="completeSize">the completesize of the request</param>
        /// <param name="currentPosition">the current position in the upload/download</param>
        /// <param name="percentage">progress percentage</param>
        /// <param name="userData">The userdata identifying the request</param>
        public AsyncOperationProgressEventArgs(long completeSize, long currentPosition, int percentage,
            Uri targetUri, string httpVerb, object userData)
            : base(percentage, userData) {
            this.completeSize = completeSize;
            this.currentPosition = currentPosition;
            this.uri = targetUri;
            this.httpVerb = httpVerb;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the complete upload size</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public long CompleteSize {
            get { return this.completeSize; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the current position in the upload process</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public long Position {
            get { return this.currentPosition; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>the Uri that was used</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public Uri Uri {
            get { return this.uri; }
        }

        /// <summary>
        /// returns the http verb that is executed
        /// </summary>
        public string HttpVerb {
            get { return this.httpVerb; }
        }
    }

    public class AsyncData {
        private Uri uriToUse;
        private object userData;
        private string httpVerb;
        private AsyncOperation op;
        private Exception e;
        private AtomFeed feed;
        private Stream stream;
        private SendOrPostCallback onProgressReportDelegate;
        private AsyncDataHandler handler;

        public AsyncData(Uri uri, AsyncOperation op, object userData, SendOrPostCallback callback, bool parseFeed) {
            this.uriToUse = uri;
            this.op = op;
            this.userData = userData;
            this.onProgressReportDelegate = callback;
            this.ParseFeed = parseFeed;
        }

        public AsyncData(Uri uri, AsyncOperation op, object userData, SendOrPostCallback callback)
            : this(uri, op, userData, callback, false) {
        }

        public AsyncData(Uri uri, object userData, SendOrPostCallback callback)
            : this(uri, null, userData, callback) {
        }

        /// <summary>
        /// the uri to use
        /// </summary>
        public Uri UriToUse {
            get {
                return this.uriToUse;
            }
            set {
                this.uriToUse = value;
            }
        }

        public string HttpVerb {
            get {
                return this.httpVerb;
            }
            set {
                this.httpVerb = value;
            }
        }

        public AsyncDataHandler DataHandler {
            get {
                return this.handler;
            }
            set {
                this.handler = value;
            }
        }

        public object UserData {
            get {
                return this.userData;
            }
        }

        public AtomFeed Feed {
            get {
                return this.feed;
            }
            set {
                this.feed = value;
            }
        }

        public AsyncOperation Operation {
            get {
                return this.op;
            }
            set {
                this.op = value;
            }
        }

        public SendOrPostCallback Delegate {
            get {
                return this.onProgressReportDelegate;
            }
        }

        public Exception Exception {
            get {
                return this.e;
            }
            set {
                this.e = value;
            }
        }

        public Stream DataStream {
            get {
                return this.stream;
            }
            set {
                this.stream = value;
            }
        }

        /// <summary>
        /// indicates if the async operation should try to 
        /// parse the server returned stream, or just return the stream
        /// </summary>
        /// <returns></returns>
        public bool ParseFeed { get; private set; }
    }

    /// <summary>
    /// internal class for the data to pass to the async worker thread
    /// </summary>
    public class AsyncQueryData : AsyncData {
        private DateTime ifModifiedDate;
        bool fParseFeed;

        public AsyncQueryData(Uri uri, DateTime timeStamp, bool doParse,
            AsyncOperation op, object userData, SendOrPostCallback callback)
            : base(uri, op, userData, callback, doParse) {
            this.ifModifiedDate = timeStamp;
            this.fParseFeed = doParse;
        }

        /// <summary>
        ///  the date for the ifModified timestamp
        /// </summary>
        public DateTime Modified {
            get {
                return this.ifModifiedDate;
            }
            set {
                this.ifModifiedDate = value;
            }
        }
    }

    public abstract class AsyncDataHandler {
        /// <summary>eventhandler, fired when an async operation is completed</summary> 
        public event AsyncOperationCompletedEventHandler AsyncOperationCompleted;

        /// <summary>eventhandler, fired when an async operation reports progress</summary> 
        public event AsyncOperationProgressEventHandler AsyncOperationProgress;

        protected delegate void WorkerQueryEventHandler(AsyncQueryData data, AsyncOperation asyncOp,
            SendOrPostCallback completionMethodDelegate);

        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;
        private SendOrPostCallback completionMethodDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();

        public AsyncDataHandler() {
            this.onProgressReportDelegate = new SendOrPostCallback(OnAsyncReportProgress);
            this.onCompletedDelegate = new SendOrPostCallback(OnAsyncCompleted);
            this.completionMethodDelegate = new SendOrPostCallback(OnAsyncCompletionMethod);
        }

        /// <summary>
        /// this method cancels the corresponding async operation. 
        /// It sends still a completed event, but that event will then
        /// have the cancel property set to true
        /// </summary>
        /// <param name="userData">your identifier for the operation to be cancelled</param>
        public void CancelAsync(object userData) {
            lock (this.userStateToLifetime.SyncRoot) {
                object obj = this.userStateToLifetime[userData];
                if (obj != null) {
                    this.userStateToLifetime.Remove(userData);

                    AsyncOperation asyncOp = obj as AsyncOperation;
                    // The asyncOp object is responsible for 
                    // marshaling the call to the proper 
                    // thread or context.

                    AsyncData data = new AsyncData(null, userData, this.onProgressReportDelegate);
                    AsyncOperationCompletedEventArgs args = new AsyncOperationCompletedEventArgs(data, true);

                    asyncOp.PostOperationCompleted(this.onCompletedDelegate, args);
                }
            }
        }

        protected SendOrPostCallback ProgressReportDelegate {
            get {
                return this.onProgressReportDelegate;
            }
        }

        protected SendOrPostCallback CompletionMethodDelegate {
            get {
                return this.completionMethodDelegate;
            }
        }

        protected SendOrPostCallback OnCompletedDelegate {
            get {
                return this.onCompletedDelegate;
            }
        }

        protected void AddUserDataToDictionary(Object userData, AsyncOperation asyncOp) {
            // Multiple threads will access the task dictionary,
            // so it must be locked to serialize access.
            lock (this.userStateToLifetime.SyncRoot) {
                if (this.userStateToLifetime.Contains(userData)) {
                    throw new ArgumentException(
                        "UserData parameter must be unique",
                        "userData");
                }

                this.userStateToLifetime[userData] = asyncOp;
            }
        }

        protected bool CheckIfOperationIsCancelled(Object userData) {
            lock (userStateToLifetime.SyncRoot) {
                if (!userStateToLifetime.Contains(userData)) {
                    return true;
                }
            }
            return false;
        }

        // This method is invoked via the AsyncOperation object,
        // so it is guaranteed to be executed on the correct thread.
        private void OnAsyncReportProgress(object state) {
            AsyncOperationProgressEventArgs e = state as AsyncOperationProgressEventArgs;

            if (this.AsyncOperationProgress != null) {
                this.AsyncOperationProgress(this, e);
            }
        }

        private void OnAsyncCompleted(Object obj) {
            if (this.AsyncOperationCompleted != null) {
                AsyncOperationCompletedEventArgs args = obj as AsyncOperationCompletedEventArgs;
                this.AsyncOperationCompleted(this, args);
            }
        }

        // This is the method that the underlying, free-threaded 
        // asynchronous behavior will invoke.  This will happen on
        // an arbitrary thread.
        private void OnAsyncCompletionMethod(object operationState) {
            AsyncData data = operationState as AsyncData;
            AsyncOperation asyncOp = data.Operation;
            AsyncOperationCompletedEventArgs args = new AsyncOperationCompletedEventArgs(data);

            // In this case, don't allow cancellation, as the method 
            // is about to raise the completed event.
            lock (this.userStateToLifetime.SyncRoot) {
                if (!userStateToLifetime.Contains(data.UserData)) {
                    asyncOp = null;
                } else {
                    this.userStateToLifetime.Remove(asyncOp.UserSuppliedState);
                }
            }

            // The asyncOp object is responsible for marshaling 
            // the call.
            if (asyncOp != null) {
                asyncOp.PostOperationCompleted(this.onCompletedDelegate, args);
            }

            // Note that after the call to OperationCompleted, 
            // asyncOp is no longer usable, and any attempt to use it
            // will cause an exception to be thrown.
        }

        /// <summary>
        /// handles the response stream
        /// copies it into the memory stream, or parses it into a feed.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="responseStream"></param>
        /// <param name="contentLength"></param>
        /// <returns></returns>
        protected virtual void HandleResponseStream(AsyncData data, Stream responseStream, long contentLength) {
            HandleResponseStream(data, responseStream, contentLength, null);
        }

        /// <summary>
        /// handles the response stream
        /// copies it into the memory stream, or parses it into a feed.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="responseStream"></param>
        /// <param name="contentLength"></param>
        /// <returns></returns>
        protected virtual void HandleResponseStream(AsyncData data, Stream responseStream, long contentLength, IService service) {
            data.DataStream = CopyResponseToMemory(data, responseStream, contentLength);
            
            IAsyncEntryData entryData = data as IAsyncEntryData;
            Service serviceImpl = service as Service;
            if (entryData != null && service != null) {
                entryData.Entry = serviceImpl.CreateAndParseEntry(data.DataStream, data.UriToUse);
            }
        }

        private MemoryStream CopyResponseToMemory(AsyncData data, Stream responseStream, long contentLength) {
            if (responseStream == null) {
                return null;
            }

            // read the stream into memory. That's the only way to satisfy the "main work
            // on the other thread requirement
            MemoryStream memStream = new MemoryStream();
            const int size = 4096;
            var bytes = new byte[size];

            int numBytes;
            double current = 0;
            long bytesWritten = 0;

            while ((numBytes = responseStream.Read(bytes, 0, size)) > 0) {
                memStream.Write(bytes, 0, numBytes);
                if (data == null || data.Delegate == null) {
                    continue;
                }
                bytesWritten += numBytes;

                if (contentLength > size) {
                    current = bytesWritten * 100d / contentLength;
                }

                // see if we are still in the list...
                // Multiple threads will access the task dictionary,
                // so it must be locked to serialize access.
                if (CheckIfOperationIsCancelled(data.UserData)) {
                    throw new ArgumentException("Operation was cancelled");
                }

                var args = new AsyncOperationProgressEventArgs(contentLength,
                    bytesWritten,
                    (int)current,
                    data.UriToUse,
                    data.HttpVerb,
                    data.UserData);
                data.Operation.Post(data.Delegate, args);
            }

            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        internal bool SendProgressData(AsyncData data, AsyncOperationProgressEventArgs args) {
            // In this case, don't allow cancellation, as the method 
            // is about to raise the completed event.
            bool ret = !CheckIfOperationIsCancelled(data.UserData);
            if (ret) {
                data.Operation.Post(data.Delegate, args);
            }
            return ret;
        }
    }

    public interface IAsyncEntryData {
        AtomEntry Entry {
            get;
            set;
        }
    }
}