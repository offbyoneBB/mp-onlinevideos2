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

		public bool IsExecuting { get { return currentBackgroundTask != null; } }
		Work currentBackgroundTask = null;

		internal bool Start<T>(Func<T> work, Action<bool, T> completed, string description = null)
		{
			if (IsExecuting) return false;

			ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
			currentBackgroundTask = (Work)ServiceRegistration.Get<IThreadPool>().Add(() =>
			{
				try
				{
					currentBackgroundTask.EventArgs.SetResult<T>(work());
				}
				catch (Exception ex)
				{
					currentBackgroundTask.Exception = ex;
				}
			},
			(args) =>
			{
				ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
				bool success = currentBackgroundTask.Exception == null && currentBackgroundTask.State == WorkState.FINISHED;
                if (!success)
                {
                    // show dialog or notification message when no success
                    Log.Warn(currentBackgroundTask.Exception.ToString());
                    var ovError = currentBackgroundTask.Exception as OnlineVideosException;
                    bool showDescription = ovError != null ? ovError.ShowCurrentTaskDescription : true;
                    string info = string.Format("{0}\n{1}", showDescription ? description : "", currentBackgroundTask.Exception.Message);
                    ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", info, DialogType.OkDialog, false, DialogButtonType.Ok);
                }
				currentBackgroundTask = null;
				completed.Invoke(success, args.GetResult<T>());
			});

			return true;
		}
	}
}
