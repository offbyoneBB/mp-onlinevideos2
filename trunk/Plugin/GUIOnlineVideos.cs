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

        public enum State { home = 0, categories = 1, videos = 2, info = 3 }

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
        [SkinControlAttribute(1)]
        protected GUIImage logoImage = null;
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
        bool ageHasBeenConfirmed = false;
        State currentState = State.home;

        SiteSettings selectedSite;
        int selectedSiteIndex = 0;
        
        Category selectedCategory;
        int selectedCategoryIndex = 0;

        VideoInfo selectedVideo;
        int selectedVideoIndex = 0; // used to remember the position of the last selected Video
        int selectedClipIndex = 0;  // used to remember the position the last selected Trailer
        
        List<VideoInfo> moCurrentVideoList = new List<VideoInfo>();
        List<VideoInfo> moCurrentTrailerList = new List<VideoInfo>();        

        bool showingFavorites = false;

        SiteOrder siteOrder = SiteOrder.AsInFile;

        RTMP_LIB.HTTPServer rtmpServer;
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
        private bool searchMode = false;        
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
            if (rtmpServer == null) rtmpServer = new RTMP_LIB.HTTPServer();
            GUIPropertyManager.SetProperty("#OnlineVideos.desc", " ");
            GUIPropertyManager.SetProperty("#OnlineVideos.length", " ");
            return result;
        }
        
        protected override void OnPageLoad()
        {
            base.OnPageLoad(); // let animations run

            // everytime the plugin is shown, after some other window was shown
            if (ageHasBeenConfirmed && PreviousWindowId==0)
            {
                // if a pin was inserted before, reset to false and show the home page in case the user was browsing some adult site last
                Log.Debug("OnlineVideos Age Confirmed set to false.");
                ageHasBeenConfirmed = false;
                currentState = State.home;
            }

            Log.Debug("OnPageLoad. CurrentState:" + currentState);
            if (currentState == State.home)
            {
                DisplaySites();
                SwitchView();
            }
            else if (currentState == State.categories)
            {
                DisplayCategories();
                SwitchView();
            }
            else if (currentState == State.videos)
            {
                DisplayCategoryVideos();                
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

            if (selectedSite.UtilName == "AppleTrailers" && currentState == State.info) liSelected = infoList.SelectedListItemIndex - 1;

            if (liSelected < 0 || currentState == State.home || currentState == State.categories || (selectedSite.UtilName == "AppleTrailers" && currentState == State.videos))
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

                if (showingFavorites == false && selectedSite.UtilName != "Favorite")
                {
                    if (selectedSite.UtilName != "DownloadedVideo") dlgSel.Add(GUILocalizeStrings.Get(930)/*Add to favorites*/);
                }
                else
                {
                    dlgSel.Add(GUILocalizeStrings.Get(933)/*Remove from favorites*/);
                }
                if (selectedSite.UtilName == "YouTube")
                {
                    dlgSel.Add("Related Videos");
                }
                if (String.IsNullOrEmpty(OnlineVideoSettings.getInstance().msDownloadDir) == false)
                {
                    if (selectedSite.UtilName == "DownloadedVideo") dlgSel.Add(GUILocalizeStrings.Get(117)/*Delete*/); else dlgSel.Add(GUILocalizeStrings.Get(190)/*Save*/);
                }
            }
            dlgSel.DoModal(GetID);
            int liSelectedIdx = dlgSel.SelectedId;
            VideoInfo loSelectedVideo;
            if (currentState == State.videos)
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
                    if (selectedSite.UtilName == "DownloadedVideo")
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
                    if (selectedSite.UtilName == "YouTube")
                    {
                        getRelatedVideos(loSelectedVideo);
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
            if (action.wID == Action.ActionType.ACTION_PREVIOUS_MENU && currentState != State.home)
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
            if (control == facadeView && actionType == Action.ActionType.ACTION_SELECT_ITEM)
            {
                if (currentState == State.home)
                {
                    selectedSite = OnlineVideoSettings.getInstance().moSiteList[facadeView.SelectedListItem.Path];
                    selectedSiteIndex = facadeView.SelectedListItemIndex;                    
                    DisplayCategories();
                    currentState = State.categories;
                }
                else if (currentState == State.categories)
                {
                    if (facadeView.SelectedListItemIndex == 0)
                    {
                        OnShowPreviousMenu();
                    }
                    else
                    {
                        selectedCategory = selectedSite.Categories[facadeView.SelectedListItem.Label];
                        selectedCategoryIndex = facadeView.SelectedListItemIndex;
                        selectedVideo = null;
                        selectedVideoIndex = 0;
                        currentState = State.videos;
                        HideFacade();

                        refreshVideoList();
                    }
                }
                else if (currentState == State.videos)
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
                        if (selectedSite.Util.hasMultipleVideos())
                        {
                            currentState = State.info;
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
            else if (control == infoList && actionType == Action.ActionType.ACTION_SELECT_ITEM && currentState == State.info)
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
                    moCurrentVideoList = selectedSite.Util.getNextPageVideos();
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                selectedVideoIndex = 0;
                DisplayCategoryVideos();
                UpdateViewState();

                if (selectedSite.Util.hasNextPage()) ShowNextPageButton();
                else HideNextPageButton();

                if (selectedSite.Util.hasPreviousPage()) ShowPreviousPageButton();
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
                    moCurrentVideoList = selectedSite.Util.getPreviousPageVideos();
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                selectedVideoIndex = 0;
                DisplayCategoryVideos();
                UpdateViewState();

                if (selectedSite.Util.hasNextPage()) ShowNextPageButton();
                else HideNextPageButton();

                if (selectedSite.Util.hasPreviousPage()) ShowPreviousPageButton();
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
                if (currentState == State.home) siteOrder = (SiteOrder)btnOrderBy.SelectedItem;
            }
            else if (control == btnTimeFrame)
            {
                GUIControl.SelectItemControl(GetID, btnTimeFrame.GetID, btnTimeFrame.SelectedItem);
            }
            else if (control == btnUpdate)
            {
                GUIControl.UnfocusControl(GetID, btnUpdate.GetID);

                switch (currentState)
                {
                    case State.home: DisplaySites(); break;
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
                GetUserInputString(ref query);
                GUIControl.FocusControl(GetID, facadeView.GetID);
                if (query != String.Empty)
                {
                    msLastSearchQuery = query;
                    SearchVideos(query);
                    currentState = State.videos;
                    UpdateViewState();
                }
            }
            else if ((control == btnFavorite))
            {
                GUIControl.FocusControl(GetID, facadeView.GetID);
                DisplayFavoriteVideos();
                currentState = State.videos;
                UpdateViewState();
            }
            else if (control == btnEnterPin)
            {
                string pin = String.Empty;
                GetUserInputString(ref pin);
                if (pin == OnlineVideoSettings.getInstance().pinAgeConfirmation)
                {
                    ageHasBeenConfirmed = true;
                    HideEnterPinButton();
                    DisplaySites();
                    GUIControl.FocusControl(GetID, facadeView.GetID);
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
            Log.Info("OnShowPreviousMenu CurrentState:" + currentState);
            if (currentState == State.categories)
            {
                DisplaySites();
                currentState = State.home;
            }
            else if (currentState == State.videos)
            {
                Log.Info("Set the stopDownload to true 3");
                ImageDownloader._stopDownload = true;

                DisplayCategories();
                currentState = State.categories;
                showingFavorites = false;
                searchMode = false;
            }
            else if (currentState == State.info)
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
                currentState = State.videos;
            }            
            UpdateViewState();
        }        

        private void item_OnItemSelected(GUIListItem item, GUIControl parent)
        {
            GUIFilmstripControl filmstrip = parent as GUIFilmstripControl;
            if (filmstrip == null)
                return;
            filmstrip.InfoImageFileName = item.ThumbnailImage;
        }
        
        private void LoadSettings()
        {
            using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                currentSiteView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                siteOrder = (SiteOrder)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.SITEVIEW_ORDER, 0);
                currentVideoView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.VIDEOVIEW_MODE, (int)GUIFacadeControl.ViewMode.SmallIcons);
                currentCategoryView = (GUIFacadeControl.ViewMode)xmlreader.GetValueAsInt(OnlineVideoSettings.SECTION, OnlineVideoSettings.CATEGORYVIEW_MODE, (int)GUIFacadeControl.ViewMode.List);
                SwitchView();
            }
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();            
            //create a favorites site
            SiteSettings SelectedSite = new SiteSettings();            
            SelectedSite.Name = "Favorites";
            SelectedSite.UtilName = "Favorite";
            SelectedSite.IsEnabled = true;
            RssLink cat = new RssLink();
            cat.Name = "dynamic";
            cat.Url = "favorites";
            SelectedSite.Categories.Add(cat.Name, cat);
            OnlineVideoSettings.getInstance().moSiteList.Add(SelectedSite.Name, SelectedSite);

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
                SelectedSite.Categories.Add(cat.Name, cat);
                OnlineVideoSettings.getInstance().moSiteList.Add(SelectedSite.Name, SelectedSite);
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
            selectedCategoryIndex = 0;
            GUIControl.ClearControl(GetID, facadeView.GetID);

            // set order by options
            btnOrderBy.Clear();
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(886)); //Default
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(103)); //Name
            GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, GUILocalizeStrings.Get(304)); //Language
            btnOrderBy.SelectedItem = (int)siteOrder;
            
            // get names in right order
            string[] names = new string[OnlineVideoSettings.getInstance().moSiteList.Count];
            switch (siteOrder)
            {
                case SiteOrder.Name:
                    OnlineVideoSettings.getInstance().moSiteList.Keys.CopyTo(names, 0);
                    Array.Sort(names);
                    break;
                case SiteOrder.Language:
                    Dictionary<string, List<string>> sitenames = new Dictionary<string, List<string>>();
                    foreach (SiteSettings aSite in OnlineVideoSettings.getInstance().moSiteList.Values)
                    {
                        string key = string.IsNullOrEmpty(aSite.Language) ? "zzzzz" : aSite.Language; // puts empty lang at the end
                        List<string> listForLang = null;
                        if (!sitenames.TryGetValue(key, out listForLang)) { listForLang = new List<string>(); sitenames.Add(key, listForLang); }
                        listForLang.Add(aSite.Name);
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
                    OnlineVideoSettings.getInstance().moSiteList.Keys.CopyTo(names, 0);
                    break;
            }
            
            foreach (string name in names)
            {
                SiteSettings aSite = OnlineVideoSettings.getInstance().moSiteList[name];
                if (aSite.IsEnabled &&
                    (!aSite.ConfirmAge || !OnlineVideoSettings.getInstance().useAgeConfirmation || ageHasBeenConfirmed))
                {
                    GUIListItem loListItem = new GUIListItem(aSite.Name);
                    loListItem.Label2 = aSite.Language;
                    loListItem.Path = aSite.Name;
                    loListItem.IsFolder = true;
                    string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + aSite.Name + ".png";
                    if (System.IO.File.Exists(image))
                    {
                        loListItem.ThumbnailImage = image;                        
                        loListItem.IconImage = image;
                        loListItem.IconImageBig = image;
                        loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(item_OnItemSelected);
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

        private void DisplayCategories()
        {
            GUIControl.ClearControl(GetID, facadeView.GetID);
            GUIListItem loListItem;
            loListItem = new GUIListItem("..");
            loListItem.IsFolder = true;
            MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
            facadeView.Add(loListItem);

            int numCategoriesWithThumb = 0;
            List<String> imagesUrlList = new List<string>();

            if (!selectedSite.DynamicCategoriesDiscovered)
            {
                try
                {
                    GUIWaitCursor.Init();
                    GUIWaitCursor.Show();
                    worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e) 
                        {
                            Log.Info("Looking for dynamic categories for {0}", selectedSite.Name);
                            try
                            {
                                int foundCategories = selectedSite.Util.DiscoverDynamicCategories(selectedSite);
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

            foreach (Category loCat in selectedSite.Categories.Values)
            {
                loListItem = new GUIListItem(loCat.Name);
                loListItem.IsFolder = true;
                MediaPortal.Util.Utils.SetDefaultIcons(loListItem);
                // Favorite Catgories can have the same images as the home view
                if (selectedSite.Util is Sites.FavoriteUtil)
                {
                    string image = OnlineVideoSettings.getInstance().BannerIconsDir + @"Icons\" + ((RssLink)loCat).Url.Substring(4) + ".png";
                    if (System.IO.File.Exists(image))
                    {
                        loListItem.ThumbnailImage = image;
                        loListItem.IconImage = image;
                        loListItem.IconImageBig = image;                    
                    }
                }                
                // SVTPlay Categories can have images from web
                else if (selectedSite.Util is Sites.SVTPlayUtil && loCat is OnlineVideos.Sites.SVTPlayCategory)
                {
                    OnlineVideos.Sites.SVTPlayCategory sVTPlayCategory = (OnlineVideos.Sites.SVTPlayCategory)loCat;

                    if (sVTPlayCategory.Thumb != null && sVTPlayCategory.Thumb != string.Empty)
                    {
                        string image = OnlineVideoSettings.getInstance().msThumbLocation + sVTPlayCategory.Thumb;

                        if (System.IO.File.Exists(image))
                        {
                            loListItem.ThumbnailImage = image;
                            loListItem.IconImage = image;
                            loListItem.IconImageBig = image;
                        }
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

            suggestedView = null;

            if (numCategoriesWithThumb > 0)
                ImageDownloader.getImages(imagesUrlList, OnlineVideoSettings.getInstance().msThumbLocation, facadeView);
            else
                suggestedView = GUIFacadeControl.ViewMode.List;

            if (selectedCategoryIndex < facadeView.Count) facadeView.SelectedListItemIndex = selectedCategoryIndex;
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
                if (searchMode)
                {
                    Log.Info("Filtering search result");
                    //filtering a search result
                    if (String.IsNullOrEmpty(msLastSearchCategory))
                    {
                        loListItems = ((IFilter)selectedSite.Util).filterSearchResultList(msLastSearchQuery, miMaxResult, msOrderBy, msTimeFrame);
                    }
                    else
                    {
                        loListItems = ((IFilter)selectedSite.Util).filterSearchResultList(msLastSearchQuery, msLastSearchCategory, miMaxResult, msOrderBy, msTimeFrame);
                    }
                }
                else
                {
                    loListItems = ((IFilter)selectedSite.Util).filterVideoList(selectedCategory, miMaxResult, msOrderBy, msTimeFrame);
                }
            });
            worker.RunWorkerAsync();
            while (worker.IsBusy) GUIWindowManager.Process();

            GUIWaitCursor.Hide();

            UpdateVideoList(loListItems);
            moCurrentVideoList = loListItems;
        }

        private void SearchVideos(String query)
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
                        loListItems = ((ISearch)selectedSite.Util).Search(selectedSite.SearchUrl, query, category);
                    }
                    else
                    {
                        Log.Info("Searching for {0} in all categories ", query);
                        loListItems = ((ISearch)selectedSite.Util).Search(selectedSite.SearchUrl, query);
                    }
                });
                worker.RunWorkerAsync();
                while (worker.IsBusy) GUIWindowManager.Process();

                GUIWaitCursor.Hide();

                UpdateVideoList(loListItems);
                searchMode = true;
                moCurrentVideoList = loListItems;
            }
        }

        private void refreshVideoList()
        {
            if (searchMode)
            {
                SearchVideos(msLastSearchQuery);
            }
            else if (showingFavorites)
            {
                DisplayFavoriteVideos();
            }
            else
            {                
                GUIWaitCursor.Init();
                GUIWaitCursor.Show();

                bool success = false;
                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                {                    
                    List<VideoInfo> loListItems = selectedSite.Util.getVideoList(selectedCategory);                    

                    if (loListItems != null && loListItems.Count > 0)
                    {
                        moCurrentVideoList.Clear();
                        GUIControl.ClearControl(GetID, facadeView.GetID);

                        if (selectedSite.Util.hasNextPage()) ShowNextPageButton();
                        else HideNextPageButton();

                        if (selectedSite.Util.hasPreviousPage()) ShowPreviousPageButton();
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

                    if (selectedSite.UtilName == "Favorite")
                    {
                        DisplayCategories();
                    }
                    currentState = State.categories;
                    UpdateViewState();
                }
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

        private void DisplayFavoriteVideos()
        {
            List<VideoInfo> loVideoList = ((IFavorite)selectedSite.Util).getFavorites(selectedSite.Username, selectedSite.Password);
            UpdateVideoList(loVideoList);
            moCurrentVideoList = loVideoList;
            showingFavorites = true;
        }
        
        private void UpdateVideoList(List<VideoInfo> foVideos)
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
            if (foVideos.Count == 0)
            {
                GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlg_error.SetHeading(PluginName());
                dlg_error.SetLine(1, GUILocalizeStrings.Get(1036)/*No Videos found!*/); 
                dlg_error.SetLine(2, String.Empty);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                //currentState = State.categories;
                //DisplayCategories();
                //UpdateViewState();
                return;
            }
            List<String> loImageUrlList = new List<string>();
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
                loListItem.RetrieveArt = false;
                loListItem.OnRetrieveArt += new MediaPortal.GUI.Library.GUIListItem.RetrieveCoverArtHandler(OnRetrieveCoverArt);
                loListItem.OnItemSelected += new MediaPortal.GUI.Library.GUIListItem.ItemSelectedHandler(OnVideoItemSelected);
                facadeView.Add(loListItem);
                loImageUrlList.Add(loVideoInfo.ImageUrl);
            }
            ImageDownloader.getImages(loImageUrlList, OnlineVideoSettings.getInstance().msThumbLocation, facadeView);
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
                loVideoList = selectedSite.Util.getOtherVideoList(foVideo);
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
                currentState = State.videos;
                DisplayCategoryVideos();
                return;
            }

            moCurrentTrailerList.Clear();
            GUIControl.ClearControl(GetID, facadeView.GetID);
            GUIControl.ClearControl(GetID, 51);
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

        private bool GetUserInputString(ref string sString)
        {
            VirtualKeyboard keyBoard = (VirtualKeyboard)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_VIRTUAL_KEYBOARD);
            keyBoard.Reset();
            keyBoard.IsSearchKeyboard = true;
            keyBoard.Text = sString;
            keyBoard.DoModal(GetID); // show it...
            if (keyBoard.IsConfirmed) sString = keyBoard.Text;
            return keyBoard.IsConfirmed;
        }

        private void Play(VideoInfo foListItem)
        {            
            bool playing = false;            
            if (selectedSite.Util.MultipleFilePlay())
            {
                PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
                PlayListItem item;

                List<String> loUrlList = selectedSite.Util.getMultipleVideoUrls(foListItem, selectedSite);
                foreach (String url in loUrlList)
                {
                    item = new PlayListItem("", url);
                    videoList.Add(item);
                }
                PlayListPlayer.SingletonPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO_TEMP;
                if (PlayListPlayer.SingletonPlayer.Play(0))
                {
                    playing = true;
                }
            }
            else
            {
                String lsUrl = "";
                try
                {
                    GUIWaitCursor.Init();
                    GUIWaitCursor.Show();
                    worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(delegate(object o, DoWorkEventArgs e)
                    {
                        lsUrl = selectedSite.Util.getUrl(foListItem, selectedSite);
                    });
                    worker.RunWorkerAsync();
                    while (worker.IsBusy) GUIWindowManager.Process();                
                }
                catch { }
                finally
                {
                    GUIWaitCursor.Hide();
                }

                if (String.IsNullOrEmpty(lsUrl))
                {                    
                    GUIDialogNotify dlg = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                    dlg.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                    dlg.SetText("Unable to play the video.");
                    dlg.DoModal(GUIWindowManager.ActiveWindow);
                    return;
                }

                if (g_Player.Playing) g_Player.Stop();
                
                IPlayerFactory savedFactory = g_Player.Factory;
                g_Player.Factory = new OnlineVideos.Player.PlayerFactory();
                g_Player.Play(lsUrl);
                g_Player.Factory = savedFactory;

                if (g_Player.Player != null && g_Player.IsVideo)
                {
                    playing = true;
                    GUIGraphicsContext.IsFullScreenVideo = true;
                    GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_FULLSCREEN_VIDEO);

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
            PlayListPlayer.SingletonPlayer.Init();
            PlayListPlayer.SingletonPlayer.Reset();
            PlayListPlayer.SingletonPlayer.g_Player = new Player.PlaylistPlayerWrapper();
            PlayList videoList = PlayListPlayer.SingletonPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP);
            videoList.Clear();
            PlayListItem item;
            OnlineVideoSettings settings = OnlineVideoSettings.getInstance();
            String lsUrl;            
            List<VideoInfo> loVideoList;
            if (selectedSite.UtilName == "AppleTrailers")
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
                lsUrl = selectedSite.Util.getUrl(loVideo, selectedSite);
                item = new PlayListItem(loVideo.Title, lsUrl);
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
            string url = selectedSite.Util.getUrl(video, selectedSite);
            string safeName = selectedSite.Util.GetFileNameForDownload(video, url);
            string localFileName = System.IO.Path.Combine(OnlineVideoSettings.getInstance().msDownloadDir, safeName);
            string thumbFile = ImageDownloader.GetThumbFile(video.ImageUrl);
            
            // download file
            WebClient loClient = new WebClient();
            loClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2");
            loClient.DownloadFileCompleted += DownloadFileCompleted;
            loClient.DownloadFileAsync(new Uri(url), localFileName, new DownloadInfo { Title = video.Title, DownloadedFile = localFileName, ThumbFile = thumbFile });
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                GUIDialogNotify loDlgNotify = (GUIDialogNotify)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_NOTIFY);
                loDlgNotify.SetHeading(GUILocalizeStrings.Get(257)/*ERROR*/);
                loDlgNotify.SetText((e.UserState as DownloadInfo).Title);
                loDlgNotify.DoModal(GetID);
            }
            else
            {
                DownloadInfo downloadInfo = e.UserState as DownloadInfo;
                // save thumb for this video as well if it exists
                if (System.IO.File.Exists(downloadInfo.ThumbFile))
                {
                    string localImageName = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(downloadInfo.DownloadedFile),
                        System.IO.Path.GetFileNameWithoutExtension(downloadInfo.DownloadedFile))
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
            }
            return fsStr;
        }

        private void UpdateViewState()
        {
            Log.Info("Updating View State");
            switch (currentState)
            {
                case State.home:
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
                    if (OnlineVideoSettings.getInstance().useAgeConfirmation && !ageHasBeenConfirmed)
                        ShowEnterPinButton();
                    else
                        HideEnterPinButton();
                    DisplayVideoInfo(null);
                    currentView = currentSiteView;
                    SwitchView();
                    break;
                case State.categories:
                    GUIPropertyManager.SetProperty("#header.label", selectedSite.Name);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Name + ".png");
                    ShowFacade();
                    //HideFacadeViewAsButton();
                    HideNextPageButton();
                    HidePreviousPageButton();
                    HideVideoDetails();
                    HideFilterButtons();                    
                    if (selectedSite.Util is ISearch) ShowSearchButtons(); else HideSearchButtons();
                    if (selectedSite.Util is IFavorite) ShowFavoriteButtons(); else HideFavoriteButtons();
                    HideEnterPinButton();
                    DisplayVideoInfo(null);
                    currentView = suggestedView != null ? suggestedView.Value : currentCategoryView;
                    SwitchView();
                    break;
                case State.videos:
                    string headerlabel = selectedCategory != null ? selectedCategory.Name : "";
                    if (searchMode) headerlabel = GUILocalizeStrings.Get(283);
                    else if (showingFavorites) headerlabel = GUILocalizeStrings.Get(932);
                    GUIPropertyManager.SetProperty("#header.label", headerlabel);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Name + ".png");
                    ShowFacade();
                    //ShowFacadeViewAsButton();
                    if (selectedSite.Util.hasNextPage()) ShowNextPageButton(); else HideNextPageButton();
                    if (selectedSite.Util.hasPreviousPage()) ShowPreviousPageButton(); else HidePreviousPageButton();
                    HideVideoDetails();
                    if (selectedSite.Util is IFilter) ShowFilterButtons(); else HideFilterButtons();
                    HideSearchButtons();
                    HideFavoriteButtons();                    
                    HideEnterPinButton();
                    DisplayVideoInfo(selectedVideo);
                    currentView = currentVideoView;                    
                    SwitchView();
                    break;
                case State.info:
                    GUIPropertyManager.SetProperty("#header.label", selectedVideo.Title);
                    GUIPropertyManager.SetProperty("#header.image", OnlineVideoSettings.getInstance().BannerIconsDir + @"Banners/" + selectedSite.Name + ".png");
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
            if (currentState == State.info)
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
            GUIPropertyManager.SetProperty("#OnlineVideos.movietitle", selectedVideo.Title);
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

            moSupportedMaxResultList = ((IFilter)selectedSite.Util).getResultSteps();
            foreach (int step in moSupportedMaxResultList)
            {
                GUIControl.AddItemLabelControl(GetID, btnMaxResult.GetID, step + "");
            }
            moSupportedOrderByList = ((IFilter)selectedSite.Util).getOrderbyList();
            foreach (String orderBy in moSupportedOrderByList.Keys)
            {
                GUIControl.AddItemLabelControl(GetID, btnOrderBy.GetID, orderBy);
            }
            moSupportedTimeFrameList = ((IFilter)selectedSite.Util).getTimeFrameList();
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
            moSupportedSearchCategoryList = ((ISearch)selectedSite.Util).GetSearchableCategories(selectedSite.CategoriesArray);
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
            if (currentState == State.home)
            {
                currentSiteView = currentView;
                SwitchView();
            }
            else if (currentState == State.categories)
            {
                currentCategoryView = currentView;
                SwitchView();
            }
            else if (currentState == State.videos)
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
            if (facadeView.View != currentView)
            {
                int rememberIndex = facadeView.SelectedListItemIndex;
                facadeView.View = currentView;
                facadeView.SelectedListItemIndex = rememberIndex;
            }
            GUIControl.SetControlLabel(GetID, btnViewAs.GetID, strLine);
        }

        private void DisplayVideoInfo(VideoInfo foVideo)
        {
            if (foVideo == null)
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.videotitle", String.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.tags", String.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.length", String.Empty);
                GUIPropertyManager.SetProperty("#OnlineVideos.desc", String.Empty);
            }
            else
            {
                GUIPropertyManager.SetProperty("#OnlineVideos.videotitle", foVideo.Title);
                if (String.IsNullOrEmpty(foVideo.Tags))
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.tags", "None");
                }
                else
                {
                    GUIPropertyManager.SetProperty("#OnlineVideos.tags", foVideo.Tags);
                }
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

            if (selectedSite.Util is IFavorite)
            {
                if (string.IsNullOrEmpty(selectedSite.Password) || string.IsNullOrEmpty(selectedSite.Username))
                {
                    GUIDialogOK dlg_error = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    dlg_error.SetHeading(PluginName());
                    dlg_error.SetLine(1, "Please set your username and password in the Configuration");
                    dlg_error.SetLine(2, String.Empty);
                    dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                }
                else
                {
                    if (showingFavorites)
                    {
                        Log.Info("Received request to remove video from favorites.");                         
                        ((IFavorite)selectedSite.Util).removeFavorite(loSelectedVideo, selectedSite.Username, selectedSite.Password);
                        refreshList = true;
                    }
                    else
                    {
                        Log.Info("Received request to add video to favorites.");
                        ((IFavorite)selectedSite.Util).addFavorite(loSelectedVideo, selectedSite.Username, selectedSite.Password);
                    }
                }
            }
            else
            {
                if (showingFavorites || selectedSite.UtilName == "Favorite")
                {
                    selectedSite.Util.RemoveFavorite(loSelectedVideo);
                    refreshList = true;
                }
                else
                {
                    selectedSite.Util.AddFavorite(loSelectedVideo, selectedSite);
                }
            }
            if (refreshList) refreshVideoList();
        }

        private void getRelatedVideos(VideoInfo loSelectedVideo)
        {
            moCurrentVideoList = selectedSite.Util.getRelatedVideos(loSelectedVideo.VideoUrl);
            DisplayCategoryVideos();
        }

        #endregion
    }
}