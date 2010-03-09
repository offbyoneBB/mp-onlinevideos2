using System;
using System.Collections.Generic;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;

namespace OnlineVideos
{
    public class GUISiteUpdater : GUIWindow
    {
        enum FilterOption { All, Reported, Broken, Working, Updatable };
        enum SortOption { Updated, Name, Lang_Name, Lang_Updated };

        [SkinControlAttribute(50)]
        protected GUIListControl GUI_infoList = null;
        [SkinControlAttribute(502)]
        protected GUIButtonControl GUI_btnUpdate = null;
        [SkinControlAttribute(503)]
        protected GUISelectButtonControl GUI_btnFilter = null;
        [SkinControlAttribute(504)]
        protected GUISelectButtonControl GUI_btnSort = null;

        OnlineVideosWebservice.Site[] onlineSites = null;
        DateTime lastSitesRetrievalTime = DateTime.MinValue;

        public override int GetID
        {
            get { return 4756; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideosUpdater.xml");
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", " ");
            return result;
        }

        protected override void OnPageLoad()
        {
            Translation.TranslateSkin();

            base.OnPageLoad();

            if (GUI_btnFilter.SubItemCount == 0)
            {
                foreach (string aFilterOption in Enum.GetNames(typeof(FilterOption)))
                {
                    GUIControl.AddItemLabelControl(GetID, GUI_btnFilter.GetID, aFilterOption);
                }
            }
            if (GUI_btnSort.SubItemCount == 0)
            {
                foreach (string aSortOption in Enum.GetNames(typeof(SortOption)))
                {
                    GUIControl.AddItemLabelControl(GetID, GUI_btnSort.GetID, aSortOption.Replace("_", ", "));
                }
            }

            GUIPropertyManager.SetProperty("#header.label",
                                           OnlineVideoSettings.getInstance().BasicHomeScreenName + " Updater");
            GUIPropertyManager.SetProperty("#header.image",
                                           OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/OnlineVideos.png");

            DisplayOnlineSites();
        }

        void DisplayOnlineSites()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.owner", String.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);

            if (DateTime.Now - lastSitesRetrievalTime > TimeSpan.FromMinutes(10)) // only get sites every 10 minutes
            {
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideosWebservice.OnlineVideosService();
                    onlineSites = ws.GetSitesOverview();
                    lastSitesRetrievalTime = DateTime.Now;
                }, "getting site overview from webservice"))
                {
                    return;
                }
            }

            if (onlineSites == null || onlineSites.Length == 0) return;

            GUIControl.ClearControl(GetID, GUI_infoList.GetID);

            List<OnlineVideosWebservice.Site> filteredsortedSites = new List<OnlineVideos.OnlineVideosWebservice.Site>(onlineSites);
            filteredsortedSites = filteredsortedSites.FindAll(SitePassesFilter);
            filteredsortedSites.Sort(CompareSiteForSort);

