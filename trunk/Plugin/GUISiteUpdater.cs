using System;
using System.Collections.Generic;
using System.Diagnostics;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Configuration;

namespace OnlineVideos
{
    public class GUISiteUpdater : GUIWindow
    {
        public const int WindowId = 4757;

        enum FilterStateOption { All, Reported, Broken, Working, Updatable };
        enum SortOption { Updated, Name, Language_Name, Language_Updated };

        [SkinControlAttribute(50)]
        protected GUIListControl GUI_infoList = null;
        [SkinControlAttribute(502)]
        protected GUIButtonControl GUI_btnUpdate = null;
        [SkinControlAttribute(503)]
        protected GUISelectButtonControl GUI_btnFilterState = null;
        [SkinControlAttribute(504)]
        protected GUISelectButtonControl GUI_btnSort = null;
        [SkinControlAttribute(505)]
        protected GUIButtonControl GUI_btnFullUpdate = null;
        [SkinControlAttribute(506)]
        protected GUISelectButtonControl GUI_btnFilterCreator = null;
        [SkinControlAttribute(507)]
        protected GUISelectButtonControl GUI_btnFilterLang = null;

        string defaultLabelBtnSort;
        string defaultLabelBtnFilterState;
        string defaultLabelBtnFilterCreator;
        string defaultLabelBtnFilterLang;

        OnlineVideosWebservice.Dll[] onlineDlls = null;
        OnlineVideosWebservice.Site[] onlineSites = null;        
        DateTime lastOverviewsRetrieved = DateTime.MinValue;
        Version versionDll = new System.Reflection.AssemblyName(System.Reflection.Assembly.GetExecutingAssembly().FullName).Version;
        Version versionOnline = null;
        DateTime lastOnlineVersionRetrieved = DateTime.MinValue;

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
            return OnlineVideoSettings.Instance.BasicHomeScreenName + ": " + Translation.ManageSites;
        }

        protected override void OnPageLoad()
        {
            Translation.TranslateSkin();

            base.OnPageLoad();

            defaultLabelBtnSort = GUIPropertyManager.Parse(GUI_btnSort.Label);
            defaultLabelBtnFilterState = GUIPropertyManager.Parse(GUI_btnFilterState.Label);
            defaultLabelBtnFilterCreator = GUIPropertyManager.Parse(GUI_btnFilterCreator.Label);
            defaultLabelBtnFilterLang = GUIPropertyManager.Parse(GUI_btnFilterLang.Label);

            if (GUI_btnFilterState.SubItemCount == 0)
            {
                foreach (string aFilterOption in Enum.GetNames(typeof(FilterStateOption)))
                {
                    GUIControl.AddItemLabelControl(GetID, GUI_btnFilterState.GetID, Translation.Strings[aFilterOption]);
                }
            }
            if (GUI_btnSort.SubItemCount == 0)
            {
                foreach (string aSortOption in Enum.GetNames(typeof(SortOption)))
                {
                    string[] singled = aSortOption.Split('_');
                    for(int i = 0; i<singled.Length;i++) singled[i] = Translation.Strings[singled[i]];                    
                    GUIControl.AddItemLabelControl(GetID, GUI_btnSort.GetID, string.Join(", ", singled));
                }
            }
            SetFilterButtonOptions();

            GUIPropertyManager.SetProperty("#header.label",
                                           OnlineVideoSettings.Instance.BasicHomeScreenName + ": " + Translation.ManageSites);
            GUIPropertyManager.SetProperty("#header.image",
                                           Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\OnlineVideos.png");

            DisplayOnlineSites();            
        }

        void DisplayOnlineSites()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", String.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);

            if (GetRemoteOverviews()) SetFilterButtonOptions();

            if (onlineSites == null || onlineSites.Length == 0) return;

            GUIControl.ClearControl(GetID, GUI_infoList.GetID);

            List<OnlineVideosWebservice.Site> filteredsortedSites = new List<OnlineVideos.OnlineVideosWebservice.Site>(onlineSites);
            filteredsortedSites = filteredsortedSites.FindAll(SitePassesFilter);
            filteredsortedSites.Sort(CompareSiteForSort);

            foreach (OnlineVideosWebservice.Site site in filteredsortedSites)
            {
                if (!site.IsAdult || !OnlineVideoSettings.Instance.useAgeConfirmation || OnlineVideoSettings.Instance.ageHasBeenConfirmed)
                {
                    GUIListItem loListItem = new GUIListItem(site.Name);
                    loListItem.TVTag = site;
                    loListItem.Label2 = site.Language;
                    loListItem.Label3 = site.LastUpdated.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                    string image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + site.Name + ".png";
                    if (System.IO.File.Exists(image)) { loListItem.IconImage = image; loListItem.ThumbnailImage = image; }
                    loListItem.PinImage = GUIGraphicsContext.Skin + @"\Media\OnlineVideos\" + site.State.ToString() + ".png";
                    loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnSiteSelected);
                    loListItem.IsPlayed = GetLocalSite(site.Name) != -1;
                    GUI_infoList.Add(loListItem);
                }
            }

