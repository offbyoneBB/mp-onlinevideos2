using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace OnlineVideos.MediaPortal2
{
	public class SiteManagementWorkflowModel : IWorkflowModel
	{
		enum FilterStateOption { All, Reported, Broken, Working, Updatable, OnlyLocal, OnlyServer };
		enum SortOption { Updated, Name, Language_Name, Language_Updated };

		public SiteManagementWorkflowModel()
		{
			SitesList = new ItemsList();
			// make sure the main workflowmodel is initialized
			var ovMainModel = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel;
		}

		object syncObject = new object();
		IWork currentBackgroundTask = null;
		bool newDllsDownloaded = false;
		bool newDataSaved = false;

		string filter_Owner;
		string filter_Language;
		FilterStateOption filter_State;
		SortOption sort;

		public ItemsList SitesList { get; protected set; }

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

		void GetFilteredAndSortedSites()
		{
			SitesList.Clear();

			var localSitesDic = OnlineVideoSettings.Instance.SiteSettingsList.ToDictionary(s => s.Name, s => s);
			var onlyLocalSites = OnlineVideoSettings.Instance.SiteSettingsList.ToDictionary(s => s.Name, s => s);
			List<OnlineVideosWebservice.Site> filteredsortedSites = new List<OnlineVideos.OnlineVideosWebservice.Site>(Sites.Updater.OnlineSites);
			filteredsortedSites.ForEach(os => { if (localSitesDic.ContainsKey(os.Name)) onlyLocalSites.Remove(os.Name); });
			filteredsortedSites.AddRange(onlyLocalSites.Select(ls => new OnlineVideosWebservice.Site() { Name = ls.Value.Name, IsAdult = ls.Value.ConfirmAge, Description = ls.Value.Description, Language = ls.Value.Language, LastUpdated = ls.Value.LastUpdated }));
			filteredsortedSites = filteredsortedSites.FindAll(SitePassesFilter);
			filteredsortedSites.Sort(CompareSiteForSort);

			foreach (OnlineVideosWebservice.Site site in filteredsortedSites)
			{
				if (!site.IsAdult || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed)
				{
					var loListItem = new OnlineSiteViewModel(site, localSitesDic.ContainsKey(site.Name));
					SitesList.Add(loListItem);
				}
			}

			SitesList.FireChange();
		}

		bool SitePassesFilter(OnlineVideosWebservice.Site site)
		{
			// language
			if (!string.IsNullOrEmpty(filter_Language) && site.Language != filter_Language) return false;
			// owner
			if (!string.IsNullOrEmpty(filter_Owner))
			{
				string owner = site.Owner_FK != null ? site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')) : "";
				if (owner != filter_Owner) return false;
			}
			// state
			switch (filter_State)
			{
				case FilterStateOption.Working:
					return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Working;
				case FilterStateOption.Reported:
					return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Reported;
				case FilterStateOption.Broken:
					return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Broken;
				case FilterStateOption.Updatable:
					SiteSettings localSite = null;
					if (OnlineVideoSettings.Instance.GetSiteByName(site.Name, out localSite) >= 0)
					{
						if ((site.LastUpdated - localSite.LastUpdated).TotalMinutes > 2) return true;
						else return false;
					}
					return false;
				case FilterStateOption.OnlyLocal:
					return OnlineVideoSettings.Instance.GetSiteByName(site.Name, out localSite) >= 0;
				case FilterStateOption.OnlyServer:
					return OnlineVideoSettings.Instance.GetSiteByName(site.Name, out localSite) < 0;
				default: return true;
			}
		}

		int CompareSiteForSort(OnlineVideosWebservice.Site site1, OnlineVideosWebservice.Site site2)
		{
			switch (sort)
			{
				case SortOption.Updated:
					return site2.LastUpdated.CompareTo(site1.LastUpdated);
				case SortOption.Name:
					return site1.Name.CompareTo(site2.Name);
				case SortOption.Language_Updated:
					int langCompResult = site1.Language.CompareTo(site2.Language);
					if (langCompResult == 0)
					{
						return site2.LastUpdated.CompareTo(site1.LastUpdated);
					}
					else return langCompResult;
				case SortOption.Language_Name:
					int langCompResult2 = site1.Language.CompareTo(site2.Language);
					if (langCompResult2 == 0)
					{
						return site1.Name.CompareTo(site2.Name);
					}
					else return langCompResult2;
			}
			return 0;
		}

		void RunUpdate(NavigationContext context)
		{
			currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
			{
				try
				{
					bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites((m, p) =>
					{
						UpdateInfo = m ?? string.Empty;
						if (p.HasValue) UpdateProgress = p.Value;
						return currentBackgroundTask.State != WorkState.CANCELED;
					});
					if (updateResult == true) newDllsDownloaded = true;
					else if (updateResult == null) newDataSaved = true;
				}
				catch (Exception ex)
				{
					currentBackgroundTask.Exception = ex;
				}
			},
			(args) =>
			{
				GetFilteredAndSortedSites();
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

		public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
		{
			return true;
		}

		public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
		{
			if (newContext.WorkflowState.StateId == Guids.DialogStateSiteUpdate)
			{
				// start the update in a background thread upon entering the dialog state
				RunUpdate(newContext);
			}
			else if (oldContext.WorkflowState.StateId == Guids.DialogStateSiteUpdate)
			{
				// cancel the update thread if it is still running
				lock (syncObject)
				{
					if (currentBackgroundTask != null) currentBackgroundTask.State = WorkState.CANCELED;
				}
			}
		}

		public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
		{
			//
		}

		public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
		{
			var update = Sites.Updater.GetRemoteOverviews();
			if (update) GetFilteredAndSortedSites();
		}

		public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
		{
			// reload DLLs or Sites depending on change
			if (newDllsDownloaded)
			{
				Log.Info("Reloading SiteUtil Dlls at runtime.");
				// todo : stop playback if an OnlineVideos video is playing
				DownloadManager.Instance.StopAll();
				// now reload the appdomain
				OnlineVideoSettings.Reload();
				TranslationLoader.SetTranslationsToSingleton();
				OnlineVideoSettings.Instance.BuildSiteUtilsList();
				GC.Collect();
				GC.WaitForFullGCComplete();
				(ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel).RebuildSitesList();
			}
			else if (newDataSaved)
			{
				OnlineVideoSettings.Instance.BuildSiteUtilsList();
				(ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel).RebuildSitesList();
			}
			newDataSaved = false;
			newDllsDownloaded = false;
		}

		public Guid ModelId
		{
			get { return Guids.WorkFlowModelSiteManagement; }
		}

		public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
		{
			//
		}

		public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
		{
			//
		}

		public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
		{
			return ScreenUpdateMode.AutoWorkflowManager;
		}
	}
}
