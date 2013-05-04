using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace OnlineVideos.MediaPortal2
{
	public class SiteManagementWorkflowModel : IWorkflowModel
	{
		public SiteManagementWorkflowModel()
		{
			SitesList = new ItemsList();
			// make sure the main workflowmodel is instantiated
			var ovMainModel = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel;
		}

		enum FilterStateOption { All, Reported, Broken, Working, Updatable, OnlyLocal, OnlyServer };
		enum SortOption { Updated, Name, Language_Name, Language_Updated };

		string filter_Owner;
		string filter_Language;
		FilterStateOption filter_State;
		SortOption sort;

		public ItemsList SitesList { get; protected set; }

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

		public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			return true;
		}

		public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
		{
			//
		}

		public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			//
		}

		public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			var update = Sites.Updater.GetRemoteOverviews();
			if (update) GetFilteredAndSortedSites();
		}

		public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			//
		}

		public Guid ModelId
		{
			get { return Guids.WorkFlowModelSiteManagement; }
		}

		public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
		{
			//
		}

		public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
		{
			//
		}

		public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
		{
			return ScreenUpdateMode.AutoWorkflowManager;
		}
	}
}
