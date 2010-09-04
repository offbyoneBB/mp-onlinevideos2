using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using OnlineVideos.MediaPortal1.Player;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using Action = MediaPortal.GUI.Library.Action;
using MediaPortal.Profile;

namespace OnlineVideos.MediaPortal1
{
    [PluginIcons("OnlineVideos.MediaPortal1.OnlineVideos.png", "OnlineVideos.MediaPortal1.OnlineVideosDisabled.png")]
    public class GUIOnlineVideos : GUIWindow, ISetupForm, IShowPlugin
    {
        public const int WindowId = 4755;

        public enum SiteOrder { AsInFile = 0, Name = 1, Language = 2 }

        public enum State { sites = 0, categories = 1, videos = 2, details = 3 }

        public enum VideosMode { Category = 0, Favorites = 1, Search = 2, Related = 3 }

        #region IShowPlugin Implementation

        bool IShowPlugin.ShowDefaultHome()
        {
            return true;
        }

        #endregion

        #region ISetupForm Implementation

        string ISetupForm.Author()
        {
            return "offbyone";
        }

        bool ISetupForm.CanEnable()
        {
            return true;
        }

        bool ISetupForm.DefaultEnabled()
        {
            return true;
        }

        string ISetupForm.Description()
        {
            return "Browse videos from various online sites.";
        }

        bool ISetupForm.GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = PluginConfiguration.Instance.BasicHomeScreenName;
            strButtonImage = String.Empty;
            strButtonImageFocus = String.Empty;
            strPictureImage = @"hover_OnlineVideos.png";
            return true;
        }

        int ISetupForm.GetWindowId()
        {
            return GetID;
        }

        bool ISetupForm.HasSetup()
        {
            return true;
        }

        string ISetupForm.PluginName()
        {
            return PluginConfiguration.PLUGIN_NAME;
        }

