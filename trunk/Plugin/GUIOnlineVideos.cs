using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Playlists;

namespace OnlineVideos
{
    [PluginIcons("OnlineVideos.OnlineVideos.png", "OnlineVideos.OnlineVideosDisabled.png")]
    public class GUIOnlineVideos : GUIWindow, ISetupForm, IShowPlugin
    {
        public enum SiteOrder { AsInFile = 0, Name = 1, Language = 2 }

        public enum State { sites = 0, categories = 1, videos = 2, details = 3 }

        public enum VideosMode { Category = 0, Favorites = 1, Search = 2, Related = 3 }

        #region IShowPlugin Implementation

        public bool ShowDefaultHome()
        {
            return true;
        }

        #endregion

        #region ISetupForm Implementation

        public string Author()
        {
            return "GregMac45|offbyone";
        }

        public bool CanEnable()
        {
            return true;
        }

        public bool DefaultEnabled()
        {
            return true;
        }

        public string Description()
        {
            return "Browse videos from various online sites.";
        }

        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = OnlineVideoSettings.getInstance().BasicHomeScreenName;
            strButtonImage = String.Empty;
            strButtonImageFocus = String.Empty;
            strPictureImage = @"hover_OnlineVideos.png";
            return true;
        }

        public int GetWindowId()
        {
            return GetID;
        }

        public bool HasSetup()
        {
            return true;
        }

        public string PluginName()
        {
            return OnlineVideoSettings.PLUGIN_NAME;
        }

        /// <summary>
        /// Show Plugin Configuration Dialog.
        /// </summary>
        public void ShowPlugin()
        {
            System.Windows.Forms.Form setup = new Configuration();
            setup.ShowDialog();
        }

        #endregion

        #region Skin Controls
        [SkinControlAttribute(2)]
        protected GUIButtonControl GUI_btnViewAs = null;
        [SkinControlAttribute(3)]
        protected GUIButtonControl GUI_btnNext = null;
        [SkinControlAttribute(4)]
        protected GUIButtonControl GUI_btnPrevious = null;
        [SkinControlAttribute(5)]
        protected GUISelectButtonControl GUI_btnMaxResult = null;
        [SkinControlAttribute(6)]
        protected GUISelectButtonControl GUI_btnOrderBy = null;
        [SkinControlAttribute(7)]
        protected GUISelectButtonControl GUI_btnTimeFrame = null;
        [SkinControlAttribute(8)]
        protected GUIButtonControl GUI_btnUpdate = null;
        [SkinControlAttribute(9)]
        protected GUISelectButtonControl GUI_btnSearchCategories = null;
        [SkinControlAttribute(10)]
        protected GUIButtonControl GUI_btnSearch = null;
        [SkinControlAttribute(11)]
        protected GUIButtonControl GUI_btnFavorite = null;
        [SkinControlAttribute(12)]
        protected GUIButtonControl GUI_btnEnterPin = null;
        [SkinControlAttribute(50)]
        protected GUIFacadeControl GUI_facadeView = null;
        [SkinControlAttribute(51)]
        protected GUIListControl GUI_infoList = null;
        #endregion

        #region Facade ViewModes
        protected GUIFacadeControl.ViewMode currentView = GUIFacadeControl.ViewMode.List;
        protected GUIFacadeControl.ViewMode currentSiteView = GUIFacadeControl.ViewMode.List;
        protected GUIFacadeControl.ViewMode currentCategoryView = GUIFacadeControl.ViewMode.List;
        protected GUIFacadeControl.ViewMode currentVideoView = GUIFacadeControl.ViewMode.SmallIcons;
        protected GUIFacadeControl.ViewMode? suggestedView;
        #endregion

        #region state variables
        State currentState = State.sites;
        public State CurrentState
        {
            get { return currentState; }
            set { currentState = value; GUIPropertyManager.SetProperty("#OnlineVideos.state", value.ToString()); }
        }
        VideosMode currentVideosDisplayMode = VideosMode.Category;
        SiteOrder siteOrder = SiteOrder.AsInFile;

        Sites.SiteUtilBase selectedSite;
        Category selectedCategory;
        VideoInfo selectedVideo;

        int selectedSiteIndex = 0;  // used to remember the position of the last selected site
        int selectedVideoIndex = 0; // used to remember the position of the last selected Video
        int selectedClipIndex = 0;  // used to remember the position the last selected Trailer

        List<VideoInfo> currentVideoList = new List<VideoInfo>();
        List<VideoInfo> currentTrailerList = new List<VideoInfo>();

        RTMP_LIB.HTTPServer proxyRtmp;
        OnlineVideos.Sites.AppleProxyServer proxyApple;

        internal static Dictionary<string, DownloadInfo> currentDownloads = new Dictionary<string, DownloadInfo>();
        #endregion

        #region filter variables
        List<int> moSupportedMaxResultList;
        Dictionary<String, String> moSupportedOrderByList;
        Dictionary<String, String> moSupportedTimeFrameList;
        Dictionary<String, String> moSupportedSearchCategoryList;

        //selected values
        int miMaxResult;
        string msOrderBy = String.Empty;
        string msTimeFrame = String.Empty;

        //selected indices
        int SelectedMaxResultIndex;
        int SelectedOrderByIndex;
        int SelectedTimeFrameIndex;
        int SelectedSearchCategoryIndex;
        #endregion

        #region search variables
        string lastSearchQuery;
        string lastSearchCategory;
        #endregion

        #region GUIWindow Overrides

