using System;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Screens;

namespace OnlineVideos.MediaPortal2
{
    internal class BackgroundTask
    {
        protected static BackgroundTask _instance = null;

        protected BackgroundTask()
        {
        }

        internal static BackgroundTask Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BackgroundTask();
                return _instance;
            }
        }

        public bool IsExecuting { get { return _currentBackgroundTask != null; } }
        private volatile IWork _currentBackgroundTask = null;

        internal bool Start<T>(Func<T> work, Action<bool, T> completed, string description = null)
        {
            if (IsExecuting) return false;

            _currentBackgroundTask = new BackgroundWork<T>(work, (success, result) => { _currentBackgroundTask = null; completed(success, result); }, description);
            return ServiceRegistration.Get<IThreadPool>().Add(_currentBackgroundTask);
        }
    }

    internal class BackgroundWork<T> : IWork
    {
        private readonly Func<T> work;
        private readonly Action<bool, T> completed;

        public WorkState State { get; set; }
        public string Description { get; set; }
        public Exception Exception { get; set; }
        public ThreadPriority ThreadPriority { get; set; }

        internal BackgroundWork(Func<T> work, Action<bool, T> completed, string description = null)
        {
            this.work = work;
            this.completed = completed;
            this.Description = description;
            this.State = WorkState.INIT;
        }

        void IWork.Process()
        {
            if (State != WorkState.INQUEUE)
                return;

            ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();

            State = WorkState.INPROGRESS;

            T result = default(T);
            bool success = false;
            try
            {
                result = work();
                success = true;
                State = WorkState.FINISHED;
            }
            catch (Exception ex)
            {
                State = WorkState.ERROR;
                Exception = ex;
            }
            finally
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
            }

            if (State == WorkState.ERROR)
                ShowErrorDialog();

            completed(success, result);
        }

        private void ShowErrorDialog()
        {
            Log.Warn(Exception?.ToString());
            bool showDescription = (Exception as OnlineVideosException)?.ShowCurrentTaskDescription ?? true;
            string info = string.Format("{0}\n{1}", showDescription ? Description : "", Exception.Message);
            ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", info, DialogType.OkDialog, false, DialogButtonType.Ok);
        }
    }
}
