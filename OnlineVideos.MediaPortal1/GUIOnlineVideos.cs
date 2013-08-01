using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Profile;
using OnlineVideos.MediaPortal1.Player;
using Action = MediaPortal.GUI.Library.Action;

namespace OnlineVideos.MediaPortal1
{
	[PluginIcons("OnlineVideos.MediaPortal1.OnlineVideos.png", "OnlineVideos.MediaPortal1.OnlineVideosDisabled.png")]
    public partial class GUIOnlineVideos : GUIWindow, ISetupForm, IShowPlugin
    {
        public const int WindowId = 4755;

        public enum State { sites = 0, categories = 1, videos = 2, details = 3, groups = 4 }

        public enum VideosMode { Category = 0, Search = 1 }

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
        [SkinControlAttribute(47016)]
        protected GUIButtonControl GUI_btnCurrentDownloads = null;
        #endregion

        #region state variables

        #region Facade ViewModes
        protected GUIFacadeControl.Layout currentView = GUIFacadeControl.Layout.List;
        protected GUIFacadeControl.Layout? suggestedView;
        #endregion
        #region CurrentState
        State currentState = State.sites;
        public State CurrentState
        {
            get { return currentState; }
            set { currentState = value; GUIPropertyManager.SetProperty("#OnlineVideos.state", value.ToString()); if (value != State.videos) ExtendedVideoInfo = false; }
        }
        #endregion
        #region SelectedSite
        Sites.SiteUtilBase selectedSite;
        internal Sites.SiteUtilBase SelectedSite
        {
            get { return selectedSite; }
            private set
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
        #region ExtendedVideoInfo
        bool extendedVideoInfo = false;
        public bool ExtendedVideoInfo
        {
            get { return extendedVideoInfo; }
            set { extendedVideoInfo = value; GUIPropertyManager.SetProperty("#OnlineVideos.ExtendedVideoInfo", value.ToString()); }
        }
        #endregion

        NavigationContextSwitch currentNavigationContextSwitch;

        public delegate void TrackVideoPlaybackHandler(ITrackingInfo info, double percentPlayed);
        public event TrackVideoPlaybackHandler TrackVideoPlayback;

        public delegate void VideoDownloadedHandler(string file, string site, string categoryRecursiveName, string videoTitle);
        public event VideoDownloadedHandler VideoDownloaded;

        OnlineVideosGuiListItem selectedSitesGroup;
        Category selectedCategory;
        VideoInfo selectedVideo;

        bool preventDialogOnLoad = false;

        int selectedClipIndex = 0;  // used to remember the position of the last selected Trailer

        VideosMode currentVideosDisplayMode = VideosMode.Category;

        List<OnlineVideosGuiListItem> currentFacadeItems = new List<OnlineVideosGuiListItem>();

        List<VideoInfo> currentVideoList = new List<VideoInfo>();
        List<VideoInfo> currentTrailerList = new List<VideoInfo>();
        Player.PlayList currentPlaylist = null;
        Player.PlayListItem currentPlayingItem = null;

        HashSet<string> extendedProperties = new HashSet<string>();

        SmsT9Filter currentFilter = new SmsT9Filter();
        string videosVKfilter = string.Empty; // used for searching in large lists of videos
        LoadParameterInfo loadParamInfo;

        bool GroupsEnabled
        {
            get { return (PluginConfiguration.Instance.SitesGroups != null && PluginConfiguration.Instance.SitesGroups.Count > 0) || PluginConfiguration.Instance.autoGroupByLang; }
        }

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
            OnlineVideosAppDomain.UseSeperateDomain = true;

            bool result = Load(GUIGraphicsContext.Skin + @"\myonlinevideos.xml");

            GUIPropertyManager.SetProperty("#OnlineVideos.desc", " "); GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.length", " "); GUIPropertyManager.SetProperty("#OnlineVideos.length", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.aired", " "); GUIPropertyManager.SetProperty("#OnlineVideos.aired", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", " "); GUIPropertyManager.SetProperty("#OnlineVideos.filter", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", " "); GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", " "); GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.currentDownloads", "0");
            GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", "OnlineVideos");
            CurrentState = State.sites;
            ExtendedVideoInfo = false;
            // get last active module settings  
            using (MediaPortal.Profile.Settings settings = new MediaPortal.Profile.MPSettings())
            {
                bool lastActiveModuleSetting = settings.GetValueAsBool("general", "showlastactivemodule", false);
                int lastActiveModule = settings.GetValueAsInt("general", "lastactivemodule", -1);
                preventDialogOnLoad = (lastActiveModuleSetting && (lastActiveModule == GetID));
            }

            StartBackgroundInitialization();

            return result;
        }

        /// <summary>
        /// Called when MediaPortal is closed.
        /// </summary>
        public override void DeInit()
        {
            // Make sure all runtime changeable properties are persisted
            PluginConfiguration.Instance.Save(true);
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad(); // let animations run

            if (initializationBackgroundWorker.IsBusy)
            {
                GUIWaitCursor.Init(); GUIWaitCursor.Show();
                initializationBackgroundWorker.RunWorkerCompleted += (o, e) =>
                {
                    GUIWaitCursor.Hide();
                    if (!firstLoadDone) DoFirstLoad();
                    else DoSubsequentLoad();
                };
            }
            else
            {
                if (!firstLoadDone) DoFirstLoad();
                else DoSubsequentLoad();
            }
        }

