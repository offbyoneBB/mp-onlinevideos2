using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Configuration;
using Action = MediaPortal.GUI.Library.Action;
using System.Reflection;

namespace OnlineVideos.MediaPortal1
{
    public class GUISiteUpdater : GUIWindow
    {
        public const int WindowId = 4757;

        enum FilterStateOption { All, Reported, Broken, Working, Updatable, OnlyLocal, OnlyServer };
        enum SortOption { Updated, Name, Language_Name, Language_Updated };

        [SkinControlAttribute(50)]
        protected GUIListControl GUI_infoList = null;
        [SkinControlAttribute(503)]
        protected GUISelectButtonControl GUI_btnFilterState = null;
        [SkinControlAttribute(504)]
        protected GUISelectButtonControl GUI_btnSort = null;
        [SkinControlAttribute(506)]
        protected GUISelectButtonControl GUI_btnFilterCreator = null;
        [SkinControlAttribute(507)]
        protected GUISelectButtonControl GUI_btnFilterLang = null;
        [SkinControlAttribute(508)]
        protected GUIButtonControl GUI_btnAutoUpdate = null;

        string defaultLabelBtnSort;
        string defaultLabelBtnFilterState;
        string defaultLabelBtnFilterCreator;
        string defaultLabelBtnFilterLang;
		bool newDllsDownloaded = false;
		bool newDataSaved = false;

        /// <summary>remember what site is selected while showing reports, so when going back we can select that site again</summary>
        string selectedSite;

        public override int GetID
        {
            get { return WindowId; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideosUpdater.xml");
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", " "); GUIPropertyManager.SetProperty("#OnlineVideos.owner", string.Empty);
            return result;
        }

        public override string GetModuleName()
        {
			return PluginConfiguration.Instance.BasicHomeScreenName + ": " + Translation.Instance.ManageSites;
        }

		protected override void OnPageLoad()
		{
			base.OnPageLoad();

			defaultLabelBtnSort = GUIPropertyManager.Parse(GUI_btnSort.Label);
			defaultLabelBtnFilterState = GUIPropertyManager.Parse(GUI_btnFilterState.Label);
			defaultLabelBtnFilterCreator = GUIPropertyManager.Parse(GUI_btnFilterCreator.Label);
			defaultLabelBtnFilterLang = GUIPropertyManager.Parse(GUI_btnFilterLang.Label);

			if (GUI_btnFilterState.SubItemCount == 0)
			{
				foreach (string aFilterOption in Enum.GetNames(typeof(FilterStateOption)))
				{
					GUIControl.AddItemLabelControl(GetID, GUI_btnFilterState.GetID, Translation.Instance.GetByName(aFilterOption));
				}
			}
			if (GUI_btnSort.SubItemCount == 0)
			{
				foreach (string aSortOption in Enum.GetNames(typeof(SortOption)))
				{
					string[] singled = aSortOption.Split('_');
					for (int i = 0; i < singled.Length; i++) singled[i] = Translation.Instance.GetByName(singled[i]);
					GUIControl.AddItemLabelControl(GetID, GUI_btnSort.GetID, string.Join(", ", singled));
				}
			}
			SetFilterButtonOptions();

			GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel",
										   PluginConfiguration.Instance.BasicHomeScreenName + ": " + Translation.Instance.ManageSites);
			GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImage",
										   SiteImageExistenceCache.GetImageForSite("OnlineVideos"));

			GUIOnlineVideos ovGuiInstance = (GUIOnlineVideos)GUIWindowManager.GetWindow(GUIOnlineVideos.WindowId);
			if (ovGuiInstance != null && ovGuiInstance.SelectedSite != null && ovGuiInstance.CurrentState != GUIOnlineVideos.State.sites)
			{
				selectedSite = ovGuiInstance.SelectedSite.Settings.Name;
			}

			RefreshDisplayedOnlineSites();
		}

