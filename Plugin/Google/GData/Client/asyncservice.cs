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
namespace Google.GData.Client
{

#if WindowsCE || PocketPC
    internal class AsyncData : Object
    {
    }
    internal class AsyncSendData : AsyncData
    {
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>async functionallity of the Service implementation
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public partial class Service : IService, IVersionAware
    {
        private void InitDelegates()
        {
        }
    }

#else

   /// <summary>
   /// EventArgument class for async events, this one is used
   /// when the operation is completed.
   /// </summary>
   public class AsyncOperationCompletedEventArgs : AsyncCompletedEventArgs
   {
       private AtomFeed feedObject;
       private Stream   stream;
       private Uri uri;
       private AtomEntry entryObject;


       /// <summary>
       /// constructor. takes the async data blog
       /// </summary>
       /// <param name="data">async data to constructor</param>
       internal AsyncOperationCompletedEventArgs(AsyncData data) : base(data.Exception, false, data.UserData)
       {
           AsyncQueryData qData = data as AsyncQueryData;
           AsyncSendData sData = data as AsyncSendData;

           this.uri = data.UriToUse;

           if (qData != null)
           {
               this.feedObject = qData.Feed;
               this.stream = qData.DataStream;
           }
           if (sData != null)
           {
               this.entryObject = sData.Entry;
           }
       }

       internal AsyncOperationCompletedEventArgs(AsyncData data, bool cancelled)
           : base(data.Exception, cancelled, data.UserData)
       {
       }
       

       //////////////////////////////////////////////////////////////////////
       /// <summary>the feed that was created. If NULL, a stream or entry was returned</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public AtomFeed Feed
       {
           get { return this.feedObject; }
       }
       ////////////////////////////////////////////////////////////////////////
       
       //////////////////////////////////////////////////////////////////////
       /// <summary>the entry that was created. If NULL, a stream or feed was returned</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public AtomEntry Entry
       {
           get { return this.entryObject; }
       }
       ////////////////////////////////////////////////////////////////////////
 

       //////////////////////////////////////////////////////////////////////
       /// <summary>the stream that was created. If NULL, a feed or entry was returned</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public Stream ResponseStream
       {
           get { return this.stream; }
       }
       ////////////////////////////////////////////////////////////////////////
    
       
       //////////////////////////////////////////////////////////////////////
       /// <summary>the Uri to be used</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public Uri Uri
       {
           get { return this.uri; }
       }
       ////////////////////////////////////////////////////////////////////////
   }
   /// <summary>Delegate declaration for the feed creation in a service</summary> 
    public delegate void AsyncOperationCompletedEventHandler(object sender, AsyncOperationCompletedEventArgs e);


   /// <summary>
   /// EventArgument class for async operation progress reports
   /// </summary>
   public class AsyncOperationProgressEventArgs : ProgressChangedEventArgs
   {
       private long completeSize;
       private long currentPosition;

       /// <summary>
       /// constructor. Takes the URI and the service this event applies to
       /// </summary>
       /// <param name="completeSize">the completesize of the request</param>
       /// <param name="currentPosition">the current position in the upload/download</param>
       /// <param name="percentage">progress percentage</param>
       /// <param name="userData">The userdata identifying the request</param>
       public AsyncOperationProgressEventArgs(long completeSize, long currentPosition, int percentage, 
                                    object userData) : base(percentage, userData)
       {
           this.completeSize = completeSize;
           this.currentPosition = currentPosition;
       }

       //////////////////////////////////////////////////////////////////////
       /// <summary>the complete upload size</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public long CompleteSize
       {
           get { return this.completeSize; }
       }
       ////////////////////////////////////////////////////////////////////////

       //////////////////////////////////////////////////////////////////////
       /// <summary>the current position in the upload process</summary> 
       /// <returns> </returns>
       //////////////////////////////////////////////////////////////////////
       public long Position
       {
           get { return this.currentPosition; }
       }
       ////////////////////////////////////////////////////////////////////////

   }

    
    /// <summary>Delegate declaration for the feed creation in a service</summary> 
    public delegate void AsyncOperationProgressEventHandler(object sender, AsyncOperationProgressEventArgs e);