        public override int GetID
        {
            get { return 4755; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideos.xml");
            LoadSettings();
            if (proxyRtmp == null) proxyRtmp = new RTMP_LIB.HTTPServer(OnlineVideoSettings.RTMP_PROXY_PORT);
            if (proxyApple == null) proxyApple = new OnlineVideos.Sites.AppleProxyServer(OnlineVideoSettings.APPLE_PROXY_PORT);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", " ");
            GUIPropertyManager.SetProperty("#OnlineVideos.length", " ");
            CurrentState = State.sites;
            return result;
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad(); // let animations run            

            // everytime the plugin is shown, after some other window was shown
            if (OnlineVideoSettings.getInstance().ageHasBeenConfirmed && PreviousWindowId == 0)
            {
                // if a pin was inserted before, reset to false and show the home page in case the user was browsing some adult site last
                Log.Debug("OnlineVideos Age Confirmed set to false.");
                OnlineVideoSettings.getInstance().ageHasBeenConfirmed = false;
                CurrentState = State.sites;
            }

            Log.Debug("OnPageLoad. CurrentState:" + CurrentState);
            if (CurrentState == State.sites)
            {
                DisplaySites();
                SetFacadeViewMode();
            }
            else if (CurrentState == State.categories)
            {
                DisplayCategories(selectedCategory);
                SetFacadeViewMode();
            }
            else if (CurrentState == State.videos)
            {
                SetVideoListToFacade(currentVideoList, true);
                SetFacadeViewMode();
            }
            else
            {
                DisplayDetails(selectedVideo);
                if (selectedClipIndex < GUI_infoList.Count) GUI_infoList.SelectedListItemIndex = selectedClipIndex;
            }
            UpdateViewState();
        }

        protected override void OnShowContextMenu()
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. getting next page videos to finish

            int liSelected = GUI_facadeView.SelectedListItemIndex - 1;

            if (CurrentState == State.details && selectedSite.HasMultipleVideos) liSelected = GUI_infoList.SelectedListItemIndex - 1;

            if (liSelected < 0 || CurrentState == State.sites || CurrentState == State.categories || (selectedSite.HasMultipleVideos && CurrentState == State.videos))
            {
                return;
            }
            GUIListItem loListItem = GUI_facadeView.SelectedListItem;
            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            dlgSel.Reset();

            if (dlgSel != null)
            {
                dlgSel.SetHeading(498);  // Actions

                dlgSel.Add(GUILocalizeStrings.Get(30003)); //Play All

                if (currentVideosDisplayMode != VideosMode.Favorites && !(selectedSite is Sites.FavoriteUtil))
                {
                    if (!(selectedSite is Sites.DownloadedVideoUtil)) dlgSel.Add(GUILocalizeStrings.Get(930)/*Add to favorites*/);
                }
                else
                {
                    dlgSel.Add(GUILocalizeStrings.Get(933)/*Remove from favorites*/);
                }
                if (selectedSite.HasRelatedVideos)
                {
                    dlgSel.Add(GUILocalizeStrings.Get(33011)); /*Related Videos*/
                }
                if (String.IsNullOrEmpty(OnlineVideoSettings.getInstance().msDownloadDir) == false)
                {
                    if (selectedSite is Sites.DownloadedVideoUtil) dlgSel.Add(GUILocalizeStrings.Get(117)/*Delete*/); else dlgSel.Add(GUILocalizeStrings.Get(190)/*Save*/);
                }
            }
            dlgSel.DoModal(GetID);
            int liSelectedIdx = dlgSel.SelectedId;
            VideoInfo loSelectedVideo;
            if (CurrentState == State.videos)
            {
                loSelectedVideo = currentVideoList[liSelected];
            }
            else
            {
                loSelectedVideo = currentTrailerList[liSelected];
            }
            switch (liSelectedIdx)
            {
                case 1:
                    PlayAll();
                    break;
                case 2:
                    if (selectedSite is Sites.DownloadedVideoUtil)
                    {
                        if (System.IO.File.Exists(loSelectedVideo.ImageUrl)) System.IO.File.Delete(loSelectedVideo.ImageUrl);
                        if (System.IO.File.Exists(loSelectedVideo.VideoUrl)) System.IO.File.Delete(loSelectedVideo.VideoUrl);
                        DisplayVideos_Category(true);
                    }
                    else
                    {
                        AddOrRemoveFavorite(loSelectedVideo);
                    }
                    break;
                case 3:
                    if (selectedSite.HasRelatedVideos)
                    {
                        DisplayVideos_Related(loSelectedVideo);
                        UpdateViewState();
                    }
                    else
                    {
                        SaveVideo(loSelectedVideo);
                    }
                    break;
                case 4:
                    SaveVideo(loSelectedVideo);
                    break;

            }
            base.OnShowContextMenu();
        }

        public override void OnAction(Action action)
        {
            if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU && CurrentState != State.sites)
            {
                if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish

                // 2009-05-21 MichelC - Prevents a bug when hitting ESC and the hidden menu is opened.
                GUIControl focusedControl = GetControl(GetFocusControlId());
                if (focusedControl != null)
                {
                    if (focusedControl.Type == "button" || focusedControl.Type == "selectbutton")
                    {
                        int focusedControlId = GetFocusControlId();
                        if (focusedControlId >= 0)
                        {
                            GUIControl.UnfocusControl(GetID, focusedControlId);
                        }
                    }
                }

                ShowPreviousMenu();
                return;
            }
            else if (action.wID == Action.ActionType.ACTION_KEY_PRESSED && GUI_facadeView.Visible && GUI_facadeView.Focus)
            {
                // search items (starting from current selected) by title and select first found one
                char pressedChar = (char)action.m_key.KeyChar;
                if (char.IsLetterOrDigit(pressedChar))
                {
                    string lowerChar = pressedChar.ToString().ToLower();
                    for (int i = GUI_facadeView.SelectedListItemIndex + 1; i < GUI_facadeView.Count; i++)
                    {
                        if (GUI_facadeView[i].Label.ToLower().StartsWith(lowerChar))
                        {
                            GUI_facadeView.SelectedListItemIndex = i;
                            return;
                        }
                    }
                }
            }
            else if (action.wID == Action.ActionType.ACTION_NEXT_ITEM && currentState == State.videos && GUI_facadeView.Visible && GUI_facadeView.Focus)
            {
                if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish

                if (GUI_btnNext.IsEnabled)
                {
                    DisplayVideos_NextPage();
                    UpdateViewState();
                }
            }
            else if (action.wID == Action.ActionType.ACTION_PREV_ITEM && currentState == State.videos && GUI_facadeView.Visible && GUI_facadeView.Focus)
            {
                if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish

                if (GUI_btnPrevious.IsEnabled)
                {
                    DisplayVideos_PreviousPage();
                    UpdateViewState();
                }
            }