		public override bool OnMessage(GUIMessage message)
		{
			switch (message.Message)
			{
				case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
					if (newDllsDownloaded)
					{
						newDllsDownloaded = false;
						ReloadDownloadedDlls();
						PluginConfiguration.Instance.BuildAutomaticSitesGroups();
					}
					else if (newDataSaved)
					{
						newDataSaved = false;
						OnlineVideoSettings.Instance.BuildSiteUtilsList();
						PluginConfiguration.Instance.BuildAutomaticSitesGroups();
						// reset the GuiOnlineVideos instance
						GUIOnlineVideos ovGuiInstance = (GUIOnlineVideos)GUIWindowManager.GetWindow(GUIOnlineVideos.WindowId);
						if (ovGuiInstance != null) ovGuiInstance.ResetToFirstView();
					}
					break;
			}
			return base.OnMessage(message);
		}

		void RefreshDisplayedOnlineSites()
		{
			RefreshDisplayedOnlineSites(-1);
		}

        void RefreshDisplayedOnlineSites(int indexToSelect)
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
				return Sites.Updater.GetRemoteOverviews();
            },
            delegate(bool success, object result)
            {
				DisplayOnlineSites(success && (bool)result, indexToSelect);
            }
			, Translation.Instance.RetrievingRemoteSites + "/" + Translation.Instance.RetrievingRemoteDlls, true);
        }

        void DisplayOnlineSites(bool newDataRetrieved, int indexToSelect)
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", String.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);

            if (newDataRetrieved) SetFilterButtonOptions();

			if (Sites.Updater.OnlineSites == null || Sites.Updater.OnlineSites.Length == 0) return;

            GUIListItem selectedItem = GUI_infoList.SelectedListItem;

            GUIControl.ClearControl(GetID, GUI_infoList.GetID);

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
                    GUIListItem loListItem = new GUIListItem(site.Name);
                    loListItem.TVTag = site;
                    loListItem.Label2 = site.Language;
                    loListItem.Label3 = site.LastUpdated.ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale);
					string image = SiteImageExistenceCache.GetImageForSite(site.Name, "", "Icon", false);
                    if (!string.IsNullOrEmpty(image)) { loListItem.IconImage = image; loListItem.ThumbnailImage = image; }
                    if (!string.IsNullOrEmpty(site.Owner_FK)) loListItem.PinImage = GUIGraphicsContext.Skin + @"\Media\OnlineVideos\" + site.State.ToString() + ".png";
                    loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnSiteSelected);
					loListItem.IsPlayed = localSitesDic.ContainsKey(site.Name);// GetLocalSite(site.Name) != -1;
                    GUI_infoList.Add(loListItem);
					if ((selectedItem != null && selectedItem.Label == loListItem.Label) ||
						selectedSite == loListItem.Label ||
						(indexToSelect > -1 && GUI_infoList.Count - 1 == indexToSelect))
					{
						GUI_infoList.SelectedListItemIndex = GUI_infoList.Count - 1;
					}
                }
            }

            if (GUI_infoList.Count > 0) GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, GUI_infoList.SelectedListItemIndex);

            selectedSite = null;

            //set object count and type labels
            GUIPropertyManager.SetProperty("#itemcount", GUI_infoList.Count.ToString());
			GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Sites);
        }

        private void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            OnlineVideosWebservice.Site site = item.TVTag as OnlineVideosWebservice.Site;
            if (site != null)
            {
                if (!string.IsNullOrEmpty(site.Owner_FK)) GUIPropertyManager.SetProperty("#OnlineVideos.owner", site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')));
                else GUIPropertyManager.SetProperty("#OnlineVideos.owner", string.Empty);
                if (!string.IsNullOrEmpty(site.Description)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", site.Description);
                else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
            }
        }

        private void OnReportSelected(GUIListItem item, GUIControl parent)
        {
            OnlineVideosWebservice.Report report = item.TVTag as OnlineVideosWebservice.Report;
            if (report != null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.owner", string.Empty);
                if (!string.IsNullOrEmpty(report.Message)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", report.Message);
                else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
            }
        }

        public override void OnAction(Action action)
        {
            if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU)
            {
                if (GUI_infoList.ListItems.Count > 0 && GUI_infoList.ListItems[0].TVTag is OnlineVideosWebservice.Report) 
                { 
                    RefreshDisplayedOnlineSites(); 
                    return; 
                }
            }
            GUI_btnSort.Label = defaultLabelBtnSort;
            GUI_btnFilterState.Label = defaultLabelBtnFilterState;
            GUI_btnFilterCreator.Label = defaultLabelBtnFilterCreator;
            GUI_btnFilterLang.Label = defaultLabelBtnFilterLang;
            base.OnAction(action);
        }

        protected override void OnShowContextMenu()
        {
            if (GUI_infoList.Focus && GUI_infoList.SelectedListItem.TVTag is OnlineVideosWebservice.Site)
            {
                ShowOptionsForSite(GUI_infoList.SelectedListItem.TVTag as OnlineVideosWebservice.Site);
            }
            else
            {
                base.OnShowContextMenu();
            }
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. online sites retrieval to finish

            if (control == GUI_btnSort)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnSort.GetID, GUI_btnSort.SelectedItem);
                RefreshDisplayedOnlineSites();
            }
            else if (control == GUI_btnFilterState)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterState.GetID, GUI_btnFilterState.SelectedItem);
                RefreshDisplayedOnlineSites();
            }
            else if (control == GUI_btnFilterCreator)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterCreator.GetID, GUI_btnFilterCreator.SelectedItem);
                RefreshDisplayedOnlineSites();
            }
            else if (control == GUI_btnFilterLang)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterLang.GetID, GUI_btnFilterLang.SelectedItem);
                RefreshDisplayedOnlineSites();
            }
            else if (control == GUI_infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                if (GUI_infoList.SelectedListItem.TVTag is OnlineVideosWebservice.Site)
                {
                    ShowOptionsForSite(GUI_infoList.SelectedListItem.TVTag as OnlineVideosWebservice.Site);
                }
            }
            else if (control == GUI_btnAutoUpdate)
            {
				if (CheckOnlineVideosVersion())
				{
					Log.Instance.Info("SiteManager: Running AutoUpdate");
					GUIDialogProgress dlgPrgrs = PrepareProgressDialog(Translation.Instance.AutomaticUpdate);
					new System.Threading.Thread(delegate()
					{
						bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites((m, p) =>
						{
							if (dlgPrgrs != null)
							{
								if (!string.IsNullOrEmpty(m)) dlgPrgrs.SetLine(1, m);
								if (p != null) dlgPrgrs.SetPercentage(p.Value);
								return dlgPrgrs.ShouldRenderLayer();
							}
							else return true;
						});
						if (updateResult == true) newDllsDownloaded = true;
						else if (updateResult == null) newDataSaved = true;
						if (updateResult != false) SiteImageExistenceCache.ClearCache();
						if (dlgPrgrs != null) dlgPrgrs.Close();
						GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { RefreshDisplayedOnlineSites(); return 0; }, 0, 0, null);
					}) { Name = "OVAutoUpdate", IsBackground = true }.Start();
				}
            }

            base.OnClicked(controlId, control, actionType);
        }

		static GUIDialogProgress PrepareProgressDialog(string header)
		{
			GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
			if (dlgPrgrs != null)
			{
				dlgPrgrs.Reset();
				dlgPrgrs.DisplayProgressBar = true;
				dlgPrgrs.ShowWaitCursor = false;
				dlgPrgrs.DisableCancel(true);
				dlgPrgrs.SetHeading(string.Format("{0} - {1}", PluginConfiguration.Instance.BasicHomeScreenName, header));
				dlgPrgrs.StartModal(GUIWindowManager.ActiveWindow);
			}
			return dlgPrgrs;
		}

        void ShowOptionsForSite(OnlineVideosWebservice.Site site)
        {
			SiteSettings localSite = null;
			int localSiteIndex = OnlineVideoSettings.Instance.GetSiteByName(site.Name, out localSite);

            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlgSel.ShowQuickNumbers = false;
            if (dlgSel != null)
            {
                dlgSel.Reset();
				dlgSel.SetHeading(Translation.Instance.Actions);

				if (localSiteIndex == -1)
                {
					if (site.State != OnlineVideosWebservice.SiteState.Broken) dlgSel.Add(Translation.Instance.AddToMySites);
                }
                else
                {
					if ((site.LastUpdated - localSite.LastUpdated).TotalMinutes > 2 && site.State != OnlineVideosWebservice.SiteState.Broken)
                    {
						dlgSel.Add(Translation.Instance.UpdateMySite);
						dlgSel.Add(Translation.Instance.UpdateMySiteSkipCategories);
                    }
					dlgSel.Add(Translation.Instance.RemoveFromMySites);
                }

                if (GUI_infoList.Count > 1)
                {
					dlgSel.Add(Translation.Instance.RemoveAllFromMySites);
					dlgSel.Add(Translation.Instance.UpdateAll);
					dlgSel.Add(Translation.Instance.UpdateAllSkipCategories);
                }

                if (!string.IsNullOrEmpty(site.Owner_FK) && localSiteIndex >= 0) // !only local && ! only global
                {
                    dlgSel.Add(Translation.Instance.ShowReports);
                    if (site.State != OnlineVideosWebservice.SiteState.Broken) dlgSel.Add(Translation.Instance.ReportBroken);
                }
            }
            dlgSel.DoModal(GUIWindowManager.ActiveWindow);
            if (dlgSel.SelectedId == -1) return; // ESC used, nothing selected
			if (dlgSel.SelectedLabelText == Translation.Instance.AddToMySites || 
				dlgSel.SelectedLabelText == Translation.Instance.UpdateMySite || 
				dlgSel.SelectedLabelText == Translation.Instance.UpdateMySiteSkipCategories)
            {
				if (CheckOnlineVideosVersion())
				{
					Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
						() =>
						{
							bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites(null, new List<OnlineVideosWebservice.Site> { site }, false,
								dlgSel.SelectedLabelText == Translation.Instance.UpdateMySiteSkipCategories);
							if (updateResult == true) newDllsDownloaded = true;
							else if (updateResult == null) newDataSaved = true;
							return updateResult != false;
						},
						(success, result) =>
						{
							if (success && (bool)result)
							{
								SiteImageExistenceCache.UnCacheImageForSite(site.Name);
								RefreshDisplayedOnlineSites();
							}
						},
						Translation.Instance.GettingSiteXml, true);
				}
            }
			else if (dlgSel.SelectedLabelText == Translation.Instance.UpdateAll ||
					 dlgSel.SelectedLabelText == Translation.Instance.UpdateAllSkipCategories)
            {
				if (CheckOnlineVideosVersion())
				{
					GUIDialogProgress dlgPrgrs = PrepareProgressDialog(Translation.Instance.FullUpdate);
					new System.Threading.Thread(delegate()
					{
						bool? updateResult = OnlineVideos.Sites.Updater.UpdateSites((m, p) =>
						{
							if (dlgPrgrs != null)
							{
								if (!string.IsNullOrEmpty(m)) dlgPrgrs.SetLine(1, m);
								if (p != null) dlgPrgrs.SetPercentage(p.Value);
								return dlgPrgrs.ShouldRenderLayer();
							}
							else return true;
						}, GUI_infoList.ListItems.Select(g => g.TVTag as OnlineVideosWebservice.Site).ToList(), dlgSel.SelectedLabelText == Translation.Instance.UpdateAllSkipCategories);
						if (updateResult == true) newDllsDownloaded = true;
						else if (updateResult == null) newDataSaved = true;
						if (updateResult != false) SiteImageExistenceCache.ClearCache();
						if (dlgPrgrs != null) dlgPrgrs.Close();
						GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { RefreshDisplayedOnlineSites(); return 0; }, 0, 0, null);
					}) { Name = "OVSelectUpdate", IsBackground = true }.Start();
				}
            }
			else if (dlgSel.SelectedLabelText == Translation.Instance.RemoveFromMySites)
            {
				OnlineVideoSettings.Instance.RemoveSiteAt(localSiteIndex);
                OnlineVideoSettings.Instance.SaveSites();
                newDataSaved = true;
				RefreshDisplayedOnlineSites(GUI_infoList.SelectedListItemIndex);
            }
			else if (dlgSel.SelectedLabelText == Translation.Instance.RemoveAllFromMySites)
			{
				bool needRefresh = false;
				foreach (var siteToRemove in GUI_infoList.ListItems.Where(g => g.IsPlayed).Select(g => g.TVTag as OnlineVideosWebservice.Site).ToList())
				{
					localSiteIndex = OnlineVideoSettings.Instance.GetSiteByName(siteToRemove.Name, out localSite);
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
					RefreshDisplayedOnlineSites();
				}
			}
			else if (dlgSel.SelectedLabelText == Translation.Instance.ShowReports)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					() =>
					{
						OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
						return ws.GetReports(site.Name);
					},
					(success, result) =>
					{
						if (success)
						{
							OnlineVideosWebservice.Report[] reports = result as OnlineVideosWebservice.Report[];

							if (reports == null || reports.Length == 0)
							{
								GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
								if (dlg != null)
								{
									dlg.Reset();
									dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
									dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
									dlg.SetText(Translation.Instance.NoReportsForSite);
									dlg.DoModal(GUIWindowManager.ActiveWindow);
								}
							}
							else
							{
								selectedSite = site.Name;
								GUIControl.ClearControl(GetID, GUI_infoList.GetID);

								Array.Sort(reports, new Comparison<OnlineVideosWebservice.Report>(delegate(OnlineVideosWebservice.Report a, OnlineVideosWebservice.Report b)
								{
									return b.Date.CompareTo(a.Date);
								}));

								foreach (OnlineVideosWebservice.Report report in reports)
								{
									string shortMsg = report.Message.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
									GUIListItem loListItem = new GUIListItem(shortMsg.Length > 44 ? shortMsg.Substring(0, 40) + " ..." : shortMsg);
									loListItem.TVTag = report;
									loListItem.Label2 = report.Type.ToString();
									loListItem.Label3 = report.Date.ToString("g", OnlineVideoSettings.Instance.Locale);
									loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnReportSelected);
									GUI_infoList.Add(loListItem);
								}
								GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, 0);
								GUIPropertyManager.SetProperty("#itemcount", GUI_infoList.Count.ToString());
								GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Reports);
							}
						}
					}, Translation.Instance.GettingReports, true);
			}
			else if (dlgSel.SelectedLabelText == Translation.Instance.ReportBroken)
			{
				if (CheckOnlineVideosVersion())
				{
					if ((site.LastUpdated - localSite.LastUpdated).TotalMinutes > 1)
					{
						GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
						if (dlg != null)
						{
							dlg.Reset();
							dlg.SetHeading(site.Name);
							dlg.SetLine(1, Translation.Instance.PleaseUpdateLocalSite);
							dlg.DoModal(GUIWindowManager.ActiveWindow);
						}
					}
					else
					{
						string userReason = "";
						if (GUIOnlineVideos.GetUserInputString(ref userReason, false))
						{
							if (userReason.Length < 15)
							{
								GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
								if (dlg != null)
								{
									dlg.Reset();
									dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
									dlg.SetLine(1, Translation.Instance.PleaseEnterDescription);
									dlg.DoModal(GUIWindowManager.ActiveWindow);
								}
							}
							else
							{
								OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
								string message = "";
								bool success = ws.SubmitReport(site.Name, userReason, OnlineVideosWebservice.ReportType.Broken, out message);
								GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
								if (dlg != null)
								{
									dlg.Reset();
									dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
									dlg.SetLine(1, success ? Translation.Instance.Done : Translation.Instance.Error);
									dlg.SetLine(2, message);
									dlg.DoModal(GUIWindowManager.ActiveWindow);
								}
								if (success)
								{
									// reload online sites
									OnlineVideos.Sites.Updater.GetRemoteOverviews(true);
									RefreshDisplayedOnlineSites();
								}
							}
						}
					}
				}
			}
        }
		
		internal static void ReloadDownloadedDlls()
		{
			Log.Instance.Info("Reloading SiteUtil Dlls at runtime.");
			bool stopPlayback = (MediaPortal.Player.g_Player.Player != null && MediaPortal.Player.g_Player.Player.GetType().Assembly == typeof(GUISiteUpdater).Assembly);
			bool stopDownload = DownloadManager.Instance.Count > 0;
			if (stopDownload || stopPlayback)
			{
				GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
				if (dlg != null)
				{
					dlg.Reset();
					dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
					dlg.SetLine(1, Translation.Instance.NewDllDownloaded);
					int i = 1;
					if (stopDownload) dlg.SetLine(i++, Translation.Instance.DownloadsWillBeAborted);
					if (stopPlayback) dlg.SetLine(i++, Translation.Instance.PlaybackWillBeStopped);
					dlg.DoModal(GUIWindowManager.ActiveWindow);
				}
			}
			// stop playback if an OnlineVideos video is playing
			if (stopPlayback) MediaPortal.Player.g_Player.Stop();
			// stop downloads
			DownloadManager.Instance.StopAll();
			// reset the GuiOnlineVideos instance and stop the LatestVideos Thread
			GUIOnlineVideos ovGuiInstance = (GUIOnlineVideos)GUIWindowManager.GetWindow(GUIOnlineVideos.WindowId);
			if (ovGuiInstance != null)
			{
				ovGuiInstance.ResetToFirstView();
				ovGuiInstance.LatestVideosManager.Stop();
			}
			// now reload the appdomain
			OnlineVideoSettings.Reload();
			TranslationLoader.SetTranslationsToSingleton();
			OnlineVideoSettings.Instance.BuildSiteUtilsList();
			GC.Collect();
			GC.WaitForFullGCComplete();
			// restart the LatestVideos thread
			ovGuiInstance.LatestVideosManager.Start();
		}

        bool SitePassesFilter(OnlineVideosWebservice.Site site)
        {
            // language
			if (GUI_btnFilterLang.SelectedLabel != Translation.Instance.All && site.Language != GUI_btnFilterLang.SelectedLabel) return false;
            // owner
			if (GUI_btnFilterCreator.SelectedLabel != Translation.Instance.All)
            {
                string owner = site.Owner_FK != null ? site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')) : "";
                if (owner != GUI_btnFilterCreator.SelectedLabel) return false;
            }
            // state
            FilterStateOption fo = (FilterStateOption)GUI_btnFilterState.SelectedItem;
            switch (fo)
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
            SortOption so = (SortOption)GUI_btnSort.SelectedItem;
            switch (so)
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

        void SetFilterButtonOptions()
        {
			if (Sites.Updater.OnlineSites == null || Sites.Updater.OnlineSites.Length == 0) return;

            // get a sorted list of all site owners and languages to display in filter buttons
            Dictionary<string, bool> creatorsHash = new Dictionary<string, bool>();
            Dictionary<string, bool> languagesHash = new Dictionary<string, bool>();
			foreach (OnlineVideosWebservice.Site site in Sites.Updater.OnlineSites)
            {
                string owner = site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@'));
                bool temp;
                if (!creatorsHash.TryGetValue(owner, out temp)) creatorsHash.Add(owner, true);
                if (!string.IsNullOrEmpty(site.Language))
                {
                    if (!languagesHash.TryGetValue(site.Language, out temp)) languagesHash.Add(site.Language, true);
                }
            }
            string[] creators = new string[creatorsHash.Count];
            creatorsHash.Keys.CopyTo(creators, 0);
            Array.Sort(creators);
            if (GUI_btnFilterCreator != null)
            {
                GUI_btnFilterCreator.Clear();
				GUIControl.AddItemLabelControl(GetID, GUI_btnFilterCreator.GetID, Translation.Instance.All);
                foreach (string creator in creators) GUIControl.AddItemLabelControl(GetID, GUI_btnFilterCreator.GetID, creator);
            }
            string[] languages = new string[languagesHash.Count];
            languagesHash.Keys.CopyTo(languages, 0);
            Array.Sort(languages);
            if (GUI_btnFilterLang != null)
            {
                GUI_btnFilterLang.Clear();
				GUIControl.AddItemLabelControl(GetID, GUI_btnFilterLang.GetID, Translation.Instance.All);
                foreach (string language in languages) GUIControl.AddItemLabelControl(GetID, GUI_btnFilterLang.GetID, language);
            }
        }

        /// <summary>
        /// Checks if the local Version is at least equal to the latest online available version and presents a message if not.
        /// </summary>
        /// <returns>true if the local version is equal or higher than the online version, otherwise false.</returns>
        bool CheckOnlineVideosVersion()
        {
			if (!Sites.Updater.VersionCompatible)
            {
                GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
					dlg.SetLine(1, Translation.Instance.AutomaticUpdateDisabled);
					dlg.SetLine(2, string.Format(Translation.Instance.LatestVersionRequired, Sites.Updater.VersionOnline.ToString()));
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return false;
            }
            return true;
        }
    }
}
