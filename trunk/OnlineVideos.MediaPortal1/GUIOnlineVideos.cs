using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public enum State { sites = 0, categories = 1, videos = 2, details = 3, groups = 4 }

        public enum VideosMode { Category = 0, Search = 1, Related = 2 }

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
            // don't use PluginConfiguration.Instance here -> GetHome is already called when MediaPortal starts up into HomeScreen
            // and we don't want to load all sites and config at that moment already
            using (Settings settings = new MPSettings())
            {
                strButtonText = settings.GetValueAsString(PluginConfiguration.CFG_SECTION, PluginConfiguration.CFG_BASICHOMESCREEN_NAME, "Online Videos");
            }
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

        #region state variables

        #region Facade ViewModes
#if MP11
        protected GUIFacadeControl.ViewMode currentView = GUIFacadeControl.ViewMode.List;
        protected GUIFacadeControl.ViewMode? suggestedView;
#else
        protected GUIFacadeControl.Layout currentView = GUIFacadeControl.Layout.List;
        protected GUIFacadeControl.Layout? suggestedView;
#endif
        #endregion
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

        SitesGroup selectedSitesGroup;
        Category selectedCategory;
        VideoInfo selectedVideo;

        bool preventDialogOnLoad = false;
        bool firstLoadDone = false;

        int selectedClipIndex = 0;  // used to remember the position the last selected Trailer

        VideosMode currentVideosDisplayMode = VideosMode.Category;

        List<VideoInfo> currentVideoList = new List<VideoInfo>();
        List<VideoInfo> currentTrailerList = new List<VideoInfo>();
        List<Player.PlayListItem> currentPlaylist = null;
        int currentPlaylistIndex = 0;

        HashSet<string> extendedProperties = new HashSet<string>();

        SmsT9Filter currentFilter = new SmsT9Filter();
        LoadParameterInfo loadParamInfo;
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

        public override void DeInit()
        {
            PluginConfiguration.Instance.Save(true);
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad(); // let animations run

            if (!firstLoadDone) DoFirstLoad_Step1();
            else DoPageLoad();
        }

        protected override void OnShowContextMenu()
        {
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. getting next page videos to finish

            int liSelected = GUI_facadeView.SelectedListItemIndex - 1;

            if (CurrentState == State.details && SelectedSite is IChoice) liSelected = GUI_infoList.SelectedListItemIndex - 1;

            if (liSelected < 0 || CurrentState == State.groups || CurrentState == State.sites || CurrentState == State.categories || (SelectedSite is IChoice && CurrentState == State.videos))
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
            if (!(SelectedSite is Sites.FavoriteUtil))
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
            if (siteSpecificEntries != null) foreach (string entry in siteSpecificEntries) { dlgSel.Add(entry); dialogOptions.Add(entry); }
            dlgSel.DoModal(GUIWindowManager.ActiveWindow);
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
                    if (Gui2UtilConnector.Instance.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish
                    if (CurrentState != State.groups)
                    {
                        ShowPreviousMenu();
                        return;
                    }
                    break;
                case Action.ActionType.ACTION_KEY_PRESSED:
#if MP11
                    if (GUI_facadeView.CurrentView.Visible && GUI_facadeView.Focus)
#else
                    if (GUI_facadeView.LayoutControl.Visible && GUI_facadeView.Focus)
#endif
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
            }
            GUI_btnOrderBy.Label = Translation.SortOptions;
            GUI_btnMaxResult.Label = Translation.MaxResults;
            GUI_btnSearchCategories.Label = Translation.Category;
            GUI_btnTimeFrame.Label = Translation.Timeframe;
            base.OnAction(action);
        }

        public override bool OnMessage(GUIMessage message)
        {
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
                        bool result = base.OnMessage(message);
                        GUI_btnSearchCategories.RestoreSelection = false;
                        GUI_btnOrderBy.RestoreSelection = false;
                        GUI_btnTimeFrame.RestoreSelection = false;
                        GUI_btnMaxResult.RestoreSelection = false;
                        return result;
                    }
                case GUIMessage.MessageType.GUI_MSG_WINDOW_DEINIT:
                    if (message.Param1 != GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO)
                    {
                        // if the plugin was called with a loadParam, reset the states, so when entering without loadParam, the default view will be shown
                        if (loadParamInfo != null)
                        {
                            SelectedSite = null;
                            CurrentState = State.sites;
                            selectedCategory = null;
                        }
                    }
                    break;
            }
            return base.OnMessage(message);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish
            if (control == GUI_facadeView && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                currentFilter.Clear();
                if (CurrentState == State.groups)
                {
                    selectedSitesGroup = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as SitesGroup;
                    DisplaySites();
                }
                else if (CurrentState == State.sites)
                {
                    if (GUI_facadeView.SelectedListItem.Label == "..")
                    {
                        ShowPreviousMenu();
                    }
                    else
                    {
                        SelectedSite = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as Sites.SiteUtilBase;
                        DisplayCategories(null, true);
                    }
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
                            DisplayCategories(categoryToDisplay, true);
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
                    else if (GUI_facadeView.SelectedListItem.Label == Translation.NextPage)
                    {
                        DisplayVideos_NextPage();
                    }
                    else
                    {
                        selectedVideo = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo;
                        if (SelectedSite is IChoice)
                        {
                            // show details view
                            DisplayDetails();
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
            else if (control == GUI_btnMaxResult)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnMaxResult.GetID, GUI_btnMaxResult.SelectedItem);
            }
            else if (control == GUI_btnOrderBy)
            {
                GUIControl.SelectItemControl(GetID, GUI_btnOrderBy.GetID, GUI_btnOrderBy.SelectedItem);
                if (CurrentState == State.sites) PluginConfiguration.Instance.siteOrder = (PluginConfiguration.SiteOrder)GUI_btnOrderBy.SelectedItem;
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
                DisplayVideos_Search();
            }
            else if (control == GUI_btnEnterPin)
            {
                string pin = String.Empty;
                if (GetUserInputString(ref pin, true))
                {
                    if (pin == PluginConfiguration.Instance.pinAgeConfirmation)
                    {
                        OnlineVideoSettings.Instance.AgeConfirmed = true;
                        GUIControl.UnfocusControl(GetID, GUI_btnEnterPin.GetID);
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
            if (newWindowId != Player.GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO && newWindowId != GUISiteUpdater.WindowId)
            {
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

        private void DoPageLoad()
        {
            // called everytime the plugin is shown, after some other window was shown (also after fullscreen playback)
            if (PreviousWindowId != 4758)
            {
                // reload settings that can be modyfied with the MPEI plugin
                PluginConfiguration.Instance.ReLoadRuntimeSettings();

                // reset the LoadParameterInfo
                loadParamInfo = null;

                string loadParam = null;
                // check if running version of mediaportal supports loading with parameter and handle _loadParameter
                System.Reflection.FieldInfo fi = typeof(GUIWindow).GetField("_loadParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fi != null)
                {
                    loadParam = (string)fi.GetValue(this);
                }
                // check for LoadParameters by GUIproperties if not yet set by the _loadParameter
                if (string.IsNullOrEmpty(loadParam))
                {
                    List<string> paramsFromGuiProps = new List<string>();
                    if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Site")))
                    {
                        paramsFromGuiProps.Add("site:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Site"));
                        GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Site", string.Empty);
                    }
                    if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Category")))
                    {
                        paramsFromGuiProps.Add("category:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Category"));
                        GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Category", string.Empty);
                    }
                    if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Search")))
                    {
                        paramsFromGuiProps.Add("search:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Search"));
                        GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Search", string.Empty);
                    }
                    if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.VKonfail")))
                    {
                        paramsFromGuiProps.Add("VKonfail:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.VKonfail"));
                        GUIPropertyManager.SetProperty("#OnlineVideos.startparams.VKonfail", string.Empty);
                    }
                    if (!string.IsNullOrEmpty(GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Return")))
                    {
                        paramsFromGuiProps.Add("return:" + GUIPropertyManager.GetProperty("#OnlineVideos.startparams.Return"));
                        GUIPropertyManager.SetProperty("#OnlineVideos.startparams.Return", string.Empty);
                    }
                    if (paramsFromGuiProps.Count > 0) loadParam = string.Join("|", paramsFromGuiProps.ToArray());
                }

                if (!string.IsNullOrEmpty(loadParam))
                {
                    loadParamInfo = new LoadParameterInfo(loadParam);

                    // set all state variables to reflect the state we were called with
                    if (!string.IsNullOrEmpty(loadParamInfo.Site) && OnlineVideoSettings.Instance.SiteUtilsList.ContainsKey(loadParamInfo.Site))
                    {
                        SelectedSite = OnlineVideoSettings.Instance.SiteUtilsList[loadParamInfo.Site];
                        CurrentState = State.categories;
                        selectedCategory = null;
                    }
                    if (SelectedSite != null && SelectedSite.CanSearch && !string.IsNullOrEmpty(loadParamInfo.Search))
                    {
                        lastSearchQuery = loadParamInfo.Search;
                        DisplayVideos_Search(loadParamInfo.Search);
                        return;
                    }
                }
            }

            Log.Instance.Debug("DoPageLoad. CurrentState:" + CurrentState);
            switch (CurrentState)
            {
                case State.groups: DisplayGroups(); break;
                case State.sites: DisplaySites(); break;
                case State.categories: DisplayCategories(selectedCategory); break;
                case State.videos: SetVideosToFacade(currentVideoList, currentVideosDisplayMode); break;
                default:
                    DisplayDetails();
                    if (selectedClipIndex < GUI_infoList.Count) GUI_infoList.SelectedListItemIndex = selectedClipIndex;
                    break;
            }
        }

        private void DoFirstLoad_Step1()
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
                    if (guiUpdater != null)
                    {
                        GUIDialogProgress dlgPrgrs = (GUIDialogProgress)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_PROGRESS);
                        if (dlgPrgrs != null)
                        {
                            dlgPrgrs.Reset();
                            dlgPrgrs.DisplayProgressBar = true;
                            dlgPrgrs.ShowWaitCursor = false;
                            dlgPrgrs.DisableCancel(true);
                            dlgPrgrs.SetHeading(string.Format("{0} - {1}", PluginConfiguration.Instance.BasicHomeScreenName, Translation.AutomaticUpdate));
                            dlgPrgrs.StartModal(GUIWindowManager.ActiveWindow);

                            new System.Threading.Thread(delegate()
                            {
                                guiUpdater.AutoUpdate(false, dlgPrgrs);
                                GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { DoFirstLoad_Step2(); return 0; }, 0, 0, null);
                            }
                            ) { Name = "OnlineVideosAutoUpdate", IsBackground = true }.Start();
                            return;
                        }
                    }
                }
            }

            DoFirstLoad_Step2();
        }

        private void DoFirstLoad_Step2()
        {
            OnlineVideoSettings.Instance.BuildSiteUtilsList();
            if (PluginConfiguration.Instance.SitesGroups != null && PluginConfiguration.Instance.SitesGroups.Count > 0) CurrentState = State.groups;
            firstLoadDone = true;

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
                    dlgPrgrs.StartModal(GUIWindowManager.ActiveWindow);
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
                    GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { DoPageLoad(); return 0; }, 0, 0, null);
                }) { Name = "OnlineVideosThumbnail", IsBackground = true }.Start();
                return;
            }

            DoPageLoad();
        }

        /// <summary>
        /// This function replaces g_player.ShowFullScreenWindowVideo
        /// </summary>
        /// <returns></returns>
        private static bool ShowFullScreenWindowHandler()
        {
            if (g_Player.HasVideo && (g_Player.Player.GetType().Assembly == typeof(GUIOnlineVideos).Assembly))
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

        private void DisplayGroups()
        {
            SelectedSite = null;
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            foreach (SitesGroup sitesGroup in PluginConfiguration.Instance.SitesGroups)
            {
                if (sitesGroup.Sites != null && sitesGroup.Sites.Count > 0)
                {
                    OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem(sitesGroup.Name);
                    loListItem.Label2 = sitesGroup.Sites.Count.ToString();
                    loListItem.IsFolder = true;
                    loListItem.Item = sitesGroup;
                    MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                    if (!string.IsNullOrEmpty(sitesGroup.Thumbnail))
                    {
                        loListItem.ThumbnailImage = sitesGroup.Thumbnail;
                        loListItem.IconImage = sitesGroup.Thumbnail;
                        loListItem.IconImageBig = sitesGroup.Thumbnail;
                    }
                    loListItem.OnItemSelected += OnGroupSelected;
                    loListItem.ItemId = GUI_facadeView.Count;
                    GUI_facadeView.Add(loListItem);
                    if (selectedSitesGroup != null && selectedSitesGroup.Name == sitesGroup.Name) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
                }
            }

            // add the item for all ungrouped sites if there are any
            HashSet<string> groupedSites = new HashSet<string>();
            foreach (SitesGroup sg in PluginConfiguration.Instance.SitesGroups)
                foreach (string site in sg.Sites)
                    if (!groupedSites.Contains(site)) groupedSites.Add(site);
            SitesGroup othersGroup = new SitesGroup() { Name = Translation.Others };
            foreach (string site in OnlineVideoSettings.Instance.SiteUtilsList.Keys) if (!groupedSites.Contains(site)) othersGroup.Sites.Add(site);
            if (othersGroup.Sites.Count > 0)
            {
                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(othersGroup.Name);
                listItem.Label2 = othersGroup.Sites.Count.ToString();
                listItem.IsFolder = true;
                listItem.Item = othersGroup;
                MediaPortal.Util.Utils.SetDefaultIcons(listItem);
                listItem.OnItemSelected += OnGroupSelected;
                listItem.ItemId = GUI_facadeView.Count;
                GUI_facadeView.Add(listItem);
                if (selectedSitesGroup != null && selectedSitesGroup.Name == othersGroup.Name) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
            }

            CurrentState = State.groups;
            UpdateViewState();
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
            GUI_btnOrderBy.SelectedItem = (int)PluginConfiguration.Instance.siteOrder;

            string[] names = selectedSitesGroup == null ? OnlineVideoSettings.Instance.SiteUtilsList.Keys.ToArray() : selectedSitesGroup.Sites.ToArray();

            // get names in right order
            switch (PluginConfiguration.Instance.siteOrder)
            {
                case PluginConfiguration.SiteOrder.Name:
                    Array.Sort(names);
                    break;
                case PluginConfiguration.SiteOrder.Language:
                    Dictionary<string, List<string>> sitenames = new Dictionary<string, List<string>>();
                    foreach (string name in names)
                    {
                        Sites.SiteUtilBase aSite;
                        if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(name, out aSite))
                        {
                            string key = string.IsNullOrEmpty(aSite.Settings.Language) ? "zzzzz" : aSite.Settings.Language; // puts empty lang at the end
                            List<string> listForLang = null;
                            if (!sitenames.TryGetValue(key, out listForLang)) { listForLang = new List<string>(); sitenames.Add(key, listForLang); }
                            listForLang.Add(aSite.Settings.Name);
                        }
                    }
                    string[] langs = new string[sitenames.Count];
                    sitenames.Keys.CopyTo(langs, 0);
                    Array.Sort(langs);
                    List<string> sortedByLang = new List<string>();
                    foreach (string lang in langs) sortedByLang.AddRange(sitenames[lang]);
                    names = sortedByLang.ToArray();
                    break;
            }

            if (PluginConfiguration.Instance.SitesGroups != null && PluginConfiguration.Instance.SitesGroups.Count > 0)
            {
                // add the first item that will go to the groups menu
                OnlineVideosGuiListItem loListItem;
                loListItem = new OnlineVideosGuiListItem("..");
                loListItem.ItemId = 0;
                loListItem.IsFolder = true;
                loListItem.OnItemSelected += OnSiteSelected;
                MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                GUI_facadeView.Add(loListItem);
            }

            int selectedSiteIndex = 0;  // used to remember the position of the last selected site
            currentFilter.StartMatching();
            foreach (string name in names)
            {
                Sites.SiteUtilBase aSite;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(name, out aSite) &&
                    aSite.Settings.IsEnabled &&
                    (!aSite.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                {
                    OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem(aSite.Settings.Name);
                    loListItem.Label2 = aSite.Settings.Language;
                    loListItem.IsFolder = true;
                    loListItem.Item = aSite;
                    loListItem.OnItemSelected += OnSiteSelected;
                    // use Icon with the same name as the Site
                    string image = GetImageForSite(aSite.Settings.Name, aSite.Settings.UtilName, "Icon");
                    if (!string.IsNullOrEmpty(image))
                    {
                        loListItem.ThumbnailImage = image;
                        loListItem.IconImage = image;
                        loListItem.IconImageBig = image;
                    }
                    else
                    {
                        Log.Instance.Debug("Icon for site {0} not found", aSite.Settings.Name);
                        MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                    }
                    if (currentFilter.Matches(name))
                    {
                        if (loListItem.Item == SelectedSite) selectedSiteIndex = GUI_facadeView.Count;
                        loListItem.ItemId = GUI_facadeView.Count;
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

        private void DisplayCategories(Category parentCategory, bool? diveDownOrUpIfSingle = null)
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
                            SetCategoriesToFacade(parentCategory, result as IList<Category>, diveDownOrUpIfSingle);
                        }
                    },
                    Translation.GettingDynamicCategories, true);
                }
                else
                {
                    SetCategoriesToFacade(parentCategory, SelectedSite.Settings.Categories, diveDownOrUpIfSingle);
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
                            SetCategoriesToFacade(parentCategory, result as IList<Category>, diveDownOrUpIfSingle);
                        }
                    },
                    Translation.GettingDynamicCategories, true);
                }
                else
                {
                    SetCategoriesToFacade(parentCategory, parentCategory.SubCategories, diveDownOrUpIfSingle);
                }
            }
        }

        private void SetCategoriesToFacade(Category parentCategory, IList<Category> categories, bool? diveDownOrUpIfSingle)
        {
            if (loadParamInfo != null && loadParamInfo.Site == SelectedSite.Settings.Name && parentCategory == null && !string.IsNullOrEmpty(loadParamInfo.Category))
            {
                var foundCat = categories.FirstOrDefault(r => r.Name == loadParamInfo.Category);
                if (foundCat != null)
                {
                    if (foundCat.HasSubCategories)
                    {
                        DisplayCategories(foundCat, true);
                    }
                    else
                    {
                        DisplayVideos_Category(foundCat, false);
                    }
                }
                return;
            }

            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);

            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem loListItem;
            loListItem = new OnlineVideosGuiListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            loListItem.OnItemSelected += OnCategorySelected;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_facadeView.Add(loListItem);

            int categoryIndexToSelect = (categories != null && categories.Count > 0) ? 1 : 0; // select the first category by default if there is one
            int numCategoriesWithThumb = 0;
            suggestedView = null;
            currentFilter.StartMatching();
            if (categories != null)
            {
                foreach (Category loCat in categories)
                {
                    if (currentFilter.Matches(loCat.Name))
                    {
                        loListItem = new OnlineVideosGuiListItem(loCat.Name);
                        loListItem.IsFolder = true;
                        loListItem.ItemId = GUI_facadeView.Count;
                        MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                        if (!string.IsNullOrEmpty(loCat.Thumb)) numCategoriesWithThumb++;
                        loListItem.Item = loCat;
                        loListItem.OnItemSelected += OnCategorySelected;
                        if (loCat == selectedCategory) categoryIndexToSelect = GUI_facadeView.Count; // select the category that was previously selected
                        GUI_facadeView.Add(loListItem);

                        if (loCat is RssLink)
                        {
                            RssLink link = loCat as RssLink;
                            if (link.EstimatedVideoCount > 0) loListItem.Label2 = link.EstimatedVideoCount.ToString();
                        }
                        else if (loCat is Group)
                        {
                            loListItem.Label2 = (loCat as Group).Channels.Count.ToString();
                        }
                    }
                }

                if (numCategoriesWithThumb > 0) ImageDownloader.GetImages<Category>(categories);
#if MP11
                if (numCategoriesWithThumb <= categories.Count / 2) suggestedView = GUIFacadeControl.ViewMode.List;
#else
                if (numCategoriesWithThumb <= categories.Count / 2) suggestedView = GUIFacadeControl.Layout.List;
#endif
            }

            GUI_facadeView.SelectedListItemIndex = categoryIndexToSelect;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.categories;
            selectedCategory = parentCategory;
            UpdateViewState();

            // automatically browse up or down if only showing a single category and parameter was set
            if (categories.Count == 1 && diveDownOrUpIfSingle != null)
            {
                if (diveDownOrUpIfSingle.Value)
                    OnClicked(GUI_facadeView.GetID, GUI_facadeView, Action.ActionType.ACTION_SELECT_ITEM);
                else
                    ShowPreviousMenu();
            }
        }

        private void DisplayDetails()
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return ((IChoice)SelectedSite).getVideoChoices(selectedVideo);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    CurrentState = State.details;

                    // make the Thumb of the VideoInfo available to the details view
                    if (string.IsNullOrEmpty(selectedVideo.ImageUrl))
                        GUIPropertyManager.SetProperty("#OnlineVideos.Details.Poster", string.Empty);
                    else
                        GUIPropertyManager.SetProperty("#OnlineVideos.Details.Poster", selectedVideo.ImageUrl);

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
                foreach (VideoInfo loVideoInfo in loVideoList)
                {
                    loListItem = new OnlineVideosGuiListItem(loVideoInfo.Title2);
                    loListItem.IconImage = "defaultVideo.png";
                    loListItem.ItemId = GUI_infoList.Count;
                    loListItem.Item = loVideoInfo;
                    loListItem.Label2 = loVideoInfo.Length;
                    loListItem.OnItemSelected += OnDetailsVideoItemSelected;
                    GUI_infoList.Add(loListItem);
                    currentTrailerList.Add(loVideoInfo);
                }
            }
            UpdateViewState();

            if (loVideoList.Count > 0)
            {
                GUI_infoList.SelectedListItemIndex = 1;
                OnDetailsVideoItemSelected(GUI_infoList[1], GUI_infoList);
            }
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
                        DisplayCategories(category.ParentCategory, false);
                    }
                }
            },
            Translation.GettingCategoryVideos, true);
        }

        private void DisplayVideos_Search(string query = null)
        {
            bool directSearch = !string.IsNullOrEmpty(query);
            if (!directSearch) query = PluginConfiguration.Instance.searchHistoryType == PluginConfiguration.SearchHistoryType.Simple ? lastSearchQuery : string.Empty;
            List<string> searchHistoryForSite = null;

            if (!directSearch && PluginConfiguration.Instance.searchHistoryType == PluginConfiguration.SearchHistoryType.Extended && PluginConfiguration.Instance.searchHistory != null && PluginConfiguration.Instance.searchHistory.Count > 0 &&
                PluginConfiguration.Instance.searchHistory.ContainsKey(SelectedSite.Settings.Name))
            {
                searchHistoryForSite = PluginConfiguration.Instance.searchHistory[SelectedSite.Settings.Name];
                if (searchHistoryForSite != null && searchHistoryForSite.Count > 0)
                {
                    GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    if (dlgSel != null)
                    {
                        dlgSel.Reset();
                        dlgSel.SetHeading(Translation.SearchHistory);
                        dlgSel.Add(Translation.NewSearch);
                        int numAdded = 0;
                        for (int i = searchHistoryForSite.Count - 1; i >= 0; i--)
                        {
                            searchHistoryForSite[i] = searchHistoryForSite[i].Trim();
                            if (!string.IsNullOrEmpty(searchHistoryForSite[i]))
                            {
                                dlgSel.Add(searchHistoryForSite[i]);
                                numAdded++;
                            }
                            else
                            {
                                searchHistoryForSite.RemoveAt(i);
                            }
                            // if the user set the number of searchhistoryitems lower than what was already stored - remove older entries
                            if (numAdded >= PluginConfiguration.Instance.searchHistoryNum && i > 0)
                            {
                                searchHistoryForSite.RemoveRange(0, i);
                                break;
                            }
                        }

                        dlgSel.DoModal(GUIWindowManager.ActiveWindow);

                        if (dlgSel.SelectedId == -1) return;

                        if (dlgSel.SelectedLabel == 0) query = "";
                        else query = dlgSel.SelectedLabelText;
                    }
                }
            }

            if (!directSearch)
            {
                if (GetUserInputString(ref query, false))
                {
                    GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
                    query = query.Trim();
                    if (query != String.Empty)
                    {
                        if (null == searchHistoryForSite) searchHistoryForSite = new List<string>();
                        if (searchHistoryForSite.Contains(query))
                            searchHistoryForSite.Remove(query);
                        searchHistoryForSite.Add(query);
                        if (searchHistoryForSite.Count > PluginConfiguration.Instance.searchHistoryNum)
                            searchHistoryForSite.RemoveAt(0);
                        if (PluginConfiguration.Instance.searchHistory.ContainsKey(SelectedSite.Settings.Name))
                            PluginConfiguration.Instance.searchHistory[SelectedSite.Settings.Name] = searchHistoryForSite;
                        else
                            PluginConfiguration.Instance.searchHistory.Add(SelectedSite.Settings.Name, searchHistoryForSite);

                        lastSearchQuery = query;
                    }
                }
                else
                {
                    return; // user cancelled from VK
                }
            }

            SelectedSearchCategoryIndex = GUI_btnSearchCategories.SelectedItem;
            if (query != String.Empty)
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    if (moSupportedSearchCategoryList != null && moSupportedSearchCategoryList.Count > 1 && GUI_btnSearchCategories.SelectedLabel != Translation.All)
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
                    // set videos to the facade -> if none were found and an empty facade is currently shown, go to previous menu
                    if ((!success || !SetVideosToFacade(result as List<VideoInfo>, VideosMode.Search)) && GUI_facadeView.Count == 0)
                    {
                        if (loadParamInfo != null && loadParamInfo.ShowVKonFailedSearch && GetUserInputString(ref query, false)) DisplayVideos_Search(query);
                        else ShowPreviousMenu();
                    }
                    else
                    {
                        // if only 1 result found and the current site has a details view - open it right away
                        if (SelectedSite is IChoice && (result as List<VideoInfo>).Count == 1)
                        {
                            selectedVideo = (GUI_facadeView[1] as OnlineVideosGuiListItem).Item as VideoInfo;
                            DisplayDetails();
                        }
                    }
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
                if (success) SetVideosToFacade(result as List<VideoInfo>, currentVideosDisplayMode, true);
            },
            Translation.GettingNextPageVideos, true);
        }

        private bool SetVideosToFacade(List<VideoInfo> videos, VideosMode mode, bool append = false)
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

            int indextoSelect = -1;
            if (append)
            {
                currentFilter.Clear();
                indextoSelect = currentVideoList.Count + 1;
                currentVideoList.AddRange(videos);
            }
            else
            {
                currentVideoList = videos;
            }

            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem backItem = new OnlineVideosGuiListItem("..");
            backItem.ItemId = 0;
            backItem.IsFolder = true;
            backItem.OnItemSelected += OnVideoItemSelected;
            MediaPortal.Util.Utils.SetDefaultIcons(backItem);
            GUI_facadeView.Add(backItem);

            // add the items
            Dictionary<string, bool> imageHash = new Dictionary<string, bool>();
            currentFilter.StartMatching();

            foreach (VideoInfo videoInfo in currentVideoList)
            {
                videoInfo.CleanDescriptionAndTitle();
                if (!currentFilter.Matches(videoInfo.Title) || FilterOut(videoInfo.Title) || FilterOut(videoInfo.Description)) continue;

                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(videoInfo.Title);
                listItem.ItemId = GUI_facadeView.Count;
                listItem.Item = videoInfo;
                listItem.IconImage = "defaultVideo.png";
                listItem.IconImageBig = "defaultVideoBig.png";
                listItem.OnItemSelected += OnVideoItemSelected;
                GUI_facadeView.Add(listItem);

                if (listItem.Item == selectedVideo) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
                if (!string.IsNullOrEmpty(videoInfo.ImageUrl)) imageHash[videoInfo.ImageUrl] = true;
            }
            // fall back to list view if there are no items with thumbs or more than one item and all have the same thumb
            suggestedView = null;
