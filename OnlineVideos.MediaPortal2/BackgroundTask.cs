using System;
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
        Work _currentBackgroundTask = null;

        internal bool Start<T>(Func<T> work, Action<bool, T> completed, string description = null)
        {
            if (IsExecuting) return false;

            ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
            _currentBackgroundTask = (Work)ServiceRegistration.Get<IThreadPool>().Add(() =>
            {
                try
                {
                    _currentBackgroundTask.EventArgs.SetResult<T>(work());
                }
                catch (Exception ex)
                {
                    _currentBackgroundTask.Exception = ex;
                }
            },
            (args) =>
            {
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                bool success = _currentBackgroundTask.Exception == null && _currentBackgroundTask.State == WorkState.FINISHED;
                if (!success)
                {
                    // show dialog or notification message when no success
                    Log.Warn(_currentBackgroundTask.Exception.ToString());
                    var ovError = _currentBackgroundTask.Exception as OnlineVideosException;
                    bool showDescription = ovError != null ? ovError.ShowCurrentTaskDescription : true;
                    string info = string.Format("{0}\n{1}", showDescription ? description : "", _currentBackgroundTask.Exception.Message);
                    ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", info, DialogType.OkDialog, false, DialogButtonType.Ok);
                }
                _currentBackgroundTask = null;
                completed.Invoke(success, args.GetResult<T>());
            });

            return true;
        }
    }
}
