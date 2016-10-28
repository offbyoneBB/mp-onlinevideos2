using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using OnlineVideos.Downloading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosWorkflowModel : IWorkflowModel
    {
        public OnlineVideosWorkflowModel()
        {
            SiteGroupsList = new ItemsList();
            SitesList = new ItemsList();
            
            // create a message queue where we listen to changes to the sites
            _messageQueue = new AsynchronousMessageQueue(this, new string[] { OnlineVideosMessaging.CHANNEL });
            _messageQueue.MessageReceived += new MessageReceivedHandler(OnlineVideosMessageReceived);
            _messageQueue.Start();
        }

        protected AsynchronousMessageQueue _messageQueue;
        
        bool sitesListHasAllSites = false;

        SiteViewModel _focusedSite;
        public SiteViewModel FocusedSite
        {
            get { return _focusedSite; }
            set { if (value != null) _focusedSite = value; }
        }

        protected AbstractProperty _searchStringProperty = new WProperty(typeof(string), string.Empty);
        public AbstractProperty SearchStringProperty { get { return _searchStringProperty; } }
        public string SearchString
        {
            get { return (string)_searchStringProperty.GetValue(); }
            set { _searchStringProperty.SetValue(value); }
        }

        public SiteViewModel SelectedSite { get; protected set; }
        public CategoryViewModel SelectedCategory { get; protected set; }
        public VideoViewModel SelectedVideo { get; protected set; }
        public VideoViewModel SelectedDetailsVideo { get; protected set; }

        public ItemsList SiteGroupsList { get; protected set; }
        public ItemsList SitesList { get; protected set; }
        public ItemsList CategoriesList { get; protected set; }
        public ItemsList VideosList { get; protected set; }
        public List<VideoViewModel> DetailsVideosList { get; protected set; }

        public void SelectSiteGroup(SiteGroupViewModel siteGroupModel)
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            SitesList.Clear();
            foreach (string siteName in siteGroupModel.Sites)
            {
                var siteutils = OnlineVideoSettings.Instance.SiteUtilsList;
                SitesList.Add(new SiteViewModel(siteutils[siteName]));
            }
            sitesListHasAllSites = false;
            SitesList.FireChange();
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateSites, new NavigationContextConfig() { NavigationContextDisplayLabel = siteGroupModel.Labels["Name"].ToString() });
        }

        public void SelectSite(SiteViewModel siteModel)
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            if (!siteModel.Site.Settings.DynamicCategoriesDiscovered)
            {
                BackgroundTask.Instance.Start<bool>(
                    () =>
                    {
                        siteModel.Site.DiscoverDynamicCategories();
                        return true;
                    },
                    (success, result) =>
                    {
                        if (success)
                        {
                            SelectedSite = siteModel;
                            ShowCategories(siteModel.Site.Settings.Categories, SelectedSite.Name);
                        }
                    },
                    Translation.Instance.GettingDynamicCategories);
            }
            else
            {
                SelectedSite = siteModel;
                ShowCategories(siteModel.Site.Settings.Categories, SelectedSite.Name);
            }
        }

        public void SelectCategory(CategoryViewModel categoryModel)
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            if (categoryModel.Category is NextPageCategory)
            {
                // discover and append next page categories
                BackgroundTask.Instance.Start<bool>(
                    () =>
                    {
                        SelectedSite.Site.DiscoverNextPageCategories(categoryModel.Category as NextPageCategory);
                        return true;
                    },
                    (success, result) =>
                    {
                        if (success)
                        {
                            int selectNr = CategoriesList.Count - 1;
                            CategoriesList.Clear();
                            IList<Category> catList = categoryModel.Category.ParentCategory == null ? (IList<Category>)SelectedSite.Site.Settings.Categories : categoryModel.Category.ParentCategory.SubCategories;
                            foreach (Category c in catList) CategoriesList.Add(new CategoryViewModel(c) { Selected = CategoriesList.Count == selectNr });
                            ImageDownloader.GetImages<Category>(catList);
                            CategoriesList.FireChange();
                        }
                    },
                    Translation.Instance.GettingNextPageVideos);
            }
            else
            {
                if (categoryModel.Category.HasSubCategories)
                {
                    if (!categoryModel.Category.SubCategoriesDiscovered)
                    {
                        // discover and show subcategories
                        BackgroundTask.Instance.Start<bool>(
                            () =>
                            {
                                SelectedSite.Site.DiscoverSubCategories(categoryModel.Category);
                                return true;
                            },
                            (success, result) =>
                            {
                                if (success)
                                {
                                    SelectedCategory = categoryModel;
                                    ShowCategories(categoryModel.Category.SubCategories, categoryModel.Name);
                                }
                            },
                            Translation.Instance.GettingDynamicCategories);
                    }
                    else
                    {
                        SelectedCategory = categoryModel;
                        ShowCategories(categoryModel.Category.SubCategories, categoryModel.Name);
                    }
                }
                else
                {
                    // discover and show videos of this category
                    BackgroundTask.Instance.Start<List<VideoInfo>>(
                        () =>
                        {
                            return SelectedSite.Site.GetVideos(categoryModel.Category);
                        },
                        (success, videos) =>
                        {
                            if (success)
                            {
                                ShowVideos(categoryModel, videos);
                            }
                        },
                        Translation.Instance.GettingCategoryVideos);
                }
            }
        }

        public void SelectVideo(VideoViewModel videoModel)
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            if (videoModel.VideoInfo == null)
            {
                // discover and append next page videos
                BackgroundTask.Instance.Start<List<VideoInfo>>(
                    () =>
                    {
                        return SelectedSite.Site.GetNextPageVideos();
                    },
                    (success, nextPageVideos) =>
                    {
                        if (success)
                        {
                            VideosList.Remove(videoModel);
                            int selectNr = VideosList.Count;
                            nextPageVideos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoViewModel(r, SelectedCategory != null ? SelectedCategory.Category : null, SelectedSite.Site.Settings, false) { Selected = VideosList.Count == selectNr }); });
                            if (SelectedSite.Site.HasNextPage) VideosList.Add(new VideoViewModel(Translation.Instance.NextPage, "NextPage.png"));
                            VideosList.FireChange();
                            ImageDownloader.GetImages<VideoInfo>(nextPageVideos);
                        }
                    },
                    Translation.Instance.GettingNextPageVideos);
            }
            else
            {
                if (SelectedSite.Site is Sites.IChoice && videoModel.VideoInfo.HasDetails)
                {
                    // get details videos and show details view
                    BackgroundTask.Instance.Start<List<DetailVideoInfo>>(
                        () =>
                        {
                            return ((Sites.IChoice)SelectedSite.Site).GetVideoChoices(videoModel.VideoInfo);
                        },
                        (success, choices) =>
                        {
                            if (success)
                            {
                                SelectedVideo = videoModel;
                                ShowDetails(choices);
                            }
                        },
                        Translation.Instance.GettingVideoDetails);
                }
                else
                {
                    // get playback urls and play or show dialog to select playback options
                    BackgroundTask.Instance.Start<List<string>>(
                        () =>
                        {
                            return SelectedSite.Site.GetMultipleVideoUrls(videoModel.VideoInfo);
                        },
                        (success, urls) =>
                        {
                            if (success)
                            {
                                if (SelectedSite.Site.Settings.Player != PlayerType.Browser)
                                    Helpers.UriUtils.RemoveInvalidUrls(urls);
                                // if no valid urls were returned show error msg
                                if (urls == null || urls.Count == 0)
                                {
                                    ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.UnableToPlayVideo]", DialogType.OkDialog, false, DialogButtonType.Ok);
                                }
                                else
                                {
                                    SelectedVideo = videoModel;
                                    SelectedVideo.ChoosePlaybackOptions(urls[0], (url) => { urls[0] = url; SelectedVideo.Play(urls); });
                                }
                            }
                        },
                        Translation.Instance.GettingPlaybackUrlsForVideo);
                }
            }
        }

        public void SelectDetailsVideo(VideoViewModel videoModel)
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            BackgroundTask.Instance.Start<List<string>>(
                () =>
                {
                    return SelectedSite.Site.GetMultipleVideoUrls(videoModel.VideoInfo);
                },
                (success, urls) =>
                {
                    if (success)
                    {
                        if (SelectedSite.Site.Settings.Player != PlayerType.Browser)
                            Helpers.UriUtils.RemoveInvalidUrls(urls);
                        // if no valid urls were returned show error msg
                        if (urls == null || urls.Count == 0)
                        {
                            ServiceRegistration.Get<IDialogManager>().ShowDialog("[OnlineVideos.Error]", "[OnlineVideos.UnableToPlayVideo]", DialogType.OkDialog, false, DialogButtonType.Ok);
                        }
                        else
                        {
                            SelectedDetailsVideo = videoModel;
                            SelectedDetailsVideo.ChoosePlaybackOptions(urls[0], (url) => { urls[0] = url; SelectedDetailsVideo.Play(urls); });
                        }
                    }
                },
                Translation.Instance.GettingPlaybackUrlsForVideo);
        }

        public void StartSearch()
        {
            if (BackgroundTask.Instance.IsExecuting) return;
            BackgroundTask.Instance.Start<List<SearchResultItem>>(
                () =>
                {
                    return SelectedSite.Site.Search(SearchString);
                },
                (success, result) =>
                {
                    if (success && result != null && result.Count > 0)
                    {
                        ShowSearchResults(result, Translation.Instance.SearchResults + " [" + SearchString + "]");
                    }
                },
                Translation.Instance.GettingSearchResults);
        }

        public void PushNavigationToOnlineVideosRoot()
        {
            var settings = ServiceRegistration.Get<ISettingsManager>().Load<Configuration.Settings>();

            // when going to sites we need to rebuild the list of sites with all sites if not already done
            if (!settings.GroupSitesByLanguage && !sitesListHasAllSites)
            {
                BuildSitesList();
            }

            ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(settings.GroupSitesByLanguage ? Guids.WorkflowStateSiteGroups : Guids.WorkflowStateSites);
        }

        void RebuildSitesList()
        {
            BuildSitesList();
            BuildAutomaticSitesGroups();
        }

        void BuildSitesList()
        {
            SitesList.Clear();
            foreach (var site in OnlineVideoSettings.Instance.SiteUtilsList)
            {
                if (site.Value.Settings.IsEnabled &&
                    (!site.Value.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                {
                    SitesList.Add(new SiteViewModel(site.Value));
                }
            }
            sitesListHasAllSites = true;
            SitesList.FireChange();
        }

        void BuildAutomaticSitesGroups()
        {
            SiteGroupsList.Clear();
            Dictionary<string, List<string>> sitenames = new Dictionary<string, List<string>>();
            var siteutils = OnlineVideoSettings.Instance.SiteUtilsList;
            foreach (string name in siteutils.Keys)
            {
                Sites.SiteUtilBase site;
                if (siteutils.TryGetValue(name, out site))
                {
                    if (site.Settings.IsEnabled &&
                    (!site.Settings.ConfirmAge || !OnlineVideoSettings.Instance.UseAgeConfirmation || OnlineVideoSettings.Instance.AgeConfirmed))
                    {
                        string key = string.IsNullOrEmpty(site.Settings.Language) ? "--" : site.Settings.Language;
                        List<string> listForLang = null;
                        if (!sitenames.TryGetValue(key, out listForLang)) { listForLang = new List<string>(); sitenames.Add(key, listForLang); }
                        listForLang.Add(site.Settings.Name);
                    }
                }
            }
            foreach (string aLang in sitenames.Keys.ToList().OrderBy(l => l))
            {
                SiteGroupsList.Add(new SiteGroupViewModel(LanguageCodeLocalizedConverter.GetLanguageInUserLocale(aLang),
                    string.Format(@"LanguageFlagsBig\{0}.png", aLang),
                    sitenames[aLang]));
            }
            SiteGroupsList.FireChange();
        }

        void ShowCategories(IList<Category> categories, string navigationLabel)
        {
            CategoriesList = new ItemsList();
            foreach (Category c in categories) CategoriesList.Add(new CategoryViewModel(c));
            ImageDownloader.GetImages<Category>(categories);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateCategories, new NavigationContextConfig() { NavigationContextDisplayLabel = navigationLabel });
        }

        void ShowVideos(CategoryViewModel category, List<VideoInfo> videos)
        {
            SelectedCategory = category;
            VideosList = new ItemsList();
            videos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoViewModel(r, SelectedCategory != null ? SelectedCategory.Category : null, SelectedSite.Site.Settings, false)); });

            if (SelectedSite.Site.HasNextPage) VideosList.Add(new VideoViewModel(Translation.Instance.NextPage, "NextPage.png"));

            ImageDownloader.GetImages<VideoInfo>(videos);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateVideos, new NavigationContextConfig() { NavigationContextDisplayLabel = SelectedCategory.Name });
        }

        void ShowDetails(List<DetailVideoInfo> choices)
        {
            DetailsVideosList = new List<VideoViewModel>();
            choices.ForEach(r => { r.CleanDescriptionAndTitle(); DetailsVideosList.Add(new VideoViewModel(r, SelectedCategory != null ? SelectedCategory.Category : null, SelectedSite.Site.Settings, true)); });
            ImageDownloader.GetImages<DetailVideoInfo>(choices);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateDetails, new NavigationContextConfig() { NavigationContextDisplayLabel = SelectedVideo.Title });
        }

        internal void ShowSearchResults(List<SearchResultItem> result, string title)
        {
            // pop all states up to the site state from the current navigation stack
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            while (workflowManager.NavigationContextStack.Peek().Predecessor != null && 
                workflowManager.NavigationContextStack.Peek().Predecessor.WorkflowState.StateId != Guids.WorkflowStateSites)
            {
                workflowManager.NavigationContextStack.Pop();
            }
            // create a "fake" Category as Parent for a results items
            var searchCategory = new CategoryViewModel(
                new Category()
                {
                    Name = title,
                    HasSubCategories = result[0] is Category,
                    SubCategoriesDiscovered = true
                });
            // display results
            if (result[0] is VideoInfo)
            {
                ShowVideos(searchCategory, result.ConvertAll(i => i as VideoInfo));
            }
            else
            {
                searchCategory.Category.SubCategories = result.ConvertAll(i => { (i as Category).ParentCategory = searchCategory.Category; return i as Category; });
                SelectedCategory = searchCategory;
                ShowCategories(searchCategory.Category.SubCategories, searchCategory.Name);
            }
        }

        void OnlineVideosMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
        {
            if (message.ChannelName == OnlineVideosMessaging.CHANNEL)
            {
                OnlineVideosMessaging.MessageType messageType = (OnlineVideosMessaging.MessageType)message.MessageType;
                switch (messageType)
                {
                    case OnlineVideosMessaging.MessageType.RebuildSites:
                        SiteGroupsList.Clear();
                        SitesList.Clear();
                        sitesListHasAllSites = false;
                        break;
                }
            }
        }

        #region IWorkflowModel implementation

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            // can only enter a new state when not running any background work
            return !BackgroundTask.Instance.IsExecuting;
        }

        public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
        {
            // reload a site when going away from configuring it and settings were changed
            if (oldContext.WorkflowState.StateId == Guids.WorkflowStateSiteSettings && FocusedSite.UserSettingsChanged)
            {
                FocusedSite.RecreateSite();
                // save Site Settings
                (OnlineVideoSettings.Instance.UserStore as Configuration.UserSiteSettingsStore).SaveAll();
            }

            // going to sites view
            if (newContext.WorkflowState.StateId == Guids.WorkflowStateSites)
            {
                SelectedCategory = null;
                CategoriesList = null;
            }
            // going from categories to categories view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateCategories && oldContext.WorkflowState.StateId == Guids.WorkflowStateCategories)
            {
                // going up in hierarchy
                if (oldContext.Predecessor == newContext)
                {
                    CategoriesList = new ItemsList();
                    if (SelectedCategory != null && SelectedCategory.Category.ParentCategory != null)
                    {
                        SelectedCategory.Category.ParentCategory.SubCategories.ForEach(c => CategoriesList.Add(new CategoryViewModel(c)));
                        ImageDownloader.GetImages<Category>(SelectedCategory.Category.ParentCategory.SubCategories);
                        SelectedCategory = new CategoryViewModel(SelectedCategory.Category.ParentCategory);
                    }
                    else
                    {
                        foreach (Category c in SelectedSite.Site.Settings.Categories) CategoriesList.Add(new CategoryViewModel(c));
                        ImageDownloader.GetImages<Category>(SelectedSite.Site.Settings.Categories);
                        SelectedCategory = null;
                    }
                }
            }
            // going from videos to categories view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateCategories && oldContext.WorkflowState.StateId == Guids.WorkflowStateVideos)
            {
                VideosList = null;
                CategoriesList = new ItemsList();

                if (SelectedCategory != null && SelectedCategory.Category.ParentCategory != null)
                {
                    SelectedCategory.Category.ParentCategory.SubCategories.ForEach(c => CategoriesList.Add(new CategoryViewModel(c)));
                    ImageDownloader.GetImages<Category>(SelectedCategory.Category.ParentCategory.SubCategories);
                }
                else
                {
                    foreach (Category c in SelectedSite.Site.Settings.Categories) CategoriesList.Add(new CategoryViewModel(c));
                    ImageDownloader.GetImages<Category>(SelectedSite.Site.Settings.Categories);
                }

                if (SelectedCategory != null && SelectedCategory.Category.ParentCategory != null)
                    SelectedCategory = new CategoryViewModel(SelectedCategory.Category.ParentCategory);
                else
                    SelectedCategory = null;
            }
            // going from details to videos view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateVideos && oldContext.WorkflowState.StateId == Guids.WorkflowStateDetails)
            {
                DetailsVideosList = null;
            }
        }

        public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
        {
        }

        public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
        {
        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            // when no siteutils have been loaded yet - background automatic update is still running
            if (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
            {
                // show the busy indicator until sites are loaded
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                while (!OnlineVideoSettings.Instance.IsSiteUtilsListBuilt())
                    System.Threading.Thread.Sleep(50);
                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
            }

            if (SiteGroupsList.Count == 0 && SitesList.Count == 0)
                RebuildSitesList();
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
        }

        public Guid ModelId
        {
            get { return Guids.WorkFlowModelOV; }
        }

        public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
        {
        }

        public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }

        #endregion
    }

}