        /// <summary>
        /// Show Plugin Configuration Dialog.
        /// </summary>
        void ISetupForm.ShowPlugin()
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
        #region CurrentState
        State currentState = State.sites;
        State CurrentState
        {
            get { return currentState; }
            set { currentState = value; GUIPropertyManager.SetProperty("#OnlineVideos.state", value.ToString()); }
        }
        #endregion
        #region SelectedSite
        Sites.SiteUtilBase selectedSite;
        Sites.SiteUtilBase SelectedSite
        {
            get { return selectedSite; }
            set
            {
                selectedSite = value;
                if (selectedSite == null)
                    ResetSelectedSite();
                else
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", selectedSite.Settings.Name);
                    GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", selectedSite.Settings.UtilName);
                }
            }
        }
        #endregion
        #region Buffering
        OnlineVideos.MediaPortal1.Player.PlayerFactory _bufferingPlayerFactory = null;
        OnlineVideos.MediaPortal1.Player.PlayerFactory BufferingPlayerFactory
        {
            get { return _bufferingPlayerFactory; }
            set 
            { 
                _bufferingPlayerFactory = value;
                GUIPropertyManager.SetProperty("#OnlineVideos.buffered", "0");
                GUIPropertyManager.SetProperty("#OnlineVideos.IsBuffering", (value != null).ToString()); 
            }
        }
        #endregion

        Category selectedCategory;
        VideoInfo selectedVideo;

        bool preventDialogOnLoad = false;
        bool firstLoadDone = false;

        int selectedClipIndex = 0;  // used to remember the position the last selected Trailer

        VideosMode currentVideosDisplayMode = VideosMode.Category;
        SiteOrder siteOrder = SiteOrder.AsInFile;

        List<VideoInfo> currentVideoList = new List<VideoInfo>();
        List<VideoInfo> currentTrailerList = new List<VideoInfo>();
        List<Player.PlayListItem> currentPlaylist = null;
        int currentPlaylistIndex = 0;

        SmsT9Filter currentFilter = new SmsT9Filter();        
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
        string lastSearchQuery = string.Empty;
        string lastSearchCategory;
        #endregion

        #region GUIWindow Overrides

        public override string GetModuleName()
        {
            return PluginConfiguration.Instance.BasicHomeScreenName;
        }

        public override int GetID
        {
            get { return WindowId; }
            set { base.GetID = value; }
        }

        public override bool Init()
        {
            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideos.xml");
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", " "); GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.length", " "); GUIPropertyManager.SetProperty("#OnlineVideos.length", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", " "); GUIPropertyManager.SetProperty("#OnlineVideos.filter", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", " "); GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", " "); GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", string.Empty);
            CurrentState = State.sites;
            // get last active module settings  
            using (MediaPortal.Profile.Settings settings = new MediaPortal.Profile.MPSettings())
            {
                bool lastActiveModuleSetting = settings.GetValueAsBool("general", "showlastactivemodule", false);
                int lastActiveModule = settings.GetValueAsInt("general", "lastactivemodule", -1);
                preventDialogOnLoad = (lastActiveModuleSetting && (lastActiveModule == GetID));
            }
            return result;
        }

        protected override void OnPageLoad()
        {
            if (!firstLoadDone) DoFirstLoad();

            base.OnPageLoad(); // let animations run

            // everytime the plugin is shown, after some other window was shown
            if (OnlineVideoSettings.Instance.AgeConfirmed && PreviousWindowId == 0)
            {
                // if a pin was inserted before, reset to false and show the home page in case the user was browsing some adult site last                
                OnlineVideoSettings.Instance.AgeConfirmed = false;
                Log.Instance.Debug("Age Confirmed set to false.");
                if (SelectedSite != null && SelectedSite.Settings.ConfirmAge)
                {
                    CurrentState = State.sites;
                    SelectedSite = null;
                }
            }

            Log.Instance.Debug("OnPageLoad. CurrentState:" + CurrentState);
            if (CurrentState == State.sites)
            {
                DisplaySites();
            }
            else if (CurrentState == State.categories)
            {
                DisplayCategories(selectedCategory);
            }
            else if (CurrentState == State.videos)
            {
                SetVideosToFacade(currentVideoList, currentVideosDisplayMode);
            }
            else
            {
                DisplayDetails(selectedVideo);
                if (selectedClipIndex < GUI_infoList.Count) GUI_infoList.SelectedListItemIndex = selectedClipIndex;
            }
        }

        protected override void OnShowContextMenu()
        {
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. getting next page videos to finish

            int liSelected = GUI_facadeView.SelectedListItemIndex - 1;

            if (CurrentState == State.details && SelectedSite is IChoice) liSelected = GUI_infoList.SelectedListItemIndex - 1;

            if (liSelected < 0 || CurrentState == State.sites || CurrentState == State.categories || (SelectedSite is IChoice && CurrentState == State.videos))
            {
                return;
            }
            List<string> dialogOptions = new List<string>();
            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlgSel == null) return;
            dlgSel.Reset();
            dlgSel.SetHeading(Translation.Actions);
            dlgSel.Add(Translation.PlayAll);
            dialogOptions.Add("PlayAll");
            if (currentVideosDisplayMode != VideosMode.Favorites && !(SelectedSite is Sites.FavoriteUtil))
            {
                if (!(SelectedSite is Sites.DownloadedVideoUtil))
                {
                    dlgSel.Add(Translation.AddToFavourites);
                    dialogOptions.Add("AddToFav");
                }                
            }
            else
            {
                dlgSel.Add(Translation.RemoveFromFavorites);
                dialogOptions.Add("RemoveFromFav");
            }
            if (SelectedSite is IRelated)
            {
                dlgSel.Add(Translation.RelatedVideos);
                dialogOptions.Add("RelatedVideos");
            }
            if (!(SelectedSite is Sites.DownloadedVideoUtil))
            {
                dlgSel.Add(Translation.Download);
                dialogOptions.Add("Download");
            }
            VideoInfo loSelectedVideo = CurrentState == State.videos ? currentVideoList[liSelected] : currentTrailerList[liSelected];
            List<string> siteSpecificEntries = SelectedSite.GetContextMenuEntries(selectedCategory, loSelectedVideo);
            if (siteSpecificEntries != null) foreach (string entry in siteSpecificEntries) {dlgSel.Add(entry); dialogOptions.Add(entry);}
            dlgSel.DoModal(GetID);
            if (dlgSel.SelectedId == -1) return;            
            switch (dialogOptions[dlgSel.SelectedId - 1])
            {
                case "PlayAll":
                    PlayAll();
                    break;
                case "AddToFav":
                    string suggestedTitle = SelectedSite.GetFileNameForDownload(loSelectedVideo, selectedCategory, null);
                    FavoritesDatabase.Instance.addFavoriteVideo(loSelectedVideo, suggestedTitle, SelectedSite.Settings.Name);                    
                    break;                
                case "RemoveFromFav":
                    FavoritesDatabase.Instance.removeFavoriteVideo(loSelectedVideo);
                    DisplayVideos_Category(selectedCategory, true); // retrieve videos again and show the updated list
                    break;
                case "RelatedVideos":
                    DisplayVideos_Related(loSelectedVideo);
                    break;
                case "Download":
                    SaveVideo_Step1(loSelectedVideo);
                    break;
                default:
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        return SelectedSite.ExecuteContextMenuEntry(selectedCategory, loSelectedVideo, dialogOptions[dlgSel.SelectedId - 1]);
                    },
                    delegate(bool success, object result)
                    {
                        if (success && result != null && (bool)result) DisplayVideos_Category(selectedCategory, true);
                    }, ": " + dialogOptions[dlgSel.SelectedId - 1], true);
                    break;
            }
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_STOP:
                    if (BufferingPlayerFactory != null)
                    {
                        ((OnlineVideosPlayer)BufferingPlayerFactory.PreparedPlayer).StopBuffering();
                        Gui2UtilConnector.Instance.StopBackgroundTask();
                        return;
                    }
                    break;
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    if (!currentFilter.IsEmpty())
                    {
                        currentFilter.Clear();
                        switch (CurrentState)
                        {
                            case State.sites: DisplaySites(); break;
                            case State.categories: DisplayCategories(selectedCategory); break;
                            case State.videos: SetVideosToFacade(currentVideoList, currentVideosDisplayMode); break;
                        }
                        return;
                    }
                    if (BufferingPlayerFactory != null)
                    {
                        ((OnlineVideosPlayer)BufferingPlayerFactory.PreparedPlayer).StopBuffering();
                        Gui2UtilConnector.Instance.StopBackgroundTask();
                        return;
                    }                    
                    if (CurrentState != State.sites)
                    {
                        if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish

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
                    break;
                case Action.ActionType.ACTION_KEY_PRESSED:
                    if (GUI_facadeView.Visible && GUI_facadeView.Focus)
                    {
                        // search items (starting from current selected) by title and select first found one
                        char pressedChar = (char)action.m_key.KeyChar;
                        if (char.IsDigit(pressedChar) || (pressedChar == '\b' && !currentFilter.IsEmpty()))
                        {                            
                            currentFilter.Add(pressedChar);
                            switch (CurrentState)
                            {
                                case State.sites: DisplaySites(); break;
                                case State.categories: DisplayCategories(selectedCategory); break;
                                case State.videos: SetVideosToFacade(currentVideoList, currentVideosDisplayMode); break;
                            }
                            return;
                        }
                        else
                        {
                            if (PluginConfiguration.Instance.useQuickSelect && char.IsLetterOrDigit(pressedChar))
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
                    }
                    break;
                case Action.ActionType.ACTION_NEXT_ITEM:
                    if (currentState == State.videos && GUI_facadeView.Visible && GUI_facadeView.Focus)
                    {
                        currentFilter.Clear();
                        if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish

                        if (GUI_btnNext.IsEnabled)
                        {
                            DisplayVideos_NextPage();
                        }
                    }
                    break;
                case Action.ActionType.ACTION_PREV_ITEM:
                    if (currentState == State.videos && GUI_facadeView.Visible && GUI_facadeView.Focus)
                    {
                        currentFilter.Clear();
                        if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish

                        if (GUI_btnPrevious.IsEnabled)
                        {
                            DisplayVideos_PreviousPage();
                        }
                    }
                    break;
            }
            GUI_btnOrderBy.Label = Translation.SortOptions;
            GUI_btnMaxResult.Label = Translation.MaxResults;
            GUI_btnSearchCategories.Label = Translation.Category;
            GUI_btnTimeFrame.Label = Translation.Timeframe;
            base.OnAction(action);
        }

        public override bool OnMessage(GUIMessage message)
        {
            if (message.Object is Gui2UtilConnector)
            {
                (message.Object as Gui2UtilConnector).ExecuteTaskResultHandler();
                return true;
            }

            if (message.Object is GUIOnlineVideoFullscreen)
            {
                if (message.Param1 == 1)
                {
                    // move to next
                    if (currentPlaylist != null && currentPlaylist.Count > currentPlaylistIndex + 1)
                    {
                        currentPlaylistIndex++;
                        Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                        return true;
                    }
                }
                else if (message.Param1 == -1)
                {
                    // move to previous
                    if (currentPlaylist != null && currentPlaylistIndex - 1 >= 0)
                    {
                        currentPlaylistIndex--;
                        Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                        return true;
                    }
                }
            }

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
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish
            if (control == GUI_facadeView && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                currentFilter.Clear();
                if (CurrentState == State.sites)
                {
                    SelectedSite = OnlineVideoSettings.Instance.SiteUtilsList[GUI_facadeView.SelectedListItem.Path];
                    DisplayCategories(null);
                }
                else if (CurrentState == State.categories)
                {
                    if (GUI_facadeView.SelectedListItemIndex == 0)
                    {
                        ShowPreviousMenu();
                    }
                    else
                    {
                        Category categoryToDisplay = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as Category;
                        if (categoryToDisplay.HasSubCategories)
                        {
                            DisplayCategories(categoryToDisplay);
                        }
                        else
                        {
                            DisplayVideos_Category(categoryToDisplay, false);
                        }
                    }
                }
                else if (CurrentState == State.videos)
                {
                    ImageDownloader.StopDownload = true;
                    if (GUI_facadeView.SelectedListItemIndex == 0)
                    {
                        ShowPreviousMenu();
                    }
                    else
                    {
                        selectedVideo = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo;
                        if (SelectedSite is IChoice)
                        {
                            // show details view
                            DisplayDetails(selectedVideo);
                        }
                        else
                        {
                            //play the video
                            currentPlaylist = null;
                            currentPlaylistIndex = 0;

                            Play_Step1(new PlayListItem(null, null)
                                    {
                                        Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                                        Video = selectedVideo,
                                        Util = selectedSite
                                    }, true);
                        }
                    }
                }
            }
            else if (control == GUI_infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM && CurrentState == State.details)
            {
                ImageDownloader.StopDownload = true;
                if (GUI_infoList.SelectedListItemIndex == 0)
                {
                    ShowPreviousMenu();
                }
                else
                {
                    selectedClipIndex = GUI_infoList.SelectedListItemIndex;
                    //play the video
                    currentPlaylist = null;
                    currentPlaylistIndex = 0;
                    Play_Step1(new PlayListItem(null, null)
                    {
                        Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                        Video = (GUI_infoList.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo,
                        Util = selectedSite
                    }, true);
                }
            }
            else if (control == GUI_btnViewAs)
            {
                ToggleFacadeViewMode();
            }
            else if (control == GUI_btnNext)
            {
                GUIControl.UnfocusControl(GetID, GUI_btnNext.GetID);
                DisplayVideos_NextPage();
            }
            else if (control == GUI_btnPrevious)
            {
                GUIControl.UnfocusControl(GetID, GUI_btnPrevious.GetID);
                DisplayVideos_PreviousPage();
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
            }
            else if (control == GUI_btnSearchCategories)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnSearchCategories.GetID, GUI_btnSearchCategories.SelectedItem);
            }
            else if (control == GUI_btnSearch)
            {
                string query = PluginConfiguration.Instance.rememberLastSearch ? lastSearchQuery : string.Empty;
                if (GetUserInputString(ref query, false))
                {
                    GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
                    if (query != String.Empty)
                    {
                        lastSearchQuery = query;
                        DisplayVideos_Search(query);
                    }
                }
            }            
            else if (control == GUI_btnEnterPin)
            {
                string pin = String.Empty;
                if (GetUserInputString(ref pin, true))
                {
                    if (pin == PluginConfiguration.Instance.pinAgeConfirmation)
                    {
                        OnlineVideoSettings.Instance.AgeConfirmed = true;
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
            // only handle if not just going to a full screen video
            if (newWindowId != Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO)
            {
                // Save view
                using (MediaPortal.Profile.Settings settings = new MediaPortal.Profile.MPSettings())
                {
                    settings.SetValue(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_SITEVIEW_MODE, (int)currentSiteView);
                    settings.SetValue(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_SITEVIEW_ORDER, (int)siteOrder);
                    settings.SetValue(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_VIDEOVIEW_MODE, (int)currentVideoView);
                    settings.SetValue(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_CATEGORYVIEW_MODE, (int)currentCategoryView);
                }

                // if a pin was inserted before, reset to false and show the home page in case the user was browsing some adult site last
                if (OnlineVideoSettings.Instance.AgeConfirmed)
                {
                    OnlineVideoSettings.Instance.AgeConfirmed = false;
                    Log.Instance.Debug("Age Confirmed set to false.");
                    if (SelectedSite != null && SelectedSite.Settings.ConfirmAge)
                    {
                        CurrentState = State.sites;
                        SelectedSite = null;
                    }
                }
            }
            base.OnPageDestroy(newWindowId);
        }

        #endregion

        #region new methods

        void DoFirstLoad()
        {
            // replace g_player's ShowFullScreenWindowVideo
            g_Player.ShowFullScreenWindowVideo = ShowFullScreenWindowHandler;
            g_Player.PlayBackEnded += new g_Player.EndedHandler(g_Player_PlayBackEnded);

            GUIPropertyManager.SetProperty("#header.label", PluginConfiguration.Instance.BasicHomeScreenName);
            Translator.TranslateSkin();
            
            if (PluginConfiguration.Instance.updateOnStart != false)
            {
                bool? doUpdate = PluginConfiguration.Instance.updateOnStart;
                if (!PluginConfiguration.Instance.updateOnStart.HasValue && !preventDialogOnLoad)
                {
                    GUIDialogYesNo dlg = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);
                    if (dlg != null)
                    {
                        dlg.Reset();
                        dlg.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                        dlg.SetLine(1, Translation.PerformAutomaticUpdate);
                        dlg.SetLine(2, Translation.UpdateAllYourSites);
                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                        doUpdate = dlg.IsConfirmed;
                    }
                }
                if (doUpdate == true)
                {
                    GUISiteUpdater guiUpdater = (GUISiteUpdater)GUIWindowManager.GetWindow(GUISiteUpdater.WindowId);
                    guiUpdater.AutoUpdate(false);
                }
            }
            if (PluginConfiguration.Instance.ThumbsAge >= 0)
            {
                GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
                if (dlgPrgrs != null)
                {
                    dlgPrgrs.Reset();
                    dlgPrgrs.DisplayProgressBar = true;
                    dlgPrgrs.ShowWaitCursor = false;
                    dlgPrgrs.DisableCancel(false);
                    dlgPrgrs.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                    dlgPrgrs.StartModal(GUIOnlineVideos.WindowId);
                    dlgPrgrs.SetLine(1, Translation.DeletingOldThumbs);
                    dlgPrgrs.Percentage = 0;
                }
                new System.Threading.Thread(delegate()
                {
                    ImageDownloader.DeleteOldThumbs(PluginConfiguration.Instance.ThumbsAge, r =>
                        {
                            dlgPrgrs.Percentage = r;
                            return dlgPrgrs.ShouldRenderLayer();
                        });
                    if (dlgPrgrs != null) { dlgPrgrs.Percentage = 100; dlgPrgrs.SetLine(1, Translation.Done); dlgPrgrs.Close(); }
                }) { Name = "OnlineVideosThumbnail", IsBackground = true }.Start();
            }
            LoadSettings();
            firstLoadDone = true;
        }

        /// <summary>
        /// This function replaces g_player.ShowFullScreenWindowVideo
        /// </summary>
        /// <returns></returns>
        private static bool ShowFullScreenWindowHandler()
        {
            if (g_Player.HasVideo && (g_Player.Player is Player.OnlineVideosPlayer || g_Player.Player is Player.WMPVideoPlayer))
            {
                if (GUIWindowManager.ActiveWindow == Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO) return true;

                Log.Instance.Info("ShowFullScreenWindow switching to fullscreen.");
                GUIWindowManager.ActivateWindow(Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                GUIGraphicsContext.IsFullScreenVideo = true;
                return true;
            }
            return g_Player.ShowFullScreenWindowVideoDefault();
        }

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
            using (Settings settings = new MPSettings())
            {
                currentSiteView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_SITEVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                siteOrder = (SiteOrder)settings.GetValueAsInt(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_SITEVIEW_ORDER, 0);
                currentVideoView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_VIDEOVIEW_MODE, (int)GUIFacadeControl.ViewMode.SmallIcons);
                currentCategoryView = (GUIFacadeControl.ViewMode)settings.GetValueAsInt(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_CATEGORYVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
            }
            OnlineVideoSettings.Instance.BuildSiteUtilsList();
        }

        private void DisplaySites()
        {
            lastSearchQuery = string.Empty;
            selectedCategory = null;
            ResetSelectedSite();
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);

            // set order by options
            GUI_btnOrderBy.Clear();
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Default);
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Name);
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Language);
            GUI_btnOrderBy.SelectedItem = (int)siteOrder;

            // get names in right order
            string[] names = new string[OnlineVideoSettings.Instance.SiteUtilsList.Count];
            switch (siteOrder)
            {
                case SiteOrder.Name:
                    OnlineVideoSettings.Instance.SiteUtilsList.Keys.CopyTo(names, 0);
                    Array.Sort(names);
                    break;
                case SiteOrder.Language:
                    Dictionary<string, List<string>> sitenames = new Dictionary<string, List<string>>();
                    foreach (Sites.SiteUtilBase aSite in OnlineVideoSettings.Instance.SiteUtilsList.Values)
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
                    OnlineVideoSettings.Instance.SiteUtilsList.Keys.CopyTo(names, 0);
                    break;
            }

            int selectedSiteIndex = 0;  // used to remember the position of the last selected site
            currentFilter.StartMatching();
            foreach (string name in names)
            {
                Sites.SiteUtilBase aSite = OnlineVideoSettings.Instance.SiteUtilsList[name];
                if (aSite.Settings.IsEnabled &&
                    (!aSite.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                {
                    OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem(aSite.Settings.Name);
                    loListItem.Label2 = aSite.Settings.Language;
                    loListItem.Path = aSite.Settings.Name;
                    loListItem.IsFolder = true;
                    loListItem.Item = aSite;
                    loListItem.OnItemSelected += OnSiteSelected;
                    // use Icon with the same name as the Site
                    string image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + aSite.Settings.Name + ".png";
                    if (!System.IO.File.Exists(image))
                    {
                        // if that does not exsist, try Icon with the same name as the Util
                        image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + aSite.Settings.UtilName + ".png";
                        if (!System.IO.File.Exists(image)) image = string.Empty;
                    }
                    if (!string.IsNullOrEmpty(image))
                    {
                        loListItem.ThumbnailImage = image;
                        loListItem.IconImage = image;
                        loListItem.IconImageBig = image;
                    }
                    else
                    {
                        Log.Instance.Debug("Icon {0} for site {1} not found", Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\" + aSite.Settings.Name + ".png", aSite.Settings.Name);
                        MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                    }
                    if (currentFilter.Matches(name))
                    {
                        if (loListItem.Item == SelectedSite) selectedSiteIndex = GUI_facadeView.Count;
                        GUI_facadeView.Add(loListItem);
                    }
                }
            }
            SelectedMaxResultIndex = -1;
            SelectedOrderByIndex = -1;
            SelectedSearchCategoryIndex = -1;
            SelectedTimeFrameIndex = -1;

            if (selectedSiteIndex < GUI_facadeView.Count)
                GUI_facadeView.SelectedListItemIndex = selectedSiteIndex;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.sites;
            UpdateViewState();
        }

        private void DisplayCategories(Category parentCategory)
        {
            if (parentCategory == null)
            {
                if (!SelectedSite.Settings.DynamicCategoriesDiscovered)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        Log.Instance.Info("Looking for dynamic categories for {0}", SelectedSite.Settings.Name);
                        int foundCategories = SelectedSite.DiscoverDynamicCategories();
                        Log.Instance.Info("Found {0} dynamic categories for {1}", foundCategories, SelectedSite.Settings.Name);
                        return SelectedSite.Settings.Categories;
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            SetCategoriesToFacade(parentCategory, result as IList<Category>);                            
                        }
                    },
                    Translation.GettingDynamicCategories, true);
                }
                else
                {
                    SetCategoriesToFacade(parentCategory, SelectedSite.Settings.Categories);                    
                }
            }
            else
            {
                if (!parentCategory.SubCategoriesDiscovered)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        Log.Instance.Info("Looking for sub categories in '{1}' on site {1}", parentCategory.Name, SelectedSite.Settings.Name);
                        int foundCategories = SelectedSite.DiscoverSubCategories(parentCategory);
                        Log.Instance.Info("Found {0} sub categories in '{1}' on site {2}", foundCategories, parentCategory.Name, SelectedSite.Settings.Name);
                        return parentCategory.SubCategories;
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            SetCategoriesToFacade(parentCategory, result as IList<Category>);
                        }
                    },
                    Translation.GettingDynamicCategories, true);
                }
                else
                {
                    SetCategoriesToFacade(parentCategory, parentCategory.SubCategories);
                }
            }
        }

        private void SetCategoriesToFacade(Category parentCategory, IList<Category> categories)
        {            
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);

            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem loListItem;
            loListItem = new OnlineVideosGuiListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_facadeView.Add(loListItem);

            int categoryIndexToSelect = (categories != null && categories.Count > 0) ? 1 : 0; // select the first category by default if there is one
            int numCategoriesWithThumb = 0;
            suggestedView = null;
            currentFilter.StartMatching();
            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    Category loCat = categories[i];
                    if (currentFilter.Matches(loCat.Name))
                    {
                        loListItem = new OnlineVideosGuiListItem(loCat.Name);
                        loListItem.IsFolder = true;
                        loListItem.ItemId = i + 1;
                        MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                        if (!string.IsNullOrEmpty(loCat.Thumb)) numCategoriesWithThumb++;
                        loListItem.Item = loCat;
                        loListItem.OnItemSelected += OnCategorySelected;
                        if (loCat == selectedCategory) categoryIndexToSelect = GUI_facadeView.Count; // select the category that was previously selected
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
                }

                if (numCategoriesWithThumb > 0) ImageDownloader.GetImages<Category>(categories);
                if (numCategoriesWithThumb <= categories.Count / 2) suggestedView = GUIFacadeControl.ViewMode.List;
            }

            GUI_facadeView.SelectedListItemIndex = categoryIndexToSelect;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.categories;
            selectedCategory = parentCategory;
            UpdateViewState();
        }

        private void DisplayDetails(VideoInfo foVideo)
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return ((IChoice)SelectedSite).getVideoChoices(foVideo);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    CurrentState = State.details;

                    // make the Thumb of the VideoInfo available to the details view
                    if (string.IsNullOrEmpty(foVideo.ImageUrl))
                        GUIPropertyManager.SetProperty("#OnlineVideos.Details.Poster", string.Empty);
                    else
                        GUIPropertyManager.SetProperty("#OnlineVideos.Details.Poster", foVideo.ImageUrl);

                    SetVideosToInfoList(result as List<VideoInfo>);
                }
            },
            Translation.GettingVideoDetails, true);
        }

        private void SetVideosToInfoList(List<VideoInfo> loVideoList)
        {
            currentTrailerList.Clear();
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            GUIControl.ClearControl(GetID, GUI_infoList.GetID);
            OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_infoList.Add(loListItem);
            if (loVideoList != null)
            {
                int liIdx = 0;
                foreach (VideoInfo loVideoInfo in loVideoList)
                {
                    liIdx++;
                    loListItem = new OnlineVideosGuiListItem(loVideoInfo.Title2);
                    loListItem.Path = loVideoInfo.VideoUrl;
                    loListItem.IconImage = "defaultVideo.png";
                    loListItem.ItemId = liIdx;
                    loListItem.Item = loVideoInfo;
                    loListItem.OnItemSelected += OnDetailsVideoItemSelected;
                    GUI_infoList.Add(loListItem);
                    currentTrailerList.Add(loVideoInfo);
                }
            }
            UpdateViewState();
        }

        private void DisplayVideos_Category(Category category, bool displayCategoriesOnError)
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return SelectedSite.getVideoList(category);
            },
            delegate(bool success, object result)
            {
                Category categoryToRestoreOnError = selectedCategory;
                selectedCategory = category;
                if (!success || !SetVideosToFacade(result as List<VideoInfo>, VideosMode.Category))
                {
                    selectedCategory = categoryToRestoreOnError;
                    if (displayCategoriesOnError)// an error occured or no videos were found -> return to the category selection if param was set
                    {
                        DisplayCategories(category.ParentCategory);
                    }
                }
            },
            Translation.GettingCategoryVideos, true);
        }
        
        private void DisplayVideos_Search(string query)
        {
            SelectedSearchCategoryIndex = GUI_btnSearchCategories.SelectedItem;
            if (query != String.Empty)
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    if (moSupportedSearchCategoryList.Count > 1 && GUI_btnSearchCategories.SelectedLabel != Translation.All)
                    {
                        string category = moSupportedSearchCategoryList[GUI_btnSearchCategories.SelectedLabel];
                        Log.Instance.Info("Searching for {0} in category {1}", query, category);
                        lastSearchCategory = category;
                        return SelectedSite.Search(query, category);
                    }
                    else
                    {
                        Log.Instance.Info("Searching for {0} in all categories ", query);
                        return SelectedSite.Search(query);
                    }
                },
                delegate(bool success, object result)
                {
                    if (success) SetVideosToFacade(result as List<VideoInfo>, VideosMode.Search);
                },
                Translation.GettingSearchResults, true);
            }
        }

        private void DisplayVideos_Related(VideoInfo video)
        {
            if (video != null)
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return ((IRelated)SelectedSite).getRelatedVideos(video);
                },
                delegate(bool success, object result)
                {
                    if (success) SetVideosToFacade(result as List<VideoInfo>, VideosMode.Related);
                },
                Translation.GettingRelatedVideos, true);
            }
        }

        private void DisplayVideos_Filter()
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

            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                if (currentVideosDisplayMode == VideosMode.Search)
                {
                    Log.Instance.Info("Filtering search result");
                    //filtering a search result
                    if (String.IsNullOrEmpty(lastSearchCategory))
                    {
                        return ((IFilter)SelectedSite).filterSearchResultList(lastSearchQuery, miMaxResult, msOrderBy, msTimeFrame);
                    }
                    else
                    {
                        return ((IFilter)SelectedSite).filterSearchResultList(lastSearchQuery, lastSearchCategory, miMaxResult, msOrderBy, msTimeFrame);
                    }
                }
                else
                {
                    if (SelectedSite.HasFilterCategories) // just for setting the category
                        return SelectedSite.Search(string.Empty, moSupportedSearchCategoryList[GUI_btnSearchCategories.SelectedLabel]);
                    if (SelectedSite is IFilter)
                        return ((IFilter)SelectedSite).filterVideoList(selectedCategory, miMaxResult, msOrderBy, msTimeFrame);
                }
                return null;
            },
            delegate(bool success, object result)
            {
                if (success) SetVideosToFacade(result as List<VideoInfo>, currentVideosDisplayMode);
            }
            , Translation.GettingFilteredVideos, true);
        }

        private void DisplayVideos_NextPage()
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return SelectedSite.getNextPageVideos();
            },
            delegate(bool success, object result)
            {
                if (success) SetVideosToFacade(result as List<VideoInfo>, currentVideosDisplayMode);
            },
            Translation.GettingNextPageVideos, true);
        }

        private void DisplayVideos_PreviousPage()
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return SelectedSite.getPreviousPageVideos();
            },
            delegate(bool success, object result)
            {
                if (success) SetVideosToFacade(result as List<VideoInfo>, currentVideosDisplayMode);
            },
            Translation.GettingPreviousPageVideos, true);
        }

        private bool SetVideosToFacade(List<VideoInfo> videos, VideosMode mode)
        {
            // Check for received data
            if (videos == null || videos.Count == 0)
            {
                GUIDialogNotify dlg_error = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg_error != null)
                {
                    dlg_error.Reset();
                    dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                    dlg_error.SetText(Translation.NoVideoFound);
                    dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                }
                return false;
            }

            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem("..");
            listItem.IsFolder = true;
            listItem.ItemId = 0;
            listItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
            MediaPortal.Util.Utils.SetDefaultIcons(listItem);
            GUI_facadeView.Add(listItem);
            // add the items
            Dictionary<string, bool> imageHash = new Dictionary<string, bool>();
            int liIdx = 0;
            currentFilter.StartMatching();

            int selectedVideoIndex = 0; // used to remember the position of the last selected Video

            foreach (VideoInfo videoInfo in videos)
            {
                liIdx++;
                videoInfo.CleanDescriptionAndTitle();
                if (!currentFilter.Matches(videoInfo.Title) || FilterOut(videoInfo.Title) || FilterOut(videoInfo.Description))
                {
                    continue;
                }
                listItem = new OnlineVideosGuiListItem(videoInfo.Title);
                listItem.Path = videoInfo.VideoUrl;
                listItem.ItemId = liIdx;
                listItem.Item = videoInfo;
                listItem.IconImage = "defaultVideo.png";
                listItem.IconImageBig = "defaultVideoBig.png";
                listItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
                GUI_facadeView.Add(listItem);

                if (listItem.Item == selectedVideo) selectedVideoIndex = GUI_facadeView.Count - 1;

                if (!string.IsNullOrEmpty(videoInfo.ImageUrl)) imageHash[videoInfo.ImageUrl] = true;
            }
            // fall back to list view if there are no items with thumbs
            if (imageHash.Count > 0) ImageDownloader.GetImages<VideoInfo>(videos);
            suggestedView = null;
            if (imageHash.Count == 0 || (videos.Count > 1 && imageHash.Count == 1)) suggestedView = GUIFacadeControl.ViewMode.List;

            // position the cursor on the selected video if restore index was true
            if (selectedVideoIndex < GUI_facadeView.Count)
                GUI_facadeView.SelectedListItemIndex = selectedVideoIndex;

            currentVideoList = videos;
            currentVideosDisplayMode = mode;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.videos;
            UpdateViewState();
            return true;
        }

        private void ShowPreviousMenu()
        {
            if (CurrentState == State.categories)
            {
                if (selectedCategory == null)
                {
                    DisplaySites();
                }
                else
                {
                    ImageDownloader.StopDownload = true;
                    DisplayCategories(selectedCategory.ParentCategory);
                }
            }
            else if (CurrentState == State.videos)
            {
                ImageDownloader.StopDownload = true;

                if (selectedCategory == null || selectedCategory.ParentCategory == null) DisplayCategories(null);
                else DisplayCategories(selectedCategory.ParentCategory);
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

                ImageDownloader.StopDownload = true;
                SetVideosToFacade(currentVideoList, currentVideosDisplayMode);
            }
        }

        void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            Sites.SiteUtilBase site = (item as OnlineVideosGuiListItem).Item as Sites.SiteUtilBase;
            string desc = site.Settings.Description;
            if (!string.IsNullOrEmpty(desc)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", desc);
            else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        void OnCategorySelected(GUIListItem item, GUIControl parent)
        {
            Category cat = (item as OnlineVideosGuiListItem).Item as Category;
            string desc = cat.Description;
            if (!string.IsNullOrEmpty(desc)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", desc);
            else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        void OnVideoItemSelected(GUIListItem item, GUIControl parent)
        {
            SetGuiProperties_SelectedVideo((item as OnlineVideosGuiListItem).Item as VideoInfo);
        }

        void OnDetailsVideoItemSelected(GUIListItem item, GUIControl parent)
        {
            SetGuiProperties_ExtendedVideoInfo((item as OnlineVideosGuiListItem).Item as VideoInfo);
        }

        private bool GetUserInputString(ref string sString, bool password)
        {
            VirtualKeyboard keyBoard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (keyBoard == null) return false;
            keyBoard.Reset();
            keyBoard.IsSearchKeyboard = true;
            keyBoard.Text = sString;
            keyBoard.Password = password;
            keyBoard.DoModal(GetID); // show it...
            if (keyBoard.IsConfirmed) sString = keyBoard.Text;
            return keyBoard.IsConfirmed;
        }

        void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (currentPlaylist != null)
            {
                if (g_Player.Player is OnlineVideosPlayer || g_Player.Player is WMPVideoPlayer)
                {
                    if (currentPlaylist.Count > currentPlaylistIndex + 1)
                    {
                        // if playing a playlist item, move to the next            
                        currentPlaylistIndex++;
                        Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                    }
                    else
                    {
                        // if last item -> clear the list
                        currentPlaylist = null;
                        currentPlaylistIndex = 0;
                    }
                }
                else
                {
                    // some other playback ended, and a playlist is still set here -> clear it
                    currentPlaylist = null;
                    currentPlaylistIndex = 0;
                }
            }
        }

        private void Play_Step1(PlayListItem playItem, bool goFullScreen)
        {
            if (!string.IsNullOrEmpty(playItem.FileName))
            {
                Play_Step2(playItem, new List<string>(new string[] { playItem.FileName }), goFullScreen);
            }
            else
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return SelectedSite.getMultipleVideoUrls(playItem.Video);
                },
                delegate(bool success, object result)
                {
                    if (success) Play_Step2(playItem, result as List<String>, goFullScreen);
                }
                , Translation.GettingPlaybackUrlsForVideo, true);
            }
        }

        private void Play_Step2(PlayListItem playItem, List<String> loUrlList, bool goFullScreen)
        {            
            // remove all invalid entries from the list of playback urls
            if (loUrlList != null)
            {
                int i = 0;
                while (i < loUrlList.Count)
                {
                    if (String.IsNullOrEmpty(loUrlList[i]) || !(Uri.IsWellFormedUriString(loUrlList[i], UriKind.Absolute) || System.IO.Path.IsPathRooted(loUrlList[i])))
                    {
                        loUrlList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            // if no valid urls were returned show error msg
            if (loUrlList == null || loUrlList.Count == 0)
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.UnableToPlayVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }
            // create playlist entries if more than one url
            if (loUrlList.Count > 1)
            {
                List<PlayListItem> playbackItems = new List<PlayListItem>();
                foreach (string url in loUrlList)
                {
                    playbackItems.Add(new PlayListItem(string.Format("{0} - {1} / {2}", playItem.Video.Title, (playbackItems.Count + 1).ToString(), loUrlList.Count), url)
                    {
                        Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                        Video = playItem.Video,
                        Util = playItem.Util
                    });
                }
                if (currentPlaylist == null)
                {
                    currentPlaylistIndex = 0;
                    currentPlaylist = playbackItems;
                }
                else
                {
                    currentPlaylist.InsertRange(currentPlaylistIndex, playbackItems);
                }
                // make the first item the current to be played now
                playItem = playbackItems[0];
                loUrlList = new List<string>(new string[] { playItem.FileName });
            }
            // if multiple quality choices are available show a selection dialogue (not on playlist playback)
            string lsUrl = loUrlList[0];
            if (currentPlaylist == null || currentPlaylist.Count == 0)
            {
                bool resolve = DisplayPlaybackOptions(playItem.Video, ref lsUrl);
                if (lsUrl == "-1") return; // the user did not chose an option but canceled the dialog
                // display wait cursor as GetPlaybackOptionUrl might do webrequests when overridden
                if (resolve)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        return playItem.Video.GetPlaybackOptionUrl(lsUrl);
                    },
                    delegate(bool success, object result)
                    {
                        if (success) Play_Step3(playItem, result as string, goFullScreen);
                    }
                    , Translation.GettingPlaybackUrlsForVideo, true);
                    return; // don't execute rest of function, we are waiting for callback
                }
            }
            Play_Step3(playItem, lsUrl, goFullScreen);
        }

        void Play_Step3(PlayListItem playItem, string lsUrl, bool goFullScreen)
        {
            // check for valid url
            if (String.IsNullOrEmpty(lsUrl) || !(Uri.IsWellFormedUriString(lsUrl, UriKind.Absolute) || System.IO.Path.IsPathRooted(lsUrl)))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.UnableToPlayVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            bool playing = false;

            // stop player if currently playing some other video
            if (g_Player.Playing) g_Player.Stop();

            // translate rtmp urls to the local proxy
            if (new Uri(lsUrl).Scheme.ToLower().StartsWith("rtmp"))
            {
                lsUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}", System.Web.HttpUtility.UrlEncode(lsUrl)));
            }

            OnlineVideos.MediaPortal1.Player.PlayerFactory factory = new OnlineVideos.MediaPortal1.Player.PlayerFactory(playItem.Util.Settings.Player, lsUrl);
            // external players cannot be created from a seperate thread
            if (factory.PreparedPlayerType != PlayerType.Internal)
            {
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = factory;
                playing = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                g_Player.Factory = savedFactory;
            }
            else
            {
                Log.Instance.Info("Preparing graph for playback of {0}", lsUrl);
                if (((OnlineVideosPlayer)factory.PreparedPlayer).PrepareGraph())
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        try
                        {
                            Log.Instance.Info("Start prebuffering ...");
                            BufferingPlayerFactory = factory;
                            if (((OnlineVideosPlayer)factory.PreparedPlayer).BufferFile())
                            {
                                Log.Instance.Info("Prebuffering finished.");
                                return factory;
                            }
                            else
                            {
                                Log.Instance.Info("Prebuffering failed.");
                                return null;
                            }
                        }                        
                        finally
                        {
                            BufferingPlayerFactory = null;
                        }
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            if (result != null)
                            {                                
                                IPlayerFactory savedFactory = g_Player.Factory;
                                g_Player.Factory = result as Player.PlayerFactory;
                                playing = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                                g_Player.Factory = savedFactory;
                                if (playing)
                                {
                                    SetGuiProperties_PlayingVideo(playItem.Video, playItem.Description);
                                    if (goFullScreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                                }
                            }
                            else
                            {
                                bool showMessage = true;
                                if (factory.PreparedPlayer is OnlineVideosPlayer && (factory.PreparedPlayer as OnlineVideosPlayer).BufferingStopped == true) showMessage = false;
                                factory.PreparedPlayer.Dispose();
                                if (showMessage)
                                {
                                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                                    if (dlg != null)
                                    {
                                        dlg.Reset();
                                        dlg.SetHeading(Translation.Error);
                                        dlg.SetText(Translation.UnableToPlayVideo);
                                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                                    }
                                }
                                return;
                            }
                        }
                        else
                        {
                            factory.PreparedPlayer.Dispose();
                        }
                    },
                    Translation.StartingPlayback, false);
                }
                else
                {
                    IPlayerFactory savedFactory = g_Player.Factory;
                    g_Player.Factory = factory;
                    playing = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                    g_Player.Factory = savedFactory;
                }
            }

            if (playing && g_Player.Player != null && g_Player.HasVideo)
            {
                if (!string.IsNullOrEmpty(playItem.Video.StartTime))
                {
                    Log.Instance.Info("Found starttime: {0}", playItem.Video.StartTime);
                    double seconds = playItem.Video.GetSecondsFromStartTime();
                    if (seconds > 0.0d)
                    {
                        Log.Instance.Info("SeekingAbsolute: {0}", seconds);
                        g_Player.SeekAbsolute(seconds);
                    }
                }
                SetGuiProperties_PlayingVideo(playItem.Video, playItem.Description);
                if (goFullScreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            }
        }

        private void PlayAll()
        {
            currentPlaylist = new List<Player.PlayListItem>();
            currentPlaylistIndex = 0;
            List<VideoInfo> loVideoList = SelectedSite is IChoice ? currentTrailerList : currentVideoList;
            foreach (VideoInfo video in loVideoList)
            {
                currentPlaylist.Add(new Player.PlayListItem(video.Title, null)
                {
                    Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                    Video = video,
                    Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName] : SelectedSite
                });
            }
            Play_Step1(currentPlaylist[0], true);
        }

        private void SaveVideo_Step1(VideoInfo video)
        {
            if (string.IsNullOrEmpty(OnlineVideoSettings.Instance.DownloadDir))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.SetDownloadFolderInConfig);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return SelectedSite.getMultipleVideoUrls(video);
            },
            delegate(bool success, object result)
            {
                if (success) SaveVideo_Step2(video, result as List<String>);
            },
            Translation.GettingPlaybackUrlsForVideo, true);
        }

        private void SaveVideo_Step2(VideoInfo video, List<String> loUrlList)
        {
            // remove all invalid entries from the list of playback urls
            if (loUrlList != null)
            {
                int i = 0;
                while (i < loUrlList.Count)
                {
                    if (String.IsNullOrEmpty(loUrlList[i]) || !(Uri.IsWellFormedUriString(loUrlList[i], UriKind.Absolute) || System.IO.Path.IsPathRooted(loUrlList[i])))
                    {
                        loUrlList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }            
            // if no valid urls were returned show error msg
            if (loUrlList == null || loUrlList.Count == 0)
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.UnableToDownloadVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }
            // if multiple quality choices are available show a selection dialogue (
            string url = loUrlList[0];
            bool resolve = DisplayPlaybackOptions(video, ref url); //downloads the first file from the list, todo: download all if multiple
            if (url == "-1") return; // user canceled the dialog -> don't download
            // display wait cursor as GetPlaybackOptionUrl might do webrequests when overridden
            if (resolve)
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return video.GetPlaybackOptionUrl(url);
                },
                delegate(bool success, object result)
                {
                    if (success) SaveVideo_Step3(video, result as string);
                }
                , Translation.GettingPlaybackUrlsForVideo, true);
                return; // don't execute rest of function, we are waiting for callback
            }
            SaveVideo_Step3(video, url);
        }

        private void SaveVideo_Step3(VideoInfo video, string url)
        {
            // check for valid url
            if (String.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.UnableToDownloadVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            // translate rtmp urls to the local proxy
            if (new Uri(url).Scheme.ToLower().StartsWith("rtmp"))
            {
                url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}", System.Web.HttpUtility.UrlEncode(url)));
            }

            DownloadInfo downloadInfo = new DownloadInfo()
            {
                Url = url,
                Title = video.Title,
                LocalFile = System.IO.Path.Combine(System.IO.Path.Combine(OnlineVideoSettings.Instance.DownloadDir, SelectedSite.Settings.Name), SelectedSite.GetFileNameForDownload(video, selectedCategory, url)),
                ThumbFile = Utils.GetThumbFile(video.ImageUrl)
            };

            Dictionary<string, DownloadInfo> currentDownloads = ((OnlineVideos.Sites.DownloadedVideoUtil)OnlineVideoSettings.Instance.SiteUtilsList[Translation.DownloadedVideos]).currentDownloads;

            if (currentDownloads.ContainsKey(url) || System.IO.File.Exists(downloadInfo.LocalFile))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetHeading(Translation.Error);
                    dlg.SetText(Translation.AlreadyDownloading);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            // make sure the target dir exists
            if (!(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(downloadInfo.LocalFile))))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(downloadInfo.LocalFile));
            }

            lock (currentDownloads) currentDownloads.Add(url, downloadInfo); // make access threadsafe

            System.Threading.Thread downloadThread = new System.Threading.Thread((System.Threading.ParameterizedThreadStart)delegate(object o)
            {
                IDownloader dlHelper = null;
                if (url.ToLower().StartsWith("mms://")) dlHelper = new MMSDownloadHelper();
                else dlHelper = new HTTPDownloader();
                DownloadInfo dlInfo = o as DownloadInfo;
                dlInfo.Downloader = dlHelper;
                Exception exception = dlHelper.Download(dlInfo);
                if (exception != null) Log.Instance.Error("Error downloading {0}, Msg: ", dlInfo.Url, exception.Message);
                OnDownloadFileCompleted(dlInfo, exception);
            });
            downloadThread.IsBackground = true;
            downloadThread.Name = "OnlineVideosDownload";
            downloadThread.Start(downloadInfo);

            GUIDialogNotify dlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (dlgNotify != null)
            {
                dlgNotify.Reset();
                dlgNotify.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                dlgNotify.SetText(Translation.DownloadStarted);
                dlgNotify.DoModal(GUIWindowManager.ActiveWindow);
            }
        }

        private void OnDownloadFileCompleted(DownloadInfo downloadInfo, Exception error)
        {
            Dictionary<string, DownloadInfo> currentDownloads = ((OnlineVideos.Sites.DownloadedVideoUtil)OnlineVideoSettings.Instance.SiteUtilsList[Translation.DownloadedVideos]).currentDownloads;
            lock (currentDownloads) currentDownloads.Remove(downloadInfo.Url); // make access threadsafe

            if (error != null && !downloadInfo.Downloader.Cancelled)
            {
                GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (loDlgNotify != null)
                {
                    loDlgNotify.Reset();
                    loDlgNotify.SetHeading(Translation.Error);
                    loDlgNotify.SetText(string.Format(Translation.DownloadFailed, downloadInfo.Title));
                    loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
                }
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
                    System.IO.File.Copy(downloadInfo.ThumbFile, localImageName, true);
                }

                // get file size
                int fileSize = downloadInfo.KbTotal;
                if (fileSize <= 0)
                {
                    try { fileSize = (int)((new System.IO.FileInfo(downloadInfo.LocalFile)).Length / 1024); }
                    catch { }
                }

                GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (loDlgNotify != null)
                {
                    loDlgNotify.Reset();
                    if (downloadInfo.Downloader.Cancelled)
                        loDlgNotify.SetHeading(Translation.DownloadCancelled);
                    else
                        loDlgNotify.SetHeading(Translation.DownloadComplete);
                    loDlgNotify.SetText(string.Format("{0}{1}", downloadInfo.Title, fileSize > 0 ? " ( " + fileSize.ToString("n0") + " KB)" : ""));
                    loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
                }
            }
        }

        private bool FilterOut(String fsStr)
        {
            if (fsStr == String.Empty)
            {
                return false;
            }
            if (PluginConfiguration.Instance.FilterArray != null)
            {
                foreach (String lsFilter in PluginConfiguration.Instance.FilterArray)
                {
                    if (fsStr.IndexOf(lsFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Instance.Info("Filtering out:{0}\n based on filter:{1}", fsStr, lsFilter);
                        return true;
                        //return false;
                    }
                }
            }
            return false;
        }

        private void UpdateViewState()
        {
            switch (CurrentState)
            {
                case State.sites:
                    GUIPropertyManager.SetProperty("#header.label", PluginConfiguration.Instance.BasicHomeScreenName);
                    GUIPropertyManager.SetProperty("#header.image", Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\OnlineVideos.png");
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    HideFilterButtons();
                    ShowOrderButtons();
                    HideSearchButtons();                    
                    if (OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed)
                        ShowAndEnable(GUI_btnEnterPin.GetID);
                    else
                        HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(null);
                    currentView = currentSiteView;
                    SetFacadeViewMode();
                    break;
                case State.categories:
                    string cat_headerlabel = selectedCategory != null ? selectedCategory.Name : SelectedSite.Settings.Name;
                    GUIPropertyManager.SetProperty("#header.label", cat_headerlabel);
                    GUIPropertyManager.SetProperty("#header.image", GetBannerForSite(SelectedSite));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();                    
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(null);
                    currentView = suggestedView != null ? suggestedView.Value : currentCategoryView;
                    SetFacadeViewMode();
                    break;
                case State.videos:
                    switch (currentVideosDisplayMode)
                    {
                        case VideosMode.Favorites: GUIPropertyManager.SetProperty("#header.label", Translation.Favourites); break;
                        case VideosMode.Search: GUIPropertyManager.SetProperty("#header.label", Translation.SearchResults + " [" + lastSearchQuery + "]"); break;
                        case VideosMode.Related: GUIPropertyManager.SetProperty("#header.label", Translation.RelatedVideos); break;
                        default:
                            {
                                string proposedLabel = SelectedSite.getCurrentVideosTitle();
                                GUIPropertyManager.SetProperty("#header.label", proposedLabel != null ? proposedLabel : selectedCategory != null ? selectedCategory.Name : ""); break;
                            }
                    }
                    GUIPropertyManager.SetProperty("#header.image", GetBannerForSite(SelectedSite));
                    ShowAndEnable(GUI_facadeView.GetID);
                    if (SelectedSite.HasNextPage) ShowAndEnable(GUI_btnNext.GetID); else HideAndDisable(GUI_btnNext.GetID);
                    if (SelectedSite.HasPreviousPage) ShowAndEnable(GUI_btnPrevious.GetID); else HideAndDisable(GUI_btnPrevious.GetID);
                    if (SelectedSite is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    if (SelectedSite.HasFilterCategories) ShowCategoryButton();                    
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(selectedVideo);
                    currentView = suggestedView != null ? suggestedView.Value : currentVideoView;
                    SetFacadeViewMode();
                    break;
                case State.details:
                    GUIPropertyManager.SetProperty("#header.label", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#header.image", GetBannerForSite(SelectedSite));
                    HideAndDisable(GUI_facadeView.GetID);
                    HideAndDisable(GUI_btnNext.GetID);
                    HideAndDisable(GUI_btnPrevious.GetID);
                    HideFilterButtons();
                    HideSearchButtons();                    
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(null);
                    SetGuiProperties_ExtendedVideoInfo(null);
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
            GUI_btnMaxResult.Clear();
            GUI_btnOrderBy.Clear();
            GUI_btnTimeFrame.Clear();

            moSupportedMaxResultList = ((IFilter)SelectedSite).getResultSteps();
            foreach (int step in moSupportedMaxResultList)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnMaxResult.GetID, step + "");
            }
            moSupportedOrderByList = ((IFilter)SelectedSite).getOrderbyList();
            foreach (String orderBy in moSupportedOrderByList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, orderBy);
            }
            moSupportedTimeFrameList = ((IFilter)SelectedSite).getTimeFrameList();
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
            HideAndDisable(GUI_btnSearchCategories.GetID);
            HideAndDisable(GUI_btnSearch.GetID);
        }

        private void ShowSearchButtons()
        {
            GUI_btnSearchCategories.Clear();
            moSupportedSearchCategoryList = SelectedSite.GetSearchableCategories();
            GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, Translation.All);
            foreach (String category in moSupportedSearchCategoryList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, category);
            }
            if (moSupportedSearchCategoryList.Count >= 1)
            {
                ShowAndEnable(GUI_btnSearchCategories.GetID);
            }
            ShowAndEnable(GUI_btnSearch.GetID);
            if (SelectedSearchCategoryIndex > -1)
            {
                Log.Instance.Info("restoring search category...");
                GUIControl.SelectItemControl(GetID, GUI_btnSearchCategories.GetID, SelectedSearchCategoryIndex);
                Log.Instance.Info("Search category restored to " + GUI_btnSearchCategories.SelectedLabel);
            }
        }

        private void ShowCategoryButton()
        {
            Log.Instance.Debug("Showing Category button");
            GUI_btnSearchCategories.Clear();
            moSupportedSearchCategoryList = SelectedSite.GetSearchableCategories();
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
                    strLine = Translation.LayoutList;
                    break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    strLine = Translation.LayoutIcons;
                    break;
                case GUIFacadeControl.ViewMode.LargeIcons:
                    strLine = Translation.LayoutBigIcons;
                    break;
            }
            GUIControl.SetControlLabel(GetID, GUI_btnViewAs.GetID, strLine);

            //set object count label
            GUIPropertyManager.SetProperty("#itemcount", MediaPortal.Util.Utils.GetObjectCountLabel(CurrentState == State.sites ? GUI_facadeView.Count : GUI_facadeView.Count - 1));

            // keep track of the currently selected item (is lost when switching view)
            int rememberIndex = GUI_facadeView.SelectedListItemIndex;
            GUI_facadeView.View = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
            if (rememberIndex > -1) GUIControl.SelectItemControl(GetID, GUI_facadeView.GetID, rememberIndex);
        }

        /// <summary>
        /// Displays a modal dialog, with a list of the PlaybackOptions to the user, 
        /// only if PlaybackOptions holds more than one entry.
        /// </summary>
        /// <param name="videoInfo"></param>
        /// <param name="defaultUrl">will be set to -1 when the user canceled the dialog</param>
        /// <returns>true when a choice from the PlaybackOptions was made and defaultUrl hold the key of that choice</returns>
        private bool DisplayPlaybackOptions(VideoInfo videoInfo, ref string defaultUrl)
        {
            // with no options set, return the VideoUrl field
            if (videoInfo.PlaybackOptions == null || videoInfo.PlaybackOptions.Count == 0) return false;
            // with just one option set, return that options url
            if (videoInfo.PlaybackOptions.Count == 1)
            {
                var enumer = videoInfo.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                defaultUrl = enumer.Current.Value;
                return false;
            }
            int defaultOption = -1;
            // show a list of available options and let the user decide
            GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
            if (dlgSel != null)
            {
                dlgSel.Reset();
                dlgSel.SetHeading(Translation.SelectSource);
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
            defaultUrl = (dlgSel.SelectedId == -1) ? "-1" : dlgSel.SelectedLabelText;
            return true;
        }

        private string GetBannerForSite(Sites.SiteUtilBase site)
        {
            // use Banner with the same name as the Site
            string image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + site.Settings.Name + ".png";
            if (!System.IO.File.Exists(image))
            {
                // if that does not exsist, try Banner with the same name as the Util
                image = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Banners\" + site.Settings.UtilName + ".png";
                if (!System.IO.File.Exists(image)) image = string.Empty;
            }
            return image;
        }

        internal static void SetGuiProperties_PlayingVideo(VideoInfo video, string alternativeTitle)
        {
            if (video == null) return;
            new System.Threading.Thread(delegate()
            {
                System.Threading.Thread.Sleep(2000);
                Log.Instance.Info("Setting Video Properties.");

                string quality = "";
                if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                {
                    var enumer = video.PlaybackOptions.GetEnumerator();
                    while (enumer.MoveNext())
                    {
                        if (enumer.Current.Value == g_Player.CurrentFile)
                        {
                            quality = " (" + enumer.Current.Key + ")";
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(alternativeTitle))
                    GUIPropertyManager.SetProperty("#Play.Current.Title", alternativeTitle);
                else if (!string.IsNullOrEmpty(video.Title))
                    GUIPropertyManager.SetProperty("#Play.Current.Title", video.Title + (string.IsNullOrEmpty(quality) ? "" : quality));
                if (!string.IsNullOrEmpty(video.Description)) GUIPropertyManager.SetProperty("#Play.Current.Plot", video.Description);
                if (!string.IsNullOrEmpty(video.ImageUrl)) GUIPropertyManager.SetProperty("#Play.Current.Thumb", video.ImageUrl);
                if (!string.IsNullOrEmpty(video.Length)) GUIPropertyManager.SetProperty("#Play.Current.Year", video.Length);
            }) { IsBackground = true, Name = "OnlineVideosInfosSetter" }.Start();
        }

        private void SetGuiProperties_SelectedVideo(VideoInfo foVideo)
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
                    GUIPropertyManager.SetProperty("#OnlineVideos.length", Translation.None);
                }
                else
                {                                        
                    GUIPropertyManager.SetProperty("#OnlineVideos.length", VideoInfo.GetDuration(foVideo.Length));                    
                }
                if (String.IsNullOrEmpty(foVideo.Description))
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.desc", Translation.None);
                }
                else
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.desc", foVideo.Description);
                }
            }
        }

        /// <summary>
        /// Processes extended properties which might be available
        /// if the VideoInfo.Other object is using the IVideoDetails interface
        /// </summary>
        /// <param name="videoInfo">if this param is null, the <see cref="selectedVideo"/> will be used</param>
        private void SetGuiProperties_ExtendedVideoInfo(VideoInfo videoInfo)
        {
            string prefix = "DetailsItem";
            if (videoInfo == null)
            {
                videoInfo = selectedVideo; prefix = "Details";
            }
            if (videoInfo != null && videoInfo.Other is IVideoDetails)
            {
                Dictionary<string, string> custom = ((IVideoDetails)videoInfo.Other).GetExtendedProperties();
                foreach (string property in custom.Keys)
                {
                    string label = "#OnlineVideos." + prefix + "." + property;
                    string value = custom[property];
                    GUIPropertyManager.SetProperty(label, value);
                }
            }
        }

        private void ResetSelectedSite()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", string.Empty);
        }

        #endregion
    }
}