#if MP11
            if ((GUI_facadeView.Count > 1 && imageHash.Count == 0) || (GUI_facadeView.Count > 2 && imageHash.Count == 1)) suggestedView = GUIFacadeControl.ViewMode.List;
#else
            if ((GUI_facadeView.Count > 1 && imageHash.Count == 0) || (GUI_facadeView.Count > 2 && imageHash.Count == 1)) suggestedView = GUIFacadeControl.Layout.List;
#endif

            if (SelectedSite.HasNextPage)
            {
                OnlineVideosGuiListItem nextPageItem = new OnlineVideosGuiListItem(Translation.NextPage);
                nextPageItem.ItemId = GUI_facadeView.Count;
                nextPageItem.IsFolder = true;
                nextPageItem.IconImage = "DefaultShortcutBig.png";
                nextPageItem.IconImageBig = "DefaultShortcutBig.png";
                nextPageItem.ThumbnailImage = "DefaultShortcutBig.png";
                nextPageItem.OnItemSelected += OnVideoItemSelected;
                GUI_facadeView.Add(nextPageItem);
            }

            if (indextoSelect > -1 && indextoSelect < GUI_facadeView.Count) GUI_facadeView.SelectedListItemIndex = indextoSelect;

            if (imageHash.Count > 0) ImageDownloader.GetImages<VideoInfo>(currentVideoList);

            currentVideosDisplayMode = mode;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.videos;
            UpdateViewState();
            return true;
        }

        private void ShowPreviousMenu()
        {
            ImageDownloader.StopDownload = true;

            if (CurrentState == State.sites)
            {
                if (PluginConfiguration.Instance.SitesGroups != null && PluginConfiguration.Instance.SitesGroups.Count > 0)
                    DisplayGroups();
                else
                    OnPreviousWindow();
            }
            else if (CurrentState == State.categories)
            {
                if (selectedCategory == null)
                {
                    // if plugin was called with loadParameter set to the current site and return locked -> go to previous window 
                    if (loadParamInfo != null && loadParamInfo.Return == LoadParameterInfo.ReturnMode.Locked && loadParamInfo.Site == selectedSite.Settings.Name)
                        OnPreviousWindow();
                    else
                        DisplaySites();
                }
                else
                {
                    // if plugin was called with loadParameter set to the current site and return locked and currently displaying subcategories of category from loadParam -> go to previous window 
                    if (loadParamInfo != null && loadParamInfo.Return == LoadParameterInfo.ReturnMode.Locked && loadParamInfo.Site == selectedSite.Settings.Name && loadParamInfo.Category == selectedCategory.Name)
                        OnPreviousWindow();
                    else
                        DisplayCategories(selectedCategory.ParentCategory, false);
                }
            }
            else if (CurrentState == State.videos)
            {
                // if plugin was called with loadParameter set to the current site with searchstring and return locked and currently displaying the searchresults or videos for the category from loadParam -> go to previous window 
                if (loadParamInfo != null && loadParamInfo.Return == LoadParameterInfo.ReturnMode.Locked && loadParamInfo.Site == selectedSite.Settings.Name &&
                    (currentVideosDisplayMode == VideosMode.Search ||
                    (currentVideosDisplayMode == VideosMode.Category && selectedCategory != null && loadParamInfo.Category == selectedCategory.Name))
                   )
                    OnPreviousWindow();
                else
                {
                    if (selectedCategory == null || selectedCategory.ParentCategory == null) DisplayCategories(null, false);
                    else DisplayCategories(selectedCategory.ParentCategory, false);
                }
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

                SetVideosToFacade(currentVideoList, currentVideosDisplayMode);
            }
        }

        void OnGroupSelected(GUIListItem item, GUIControl parent)
        {
            SitesGroup group = (item as OnlineVideosGuiListItem).Item as SitesGroup;
            string desc = group == null ? null : group.Description;
            if (!string.IsNullOrEmpty(desc)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", desc);
            else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        void OnSiteSelected(GUIListItem item, GUIControl parent)
        {
            Sites.SiteUtilBase site = (item as OnlineVideosGuiListItem).Item as Sites.SiteUtilBase;
            string desc = site == null ? null : site.Settings.Description;
            if (!string.IsNullOrEmpty(desc)) GUIPropertyManager.SetProperty("#OnlineVideos.desc", desc);
            else GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        void OnCategorySelected(GUIListItem item, GUIControl parent)
        {
            Category cat = (item as OnlineVideosGuiListItem).Item as Category;
            string desc = cat == null ? null : cat.Description;
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

        private bool UrlOk(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute) ||
                Uri.IsWellFormedUriString(Uri.EscapeUriString(url), UriKind.Absolute) ||
                System.IO.Path.IsPathRooted(url);
        }

        private void removeInvalidEntries(List<string> loUrlList)
        {
            // remove all invalid entries from the list of playback urls
            if (loUrlList != null)
            {
                int i = 0;
                while (i < loUrlList.Count)
                {
                    if (String.IsNullOrEmpty(loUrlList[i]) || !UrlOk(loUrlList[i]))
                    {
                        Log.Instance.Debug("removed invalid url {0}", loUrlList[i]);
                        loUrlList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private bool GetUserInputString(ref string sString, bool password)
        {
            VirtualKeyboard keyBoard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (keyBoard == null) return false;
            keyBoard.Reset();
            keyBoard.IsSearchKeyboard = true;
            keyBoard.Text = sString;
            keyBoard.Password = password;
            keyBoard.DoModal(GUIWindowManager.ActiveWindow); // show it...
            if (keyBoard.IsConfirmed) sString = keyBoard.Text;
            return keyBoard.IsConfirmed;
        }

        void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (currentPlaylist != null)
            {
                if (g_Player.Player.GetType().Assembly == typeof(GUIOnlineVideos).Assembly)
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
            removeInvalidEntries(loUrlList);

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
            // check for valid url and cutoff additional parameter
            if (String.IsNullOrEmpty(lsUrl) ||
                !UrlOk((lsUrl.IndexOf("&&&&") > 0) ? lsUrl.Substring(0, lsUrl.IndexOf("&&&&")) : lsUrl))
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
                bool? prepareResult = ((OnlineVideosPlayer)factory.PreparedPlayer).PrepareGraph();
                if (prepareResult == true)
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
                else if (prepareResult == false)
                {
                    IPlayerFactory savedFactory = g_Player.Factory;
                    g_Player.Factory = factory;
                    playing = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                    g_Player.Factory = savedFactory;
                }
                else
                {
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (dlg != null)
                    {
                        dlg.Reset();
                        dlg.SetHeading(Translation.Error);
                        dlg.SetText(Translation.UnableToPlayVideo);
                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                    }
                    factory.PreparedPlayer.Dispose();
                    return;
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
            removeInvalidEntries(loUrlList);

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
            // check for valid url and cutoff additional parameter
            if (String.IsNullOrEmpty(url) ||
                !UrlOk((url.IndexOf("&&&&") > 0) ? url.Substring(0, url.IndexOf("&&&&")) : url))
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
                case State.groups:
                    GUIPropertyManager.SetProperty("#header.label", PluginConfiguration.Instance.BasicHomeScreenName);
                    GUIPropertyManager.SetProperty("#header.image", GetImageForSite("OnlineVideos"));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    HideSearchButtons();
                    currentView = PluginConfiguration.Instance.currentGroupView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Groups);
                    break;
                case State.sites:
                    GUIPropertyManager.SetProperty("#header.label", PluginConfiguration.Instance.BasicHomeScreenName);
                    GUIPropertyManager.SetProperty("#header.image", GetImageForSite("OnlineVideos"));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    ShowOrderButtons();
                    HideSearchButtons();
                    if (OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed)
                        ShowAndEnable(GUI_btnEnterPin.GetID);
                    else
                        HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(null);
                    currentView = PluginConfiguration.Instance.currentSiteView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Sites);
                    break;
                case State.categories:
                    string cat_headerlabel = selectedCategory != null ? selectedCategory.RecursiveName() : SelectedSite.Settings.Name;
                    GUIPropertyManager.SetProperty("#header.label", cat_headerlabel);
                    GUIPropertyManager.SetProperty("#header.image", GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(null);
                    currentView = suggestedView != null ? suggestedView.Value : PluginConfiguration.Instance.currentCategoryView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Categories);
                    break;
                case State.videos:
                    switch (currentVideosDisplayMode)
                    {
                        case VideosMode.Search: GUIPropertyManager.SetProperty("#header.label", Translation.SearchResults + " [" + lastSearchQuery + "]"); break;
                        case VideosMode.Related: GUIPropertyManager.SetProperty("#header.label", Translation.RelatedVideos); break;
                        default:
                            {
                                string proposedLabel = SelectedSite.getCurrentVideosTitle();
                                GUIPropertyManager.SetProperty("#header.label", proposedLabel != null ? proposedLabel : selectedCategory != null ? selectedCategory.RecursiveName() : ""); break;
                            }
                    }
                    GUIPropertyManager.SetProperty("#header.image", GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    ShowAndEnable(GUI_facadeView.GetID);
                    if (SelectedSite is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    if (SelectedSite.HasFilterCategories) ShowCategoryButton();
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    SetGuiProperties_SelectedVideo(selectedVideo);
                    currentView = suggestedView != null ? suggestedView.Value : PluginConfiguration.Instance.currentVideoView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Videos);
                    break;
                case State.details:
                    GUIPropertyManager.SetProperty("#header.label", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#header.image", GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    HideAndDisable(GUI_facadeView.GetID);
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
#if MP11
                case GUIFacadeControl.ViewMode.List:
                    currentView = GUIFacadeControl.ViewMode.SmallIcons; break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    currentView = GUIFacadeControl.ViewMode.LargeIcons; break;
                case GUIFacadeControl.ViewMode.LargeIcons:
                    currentView = GUIFacadeControl.ViewMode.List; break;
#else
                case GUIFacadeControl.Layout.List:
                    currentView = GUIFacadeControl.Layout.SmallIcons; break;
                case GUIFacadeControl.Layout.SmallIcons:
                    currentView = GUIFacadeControl.Layout.LargeIcons; break;
                case GUIFacadeControl.Layout.LargeIcons:
                    currentView = GUIFacadeControl.Layout.List; break;
#endif
            }
            switch (CurrentState)
            {
                case State.groups: PluginConfiguration.Instance.currentGroupView = currentView; break;
                case State.sites: PluginConfiguration.Instance.currentSiteView = currentView; break;
                case State.categories: PluginConfiguration.Instance.currentCategoryView = currentView; break;
                case State.videos: PluginConfiguration.Instance.currentVideoView = currentView; break;
            }
            if (CurrentState != State.details) SetFacadeViewMode();
        }

        protected void SetFacadeViewMode()
        {
            if (GUI_facadeView == null) return;

            string strLine = String.Empty;
            switch (currentView)
            {
#if MP11
                case GUIFacadeControl.ViewMode.List:
                    strLine = Translation.LayoutList;
                    break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    strLine = Translation.LayoutIcons;
                    break;
                case GUIFacadeControl.ViewMode.LargeIcons:
                    strLine = Translation.LayoutBigIcons;
                    break;
#else
                case GUIFacadeControl.Layout.List:
                    strLine = Translation.LayoutList;
                    break;
                case GUIFacadeControl.Layout.SmallIcons:
                    strLine = Translation.LayoutIcons;
                    break;
                case GUIFacadeControl.Layout.LargeIcons:
                    strLine = Translation.LayoutBigIcons;
                    break;
#endif
            }
            GUIControl.SetControlLabel(GetID, GUI_btnViewAs.GetID, strLine);

            //set object count label
            int itemcount = GUI_facadeView.Count;
            if (itemcount > 0)
            {
                if (GUI_facadeView[0].Label == "..") itemcount--;
                if (itemcount > 0 && (GUI_facadeView[GUI_facadeView.Count - 1] as OnlineVideosGuiListItem).Item == null) itemcount--;
            }
            GUIPropertyManager.SetProperty("#itemcount", itemcount.ToString());

            // keep track of the currently selected item (is lost when switching view)
            int rememberIndex = GUI_facadeView.SelectedListItemIndex;
#if MP11
            GUI_facadeView.View = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
#else
            GUI_facadeView.CurrentLayout = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
#endif
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
            dlgSel.DoModal(GUIWindowManager.ActiveWindow);
            defaultUrl = (dlgSel.SelectedId == -1) ? "-1" : dlgSel.SelectedLabelText;
            return true;
        }

        internal static string GetImageForSite(string siteName, string utilName = "", string type = "Banner")
        {
            // use png with the same name as the Site - first check subfolder of current skin (allows skinners to use custom icons)
            string image = string.Format(@"{0}\Media\OnlineVideos\{1}s\{2}.png", GUIGraphicsContext.Skin, type, siteName);
            if (!System.IO.File.Exists(image))
            {
                // use png with the same name as the Site
                image = string.Format(@"{0}\OnlineVideos\{1}s\{2}.png", Config.GetFolder(Config.Dir.Thumbs), type, siteName);
                if (!System.IO.File.Exists(image))
                {
                    image = string.Empty;
                    // if that does not exist, try image with the same name as the Util
                    if (!string.IsNullOrEmpty(utilName))
                    {
                        image = string.Format(@"{0}\OnlineVideos\{1}s\{2}.png", Config.GetFolder(Config.Dir.Thumbs), type, utilName);
                        if (!System.IO.File.Exists(image)) image = string.Empty;
                    }
                }
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
                        string compareTo = enumer.Current.Value.ToLower().StartsWith("rtmp") ?
                            ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}", System.Web.HttpUtility.UrlEncode(enumer.Current.Value)))
                            : enumer.Current.Value;
                        if (compareTo == g_Player.CurrentFile)
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
                if (!string.IsNullOrEmpty(video.ThumbnailImage)) GUIPropertyManager.SetProperty("#Play.Current.Thumb", video.ThumbnailImage);
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
            string prefix = "#OnlineVideos.";
            if (videoInfo == null)
            {
                videoInfo = selectedVideo;
                prefix = prefix + "Details.";
            }
            else
            {
                prefix = prefix + "DetailsItem.";
            }

            ResetExtendedGuiProperties(prefix);

            if (videoInfo != null && videoInfo.Other is IVideoDetails)
            {
                Dictionary<string, string> custom = ((IVideoDetails)videoInfo.Other).GetExtendedProperties();
                foreach (string property in custom.Keys)
                {
                    string label = prefix + property;
                    string value = custom[property];
                    SetExtendedGuiProperty(label, value);
                }
            }
        }

        /// <summary>
        /// Set an extended property in the GUIPropertyManager
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetExtendedGuiProperty(string key, string value)
        {
            extendedProperties.Add(key);
            GUIPropertyManager.SetProperty(key, value);
        }

        /// <summary>
        /// Clears all known set extended property values
        /// </summary>
        /// <param name="prefix">prefix</param>
        public void ResetExtendedGuiProperties(string prefix)
        {
            if (extendedProperties.Count == 0)
            {
                return;
            }

            string[] keys = extendedProperties.Where(s => s.StartsWith(prefix)).ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                GUIPropertyManager.SetProperty(keys[i], string.Empty);
                extendedProperties.Remove(keys[i]);
            }
        }

        private void ResetSelectedSite()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        #endregion
    }
}