    internal class AsyncData
    {
        private Uri uriToUse;
        private object userData;
        private AsyncOperation op;
        private Exception e;
        private AtomFeed feed;
        private Stream stream;
        private SendOrPostCallback onProgressReportDelegate;
        private Service service; 

        public AsyncData(Uri uri, AsyncOperation op, object userData, SendOrPostCallback callback) 
        {
            this.uriToUse = uri;
            this.userData = userData;
            this.op = op;
            this.onProgressReportDelegate = callback;
        }

        public AsyncData(Uri uri, object userData, SendOrPostCallback callback) 
        {
            this.uriToUse = uri;
            this.userData = userData;
            this.onProgressReportDelegate = callback;
        }

        /// <summary>
        /// the uri to use
        /// </summary>
        public Uri UriToUse
        {
            get
            {
                return this.uriToUse;
            }
            set
            {
                this.uriToUse = value;
            }
        }

        public Service Service
        {
            get
            {
                return this.service;
            }
            set
            {
                this.service = value;
            }
        }




        public object UserData
        {
            get
            {
                return this.userData;
            }
        }


        public AtomFeed Feed
        {
            get
            {
                return this.feed;
            }
            set
            {
                this.feed = value;
            }
        }
        public AsyncOperation Operation
        {
            get
            {
                return this.op;
            }
            set 
            {
                this.op = value;
            }
        }

        public SendOrPostCallback Delegate
        {
            get 
            {
                return this.onProgressReportDelegate;
            }
        }

        public Exception Exception 
        {
            get
            {
                return this.e;
            }
            set
            {
                this.e= value;
            }
        }

        public Stream DataStream
        {
            get
            {
                return this.stream;
            }
            set
            {
                this.stream = value;
            }
        }
    }

    internal class AsyncSendData : AsyncData
    {
        private AtomEntry entry; 
        private GDataRequestType type;
        private string contentType;
        private string slugHeader;

        public AsyncSendData(Service service, AtomEntry entry, SendOrPostCallback callback, object userData)
            : base(null, userData, callback)
        {
            this.entry = entry;
            this.Service = service; 
        }

        public AsyncSendData(Service service, Uri uriToUse, AtomEntry entry, SendOrPostCallback callback, object userData)
            : base(uriToUse, userData, callback)
        {
            this.entry = entry;
            this.Service = service; 
        }

        public AsyncSendData(Service service, Uri uriToUse, AtomFeed feed, SendOrPostCallback callback, object userData)
            : base(uriToUse, userData, callback)
        {
            this.Feed = feed;
            this.Service = service; 
        }


        public AsyncSendData(Service service, Uri uriToUse, Stream stream, GDataRequestType type,
                             string contentType, string slugHeader, SendOrPostCallback callback, object userData)
            : base(uriToUse, userData, callback)
        {
            this.DataStream = stream;
            this.type = type;
            this.contentType = contentType;
            this.slugHeader = slugHeader;
            this.Service = service; 
        }

        public AtomEntry Entry
        {
            get
            {
                return this.entry;
            }
            set 
            {
                this.entry = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.contentType;
            }
        }

        public string SlugHeader
        {
            get
            {
                return this.slugHeader;
            }
        }

        public GDataRequestType Type
        {
            get
            {
                return this.type;
            }
        }
    }

    /// <summary>
    /// internal class for the data to pass to the async worker thread
    /// </summary>
    internal class AsyncQueryData : AsyncData
    {
        private DateTime ifModifiedDate;
        bool fParseFeed;


        public AsyncQueryData(Uri uri, DateTime timeStamp, bool doParse,
                               AsyncOperation op, object userData, SendOrPostCallback callback) 
                    : base(uri, op, userData, callback)
        {
            this.ifModifiedDate = timeStamp;
            this.fParseFeed = doParse;
        }

        
        /// <summary>
        ///  the date for the ifModified timestamp
        /// </summary>
        public DateTime Modified
        {
            get
            {
                return this.ifModifiedDate;
            }
            set
            {
                this.ifModifiedDate = value;
            }
        }

