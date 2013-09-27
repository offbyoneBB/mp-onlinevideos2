using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace OnlineVideos.MediaPortal2
{
	public class SiteUpdateWorkflowModels : IWorkflowModel
	{
		#region Protected fields

		protected readonly object syncObject = new object();
		protected IWork currentBackgroundTask = null;

		#endregion

		#region Public properties - Bindable Data

		protected AbstractProperty _updateProgressProperty = new WProperty(typeof(byte), (byte)0);
		public AbstractProperty UpdateProgressProperty { get { return _updateProgressProperty; } }
		public byte UpdateProgress
		{
			get { return (byte)_updateProgressProperty.GetValue(); }
			protected set { _updateProgressProperty.SetValue(value); }
		}

		protected AbstractProperty _updateInfoProperty = new WProperty(typeof(string), string.Empty);
		public AbstractProperty UpdateInfoProperty { get { return _updateInfoProperty; } }
		public string UpdateInfo
		{
			get { return (string)_updateInfoProperty.GetValue(); }
			protected set { _updateInfoProperty.SetValue(value); }
		}

		#endregion

		#region IWorkflowModel implementation

		public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			return true;
		}

		public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
		{

		}

		public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			
		}

		public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			// reset the properties
			UpdateInfo = string.Empty;
			UpdateProgress = 0;
			// start the update in a background thread
			RunUpdate(newContext);
		}

		public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			// set a flag to stop the update if the thread is still running
			lock (syncObject)
			{
				if (currentBackgroundTask != null) currentBackgroundTask.State = WorkState.CANCELED;
			}
			// wait until the update background thread has ended
			while (currentBackgroundTask != null)
			{
				System.Threading.Thread.Sleep(20);
			}
		}

		public Guid ModelId
		{
			get { return Guids.WorkFlowModelSiteUpdate; }
		}

		public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			
		}

		public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
		{
			
		}

		public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
		{
			return ScreenUpdateMode.AutoWorkflowManager;
		}

		#endregion

		void RunUpdate(NavigationContext context)
		{
			currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
			{
				try
				{
					List<OnlineVideosWebservice.Site> sitesToUpdate = null;
					bool onlyUpdateNoAdd = true;
					bool isManualUpdate = context.DisplayLabel == "[OnlineVideos.UpdateAll]";
					if (isManualUpdate)
					{
						onlyUpdateNoAdd = false;
						sitesToUpdate = (ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelSiteManagement) as SiteManagementWorkflowModel).SitesList.Select(s => ((OnlineSiteViewModel)s).Site).ToList();
					}

					bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites((m, p) =>
					{
						UpdateInfo = m ?? string.Empty;
						if (p.HasValue) UpdateProgress = p.Value;
						return currentBackgroundTask.State != WorkState.CANCELED;
					}, sitesToUpdate, onlyUpdateNoAdd);

					if (!isManualUpdate)
					{
						var settingsManager = ServiceRegistration.Get<ISettingsManager>();
						var settings = settingsManager.Load<Configuration.Settings>();
						settings.LastAutomaticUpdate = DateTime.Now;
						settingsManager.Save(settings);
					}

					SystemMessage msg = new SystemMessage(OnlineVideosMessaging.MessageType.SitesUpdated);
					msg.MessageData[OnlineVideosMessaging.UPDATE_RESULT] = updateResult;
					ServiceRegistration.Get<IMessageBroker>().Send(OnlineVideosMessaging.CHANNEL, msg);
				}
				catch (Exception ex)
				{
					currentBackgroundTask.Exception = ex;
				}
			},
			(args) =>
			{
				lock (syncObject)
				{
					currentBackgroundTask = null;
				}
				// close dialog when still open
				var screenMgr = ServiceRegistration.Get<IScreenManager>();
				if (screenMgr.TopmostDialogInstanceId == context.DialogInstanceId)
					screenMgr.CloseTopmostDialog();
			});
		}

	}
}
