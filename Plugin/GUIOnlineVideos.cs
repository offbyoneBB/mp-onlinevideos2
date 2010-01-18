using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
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

        public const string PLUGIN_NAME = "Online Videos";

        BackgroundWorker worker;

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
            return PLUGIN_NAME;
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
        protected GUIButtonControl btnViewAs = null;
        [SkinControlAttribute(3)]
        protected GUIButtonControl btnNext = null;
        [SkinControlAttribute(4)]
        protected GUIButtonControl btnPrevious = null;
        [SkinControlAttribute(5)]
        protected GUISelectButtonControl btnMaxResult = null;
        [SkinControlAttribute(6)]
        protected GUISelectButtonControl btnOrderBy = null;
        [SkinControlAttribute(7)]
        protected GUISelectButtonControl btnTimeFrame = null;
        [SkinControlAttribute(8)]
        protected GUIButtonControl btnUpdate = null;        
        [SkinControlAttribute(9)]
        protected GUISelectButtonControl btnSearchCategories = null;
        [SkinControlAttribute(10)]
        protected GUIButtonControl btnSearch = null;
        [SkinControlAttribute(11)]
        protected GUIButtonControl btnFavorite = null;
        [SkinControlAttribute(12)]
        protected GUIButtonControl btnEnterPin = null;
        [SkinControlAttribute(50)]
        protected GUIFacadeControl facadeView = null;
        [SkinControlAttribute(51)]
        protected GUIListControl infoList = null;
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
        
        List<VideoInfo> moCurrentVideoList = new List<VideoInfo>();
        List<VideoInfo> moCurrentTrailerList = new List<VideoInfo>();

        RTMP_LIB.HTTPServer rtmpServer;
        OnlineVideos.Sites.AppleProxyServer appleProxyServer;

        internal static Dictionary<string, DownloadInfo> currentDownloads = new Dictionary<string,DownloadInfo>();
        #endregion

        #region filter variables
        private List<int> moSupportedMaxResultList;
        private Dictionary<String, String> moSupportedOrderByList;
        private Dictionary<String, String> moSupportedTimeFrameList;
        private Dictionary<String, String> moSupportedSearchCategoryList;

        //selected values
        private int miMaxResult;
        private String msOrderBy = String.Empty;
        private String msTimeFrame = String.Empty;
        
        //selected indices
        private int SelectedMaxResultIndex;
        private int SelectedOrderByIndex;
        private int SelectedTimeFrameIndex;
        private int SelectedSearchCategoryIndex;
        #endregion        

        #region search variables
        private String msLastSearchQuery;
        private String msLastSearchCategory;        
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
            if (rtmpServer == null) rtmpServer = new RTMP_LIB.HTTPServer(OnlineVideoSettings.RTMP_PROXY_PORT);
            if (appleProxyServer == null) appleProxyServer = new OnlineVideos.Sites.AppleProxyServer(OnlineVideoSettings.APPLE_PROXY_PORT);
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
                SwitchView();
            }
            else if (CurrentState == State.categories)
            {
                DisplayCategories(null);
                SwitchView();
            }
            else if (CurrentState == State.videos)
            {
                DisplayCategoryVideos();
                SwitchView();
            }
            else
            {
                DisplayVideoDetails(selectedVideo);
                if (selectedClipIndex < infoList.Count) infoList.SelectedListItemIndex = selectedClipIndex;
            }
            UpdateViewState();
        }

        protected override void OnShowContextMenu()
        {
            int liSelected = facadeView.SelectedListItemIndex - 1;

            if (selectedSite.HasMultipleVideos && CurrentState == State.details) liSelected = infoList.SelectedListItemIndex - 1;

            if (liSelected < 0 || CurrentState == State.sites || CurrentState == State.categories || (selectedSite.HasMultipleVideos && CurrentState == State.videos))
            {
                return;
            }
            GUIListItem loListItem = facadeView.SelectedListItem;
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
                loSelectedVideo = moCurrentVideoList[liSelected];
            }
            else
            {
                loSelectedVideo = moCurrentTrailerList[liSelected];
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
                        refreshVideoList();
                    }
                    else
                    {
                        AddOrRemoveFavorite(loSelectedVideo);
                    }
                    break;
                case 3:
                    if (selectedSite.HasRelatedVideos)
                    {
                        List<VideoInfo> relatedVideos = new List<VideoInfo>();
                        GUIWaitCursor.Init(); GUIWaitCursor.Show();                        
                        worker = new BackgroundWorker();
                        worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                        {
                            relatedVideos = selectedSite.getRelatedVideos(loSelectedVideo);
                        });
                        worker.RunWorkerAsync();
                        while (worker.IsBusy) GUIWindowManager.Process();

                        GUIWaitCursor.Hide();

                        moCurrentVideoList = relatedVideos;
                        currentVideosDisplayMode = VideosMode.Related;
                        DisplayCategoryVideos();
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

                OnShowPreviousMenu();
                return;
            }
            else if (action.wID == Action.ActionType.ACTION_KEY_PRESSED && facadeView.Visible && facadeView.Focus)
            {
                // search items (starting from current selected) by title and select first found one
                char pressedChar = (char)action.m_key.KeyChar;
                if (char.IsLetterOrDigit(pressedChar))
                {
                    string lowerChar = pressedChar.ToString().ToLower();
                    for (int i = facadeView.SelectedListItemIndex+1; i < facadeView.Count; i++)
                    {
                        if (facadeView[i].Label.ToLower().StartsWith(lowerChar))
                        {
                            facadeView.SelectedListItemIndex = i;
                            return;
                        }
                    }
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
                        btnSearchCategories.RestoreSelection = false;
                        btnOrderBy.RestoreSelection = false;
                        btnTimeFrame.RestoreSelection = false;
                        btnMaxResult.RestoreSelection = false;
                        return true;
                    }
            }            
            return base.OnMessage(message);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            if (worker != null && worker.IsBusy) return; // wait for any background action e.g. dynamic category discovery to finish

            if (control == facadeView && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                if (CurrentState == State.sites)
                {
                    selectedSite = OnlineVideoSettings.getInstance().SiteList[facadeView.SelectedListItem.Path];
                    selectedSiteIndex = facadeView.SelectedListItemIndex;                    
                    DisplayCategories(null);
                    CurrentState = State.categories;
                }
                else if (CurrentState == State.categories)
                {
                    if (facadeView.SelectedListItemIndex == 0)
                    {
                        OnShowPreviousMenu();
                    }
                    else
                    {
                        if (selectedCategory == null) selectedCategory = selectedSite.Settings.Categories[facadeView.SelectedListItemIndex-1];
                        else selectedCategory = selectedCategory.SubCategories[facadeView.SelectedListItemIndex - 1];

                        if (selectedCategory.HasSubCategories)
                        {                            
                            DisplayCategories(selectedCategory);
                        }
                        else
                        {
                            selectedVideo = null;
                            selectedVideoIndex = 0;
                            CurrentState = State.videos;
                            HideFacade();
                            refreshVideoList();
                            // if there were no videos found select the parent category
                            //if (currentState != State.videos) selectedCategory = selectedCategory.ParentCategory;
                        }
                    }
                }
                else if (CurrentState == State.videos)
                {
                    Log.Info("Set the stopDownload to true 2");
                    ImageDownloader._stopDownload = true;
                    if (facadeView.SelectedListItemIndex == 0)
                    {
                        OnShowPreviousMenu();
                    }
                    else
                    {
                        selectedVideoIndex = facadeView.SelectedListItemIndex;
                        if (selectedSite.HasMultipleVideos)
                        {
                            CurrentState = State.details;
                            DisplayVideoDetails(moCurrentVideoList[facadeView.SelectedListItemIndex - 1]);
                        }
                        else
                        {
                            //play the video
                            Play(moCurrentVideoList[facadeView.SelectedListItemIndex - 1]);
                        }
                    }
                }                
                UpdateViewState();
            }
            else if (control == infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM && CurrentState == State.details)
            {
                ImageDownloader._stopDownload = true;
                if (infoList.SelectedListItemIndex == 0)
                {
                    OnShowPreviousMenu();
                }
                else
                {
                    selectedClipIndex = infoList.SelectedListItemIndex;
                    //play the video
                    Play(moCurrentTrailerList[infoList.SelectedListItemIndex - 1]);
                }
                UpdateViewState();
            }
            else if (control == btnViewAs)
            {
                ChangeFacadeView();
            }

            else if (control == btnNext)
            {
                moCurrentVideoList = null;

                GUIWaitCursor.Init();
                GUIWaitCursor.Show();
                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                {
                    moCurrentVideoList = selectedSite.getNextPageVideos();
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                selectedVideoIndex = 0;
                DisplayCategoryVideos();
                UpdateViewState();

                if (selectedSite.HasNextPage) ShowNextPageButton();
                else HideNextPageButton();

                if (selectedSite.HasPreviousPage) ShowPreviousPageButton();
                else HidePreviousPageButton();

                GUIControl.UnfocusControl(GetID, btnNext.GetID);
            }
            else if (control == btnPrevious)
            {
                moCurrentVideoList = null;

                GUIWaitCursor.Init();
                GUIWaitCursor.Show();
                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                {
                    moCurrentVideoList = selectedSite.getPreviousPageVideos();
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                selectedVideoIndex = 0;
                DisplayCategoryVideos();
                UpdateViewState();

                if (selectedSite.HasNextPage) ShowNextPageButton();
                else HideNextPageButton();

                if (selectedSite.HasPreviousPage) ShowPreviousPageButton();
                else HidePreviousPageButton();

                GUIControl.UnfocusControl(GetID, btnPrevious.GetID);
            }
            else if (control == btnMaxResult)
            {
                GUIControl.SelectItemControl(GetID, btnMaxResult.GetID, btnMaxResult.SelectedItem);
            }
            else if (control == btnOrderBy)
            {
                GUIControl.SelectItemControl(GetID, btnOrderBy.GetID, btnOrderBy.SelectedItem);
                if (CurrentState == State.sites) siteOrder = (SiteOrder)btnOrderBy.SelectedItem;
            }
            else if (control == btnTimeFrame)
            {
                GUIControl.SelectItemControl(GetID, btnTimeFrame.GetID, btnTimeFrame.SelectedItem);
            }
            else if (control == btnUpdate)
            {
                GUIControl.UnfocusControl(GetID, btnUpdate.GetID);

                switch (CurrentState)
                {
                    case State.sites: DisplaySites(); break;
                    case State.videos: FilterVideos(); break;                    
                }
                UpdateViewState();
            }
            else if (control == btnSearchCategories)
            {
                GUIControl.SelectItemControl(GetID, btnSearchCategories.GetID, btnSearchCategories.SelectedItem);
            }
            else if (control == btnSearch)
            {
                String query = String.Empty;
                if (GetUserInputString(ref query, false))
                {
                    GUIControl.FocusControl(GetID, facadeView.GetID);
                    if (query != String.Empty)
                    {
                        msLastSearchQuery = query;
                        if (SearchVideos(query))
                        {
                            CurrentState = State.videos;
                            UpdateViewState();
                        }
                    }
                }
            }
            else if ((control == btnFavorite))
            {
                GUIControl.FocusControl(GetID, facadeView.GetID);
                if (DisplayFavoriteVideos())
                {
                    CurrentState = State.videos;
                    UpdateViewState();
                }
            }
            else if (control == btnEnterPin)
            {
                string pin = String.Empty;
                if (GetUserInputString(ref pin, true))
                {
                    if (pin == OnlineVideoSettings.getInstance().pinAgeConfirmation)
                    {
                        OnlineVideoSettings.getInstance().ageHasBeenConfirmed = true;
                        HideEnterPinButton();
                        DisplaySites();
                        GUIControl.FocusControl(GetID, facadeView.GetID);
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

        private void OnShowPreviousMenu()
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
                GUIControl.UnfocusControl(GetID, infoList.GetID);
                infoList.Focus = false;
                ///------------------------------------------------------------------------

                ImageDownloader._stopDownload = true;
                DisplayCategoryVideos();
                SwitchView();                
                CurrentState = State.videos;
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
        
        private void LoadSettings()
        {
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                currentSiteView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                siteOrder = (SiteOrder)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_ORDER, 0);
                currentVideoView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.VIDEOVIEW_MODE, (int)GUIFacadeControl.ViewMode.SmallIcons);
                currentCategoryView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.CATEGORYVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
            }
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            // build the list of configured sites with utils
            foreach (SiteSettings siteSettings in settings.SiteSettingsList)
            {
                // only need enabled sites
                if (siteSettings.IsEnabled)
                {
                    settings.SiteList.Add(siteSettings.Name, SiteUtilFactory.CreateFromShortName(siteSettings.UtilName, siteSettings));
                }
            }
            //create a favorites site
            SiteSettings SelectedSite = new SiteSettings();            
            SelectedSite.Name = "Favorites";
            SelectedSite.UtilName = "Favorite";
            SelectedSite.IsEnabled = true;
            RssLink cat = new RssLink();
            cat.Name = "dynamic";
            cat.Url = "favorites";
            SelectedSite.Categories.Add(cat);
            settings.SiteList.Add(SelectedSite.Name, SiteUtilFactory.CreateFromShortName(SelectedSite.UtilName, SelectedSite));

            if (!String.IsNullOrEmpty(settings.msDownloadDir))
            {
                try
                {
                    if (System.IO.Directory.Exists(settings.msDownloadDir) == false)
                    {
                        System.IO.Directory.CreateDirectory(settings.msDownloadDir);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failed to create download dir");
                    Log.Error(e);
                }
                //add a downloaded videos site
                SelectedSite = new SiteSettings();                
                SelectedSite.Name = "Downloaded Videos";
                SelectedSite.UtilName = "DownloadedVideo";
                SelectedSite.IsEnabled = true;
                cat = new RssLink();
                cat.Name = "All";
                cat.Url = settings.msDownloadDir;
                SelectedSite.Categories.Add(cat);
                Category currentDlsCat = new Category() { Name = "Downloading", Description = "Shows a list of downloads currently running." };
                SelectedSite.Categories.Add(currentDlsCat);
                settings.SiteList.Add(SelectedSite.Name, SiteUtilFactory.CreateFromShortName(SelectedSite.UtilName, SelectedSite));
            }
            try
            {
                if (System.IO.Directory.Exists(settings.msThumbLocation) == false)
                {
                    Log.Info("Thumb dir does not exist.");
                    System.IO.Directory.CreateDirectory(settings.msThumbLocation);
                    Log.Info("thumb dir created");
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to create thumb dir");
                Log.Error(e);
            }
        }

        private void DisplaySites()
        {
            selectedCategory = null;
            GUIControl.ClearControl(GetID, facadeView.GetID);

            // set order by options
            btnOrderBy.Clear();
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(886)); //Default
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(103)); //Name
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(304)); //Language
            btnOrderBy.SelectedItem = (int)siteOrder;
            
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
                    facadeView.Add(loListItem);
                }
            }
            SelectedMaxResultIndex = -1;
            SelectedOrderByIndex = -1;
            SelectedSearchCategoryIndex = -1;
            SelectedTimeFrameIndex = -1;
            
            if (selectedSiteIndex < facadeView.Count) facadeView.SelectedListItemIndex = selectedSiteIndex;

            GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(2143)/*Home*/);
            GUIPropertyManager.SetProperty("#header.image", "OnlineVideos/OnlineVideos.png");
        }

        private void DisplayCategories(Category parentCategory)
        {            
            GUIControl.ClearControl(GetID, facadeView.GetID);
            GUIListItem loListItem;
            loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            facadeView.Add(loListItem);
            
            int numCategoriesWithThumb = 0;
            List<String> imagesUrlList = new List<string>();

            suggestedView = null;
            IList<Category> categories = null;

            if (parentCategory == null)
            {
                if (!selectedSite.Settings.DynamicCategoriesDiscovered)
                {
                    try
                    {
                        GUIWaitCursor.Init();
                        GUIWaitCursor.Show();
                        worker = new BackgroundWorker();
                        worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
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
                            });
                        worker.RunWorkerAsync();
                        while (worker.IsBusy) GUIWindowManager.Process();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    finally
                    {
                        GUIWaitCursor.Hide();
                    }
                }

                categories = selectedSite.Settings.Categories;
            }
            else
            {
                if (!parentCategory.SubCategoriesDiscovered)
                {
                    try
                    {
                        GUIWaitCursor.Init();
                        GUIWaitCursor.Show();
                        worker = new BackgroundWorker();
                        worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                        {
                            Log.Info("Looking for sub categories for {0} in {1}", selectedSite.Settings.Name, parentCategory.Name);
                            try
                            {
                                int foundCategories = selectedSite.DiscoverSubCategories(parentCategory);
                                Log.Info("Found {0} sub categories.", foundCategories);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error looking for sub categories: " + ex.ToString());
                            }
                        });
                        worker.RunWorkerAsync();
                        while (worker.IsBusy) GUIWindowManager.Process();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    finally
                    {
                        GUIWaitCursor.Hide();
                    }
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

                    facadeView.Add(loListItem);

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

                if (numCategoriesWithThumb > 0) ImageDownloader.getImages(imagesUrlList, OnlineVideoSettings.getInstance().msThumbLocation, facadeView);
                if (numCategoriesWithThumb < categories.Count / 2) suggestedView = GUIFacadeControl.ViewMode.List;
            }

            selectedCategory = parentCategory;
            facadeView.SelectedListItemIndex = categoryIndexToSelect;
        }
        
        private void FilterVideos()
        {
            miMaxResult = -1;
            SelectedMaxResultIndex = btnMaxResult.SelectedItem;
            SelectedOrderByIndex = btnOrderBy.SelectedItem;
            SelectedTimeFrameIndex = btnTimeFrame.SelectedItem;
            try
            {
                miMaxResult = Int32.Parse(btnMaxResult.SelectedLabel);
            }
            catch (Exception) { }
            msOrderBy = String.Empty;
            try
            {
                msOrderBy = moSupportedOrderByList[btnOrderBy.SelectedLabel];
            }
            catch (Exception) { }
            msTimeFrame = String.Empty;
            try
            {
                msTimeFrame = moSupportedTimeFrameList[btnTimeFrame.SelectedLabel];
            }
            catch (Exception) { }

            GUIWaitCursor.Init();
            GUIWaitCursor.Show();

            List<VideoInfo> loListItems = new List<VideoInfo>();

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
            {                    
                if (currentVideosDisplayMode == VideosMode.Search)
                {
                    Log.Info("Filtering search result");
                    //filtering a search result
                    if (String.IsNullOrEmpty(msLastSearchCategory))
                    {
                        loListItems = ((IFilter)selectedSite).filterSearchResultList(msLastSearchQuery, miMaxResult, msOrderBy, msTimeFrame);
                    }
                    else
                    {
                        loListItems = ((IFilter)selectedSite).filterSearchResultList(msLastSearchQuery, msLastSearchCategory, miMaxResult, msOrderBy, msTimeFrame);
                    }
                }
                else
                {
                    if (selectedSite.HasFilterCategories) // just for setting the category
                        loListItems = selectedSite.Search(string.Empty, moSupportedSearchCategoryList[btnSearchCategories.SelectedLabel]);
                    if (selectedSite is IFilter)
                        loListItems = ((IFilter)selectedSite).filterVideoList(selectedCategory, miMaxResult, msOrderBy, msTimeFrame);
                }
            });
            worker.RunWorkerAsync();
            while (worker.IsBusy) GUIWindowManager.Process();

            GUIWaitCursor.Hide();

            UpdateVideoList(loListItems);
            moCurrentVideoList = loListItems;
        }

        private bool SearchVideos(String query)
        {
            List<VideoInfo> loListItems = null;
            SelectedSearchCategoryIndex = btnSearchCategories.SelectedItem;
            if (query != String.Empty)
            {
                String category = String.Empty;

                GUIWaitCursor.Init();
                GUIWaitCursor.Show();

                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                {
                    if (moSupportedSearchCategoryList.Count > 1 && !btnSearchCategories.SelectedLabel.Equals("All"))
                    {
                        category = moSupportedSearchCategoryList[btnSearchCategories.SelectedLabel];
                        Log.Info("Searching for {0} in category {1}", query, category);
                        msLastSearchCategory = category;
                        loListItems = selectedSite.Search(query, category);
                    }
                    else
                    {
                        Log.Info("Searching for {0} in all categories ", query);
                        loListItems = selectedSite.Search(query);
                    }
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                if (UpdateVideoList(loListItems))
                {
                    currentVideosDisplayMode = VideosMode.Search;
                    moCurrentVideoList = loListItems;
                    return true;
                }
            }
            return false;
        }

        private void refreshVideoList()
        {
            switch (currentVideosDisplayMode)
            {
                case VideosMode.Search: SearchVideos(msLastSearchQuery); break;
                case VideosMode.Favorites: DisplayFavoriteVideos(); break;
                case VideosMode.Category:

                    GUIWaitCursor.Init();
                    GUIWaitCursor.Show();

                    bool success = false;
                    worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                    {
                        List<VideoInfo> loListItems = selectedSite.getVideoList(selectedCategory);

                        if (loListItems != null && loListItems.Count > 0)
                        {
                            moCurrentVideoList = new List<VideoInfo>();
                            GUIControl.ClearControl(GetID, facadeView.GetID);

                            if (selectedSite.HasNextPage) ShowNextPageButton();
                            else HideNextPageButton();

                            if (selectedSite.HasPreviousPage) ShowPreviousPageButton();
                            else HidePreviousPageButton();

                            UpdateVideoList(loListItems);
                            moCurrentVideoList = loListItems;
                            success = true;
                        }
                    });
                    worker.RunWorkerAsync();
                    while (worker.IsBusy) GUIWindowManager.Process();

                    GUIWaitCursor.Hide();

                    if (!success)
                    {
                        GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                        dlg_error.SetHeading(PluginName());
                        dlg_error.SetLine(1, GUILocalizeStrings.Get(1036)/*No Videos found!*/);
                        dlg_error.SetLine(2, String.Empty);
                        dlg_error.DoModal(GUIWindowManager.ActiveWindow);

                        //if (selectedSite.UtilName == "Favorite")
                        //{
                        DisplayCategories(selectedCategory.ParentCategory);
                        //}
                        CurrentState = State.categories;
                        UpdateViewState();
                    }

                    break;
            }           
        }       

        private void DisplayCategoryVideos()
        {
            List<VideoInfo> loListItems;            
            loListItems = moCurrentVideoList;            
            UpdateVideoList(loListItems);
            moCurrentVideoList = loListItems;
            if (selectedVideoIndex < facadeView.Count) facadeView.SelectedListItemIndex = selectedVideoIndex; //Reposition the cursor on the selected video
        }

        private bool DisplayFavoriteVideos()
        {
            List<VideoInfo> loVideoList = null;

            GUIWaitCursor.Init();
            GUIWaitCursor.Show();

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
            {
                loVideoList = ((IFavorite)selectedSite).getFavorites();
            });
            worker.RunWorkerAsync();
            while (worker.IsBusy) GUIWindowManager.Process();

            GUIWaitCursor.Hide();

            if (UpdateVideoList(loVideoList))
            {
                moCurrentVideoList = loVideoList;
                currentVideosDisplayMode = VideosMode.Favorites;                
                return true;
            }
            return false;
        }
        
        private bool UpdateVideoList(List<VideoInfo> foVideos)
        {
            GUIListItem loListItem;
            GUIControl.ClearControl(GetID, facadeView.GetID);
            loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            facadeView.Add(loListItem);
            // Check for received data
            if (foVideos == null || foVideos.Count == 0)
            {
                GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlg_error.SetHeading(PluginName());
                dlg_error.SetLine(1, GUILocalizeStrings.Get(1036)/*No Videos found!*/); 
                dlg_error.SetLine(2, String.Empty);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                
                CurrentState = State.categories;
                DisplayCategories(selectedCategory == null ? null : selectedCategory.ParentCategory);
                UpdateViewState();
                return false;
            }
            List<String> loImageUrlList = new List<string>();
            int numVideosWithThumb = 0;
            int liIdx = 0;
            foreach (VideoInfo loVideoInfo in foVideos)
            {
                liIdx++;
                loVideoInfo.Description = cleanString(loVideoInfo.Description);
                if (FilterOut(loVideoInfo.Title) || FilterOut(loVideoInfo.Description))
                {
                    continue;
                }
                loListItem = new GUIListItem(loVideoInfo.Title);
                loListItem.Path = loVideoInfo.VideoUrl;
                loListItem.ItemId = liIdx;
                loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
                facadeView.Add(loListItem);
                loImageUrlList.Add(loVideoInfo.ImageUrl);
                if (!string.IsNullOrEmpty(loVideoInfo.ImageUrl))
                {
                    loListItem.RetrieveArt = false;
                    loListItem.OnRetrieveArt += new MediaPortal.GUI.Library.GUIListItem.RetrieveCoverArtHandler(OnRetrieveCoverArt);
                    numVideosWithThumb++;
                }
            }

            suggestedView = null;
            if (numVideosWithThumb > 0)
                ImageDownloader.getImages(loImageUrlList, OnlineVideoSettings.getInstance().msThumbLocation, facadeView);
            else
                suggestedView = GUIFacadeControl.ViewMode.List;

            return true;
        }

        private void DisplayVideoDetails(VideoInfo foVideo)
        {
            selectedVideo = foVideo;
            List<VideoInfo> loVideoList = null;

            bool success = false;
            GUIWaitCursor.Init();
            GUIWaitCursor.Show();
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
            {
                loVideoList = selectedSite.getOtherVideoList(foVideo);
                success = true;
            });
            worker.RunWorkerAsync();
            while (worker.IsBusy) GUIWindowManager.Process();
            GUIWaitCursor.Hide();
            if (!success)
            {
                GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlg_error.SetHeading(PluginName());
                dlg_error.SetLine(1, "Unable to load moviedetails");
                dlg_error.SetLine(2, String.Empty);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                CurrentState = State.videos;
                DisplayCategoryVideos();
                return;
            }

            moCurrentTrailerList.Clear();
            GUIControl.ClearControl(GetID, facadeView.GetID);
            GUIControl.ClearControl(GetID, infoList.GetID);
            GUIListItem loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            loListItem.ItemId = 0;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            infoList.Add(loListItem);
            int liIdx = 0;
            foreach (VideoInfo loVideoInfo in loVideoList)
            {
                liIdx++;
                loVideoInfo.Description = cleanString(loVideoInfo.Description);
                loListItem = new GUIListItem(loVideoInfo.Title2);
                loListItem.Path = loVideoInfo.VideoUrl;
                loListItem.ItemId = liIdx;
                infoList.Add(loListItem);
                moCurrentTrailerList.Add(loVideoInfo);
            }
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
            if (item.ItemId == 0)
            {
                DisplayVideoInfo(null);
            }
            else
            {
                DisplayVideoInfo(moCurrentVideoList[item.ItemId - 1]);
            }
            /*12/16/08
			if(currentView == View.FilmStrip){
				GUIFilmstripControl filmstrip = parent as GUIFilmstripControl;
				if (filmstrip == null)
					return;
				filmstrip.InfoImageFileName = item.ThumbnailImage;
                filmstrip.InfoImageFileName = item.Label;
			}
            */
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
            if (selectedSite.MultipleFilePlay)
            {
                //PlayListPlayer.SingletonPlayer.Init(); // re.registers Eventhandler among other for PlaybackStoppt, so will call play twice later!
                PlayListPlayer.SingletonPlayer.Reset();
                PlayListPlayer.SingletonPlayer.g_Player = new Player.PlaylistPlayerWrapper(selectedSite.Settings.Player);
                PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
                videoList.Clear();                
                List<String> loUrlList = selectedSite.getMultipleVideoUrls(foListItem);
                int i = 0;
                foreach (String url in loUrlList)
                {
                    i++;
                    PlayListItem item = new PlayListItem(string.Format("{0} - {1} / {2}", foListItem.Title, i.ToString(), loUrlList.Count), url);
                    videoList.Add(item);                   
                }

                PlayListPlayer.SingletonPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO_TEMP;
                if (PlayListPlayer.SingletonPlayer.Play(0))
                {
                    playing = true;
                }
                if (playing)
                {                    
                    GUIGraphicsContext.IsFullScreenVideo = true;
                    GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);
                }
            }
            else
            {
                string lsUrl = "";
                try
                {
                    GUIWaitCursor.Init();
                    GUIWaitCursor.Show();
                    worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                    {
                        try
                        {
                            lsUrl = selectedSite.getUrl(foListItem);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                        
                    });
                    worker.RunWorkerAsync();
                    while (worker.IsBusy) GUIWindowManager.Process();                
                }
                catch { }
                finally
                {
                    GUIWaitCursor.Hide();
                }

                if (String.IsNullOrEmpty(lsUrl) || !(Uri.IsWellFormedUriString(lsUrl, UriKind.Absolute) || System.IO.Path.IsPathRooted(lsUrl)) )
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
                bool playSuccess = g_Player.Play(lsUrl, g_Player.MediaType.Video);
                // restore the factory
                g_Player.Factory = savedFactory;

                if (playSuccess && g_Player.Player != null && g_Player.IsVideo)
                {
                    playing = true;

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
        }

        private void PlayAll()
        {            
            bool playing = false;
            //PlayListPlayer.SingletonPlayer.Init(); // re.registers Eventhandler among other for PlaybackStoppt, so will call play twice later!
            PlayListPlayer.SingletonPlayer.Reset();
            PlayListPlayer.SingletonPlayer.g_Player = new Player.PlaylistPlayerWrapper(selectedSite.Settings.Player);
            PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
            videoList.Clear();
            List<VideoInfo> loVideoList;
            if (selectedSite.HasMultipleVideos)
            {
                loVideoList = moCurrentTrailerList;
            }
            else
            {
                loVideoList = moCurrentVideoList;
            }
            bool firstAdded = false;
            foreach (VideoInfo loVideo in loVideoList)
            {
                string lsUrl = selectedSite.getUrl(loVideo);
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
            string url = selectedSite.getUrl(video);

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
                    if (exception == null) DownloadFileCompleted(this, new AsyncCompletedEventArgs(null, false, o as DownloadInfo));
                    else
                    {
                        Log.Error("Error downloading {0}, Msg: ", url, exception.Message);
                        DownloadFileCompleted(this, new AsyncCompletedEventArgs(exception, true, o as DownloadInfo));
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
                loClient.DownloadFileCompleted += DownloadFileCompleted;
                loClient.DownloadFileAsync(new Uri(url), downloadInfo.LocalFile, downloadInfo);
            }
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
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

        private String cleanString(String fsStr)
        {
            if (fsStr != null)
            {
                // Strip \n's
                fsStr = Regex.Replace(fsStr, @"(\n|\r)", "", RegexOptions.Multiline);

                // Remove whitespace (double spaces)
                fsStr = Regex.Replace(fsStr, @"  +", "", RegexOptions.Multiline);

                // Remove &nbsp;
                fsStr = Regex.Replace(fsStr, @"&nbsp;", " ", RegexOptions.Multiline);


                // Replace <br/> with \n
                fsStr = Regex.Replace(fsStr, @"< *br */*>", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

                // Remove remaining HTML tags
                fsStr = Regex.Replace(fsStr, @"<[^>]*>", "", RegexOptions.Multiline);

                fsStr = fsStr.Trim();

                fsStr = System.Web.HttpUtility.HtmlDecode(fsStr);
            }
            return fsStr;
        }

        private void UpdateViewState()
        {
            Log.Info("Updating View State");
            switch (CurrentState)
            {
                case State.sites:
                    GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(2143)/*Home*/);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/OnlineVideos.png");
                    ShowFacade();
                    //ShowFacadeViewAsButton();
                    HideNextPageButton();
                    HidePreviousPageButton();
                    HideVideoDetails();                    
                    HideFilterButtons();
                    ShowOrderButtons();
                    HideSearchButtons();
                    HideFavoriteButtons();
                    if (OnlineVideoSettings.getInstance().useAgeConfirmation && !OnlineVideoSettings.getInstance().ageHasBeenConfirmed)
                        ShowEnterPinButton();
                    else
                        HideEnterPinButton();
                    DisplayVideoInfo(null);
                    currentView = currentSiteView;
                    SwitchView();
                    break;
                case State.categories:
                    string cat_headerlabel = selectedCategory != null ? selectedCategory.Name : selectedSite.Settings.Name;
                    GUIPropertyManager.SetProperty("#header.label", cat_headerlabel);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    ShowFacade();
                    //HideFacadeViewAsButton();
                    HideNextPageButton();
                    HidePreviousPageButton();
                    HideVideoDetails();
                    HideFilterButtons();                    
                    if (selectedSite.CanSearch) ShowSearchButtons(); else HideSearchButtons();
                    if (selectedSite is IFavorite) ShowFavoriteButtons(); else HideFavoriteButtons();
                    HideEnterPinButton();
                    DisplayVideoInfo(null);
                    currentView = suggestedView != null ? suggestedView.Value : currentCategoryView;
                    SwitchView();
                    break;
                case State.videos:
                    switch (currentVideosDisplayMode)
                    {
                        case VideosMode.Favorites: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(932)); break;
                        case VideosMode.Search: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(283)); break;
                        case VideosMode.Related: GUIPropertyManager.SetProperty("#header.label", GUILocalizeStrings.Get(33011)); break;
                        default: GUIPropertyManager.SetProperty("#header.label", selectedCategory != null ? selectedCategory.Name : ""); break;
                    }
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    ShowFacade();
                    //ShowFacadeViewAsButton();
                    if (selectedSite.HasNextPage) ShowNextPageButton(); else HideNextPageButton();
                    if (selectedSite.HasPreviousPage) ShowPreviousPageButton(); else HidePreviousPageButton();
                    HideVideoDetails();
                    if (selectedSite is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    HideSearchButtons();
                    if (selectedSite.HasFilterCategories) ShowCategoryButton();
                    HideFavoriteButtons();                    
                    HideEnterPinButton();
                    DisplayVideoInfo(selectedVideo);
                    currentView = suggestedView != null ? suggestedView.Value : currentVideoView;                    
                    SwitchView();
                    break;
                case State.details:
                    GUIPropertyManager.SetProperty("#header.label", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Settings.Name + ".png");
                    HideFacade();
                    //HideFacadeViewAsButton();
                    HideNextPageButton();
                    HidePreviousPageButton();
                    ShowVideoDetails();
                    HideFilterButtons();
                    HideSearchButtons();
                    HideFavoriteButtons();
                    HideEnterPinButton();                    
                    DisplayVideoInfo(null);
                    break;
            }
            if (CurrentState == State.details)
            {
                infoList.Focus = true;
                GUIControl.FocusControl(GetID, infoList.GetID);
            }
            else
            {
                // set the selected item to -1 and afterwards back, so the displayed title gets refreshed
                int temp = facadeView.SelectedListItemIndex;
                facadeView.SelectedListItemIndex = -1;
                facadeView.SelectedListItemIndex = temp;

                facadeView.Focus = true;
                GUIControl.FocusControl(GetID, facadeView.GetID);
            }            
        }

        private void HideVideoDetails()
        {
            GUIControl.HideControl(GetID, 23);
            GUIControl.DisableControl(GetID, 23);
            GUIControl.HideControl(GetID, 24);
            GUIControl.DisableControl(GetID, 24);
            GUIControl.HideControl(GetID, 51);
            GUIControl.DisableControl(GetID, 51);
            GUIControl.HideControl(GetID, 52);
            GUIControl.DisableControl(GetID, 52);
            GUIControl.HideControl(GetID, 53);
            GUIControl.DisableControl(GetID, 53);
            GUIControl.HideControl(GetID, 54);
            GUIControl.DisableControl(GetID, 54);
            GUIControl.HideControl(GetID, 55);
            GUIControl.DisableControl(GetID, 55);
            GUIControl.HideControl(GetID, 56);
            GUIControl.DisableControl(GetID, 56);
            GUIControl.HideControl(GetID, 57);
            GUIControl.DisableControl(GetID, 57);
            GUIControl.HideControl(GetID, 58);
            GUIControl.DisableControl(GetID, 58);
            GUIControl.HideControl(GetID, 59);
            GUIControl.DisableControl(GetID, 59);
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
            }
            GUIControl.ShowControl(GetID, 23);
            GUIControl.EnableControl(GetID, 23);            
            GUIControl.ShowControl(GetID, 24);
            GUIControl.EnableControl(GetID, 24);
            GUIControl.ShowControl(GetID, 51);
            GUIControl.EnableControl(GetID, 51);
            GUIControl.ShowControl(GetID, 52);
            GUIControl.EnableControl(GetID, 52);
            GUIControl.ShowControl(GetID, 53);
            GUIControl.EnableControl(GetID, 53);
            GUIControl.ShowControl(GetID, 54);
            GUIControl.EnableControl(GetID, 54);
            GUIControl.ShowControl(GetID, 55);
            GUIControl.EnableControl(GetID, 55);
            GUIControl.ShowControl(GetID, 56);
            GUIControl.EnableControl(GetID, 56);
            GUIControl.ShowControl(GetID, 57);
            GUIControl.EnableControl(GetID, 57);
            GUIControl.ShowControl(GetID, 58);
            GUIControl.EnableControl(GetID, 58);
            GUIControl.ShowControl(GetID, 59);
            GUIControl.EnableControl(GetID, 59);
        }        

        private void ShowOrderButtons()
        {
            GUIControl.ShowControl(GetID, btnOrderBy.GetID);
            GUIControl.EnableControl(GetID, btnOrderBy.GetID);
            GUIControl.ShowControl(GetID, btnUpdate.GetID);
            GUIControl.EnableControl(GetID, btnUpdate.GetID);
        }

        private void HideFilterButtons()
        {
            GUIControl.DisableControl(GetID, btnMaxResult.GetID);
            GUIControl.HideControl(GetID, btnMaxResult.GetID);
            GUIControl.DisableControl(GetID, btnTimeFrame.GetID);
            GUIControl.HideControl(GetID, btnTimeFrame.GetID);
            GUIControl.DisableControl(GetID, btnOrderBy.GetID);
            GUIControl.HideControl(GetID, btnOrderBy.GetID);
            GUIControl.DisableControl(GetID, btnUpdate.GetID);            
            GUIControl.HideControl(GetID, btnUpdate.GetID);
        }

        private void ShowFilterButtons()
        {
            Log.Debug("Showing Filter buttons");
            btnMaxResult.Clear();
            btnOrderBy.Clear();
            btnTimeFrame.Clear();

            moSupportedMaxResultList = ((IFilter)selectedSite).getResultSteps();
            foreach (int step in moSupportedMaxResultList)
            {
                GUIControl.AddItemLabelControl(GetID, btnMaxResult.GetID, step + "");
            }
            moSupportedOrderByList = ((IFilter)selectedSite).getOrderbyList();
            foreach (String orderBy in moSupportedOrderByList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, orderBy);
            }
            moSupportedTimeFrameList = ((IFilter)selectedSite).getTimeFrameList();
            foreach (String time in moSupportedTimeFrameList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, btnTimeFrame.GetID, time);
            }

            GUIControl.ShowControl(GetID, btnMaxResult.GetID);
            GUIControl.ShowControl(GetID, btnOrderBy.GetID);
            GUIControl.ShowControl(GetID, btnTimeFrame.GetID);
            GUIControl.ShowControl(GetID, btnUpdate.GetID);
            
            GUIControl.EnableControl(GetID, btnMaxResult.GetID);
            GUIControl.EnableControl(GetID, btnOrderBy.GetID);
            GUIControl.EnableControl(GetID, btnTimeFrame.GetID);
            GUIControl.EnableControl(GetID, btnUpdate.GetID);

            if (SelectedMaxResultIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, btnMaxResult.GetID, SelectedMaxResultIndex);
            }
            if (SelectedOrderByIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, btnOrderBy.GetID, SelectedOrderByIndex);
            }
            if (SelectedTimeFrameIndex > -1)
            {
                GUIControl.SelectItemControl(GetID, btnTimeFrame.GetID, SelectedTimeFrameIndex);
            }
        }

        private void HideSearchButtons()
        {
            Log.Debug("Hiding Search buttons");

            //disable the buttons to allow remote navigation
            GUIControl.DisableControl(GetID, btnSearchCategories.GetID);
            GUIControl.DisableControl(GetID, btnSearch.GetID);

            GUIControl.HideControl(GetID, btnSearchCategories.GetID);
            GUIControl.HideControl(GetID, btnSearch.GetID);
        }

        private void ShowSearchButtons()
        {
            Log.Debug("Showing Search buttons");
            btnSearchCategories.Clear();
            moSupportedSearchCategoryList = selectedSite.GetSearchableCategories();
            GUIControl.AddItemLabelControl(GetID, btnSearchCategories.GetID, "All");
            foreach (String category in moSupportedSearchCategoryList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, btnSearchCategories.GetID, category);
            }
            if (moSupportedSearchCategoryList.Count > 1)
            {
                GUIControl.ShowControl(GetID, btnSearchCategories.GetID);
                GUIControl.EnableControl(GetID, btnSearchCategories.GetID);
            }
            GUIControl.ShowControl(GetID, btnSearch.GetID);
            GUIControl.EnableControl(GetID, btnSearch.GetID);
            if (SelectedSearchCategoryIndex > -1)
            {
                Log.Info("restoring search category...");
                GUIControl.SelectItemControl(GetID, btnSearchCategories.GetID, SelectedSearchCategoryIndex);
                Log.Info("Search category restored to " + btnSearchCategories.SelectedLabel);
            }
        }

        private void ShowCategoryButton()
        {
            Log.Debug("Showing Category button");
            btnSearchCategories.Clear();
            moSupportedSearchCategoryList = selectedSite.GetSearchableCategories();
            foreach (String category in moSupportedSearchCategoryList.Keys)
                GUIControl.AddItemLabelControl(GetID, btnSearchCategories.GetID, category);
            if (moSupportedSearchCategoryList.Count > 1)
            {
                GUIControl.ShowControl(GetID, btnSearchCategories.GetID);
                GUIControl.EnableControl(GetID, btnSearchCategories.GetID);

                GUIControl.ShowControl(GetID, btnUpdate.GetID);
                GUIControl.EnableControl(GetID, btnUpdate.GetID);
            }
        }

        private void HideFavoriteButtons()
        {
            Log.Debug("Hiding Favorite buttons");
            //disable the buttons to allow remote navigation
            GUIControl.DisableControl(GetID, btnFavorite.GetID);
            GUIControl.HideControl(GetID, btnFavorite.GetID);
        }

        private void ShowFavoriteButtons()
        {
            GUIControl.ShowControl(GetID, btnFavorite.GetID);
            GUIControl.EnableControl(GetID, btnFavorite.GetID);
        }

        void HideFacade()
        {
            GUIControl.DisableControl(GetID, facadeView.GetID);
            GUIControl.HideControl(GetID, facadeView.GetID);
        }

        void ShowFacade()
        {
            GUIControl.ShowControl(GetID, facadeView.GetID);
            GUIControl.EnableControl(GetID, facadeView.GetID);
        }

        /*
        void HideFacadeViewAsButton()
        {
            GUIControl.DisableControl(GetID, btnViewAs.GetID);
            GUIControl.HideControl(GetID, btnViewAs.GetID);
        }

        void ShowFacadeViewAsButton()
        {
            GUIControl.ShowControl(GetID, btnViewAs.GetID);
            GUIControl.EnableControl(GetID, btnViewAs.GetID);
        }
        */

        void HideNextPageButton()
        {
            GUIControl.DisableControl(GetID, btnNext.GetID);
            GUIControl.HideControl(GetID, btnNext.GetID);
        }

        void ShowNextPageButton()
        {
            GUIControl.ShowControl(GetID, btnNext.GetID);
            GUIControl.EnableControl(GetID, btnNext.GetID);
        }

        void HidePreviousPageButton()
        {
            GUIControl.DisableControl(GetID, btnPrevious.GetID);
            GUIControl.HideControl(GetID, btnPrevious.GetID);
        }

        void ShowPreviousPageButton()
        {
            GUIControl.ShowControl(GetID, btnPrevious.GetID);
            GUIControl.EnableControl(GetID, btnPrevious.GetID);
        }

        void HideEnterPinButton()
        {            
            GUIControl.HideControl(GetID, btnEnterPin.GetID);
            GUIControl.DisableControl(GetID, btnEnterPin.GetID);
        }

        void ShowEnterPinButton()
        {
            GUIControl.ShowControl(GetID, btnEnterPin.GetID);
            GUIControl.EnableControl(GetID, btnEnterPin.GetID);
        }

        private void ChangeFacadeView()
        {
            switch (currentView)
            {
                case GUIFacadeControl.ViewMode.List:
                    currentView = GUIFacadeControl.ViewMode.SmallIcons;
                    break;
                case GUIFacadeControl.ViewMode.SmallIcons:
                    currentView = GUIFacadeControl.ViewMode.LargeIcons;
                    break;
                case GUIFacadeControl.ViewMode.LargeIcons:                    
                    currentView = GUIFacadeControl.ViewMode.List;
                    break;
            }
            if (CurrentState == State.sites)
            {
                currentSiteView = currentView;
                SwitchView();
            }
            else if (CurrentState == State.categories)
            {
                currentCategoryView = currentView;
                SwitchView();
            }
            else if (CurrentState == State.videos)
            {
                currentVideoView = currentView;
                SwitchView();
            }            
        }

        protected void SwitchView()
        {
            if (facadeView == null) return;
            
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
            GUIControl.SetControlLabel(GetID, btnViewAs.GetID, strLine);

            // keep track of the currently selected item (is lost when switching view)
            int rememberIndex = facadeView.SelectedListItemIndex;
            facadeView.View = currentView; // explicitly set the view (fixes bug that facadeView.list isn't working at startup
            if (rememberIndex > -1) GUIControl.SelectItemControl(GetID, facadeView.GetID, rememberIndex);
        }

        private void DisplayVideoInfo(VideoInfo foVideo)
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
            bool refreshList = false;

            if (selectedSite is IFavorite)
            {                
                if (currentVideosDisplayMode == VideosMode.Favorites)
                {
                    Log.Info("Received request to remove video from favorites.");                         
                    ((IFavorite)selectedSite).removeFavorite(loSelectedVideo);
                    refreshList = true;
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

                if (currentVideosDisplayMode == VideosMode.Favorites || selectedSite is Sites.FavoriteUtil)
                {
                    db.removeFavoriteVideo(loSelectedVideo);
                    refreshList = true;
                }
                else
                {
                    db.addFavoriteVideo(loSelectedVideo, selectedSite.Settings.Name);
                }
            }
            if (refreshList) refreshVideoList();
        }

        #endregion
         
        /// <summary>
        /// This method should be used to call methods from site utils that might take a few seconds.
        /// It makes sure only on thread at a time executes and has a timeout for the execution.
        /// It also catches Execeptions from the utils and writes errors to the log.
        /// </summary>
        /// <param name="task">a delegate pointing to the (anonymous) method to invoke.</param>
        /// <returns>true, if execution finished successfully before the timeout.</returns>
        bool ExecuteInBackgroundAndWait(System.Threading.ThreadStart task)
        {
            lock (this) // make sure only one task at a time can be executed in background
            {
                bool success = false;
                try
                {
                    GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor in MediaPortal
                    DateTime end = DateTime.Now.AddSeconds(10); // point in time until we wait for the execution of this task
                    bool? result = null; // while this is null the task has not finished, true indicates successfull completion and false and error (or canceled due to timeout)

                    System.Threading.Thread backgroundThread = new System.Threading.Thread(delegate()
                        {
                            try
                            {
                                task.Invoke();
                                result = true;
                            }
                            catch (System.Threading.ThreadAbortException)
                            {
                                Log.Error("Timeout waiting for results.");
                                result = false;
                            }
                            catch (Exception threadException)
                            {
                                Log.Error(threadException);
                                result = false;
                            }
                        }
                        ) { Name = "OnlineVideos", IsBackground = true };
                    backgroundThread.Start();

                    while (result == null)
                    {
                        GUIWindowManager.Process();
                        if (DateTime.Now > end) backgroundThread.Abort();
                    }
                    success = result.Value;
                }
                catch (Exception ex)
                {
                    success = false;
                    Log.Error(ex);
                }
                finally
                {
                    GUIWaitCursor.Hide();
                }
                return success;
            }
        }
    }
}