            foreach (OnlineVideosWebservice.Site site in filteredsortedSites)
            {
                if (!site.IsAdult || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed)
                {
                    GUIListItem loListItem = new GUIListItem(site.Name);
                    loListItem.TVTag = site;
                    loListItem.Label2 = site.Language;
                    loListItem.Label3 = site.LastUpdated.ToString("dd.MM.yy HH:mm");
                    string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + site.Name + ".png";
                    if (System.IO.File.Exists(image)) { loListItem.IconImage = image; loListItem.ThumbnailImage = image; }
                    loListItem.PinImage = GUIGraphicsContext.Skin + @"\Media\OnlineVideos\" + site.State.ToString() + ".png";
                    loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnSiteSelected);
                    GUI_infoList.Add(loListItem);
                }
            }

            GUIControl.SelectItemControl(GetID, GUI_infoList.GetID, 0);
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
            else if (control == GUI_btnFilter)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnFilter.GetID, GUI_btnFilter.SelectedItem);
            }
            else if (control == GUI_infoList)
            {
                ShowOptionsForSite(GUI_infoList.SelectedListItem.TVTag as OnlineVideosWebservice.Site);
            }

            base.OnClicked(controlId, control, actionType);
        }

        void ShowOptionsForSite(OnlineVideosWebservice.Site site)
        {
            SiteSettings localSite = GetLocalSite(site.Name);

            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlgSel != null)
            {
                dlgSel.Reset();
                dlgSel.SetHeading("Options");

                if (localSite == null)
                    dlgSel.Add("Add to my sites");
                else
                {
                    if (localSite.LastUpdated < site.LastUpdated) dlgSel.Add("Update my site");
                }
            }
            dlgSel.DoModal(GetID);
            if (dlgSel.SelectedId != -1)
            {
                switch (dlgSel.SelectedLabelText)
                {
                    case "Add to my sites":
                        SiteSettings newSite = GetRemoteSite(site.Name);
                        if (newSite != null)
                        {
                            OnlineVideoSettings.getInstance().SiteSettingsList.Add(newSite);
                            OnlineVideoSettings.getInstance().SaveSites();
                            OnlineVideoSettings.getInstance().BuildSiteList();
                        }
                        break;
                    case "Update my site":
                        SiteSettings site2Update = GetRemoteSite(site.Name);
                        if (site2Update != null)
                        {
                            for (int i = 0; i < OnlineVideoSettings.getInstance().SiteSettingsList.Count; i++)
                            {
                                if (OnlineVideoSettings.getInstance().SiteSettingsList[i].Name == site2Update.Name)
                                {
                                    OnlineVideoSettings.getInstance().SiteSettingsList[i] = site2Update;
                                    OnlineVideoSettings.getInstance().SaveSites();
                                    OnlineVideoSettings.getInstance().BuildSiteList();
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        bool SitePassesFilter(OnlineVideosWebservice.Site site)
        {
            FilterOption fo = (FilterOption)Enum.Parse(typeof(FilterOption), GUI_btnFilter.SelectedLabel);
            switch (fo)
            {
                case FilterOption.Working:
                    return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Working;
                case FilterOption.Reported:
                    return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Reported;
                case FilterOption.Broken:
                    return site.State == OnlineVideos.OnlineVideosWebservice.SiteState.Broken;
                case FilterOption.Updatable:
                    foreach (SiteSettings localSite in OnlineVideoSettings.getInstance().SiteSettingsList)
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
            SortOption so = (SortOption)Enum.Parse(typeof(SortOption), GUI_btnSort.SelectedLabel.Replace(", ", "_"));
            switch (so)
            {
                case SortOption.Updated:
                    return site1.LastUpdated.CompareTo(site2.LastUpdated);
                case SortOption.Name:
                    return site1.Name.CompareTo(site2.Name);
                case SortOption.Lang_Updated:
                    int langCompResult = site1.Language.CompareTo(site2.Language);
                    if (langCompResult == 0)
                    {
                        return site1.LastUpdated.CompareTo(site2.LastUpdated);
                    }
                    else return langCompResult;
                case SortOption.Lang_Name:
                    int langCompResult2 = site1.Language.CompareTo(site2.Language);
                    if (langCompResult2 == 0)
                    {
                        return site1.Name.CompareTo(site2.Name);
                    }
                    else return langCompResult2;
            }
            return 0;
        }

        SiteSettings GetLocalSite(string name)
        {
            foreach (SiteSettings localSite in OnlineVideoSettings.getInstance().SiteSettingsList)
            {
                if (localSite.Name == name) return localSite;
            }
            return null;
        }

        SiteSettings GetRemoteSite(string name)
        {
            string siteXml = "";
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                OnlineVideosWebservice.OnlineVideosService ws = new OnlineVideos.OnlineVideosWebservice.OnlineVideosService();
                siteXml = ws.GetSiteXml(name);
            }, "getting site xml from webservice"))
            {
                if (siteXml.Length > 0)
                {
                    siteXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<OnlineVideoSites xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<Sites>
" + siteXml + @"
</Sites>
</OnlineVideoSites>";
                    System.IO.StringReader sr = new System.IO.StringReader(siteXml);
                    System.Xml.Serialization.XmlSerializer ser = OnlineVideoSettings.getInstance().XmlSerImp.GetSerializer(typeof(SerializableSettings));
                    SerializableSettings s = (SerializableSettings)ser.Deserialize(sr);
                    if (s.Sites != null && s.Sites.Count > 0)
                    {
                        return s.Sites[0];
                    }
                }
            }
            return null;
        }
    }
}
