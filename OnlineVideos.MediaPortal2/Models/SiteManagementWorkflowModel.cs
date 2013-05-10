using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
	public class SiteManagementWorkflowModel : IWorkflowModel
	{
		public enum FilterStateOption { All, Reported, Broken, Working, Updatable, OnlyLocal, OnlyServer };
		public enum SortOption { Updated, Name, Language_Name, Language_Updated };

		#region Constructor

		public SiteManagementWorkflowModel()
		{
			SitesList = new ItemsList();
			// make sure the main workflowmodel is initialized
			var ovMainModel = ServiceRegistration.Get<IWorkflowManager>().GetModel(Guids.WorkFlowModelOV) as OnlineVideosWorkflowModel;
		}

		#endregion

		#region Protected fields

		protected DialogCloseWatcher _dialogCloseWatcher = null;
		protected readonly object syncObject = new object();
		protected IWork currentBackgroundTask = null;
		protected bool newDllsDownloaded = false;
		protected bool newDataSaved = false;

		#endregion

		#region Public properties - Bindable Data

		public ItemsList SitesList { get; protected set; }

		protected AbstractProperty _filterOwnerProperty = new WProperty(typeof(string), null);
		public AbstractProperty Filter_OwnerProperty { get { return _filterOwnerProperty; } }
		public string Filter_Owner
		{
			get { return (string)_filterOwnerProperty.GetValue(); }
			protected set { _filterOwnerProperty.SetValue(value); }
		}

		protected AbstractProperty _filterLanguageProperty = new WProperty(typeof(string), null);
		public AbstractProperty Filter_LanguageProperty { get { return _filterLanguageProperty; } }
		public string Filter_Language
		{
			get { return (string)_filterLanguageProperty.GetValue(); }
			protected set { _filterLanguageProperty.SetValue(value); }
		}

		protected AbstractProperty _filterStateProperty = new WProperty(typeof(FilterStateOption), FilterStateOption.All);
		public AbstractProperty Filter_StateProperty { get { return _filterStateProperty; } }
		public FilterStateOption Filter_State
		{
			get { return (FilterStateOption)_filterStateProperty.GetValue(); }
			protected set { _filterStateProperty.SetValue(value); }
		}


		protected AbstractProperty _sortProperty = new WProperty(typeof(SortOption), SortOption.Updated);
		public AbstractProperty SortProperty { get { return _sortProperty; } }
		public SortOption Sort
		{
			get { return (SortOption)_sortProperty.GetValue(); }
			protected set { _sortProperty.SetValue(value); }
		}

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

		#region Public methods - Callable from GUI

		public void ShowSiteOptions(OnlineSiteViewModel item)
		{
			ServiceRegistration.Get<IWorkflowManager>().NavigatePushTransient(
				WorkflowState.CreateTransientState("OnlineSiteOptions", item.Site.Name, true, "ovsDialogGenericItems", false, WorkflowType.Dialog),
				new NavigationContextConfig()
				{
					AdditionalContextVariables = new Dictionary<string, object>
					{
						{ Constants.CONTEXT_VAR_ITEMS, GetSiteOptions(item) },
						{ Constants.CONTEXT_VAR_COMMAND, new CommandContainer<ListItem>(ExecuteSiteOption) }
					}
				});
		}

		#endregion

		#region IWorkflowModel implementation

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
			actions.Add(Guids.FilterOwnerAction, DynamicWorkflow.CreateDialogMenuAction(
				Guids.FilterOwnerAction,
				"FilterOwner", 
				string.Format("{0}: {1}", LocalizationHelper.Translate("[OnlineVideos.Filter]"), LocalizationHelper.Translate("[OnlineVideos.Creator]")),
				CreateOwnersList(),
				context.WorkflowState,
				(item) => { Filter_Owner = item.AdditionalProperties[Constants.KEY_VALUE] as string; GetFilteredAndSortedSites(); }
			));

			actions.Add(Guids.FilterLanguageAction, DynamicWorkflow.CreateDialogMenuAction(
				Guids.FilterLanguageAction,
				"FilterLanguage",
				string.Format("{0}: {1}", LocalizationHelper.Translate("[OnlineVideos.Filter]"), LocalizationHelper.Translate("[OnlineVideos.Language]")),
				CreateLanguagesList(),
				context.WorkflowState,
				(item) => { Filter_Language = item.AdditionalProperties[Constants.KEY_VALUE] as string; GetFilteredAndSortedSites(); }
			));

			actions.Add(Guids.FilterStateAction, DynamicWorkflow.CreateDialogMenuAction(
				Guids.FilterStateAction,
				"FilterState",
				string.Format("{0}: {1}", LocalizationHelper.Translate("[OnlineVideos.Filter]"), LocalizationHelper.Translate("[OnlineVideos.State]")),
				CreateStatesList(),
				context.WorkflowState,
				(item) => { Filter_State = (FilterStateOption)item.AdditionalProperties[Constants.KEY_VALUE]; GetFilteredAndSortedSites(); }
			));

			actions.Add(Guids.SortSitesAction, DynamicWorkflow.CreateDialogMenuAction(
				Guids.SortSitesAction,
				"SortSites",
				"[OnlineVideos.SortOptions]",
				CreateSortOptionsList(),
				context.WorkflowState,
				(item) => { Sort = (SortOption)item.AdditionalProperties[Constants.KEY_VALUE]; GetFilteredAndSortedSites(); }
			));
		}

		public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
		{
			return ScreenUpdateMode.AutoWorkflowManager;
		}

		#endregion

		#region Private members - Sitelist sorting and filtering

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
					SiteSettings localSite = null;
					localSitesDic.TryGetValue(site.Name, out localSite);
					var loListItem = new OnlineSiteViewModel(site, localSite);
					SitesList.Add(loListItem);
				}
			}

			SitesList.FireChange();
		}

		bool SitePassesFilter(OnlineVideosWebservice.Site site)
		{
			// language
			if (!string.IsNullOrEmpty(Filter_Language) && site.Language != Filter_Language) return false;
			// owner
			if (!string.IsNullOrEmpty(Filter_Owner))
			{
				string owner = site.Owner_FK != null ? site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')) : "";
				if (owner != Filter_Owner) return false;
			}
			// state
			switch (Filter_State)
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
			switch (Sort)
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

		#endregion

		#region Private members - Menu commands

		void RunUpdate(NavigationContext context)
		{
			bool isManualUpdate = context.DisplayLabel == "[OnlineVideos.UpdateAll]";
			currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(() =>
			{
				try
				{
					bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites((m, p) =>
					{
						UpdateInfo = m ?? string.Empty;
						if (p.HasValue) UpdateProgress = p.Value;
						return currentBackgroundTask.State != WorkState.CANCELED;
					}, isManualUpdate ? SitesList.Select(s => ((OnlineSiteViewModel)s).Site).ToList() : null);
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

		void RemoveAllSites()
		{
			if (SitesList.Count > 0)
			{
				var dialogHandleId = ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.RemoveAllFromMySites]", "", DialogType.YesNoDialog, false, DialogButtonType.Cancel);
				_dialogCloseWatcher = new DialogCloseWatcher(this, dialogHandleId, (dialogResult) =>
				{
					if (dialogResult == DialogResult.Yes)
					{
						bool needRefresh = false;
						foreach (var siteToRemove in SitesList)
						{
							SiteSettings localSite = null;
							var localSiteIndex = OnlineVideoSettings.Instance.GetSiteByName(((OnlineSiteViewModel)siteToRemove).Site.Name, out localSite);
							if (localSiteIndex >= 0)
							{
								OnlineVideoSettings.Instance.RemoveSiteAt(localSiteIndex);
								needRefresh = true;
							}
						}
						if (needRefresh)
						{
							OnlineVideoSettings.Instance.SaveSites();
							newDataSaved = true;
							GetFilteredAndSortedSites();
						}
					}
				});
			}
		}

		ItemsList CreateSortOptionsList()
		{
			var items = new ItemsList();
			foreach (var sortOption in Enum.GetValues(typeof(SortOption)))
			{
				var sortItem = new ListItem(Consts.KEY_NAME, string.Join(", ", Enum.GetName(typeof(SortOption), sortOption).Split('_').Select(s => LocalizationHelper.Translate(string.Format("[OnlineVideos.{0}]", s)))));
				sortItem.AdditionalProperties[Constants.KEY_VALUE] = sortOption;
				items.Add(sortItem);
			}
			return items;
		}

		ItemsList CreateStatesList()
		{
			var items = new ItemsList();
			foreach (var state in Enum.GetValues(typeof(FilterStateOption)))
			{
				var stateItem = new ListItem(Consts.KEY_NAME, "[OnlineVideos."+ Enum.GetName(typeof(FilterStateOption), state) + "]");
				stateItem.AdditionalProperties[Constants.KEY_VALUE] = state;
				items.Add(stateItem);
			}
			return items;
		}

		ItemsList CreateOwnersList()
		{
			var items = new ItemsList();
			var allItem = new ListItem(Consts.KEY_NAME, "[OnlineVideos.All]");
			allItem.AdditionalProperties[Constants.KEY_VALUE] = null;
			items.Add(allItem);
			foreach (var owner in Sites.Updater.OnlineSites.Select(s => s.Owner_FK != null ? s.Owner_FK.Substring(0, s.Owner_FK.IndexOf('@')) : "").Distinct().OrderBy(s => s))
			{
				var ownerItem = new ListItem(Consts.KEY_NAME, owner);
				ownerItem.AdditionalProperties[Constants.KEY_VALUE] = owner;
				items.Add(ownerItem);
			}
			return items;
		}

		ItemsList CreateLanguagesList()
		{
			var items = new ItemsList();
			var allItem = new ListItem(Consts.KEY_NAME, "[OnlineVideos.All]");
			allItem.AdditionalProperties[Constants.KEY_VALUE] = null;
			items.Add(allItem);
			foreach (var lang in Sites.Updater.OnlineSites.Select(s => s.Language != null ? s.Language : "--").Distinct().Select(s => new { Code = s, Name = GetLocalizedLanguageName(s) }).OrderBy(s => s.Name))
			{
				var langItem = new ListItem(Consts.KEY_NAME, lang.Name);
				langItem.AdditionalProperties[Constants.KEY_VALUE] = lang.Code;
				items.Add(langItem);
			}
			return items;
		}

		#endregion

		#region Private members - ContextMenu

		ItemsList GetUserReports(OnlineSiteViewModel item)
		{
			var items = new ItemsList();

			OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
			var reports = ws.GetReports(item.Site.Name);
			foreach (var report in reports.OrderByDescending(r => r.Date))
			{
				items.Add(new ReportViewModel(report));
			}
			return items;
		}

		ItemsList GetSiteOptions(OnlineSiteViewModel item)
		{
			var items = new ItemsList();

			if (item.LocalSite == null && item.Site.State != OnlineVideosWebservice.SiteState.Broken)
			{
				var option = new ListItem(Consts.KEY_NAME, "[OnlineVideos.AddToMySites]");
				option.AdditionalProperties[Constants.KEY_VALUE] = "AddToMySites";
				items.Add(option);
			}
			else
			{
				if ((item.Site.LastUpdated - item.LocalSite.LastUpdated).TotalMinutes > 2 && item.Site.State != OnlineVideosWebservice.SiteState.Broken)
				{
					var option = new ListItem(Consts.KEY_NAME, "[OnlineVideos.UpdateMySite]");
					option.AdditionalProperties[Constants.KEY_VALUE] = "UpdateMySite";
					items.Add(option);
				}
				var optionR = new ListItem(Consts.KEY_NAME, "[OnlineVideos.RemoveFromMySites]");
				optionR.AdditionalProperties[Constants.KEY_VALUE] = "RemoveFromMySites";
				items.Add(optionR);
			}

			if (!string.IsNullOrEmpty(item.Site.Owner_FK) && item.LocalSite != null) // !only local && ! only global
			{
				var option = new ListItem(Consts.KEY_NAME, "[OnlineVideos.ShowReports]");
				option.AdditionalProperties[Constants.KEY_VALUE] = "ShowReports";
				items.Add(option);

				if (item.Site.State != OnlineVideosWebservice.SiteState.Broken)
				{
					var optionR = new ListItem(Consts.KEY_NAME, "[OnlineVideos.ReportBroken]");
					optionR.AdditionalProperties[Constants.KEY_VALUE] = "ReportBroken";
					items.Add(optionR);
				}
			}

			foreach (var anOption in items)
				anOption.AdditionalProperties.Add(Consts.KEY_MEDIA_ITEM, item);

			return items;
		}

		void ExecuteSiteOption(ListItem option)
		{
			var site = option.AdditionalProperties[Consts.KEY_MEDIA_ITEM] as OnlineSiteViewModel;
			SiteSettings localSite = null;
			var localSiteIndex = OnlineVideoSettings.Instance.GetSiteByName(((OnlineSiteViewModel)site).Site.Name, out localSite);

			switch (option.AdditionalProperties[Constants.KEY_VALUE] as string)
			{
				case "AddToMySites":
				case "UpdateMySite":
					bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites(null, new List<OnlineVideosWebservice.Site> { site.Site }, false, false);
					if (updateResult == true) newDllsDownloaded = true;
					else if (updateResult == null) newDataSaved = true;
					break;
				case "RemoveFromMySites":
					OnlineVideoSettings.Instance.RemoveSiteAt(localSiteIndex);
					OnlineVideoSettings.Instance.SaveSites();
					newDataSaved = true;
					GetFilteredAndSortedSites();
					break;
				case "ShowReports":
					ServiceRegistration.Get<IWorkflowManager>().NavigatePush(Guids.WorkflowStateUserReports, new NavigationContextConfig()
					{
						NavigationContextDisplayLabel = site.Site.Name,
						AdditionalContextVariables = new Dictionary<string, object>
						{
							{ Constants.CONTEXT_VAR_ITEMS, GetUserReports(site) }
						}
					});
					break;
				case "ReportBroken":
					// wait for the current dialog to close!
					var scm = ServiceRegistration.Get<IScreenManager>();
					while (scm.IsDialogVisible) System.Threading.Thread.Sleep(20);
					// when site is not up 2 date show message telling the user to update the local site first
					if ((site.Site.LastUpdated - site.LocalSite.LastUpdated).TotalMinutes > 1)
					{
						ServiceRegistration.Get<IDialogManager>().ShowDialog(site.Site.Name, "[OnlineVideos.PleaseUpdateLocalSite]", DialogType.OkDialog, false, DialogButtonType.Ok);
					}
					else
					{
						ServiceRegistration.Get<IWorkflowManager>().NavigatePush(Guids.DialogStateReportSite, new NavigationContextConfig()
						{
							NavigationContextDisplayLabel = site.Site.Name,
							AdditionalContextVariables = new Dictionary<string, object>
							{
								{ Constants.CONTEXT_VAR_COMMAND, new CommandContainer<string, OnlineVideosWebservice.Site>(ReportSite, site.Site) }
							}
						});
					}
					break;
			}
		}

		public void ReportSite(string userReason, OnlineVideosWebservice.Site site)
		{
			if (userReason.Length < 15)
			{
				ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.ReportBroken]", "[OnlineVideos.PleaseEnterDescription]", DialogType.OkDialog, false, DialogButtonType.Ok);
			}
			else
			{
				OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
				string message = "";
				bool success = ws.SubmitReport(site.Name, userReason, OnlineVideosWebservice.ReportType.Broken, out message);
				ServiceRegistration.Get<IDialogManager>().ShowDialog(success ? "[OnlineVideos.Done]" : "[OnlineVideos.Error]", message, DialogType.OkDialog, false, DialogButtonType.Ok);
				if (success)
				{
					// reload online sites
					OnlineVideos.Sites.Updater.GetRemoteOverviews(true);
					GetFilteredAndSortedSites();
				}
			}
		}

		string GetLocalizedLanguageName(string aLang)
		{
			string name = aLang;
			try
			{
				name = aLang != "--" ? CultureInfo.GetCultureInfoByIetfLanguageTag(aLang).DisplayName : "Global";
			}
			catch
			{
				var temp = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(
					ci => ci.IetfLanguageTag == aLang || ci.ThreeLetterISOLanguageName == aLang || ci.TwoLetterISOLanguageName == aLang || ci.ThreeLetterWindowsLanguageName == aLang);
				if (temp != null)
				{
					name = temp.DisplayName;
				}
			}
			return name;
		}

		#endregion
	}
}