            base.OnAction(action);
        }

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                    {
                        base.OnMessage(message);
                        GUI_btnSearchCategories.RestoreSelection = false;
                        GUI_btnOrderBy.RestoreSelection = false;
                        GUI_btnTimeFrame.RestoreSelection = false;
                        GUI_btnMaxResult.RestoreSelection = false;
                        return true;
                    }
            }
            return base.OnMessage(message);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish

            if (control == GUI_facadeView && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                if (CurrentState == State.sites)
                {
                    selectedSite = OnlineVideoSettings.getInstance().SiteList[GUI_facadeView.SelectedListItem.Path];
                    selectedSiteIndex = GUI_facadeView.SelectedListItemIndex;
                    DisplayCategories(null);
                    CurrentState = State.categories;
                }
                else if (CurrentState == State.categories)
                {
                    if (GUI_facadeView.SelectedListItemIndex == 0)
                    {
                        ShowPreviousMenu();
                    }
                    else
                    {
                        Category categoryToRestoreOnError = selectedCategory;
                        if (selectedCategory == null) selectedCategory = selectedSite.Settings.Categories[GUI_facadeView.SelectedListItemIndex - 1];
                        else selectedCategory = selectedCategory.SubCategories[GUI_facadeView.SelectedListItemIndex - 1];

                        if (selectedCategory.HasSubCategories)
                        {
                            DisplayCategories(selectedCategory);
                        }
                        else
                        {
                            if (!DisplayVideos_Category(false)) selectedCategory = categoryToRestoreOnError;
                        }
                    }
                }
                else if (CurrentState == State.videos)
                {
                    Log.Info("Set the stopDownload to true 2");
                    ImageDownloader._stopDownload = true;
                    if (GUI_facadeView.SelectedListItemIndex == 0)
                    {
                        ShowPreviousMenu();
                    }
                    else
                    {
                        selectedVideoIndex = GUI_facadeView.SelectedListItemIndex;
                        if (selectedSite.HasMultipleVideos)
                        {
                            if (DisplayDetails(currentVideoList[GUI_facadeView.SelectedListItemIndex - 1]))
                            {
                                CurrentState = State.details;
                            }
                        }
                        else
                        {
                            //play the video
                            Play(currentVideoList[GUI_facadeView.SelectedListItemIndex - 1]);
                        }
                    }
                }
                UpdateViewState();
            }
            else if (control == GUI_infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM && CurrentState == State.details)
            {
                ImageDownloader._stopDownload = true;
                if (GUI_infoList.SelectedListItemIndex == 0)
                {
                    ShowPreviousMenu();
                }
                else
                {
                    selectedClipIndex = GUI_infoList.SelectedListItemIndex;
                    //play the video
                    Play(currentTrailerList[GUI_infoList.SelectedListItemIndex - 1]);
                }
                UpdateViewState();
            }
            else if (control == GUI_btnViewAs)
            {
                ToggleFacadeViewMode();
            }
            else if (control == GUI_btnNext)
            {
                GUIControl.UnfocusControl(GetID, GUI_btnNext.GetID);
                DisplayVideos_NextPage();
                UpdateViewState();
            }
            else if (control == GUI_btnPrevious)
            {
                GUIControl.UnfocusControl(GetID, GUI_btnPrevious.GetID);
                DisplayVideos_PreviousPage();
                UpdateViewState();
            }
            else if (control == GUI_btnMaxResult)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnMaxResult.GetID, GUI_btnMaxResult.SelectedItem);
            }
            else if (control == GUI_btnOrderBy)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnOrderBy.GetID, GUI_btnOrderBy.SelectedItem);
                if (CurrentState == State.sites) siteOrder = (SiteOrder)GUI_btnOrderBy.SelectedItem;
            }
            else if (control == GUI_btnTimeFrame)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnTimeFrame.GetID, GUI_btnTimeFrame.SelectedItem);
            }
            else if (control == GUI_btnUpdate)
            {
                GUIControl.UnfocusControl(GetID, GUI_btnUpdate.GetID);
                switch (CurrentState)
                {
                    case State.sites: DisplaySites(); break;
                    case State.videos: DisplayVideos_Filter(); break;
                }
                UpdateViewState();
            }
            else if (control == GUI_btnSearchCategories)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnSearchCategories.GetID, GUI_btnSearchCategories.SelectedItem);
            }
            else if (control == GUI_btnSearch)
            {
                String query = String.Empty;
                if (GetUserInputString(ref query, false))
                {
                    GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
                    if (query != String.Empty)
                    {
                        lastSearchQuery = query;
                        if (DisplayVideos_Search(query))
                        {
                            CurrentState = State.videos;
                            UpdateViewState();
                        }
                    }
                }
            }
            else if ((control == GUI_btnFavorite))
            {
                GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
                if (DisplayVideos_Favorite())
                {
                    CurrentState = State.videos;
                    UpdateViewState();
                }
            }
            else if (control == GUI_btnEnterPin)
            {
                string pin = String.Empty;
                if (GetUserInputString(ref pin, true))
                {
                    if (pin == OnlineVideoSettings.getInstance().pinAgeConfirmation)
                    {
                        OnlineVideoSettings.getInstance().ageHasBeenConfirmed = true;
                        HideAndDisable(GUI_btnEnterPin.GetID);
                        DisplaySites();
                        GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
                    }
                }
            }

            base.OnClicked(controlId, control, actionType);
        }

        protected override void OnPageDestroy(int newWindowId)
        {
            // Save view
            using (MediaPortal.Profile.Settings xmlwriter = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                xmlwriter.SetValue(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_MODE, (int)currentSiteView);
                xmlwriter.SetValue(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_ORDER, (int)siteOrder);
                xmlwriter.SetValue(OnlineVideoSettings.SECTION, OnlineVideoSettings.VIDEOVIEW_MODE, (int)currentVideoView);
                xmlwriter.SetValue(OnlineVideoSettings.SECTION, OnlineVideoSettings.CATEGORYVIEW_MODE, (int)currentCategoryView);
            }
            base.OnPageDestroy(newWindowId);
        }

        #endregion

        #region new methods

        private void ShowAndEnable(int iControlId)
        {
            GUIControl.ShowControl(GetID, iControlId);
            GUIControl.EnableControl(GetID, iControlId);
        }

        private void HideAndDisable(int iControlId)
        {
            GUIControl.HideControl(GetID, iControlId);
            GUIControl.DisableControl(GetID, iControlId);
        }

        private void LoadSettings()
        {
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                currentSiteView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                siteOrder = (SiteOrder)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_ORDER, 0);
                currentVideoView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.VIDEOVIEW_MODE, (int)GUIFacadeControl.ViewMode.SmallIcons);
                currentCategoryView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.CATEGORYVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
            }            
            OnlineVideoSettings.getInstance().BuildSiteList();            
        }

        private void DisplaySites()
        {
            selectedCategory = null;
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);

            // set order by options
            GUI_btnOrderBy.Clear();
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, GUILocalizeStrings.Get(886)); //Default
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, GUILocalizeStrings.Get(103)); //Name
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, GUILocalizeStrings.Get(304)); //Language
            GUI_btnOrderBy.SelectedItem = (int)siteOrder;

            // get names in right order
            string[] names = new string[OnlineVideoSettings.getInstance().SiteList.Count];
            switch (siteOrder)
            {
                case SiteOrder.Name:
                    OnlineVideoSettings.getInstance().SiteList.Keys.CopyTo(names, 0);
                    Array.Sort(names);
                    break;
                case SiteOrder.Language:
                    Dictionary<string, List<string>> sitenames = new Dictionary<string, List<string>>();
                    foreach (Sites.SiteUtilBase aSite in OnlineVideoSettings.getInstance().SiteList.Values)
                    {
                        string key = string.IsNullOrEmpty(aSite.Settings.Language) ? "zzzzz" : aSite.Settings.Language; // puts empty lang at the end
                        List<string> listForLang = null;
                        if (!sitenames.TryGetValue(key, out listForLang)) { listForLang = new List<string>(); sitenames.Add(key, listForLang); }
                        listForLang.Add(aSite.Settings.Name);
                    }
                    string[] langs = new string[sitenames.Count];
                    sitenames.Keys.CopyTo(langs, 0);
                    Array.Sort(langs);
                    int index = 0;
                    foreach (string lang in langs)
                    {
                        sitenames[lang].CopyTo(names, index);
                        index += sitenames[lang].Count;
                    }
                    break;
                default:
                    OnlineVideoSettings.getInstance().SiteList.Keys.CopyTo(names, 0);
                    break;
            }

            foreach (string name in names)
            {
                Sites.SiteUtilBase aSite = OnlineVideoSettings.getInstance().SiteList[name];
                if (aSite.Settings.IsEnabled &&
                    (!aSite.Settings.ConfirmAge || !OnlineVideoSettings.getInstance().useAgeConfirmation || OnlineVideoSettings.getInstance().ageHasBeenConfirmed))
                {
                    GUIListItem loListItem = new GUIListItem(aSite.Settings.Name);
                    loListItem.Label2 = aSite.Settings.Language;
                    loListItem.Path = aSite.Settings.Name;
                    loListItem.IsFolder = true;
                    string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + aSite.Settings.Name + ".png";
                    if (System.IO.File.Exists(image))
                    {
                        loListItem.ThumbnailImage = image;
                        loListItem.IconImage = image;
                        loListItem.IconImageBig = image;
                        loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnSiteSelected);
                    }
                    else
                    {
                        MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                    }
                    GUI_facadeView.Add(loListItem);
                }
            }
            SelectedMaxResultIndex = -1;
            SelectedOrderByIndex = -1;
            SelectedSearchCategoryIndex = -1;
            SelectedTimeFrameIndex = -1;

            if (selectedSiteIndex < GUI_facadeView.Count) GUI_facadeView.SelectedListItemIndex = selectedSiteIndex;

            GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(2143)/*Home*/);
            GUIPropertyManager.SetProperty("#header.image", "OnlineVideos/OnlineVideos.png");
        }

        private void DisplayCategories(Category parentCategory)
        {
            // whenever categories are displayed, reset the selected video index, 
            // so re-entering another category won't restore a previously selected video
            selectedVideoIndex = 0; 

            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            GUIListItem loListItem;
            loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_facadeView.Add(loListItem);

            int numCategoriesWithThumb = 0;
            List<String> imagesUrlList = new List<string>();

            suggestedView = null;
            IList<Category> categories = null;

            if (parentCategory == null)
            {
                if (!selectedSite.Settings.DynamicCategoriesDiscovered)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                    {
                        Log.Info("Looking for dynamic categories for {0}", selectedSite.Settings.Name);
                        try
                        {
                            int foundCategories = selectedSite.DiscoverDynamicCategories();
                            Log.Info("Found {0} dynamic categories.", foundCategories);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error looking for dynamic categories: " + ex.ToString());
                        }
                    }, "getting dynamic categories");
                }
                categories = selectedSite.Settings.Categories;
            }
            else
            {
                if (!parentCategory.SubCategoriesDiscovered)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                    {
                        Log.Info("Looking for sub categories of site {0} in {1}", selectedSite.Settings.Name, parentCategory.Name);
                        try
                        {
                            int foundCategories = selectedSite.DiscoverSubCategories(parentCategory);
                            Log.Info("Found {0} sub categories.", foundCategories);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error looking for sub categories: " + ex.ToString());
                        }
                    }, "getting dynamic subcategories");
                }

                categories = parentCategory.SubCategories;
            }

            int categoryIndexToSelect = (categories != null && categories.Count > 0) ? 1 : 0; // select the first category by default if there is one
            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    Category loCat = categories[i];
                    if (loCat == selectedCategory) categoryIndexToSelect = i + 1; // select the category that was previously selected

                    loListItem = new GUIListItem(loCat.Name);
                    loListItem.IsFolder = true;
                    MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                    // Favorite Categories can have the same images as the home view
                    if (selectedSite is Sites.FavoriteUtil)
                    {
                        string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + ((RssLink)loCat).Url.Substring(4) + ".png";
                        if (System.IO.File.Exists(image))
                        {
                            loListItem.ThumbnailImage = image;
                            loListItem.IconImage = image;
                            loListItem.IconImageBig = image;
                            numCategoriesWithThumb++;
                        }
                    }
                    else
                    {
                        imagesUrlList.Add(loCat.Thumb);
                        if (!string.IsNullOrEmpty(loCat.Thumb))
                        {
                            numCategoriesWithThumb++;
                            loListItem.ItemId = imagesUrlList.Count;
                            loListItem.RetrieveArt = false;
                            loListItem.OnRetrieveArt += new MediaPortal.GUI.Library.GUIListItem.RetrieveCoverArtHandler(OnRetrieveCoverArt);
                        }
                    }

                    GUI_facadeView.Add(loListItem);

                    if (loCat is RssLink)
                    {
                        RssLink link = loCat as RssLink;
                        loListItem.Path = link.Url;
                        if (link.EstimatedVideoCount > 0) loListItem.Label2 = link.EstimatedVideoCount.ToString();
                    }
                    else
                    {
                        loListItem.Path = loCat.Name;
                    }

                    if (loCat is Group)
                    {
                        loListItem.Label2 = (loCat as Group).Channels.Count.ToString();
                    }
                }

                if (numCategoriesWithThumb > 0) ImageDownloader.getImages(imagesUrlList, OnlineVideoSettings.getInstance().msThumbLocation, GUI_facadeView);
                if (numCategoriesWithThumb <= categories.Count / 2) suggestedView = GUIFacadeControl.ViewMode.List;
            }

            selectedCategory = parentCategory;
            GUI_facadeView.SelectedListItemIndex = categoryIndexToSelect;
        }

        private bool DisplayDetails(VideoInfo foVideo)
        {
            selectedVideo = foVideo;
            List<VideoInfo> loVideoList = null;
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                loVideoList = selectedSite.getOtherVideoList(foVideo);
            }, "getting video details"))
            {
                currentTrailerList.Clear();
                GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
                GUIControl.ClearControl(GetID, GUI_infoList.GetID);
                GUIListItem loListItem = new GUIListItem("..");
                loListItem.IsFolder = true;
                loListItem.ItemId = 0;
                MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                GUI_infoList.Add(loListItem);
                int liIdx = 0;
                foreach (VideoInfo loVideoInfo in loVideoList)
                {
                    liIdx++;
                    loVideoInfo.CleanDescription();
                    loListItem = new GUIListItem(loVideoInfo.Title2);
                    loListItem.Path = loVideoInfo.VideoUrl;
                    loListItem.ItemId = liIdx;
                    GUI_infoList.Add(loListItem);
                    currentTrailerList.Add(loVideoInfo);
                }
                return true;
            }
            return false;
        }

        private bool DisplayVideos_Category(bool displayCategoriesOnError)
        {
            List<VideoInfo> categoryVideos = new List<VideoInfo>();
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                categoryVideos = selectedSite.getVideoList(selectedCategory);
            }, "getting category videos"))
            {
                if (SetVideoListToFacade(categoryVideos, false))
                {
                    CurrentState = State.videos;
                    currentVideosDisplayMode = VideosMode.Category;
                    return true;
                }
            }
            if (displayCategoriesOnError)// an error occured or no videos were found -> return to the category selection if param was set
            {
                CurrentState = State.categories;
                DisplayCategories(selectedCategory == null ? null : selectedCategory.ParentCategory);
                UpdateViewState();
            }
            return false;
        }

        private bool DisplayVideos_Favorite()
        {
            List<VideoInfo> favoriteVideos = null;
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                favoriteVideos = ((IFavorite)selectedSite).getFavorites();
            }, "getting favorite videos"))
            {
                if (SetVideoListToFacade(favoriteVideos, false))
                {
                    currentVideosDisplayMode = VideosMode.Favorites;
                    return true;
                }
            }
            return false;
        }

        private bool DisplayVideos_Search(String query)
        {
            SelectedSearchCategoryIndex = GUI_btnSearchCategories.SelectedItem;
            if (query != String.Empty)
            {
                List<VideoInfo> searchResultVideos = null;
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    if (moSupportedSearchCategoryList.Count > 1 && !GUI_btnSearchCategories.SelectedLabel.Equals("All"))
                    {
                        string category = moSupportedSearchCategoryList[GUI_btnSearchCategories.SelectedLabel];
                        Log.Info("Searching for {0} in category {1}", query, category);
                        lastSearchCategory = category;
                        searchResultVideos = selectedSite.Search(query, category);
                    }
                    else
                    {
                        Log.Info("Searching for {0} in all categories ", query);
                        searchResultVideos = selectedSite.Search(query);
                    }
                }, "getting search results"))
                {
                    if (SetVideoListToFacade(searchResultVideos, false))
                    {
                        currentVideosDisplayMode = VideosMode.Search;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool DisplayVideos_Related(VideoInfo video)
        {
            if (video != null)
            {
                List<VideoInfo> relatedVideos = new List<VideoInfo>();
                if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    relatedVideos = selectedSite.getRelatedVideos(video);
                }, "getting related videos"))
                {
                    if (SetVideoListToFacade(relatedVideos, false))
                    {
                        currentVideosDisplayMode = VideosMode.Related;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool DisplayVideos_Filter()
        {
            miMaxResult = -1;
            SelectedMaxResultIndex = GUI_btnMaxResult.SelectedItem;
            SelectedOrderByIndex = GUI_btnOrderBy.SelectedItem;
            SelectedTimeFrameIndex = GUI_btnTimeFrame.SelectedItem;
            try
            {
                miMaxResult = Int32.Parse(GUI_btnMaxResult.SelectedLabel);
            }
            catch (Exception) { }
            msOrderBy = String.Empty;
            try
            {
                msOrderBy = moSupportedOrderByList[GUI_btnOrderBy.SelectedLabel];
            }
            catch (Exception) { }
            msTimeFrame = String.Empty;
            try
            {
                msTimeFrame = moSupportedTimeFrameList[GUI_btnTimeFrame.SelectedLabel];
            }
            catch (Exception) { }

            List<VideoInfo> filteredVideos = new List<VideoInfo>();
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                if (currentVideosDisplayMode == VideosMode.Search)
                {
                    Log.Info("Filtering search result");
                    //filtering a search result
                    if (String.IsNullOrEmpty(lastSearchCategory))
                    {
                        filteredVideos = ((IFilter)selectedSite).filterSearchResultList(lastSearchQuery, miMaxResult, msOrderBy, msTimeFrame);
                    }
                    else
                    {
                        filteredVideos = ((IFilter)selectedSite).filterSearchResultList(lastSearchQuery, lastSearchCategory, miMaxResult, msOrderBy, msTimeFrame);
                    }
                }
                else
                {
                    if (selectedSite.HasFilterCategories) // just for setting the category
                        filteredVideos = selectedSite.Search(string.Empty, moSupportedSearchCategoryList[GUI_btnSearchCategories.SelectedLabel]);
                    if (selectedSite is IFilter)
                        filteredVideos = ((IFilter)selectedSite).filterVideoList(selectedCategory, miMaxResult, msOrderBy, msTimeFrame);
                }
            }, "getting filtered videos"))
            {
                if (SetVideoListToFacade(filteredVideos, false))
                {
                    return true;
                }
            }
            return false;
        }

        private bool DisplayVideos_NextPage()
        {
            List<VideoInfo> nextPageVideos = new List<VideoInfo>();
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                nextPageVideos = selectedSite.getNextPageVideos();
            }, "getting next page videos"))
            {
                if (SetVideoListToFacade(nextPageVideos, false))
                {
                    return true;
                }
            }
            return false;
        }

        private bool DisplayVideos_PreviousPage()
        {
            List<VideoInfo> previousPageVideos = new List<VideoInfo>();
            if (Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                previousPageVideos = selectedSite.getPreviousPageVideos();
            }, "getting previous page videos"))
            {
                if (SetVideoListToFacade(previousPageVideos, false))
                {
                    return true;
                }
            }
            return false;
        }

        private bool SetVideoListToFacade(List<VideoInfo> foVideos, bool restoreSelectedIndex)
        {
            // Check for received data
            if (foVideos == null || foVideos.Count == 0)
            {
                GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlg_error.SetHeading(PluginName());
                dlg_error.SetLine(1, GUILocalizeStrings.Get(1036)/*No Videos found!*/);
                dlg_error.SetLine(2, String.Empty);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                return false;
            }
            // add the first item that will go to the previous menu
            GUIListItem loListItem;
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_facadeView.Add(loListItem);
            // add the items
            List<String> loImageUrlList = new List<string>();
            int numVideosWithThumb = 0;
            int liIdx = 0;
            foreach (VideoInfo loVideoInfo in foVideos)
            {
                liIdx++;
                loVideoInfo.CleanDescription();
                if (FilterOut(loVideoInfo.Title) || FilterOut(loVideoInfo.Description))
                {
                    continue;
                }
                loListItem = new GUIListItem(loVideoInfo.Title);
                loListItem.Path = loVideoInfo.VideoUrl;
                loListItem.ItemId = liIdx;
                loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
                GUI_facadeView.Add(loListItem);
                loImageUrlList.Add(loVideoInfo.ImageUrl);
                if (!string.IsNullOrEmpty(loVideoInfo.ImageUrl))
                {
                    loListItem.RetrieveArt = false;
                    loListItem.OnRetrieveArt += new MediaPortal.GUI.Library.GUIListItem.RetrieveCoverArtHandler(OnRetrieveCoverArt);
                    numVideosWithThumb++;
                }
            }
            // fall back to list view if there are no items with thumbs
            suggestedView = null;
            if (numVideosWithThumb > 0)
                ImageDownloader.getImages(loImageUrlList, OnlineVideoSettings.getInstance().msThumbLocation, GUI_facadeView);
            else
                suggestedView = GUIFacadeControl.ViewMode.List;

            currentVideoList = foVideos;

            // position the cursor on the selected video if restore index was true
            if (selectedVideoIndex < GUI_facadeView.Count)
                GUI_facadeView.SelectedListItemIndex = selectedVideoIndex;
            else
                selectedVideoIndex = 0;

            return true;
        }

        private void ShowPreviousMenu()
        {
            Log.Info("OnShowPreviousMenu CurrentState:" + CurrentState);
            if (CurrentState == State.categories)
            {
                if (selectedCategory == null)
                {
                    DisplaySites();
                    CurrentState = State.sites;
                }
                else
                {
                    ImageDownloader._stopDownload = true;
                    if (selectedCategory.ParentCategory == null) DisplayCategories(null);
                    else DisplayCategories(selectedCategory.ParentCategory);
                }
            }
            else if (CurrentState == State.videos)
            {
                Log.Info("Set the stopDownload to true 3");
                ImageDownloader._stopDownload = true;

                if (selectedCategory == null || selectedCategory.ParentCategory == null) DisplayCategories(null);
                else DisplayCategories(selectedCategory.ParentCategory);

                CurrentState = State.categories;
                currentVideosDisplayMode = VideosMode.Category;
            }
            else if (CurrentState == State.details)
            {
                ///------------------------------------------------------------------------
                /// 2009-05-31 MichelC
                /// For some reason, without like, the menu functionality gets weird after
                /// viewing the Apple Trailer Details section in Blue3 & Blue3Wide skins.
                ///------------------------------------------------------------------------
                GUIControl.UnfocusControl(GetID, GUI_infoList.GetID);
                GUI_infoList.Focus = false;
                ///------------------------------------------------------------------------

                ImageDownloader._stopDownload = true;
                SetVideoListToFacade(currentVideoList, true);
                CurrentState = State.videos;
                SetFacadeViewMode();
            }
            UpdateViewState();
        }

        private void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            GUIFilmstripControl filmstrip = parent as GUIFilmstripControl;
            if (filmstrip != null) filmstrip.InfoImageFileName = item.ThumbnailImage;

            string desc = OnlineVideoSettings.getInstance().SiteList[item.Label].Settings.Description;
            if (!string.IsNullOrEmpty(desc)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", desc);
            else GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);
        }

        void OnRetrieveCoverArt(GUIListItem item)
        {
            if (ImageDownloader._imageLocationList.Count > item.ItemId - 1)
            {
                item.ThumbnailImage = ImageDownloader._imageLocationList[item.ItemId - 1];
                item.IconImage = ImageDownloader._imageLocationList[item.ItemId - 1];
                item.IconImageBig = ImageDownloader._imageLocationList[item.ItemId - 1];
            }
        }

        void OnVideoItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item.ItemId == 0) SetVideoInfoGuiProperties(null);
            else SetVideoInfoGuiProperties(currentVideoList[item.ItemId - 1]);
        }

        private bool GetUserInputString(ref string sString, bool password)
        {
            VirtualKeyboard keyBoard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            keyBoard.Reset();
            keyBoard.IsSearchKeyboard = true;
            keyBoard.Text = sString;
            keyBoard.Password = password;
            keyBoard.DoModal(GetID); // show it...
            if (keyBoard.IsConfirmed) sString = keyBoard.Text;
            return keyBoard.IsConfirmed;
        }

        private void Play(VideoInfo foListItem)
        {
            bool playing = false;
            string lsUrl = "";
            if (selectedSite.MultipleFilePlay)
            {
                List<String> loUrlList = null;
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    loUrlList = selectedSite.getMultipleVideoUrls(foListItem);
                }, "getting multiple urls for video"))
                {
                    return;
                }

                if (loUrlList == null || loUrlList.Count == 0)
                {
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    dlg.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                    dlg.SetText("Unable to play the video. No URL.");
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                    return;
                }

                // stop player if currently playing some other video
                if (g_Player.Playing) g_Player.Stop();

                //PlayListPlayer.SingletonPlayer.Init(); // re.registers Eventhandler among other for PlaybackStoppt, so will call play twice later!
                PlayListPlayer.SingletonPlayer.Reset();
                PlayListPlayer.SingletonPlayer.g_Player = new Player.PlaylistPlayerWrapper(selectedSite.Settings.Player);
                PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
                videoList.Clear();
                int i = 0;
                foreach (String url in loUrlList)
                {
                    i++;
                    PlayListItem item = new PlayListItem(string.Format("{0} - {1} / {2}", foListItem.Title, i.ToString(), loUrlList.Count), url);
                    videoList.Add(item);
                }
                lsUrl = loUrlList[0];

                PlayListPlayer.SingletonPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO_TEMP;
                playing = PlayListPlayer.SingletonPlayer.Play(0);
            }
            else
            {
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    lsUrl = selectedSite.getUrl(foListItem);
                }, "getting url for video"))
                {
                    return;
                }

                if (foListItem.PlaybackOptions != null) lsUrl = DisplayPlaybackOptions(foListItem, lsUrl);
                if (lsUrl == "-1") return;

                if (String.IsNullOrEmpty(lsUrl) || !(Uri.IsWellFormedUriString(lsUrl, UriKind.Absolute) || System.IO.Path.IsPathRooted(lsUrl)))
                {
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    dlg.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                    dlg.SetText("Unable to play the video. Invalid URL.");
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                    return;
                }
                // stop player if currently playing some other video
                if (g_Player.Playing) g_Player.Stop();

                // we use our own factory, so store the one currently used
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new OnlineVideos.Player.PlayerFactory(selectedSite.Settings.Player);
                playing = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                // restore the factory
                g_Player.Factory = savedFactory;
            }

            if (playing && g_Player.Player != null && g_Player.IsVideo)
            {
                GUIGraphicsContext.IsFullScreenVideo = true;
                GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);

                // wait until the internal g_Player Code sets the title
                int maxWaits = 10;
                while (maxWaits > 0 && (GUIPropertyManager.GetProperty("#Play.Current.Title") == "" || !lsUrl.Contains(GUIPropertyManager.GetProperty("#Play.Current.Title"))))
                {
                    maxWaits--;
                    System.Diagnostics.Debug.WriteLine(maxWaits);
                    GUIWindowManager.Process();
                }
                // and after that set our title
                GUIPropertyManager.SetProperty("#Play.Current.Title", foListItem.Title);

                if (foListItem.StartTime != String.Empty)
                {
                    Log.Info("Found starttime: {0}", foListItem.StartTime);
                    double seconds = foListItem.GetSecondsFromStartTime();
                    Log.Info("SeekingAbsolute: {0}", seconds);
                    g_Player.SeekAbsolute(seconds);
                }
            }
        }

        private void PlayAll()
        {
            bool playing = false;
            //PlayListPlayer.SingletonPlayer.Init(); // re.registers Eventhandler among other for PlaybackStoppt, so will call play twice later!
            PlayListPlayer.SingletonPlayer.Reset();
            PlayListPlayer.SingletonPlayer.g_Player = new Player.PlaylistPlayerWrapper(selectedSite.Settings.Player);
            PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
            videoList.Clear();
            List<VideoInfo> loVideoList = selectedSite.HasMultipleVideos ? currentTrailerList : currentVideoList;
            bool firstAdded = false;
            foreach (VideoInfo loVideo in loVideoList)
            {
                string lsUrl = "";
                if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
                {
                    lsUrl = selectedSite.getUrl(loVideo);
                }, "getting url for video"))
                {
                    continue;
                }
                PlayListItem item = new PlayListItem(loVideo.Title, lsUrl);
                videoList.Add(item);
                Log.Info("GUIOnlineVideos.playAll:Added {0} to playlist", loVideo.Title);
                if (!firstAdded)
                {
                    firstAdded = true;
                    PlayListPlayer.SingletonPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO_TEMP;
                    if (PlayListPlayer.SingletonPlayer.Play(0))
                    {
                        playing = true;
                    }
                    if (playing)
                    {
                        Log.Info("GUIOnlineVideos.playAll:Playing first video.");
                        GUIGraphicsContext.IsFullScreenVideo = true;
                        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);
                    }
                }
            }
        }

        private void SaveVideo(VideoInfo video)
        {
            string url = "";
            if (!Gui2UtilConnector.Instance.ExecuteInBackgroundAndWait(delegate()
            {
                url = selectedSite.getUrl(video);
            }, "getting url for video"))
            {
                return;
            }

            if (video.PlaybackOptions != null) url = DisplayPlaybackOptions(video, url);
            if (url == "-1") return;

            if (String.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                dlg.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                dlg.SetText("Unable to download the video. Invalid URL.");
                dlg.DoModal(GUIWindowManager.ActiveWindow);
                return;
            }

            if (currentDownloads.ContainsKey(url))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                dlg.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                dlg.SetText("Already downloading this file.");
                dlg.DoModal(GUIWindowManager.ActiveWindow);
                return;
            }

            DownloadInfo downloadInfo = new DownloadInfo()
            {
                Url = url,
                Title = video.Title,
                LocalFile = System.IO.Path.Combine(OnlineVideoSettings.getInstance().msDownloadDir, selectedSite.GetFileNameForDownload(video, url)),
                ThumbFile = ImageDownloader.GetThumbFile(video.ImageUrl)
            };

            lock (GUIOnlineVideos.currentDownloads) currentDownloads.Add(url, downloadInfo); // make access threadsafe

            if (url.ToLower().StartsWith("mms://"))
            {
                System.Threading.Thread downloadThread = new System.Threading.Thread((System.Threading.ParameterizedThreadStart)delegate(object o)
                {
                    Exception exception = MMSDownloadHelper.Download(o as DownloadInfo);
                    if (exception == null) OnDownloadFileCompleted(this, new AsyncCompletedEventArgs(null, false, o as DownloadInfo));
                    else
                    {
                        Log.Error("Error downloading {0}, Msg: ", url, exception.Message);
                        OnDownloadFileCompleted(this, new AsyncCompletedEventArgs(exception, true, o as DownloadInfo));
                    }
                });
                downloadThread.IsBackground = true;
                downloadThread.Name = "OnlineVideosDownload";
                downloadThread.Start(downloadInfo);
            }
            else
            {
                // download file from web
                WebClient loClient = new WebClient();
                loClient.Headers.Add("user-agent", OnlineVideoSettings.UserAgent);
                loClient.DownloadFileCompleted += OnDownloadFileCompleted;
                loClient.DownloadFileAsync(new Uri(url), downloadInfo.LocalFile, downloadInfo);
            }
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DownloadInfo downloadInfo = e.UserState as DownloadInfo;
            lock (GUIOnlineVideos.currentDownloads) currentDownloads.Remove(downloadInfo.Url); // make access threadsafe

            if (e.Error != null)
            {
                GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                loDlgNotify.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                loDlgNotify.SetText(string.Format("Download failed: {0}", downloadInfo.Title));
                loDlgNotify.DoModal(GetID);
            }
            else
            {
                // save thumb for this video as well if it exists
                if (System.IO.File.Exists(downloadInfo.ThumbFile))
                {
                    string localImageName = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(downloadInfo.LocalFile),
                        System.IO.Path.GetFileNameWithoutExtension(downloadInfo.LocalFile))
                        + System.IO.Path.GetExtension(downloadInfo.ThumbFile);
                    System.IO.File.Copy(downloadInfo.ThumbFile, localImageName);
                }

                GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                loDlgNotify.SetHeading("Download Complete");
                loDlgNotify.SetText((e.UserState as DownloadInfo).Title);
                loDlgNotify.DoModal(GetID);
            }
        }

        private bool FilterOut(String fsStr)
        {
            if (fsStr == String.Empty)
            {
                return false;
            }
            if (OnlineVideoSettings.getInstance().msFilterArray != null)
            {
                foreach (String lsFilter in OnlineVideoSettings.getInstance().msFilterArray)
                {
                    if (fsStr.IndexOf(lsFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Info("Filtering out:{0}\n based on filter:{1}", fsStr, lsFilter);
                        return true;
                        //return false;
                    }
                }
            }
            return false;
        }

        private void UpdateViewState()
        {
            Log.Info("Updating View State");
            switch (CurrentState)
            {
                case State.sites:
                    GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(2143)/*Home*/);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/OnlineVideos.png");
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    HideVideoDetails();
                    HideFilterButtons();
                    ShowOrderButtons();
                    HideSearchButtons();
                    HideAndDisable(GUI_btnFavorite.GetID);
                    if (OnlineVideoSettings.getInstance().useAgeConfirmation && !OnlineVideoSettings.getInstance().ageHasBeenConfirmed)
                        ShowAndEnable(GUI_btnEnterPin.GetID);
                    else
                        HideAndDisable(GUI_btnEnterPin.GetID);
                    SetVideoInfoGuiProperties(null);
                    currentView = currentSiteView;
                    SetFacadeViewMode();
                    break;
                case State.categories:
                    string cat_headerlabel = selectedCategory != null ? selectedCategory.Name : selectedSite.Settings.Name;
                    GUIPropertyManager.SetProperty("#header.label", cat_headerlabel);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    HideVideoDetails();
                    HideFilterButtons();
                    if (selectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    if (selectedSite is IFavorite) ShowAndEnable(GUI_btnFavorite.GetID); else HideAndDisable(GUI_btnFavorite.GetID);
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetVideoInfoGuiProperties(null);
                    currentView = suggestedView != null ? suggestedView.Value : currentCategoryView;
                    SetFacadeViewMode();
                    break;
                case State.videos:
                    switch (currentVideosDisplayMode)
                    {
                        case VideosMode.Favorites: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(932)); break;
                        case VideosMode.Search: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(283)); break;
                        case VideosMode.Related: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(33011)); break;
                        default:
                            {
                                string proposedLabel = selectedSite.getCurrentVideosTitle();
                                GUIPropertyManager.SetProperty("#header.label", proposedLabel != null ? proposedLabel : selectedCategory != null ? selectedCategory.Name : ""); break;
                            }
                    }
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    ShowAndEnable(GUI_facadeView.GetID);
                    if (selectedSite.HasNextPage) ShowAndEnable(GUI_btnNext.GetID); else HideAndDisable(GUI_btnNext.GetID);
                    if (selectedSite.HasPreviousPage) ShowAndEnable(GUI_btnPrevious.GetID); else HideAndDisable(GUI_btnPrevious.GetID);
                    HideVideoDetails();
                    if (selectedSite is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    HideSearchButtons();
                    if (selectedSite.HasFilterCategories) ShowCategoryButton();
                    HideAndDisable(GUI_btnFavorite.GetID);
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetVideoInfoGuiProperties(selectedVideo);
                    currentView = suggestedView != null ? suggestedView.Value : currentVideoView;
                    SetFacadeViewMode();
                    break;
                case State.details:
                    GUIPropertyManager.SetProperty("#header.label", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    HideAndDisable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    ShowVideoDetails();
                    HideFilterButtons();
                    HideSearchButtons();
                    HideAndDisable(GUI_btnFavorite.GetID);
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetVideoInfoGuiProperties(null);
                    break;
            }
            if (CurrentState == State.details)
            {
                GUI_infoList.Focus = true;
                GUIControl.FocusControl(GetID, GUI_infoList.GetID);
            }
            else
            {
                // set the selected item to -1 and afterwards back, so the displayed title gets refreshed
                int temp = GUI_facadeView.SelectedListItemIndex;
                GUI_facadeView.SelectedListItemIndex = -1;
                GUI_facadeView.SelectedListItemIndex = temp;

                GUI_facadeView.Focus = true;
                GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
            }
        }

        private void HideVideoDetails()
        {
            HideAndDisable(23);
            HideAndDisable(24);
            HideAndDisable(51);
            HideAndDisable(52);
            HideAndDisable(53);
            HideAndDisable(54);
            HideAndDisable(55);
            HideAndDisable(56);
            HideAndDisable(57);
            HideAndDisable(58);
            HideAndDisable(59);
        }

        private void ShowVideoDetails()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.movieposter", ImageDownloader.downloadPoster(selectedVideo.Tags, selectedVideo.Title, OnlineVideoSettings.getInstance().msThumbLocation));
            GUIPropertyManager.SetProperty("#OnlineVideos.trailerdesc", selectedVideo.Description);
            Sites.AppleTrailersUtil.Trailer info = selectedVideo.Other as Sites.AppleTrailersUtil.Trailer;
            if (info != null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.genre", info.Genres.ToString());
                GUIPropertyManager.SetProperty("#OnlineVideos.releasedate", info.ReleaseDate.ToShortDateString());
                GUIPropertyManager.SetProperty("#OnlineVideos.cast", info.Cast.ToString());
                ShowAndEnable(52);
                ShowAndEnable(53);
                ShowAndEnable(54);
                ShowAndEnable(55);
                ShowAndEnable(56);
                ShowAndEnable(57);
            }
            else
            {
                HideAndDisable(52);
                HideAndDisable(53);
                HideAndDisable(54);
                HideAndDisable(55);
                HideAndDisable(56);
                HideAndDisable(57);
            }
            ShowAndEnable(23);
            ShowAndEnable(24);
            ShowAndEnable(51);
            ShowAndEnable(58);
            ShowAndEnable(59);
        }

        private void ShowOrderButtons()
        {
            ShowAndEnable(GUI_btnOrderBy.GetID);
            ShowAndEnable(GUI_btnUpdate.GetID);
        }

        private void HideFilterButtons()
        {
            HideAndDisable(GUI_btnMaxResult.GetID);
            HideAndDisable(GUI_btnTimeFrame.GetID);
            HideAndDisable(GUI_btnOrderBy.GetID);
            HideAndDisable(GUI_btnUpdate.GetID);
        }

        private void ShowFilterButtons()
        {
            Log.Debug("Showing Filter buttons");
            GUI_btnMaxResult.Clear();
            GUI_btnOrderBy.Clear();
            GUI_btnTimeFrame.Clear();

            moSupportedMaxResultList = ((IFilter)selectedSite).getResultSteps();
            foreach (int step in moSupportedMaxResultList)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnMaxResult.GetID, step + "");
            }
            moSupportedOrderByList = ((IFilter)selectedSite).getOrderbyList();
            foreach (String orderBy in moSupportedOrderByList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, orderBy);
            }
            moSupportedTimeFrameList = ((IFilter)selectedSite).getTimeFrameList();
            foreach (String time in moSupportedTimeFrameList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnTimeFrame.GetID, time);
            }

            ShowAndEnable(GUI_btnMaxResult.GetID);
            ShowAndEnable(GUI_btnOrderBy.GetID);
            ShowAndEnable(GUI_btnTimeFrame.GetID);
            ShowAndEnable(GUI_btnUpdate.GetID);

            if (SelectedMaxResultIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnMaxResult.GetID, SelectedMaxResultIndex);
            }
            if (SelectedOrderByIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnOrderBy.GetID, SelectedOrderByIndex);
            }
            if (SelectedTimeFrameIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnTimeFrame.GetID, SelectedTimeFrameIndex);
            }
        }

        private void HideSearchButtons()
        {
            Log.Debug("Hiding Search buttons");
            HideAndDisable(GUI_btnSearchCategories.GetID);
            HideAndDisable(GUI_btnSearch.GetID);
        }

        private void ShowSearchButtons()
        {
            Log.Debug("Showing Search buttons");
            GUI_btnSearchCategories.Clear();
            moSupportedSearchCategoryList = selectedSite.GetSearchableCategories();
            GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, "All");
            foreach (String category in moSupportedSearchCategoryList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, category);
            }
            if (moSupportedSearchCategoryList.Count > 1)
            {
                ShowAndEnable(GUI_btnSearchCategories.GetID);
            }
            ShowAndEnable(GUI_btnSearch.GetID);
            if (SelectedSearchCategoryIndex > -1)
            {
                Log.Info("restoring search category...");
                GUIControl.SelectItemControl(GetID, GUI_btnSearchCategories.GetID, SelectedSearchCategoryIndex);
                Log.Info("Search category restored to " + GUI_btnSearchCategories.SelectedLabel);
            }
        }

        private void ShowCategoryButton()
        {
            Log.Debug("Showing Category button");
            GUI_btnSearchCategories.Clear();
            moSupportedSearchCategoryList = selectedSite.GetSearchableCategories();
            foreach (String category in moSupportedSearchCategoryList.Keys)
                GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, category);
            if (moSupportedSearchCategoryList.Count > 1)
            {
                ShowAndEnable(GUI_btnSearchCategories.GetID);
                ShowAndEnable(GUI_btnUpdate.GetID);
            }
        }
                       
        private void ToggleFacadeViewMode()
        {
            switch (currentView)
            {
                case GUIFacadeControl.ViewMode.List:
                    currentView = GUIFacadeControl.ViewMode.SmallIcons; break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    currentView = GUIFacadeControl.ViewMode.LargeIcons; break;
                case GUIFacadeControl.ViewMode.LargeIcons:
                    currentView = GUIFacadeControl.ViewMode.List; break;
            }
            switch (CurrentState)
            {
                case State.sites: currentSiteView = currentView; break;
                case State.categories: currentCategoryView = currentView; break;
                case State.videos: currentVideoView = currentView; break;
            }
            if (CurrentState != State.details) SetFacadeViewMode();
        }

        protected void SetFacadeViewMode()
        {
            if (GUI_facadeView == null) return;

            string strLine = String.Empty;
            switch (currentView)
            {
                case GUIFacadeControl.ViewMode.List:
                    strLine = GUILocalizeStrings.Get(101);
                    break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    strLine = GUILocalizeStrings.Get(100);
                    break;
                case GUIFacadeControl.ViewMode.LargeIcons:
                    strLine = GUILocalizeStrings.Get(417);
                    break;
            }
            GUIControl.SetControlLabel(GetID, GUI_btnViewAs.GetID, strLine);

            // keep track of the currently selected item (is lost when switching view)
            int rememberIndex = GUI_facadeView.SelectedListItemIndex;
            GUI_facadeView.View = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
            if (rememberIndex > -1) GUIControl.SelectItemControl(GetID, GUI_facadeView.GetID, rememberIndex);
        }

        private void SetVideoInfoGuiProperties(VideoInfo foVideo)
        {
            if (foVideo == null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.length", String.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);
            }
            else
            {
                if (String.IsNullOrEmpty(foVideo.Length))
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.length", "None");
                }
                else
                {
                    double ldLength;
                    if (Double.TryParse(foVideo.Length, out ldLength))
                    {
                        TimeSpan t = TimeSpan.FromSeconds(ldLength);
                        GUIPropertyManager.SetProperty("#OnlineVideos.length", t.ToString());
                    }
                    else
                    {
                        GUIPropertyManager.SetProperty("#OnlineVideos.length", foVideo.Length);
                    }
                }
                if (String.IsNullOrEmpty(foVideo.Description))
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.desc", "None");
                }
                else
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.desc", foVideo.Description);
                }
            }
        }

        private void AddOrRemoveFavorite(VideoInfo loSelectedVideo)
        {
            if (selectedSite is IFavorite)
            {
                if (currentVideosDisplayMode == VideosMode.Favorites)
                {
                    Log.Info("Received request to remove video from favorites.");
                    ((IFavorite)selectedSite).removeFavorite(loSelectedVideo);
                    DisplayVideos_Favorite(); // retrieve favorites again and show the updated list
                }
                else
                {
                    Log.Info("Received request to add video to favorites.");
                    ((IFavorite)selectedSite).addFavorite(loSelectedVideo);
                }
            }
            else
            {
                OnlineVideos.Database.FavoritesDatabase db = OnlineVideos.Database.FavoritesDatabase.getInstance();

                if (selectedSite is Sites.FavoriteUtil)
                {
                    db.removeFavoriteVideo(loSelectedVideo);
                    DisplayVideos_Category(true); // retrieve videos again and show the updated list
                }
                else
                {
                    db.addFavoriteVideo(loSelectedVideo, selectedSite.Settings.Name);
                }
            }
        }

        private string DisplayPlaybackOptions(VideoInfo videoInfo, string defaultUrl)
        {
            // with no options set, return the VideoUrl field
            if (videoInfo.PlaybackOptions == null || videoInfo.PlaybackOptions.Count == 0) return videoInfo.VideoUrl;
            // with just one option set, return that options url
            if (videoInfo.PlaybackOptions.Count == 1)
            {
                var enumer = videoInfo.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            int defaultOption = -1;
            // show a list of available options and let the user decide
            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlgSel != null)
            {
                dlgSel.Reset();
                dlgSel.SetHeading(GUILocalizeStrings.Get(2201)/*Select Source*/);
                int option = 0;
                foreach (string key in videoInfo.PlaybackOptions.Keys)
                {
                    dlgSel.Add(key);
                    if (videoInfo.PlaybackOptions[key] == defaultUrl) defaultOption = option;
                    option++;
                }
            }
            if (defaultOption != -1) dlgSel.SelectedLabel = defaultOption;
            dlgSel.DoModal(GetID);
            if (dlgSel.SelectedId == -1) return "-1";
            return videoInfo.PlaybackOptions[dlgSel.SelectedLabelText];
        }

        #endregion
    }
}