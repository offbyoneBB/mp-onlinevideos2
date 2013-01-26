using System;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;

namespace OnlineVideos.MediaPortal1
{
    internal class Gui2UtilConnector
    {
        # region Singleton
        protected Gui2UtilConnector()
        {
            timeoutTimer.Elapsed += TaskWatcherTimerElapsed;
        }
        protected static Gui2UtilConnector instance = null;
        internal static Gui2UtilConnector Instance
        {
            get
            {
                if (instance == null) instance = new Gui2UtilConnector();
                return instance;
            }
        }
        #endregion

        internal bool IsBusy { get; private set; }

        Action<bool, object> _CurrentResultHandler = null;
        object _CurrentResult = null;
        bool? _CurrentTaskSuccess = null;
        OnlineVideosException _CurrentError = null;
        string _CurrentTaskDescription = null;
        Thread backgroundThread = null;
        bool abortedByUser = false;
        System.Timers.Timer timeoutTimer = new System.Timers.Timer(OnlineVideoSettings.Instance.UtilTimeout * 1000) { AutoReset = false };

        public void StopBackgroundTask()
        {
            StopBackgroundTask(true);
        }

        void StopBackgroundTask(bool byUserRequest)
        {
            if (IsBusy && _CurrentTaskSuccess == null && backgroundThread != null && backgroundThread.IsAlive)
            {
                Log.Instance.Info("Aborting background thread{0}.", byUserRequest ? " by User Request" : "");
                backgroundThread.Abort();
                abortedByUser = byUserRequest;
                return;
            }
        }

        void TaskWatcherTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopBackgroundTask(false);
        }

        /// <summary>
        /// This method should be used to call methods from siteutils that might take a few seconds.
        /// It makes sure only on thread at a time executes and has a timeout for the execution.
        /// It also catches Exceptions from the utils and writes errors to the log, and show a message on the GUI.
        /// The Wait Cursor will be shown on while executing the task and the resultHandler will be called on the MPMain thread.
        /// </summary>
        /// <param name="task">method to invoke on a background thread</param>
        /// <param name="resultHandler">method to invoke on the GUI Thread with the result of the task</param>
        /// <param name="taskDescription">description of the tak to be invoked - will be shown in the error message if execution fails or times out</param>
        /// <param name="timeout">true: use the timeout, or false: wait forever</param>
        /// <returns>true, if the task could be successfully started in the background</returns>
        internal bool ExecuteInBackgroundAndCallback(Func<object> task, Action<bool, object> resultHandler, string taskDescription, bool timeout)
        {
            // make sure only one background task can be executed at a time
            if (!IsBusy && Monitor.TryEnter(this))
            {
                try
                {
                    IsBusy = true;
                    abortedByUser = false;
                    _CurrentResultHandler = resultHandler;
                    _CurrentTaskDescription = taskDescription;
                    _CurrentResult = null;
                    _CurrentError = null;
                    _CurrentTaskSuccess = null;// while this is null the task has not finished (or later on timeouted), true indicates successfull completion and false error
                    GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor in MediaPortal
                    backgroundThread = new Thread(delegate()
                    {
                        try
                        {
                            _CurrentResult = task.Invoke();
                            _CurrentTaskSuccess = true;
                        }
                        catch (ThreadAbortException)
                        {
                            if (!abortedByUser) Log.Instance.Warn("Timeout waiting for results.");
                            Thread.ResetAbort();
                        }
                        catch (Exception threadException)
                        {
                            _CurrentError = threadException as OnlineVideosException;
                            Log.Instance.Warn(threadException.ToString());
                            _CurrentTaskSuccess = false;
                        }
                        timeoutTimer.Stop();
                        // hide the wait cursor
                        GUIWaitCursor.Hide();
                        // execute the ResultHandler on the Main Thread
                        GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) => { ExecuteTaskResultHandler(); return 0; }, 0, 0, null);
                    }) { Name = "OnlineVideos", IsBackground = true };
                    // disable timeout when debugging
                    if (timeout && !System.Diagnostics.Debugger.IsAttached) timeoutTimer.Start();
                    backgroundThread.Start();
                    // successfully started the background task
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Instance.Error(ex);
                    IsBusy = false;
                    _CurrentResultHandler = null;
                    GUIWaitCursor.Hide(); // hide the wait cursor
                    return false; // could not start the background task
                }
            }
            else
            {
                Log.Instance.Error("Another thread tried to execute a task in background.");
                return false;
            }
        }

        void ExecuteTaskResultHandler()
        {
            if (!IsBusy) return;

            // show an error message if task was not completed successfully
            if (_CurrentTaskSuccess != true)
            {
                if (_CurrentError != null)
                {
                    MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    if (dlg_error != null)
                    {
                        dlg_error.Reset();
                        dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        if (_CurrentError.ShowCurrentTaskDescription)
                        {
							dlg_error.SetLine(1, string.Format("{0} {1}", Translation.Instance.Error, _CurrentTaskDescription));
                        }
                        dlg_error.SetLine(2, _CurrentError.Message);
                        dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
                else
                {
                    GUIDialogNotify dlg_error = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (dlg_error != null)
                    {
                        dlg_error.Reset();
						dlg_error.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        if (_CurrentTaskSuccess.HasValue)
							dlg_error.SetText(string.Format("{0} {1}", Translation.Instance.Error, _CurrentTaskDescription));
                        else
							dlg_error.SetText(string.Format("{0} {1}", Translation.Instance.Timeout, _CurrentTaskDescription));
                        if (!abortedByUser) dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
            }

            // store info needed to invoke the result handler
            bool stored_TaskSuccess = _CurrentTaskSuccess == true;
            var stored_Handler = _CurrentResultHandler;
            object stored_ResultObject = _CurrentResult;

            // clear all fields and allow execution of another background task 
            // before actually executing the result handler -> this way a result handler can also inovke another background task)
            _CurrentResultHandler = null;
            _CurrentResult = null;
            _CurrentTaskSuccess = null;
            _CurrentError = null;
            backgroundThread = null;
            abortedByUser = false;
            IsBusy = false;
            timeoutTimer.Stop();
            Monitor.Exit(this);

            // execute the result handler
            if (stored_Handler != null) stored_Handler.Invoke(stored_TaskSuccess, stored_ResultObject);
        }
    }
}