        protected override void OnShowContextMenu()
        {
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. getting next page videos to finish

            if (CurrentState == State.sites && GetFocusControlId() == GUI_facadeView.GetID)
            {
                // handle a site's context menu
                OnlineVideosGuiListItem selectedItem = GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem;
                if (selectedItem == null || selectedItem.Item == null) return; // only context menu for items with an object backing them

                Sites.SiteUtilBase aSite = selectedItem.Item as Sites.SiteUtilBase;
                if (aSite != null)
                {
                    selectedSite = SiteUserSettingsDialog.ShowDialog(aSite);
                    selectedItem.Item = selectedSite;
                }
            }
            else if (CurrentState == State.categories && GetFocusControlId() == GUI_facadeView.GetID)
            {
                // handle a category's context menu
                OnlineVideosGuiListItem selectedItem = GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem;
                if (selectedItem == null || selectedItem.Item == null) return; // only context menu for items with an object backing them

                Category aCategory = selectedItem.Item as Category;
                if (aCategory != null && !(aCategory is NextPageCategory))
                {
                    GUIDialogMenu dlgCat = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    if (dlgCat == null) return;
                    dlgCat.Reset();
                    dlgCat.SetHeading(Translation.Instance.Actions);
                    List<KeyValuePair<string, Sites.ContextMenuEntry>> dialogOptions = new List<KeyValuePair<string, Sites.ContextMenuEntry>>();
                    if (!(SelectedSite is Sites.FavoriteUtil))
                    {
                        if (selectedItem.IsPlayed)
                        {
                            dlgCat.Add(Translation.Instance.RemoveFromFavorites);
                            dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(Translation.Instance.RemoveFromFavorites, null));
                        }
                        else
                        {
                            dlgCat.Add(Translation.Instance.AddToFavourites);
                            dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(Translation.Instance.AddToFavourites, null));
                        }
                    }
                    foreach (var entry in SelectedSite.GetContextMenuEntries(aCategory, null))
                    {
                        dlgCat.Add(entry.DisplayText);
                        dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(entry.DisplayText, entry));
                    }
                    dlgCat.DoModal(GUIWindowManager.ActiveWindow);
                    if (dlgCat.SelectedId == -1) return;
                    else
                    {
                        var selectedOption = dialogOptions[dlgCat.SelectedId - 1];
                        if (selectedOption.Value == null)
                        {
                            if (dlgCat.SelectedLabelText == Translation.Instance.AddToFavourites)
                            {
                                bool result = OnlineVideoSettings.Instance.FavDB.addFavoriteCategory(aCategory, SelectedSite.Settings.Name);
                                if (result)
                                {
                                    cachedFavoritedCategoriesOfSelectedSite = default(KeyValuePair<string, List<string>>);
                                    selectedItem.IsPlayed = true;
                                    selectedItem.PinImage = SiteImageExistenceCache.GetImageForSite(Translation.Instance.Favourites, type: "Icon");
                                }
                            }
                            else if (dlgCat.SelectedLabelText == Translation.Instance.RemoveFromFavorites)
                            {
                                bool result = OnlineVideoSettings.Instance.FavDB.removeFavoriteCategory(SelectedSite.Settings.Name, aCategory.RecursiveName("|"));
                                if (result)
                                {
                                    cachedFavoritedCategoriesOfSelectedSite = default(KeyValuePair<string, List<string>>);
                                    selectedItem.IsPlayed = false;
                                    selectedItem.PinImage = "";
                                    selectedItem.RefreshCoverArt();
                                }
                            }
                        }
                        else
                        {
                            HandleCustomContextMenuEntry(selectedOption.Value, aCategory, null);
                        }
                    }
                }
            }
            else if ((CurrentState == State.videos && GetFocusControlId() == GUI_facadeView.GetID) ||
                (CurrentState == State.details && GetFocusControlId() == GUI_infoList.GetID))
            {
                // handle a video's context menu
                int numItemsShown = (CurrentState == State.videos ? GUI_facadeView.Count : GUI_infoList.Count) - 1; // first item is always ".."
                OnlineVideosGuiListItem selectedItem = CurrentState == State.videos ?
                    GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem : GUI_infoList.SelectedListItem as OnlineVideosGuiListItem;
                if (selectedItem == null || selectedItem.Item == null) return; // only context menu for items with an object backing them

                VideoInfo aVideo = selectedItem.Item as VideoInfo;

                if (aVideo != null)
                {
                    GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    if (dlgSel == null) return;
                    dlgSel.Reset();
                    dlgSel.SetHeading(Translation.Instance.Actions);
                    List<KeyValuePair<string, Sites.ContextMenuEntry>> dialogOptions = new List<KeyValuePair<string, Sites.ContextMenuEntry>>();
                    // these context menu entries should only show if the item will not go to the details view
                    if (!(SelectedSite is IChoice && CurrentState == State.videos && aVideo.HasDetails))
                    {
                        if (!(SelectedSite is Sites.FavoriteUtil && aVideo.HasDetails &&
                            (selectedCategory is Sites.FavoriteUtil.FavoriteCategory && (selectedCategory as Sites.FavoriteUtil.FavoriteCategory).Site is IChoice)))
                        {
                            dlgSel.Add(Translation.Instance.PlayWith);
                            dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("PlayWith", null));
                            if (numItemsShown > 1)
                            {
                                dlgSel.Add(Translation.Instance.PlayAll);
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("PlayAll", null));
								dlgSel.Add(Translation.Instance.PlayAllFromHere);
								dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("PlayAllFromHere", null));
                                dlgSel.Add(Translation.Instance.PlayAllRandom);
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("PlayAllRandom", null));
                            }
							if (SelectedSite.CanSearch)
							{
								// Add context keyword search
								dlgSel.Add(Translation.Instance.SearchRelatedKeywords);
								dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("SearchKeywords", null));
							}
                            if (!(SelectedSite is Sites.FavoriteUtil) && !(SelectedSite is Sites.DownloadedVideoUtil))
                            {
                                dlgSel.Add(Translation.Instance.AddToFavourites);
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("AddToFav", null));
                            }
                            if (!(SelectedSite is Sites.DownloadedVideoUtil))
                            {
                                dlgSel.Add(string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Concurrent));
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("DownloadConcurrent", null));
                                dlgSel.Add(string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Queued));
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("DownloadQueued", null));

                                if (loadParamInfo != null && !string.IsNullOrEmpty(loadParamInfo.DownloadDir) && Directory.Exists(loadParamInfo.DownloadDir))
                                {
                                    if (string.IsNullOrEmpty(loadParamInfo.DownloadMenuEntry))
                                        dlgSel.Add(Translation.Instance.DownloadUserdefined);
                                    else
                                        dlgSel.Add(loadParamInfo.DownloadMenuEntry);
                                    dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("UserdefinedDownload", null));
                                }
                            }
                            foreach (var entry in SelectedSite.GetContextMenuEntries(selectedCategory, aVideo))
                            {
                                dlgSel.Add(entry.DisplayText);
                                dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(entry.DisplayText, entry));
                            }
                        }
                    }
                    // always allow the VK filtering in videos view
                    if (CurrentState == State.videos && numItemsShown > 1)
                    {
                        dlgSel.Add(Translation.Instance.Filter);
                        dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>("Filter", null));
                    }
                    if (dialogOptions.Count > 0)
                    {
                        dlgSel.DoModal(GUIWindowManager.ActiveWindow);
                        if (dlgSel.SelectedId == -1) return;
                        else
                        {
                            switch (dialogOptions[dlgSel.SelectedId - 1].Key)
                            {
                                case "PlayWith":
                                    dialogOptions.Clear();
                                    dlgSel.Reset();
                                    dlgSel.SetHeading(Translation.Instance.Actions);
                                    dlgSel.Add("MediaPortal");
                                    dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(OnlineVideos.PlayerType.Internal.ToString(), null));
                                    dlgSel.Add("Windows Media Player");
                                    dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(OnlineVideos.PlayerType.WMP.ToString(), null));
                                    if (VLCPlayer.IsInstalled)
                                    {
                                        dlgSel.Add("VLC media player");
                                        dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(OnlineVideos.PlayerType.VLC.ToString(), null));
                                    }
                                    dlgSel.DoModal(GUIWindowManager.ActiveWindow);
                                    if (dlgSel.SelectedId == -1) return;
                                    else
                                    {
                                        OnlineVideos.PlayerType forcedPlayer = (OnlineVideos.PlayerType)Enum.Parse(typeof(OnlineVideos.PlayerType), dialogOptions[dlgSel.SelectedId - 1].Key);
                                        if (CurrentState == State.videos) selectedVideo = aVideo;
                                        else selectedClipIndex = GUI_infoList.SelectedListItemIndex;
                                        //play the video
                                        currentPlaylist = null;
                                        currentPlayingItem = null;
                                        Play_Step1(new PlayListItem(null, null)
                                                {
                                                    Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                                                    Video = aVideo,
                                                    Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[selectedVideo.SiteName] : selectedSite,
                                                    ForcedPlayer = forcedPlayer
                                                }, true);
                                    }
                                    break;
                                case "PlayAll":
                                    PlayAll();
                                    break;
								case "PlayAllFromHere":
									PlayAll(false, aVideo);
									break;
                                case "PlayAllRandom":
                                    PlayAll(true);
                                    break;
                                case "SearchKeywords":
                                  List<string> searchexpressions = new List<string> { selectedItem.Label };
                                  if (selectedItem.Description.Length > 0) searchexpressions.Add(selectedItem.Description);
                                  ContextKeywordSelection(searchexpressions);
                                  break;
                                case "AddToFav":
                                    string suggestedTitle = SelectedSite.GetFileNameForDownload(aVideo, selectedCategory, null);
                                    bool successAddingToFavs = OnlineVideoSettings.Instance.FavDB.addFavoriteVideo(aVideo, suggestedTitle, SelectedSite.Settings.Name);
                                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                                    if (dlg != null)
                                    {
                                        dlg.Reset();
                                        dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                                        dlg.SetHeading(successAddingToFavs ? Translation.Instance.Success : Translation.Instance.Error);
                                        dlg.SetText(Translation.Instance.AddingToFavorites);
                                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                                    }
                                    break;
                                case "DownloadConcurrent":
                                    SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(aVideo, selectedCategory, selectedSite)));
                                    break;
                                case "DownloadQueued":
                                    SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(aVideo, selectedCategory, selectedSite)), true);
                                    break;
                                case "UserdefinedDownload":
                                    var dlInfo = DownloadInfo.Create(aVideo, selectedCategory, selectedSite);
                                    dlInfo.OverrideFolder = loadParamInfo.DownloadDir;
                                    dlInfo.OverrideFileName = loadParamInfo.DownloadFilename;
                                    SaveVideo_Step1(DownloadList.Create(dlInfo));
                                    break;
                                case "Filter":
                                    if (GetUserInputString(ref videosVKfilter, false)) SetVideosToFacade(currentVideoList, currentVideosDisplayMode);
                                    break;
                                default:
                                    HandleCustomContextMenuEntry(dialogOptions[dlgSel.SelectedId - 1].Value, selectedCategory, aVideo);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        void HandleCustomContextMenuEntry(Sites.ContextMenuEntry currentEntry, Category aCategory, VideoInfo aVideo)
        {
            List<KeyValuePair<string, Sites.ContextMenuEntry>> dialogOptions = new List<KeyValuePair<string, Sites.ContextMenuEntry>>();
            while (true)
            {
                bool execute = currentEntry.Action == Sites.ContextMenuEntry.UIAction.Execute;

                if (currentEntry.Action == Sites.ContextMenuEntry.UIAction.GetText)
                {
                    string text = currentEntry.UserInputText ?? "";
                    if (GetUserInputString(ref text, false))
                    {
                        currentEntry.UserInputText = text;
                        execute = true;
                    }
                    else break;
                }
                if (currentEntry.Action == Sites.ContextMenuEntry.UIAction.ShowList)
                {
                    GUIDialogMenu dlgCat = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    dlgCat.Reset();
                    dlgCat.SetHeading(currentEntry.DisplayText);
                    dialogOptions.Clear();
                    foreach (var subEntry in currentEntry.SubEntries)
                    {
                        dlgCat.Add(subEntry.DisplayText);
                        dialogOptions.Add(new KeyValuePair<string, Sites.ContextMenuEntry>(subEntry.DisplayText, subEntry));
                    }
                    dlgCat.DoModal(GUIWindowManager.ActiveWindow);
                    if (dlgCat.SelectedId == -1) break;
                    else currentEntry = dialogOptions[dlgCat.SelectedId - 1].Value;
                }
                if (currentEntry.Action == Sites.ContextMenuEntry.UIAction.PromptYesNo)
                {
                    GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_YES_NO);
                    dlgYesNo.Reset();
                    dlgYesNo.SetHeading(currentEntry.PromptText);
                    dlgYesNo.DoModal(GUIWindowManager.ActiveWindow);
                    if (dlgYesNo.IsConfirmed)
                        execute = true;
                    else
                        break;
                }
                if (execute)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        return SelectedSite.ExecuteContextMenuEntry(aCategory, aVideo, currentEntry);
                    },
                    delegate(bool success, object result)
                    {
                        if (success && result is Sites.ContextMenuExecutionResult)
                        {
                            var cmer = result as Sites.ContextMenuExecutionResult;
                            if (!string.IsNullOrEmpty(cmer.ExecutionResultMessage))
                            {
                                GUIDialogNotify dlg_notify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                                if (dlg_notify != null)
                                {
                                    dlg_notify.Reset();
                                    dlg_notify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                                    dlg_notify.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                                    dlg_notify.SetText(cmer.ExecutionResultMessage);
                                    dlg_notify.DoModal(GUIWindowManager.ActiveWindow);
                                }
                            }
                            if (cmer.RefreshCurrentItems)
                            {
                                if (aVideo == null) DisplayCategories(selectedCategory, null);
                                else DisplayVideos_Category(selectedCategory, true);
                            }
                            if (cmer.ResultItems != null && cmer.ResultItems.Count > 0) SetSearchResultItemsToFacade(cmer.ResultItems, VideosMode.Category, currentEntry.DisplayText);
                        }
                    },
                    ": " + currentEntry.DisplayText, true);
                    break;
                }
            }
        }

        public override void OnAction(Action action)
        {
            switch (action.wID)
            {
                case Action.ActionType.ACTION_RECORD:
                    {
                        if (CurrentState == State.videos)
                        {
                            OnlineVideosGuiListItem selectedItem = GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem;
                            if (selectedItem != null)
                            {
                                VideoInfo aVideo = selectedItem.Item as VideoInfo;
                                if (aVideo != null && !(SelectedSite is IChoice && aVideo.HasDetails))
                                    SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(aVideo, selectedCategory, selectedSite)));
                            }
                        }
                        else if (CurrentState == State.details)
                        {
                            OnlineVideosGuiListItem selectedItem = GUI_infoList.SelectedListItem as OnlineVideosGuiListItem;
                            if (selectedItem != null)
                            {
                                VideoInfo aVideo = selectedItem.Item as VideoInfo;
                                if (aVideo != null)
                                    SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(aVideo, selectedCategory, selectedSite)));
                            }
                        }
                        break;
                    }
                case Action.ActionType.ACTION_STOP:
                    if (BufferingPlayerFactory != null)
                    {
                        ((OnlineVideosPlayer)BufferingPlayerFactory.PreparedPlayer).StopBuffering();
                        Gui2UtilConnector.Instance.StopBackgroundTask();
                        return;
                    }
                    break;
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (BufferingPlayerFactory != null)
                    {
                        ((OnlineVideosPlayer)BufferingPlayerFactory.PreparedPlayer).SkipBuffering();
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
                    if (GUI_facadeView.LayoutControl.Visible && GUI_facadeView.Focus)
                    {
                        // search items (starting from current selected) by title and select first found one
                        char pressedChar = (char)action.m_key.KeyChar;
                        if (char.IsDigit(pressedChar) || (pressedChar == '\b' && !currentFilter.IsEmpty()))
                        {
                            currentFilter.Add(pressedChar);
                            FilterCurrentFacadeItems();
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
            GUI_btnOrderBy.Label = Translation.Instance.SortOptions;
            GUI_btnMaxResult.Label = Translation.Instance.MaxResults;
            GUI_btnSearchCategories.Label = Translation.Instance.Category;
            GUI_btnTimeFrame.Label = Translation.Instance.Timeframe;
            base.OnAction(action);
        }

        public override bool OnMessage(GUIMessage message)
        {
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

        void GUIWindowManager_OnThreadMessageHandler(object sender, GUIMessage message)
        {
            if (message.Message == GUIMessage.MessageType.GUI_MSG_GOTO_WINDOW &&
                message.TargetWindowId == 0 && message.TargetControlId == 0 && message.SenderControlId == 0 &&
                message.SendToTargetWindow == false && message.Object == null && message.Object2 == null &&
                message.Param2 == 0 && message.Param3 == 0 && message.Param4 == 0 &&
                (message.Param1 == (int)GUIWindow.Window.WINDOW_HOME || message.Param1 == (int)GUIWindow.Window.WINDOW_SECOND_HOME)
                )
            {
                if (CurrentState != State.groups && GroupsEnabled)
                {
                    // prevent message from beeing sent to MP core
                    message.SendToTargetWindow = true;
                    message.TargetWindowId = GetID;
                    message.Param1 = GetID;
                    message.Message = GUIMessage.MessageType.GUI_MSG_HIDE_MESSAGE;

                    // reset to groups view
                    SelectedSite = null;
                    selectedCategory = null;
                    selectedVideo = null;
                    currentVideoList = new List<VideoInfo>();
                    currentTrailerList = new List<VideoInfo>();
                    currentNavigationContextSwitch = null;
                    loadParamInfo = null;
                    DisplayGroups();
                }
                else if (CurrentState != State.sites && !GroupsEnabled)
                {
                    // prevent message from beeing sent to MP core
                    message.SendToTargetWindow = true;
                    message.TargetWindowId = GetID;
                    message.Param1 = GetID;
                    message.Message = GUIMessage.MessageType.GUI_MSG_HIDE_MESSAGE;

                    // reset to sites view
                    selectedCategory = null;
                    selectedVideo = null;
                    currentVideoList = new List<VideoInfo>();
                    currentTrailerList = new List<VideoInfo>();
                    currentNavigationContextSwitch = null;
                    loadParamInfo = null;
                    DisplaySites();
                }
            }
        }

        void GUIWindowManager_OnNewAction(Action action)
        {
            if (currentPlaylist != null && g_Player.HasVideo && g_Player.Player.GetType().Assembly == typeof(GUIOnlineVideos).Assembly)
            {
                if (action.wID == Action.ActionType.ACTION_NEXT_ITEM)
                {
                    int currentPlaylistIndex = currentPlayingItem != null ? currentPlaylist.IndexOf(currentPlayingItem) : 0;
                    // move to next
                    if (currentPlaylist.Count > currentPlaylistIndex + 1)
                    {
                        currentPlaylistIndex++;
                        Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                    }
                }
                else if (action.wID == Action.ActionType.ACTION_PREV_ITEM)
                {
                    int currentPlaylistIndex = currentPlayingItem != null ? currentPlaylist.IndexOf(currentPlayingItem) : 0;
                    // move to previous
                    if (currentPlaylistIndex - 1 >= 0)
                    {
                        currentPlaylistIndex--;
                        Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                    }
                }
            }
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (Gui2UtilConnector.Instance.IsBusy || BufferingPlayerFactory != null) return; // wait for any background action e.g. dynamic category discovery to finish
            if (control == GUI_facadeView)
            {
                if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                {
                    currentFilter.Clear();
                    GUIPropertyManager.SetProperty("#OnlineVideos.filter", string.Empty);
                    switch (CurrentState)
                    {
                        case State.groups:
                            selectedSitesGroup = GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem;
                            if (selectedSitesGroup.Item is SitesGroup)
                                DisplaySites();
                            else
                            {
                                SelectedSite = selectedSitesGroup.Item as Sites.SiteUtilBase;
                                DisplayCategories(null, true);
                            }
                            break;
                        case State.sites:
                            if (GUI_facadeView.SelectedListItem.Label == "..")
                            {
                                ShowPreviousMenu();
                            }
                            else
                            {
                                SelectedSite = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as Sites.SiteUtilBase;
                                DisplayCategories(null, true);
                            }
                            break;
                        case State.categories:
                            if (GUI_facadeView.SelectedListItem.Label == "..")
                            {
                                ShowPreviousMenu();
                            }
                            else
                            {
                                Category categoryToDisplay = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as Category;
                                if (categoryToDisplay is NextPageCategory)
                                {
                                    DisplayCategories_NextPage(categoryToDisplay as NextPageCategory);
                                }
                                else if (categoryToDisplay is Sites.FavoriteUtil.FavoriteCategory)
                                {
                                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                                    {
                                        var favCat = categoryToDisplay as Sites.FavoriteUtil.FavoriteCategory;
                                        if (favCat.SiteCategory == null) favCat.DiscoverSiteCategory();
                                        return favCat;
                                    },
                                    delegate(bool success, object result)
                                    {
                                        if (success)
                                        {
                                            var favCat = result as Sites.FavoriteUtil.FavoriteCategory;
                                            if (favCat != null && favCat.SiteCategory != null)
                                            {
                                                currentNavigationContextSwitch = new NavigationContextSwitch()
                                                {
                                                    ReturnToUtil = SelectedSite,
                                                    ReturnToCategory = categoryToDisplay.ParentCategory,
                                                    GoToUtil = favCat.Site,
                                                    GoToCategory = favCat.SiteCategory,
                                                    BridgeCategory = favCat
                                                };
                                                SelectedSite = currentNavigationContextSwitch.GoToUtil;
                                                if (currentNavigationContextSwitch.GoToCategory.HasSubCategories)
                                                    DisplayCategories(currentNavigationContextSwitch.GoToCategory, true);
                                                else
                                                    DisplayVideos_Category(currentNavigationContextSwitch.GoToCategory, false);
                                            }
                                            else
                                            {
                                                GUIDialogNotify dlg_error = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                                                if (dlg_error != null)
                                                {
                                                    dlg_error.Reset();
                                                    dlg_error.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                                                    dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                                                    dlg_error.SetText(string.Format("{0}: {1}", Translation.Instance.Error, Translation.Instance.CategoryNotFound));
                                                    dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                                                }
                                            }
                                        }
                                    },
                                    Translation.Instance.GettingDynamicCategories, true);
                                }
                                else if (categoryToDisplay.HasSubCategories)
                                {
                                    DisplayCategories(categoryToDisplay, true);
                                }
                                else
                                {
                                    DisplayVideos_Category(categoryToDisplay, false);
                                }
                            }
                            break;
                        case State.videos:
                            ImageDownloader.StopDownload = true;
                            if (GUI_facadeView.SelectedListItem.Label == "..")
                            {
                                ShowPreviousMenu();
                            }
                            else if (GUI_facadeView.SelectedListItem.Label == Translation.Instance.NextPage)
                            {
                                DisplayVideos_NextPage();
                            }
                            else
                            {
                                selectedVideo = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo;
                                if (SelectedSite is IChoice && selectedVideo.HasDetails)
                                {
                                    // show details view
                                    DisplayDetails();
                                }
                                else if (SelectedSite is Sites.FavoriteUtil && selectedVideo.HasDetails &&
                                    (selectedCategory is Sites.FavoriteUtil.FavoriteCategory && (selectedCategory as Sites.FavoriteUtil.FavoriteCategory).Site is IChoice))
                                {
                                    SelectedSite = (selectedCategory as Sites.FavoriteUtil.FavoriteCategory).Site;
                                    // show details view
                                    DisplayDetails();
                                }
                                else
                                {
                                    //play the video
                                    currentPlaylist = null;
                                    currentPlayingItem = null;

                                    Play_Step1(new PlayListItem(null, null)
                                            {
                                                Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                                                Video = selectedVideo,
                                                Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[selectedVideo.SiteName] : selectedSite
                                            }, true);
                                }
                            }
                            break;
                    }
                }
                else if (CurrentState == State.videos && !(SelectedSite is IChoice && selectedVideo.HasDetails) &&
                    (actionType == Action.ActionType.ACTION_MUSIC_PLAY || actionType == Action.ActionType.ACTION_PLAY))
                {
                    VideoInfo videoPressedPlayOn = (GUI_facadeView.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo;
                    if (videoPressedPlayOn != null)
                    {
                        ImageDownloader.StopDownload = true;

                        currentFilter.Clear();
                        GUIPropertyManager.SetProperty("#OnlineVideos.filter", string.Empty);

                        selectedVideo = videoPressedPlayOn;

                        //play the video
                        currentPlaylist = null;
                        currentPlayingItem = null;

                        Play_Step1(new PlayListItem(null, null)
                        {
                            Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                            Video = selectedVideo,
                            Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[selectedVideo.SiteName] : selectedSite
                        }, true, true);
                    }
                }
                else if (CurrentState == State.videos && actionType == Action.ActionType.ACTION_SHOW_INFO)
                {
                    // toggles showing detailed info about a selectected video - is automatically reset when leaving videos view
                    ExtendedVideoInfo = !ExtendedVideoInfo;
                }
            }
            else if (control == GUI_infoList && CurrentState == State.details &&
                (actionType == Action.ActionType.ACTION_SELECT_ITEM || actionType == Action.ActionType.ACTION_MUSIC_PLAY || actionType == Action.ActionType.ACTION_PLAY))
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
                    currentPlayingItem = null;
                    Play_Step1(new PlayListItem(null, null)
                    {
                        Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                        Video = (GUI_infoList.SelectedListItem as OnlineVideosGuiListItem).Item as VideoInfo,
                        Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[selectedVideo.SiteName] : selectedSite
                    }, true, actionType != Action.ActionType.ACTION_SELECT_ITEM);
                }
            }
            else if (control == GUI_btnViewAs)
            {
                ToggleFacadeViewMode();
                // store as preferred layout in DB
                if (SelectedSite != null && PluginConfiguration.Instance.StoreLayoutPerCategory && (currentState == State.categories || currentState == State.videos))
                {
                    FavoritesDatabase.Instance.SetPreferredLayout(SelectedSite.Settings.Name, selectedCategory, (int)currentView);
                }
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
                Display_SearchResults();
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
                        if (CurrentState == State.groups) DisplayGroups();
                        else DisplaySites();
                    }
                }
            }
            else if (control == GUI_btnCurrentDownloads)
            {
                // go to current downloads
                Sites.SiteUtilBase aSite = null;
                if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(Translation.Instance.DownloadedVideos, out aSite))
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        if (!aSite.Settings.DynamicCategoriesDiscovered)
                        {
                            Log.Instance.Info("Looking for dynamic categories on Site '{0}'", aSite.Settings.Name);
                            int foundCategories = aSite.DiscoverDynamicCategories();
                            Log.Instance.Info("Found {0} dynamic categories on Site '{1}'", foundCategories, aSite.Settings.Name);
                        }
                        return aSite.Settings.Categories;
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            Category aCategory = aSite.Settings.Categories.FirstOrDefault(c => c.Name == Translation.Instance.Downloading);
                            if (aCategory != null)
                            {
                                SelectedSite = aSite;
                                selectedCategory = aCategory;
                                DisplayVideos_Category(aCategory, true);
                            }
                        }
                    },
                    Translation.Instance.GettingDynamicCategories, true);
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
                    // adult site, Downloads or Favorites might show adult videos or categories, so reset to sites overview
                    if (SelectedSite != null && (SelectedSite.Settings.ConfirmAge || SelectedSite is Sites.FavoriteUtil || SelectedSite is Sites.DownloadedVideoUtil))
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
            GUIControl.UnfocusControl(GetID, iControlId);
            GUIControl.DeSelectControl(GetID, iControlId);
            GUIControl.HideControl(GetID, iControlId);
            GUIControl.DisableControl(GetID, iControlId);
        }

        private void DisplayGroups()
        {
            var sitesGroups = PluginConfiguration.Instance.SitesGroups;
            if ((sitesGroups == null || sitesGroups.Count == 0) && PluginConfiguration.Instance.autoGroupByLang) sitesGroups = PluginConfiguration.Instance.CachedAutomaticSitesGroups;

            SelectedSite = null;
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);

            // add Favorites and Downloads Site as first two Groups (if they are available and user configured them to be first)
            if (OnlineVideoSettings.Instance.FavoritesFirst) AddFavoritesAndDownloadsSitesToFacade();

            HashSet<string> groupedSites = new HashSet<string>();
            foreach (SitesGroup sitesGroup in sitesGroups)
            {
                if (sitesGroup.Sites != null && sitesGroup.Sites.Count > 0)
                {
                    OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem(sitesGroup);
                    loListItem.OnItemSelected += OnItemSelected;
                    loListItem.ItemId = GUI_facadeView.Count;
                    GUI_facadeView.Add(loListItem);
                    if (selectedSitesGroup != null && selectedSitesGroup.Label == sitesGroup.Name) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
                }

                foreach (string site in sitesGroup.Sites) groupedSites.Add(site);
            }

            // add the item for all ungrouped sites if there are any
            SitesGroup othersGroup = new SitesGroup() { Name = Translation.Instance.Others };
            foreach (string site in OnlineVideoSettings.Instance.SiteUtilsList.Keys)
                if (!groupedSites.Contains(site) && site != Translation.Instance.Favourites && site != Translation.Instance.DownloadedVideos)
                    othersGroup.Sites.Add(site);
            if (othersGroup.Sites.Count > 0)
            {
                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(othersGroup);
                listItem.OnItemSelected += OnItemSelected;
                listItem.ItemId = GUI_facadeView.Count;
                GUI_facadeView.Add(listItem);
                if (selectedSitesGroup != null && selectedSitesGroup.Label == othersGroup.Name) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
            }

            // add Favorites and Downloads Site as last two Groups (if they are available)
            if (!OnlineVideoSettings.Instance.FavoritesFirst) AddFavoritesAndDownloadsSitesToFacade();

            CurrentState = State.groups;
            UpdateViewState();
        }

        private void AddFavoritesAndDownloadsSitesToFacade()
        {
            Sites.SiteUtilBase aSite;
            if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(Translation.Instance.Favourites, out aSite))
            {
                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(aSite);
                listItem.OnItemSelected += OnItemSelected;
                listItem.ItemId = GUI_facadeView.Count;
                GUI_facadeView.Add(listItem);
                if (selectedSitesGroup != null && selectedSitesGroup.Label == listItem.Label) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
            }
            if (OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(Translation.Instance.DownloadedVideos, out aSite))
            {
                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(aSite);
                listItem.OnItemSelected += OnItemSelected;
                listItem.ItemId = GUI_facadeView.Count;
                GUI_facadeView.Add(listItem);
                if (selectedSitesGroup != null && selectedSitesGroup.Label == listItem.Label) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
            }
        }

        private void DisplaySites()
        {
            lastSearchQuery = string.Empty;
            selectedCategory = null;
            ResetSelectedSite();
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            currentFacadeItems.Clear();

            // set order by options
            GUI_btnOrderBy.Clear();
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Instance.Default);
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Instance.Name);
            GUIControl.AddItemLabelControl(GetID, GUI_btnOrderBy.GetID, Translation.Instance.Language);
            GUI_btnOrderBy.SelectedItem = (int)PluginConfiguration.Instance.siteOrder;

            // previous selected group was actually a site or currently selected site Fav or Downl and groups enabled -> skip this step
            if (GroupsEnabled &&
                ((selectedSitesGroup != null && selectedSitesGroup.Item is Sites.SiteUtilBase) ||
                (selectedSite is Sites.FavoriteUtil || selectedSite is Sites.DownloadedVideoUtil)))
            {
                DisplayGroups();
                return;
            }
            var siteutils = OnlineVideoSettings.Instance.SiteUtilsList;
            string[] names = selectedSitesGroup == null ? siteutils.Keys.ToArray() : (selectedSitesGroup.Item as SitesGroup).Sites.ToArray();

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
                        if (siteutils.TryGetValue(name, out aSite))
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

            if (GroupsEnabled)
            {
                // add the first item that will go to the groups menu
                OnlineVideosGuiListItem loListItem;
                loListItem = new OnlineVideosGuiListItem("..");
                loListItem.ItemId = 0;
                loListItem.IsFolder = true;
                loListItem.OnItemSelected += OnItemSelected;
                MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                GUI_facadeView.Add(loListItem);
                currentFacadeItems.Add(loListItem);
            }

            int selectedSiteIndex = 0;  // used to remember the position of the last selected site
            currentFilter.StartMatching();
            foreach (string name in names)
            {
                Sites.SiteUtilBase aSite;
                if (currentFilter.Matches(name) &&
                    siteutils.TryGetValue(name, out aSite) &&
                    aSite.Settings.IsEnabled &&
                    !(GroupsEnabled & (aSite is Sites.FavoriteUtil | aSite is Sites.DownloadedVideoUtil)) && // don't show Favorites and Downloads site if groups are enabled (because they are added as groups)
                    (!aSite.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                {
                    OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem(aSite);
                    loListItem.OnItemSelected += OnItemSelected;
                    if (loListItem.Item == SelectedSite) selectedSiteIndex = GUI_facadeView.Count;
                    loListItem.ItemId = GUI_facadeView.Count;
                    GUI_facadeView.Add(loListItem);
                    currentFacadeItems.Add(loListItem);
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
                        Log.Instance.Info("Looking for dynamic categories for site '{0}'", SelectedSite.Settings.Name);
                        int foundCategories = SelectedSite.DiscoverDynamicCategories();
                        Log.Instance.Info("Found {0} dynamic categories for site '{1}'", foundCategories, SelectedSite.Settings.Name);
                        return SelectedSite.Settings.Categories;
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            SetCategoriesToFacade(parentCategory, result as IList<Category>, diveDownOrUpIfSingle);
                        }
                    },
                    Translation.Instance.GettingDynamicCategories, true);
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
                        Log.Instance.Info("Looking for subcategories in '{0}' on site '{1}'", parentCategory.Name, SelectedSite.Settings.Name);
                        int foundCategories = SelectedSite.DiscoverSubCategories(parentCategory);
                        Log.Instance.Info("Found {0} subcategories in '{1}' on site '{2}'", foundCategories, parentCategory.Name, SelectedSite.Settings.Name);
                        return parentCategory.SubCategories;
                    },
                    delegate(bool success, object result)
                    {
                        if (success)
                        {
                            SetCategoriesToFacade(parentCategory, result as IList<Category>, diveDownOrUpIfSingle);
                        }
                    },
                    Translation.Instance.GettingDynamicCategories, true);
                }
                else
                {
                    SetCategoriesToFacade(parentCategory, parentCategory.SubCategories, diveDownOrUpIfSingle);
                }
            }
        }

        private void DisplayCategories_NextPage(NextPageCategory cat)
        {
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
            {
                return SelectedSite.DiscoverNextPageCategories(cat);
            },
            delegate(bool success, object result)
            {
                if (success) SetCategoriesToFacade(cat.ParentCategory, cat.ParentCategory == null ? SelectedSite.Settings.Categories as IList<Category> : cat.ParentCategory.SubCategories, false, true);
            },
            Translation.Instance.GettingNextPageVideos, true);
        }

        KeyValuePair<string, List<string>> cachedFavoritedCategoriesOfSelectedSite;
        List<string> FavoritedCategoriesOfSelectedSite
        {
            get
            {
                string siteName = SelectedSite.Settings.Name;
                if (!string.IsNullOrEmpty(siteName) && cachedFavoritedCategoriesOfSelectedSite.Key != siteName)
                {
                    cachedFavoritedCategoriesOfSelectedSite = new KeyValuePair<string, List<string>>(siteName, OnlineVideoSettings.Instance.FavDB.getFavoriteCategoriesNames(siteName));
                }
                return cachedFavoritedCategoriesOfSelectedSite.Value;
            }
        }

        private void SetCategoriesToFacade(Category parentCategory, IList<Category> categories, bool? diveDownOrUpIfSingle, bool append = false)
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

            int categoryIndexToSelect = (categories != null && categories.Count > 0) ? 1 : 0; // select the first category by default if there is one
            if (append)
            {
                currentFilter.Clear();
                categoryIndexToSelect = GUI_facadeView.Count - 1;
            }

            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            currentFacadeItems.Clear();

            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem loListItem;
            loListItem = new OnlineVideosGuiListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            loListItem.OnItemSelected += OnItemSelected;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_facadeView.Add(loListItem);
            currentFacadeItems.Add(loListItem);

            Dictionary<string, bool> imageHash = new Dictionary<string, bool>();
            suggestedView = null;
            currentFilter.StartMatching();
            if (categories != null)
            {
                foreach (Category loCat in categories)
                {
                    if (currentFilter.Matches(loCat.Name))
                    {
                        loListItem = new OnlineVideosGuiListItem(loCat);
                        loListItem.ItemId = GUI_facadeView.Count;
                        if (loCat is NextPageCategory)
                        {
                            loListItem.IconImage = "OnlineVideos\\NextPage.png";
                            loListItem.IconImageBig = "OnlineVideos\\NextPage.png";
                            loListItem.ThumbnailImage = "OnlineVideos\\NextPage.png";
                        }
                        else
                        {
                            if (FavoritedCategoriesOfSelectedSite.Contains(loCat.RecursiveName("|")))
                            {
                                loListItem.IsPlayed = true;
                                loListItem.PinImage = SiteImageExistenceCache.GetImageForSite(Translation.Instance.Favourites, type: "Icon");
                            }
                        }
                        if (!string.IsNullOrEmpty(loCat.Thumb)) imageHash[loCat.Thumb] = true;
                        loListItem.OnItemSelected += OnItemSelected;
                        if (loCat == selectedCategory) categoryIndexToSelect = GUI_facadeView.Count; // select the category that was previously selected
                        GUI_facadeView.Add(loListItem);
                        currentFacadeItems.Add(loListItem);
                    }
                }

                if (imageHash.Count > 0) ImageDownloader.GetImages<Category>(categories);
                if ((GUI_facadeView.Count > 1 && imageHash.Count == 0) || (GUI_facadeView.Count > 2 && imageHash.Count == 1)) suggestedView = GUIFacadeControl.Layout.List;
                // only set selected index when not doing an automatic dive up (MediaPortal would set the old selected index asynchroneously)
                if (!(categories.Count == 1 && diveDownOrUpIfSingle == false)) GUI_facadeView.SelectedListItemIndex = categoryIndexToSelect;
            }

            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            CurrentState = State.categories;
            selectedCategory = parentCategory;
            if (PluginConfiguration.Instance.StoreLayoutPerCategory) suggestedView = FavoritesDatabase.Instance.GetPreferredLayout(SelectedSite.Settings.Name, selectedCategory) ?? suggestedView;
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
            Translation.Instance.GettingVideoDetails, true);
        }

        private void SetVideosToInfoList(List<VideoInfo> loVideoList)
        {
            SetGuiProperties_ExtendedVideoInfo(null, false);
            currentTrailerList = loVideoList;
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            GUIControl.ClearControl(GetID, GUI_infoList.GetID);
            OnlineVideosGuiListItem loListItem = new OnlineVideosGuiListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            loListItem.OnItemSelected += OnItemSelected;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            GUI_infoList.Add(loListItem);
            Dictionary<string, bool> imageHash = new Dictionary<string, bool>();
            if (loVideoList != null)
            {
                foreach (VideoInfo loVideoInfo in loVideoList)
                {
                    loListItem = new OnlineVideosGuiListItem(loVideoInfo, true);
                    loListItem.ItemId = GUI_infoList.Count;
                    loListItem.OnItemSelected += OnItemSelected;
                    GUI_infoList.Add(loListItem);
                    if (!string.IsNullOrEmpty(loVideoInfo.ImageUrl)) imageHash[loVideoInfo.ImageUrl] = true;
                }
            }
            if (imageHash.Count > 0) ImageDownloader.GetImages<VideoInfo>(currentTrailerList);

            if (loVideoList.Count > 0)
            {
                if (selectedClipIndex == 0 || selectedClipIndex >= GUI_infoList.Count) selectedClipIndex = 1;
                GUI_infoList.SelectedListItemIndex = selectedClipIndex;
                OnItemSelected(GUI_infoList[selectedClipIndex], GUI_infoList);
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

                    // reset a navigation context switch if it was set for this operation
                    if (currentNavigationContextSwitch != null && currentNavigationContextSwitch.GoToCategory == category)
                    {
                        SelectedSite = currentNavigationContextSwitch.ReturnToUtil;
                        selectedCategory = currentNavigationContextSwitch.ReturnToCategory;
                        currentNavigationContextSwitch = null;
                    }

                    if (displayCategoriesOnError)// an error occured or no videos were found -> return to the category selection if param was set
                    {
                        DisplayCategories(category.ParentCategory, false);
                    }
                }
            },
            Translation.Instance.GettingCategoryVideos, true);
        }

		private void ContextKeywordSelection(List<string> searchexpressions)
		{
			string query = null;
			const int minchars = 4;
			string[] sep = new string[] { "|", " ", ",", ";" };
			string[] titlesep = { " - " };
			int totalitems = 0;

			GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
			if (dlg == null) return;
			dlg.SetHeading(Translation.Instance.SearchRelatedKeywords);

			if (searchexpressions.Count > 0)
			{
				// try to get expression parts from title
				List<string> titleexpressions = CleanExpression(searchexpressions[0]).Split(titlesep, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrEmpty(s) && s.Length >= minchars).Distinct().ToList();

				foreach (GUIListItem item in titleexpressions.Select(keyword => new GUIListItem(keyword.Trim().TrimEnd('.').TrimEnd(':'))))
				{
					dlg.Add(item);
					totalitems++;
				}
			}

			// add keywords
			foreach (string searchexpression in searchexpressions)
			{
				List<string> keywords = CleanExpression(searchexpression).Split(sep, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrEmpty(s) && s.Length >= minchars).OrderByDescending(x => x.Length).Distinct().ToList();

				foreach (GUIListItem item in keywords.Select(keyword => new GUIListItem(keyword.Trim().TrimEnd('.').TrimEnd(':'))))
				{
					dlg.Add(item);
					totalitems++;
				}
			}

			Log.Instance.Info("Found '{0}' keywords for user selection", totalitems);
			if (totalitems == 0) return;

			dlg.SelectedLabel = 0;
			dlg.DoModal(GUIWindowManager.ActiveWindow);
			if (dlg.SelectedLabel == -1) return;
			query = dlg.SelectedLabelText;
			Display_SearchResults(query);
		}

        private string CleanExpression(string expression)
        {
          // Clean searchexpression
          expression = expression.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\n\r", " ");
          Regex oRegexReplace = new Regex("[,;!?'\"()]");
          MatchCollection oMatches = oRegexReplace.Matches(expression);
          expression = oMatches.Cast<Match>().Aggregate(expression, (current, match) => current.Replace(match.Value, oRegexReplace.Replace(match.Value, string.Empty))).Replace("  ", " ").Trim();
          return expression;
        }

        private void Display_SearchResults(string query = null)
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
                        dlgSel.SetHeading(Translation.Instance.SearchHistory);
                        dlgSel.Add(Translation.Instance.NewSearch);
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
				lastSearchQuery = query;
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    if (moSupportedSearchCategoryList != null && moSupportedSearchCategoryList.Count > 1 && GUI_btnSearchCategories.SelectedLabel != Translation.Instance.All
                        && !string.IsNullOrEmpty(GUI_btnSearchCategories.SelectedLabel) && moSupportedSearchCategoryList.ContainsKey(GUI_btnSearchCategories.SelectedLabel))
                    {
                        string category = moSupportedSearchCategoryList[GUI_btnSearchCategories.SelectedLabel];
                        Log.Instance.Info("Searching for {0} in category {1}", query, category);
                        lastSearchCategory = category;
                        return SelectedSite.DoSearch(query, category);
                    }
                    else
                    {
                        Log.Instance.Info("Searching for {0} in all categories ", query);
                        return SelectedSite.DoSearch(query);
                    }
                },
                delegate(bool success, object result)
                {
                    List<ISearchResultItem> resultList = (result as List<ISearchResultItem>);
                    // set videos to the facade -> if none were found and an empty facade is currently shown, go to previous menu
                    if ((!success || resultList == null || resultList.Count == 0) && GUI_facadeView.Count == 0)
                    {
                        if (loadParamInfo != null && loadParamInfo.ShowVKonFailedSearch && GetUserInputString(ref query, false)) Display_SearchResults(query);
                        else ShowPreviousMenu();
                    }
                    else
                    {
                        SetSearchResultItemsToFacade(resultList, VideosMode.Search, Translation.Instance.SearchResults + " [" + lastSearchQuery + "]");
                    }
                },
                Translation.Instance.GettingSearchResults, true);
            }
        }

        private void SetSearchResultItemsToFacade(List<ISearchResultItem> resultList, VideosMode mode = VideosMode.Search, string categoryName = "")
        {
            if (resultList != null && resultList.Count > 0)
            {
                if (resultList[0] is VideoInfo)
                {
                    SetVideosToFacade(resultList.ConvertAll(i => i as VideoInfo), mode);
                    // if only 1 result found and the current site has a details view for this video - open it right away
                    if (SelectedSite is IChoice && resultList.Count == 1 && (resultList[0] as VideoInfo).HasDetails)
                    {
                        // actually select this item, so fanart can be shown in this and the coming screen! (fanart handler inspects the #selecteditem proeprty of teh facade)
                        GUI_facadeView.SelectedListItemIndex = 1;
                        selectedVideo = (GUI_facadeView[1] as OnlineVideosGuiListItem).Item as VideoInfo;
                        DisplayDetails();
                    }
                }
                else
                {
                    Category searchCategory = OnlineVideosAppDomain.Domain.CreateInstanceAndUnwrap(typeof(Category).Assembly.FullName, typeof(Category).FullName) as Category;
                    searchCategory.Name = categoryName;
                    searchCategory.HasSubCategories = true;
                    searchCategory.SubCategoriesDiscovered = true;
                    searchCategory.SubCategories = resultList.ConvertAll(i => { (i as Category).ParentCategory = searchCategory; return i as Category; });
                    SetCategoriesToFacade(searchCategory, searchCategory.SubCategories, true);
                }
            }
            else
            {
                GUIDialogNotify dlg_error = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg_error != null)
                {
                    dlg_error.Reset();
                    dlg_error.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                    dlg_error.SetText(Translation.Instance.NoVideoFound);
                    dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                }
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
            , Translation.Instance.GettingFilteredVideos, true);
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
            Translation.Instance.GettingNextPageVideos, true);
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
                    dlg_error.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                    dlg_error.SetText(Translation.Instance.NoVideoFound);
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
            currentFacadeItems.Clear();

            // add the first item that will go to the previous menu
            OnlineVideosGuiListItem backItem = new OnlineVideosGuiListItem("..");
            backItem.ItemId = 0;
            backItem.IsFolder = true;
            backItem.OnItemSelected += OnItemSelected;
            MediaPortal.Util.Utils.SetDefaultIcons(backItem);
            GUI_facadeView.Add(backItem);
            currentFacadeItems.Add(backItem);

            // add the items
            Dictionary<string, bool> imageHash = new Dictionary<string, bool>();
            currentFilter.StartMatching();

            foreach (VideoInfo videoInfo in currentVideoList)
            {
                videoInfo.CleanDescriptionAndTitle();
                if (!currentFilter.Matches(videoInfo.Title) || FilterOut(videoInfo.Title) || FilterOut(videoInfo.Description)) continue;
                if (!string.IsNullOrEmpty(videosVKfilter) && !videoInfo.Title.ToLower().Contains(videosVKfilter.ToLower())) continue;

                OnlineVideosGuiListItem listItem = new OnlineVideosGuiListItem(videoInfo);
                listItem.ItemId = GUI_facadeView.Count;
                listItem.OnItemSelected += OnItemSelected;
                GUI_facadeView.Add(listItem);
                currentFacadeItems.Add(listItem);

                if (listItem.Item == selectedVideo) GUI_facadeView.SelectedListItemIndex = GUI_facadeView.Count - 1;
                if (!string.IsNullOrEmpty(videoInfo.ImageUrl)) imageHash[videoInfo.ImageUrl] = true;
            }
            // fall back to list view if there are no items with thumbs or more than one item and all have the same thumb
            suggestedView = null;
            if ((GUI_facadeView.Count > 1 && imageHash.Count == 0) || (GUI_facadeView.Count > 2 && imageHash.Count == 1)) suggestedView = GUIFacadeControl.Layout.List;

            if (SelectedSite.HasNextPage)
            {
                OnlineVideosGuiListItem nextPageItem = new OnlineVideosGuiListItem(Translation.Instance.NextPage);
                nextPageItem.ItemId = GUI_facadeView.Count;
                nextPageItem.IsFolder = true;
                nextPageItem.IconImage = "OnlineVideos\\NextPage.png";
                nextPageItem.IconImageBig = "OnlineVideos\\NextPage.png";
                nextPageItem.ThumbnailImage = "OnlineVideos\\NextPage.png";
                nextPageItem.OnItemSelected += OnItemSelected;
                GUI_facadeView.Add(nextPageItem);
                currentFacadeItems.Add(nextPageItem);
            }

            if (indextoSelect > -1 && indextoSelect < GUI_facadeView.Count) GUI_facadeView.SelectedListItemIndex = indextoSelect;

            if (imageHash.Count > 0) ImageDownloader.GetImages<VideoInfo>(currentVideoList);

            string filterstring = currentFilter.ToString();
            if (!string.IsNullOrEmpty(filterstring) && !string.IsNullOrEmpty(videosVKfilter)) filterstring += " & ";
            filterstring += videosVKfilter;
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", filterstring);

            currentVideosDisplayMode = mode;
            CurrentState = State.videos;
            if (PluginConfiguration.Instance.StoreLayoutPerCategory) suggestedView = FavoritesDatabase.Instance.GetPreferredLayout(SelectedSite.Settings.Name, selectedCategory) ?? suggestedView;
            UpdateViewState();
            return true;
        }

        private void ShowPreviousMenu()
        {
            ImageDownloader.StopDownload = true;

            if (CurrentState == State.sites)
            {
                if (GroupsEnabled)
                {
                    // if plugin was called with loadParameter set to the current group and return locked -> go to previous window 
                    if (loadParamInfo != null && loadParamInfo.Return == LoadParameterInfo.ReturnMode.Locked && loadParamInfo.Group == selectedSitesGroup.Label)
                        OnPreviousWindow();
                    else
                        DisplayGroups();
                }
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
                    {
                        if (currentNavigationContextSwitch != null && currentNavigationContextSwitch.GoToCategory == selectedCategory)
                        {
                            SelectedSite = currentNavigationContextSwitch.ReturnToUtil;
                            selectedCategory = currentNavigationContextSwitch.BridgeCategory;
                            var categoryToReturnTo = currentNavigationContextSwitch.ReturnToCategory;
                            currentNavigationContextSwitch = null;
                            DisplayCategories(categoryToReturnTo, false);
                        }
                        else
                        {
                            DisplayCategories(selectedCategory.ParentCategory, false);
                        }
                    }
                }
            }
            else if (CurrentState == State.videos)
            {
                videosVKfilter = string.Empty;
                // if plugin was called with loadParameter set to the current site with searchstring and return locked and currently displaying the searchresults or videos for the category from loadParam -> go to previous window 
                if (loadParamInfo != null && loadParamInfo.Return == LoadParameterInfo.ReturnMode.Locked && loadParamInfo.Site == selectedSite.Settings.Name &&
                    (currentVideosDisplayMode == VideosMode.Search ||
                    (currentVideosDisplayMode == VideosMode.Category && selectedCategory != null && loadParamInfo.Category == selectedCategory.Name))
                   )
                    OnPreviousWindow();
                else
                {
                    if (currentNavigationContextSwitch != null && currentNavigationContextSwitch.GoToCategory == selectedCategory)
                    {
                        SelectedSite = currentNavigationContextSwitch.ReturnToUtil;
                        selectedCategory = currentNavigationContextSwitch.BridgeCategory;
                        var categoryToReturnTo = currentNavigationContextSwitch.ReturnToCategory;
                        currentNavigationContextSwitch = null;
                        DisplayCategories(categoryToReturnTo, false);
                    }
                    else
                    {
                        if (selectedCategory == null || selectedCategory.ParentCategory == null) DisplayCategories(null, false);
                        else DisplayCategories(selectedCategory.ParentCategory, false);
                    }
                }
            }
            else if (CurrentState == State.details)
            {
                if (selectedCategory is Sites.FavoriteUtil.FavoriteCategory && !(SelectedSite is Sites.FavoriteUtil))
                {
                    SelectedSite = (selectedCategory as Sites.FavoriteUtil.FavoriteCategory).FavSite;
                }
                GUIControl.UnfocusControl(GetID, GUI_infoList.GetID);
                GUI_infoList.Focus = false;
                selectedClipIndex = 0;
                SetVideosToFacade(currentVideoList, currentVideosDisplayMode);
            }
        }

        void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            OnlineVideosGuiListItem ovItem = item as OnlineVideosGuiListItem;
            if (parent == GUI_infoList)
            {
                SetGuiProperties_ExtendedVideoInfo(ovItem != null ? ovItem.Item as VideoInfo : null, true);
            }
            else
            {
                SetGuiProperties_ExtendedVideoInfo(ovItem != null ? ovItem.Item as VideoInfo : null, false);
                GUIPropertyManager.SetProperty("#OnlineVideos.desc", ovItem != null ? ovItem.Description : string.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.length", ovItem != null && ovItem.Item is VideoInfo ? VideoInfo.GetDuration((ovItem.Item as VideoInfo).Length) : string.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.aired", ovItem != null && ovItem.Item is VideoInfo ? (ovItem.Item as VideoInfo).Airdate : string.Empty);
            }
        }

        internal static bool GetUserInputString(ref string sString, bool password)
        {
            VirtualKeyboard keyBoard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            if (keyBoard == null) return false;
            keyBoard.Reset();
            keyBoard.SetLabelAsInitialText(false); // set to false, otherwise our intial text is cleared
            keyBoard.Text = sString;
            keyBoard.Password = password;
            keyBoard.DoModal(GUIWindowManager.ActiveWindow); // show it...
            if (keyBoard.IsConfirmed) sString = keyBoard.Text;
            return keyBoard.IsConfirmed;
        }

        void g_Player_PlayBackEnded(g_Player.MediaType type, string filename)
        {
            try
            {
                if (currentPlayingItem != null && currentPlayingItem.Util != null)
                {
                    double percent = g_Player.Duration > 0 ? g_Player.CurrentPosition / g_Player.Duration : 0;
                    currentPlayingItem.Util.OnPlaybackEnded(currentPlayingItem.Video, currentPlayingItem.FileName, percent, false);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn("Error on Util.OnPlaybackEnded: {0}", ex);
            }

            if (currentPlaylist != null)
            {
                if ((g_Player.Player != null && g_Player.Player.GetType().Assembly == typeof(GUIOnlineVideos).Assembly) ||
                     g_Player.Player == null && (filename == "http://localhost/OnlineVideo.mp4" || (currentPlayingItem != null && filename == currentPlayingItem.FileName)))
                {
                    PlayNextPlaylistItem();
                }
                else
                {
                    // some other playback ended, and a playlist is still set here -> clear it
                    currentPlaylist = null;
                    currentPlayingItem = null;
                }
            }
            else
            {
                TrackPlayback();
                currentPlayingItem = null;
            }
        }

        void PlayNextPlaylistItem()
        {
            int currentPlaylistIndex = currentPlayingItem != null ? currentPlaylist.IndexOf(currentPlayingItem) : 0;
            if (currentPlaylist.Count > currentPlaylistIndex + 1)
            {
                // if playing a playlist item, move to the next            
                currentPlaylistIndex++;
                Play_Step1(currentPlaylist[currentPlaylistIndex], GUIWindowManager.ActiveWindow == GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            }
            else
            {
                // if last item -> clear the list
                TrackPlayback();
                currentPlaylist = null;
                currentPlayingItem = null;
            }
        }

        void g_Player_PlayBackStopped(g_Player.MediaType type, int stoptime, string filename)
        {
            try
            {
                if (currentPlayingItem != null && currentPlayingItem.Util != null)
                {
                    double percent = g_Player.Duration > 0 ? g_Player.CurrentPosition / g_Player.Duration : 0;
                    currentPlayingItem.Util.OnPlaybackEnded(currentPlayingItem.Video, currentPlayingItem.FileName, percent, true);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn("Error on Util.OnPlaybackEnded: {0}", ex);
            }

            if (stoptime > 0 && g_Player.Duration > 0 && (stoptime / g_Player.Duration) > 0.8) TrackPlayback();
            currentPlayingItem = null;
        }

        void TrackPlayback()
        {
            double percent = g_Player.Duration > 0 ? g_Player.CurrentPosition / g_Player.Duration : 0;
            if (TrackVideoPlayback != null && currentPlayingItem != null && currentPlayingItem.Util != null && currentPlayingItem.Video != null)
            {
                new System.Threading.Thread((item) =>
                {
                    var myItem = item as PlayListItem;
                    ITrackingInfo info = myItem.Util.GetTrackingInfo(myItem.Video);
                    if (info.VideoKind == VideoKind.TvSeries || info.VideoKind == VideoKind.Movie) TrackVideoPlayback(info, percent);
                }) { IsBackground = true, Name = "OnlineVideosTracking" }.Start(currentPlayingItem);
            }
        }

        private void Play_Step1(PlayListItem playItem, bool goFullScreen, bool skipPlaybackOptionsDialog = false)
        {
            if (!string.IsNullOrEmpty(playItem.FileName))
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return SelectedSite.getPlaylistItemUrl(playItem.Video, currentPlaylist[0].ChosenPlaybackOption, currentPlaylist.IsPlayAll);
                },
                delegate(bool success, object result)
                {
                    if (success) Play_Step2(playItem, new List<String>() { result as string }, goFullScreen, skipPlaybackOptionsDialog);
                    else if (currentPlaylist != null && currentPlaylist.Count > 1) PlayNextPlaylistItem();
                }
                , Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
            else
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return SelectedSite.getMultipleVideoUrls(playItem.Video, currentPlaylist != null && currentPlaylist.Count > 1);
                },
                delegate(bool success, object result)
                {
                    if (success) Play_Step2(playItem, result as List<String>, goFullScreen, skipPlaybackOptionsDialog);
                    else if (currentPlaylist != null && currentPlaylist.Count > 1) PlayNextPlaylistItem();
                }
                , Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
        }

        private void Play_Step2(PlayListItem playItem, List<String> loUrlList, bool goFullScreen, bool skipPlaybackOptionsDialog)
        {
            Utils.RemoveInvalidUrls(loUrlList);

            // if no valid urls were returned show error msg
            if (loUrlList == null || loUrlList.Count == 0)
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg.SetHeading(Translation.Instance.Error);
                    dlg.SetText(Translation.Instance.UnableToPlayVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }
            // create playlist entries if more than one url
            if (loUrlList.Count > 1)
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    Player.PlayList playbackItems = new Player.PlayList();
                    foreach (string url in loUrlList)
                    {
                        VideoInfo vi = playItem.Video.CloneForPlayList(url, url == loUrlList[0]);
                        string url_new = url;
                        if (url == loUrlList[0])
                        {
                            url_new = SelectedSite.getPlaylistItemUrl(vi, string.Empty, currentPlaylist != null && currentPlaylist.IsPlayAll);
                        }
                        PlayListItem pli = new PlayListItem(string.Format("{0} - {1} / {2}", playItem.Video.Title, (playbackItems.Count + 1).ToString(), loUrlList.Count), url_new);
                        pli.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream;
                        pli.Video = vi;
                        pli.Util = playItem.Util;
                        pli.ForcedPlayer = playItem.ForcedPlayer;
                        playbackItems.Add(pli);
                    }
                    if (currentPlaylist == null)
                    {
                        currentPlaylist = playbackItems;
                    }
                    else
                    {
                        int currentPlaylistIndex = currentPlayingItem != null ? currentPlaylist.IndexOf(currentPlayingItem) : 0;
                        currentPlaylist.InsertRange(currentPlaylistIndex, playbackItems);
                    }
                    // make the first item the current to be played now
                    playItem = playbackItems[0];
                    loUrlList = new List<string>(new string[] { playItem.FileName });
                    return null;
                },
                delegate(bool success, object result)
                {
                    if (success) Play_Step3(playItem, loUrlList, goFullScreen, skipPlaybackOptionsDialog);
                    else currentPlaylist = null;
                }
                , Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
            else
            {
                Play_Step3(playItem, loUrlList, goFullScreen, skipPlaybackOptionsDialog);
            }
        }

        private void Play_Step3(PlayListItem playItem, List<String> loUrlList, bool goFullScreen, bool skipPlaybackOptionsDialog)
        {
            // if multiple quality choices are available show a selection dialogue (also on playlist playback)
            string lsUrl = loUrlList[0];
            bool resolve = DisplayPlaybackOptions(playItem.Video, ref lsUrl, skipPlaybackOptionsDialog); // resolve only when any playbackoptions were set
            if (lsUrl == "-1") return; // the user did not chose an option but canceled the dialog
            if (resolve)
            {
                playItem.ChosenPlaybackOption = lsUrl;
                // display wait cursor as GetPlaybackOptionUrl might do webrequests when overridden
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return playItem.Video.GetPlaybackOptionUrl(lsUrl);
                },
                delegate(bool success, object result)
                {
                    if (success) Play_Step4(playItem, result as string, goFullScreen);
                }
                , Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
            else
            {
                Play_Step4(playItem, lsUrl, goFullScreen);
            }
        }

        void Play_Step4(PlayListItem playItem, string lsUrl, bool goFullScreen)
        {
            // check for valid url and cut off additional parameter
            if (String.IsNullOrEmpty(lsUrl) ||
                !Utils.IsValidUri((lsUrl.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? lsUrl.Substring(0, lsUrl.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : lsUrl))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg.SetHeading(Translation.Instance.Error);
                    dlg.SetText(Translation.Instance.UnableToPlayVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            // stop player if currently playing some other video
            if (g_Player.Playing) g_Player.Stop();

            currentPlayingItem = null;

            OnlineVideos.MediaPortal1.Player.PlayerFactory factory = new OnlineVideos.MediaPortal1.Player.PlayerFactory(playItem.ForcedPlayer != null ? playItem.ForcedPlayer.Value : playItem.Util.Settings.Player, lsUrl);
            if (factory.PreparedPlayerType != PlayerType.Internal)
            {
                // external players can only be created on the main thread
                Play_Step5(playItem, lsUrl, goFullScreen, factory, true, true);
            }
            else
            {
                Log.Instance.Info("Preparing graph for playback of '{0}'", lsUrl);
                bool? prepareResult = ((OnlineVideosPlayer)factory.PreparedPlayer).PrepareGraph();
                switch (prepareResult)
                {
                    case true:// buffer in background
                        Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                        {
                            try
                            {
                                Log.Instance.Info("Start prebuffering ...");
                                BufferingPlayerFactory = factory;
                                if (((OnlineVideosPlayer)factory.PreparedPlayer).BufferFile())
                                {
                                    Log.Instance.Info("Prebuffering finished.");
                                    return true;
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
                            // success false means BufferFile threw an exception that was shown to the user - pass it as showMessage
                            Play_Step5(playItem, lsUrl, goFullScreen, factory, result as bool?, success);
                        },
                        Translation.Instance.StartingPlayback, false);
                        break;
                    case false:// play without buffering in background
                        Play_Step5(playItem, lsUrl, goFullScreen, factory, prepareResult, true);
                        break;
                    default: // error building graph
                        GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                        if (dlg != null)
                        {
                            dlg.Reset();
                            dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                            dlg.SetHeading(Translation.Instance.Error);
                            dlg.SetText(Translation.Instance.UnableToPlayVideo);
                            dlg.DoModal(GUIWindowManager.ActiveWindow);
                        }
                        break;
                }
            }
        }

        void Play_Step5(PlayListItem playItem, string lsUrl, bool goFullScreen, OnlineVideos.MediaPortal1.Player.PlayerFactory factory, bool? factoryPrepareResult, bool showMessage)
        {
            if (factoryPrepareResult == null)
            {
                if (factory.PreparedPlayer is OnlineVideosPlayer && (factory.PreparedPlayer as OnlineVideosPlayer).BufferingStopped == true) showMessage = false;
                factory.PreparedPlayer.Dispose();
                if (showMessage)
                {
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (dlg != null)
                    {
                        dlg.Reset();
                        dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        dlg.SetHeading(Translation.Instance.Error);
                        dlg.SetText(Translation.Instance.UnableToPlayVideo);
                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
            }
            else
            {
                (factory.PreparedPlayer as OVSPLayer).GoFullscreen = goFullScreen;

                Uri subtitleUri = null;
                bool validUri = !String.IsNullOrEmpty(playItem.Video.SubtitleUrl) && Uri.TryCreate(playItem.Video.SubtitleUrl, UriKind.Absolute, out subtitleUri);

                if (!string.IsNullOrEmpty(playItem.Video.SubtitleText) || (validUri && !subtitleUri.IsFile))
                {
                    // download subtitle file before starting playback
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        string subs = string.IsNullOrEmpty(playItem.Video.SubtitleText) ? Sites.SiteUtilBase.GetWebData(playItem.Video.SubtitleUrl) : playItem.Video.SubtitleText;
                        if (!string.IsNullOrEmpty(subs))
                        {
                            string subFile = Path.Combine(Path.GetTempPath(), "OnlineVideoSubtitles.txt");
                            File.WriteAllText(subFile, subs, System.Text.Encoding.UTF8);
                            (factory.PreparedPlayer as OVSPLayer).SubtitleFile = subFile;
                        }
                        return true;
                    },
                    delegate(bool success, object result)
                    {
                        Play_Step6(playItem, lsUrl, factory);
                    },
                    Translation.Instance.DownloadingSubtitle, true);
                }
                else
                {
                    if (validUri && subtitleUri.IsFile)
                        (factory.PreparedPlayer as OVSPLayer).SubtitleFile = subtitleUri.AbsolutePath;
                    Play_Step6(playItem, lsUrl, factory);
                }
            }
        }

        private void Play_Step6(PlayListItem playItem, string lsUrl, OnlineVideos.MediaPortal1.Player.PlayerFactory factory)
        {
            IPlayerFactory savedFactory = g_Player.Factory;
            g_Player.Factory = factory;
            try
            {
                if (factory.PreparedPlayer is OnlineVideosPlayer)
                    g_Player.Play("http://localhost/OnlineVideo.mp4", g_Player.MediaType.Video); // hack to get around the MP 1.3 Alpha bug with non http URLs
                else
                    g_Player.Play(lsUrl, g_Player.MediaType.Video);
            }
            catch (Exception ex) // since many plugins attach to the g_Player.PlayBackStarted event, this might throw unexpected errors
            {
                Log.Instance.Warn(ex.ToString());
            }
            g_Player.Factory = savedFactory;

            if (g_Player.Player != null && g_Player.HasVideo)
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
                playItem.FileName = lsUrl;
                currentPlayingItem = playItem;
                SetGuiProperties_PlayingVideo(playItem);
            }
        }

        private void PlayAll(bool random = false, VideoInfo startWith = null)
        {
            currentPlaylist = new Player.PlayList() { IsPlayAll = true };
            currentPlayingItem = null;
            List<VideoInfo> loVideoList = (SelectedSite is IChoice && currentState == State.details) ? currentTrailerList : currentVideoList;
			bool startVideoFound = startWith == null;
            foreach (VideoInfo video in loVideoList)
            {
                // when not in details view of a site with details view only include videos that don't have details
                if (currentState != State.details && SelectedSite is IChoice && video.HasDetails) continue;

                // filter out by the current filter
                if (!currentFilter.Matches(video.Title) || FilterOut(video.Title) || FilterOut(video.Description)) continue;
                if (!string.IsNullOrEmpty(videosVKfilter) && !video.Title.ToLower().Contains(videosVKfilter.ToLower())) continue;

				if (!startVideoFound && video != startWith) continue;
				else startVideoFound = true;

                currentPlaylist.Add(new Player.PlayListItem(video.Title, null)
                {
                    Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.VideoStream,
                    Video = video,
                    Util = selectedSite is Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName] : SelectedSite
                });
            }
            if (currentPlaylist.Count > 0)
            {
                if (random) ((List<PlayListItem>)currentPlaylist).Randomize();
                Play_Step1(currentPlaylist[0], true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveItems"></param>
        /// <param name="enque">null : download the next item in a DownloadList that is already in the Manager</param>
        private void SaveVideo_Step1(DownloadList saveItems, bool? enque = false)
        {
            if (string.IsNullOrEmpty(OnlineVideoSettings.Instance.DownloadDir))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg.SetHeading(Translation.Instance.Error);
                    dlg.SetText(Translation.Instance.SetDownloadFolderInConfig);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            if (enque != null) // when the DownloadManager already contains the current DownloadInfo of the given list - show already downloading message
            {
                if (DownloadManager.Instance.Contains(saveItems.CurrentItem))
                {
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (dlg != null)
                    {
                        dlg.Reset();
                        dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        dlg.SetHeading(Translation.Instance.Error);
                        dlg.SetText(Translation.Instance.AlreadyDownloading);
                        dlg.DoModal(GUIWindowManager.ActiveWindow);
                    }
                    return;
                }
                // check if there is already a download running from this site - yes? -> enque | no -> start now
                if (enque == true && DownloadManager.Instance.Contains(saveItems.CurrentItem.Util.Settings.Name))
                {
                    DownloadManager.Instance.Add(saveItems.CurrentItem.Util.Settings.Name, saveItems);
                    return;
                }
            }
            if (!string.IsNullOrEmpty(saveItems.CurrentItem.Url))
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return saveItems.CurrentItem.Util.getPlaylistItemUrl(saveItems.CurrentItem.VideoInfo, saveItems.ChosenPlaybackOption);
                },
                delegate(bool success, object result)
                {
                    if (success) SaveVideo_Step2(saveItems, new List<string>() { result as string }, enque);
                },
                Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
            else
            {
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                {
                    return saveItems.CurrentItem.Util.getMultipleVideoUrls(saveItems.CurrentItem.VideoInfo);
                },
                delegate(bool success, object result)
                {
                    if (success) SaveVideo_Step2(saveItems, result as List<String>, enque);
                },
                Translation.Instance.GettingPlaybackUrlsForVideo, true);
            }
        }

        private void SaveVideo_Step2(DownloadList saveItems, List<String> loUrlList, bool? enque)
        {
			Utils.RemoveInvalidUrls(loUrlList);

            // if no valid urls were returned show error msg
            if (loUrlList == null || loUrlList.Count == 0)
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg.SetHeading(Translation.Instance.Error);
                    dlg.SetText(Translation.Instance.UnableToDownloadVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            // create download list if more than one url
            if (loUrlList.Count > 1)
            {
                saveItems.DownloadItems = new List<DownloadInfo>();
                foreach (string url in loUrlList)
                {
                    VideoInfo vi = saveItems.CurrentItem.VideoInfo.CloneForPlayList(url, url == loUrlList[0]);
                    string url_new = url;
                    if (url == loUrlList[0])
                    {
                        url_new = saveItems.CurrentItem.Util.getPlaylistItemUrl(vi, string.Empty);
                    }
                    DownloadInfo pli = DownloadInfo.Create(vi, saveItems.CurrentItem.Category, saveItems.CurrentItem.Util);
                    pli.Title = string.Format("{0} - {1} / {2}", vi.Title, (saveItems.DownloadItems.Count + 1).ToString(), loUrlList.Count);
                    pli.Url = url_new;
                    pli.OverrideFolder = saveItems.CurrentItem.OverrideFolder;
                    pli.OverrideFileName = saveItems.CurrentItem.OverrideFileName;
                    saveItems.DownloadItems.Add(pli);
                }
                // make the first item the current to be saved now
                saveItems.CurrentItem = saveItems.DownloadItems[0];
                loUrlList = new List<string>(new string[] { saveItems.CurrentItem.Url });
            }
            // if multiple quality choices are available show a selection dialogue
            string lsUrl = loUrlList[0];
            bool resolve = DisplayPlaybackOptions(saveItems.CurrentItem.VideoInfo, ref lsUrl, enque == null); // skip dialog when downloading an item of a queue
            if (lsUrl == "-1") return; // user canceled the dialog -> don't download
            if (resolve)
            {
                saveItems.ChosenPlaybackOption = lsUrl;
                if (saveItems.CurrentItem.VideoInfo.GetType().FullName == typeof(VideoInfo).FullName)
                {
                    SaveVideo_Step3(saveItems, saveItems.CurrentItem.VideoInfo.GetPlaybackOptionUrl(lsUrl), enque);
                }
                else
                {
                    // display wait cursor as GetPlaybackOptionUrl might do webrequests when overridden
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        return saveItems.CurrentItem.VideoInfo.GetPlaybackOptionUrl(lsUrl);
                    },
                    delegate(bool success, object result)
                    {
                        if (success) SaveVideo_Step3(saveItems, result as string, enque);
                    }
                    , Translation.Instance.GettingPlaybackUrlsForVideo, true);
                }
            }
            else
            {
                SaveVideo_Step3(saveItems, lsUrl, enque);
            }
        }

        private void SaveVideo_Step3(DownloadList saveItems, string url, bool? enque)
        {
            // check for valid url and cut off additional parameter
            if (String.IsNullOrEmpty(url) ||
                !Utils.IsValidUri((url.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? url.Substring(0, url.IndexOf(MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : url))
            {
                GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                if (dlg != null)
                {
                    dlg.Reset();
                    dlg.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                    dlg.SetHeading(Translation.Instance.Error);
                    dlg.SetText(Translation.Instance.UnableToDownloadVideo);
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                }
                return;
            }

            saveItems.CurrentItem.Url = url;
            if (string.IsNullOrEmpty(saveItems.CurrentItem.Title)) saveItems.CurrentItem.Title = saveItems.CurrentItem.VideoInfo.Title;

            if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFolder))
            {
                if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFileName))
                    saveItems.CurrentItem.LocalFile = Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.OverrideFileName);
                else
                    saveItems.CurrentItem.LocalFile = Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
            }
            else
            {
                saveItems.CurrentItem.LocalFile = Path.Combine(Path.Combine(OnlineVideoSettings.Instance.DownloadDir, saveItems.CurrentItem.Util.Settings.Name), saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
            }

            if (saveItems.DownloadItems != null && saveItems.DownloadItems.Count > 1)
            {
                saveItems.CurrentItem.LocalFile = string.Format(@"{0}\{1} - {2}#{3}{4}",
                    Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
                    Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile),
                    (saveItems.DownloadItems.IndexOf(saveItems.CurrentItem) + 1).ToString().PadLeft((saveItems.DownloadItems.Count).ToString().Length, '0'),
                    (saveItems.DownloadItems.Count).ToString(),
                    Path.GetExtension(saveItems.CurrentItem.LocalFile));
            }

            saveItems.CurrentItem.LocalFile = Utils.GetNextFileName(saveItems.CurrentItem.LocalFile);
            saveItems.CurrentItem.ThumbFile = string.IsNullOrEmpty(saveItems.CurrentItem.VideoInfo.ThumbnailImage) ? saveItems.CurrentItem.VideoInfo.ImageUrl : saveItems.CurrentItem.VideoInfo.ThumbnailImage;

            // make sure the target dir exists
            if (!(Directory.Exists(Path.GetDirectoryName(saveItems.CurrentItem.LocalFile))))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveItems.CurrentItem.LocalFile));
            }

            if (enque == true)
                DownloadManager.Instance.Add(saveItems.CurrentItem.Util.Settings.Name, saveItems);
            else if (enque == false)
                DownloadManager.Instance.Add(null, saveItems);

            GUIPropertyManager.SetProperty("#OnlineVideos.currentDownloads", DownloadManager.Instance.Count.ToString());

            System.Threading.Thread downloadThread = new System.Threading.Thread((System.Threading.ParameterizedThreadStart)delegate(object o)
            {
                DownloadList dlList = o as DownloadList;
                try
                {
                    IDownloader dlHelper = null;
                    if (dlList.CurrentItem.Url.ToLower().StartsWith("mms://")) dlHelper = new MMSDownloader();
                    else dlHelper = new MPUrlSourceFilter.MPUrlSourceFilterDownloader();
                    dlList.CurrentItem.Downloader = dlHelper;
                    dlList.CurrentItem.Start = DateTime.Now;
                    Log.Instance.Info("Starting download of '{0}' to '{1}' from Site '{2}'", dlList.CurrentItem.Url, dlList.CurrentItem.LocalFile, dlList.CurrentItem.Util.Settings.Name);
                    Exception exception = dlHelper.Download(dlList.CurrentItem);
                    if (exception != null) Log.Instance.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, exception.Message);
                    OnDownloadFileCompleted(dlList, exception);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // the thread was aborted on purpose, let it finish gracefully
                    System.Threading.Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, ex.Message);
                    OnDownloadFileCompleted(dlList, ex);
                }
            });
            downloadThread.IsBackground = true;
            downloadThread.Name = "OVDownload";
            downloadThread.Start(saveItems);

            GUIDialogNotify dlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
            if (dlgNotify != null)
            {
                dlgNotify.Reset();
                dlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                dlgNotify.SetHeading(Translation.Instance.DownloadStarted);
                dlgNotify.SetText(saveItems.CurrentItem.Title);
                dlgNotify.DoModal(GUIWindowManager.ActiveWindow);
            }
        }

        private void SaveSubtitles(VideoInfo video, string destinationFileName)
        {
            Uri subtitleUri = null;
            bool validUri = !String.IsNullOrEmpty(video.SubtitleUrl) && Uri.TryCreate(video.SubtitleUrl, UriKind.Absolute, out subtitleUri);

            if (!string.IsNullOrEmpty(video.SubtitleText) || (validUri && !subtitleUri.IsFile))
            {
                Log.Instance.Info("Downloading subtitles to " + destinationFileName);
                string subs = string.IsNullOrEmpty(video.SubtitleText) ? Sites.SiteUtilBase.GetWebData(video.SubtitleUrl) : video.SubtitleText;
                if (!string.IsNullOrEmpty(subs))
                    File.WriteAllText(destinationFileName, subs, System.Text.Encoding.UTF8);
            }
            else
                if (validUri && subtitleUri.IsFile)
                {
                    Log.Instance.Info("Downloading subtitles to " + destinationFileName);
                    File.Copy(subtitleUri.AbsolutePath, destinationFileName);
                }
        }

        private void OnDownloadFileCompleted(DownloadList saveItems, Exception error)
        {
            // notify the Util of the downloaded video that the download has stopped
            try
            {
                if (saveItems.CurrentItem != null && saveItems.CurrentItem.Util != null)
                {
                    saveItems.CurrentItem.Util.OnDownloadEnded(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Url, (double)saveItems.CurrentItem.PercentComplete / 100.0d, error != null);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn("Error on Util.OnDownloadEnded: {0}", ex.ToString());
            }

            bool preventMessageDuetoAdult = (saveItems.CurrentItem.Util != null && saveItems.CurrentItem.Util.Settings.ConfirmAge && OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed);

            if (error != null && !saveItems.CurrentItem.Downloader.Cancelled)
            {
                if (!preventMessageDuetoAdult)
                {
                    GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (loDlgNotify != null)
                    {
                        loDlgNotify.Reset();
                        loDlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        loDlgNotify.SetHeading(Translation.Instance.Error);
                        loDlgNotify.SetText(string.Format(Translation.Instance.DownloadFailed, saveItems.CurrentItem.Title));
                        loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }
            }
            else
            {
                try
                {
                    // if the image given was an url -> check if thumb exists otherwise download
                    if (saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http"))
                    {
                        string thumbFile = Utils.GetThumbFile(saveItems.CurrentItem.ThumbFile);
                        if (File.Exists(thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
                        else if (ImageDownloader.DownloadAndCheckImage(saveItems.CurrentItem.ThumbFile, thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
                    }
                    // save thumb for this video as well if it exists
                    if (!saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http") && File.Exists(saveItems.CurrentItem.ThumbFile))
                    {
                        string localImageName = Path.Combine(
                            Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
                            Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile))
                            + Path.GetExtension(saveItems.CurrentItem.ThumbFile);
                        File.Copy(saveItems.CurrentItem.ThumbFile, localImageName, true);
                    }
					// save subtitles if SubtitlesUrl was set
					SaveSubtitles(saveItems.CurrentItem.VideoInfo, Path.ChangeExtension(saveItems.CurrentItem.LocalFile, ".srt"));
					// save matroska tag
					File.WriteAllText(Path.ChangeExtension(saveItems.CurrentItem.LocalFile, ".xml"), saveItems.CurrentItem.VideoInfo.CreateMatroskaXmlTag(), System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error saving additional files for download: {0}", ex.ToString());
                }

                // get file size
                int fileSize = saveItems.CurrentItem.KbTotal;
                if (fileSize <= 0)
                {
                    try { fileSize = (int)((new FileInfo(saveItems.CurrentItem.LocalFile)).Length / 1024); }
                    catch { }
                }

                Log.Instance.Info("{3} download of '{0}' - {1} KB in {2}", saveItems.CurrentItem.LocalFile, fileSize, (DateTime.Now - saveItems.CurrentItem.Start).ToString(), saveItems.CurrentItem.Downloader.Cancelled ? "Cancelled" : "Finished");

                if (!preventMessageDuetoAdult)
                {
                    GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    if (loDlgNotify != null)
                    {
                        loDlgNotify.Reset();
                        loDlgNotify.SetImage(SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon"));
                        if (saveItems.CurrentItem.Downloader.Cancelled)
                            loDlgNotify.SetHeading(Translation.Instance.DownloadCancelled);
                        else
                            loDlgNotify.SetHeading(Translation.Instance.DownloadComplete);
                        loDlgNotify.SetText(string.Format("{0}{1}", saveItems.CurrentItem.Title, fileSize > 0 ? " ( " + fileSize.ToString("n0") + " KB)" : ""));
                        loDlgNotify.DoModal(GUIWindowManager.ActiveWindow);
                    }
                }

                // invoke VideoDownloaded event
                if (VideoDownloaded != null)
                {
                    try
                    {
                        VideoDownloaded(saveItems.CurrentItem.LocalFile, saveItems.CurrentItem.Util.Settings.Name, saveItems.CurrentItem.Category != null ? saveItems.CurrentItem.Category.RecursiveName() : "", saveItems.CurrentItem.Title);
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Warn("Error invoking external VideoDownloaded event handler: {0}", ex.ToString());
                    }
                }
            }

            // download the next if list not empty and not last in list and not cancelled by the user
            string site = null;
            if (saveItems.DownloadItems != null && saveItems.DownloadItems.Count > 1 && !saveItems.CurrentItem.Downloader.Cancelled)
            {
                int currentDlIndex = saveItems.DownloadItems.IndexOf(saveItems.CurrentItem);
                if (currentDlIndex >= 0 && currentDlIndex + 1 < saveItems.DownloadItems.Count)
                {
                    saveItems.CurrentItem = saveItems.DownloadItems[currentDlIndex + 1];
                    GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { SaveVideo_Step1(saveItems, null); return 0; }, 0, 0, null);
                }
                else
                {
                    site = DownloadManager.Instance.Remove(saveItems);
                }
            }
            else
            {
                site = DownloadManager.Instance.Remove(saveItems);
            }

            if (!string.IsNullOrEmpty(site))
            {
                var continuationList = DownloadManager.Instance.GetNext(site);
                if (continuationList != null)
                {
                    GUIWindowManager.SendThreadCallbackAndWait((p1, p2, data) => { SaveVideo_Step1(continuationList, null); return 0; }, 0, 0, null);
                }
            }

            GUIPropertyManager.SetProperty("#OnlineVideos.currentDownloads", DownloadManager.Instance.Count.ToString());
        }

        void FilterCurrentFacadeItems()
        {
            currentFilter.StartMatching();
            GUIControl.ClearControl(GetID, GUI_facadeView.GetID);
            foreach (var item in currentFacadeItems)
            {
                if (currentFilter.Matches(item.Label))
                {
                    GUI_facadeView.Add(item);
                }
            }
            GUIPropertyManager.SetProperty("#OnlineVideos.filter", currentFilter.ToString());
            GUIPropertyManager.SetProperty("#itemcount", GUI_facadeView.Count.ToString());
        }

        private bool FilterOut(string fsStr)
        {
            if (!string.IsNullOrEmpty(fsStr) && PluginConfiguration.Instance.FilterArray != null)
            {
                foreach (string lsFilter in PluginConfiguration.Instance.FilterArray)
                {
                    if (fsStr.IndexOf(lsFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Instance.Debug("Filtering out '{0}' based on filter '{1}'", fsStr, lsFilter);
                        return true;
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
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", PluginConfiguration.Instance.BasicHomeScreenName);
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImage", SiteImageExistenceCache.GetImageForSite("OnlineVideos"));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    HideSearchButtons();
                    if (OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed)
                        ShowAndEnable(GUI_btnEnterPin.GetID);
                    else
                        HideAndDisable(GUI_btnEnterPin.GetID);
                    currentView = PluginConfiguration.Instance.currentGroupView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Groups);
                    break;
                case State.sites:
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", PluginConfiguration.Instance.BasicHomeScreenName + (selectedSitesGroup != null ? ": " + selectedSitesGroup.Label : ""));
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImage", SiteImageExistenceCache.GetImageForSite("OnlineVideos"));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    ShowOrderButtons();
                    HideSearchButtons();
                    if (OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed)
                        ShowAndEnable(GUI_btnEnterPin.GetID);
                    else
                        HideAndDisable(GUI_btnEnterPin.GetID);
                    currentView = PluginConfiguration.Instance.currentSiteView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Sites);
                    break;
                case State.categories:
                    string cat_headerlabel = selectedCategory != null ? selectedCategory.RecursiveName() : SelectedSite.Settings.Name;
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", cat_headerlabel);
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImage", SiteImageExistenceCache.GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    ShowAndEnable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    currentView = suggestedView != null ? suggestedView.Value : PluginConfiguration.Instance.currentCategoryView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Categories);
                    break;
                case State.videos:
                    switch (currentVideosDisplayMode)
                    {
                        case VideosMode.Search: GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", Translation.Instance.SearchResults + " [" + lastSearchQuery + "]"); break;
                        default:
                            {
                                string proposedLabel = SelectedSite.getCurrentVideosTitle();
                                GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", proposedLabel != null ? proposedLabel : selectedCategory != null ? selectedCategory.RecursiveName() : ""); break;
                            }
                    }
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImage", SiteImageExistenceCache.GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    ShowAndEnable(GUI_facadeView.GetID);
                    if (SelectedSite is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    if (SelectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    if (SelectedSite.HasFilterCategories) ShowCategoryButton();
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    currentView = suggestedView != null ? suggestedView.Value : PluginConfiguration.Instance.currentVideoView;
                    SetFacadeViewMode();
                    GUIPropertyManager.SetProperty("#itemtype", Translation.Instance.Videos);
                    break;
                case State.details:
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderLabel", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#OnlineVideos.HeaderImagee", SiteImageExistenceCache.GetImageForSite(SelectedSite.Settings.Name, SelectedSite.Settings.UtilName));
                    HideAndDisable(GUI_facadeView.GetID);
                    HideFilterButtons();
                    HideSearchButtons();
                    HideAndDisable(GUI_btnEnterPin.GetID);
                    GUIPropertyManager.SetProperty("#itemcount", (GUI_infoList.Count - 1).ToString());
                    break;
            }
            GUIWindowManager.Process(); // required for the next statement to work correctly, so the skinengine has correct state for visibility and focus
            if (CurrentState == State.details) GUIControl.FocusControl(GetID, GUI_infoList.GetID);
            else GUIControl.FocusControl(GetID, GUI_facadeView.GetID);
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
            GUI_btnSearchCategories.Clear();
            HideAndDisable(GUI_btnSearchCategories.GetID);
            HideAndDisable(GUI_btnSearch.GetID);
        }

        private void ShowSearchButtons()
        {
            GUI_btnSearchCategories.Clear();
            moSupportedSearchCategoryList = SelectedSite.GetSearchableCategories();
            GUIControl.AddItemLabelControl(GetID, GUI_btnSearchCategories.GetID, Translation.Instance.All);
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
                case GUIFacadeControl.Layout.List:
                    currentView = GUIFacadeControl.Layout.SmallIcons; break;
                case GUIFacadeControl.Layout.SmallIcons:
                    currentView = GUIFacadeControl.Layout.LargeIcons; break;
                case GUIFacadeControl.Layout.LargeIcons:
                    currentView = GUIFacadeControl.Layout.List; break;
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
                case GUIFacadeControl.Layout.List:
                    strLine = Translation.Instance.LayoutList;
                    break;
                case GUIFacadeControl.Layout.SmallIcons:
                    strLine = Translation.Instance.LayoutIcons;
                    break;
                case GUIFacadeControl.Layout.LargeIcons:
                    strLine = Translation.Instance.LayoutBigIcons;
                    break;
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
            GUI_facadeView.CurrentLayout = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
            if (rememberIndex > -1) GUIControl.SelectItemControl(GetID, GUI_facadeView.GetID, rememberIndex);
        }

        /// <summary>
        /// Displays a modal dialog, with a list of the PlaybackOptions to the user, 
        /// only if PlaybackOptions holds more than one entry.
        /// </summary>
        /// <param name="videoInfo"></param>
        /// <param name="defaultUrl">will be set to -1 when the user canceled the dialog, will not be touched if no playbackoptions are set, otherwise set to the chosen key</param>
        /// <param name="skipDialog">when set to true, the dialog will not display, the default choice is returned</param>
        /// <returns>true when a choice from the PlaybackOptions was made (or only one option was available)</returns>
        private bool DisplayPlaybackOptions(VideoInfo videoInfo, ref string defaultUrl, bool skipDialog)
        {
            // with no options set, return the VideoUrl field
            if (videoInfo.PlaybackOptions == null || videoInfo.PlaybackOptions.Count == 0) return false;
            // with just one option set, return that options url
            if (videoInfo.PlaybackOptions.Count == 1)
            {
                defaultUrl = videoInfo.PlaybackOptions.First().Key;
            }
            else
            {
                if (skipDialog)
                {
                    string defaultUrlUnRef = defaultUrl;
                    var defaultOption = videoInfo.PlaybackOptions.FirstOrDefault(p => p.Value == defaultUrlUnRef).Key;
                    if (!string.IsNullOrEmpty(defaultOption)) defaultUrl = defaultOption;
                    else defaultUrl = videoInfo.PlaybackOptions.First().Key;
                }
                else
                {
                    int defaultOption = -1;
                    // show a list of available options and let the user decide
                    GUIDialogMenu dlgSel = (GUIDialogMenu)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                    if (dlgSel != null)
                    {
                        dlgSel.Reset();
                        dlgSel.SetHeading(string.Format("{0} - {1}", videoInfo.Title, Translation.Instance.SelectSource));
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
                }
            }
            return true;
        }

        internal void SetGuiProperties_PlayingVideo(PlayListItem playItem)
        {
            // first reset our own properties
            GUIPropertyManager.SetProperty("#Play.Current.OnlineVideos.SiteIcon", string.Empty);
            GUIPropertyManager.SetProperty("#Play.Current.OnlineVideos.SiteName", string.Empty);

            // start a thread that will set the properties in 2 seconds (otherwise MediaPortal core logic would overwrite them)
            if (playItem == null || playItem.Video == null) return;
            new System.Threading.Thread(delegate(object o)
            {
                try
                {
                    VideoInfo video = (o as PlayListItem).Video;
                    string alternativeTitle = (o as PlayListItem).Description;
                    Sites.SiteUtilBase site = (o as PlayListItem).Util;

                    System.Threading.Thread.Sleep(2000);

                    string quality = video.PlaybackOptions != null ? video.PlaybackOptions.FirstOrDefault(po => po.Value == (g_Player.Player as OVSPLayer).PlaybackUrl).Key : null;

                    string titleToShow = "";
                    if (!string.IsNullOrEmpty(alternativeTitle))
                        titleToShow = alternativeTitle;
                    else if (!string.IsNullOrEmpty(video.Title))
                        titleToShow = video.Title + (string.IsNullOrEmpty(quality) ? "" : " (" + quality + ")");

                    Log.Instance.Info("Setting Video Properties for '{0}'", titleToShow);

                    if (!string.IsNullOrEmpty(titleToShow)) GUIPropertyManager.SetProperty("#Play.Current.Title", titleToShow);
                    if (!string.IsNullOrEmpty(video.Description)) GUIPropertyManager.SetProperty("#Play.Current.Plot", video.Description);
                    if (!string.IsNullOrEmpty(video.ThumbnailImage)) GUIPropertyManager.SetProperty("#Play.Current.Thumb", video.ThumbnailImage);
                    if (!string.IsNullOrEmpty(video.Airdate)) GUIPropertyManager.SetProperty("#Play.Current.Year", video.Airdate);
                    else if (!string.IsNullOrEmpty(video.Length)) GUIPropertyManager.SetProperty("#Play.Current.Year", VideoInfo.GetDuration(video.Length));

                    if (site != null)
                    {
                        GUIPropertyManager.SetProperty("#Play.Current.OnlineVideos.SiteIcon", SiteImageExistenceCache.GetImageForSite(site.Settings.Name, site.Settings.UtilName, "Icon"));
                        GUIPropertyManager.SetProperty("#Play.Current.OnlineVideos.SiteName", site.Settings.Name);
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn("Error setting playing video properties: {0}", ex.ToString());
                }
            }) { IsBackground = true, Name = "OVPlaying" }.Start(playItem);

            TrackPlayback();
        }

        /// <summary>
        /// Processes extended properties which might be available
        /// if the VideoInfo.Other object is using the IVideoDetails interface
        /// </summary>
        /// <param name="videoInfo">if this param is null, the <see cref="selectedVideo"/> will be used</param>
        private void SetGuiProperties_ExtendedVideoInfo(VideoInfo videoInfo, bool DetailsItem)
        {
            string prefix = "#OnlineVideos.";
            if (!DetailsItem)
            {
                ResetExtendedGuiProperties(prefix); // remove everything
                if (videoInfo == null) videoInfo = selectedVideo; // set everything for the selected video in the next step if given video is null
                prefix = prefix + "Details.";
            }
            else
            {
                prefix = prefix + "DetailsItem.";
                ResetExtendedGuiProperties(prefix); // remove all entries for the last selected "DetailsItem" (will be set for the parameter in the next step)
            }

            if (videoInfo != null)
            {
                Dictionary<string, string> custom = videoInfo.GetExtendedProperties();
                if (custom != null)
                {
                    foreach (string property in custom.Keys)
                    {
                        string label = prefix + property;
                        string value = custom[property];
                        SetExtendedGuiProperty(label, value);
                    }
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
            lock (extendedProperties)
            {
                extendedProperties.Add(key);
                GUIPropertyManager.SetProperty(key, value);
            }
        }

        /// <summary>
        /// Clears all known set extended property values
        /// </summary>
        /// <param name="prefix">prefix</param>
        public void ResetExtendedGuiProperties(string prefix)
        {
            lock (extendedProperties)
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
        }

        private void ResetSelectedSite()
        {
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSite", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.selectedSiteUtil", string.Empty);
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", string.Empty);
        }

        public void ResetToFirstView()
        {
            selectedSitesGroup = null;
            SelectedSite = null;
            selectedCategory = null;
            selectedVideo = null;
            currentVideoList = new List<VideoInfo>();
            currentTrailerList = new List<VideoInfo>();
            currentNavigationContextSwitch = null;
            currentPlaylist = null;
            currentPlayingItem = null;
            CurrentState = State.groups;
        }

        #endregion
    }
}