using System;
using System.Threading;
using OnlineVideos;

namespace Standalone
{    
    public class Gui2UtilConnector
    {
        public class ResultInfo
        {
            /// <summary>
            /// null: task not finished or timeout expired | true: successfull completion | false: Exception thrown by task
            /// </summary>
            public bool? TaskSuccess { get; internal set; }
            public object ResultObject { get; internal set; }
            public OnlineVideosException TaskError { get; internal set; }
            public bool AbortedByUser { get; internal set; }
        }

        # region Singleton
        protected Gui2UtilConnector()
        {
            timeoutTimer = new System.Timers.Timer(OnlineVideoSettings.Instance.UtilTimeout * 1000) { AutoReset = false };
            timeoutTimer.Elapsed += (o, e) => StopBackgroundTask(false);
        }
        protected static Gui2UtilConnector instance = null;
        public static Gui2UtilConnector Instance
        {
            get
            {
                if (instance == null) instance = new Gui2UtilConnector();
                return instance;
            }
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// This Property indicates that a background task is running and the result handler has not been executed.
        /// </summary>
        public bool IsBusy { get; protected set; }
        
        /// <summary>
        /// The event is invoked when the current task has finished executing.
        /// Careful: It will be called from a background thread!
        /// Use it, to schedule or directly invoke the <see cref="ExecuteTaskResultHandler"/> method.
        /// </summary>
        public event Action TaskFinishedCallback;

        #endregion

        #region Protected Fields

        protected Thread backgroundThread = null;
        protected System.Timers.Timer timeoutTimer = null;
        protected ResultInfo resultInfo;
        protected Action<ResultInfo> currentResultHandler = null;        
        
        #endregion

        /// <summary>
        /// Try to abort the background thread.
        /// </summary>
        /// <param name="byUserRequest"></param>
        public void StopBackgroundTask(bool byUserRequest = true)
        {
            if (IsBusy && resultInfo != null && resultInfo.TaskSuccess == null && backgroundThread != null && backgroundThread.IsAlive)
            {
                Log.Info("Aborting background thread.");
                backgroundThread.Abort();
                resultInfo.AbortedByUser = byUserRequest;
                return;
            }
        }        

        /// <summary>
        /// This method should be used to call methods from site utils that might take a few seconds.
        /// It makes sure only one task at a time executes and has a timeout for the execution.
        /// It also catches Execeptions from the utils and writes errors to the log.
        /// </summary>
        /// <param name="task">a delegate pointing to the method to invoke in a background thread.</param>
        /// <param name="resultHandler">the method to invoke to handle the result of the background task</param>
        /// <param name="timeoutEnabled">default: true, watches the background task and tries to abort it when timeout has elapsed</param>
        /// <returns>true, if execution finished successfully before the timeout expired.</returns>
        public bool ExecuteInBackgroundAndCallback(Func<object> task, Action<ResultInfo> resultHandler, bool timeoutEnabled = true)
        {
            // make sure only one background task can be executed at a time
            if (!IsBusy && Monitor.TryEnter(this))
            {
                try
                {
                    IsBusy = true;
                    resultInfo = new ResultInfo();
                    currentResultHandler = resultHandler;
                    backgroundThread = new Thread(delegate()
                    {
                        try
                        {
                            resultInfo.ResultObject = task.Invoke();
                            resultInfo.TaskSuccess = true;
                        }
                        catch (ThreadAbortException)
                        {
                            if (!resultInfo.AbortedByUser) Log.Warn("Timeout waiting for results.");
                            Thread.ResetAbort();
                        }
                        catch (Exception threadException)
                        {
                            resultInfo.TaskError = threadException as OnlineVideosException;
                            Log.Warn(threadException.ToString());
                            resultInfo.TaskSuccess = false;
                        }
                        timeoutTimer.Stop();
                        TaskFinishedCallback();
                    }) { Name = "OnlineVideos", IsBackground = true };

                    backgroundThread.Start();
                    // only timeout when parameter was set and not debugging
                    if (timeoutEnabled && !System.Diagnostics.Debugger.IsAttached) timeoutTimer.Start();
                    // successfully started the background task
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    IsBusy = false;
                    resultInfo = null;
                    return false; // could not start the background thread
                }
            }
            else
            {
                Log.Error("Another thread tried to execute a task in background.");
                return false;
            }
        }

        public void ExecuteTaskResultHandler()
        {
            if (!IsBusy) return;            

            // store info needed to invoke the result handler
            var stored_ResultInfo = resultInfo;
            var stored_Handler = currentResultHandler;            

            // clear all fields to allow execution of another background task before actually executing the result handler
            // -> this way a result handler can also inovke another background task)
            currentResultHandler = null;            
            backgroundThread = null;
            IsBusy = false;
            Monitor.Exit(this);

            // execute the result handler
            if (stored_Handler != null) stored_Handler.Invoke(stored_ResultInfo);
        }
    }
}