            if (GUI_infoList.Count > 0) GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, 0);

            //set object count label
            GUIPropertyManager.SetProperty("#itemcount", MediaPortal.Util.Utils.GetObjectCountLabel(GUI_infoList.Count));
        }

        private void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            OnlineVideosWebservice.Site site = item.TVTag as OnlineVideosWebservice.Site;
            if (site != null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.owner", site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@')));
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
                    DisplayOnlineSites(); 
                    return; 
                }
            }
            GUI_btnSort.Label = defaultLabelBtnSort;
            GUI_btnFilterState.Label = defaultLabelBtnFilterState;
            GUI_btnFilterCreator.Label = defaultLabelBtnFilterCreator;
            GUI_btnFilterLang.Label = defaultLabelBtnFilterLang;
            base.OnAction(action);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (control == GUI_btnUpdate)
            {
                DisplayOnlineSites();
                GUIControl.FocusControl(GetID, GUI_infoList.GetID);
            }
            else if (control == GUI_btnSort)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnSort.GetID, GUI_btnSort.SelectedItem);
            }
            else if (control == GUI_btnFilterState)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterState.GetID, GUI_btnFilterState.SelectedItem);
            }
            else if (control == GUI_btnFilterCreator)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterCreator.GetID, GUI_btnFilterCreator.SelectedItem);
            }
            else if (control == GUI_btnFilterLang)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilterLang.GetID, GUI_btnFilterLang.SelectedItem);
            }
            else if (control == GUI_infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                if (GUI_infoList.SelectedListItem.TVTag is OnlineVideosWebservice.Site)
                {
                    ShowOptionsForSite(GUI_infoList.SelectedListItem.TVTag as OnlineVideosWebservice.Site);
                }
            }
            else if (control == GUI_btnFullUpdate)
            {
                FullUpdate();
            }

            base.OnClicked(controlId, control, actionType);
        }

        void ShowOptionsForSite(OnlineVideosWebservice.Site site)
        {
            int localSiteIndex = GetLocalSite(site.Name);

            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlgSel.ShowQuickNumbers = false;
            if (dlgSel != null)
            {
                dlgSel.Reset();
                dlgSel.SetHeading(Translation.Actions);

                if (localSiteIndex == -1)
                {
                    dlgSel.Add(Translation.AddToMySites);
                }
                else
                {
                    if ((site.LastUpdated - OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].LastUpdated).TotalMinutes > 2)
                    {
                        dlgSel.Add(Translation.UpdateMySite);
                        dlgSel.Add(Translation.UpdateMySiteSkipCategories);                        
                    }
                    dlgSel.Add(Translation.RemoveFromMySites);
                }
                dlgSel.Add(Translation.ShowReports);
            }
            dlgSel.DoModal(GetID);
            if (dlgSel.SelectedId == -1) return; // ESC used, nothing selected
            if (dlgSel.SelectedLabelText == Translation.AddToMySites)
            {
                SiteSettings newSite = GetRemoteSite(site.Name);
                if (newSite != null)
                {
                    OnlineVideoSettings.Instance.SiteSettingsList.Add(newSite);
                    OnlineVideoSettings.Instance.SaveSites();
                    OnlineVideoSettings.Instance.BuildSiteList();
                    if (DownloadRequiredDllIfNeeded(site.RequiredDll)) ShowRestartMessage();
                }
            }
            else if (dlgSel.SelectedLabelText == Translation.UpdateMySite)
            {
                SiteSettings site2Update = GetRemoteSite(site.Name);
                if (site2Update != null)
                {
                    OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex] = site2Update;
                    OnlineVideoSettings.Instance.SaveSites();
                    OnlineVideoSettings.Instance.BuildSiteList();
                    if (DownloadRequiredDllIfNeeded(site.RequiredDll)) ShowRestartMessage();
                }
            }
            else if (dlgSel.SelectedLabelText == Translation.UpdateMySiteSkipCategories)
            {
                SiteSettings site2Update = GetRemoteSite(site.Name);
                if (site2Update != null)
                {
                    site2Update.Categories = OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].Categories;
                    OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex] = site2Update;
                    OnlineVideoSettings.Instance.SaveSites();
                    OnlineVideoSettings.Instance.BuildSiteList();
                    if (DownloadRequiredDllIfNeeded(site.RequiredDll)) ShowRestartMessage();    
                }
            }
            else if (dlgSel.SelectedLabelText == Translation.RemoveFromMySites)
            {
                OnlineVideoSettings.Instance.SiteSettingsList.RemoveAt(localSiteIndex);
                OnlineVideoSettings.Instance.SaveSites();
                OnlineVideoSettings.Instance.BuildSiteList();
            }
            else if (dlgSel.SelectedLabelText == Translation.ShowReports)
            {
                OnlineVideosWebservice.Report[] reports = null;
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
                    reports = ws.GetReports(site.Name);
                }, Translation.GettingReports))
                {
                    if (reports == null || reports.Length == 0)
                    {
                        GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                        if (dlg != null)
                        {
                            dlg.Reset();
                            dlg.SetHeading(OnlineVideoSettings.Instance.BasicHomeScreenName);
                            dlg.SetText(Translation.NoReportsForSite);
                            dlg.DoModal(GUIWindowManager.ActiveWindow);
                        }
                    }
                    else
                    {
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
                            loListItem.Label3 = report.Date.ToString("g", OnlineVideoSettings.Instance.MediaPortalLocale);
                            loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnReportSelected);
                            GUI_infoList.Add(loListItem);
                        }
                        GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, 0);
                        GUIPropertyManager.SetProperty("#itemcount", MediaPortal.Util.Utils.GetObjectCountLabel(GUI_infoList.Count));
                    }
                }
            }
        }

        bool DownloadRequiredDllIfNeeded(string dllName)
        {
            if (!string.IsNullOrEmpty(dllName))
            {
                foreach (OnlineVideosWebservice.Dll anOnlineDll in onlineDlls)
                {
                    if (anOnlineDll.Name == dllName)
                    {
                        // target directory for dlls
                        string dllDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
                        // target directory for dlls (temp) (if exists, delete and recreate)
                        string dllTempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OnlineVideos\\");
                        if (System.IO.Directory.Exists(dllTempDir)) System.IO.Directory.Delete(dllTempDir, true);
                        System.IO.Directory.CreateDirectory(dllTempDir);
                        // update or download dll if needed
                        string location = dllDir + anOnlineDll.Name + ".dll";
                        if (System.IO.File.Exists(location))
                        {
                            byte[] data = null;
                            data = System.IO.File.ReadAllBytes(location);
                            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                            string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                            if (md5LocalDll != anOnlineDll.MD5)
                            {
                                // download dll to temp dir first
                                if (DownloadDll(anOnlineDll.Name, dllTempDir + anOnlineDll.Name + ".dll"))
                                {
                                    // if download was successfull, try to copy to target dir (if not successfull, do UAC prompted copy)
                                    try { System.IO.File.Copy(dllTempDir + anOnlineDll.Name + ".dll", location, true); }
                                    catch { CopyDlls(dllTempDir, dllDir); }
                                    return true;
                                }                                
                            }
                        }
                        break;   
                    }
                }
            }
            return false;       
        }

        void FullUpdate()
        {
            GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
            try
            {
                if (dlgPrgrs != null)
                {
                    dlgPrgrs.Reset();
                    dlgPrgrs.DisplayProgressBar = true;
                    dlgPrgrs.ShowWaitCursor = false;
                    dlgPrgrs.DisableCancel(true);
                    dlgPrgrs.SetHeading(string.Format("{0} - {1}", OnlineVideoSettings.Instance.BasicHomeScreenName, Translation.FullUpdate));
                    dlgPrgrs.StartModal(GetID);
                }

                if (onlineSites == null) return;

                Dictionary<string, bool> requiredDlls = new Dictionary<string, bool>();

                // update or add all sites, so the user has everything the server currently has
                for (int i = 0; i < onlineSites.Length; i++)
                {
                    OnlineVideosWebservice.Site onlineSite = onlineSites[i];
                    if (dlgPrgrs != null) dlgPrgrs.SetLine(1, onlineSite.Name);
                    if (!string.IsNullOrEmpty(onlineSite.RequiredDll)) requiredDlls[onlineSite.RequiredDll] = true;
                    int localSiteIndex = GetLocalSite(onlineSite.Name);
                    if (localSiteIndex == -1)
                    {
                        // add
                        SiteSettings newSite = GetRemoteSite(onlineSite.Name);
                        if (newSite != null)
                        {
                            // disable local site if broken
                            if (onlineSite.State == OnlineVideos.OnlineVideosWebservice.SiteState.Broken) newSite.IsEnabled = false;
                            OnlineVideoSettings.Instance.SiteSettingsList.Add(newSite);
                        }
                    }
                    else if ((onlineSite.LastUpdated - OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].LastUpdated).TotalMinutes > 2)
                    {
                        // update
                        SiteSettings updatedSite = GetRemoteSite(onlineSite.Name);
                        if (updatedSite != null)
                        {
                            // disable local site if broken
                            if (onlineSite.State == OnlineVideos.OnlineVideosWebservice.SiteState.Broken) updatedSite.IsEnabled = false;
                            OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex] = updatedSite;
                        }
                    }
                    if (dlgPrgrs != null) dlgPrgrs.Percentage = (80 * (i + 1) / onlineSites.Length);
                }

                if (dlgPrgrs != null) dlgPrgrs.SetLine(1, Translation.SavingLocalSiteList);
                OnlineVideoSettings.Instance.SaveSites();
                OnlineVideoSettings.Instance.BuildSiteList();
                if (dlgPrgrs != null)
                {
                    dlgPrgrs.Percentage = 90;
                    if (dlgPrgrs != null) dlgPrgrs.SetLine(1, Translation.RetrievingRemoteDlls);
                }
                if (requiredDlls.Count > 0)
                {
                    // updated dlls can't be loaded here - tell the user to restart MediaPortal if new dlls were downloaded
                    bool showMessage = false;
                    // target directory for dlls
                    string dllDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
                    // target directory for dlls (temp) (if exists, delete and recreate)
                    string dllTempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OnlineVideos\\");
                    if (System.IO.Directory.Exists(dllTempDir)) System.IO.Directory.Delete(dllTempDir, true);
                    System.IO.Directory.CreateDirectory(dllTempDir);
                    int dllsToCopy = 0;
                    for (int i = 0; i < onlineDlls.Length; i++)
                    {
                        OnlineVideosWebservice.Dll anOnlineDll = onlineDlls[i];
                        if (dlgPrgrs != null) dlgPrgrs.SetLine(1, anOnlineDll.Name);
                        if (requiredDlls.ContainsKey(anOnlineDll.Name))
                        {
                            // update or download dll if needed
                            string location = dllDir + anOnlineDll.Name + ".dll";
                            bool download = true;
                            if (System.IO.File.Exists(location))
                            {
                                byte[] data = null;
                                data = System.IO.File.ReadAllBytes(location);
                                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                                string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                                if (md5LocalDll == anOnlineDll.MD5) download = false;
                            }
                            if (download)
                            {
                                // download dll to temp dir first
                                if (DownloadDll(anOnlineDll.Name, dllTempDir + anOnlineDll.Name + ".dll"))
                                {
                                    // if download was successfull, try to copy to target dir (if not successfull, mark for UAC prompted copy later)
                                    try { System.IO.File.Copy(dllTempDir + anOnlineDll.Name + ".dll", location, true); }
                                    catch { dllsToCopy++; }
                                    showMessage = true;
                                }
                            }
                        }
                        if (dlgPrgrs != null) dlgPrgrs.Percentage = 90 + (10 * (i + 1) / onlineDlls.Length);
                    }
                    if (dllsToCopy > 0) CopyDlls(dllTempDir, dllDir);
                    if (dlgPrgrs != null) { dlgPrgrs.Percentage = 100; dlgPrgrs.SetLine(1, Translation.Done); }
                    if (showMessage) ShowRestartMessage();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                if (dlgPrgrs != null) dlgPrgrs.Close();
            }            
        }

        public void AutoUpdate(bool dllsLoaded)
        {
            bool? hasLatestVersion = CheckOnlineVideosVersion();

            if (hasLatestVersion == false)
            {
                GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(OnlineVideoSettings.Instance.BasicHomeScreenName);
                    dlg.SetLine(1, Translation.AutomaticUpdateDisabled);
                    dlg.SetLine(2, string.Format(Translation.LatestVersionRequired, versionOnline.ToString()));
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
            try
            {
                if (dlgPrgrs != null)
                {
                    dlgPrgrs.Reset();
                    dlgPrgrs.DisplayProgressBar = true;
                    dlgPrgrs.ShowWaitCursor = false;
                    dlgPrgrs.DisableCancel(true);
                    dlgPrgrs.SetHeading(string.Format("{0} - {1}", OnlineVideoSettings.Instance.BasicHomeScreenName, Translation.AutomaticUpdate));
                    dlgPrgrs.SetLine(1, Translation.RetrievingRemoteSites);
                    dlgPrgrs.StartModal(GUIWindowManager.ActiveWindow);
                }

                GetRemoteOverviews();

                if (onlineSites == null) return;
                
                if (dlgPrgrs != null) dlgPrgrs.Percentage = 10;
                bool saveRequired = false;
                Dictionary<string, bool> requiredDlls = new Dictionary<string, bool>();
                for (int i = 0; i < OnlineVideoSettings.Instance.SiteSettingsList.Count; i++)
                {
                    SiteSettings localSite = OnlineVideoSettings.Instance.SiteSettingsList[i];
                    if (localSite.IsEnabled)
                    {
                        if (dlgPrgrs != null) dlgPrgrs.SetLine(1, localSite.Name);
                        OnlineVideosWebservice.Site remoteSite = Array.Find(onlineSites, delegate(OnlineVideosWebservice.Site site) { return site.Name == localSite.Name; });
                        if (remoteSite != null)
                        {
                            // remember what dlls are required and check for changed dlls later (regardless of lastUpdated on site)
                            if (!string.IsNullOrEmpty(remoteSite.RequiredDll)) requiredDlls[remoteSite.RequiredDll] = true;
                            // get site if updated on server
                            if ((remoteSite.LastUpdated - localSite.LastUpdated).TotalMinutes > 2)
                            {
                                SiteSettings updatedSite = GetRemoteSite(remoteSite.Name);
                                if (updatedSite != null)
                                {
                                    OnlineVideoSettings.Instance.SiteSettingsList[i] = updatedSite;
                                    saveRequired = true;
                                }
                            }
                        }
                    }
                    if (dlgPrgrs != null) dlgPrgrs.Percentage = 10 + (70 * (i + 1) / OnlineVideoSettings.Instance.SiteSettingsList.Count);
                }

                if (requiredDlls.Count > 0)
                {
                    if (dlgPrgrs != null) dlgPrgrs.SetLine(1, Translation.RetrievingRemoteDlls);                    

                    // target directory for dlls
                    string dllDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
                    // temp target directory for dlls (if exists, delete and recreate)
                    string dllTempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OnlineVideos\\");
                    if (System.IO.Directory.Exists(dllTempDir)) System.IO.Directory.Delete(dllTempDir, true);
                    System.IO.Directory.CreateDirectory(dllTempDir);
                    int dllsToCopy = 0;
                    for (int i = 0; i < onlineDlls.Length; i++)
                    {
                        OnlineVideosWebservice.Dll anOnlineDll = onlineDlls[i];
                        if (dlgPrgrs != null) dlgPrgrs.SetLine(1, anOnlineDll.Name);
                        if (requiredDlls.ContainsKey(anOnlineDll.Name))
                        {
                            // update or download dll if needed
                            string location = dllDir + anOnlineDll.Name + ".dll";
                            bool download = true;
                            if (System.IO.File.Exists(location))
                            {
                                byte[] data = null;
                                data = System.IO.File.ReadAllBytes(location);
                                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                                string md5LocalDll = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();
                                if (md5LocalDll == anOnlineDll.MD5) download = false;
                            }
                            if (download)
                            {
                                // download dll to temp dir first
                                if (DownloadDll(anOnlineDll.Name, dllTempDir + anOnlineDll.Name + ".dll"))
                                {
                                    // if download was successfull, try to copy to target dir (if not successfull, mark for UAC prompted copy later)
                                    try { System.IO.File.Copy(dllTempDir + anOnlineDll.Name + ".dll", location, true); }
                                    catch { dllsToCopy++; }
                                }
                            }
                        }
                        if (dlgPrgrs != null) dlgPrgrs.Percentage = 80 + (15 * (i + 1) / onlineDlls.Length);
                    }
                    if (dllsToCopy > 0)
                    {
                        CopyDlls(dllTempDir, dllDir);
                        if (dllsLoaded) ShowRestartMessage();
                    }
                }
                if (saveRequired)
                {
                    if (dlgPrgrs != null) dlgPrgrs.SetLine(1, Translation.SavingLocalSiteList);
                    OnlineVideoSettings.Instance.SaveSites();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                if (dlgPrgrs != null) { dlgPrgrs.Percentage = 100; dlgPrgrs.SetLine(1, Translation.Done); dlgPrgrs.Close(); }
            }
        }

        void ShowRestartMessage()
        {
            GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
            if (dlg != null)
            {
                dlg.Reset();
                dlg.SetHeading(OnlineVideoSettings.Instance.BasicHomeScreenName);
                dlg.SetLine(1, Translation.NewDllDownloaded);
                dlg.SetLine(2, Translation.RestartMediaPortal);
                dlg.DoModal(GUIWindowManager.ActiveWindow);
            }
        }

        bool SitePassesFilter(OnlineVideosWebservice.Site site)
        {
            // language
            if (GUI_btnFilterLang.SelectedLabel != Translation.All && site.Language != GUI_btnFilterLang.SelectedLabel) return false;
            // owner
            if (GUI_btnFilterCreator.SelectedLabel != Translation.All)
            {
                string owner = site.Owner_FK.Substring(0, site.Owner_FK.IndexOf('@'));
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
                    foreach (SiteSettings localSite in OnlineVideoSettings.Instance.SiteSettingsList)
                    {
                        if (localSite.Name == site.Name)
                        {
                            if ((site.LastUpdated - localSite.LastUpdated).TotalMinutes > 2) return true;
                            else return false;
                        }
                    }
                    return false;
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

        int GetLocalSite(string name)
        {
            for (int i = 0; i < OnlineVideoSettings.Instance.SiteSettingsList.Count; i++)
            {
                if (OnlineVideoSettings.Instance.SiteSettingsList[i].Name == name) return i;
            }
            return -1;
        }        

        void SetFilterButtonOptions()
        {
            if (onlineSites == null || onlineSites.Length == 0) return;

            // get a sorted list of all site owners and languages to display in filter buttons
            Dictionary<string, bool> creatorsHash = new Dictionary<string, bool>();
            Dictionary<string, bool> languagesHash = new Dictionary<string, bool>();
            foreach (OnlineVideosWebservice.Site site in onlineSites)
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
                GUIControl.AddItemLabelControl(GetID, GUI_btnFilterCreator.GetID, Translation.All);
                foreach (string creator in creators) GUIControl.AddItemLabelControl(GetID, GUI_btnFilterCreator.GetID, creator);
            }
            string[] languages = new string[languagesHash.Count];
            languagesHash.Keys.CopyTo(languages, 0);
            Array.Sort(languages);
            if (GUI_btnFilterLang != null)
            {
                GUI_btnFilterLang.Clear();
                GUIControl.AddItemLabelControl(GetID, GUI_btnFilterLang.GetID, Translation.All);
                foreach (string language in languages) GUIControl.AddItemLabelControl(GetID, GUI_btnFilterLang.GetID, language);
            }
        }

        bool GetRemoteOverviews()
        {
            if (DateTime.Now - lastOverviewsRetrieved > TimeSpan.FromMinutes(10)) // only get sites every 10 minutes
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
                
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    onlineSites = ws.GetSitesOverview();   
                    
                }, Translation.RetrievingRemoteSites))
                {
                    return false;
                }
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    onlineDlls = ws.GetDllsOverview();

                }, Translation.RetrievingRemoteDlls))
                {
                    return false;
                }
                lastOverviewsRetrieved = DateTime.Now;
                return true;
            }
            return false;
        }

        SiteSettings GetRemoteSite(string name)
        {
            string siteXml = "";
            OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                siteXml = ws.GetSiteXml(name);
            }, Translation.GettingSiteXml))
            {
                if (siteXml.Length > 0)
                {
                    IList<SiteSettings> sitesFromWeb = Utils.SiteSettingsFromXml(siteXml);
                    if (sitesFromWeb != null && sitesFromWeb.Count > 0)
                    {
                        DownloadImages(sitesFromWeb[0].Name, ws);                        
                        return sitesFromWeb[0];
                    }
                }
            }
            return null;
        }

        void DownloadImages(string siteName, OnlineVideosWebservice.OnlineVideosService ws)
        {
            try
            {
                byte[] icon = null;
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    icon = ws.GetSiteIcon(siteName);
                }, Translation.GettingIcon))
                {
                    if (icon != null && icon.Length > 0)
                    {
                        System.IO.File.WriteAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + siteName + ".png", icon);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                byte[] banner = null;
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    banner = ws.GetSiteBanner(siteName);
                }, Translation.GettingBanner))
                {
                    if (banner != null && banner.Length > 0)
                    {
                        System.IO.File.WriteAllBytes(Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + siteName + ".png", banner);
                    }
                }                
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        bool DownloadDll(string dllName, string localPath)
        {
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                byte[] onlineDllData = ws.GetDll(dllName);
                if (onlineDllData != null && onlineDllData.Length > 0) System.IO.File.WriteAllBytes(localPath, onlineDllData);
            }, Translation.GettingDll))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Downloads and retrieves the latest version of OnlineVides that has been published online, by checking update.xml in SVN.
        /// </summary>
        /// <returns>null if no onlineversion could be found, true if the local version is equal or higher than the online version, otherwise false.</returns>
        bool? CheckOnlineVideosVersion()
        {
            if (DateTime.Now - lastOnlineVersionRetrieved > TimeSpan.FromDays(1)) // only check for a new available version once a day
            {
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    string tempFile = System.IO.Path.GetTempFileName();
                    new System.Net.WebClient().DownloadFile("http://mp-onlinevideos2.googlecode.com/svn/trunk/MPEI/update.xml", tempFile);
                    versionOnline = new Version(MpeCore.Classes.ExtensionCollection.Load(tempFile).GetUniqueList().Items[0].GeneralInfo.Version.ToString());
                }, Translation.CheckingForPluginUpdate))
                {
                    return null;
                }
            }
            if (versionOnline == null) return null;
            return versionOnline <= versionDll;
        }

        void CopyDlls(string sourceDir, string targetDir)
        {
            // todo : maybe "mkdir" if target dir does not exist?
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c copy /B /V /Y \"" + sourceDir + "OnlineVideos.Sites.*.dll\" \"" + targetDir + "\"";
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.ErrorDialog = false;
            try
            {
                Process p = System.Diagnostics.Process.Start(psi);
                p.WaitForExit(10000);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
