using System;
using System.Collections.Generic;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Configuration;

namespace OnlineVideos
{
    public class GUISiteUpdater : GUIWindow
    {
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
        DateTime lastRetrievalTime = DateTime.MinValue;

        public override int GetID
        {
            get { return 4757; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideosUpdater.xml");
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", " ");
            return result;
        }

        #if !MP102
        public override string GetModuleName()
        {
            return OnlineVideoSettings.PLUGIN_NAME;
        }
        #endif

        protected override void OnPageLoad()
        {
            #if MP102
            GUIPropertyManager.SetProperty("#currentmodule", OnlineVideoSettings.PLUGIN_NAME);
            #endif

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

            if (DateTime.Now - lastRetrievalTime > TimeSpan.FromMinutes(10)) // only get sites every 10 minutes
            {
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
                    onlineSites = ws.GetSitesOverview();
                    onlineDlls = ws.GetDllsOverview();
                    lastRetrievalTime = DateTime.Now;
                }, "getting site overview from webservice"))
                {
                    return;
                }
                SetFilterButtonOptions();
            }

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
                else GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);
            }
        }

        public override void OnAction(Action action)
        {
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
                ShowOptionsForSite(GUI_infoList.SelectedListItem.TVTag as OnlineVideosWebservice.Site);
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
                    if (OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].LastUpdated < site.LastUpdated)
                    {
                        dlgSel.Add(Translation.UpdateMySite);
                        dlgSel.Add(Translation.UpdateMySiteSkipCategories);                        
                    }
                    dlgSel.Add(Translation.RemoveFromMySites);
                }
            }
            dlgSel.DoModal(GetID);

            if (dlgSel.SelectedLabelText == Translation.AddToMySites)
            {
                
                    SiteSettings newSite = GetRemoteSite(site.Name);
                    if (newSite != null)
                    {
                        OnlineVideoSettings.Instance.SiteSettingsList.Add(newSite);
                        OnlineVideoSettings.Instance.SaveSites();
                        OnlineVideoSettings.Instance.BuildSiteList();
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
                }
            }
            else if (dlgSel.SelectedLabelText == Translation.UpdateMySiteSkipCategories)
            {
                SiteSettings siteRemote = GetRemoteSite(site.Name);
                if (siteRemote != null)
                {
                    siteRemote.Categories = OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].Categories;
                    OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex] = siteRemote;
                    OnlineVideoSettings.Instance.SaveSites();
                    OnlineVideoSettings.Instance.BuildSiteList();                            
                }
            }
            else if (dlgSel.SelectedLabelText == Translation.RemoveFromMySites)
            {
                OnlineVideoSettings.Instance.SiteSettingsList.RemoveAt(localSiteIndex);
                OnlineVideoSettings.Instance.SaveSites();
                OnlineVideoSettings.Instance.BuildSiteList();
            }
        }

        void FullUpdate()
        {
            GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
            if (dlgPrgrs != null)
            {
                dlgPrgrs.Reset();
                dlgPrgrs.DisplayProgressBar = true;
                dlgPrgrs.ShowWaitCursor = false;
                dlgPrgrs.DisableCancel(true);
                dlgPrgrs.SetHeading(OnlineVideoSettings.PLUGIN_NAME);
                dlgPrgrs.StartModal(GetID);
            }

            Dictionary<string, bool> requiredDlls = new Dictionary<string, bool>();

            // update or add all sites, so the user has everything the server currently has
            for (int i = 0; i < onlineSites.Length; i++ )
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
                else if (OnlineVideoSettings.Instance.SiteSettingsList[localSiteIndex].LastUpdated < onlineSite.LastUpdated)
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

            if (dlgPrgrs != null) dlgPrgrs.SetLine(1, "Saving local site list");
            OnlineVideoSettings.Instance.SaveSites();
            OnlineVideoSettings.Instance.BuildSiteList();
            if (dlgPrgrs != null) dlgPrgrs.Percentage = 90;

            // we can't update dlls here - check if new dlls are needed and tell the user to restart MediaPortal so AutoUpdate can work
            if (dlgPrgrs != null) dlgPrgrs.SetLine(1, "Checking dlls");
            bool showMessage = false;
            if (requiredDlls.Count > 0)
            {     
                // if dir not found -> no need to check the dlls with MD5
                string dllDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "OnlineVideos\\");
                if (!System.IO.Directory.Exists(dllDir)) showMessage = true;
                else
                {                    
                    for (int i = 0; i < onlineDlls.Length; i++)
                    {
                        OnlineVideosWebservice.Dll anOnlineDll = onlineDlls[i];
                        if (dlgPrgrs != null) dlgPrgrs.SetLine(1, anOnlineDll.Name);
                        if (requiredDlls.ContainsKey(anOnlineDll.Name))
                        {
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
                                    showMessage = true;
                                    break;
                                }
                            }
                            else
                            {
                                showMessage = true;
                                break;
                            }
                        }
                        if (dlgPrgrs != null) dlgPrgrs.Percentage = 90 + (10 * (i + 1) / onlineDlls.Length);
                    }
                }
            }
            if (dlgPrgrs != null) { dlgPrgrs.Percentage = 100; dlgPrgrs.SetLine(1, "Done"); dlgPrgrs.Close(); }
            if (showMessage)
            {
                GUIDialogOK dlg = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(OnlineVideoSettings.PLUGIN_NAME);
                    dlg.SetLine(1, Translation.NewDllRequired);
                    dlg.SetLine(2, Translation.RestartMediaPortalAndAutomaticUpdate);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
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
                            if (localSite.LastUpdated < site.LastUpdated) return true;
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

        public static SiteSettings GetRemoteSite(string name)
        {
             return GetRemoteSite(name, null);
        }

        public static SiteSettings GetRemoteSite(string name, OnlineVideosWebservice.OnlineVideosService ws)
        {
            string siteXml = "";            
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                if (ws == null) ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                siteXml = ws.GetSiteXml(name);
            }, "getting site xml from webservice"))
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

        static void DownloadImages(string siteName, OnlineVideosWebservice.OnlineVideosService ws)
        {
            try
            {
                byte[] icon = null;
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    icon = ws.GetSiteIcon(siteName);
                }, "getting site icon from webservice"))
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
                }, "getting site banner from webservice"))
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
    }
}