        /// <summary>
        /// indicates if the async operation should try to 
        /// parse the server returned stream, or just return the stream
        /// </summary>
        /// <returns></returns>
        public bool ParseFeed
        {
            get 
            {
                return this.fParseFeed;
            }
            set
            {
                this.fParseFeed = value;
            }
        }


    }
   
    //////////////////////////////////////////////////////////////////////
    /// <summary>async functionallity of the Service implementation
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public partial class Service : IService, IVersionAware
    {
        /// <summary>eventhandler, fired when an async operation is completed</summary> 
        public event AsyncOperationCompletedEventHandler AsyncOperationCompleted;

        /// <summary>eventhandler, fired when an async operation reports progress</summary> 
        public event AsyncOperationProgressEventHandler AsyncOperationProgress;

        private delegate void WorkerQueryEventHandler(AsyncQueryData data, AsyncOperation asyncOp,
                                    SendOrPostCallback completionMethodDelegate);

        private delegate void WorkerSendEventHandler(AsyncSendData data, AsyncOperation asyncOp,
                                    SendOrPostCallback completionMethodDelegate);



        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;
        private SendOrPostCallback completionMethodDelegate;
        

        private HybridDictionary userStateToLifetime = 
                                    new HybridDictionary();







        /// <summary>
        /// the basic interface as an async version. This call will return directly
        /// and you need to rely on the events fired to figure out what happened.
        /// </summary>
        /// <param name="queryUri">the Uri to Query</param>
        /// <param name="ifModifiedSince">The ifmodifiedsince date, use DateTime.MinValue if you want everything</param>
        /// <param name="userData">The userData token. this must be unique if you make several async requests at once</param>
        /// <returns>nothing</returns>
        public void QueryFeedAync(Uri queryUri, DateTime ifModifiedSince, Object userData)
        {
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
        public void QueryStreamAync(Uri queryUri, DateTime ifModifiedSince, Object userData)
        {
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
        private void QueryAsync(Uri queryUri, DateTime ifModifiedSince, bool doParse, Object userData)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userData);
            AsyncQueryData data = new AsyncQueryData(queryUri, ifModifiedSince, doParse, asyncOp, userData, this.onProgressReportDelegate);

            // Multiple threads will access the task dictionary,
            // so it must be locked to serialize access.
            lock (this.userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(userData))
                {
                    throw new ArgumentException(
                        "UserData parameter must be unique", 
                        "userData");
                }

                this.userStateToLifetime[userData] = asyncOp;
            }



            // Start the asynchronous operation.
            WorkerQueryEventHandler workerDelegate = new WorkerQueryEventHandler(AsyncQueryWorker);
            workerDelegate.BeginInvoke(
                data,
                asyncOp,
                completionMethodDelegate,
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
        private void AsyncQueryWorker(AsyncQueryData data, 
                                      AsyncOperation asyncOp,
                                      SendOrPostCallback completionMethodDelegate)

        {
            Stream responseStream = null;
            try
            {
                long contentLength;
                responseStream = this.Query(data.UriToUse, data.Modified, null, out contentLength);
                AtomFeed feed = null;
                MemoryStream memStream = null;

                if (responseStream != null)
                {
                    // read the stream into memory. That's the only way to satisfy the "main work
                    // on the other thread requirement
                    memStream = new MemoryStream();
                    const int size = 4096;
                    byte[] bytes = new byte[4096];

                    int numBytes;

                    double current = 0;
                    long bytesWritten = 0;

                    while ((numBytes = responseStream.Read(bytes, 0, size)) > 0)
                    {
                        memStream.Write(bytes, 0, numBytes);
                        bytesWritten += numBytes;
                        if (data != null && data.Delegate != null)
                        {
                            AsyncOperationProgressEventArgs args;
                            if (contentLength > size)
                            {
                                current = (double)bytesWritten * 100 / (double)contentLength;
                            }
                            // see if we are still in the list...
                            // Multiple threads will access the task dictionary,
                            // so it must be locked to serialize access.
                            lock (this.userStateToLifetime.SyncRoot)
                            {
                                if (userStateToLifetime.Contains(data.UserData) == false)
                                {
                                    throw new ArgumentException("Operation was cancelled");
                                }
                            }
                            args = new AsyncOperationProgressEventArgs(contentLength, bytesWritten, (int)current, data.UserData);
                            data.Operation.Post(data.Delegate, args);
                        }
                    }
                    memStream.Seek(0, SeekOrigin.Begin);
                }

                if (data.ParseFeed == true && responseStream != null)
                {
                    Tracing.TraceCall("Using Atom always.... ");
                    feed = CreateFeed(data.UriToUse);

                    feed.NewAtomEntry += new FeedParserEventHandler(this.OnParsedNewEntry);
                    feed.NewExtensionElement += new ExtensionElementEventHandler(this.OnNewExtensionElement);
                    feed.Parse(memStream, AlternativeFormat.Atom);
                    memStream.Close();
                    memStream = null;
                }

                data.Feed = feed;
                data.DataStream = memStream;
            }
            catch (Exception e)
            {
                data.Exception = e;
            }
            finally
            {
                if (responseStream != null)
                    responseStream.Close();
            }
            completionMethodDelegate(data);
        }

        // This method is invoked via the AsyncOperation object,
        // so it is guaranteed to be executed on the correct thread.
        private void AsyncReportProgress(object state)
        {
            AsyncOperationProgressEventArgs e =
                state as AsyncOperationProgressEventArgs;

            if (this.AsyncOperationProgress != null)
            {
                this.AsyncOperationProgress(this, e);
            }
        }





        // This is the method that the underlying, free-threaded 
        // asynchronous behavior will invoke.  This will happen on
        // an arbitrary thread.
        private void AsyncCompletionMethod(object operationState)
        {
            AsyncData data = operationState as AsyncData;

            AsyncOperation asyncOp = data.Operation;
    
            AsyncOperationCompletedEventArgs args =
                         new AsyncOperationCompletedEventArgs(data);

            // In this case, don't allow cancellation, as the method 
            // is about to raise the completed event.
            lock (this.userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(data.UserData) == false)
                {
                    asyncOp = null;
                }
                else
                {
                    this.userStateToLifetime.Remove(asyncOp.UserSuppliedState);
                }
            }
    
            // The asyncOp object is responsible for marshaling 
            // the call.
            if (asyncOp != null)
                asyncOp.PostOperationCompleted(onCompletedDelegate, args);
    
            // Note that after the call to OperationCompleted, 
            // asyncOp is no longer usable, and any attempt to use it
            // will cause an exception to be thrown.
        }


        private void OnAsyncCompleted(Object obj)
        {
            if (this.AsyncOperationCompleted != null)
            {
                AsyncOperationCompletedEventArgs args = obj as AsyncOperationCompletedEventArgs;
                this.AsyncOperationCompleted(this, args);
            }
        }



        /// <summary>
        /// updates the entry asynchronous, you need to supply a valid and unique
        /// token. Events will be send to the async delegates you setup on the service
        /// object
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="userData">a unique identifier to associate this request with</param>
        /// <returns></returns>
        public void UpdateAsync(AtomEntry entry, Object userData)
        {
            AsyncSendData data = new AsyncSendData(this, entry, this.onProgressReportDelegate, userData);
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
        public void InsertAsync(Uri feedUri, AtomEntry entry, Object userData)
        {
            AsyncSendData data = new AsyncSendData(this, feedUri, entry, this.onProgressReportDelegate, userData);
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
        public void  BatchAsync(AtomFeed feed, Uri batchUri, Object userData) 
        {
            AsyncSendData data = new AsyncSendData(this, batchUri, feed,this.onProgressReportDelegate, userData);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncBatchWorker);
            this.AsyncStarter(data, workerDelegate, userData);
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
        /// <returns></returns>
        public void StreamSendAsync(Uri targetUri, 
                                 Stream inputStream, 
                                 GDataRequestType type, 
                                 string contentType,
                                 string slugHeader,
                                 object userData)
        {
            AsyncSendData data = new AsyncSendData(this, targetUri, inputStream, type, contentType, slugHeader, 
                                                   this.onProgressReportDelegate, userData);
            WorkerSendEventHandler workerDelegate = new WorkerSendEventHandler(AsyncStreamSendWorker);
            this.AsyncStarter(data, workerDelegate, userData);
        }



        /// <summary>
        /// starts the async job for several send methods
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        /// <param name="workerDelegate"></param>
        /// <returns></returns>
        private void AsyncStarter(AsyncSendData data, WorkerSendEventHandler workerDelegate, Object userData)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(userData);

            data.Operation = asyncOp;

            // Multiple threads will access the task dictionary,
            // so it must be locked to serialize access.
            lock (this.userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(userData))
                {
                    throw new ArgumentException(
                        "UserData parameter must be unique", 
                        "userData");
                }

                this.userStateToLifetime[userData] = asyncOp;
            }
            // Start the asynchronous operation.
            workerDelegate.BeginInvoke(
                data,
                asyncOp,
                this.completionMethodDelegate,
                null,
                null);
        }



        /// <summary>
        ///  worker method for the update case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncUpdateWorker(AsyncSendData data, 
                                      AsyncOperation asyncOp,
                                      SendOrPostCallback completionMethodDelegate)

