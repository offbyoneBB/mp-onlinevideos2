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
				// todo : show dialog or notification message when no success?
				//Log.Warn(ex.Message);
				//ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", description, DialogType.OkDialog, false, DialogButtonType.Ok);
				currentBackgroundTask = null;
				completed.Invoke(success, args.GetResult<T>());
			});

			return true;
		}
	}
}