        {
            try
            {
                data.Entry = this.Update(data.Entry, data);
            } catch (Exception e)
            {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        ///  worker method for the update case
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncInsertWorker(AsyncSendData data, 
                                      AsyncOperation asyncOp,
                                      SendOrPostCallback completionMethodDelegate)

        {
            try
            {
                data.Entry = this.Insert(data.UriToUse, data.Entry, data);
            } catch (Exception e)
            {
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
                                      SendOrPostCallback completionMethodDelegate)

        {
            try
            {
                data.Feed = this.Batch(data.Feed, data.UriToUse, data); 

            } catch (Exception e)
            {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        /// <summary>
        ///  worker method for the direct stream send
        /// </summary>
        /// <param name="data"></param>
        /// <param name="asyncOp"></param>
        /// <param name="completionMethodDelegate"></param>
        /// <returns></returns>
        private void AsyncStreamSendWorker(AsyncSendData data, 
                                      AsyncOperation asyncOp,
                                      SendOrPostCallback completionMethodDelegate)

        {
            try
            {
                data.DataStream = this.StreamSend(data.UriToUse, data.DataStream, data.Type,
                                            data.ContentType, data.SlugHeader, null, data);

            } catch (Exception e)
            {
                data.Exception = e;
            }
            completionMethodDelegate(data);
        }

        internal bool SendProgressData(AsyncData data, AsyncOperationProgressEventArgs args)
        {
            bool ret = true; 
            // In this case, don't allow cancellation, as the method 
            // is about to raise the completed event.
            lock (this.userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(data.UserData) == false)
                {
                    ret = false; 
                }
            }
            if (ret == true)
            {
                data.Operation.Post(data.Delegate, args);
            }
            return ret;
        }



        /// <summary>
        /// this method cancels the corresponding async operation. 
        /// It sends still a completed event, but that event will then
        /// have the cancel property set to true
        /// </summary>
        /// <param name="userData">your identifier for the operation to be cancelled</param>
        public void CancelAsync(object userData)
        {
            lock (this.userStateToLifetime.SyncRoot)
            {
                object obj = this.userStateToLifetime[userData];
                if (obj != null)
                {

                    this.userStateToLifetime.Remove(userData);

                    AsyncOperation asyncOp = obj as AsyncOperation;
                    // The asyncOp object is responsible for 
                    // marshaling the call to the proper 
                    // thread or context.

                    AsyncData data = new AsyncData(null, userData, this.onProgressReportDelegate);
                    AsyncOperationCompletedEventArgs args =
                     new AsyncOperationCompletedEventArgs(data, true);
        
                    asyncOp.PostOperationCompleted(this.onCompletedDelegate, args);
                }
            }
        }
        
        private void InitDelegates()
        {
            this.onProgressReportDelegate = new SendOrPostCallback(AsyncReportProgress);
            this.onCompletedDelegate = new SendOrPostCallback(OnAsyncCompleted);
            this.completionMethodDelegate= new SendOrPostCallback(AsyncCompletionMethod);
        }

    }
    /////////////////////////////////////////////////////////////////////////////

#endif

}
/////////////////////////////////////////////////////////////////////////////
