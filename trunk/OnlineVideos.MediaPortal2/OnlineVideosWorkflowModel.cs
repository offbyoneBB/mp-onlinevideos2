using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using OnlineVideos.Sites;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosWorkflowModel : IWorkflowModel
    {
        public OnlineVideosWorkflowModel()
        {
            Configuration.SettingsBag settings = ServiceRegistration.Get<ISettingsManager>().Load<Configuration.SettingsBag>();

            OnlineVideoSettings.Instance.Logger = new LogDelegator();

            OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Team MediaPortal\MediaPortal\plugins\Windows\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.DllsDir)) OnlineVideoSettings.Instance.DllsDir = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);

            OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\thumbs\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ThumbsDir)) OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location), "Thumbs");

            OnlineVideoSettings.Instance.ConfigDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ConfigDir)) OnlineVideoSettings.Instance.ConfigDir = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);

            OnlineVideoSettings.Instance.UseAgeConfirmation = settings.UseAgeConfirmation;
            OnlineVideoSettings.Instance.CacheTimeout = settings.CacheTimeout;
            OnlineVideoSettings.Instance.UtilTimeout = settings.UtilTimeout;

            OnlineVideoSettings.Instance.LoadSites();
            OnlineVideoSettings.Instance.BuildSiteUtilsList();
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asf")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asf", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".flv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".flv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".m4v")) OnlineVideoSettings.Instance.VideoExtensions.Add(".m4v", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mov")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mov", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mkv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mkv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mp4")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mp4", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".wmv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".wmv", false);

            SitesList = new List<SiteViewModel>();

            OnlineVideoSettings.Instance.SiteUtilsList.Values.ToList().ForEach(s => SitesList.Add(new SiteViewModel(s) { HasFocus = SitesList.Count == 0 }));

            // add a special reversed proxy handler for rtmp
            ReverseProxy.Instance.AddHandler(RTMP_LIB.RTMPRequestHandler.Instance); 
        }

        protected AbstractProperty _searchStringProperty = new WProperty(typeof(string), string.Empty);
        public AbstractProperty SearchStringProperty { get { return _searchStringProperty; } }
        public string SearchString
        {
            get { return (string)_searchStringProperty.GetValue(); }
            set { _searchStringProperty.SetValue(value); }
        }

        public List<SiteViewModel> SitesList { get; protected set; }
        public SiteUtilBase SelectedSite { get; protected set; }
        public Category SelectedCategory { get; protected set; }
        public List<CategoryViewModel> CategoriesList { get; protected set; }
        public ItemsList VideosList { get; protected set; }
        public List<VideoInfoViewModel> DetailsVideosList { get; protected set; }
        public VideoInfoViewModel SelectedVideo { get; protected set; }
        public Dictionary<string, string> PlaybackOptions { get; protected set; }
        
        public bool IsExecutingBackgroundTask { get { return currentBackgroundTask != null; } }
        IWork currentBackgroundTask = null;

        public void SelectSite(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            SiteViewModel sub = selectedItem as SiteViewModel;
            if (sub != null)
            {
                if (!sub.Settings.DynamicCategoriesDiscovered)
                {
                    ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                    currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                        =>
                        {
                            try
                            {
                                sub.Site.DiscoverDynamicCategories();
                            }
                            catch (Exception ex)
                            {
                                currentBackgroundTask.Exception = ex;
                            }
                        },
                        (args) =>
                        {
                            ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                            SelectedSite = sub.Site;
                            ShowCategories(sub.Settings.Categories, SelectedSite.Settings.Name);
                            currentBackgroundTask = null;
                        });
                }
                else
                {
                    SelectedSite = sub.Site;
                    ShowCategories(sub.Settings.Categories, SelectedSite.Settings.Name);
                }
            }
        }

        public void SelectCategory(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            CategoryViewModel catProxy = selectedItem as CategoryViewModel;
            if (catProxy != null)
            {
                Category cat = catProxy.Category;
                if (cat.HasSubCategories)
                {
                    if (!cat.SubCategoriesDiscovered)
                    {
                        ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                        currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                            =>
                            {
                                try
                                {
                                    SelectedSite.DiscoverSubCategories(cat);
                                }
                                catch (Exception ex)
                                {
                                    currentBackgroundTask.Exception = ex;
                                }
                            },
                            (args) =>
                            {
                                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                                SelectedCategory = cat;
                                ShowCategories(cat.SubCategories, cat.Name);
                                currentBackgroundTask = null;
                            });
                    }
                    else
                    {
                        SelectedCategory = cat;
                        ShowCategories(cat.SubCategories, cat.Name);
                    }
                }
                else
                {
                    List<VideoInfo> videos = null;
                    ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                    currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                        =>
                        {
                            try
                            {
                                videos = SelectedSite.getVideoList(cat);
                            }
                            catch (Exception ex)
                            {
                                currentBackgroundTask.Exception = ex;
                            }
                        },
                        (args) =>
                        {
                            ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                            ShowVideos(cat, videos);
                            currentBackgroundTask = null;
                        });
                }
            }
        }

        public void SelectVideo(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            SelectedVideo = selectedItem as VideoInfoViewModel;
            if (SelectedVideo != null)
            {
                if (SelectedVideo.VideoInfo == null)
                {
                    // append next page videos
                    List<VideoInfo> nextPageVideos = null;
                    ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                    currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                        =>
                        {
                            try
                            {
                                nextPageVideos = SelectedSite.getNextPageVideos();
                            }
                            catch (Exception ex)
                            {
                                currentBackgroundTask.Exception = ex;
                            }
                        },
                        (args) =>
                        {
                            ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                            currentBackgroundTask = null;
                            VideosList.Remove(SelectedVideo);
                            int selectNr = VideosList.Count;
                            foreach (VideoInfoViewModel video in VideosList) video.HasFocus = false;
                            nextPageVideos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoInfoViewModel(r) { HasFocus = VideosList.Count == selectNr }); });
                            if (SelectedSite.HasNextPage) VideosList.Add(new VideoInfoViewModel(Translation.Instance.NextPage, "NextPage.png"));
                            VideosList.FireChange();
                            ImageDownloader.GetImages<VideoInfo>(nextPageVideos);
                        });
                }
                else
                {
                    if (SelectedSite is IChoice && SelectedVideo.VideoInfo.HasDetails)
                    {
                        // show details view
                        List<VideoInfo> choices = null;
                        ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                        currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                            =>
                            {
                                try
                                {
                                    choices = ((IChoice)SelectedSite).getVideoChoices(SelectedVideo.VideoInfo);
                                }
                                catch (Exception ex)
                                {
                                    currentBackgroundTask.Exception = ex;
                                }
                            },
                            (args) =>
                            {
                                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                                currentBackgroundTask = null;
                                ShowDetails(choices);
                            });
                    }
                    else
                    {
                        List<string> urls = null;
                        ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                        currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                            =>
                            {
                                try
                                {
                                    urls = SelectedSite.getMultipleVideoUrls(SelectedVideo.VideoInfo);
                                }
                                catch (Exception ex)
                                {
                                    currentBackgroundTask.Exception = ex;
                                }
                            },
                            (args) =>
                            {
                                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                                currentBackgroundTask = null;

                                if (SelectedVideo.VideoInfo.PlaybackOptions != null && SelectedVideo.VideoInfo.PlaybackOptions.Count > 1)
                                {
                                    ShowPlaybackOptions(SelectedVideo.VideoInfo);
                                }
                                else
                                {
                                    Play(SelectedVideo, urls);
                                }
                            });
                    }
                }
            }
        }

        public void SelectDetailsVideo(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            VideoInfoViewModel detailsVideo = selectedItem as VideoInfoViewModel;
            if (detailsVideo != null)
            {
                List<string> urls = null;
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                    =>
                {
                    try
                    {
                        urls = SelectedSite.getMultipleVideoUrls(detailsVideo.VideoInfo);
                    }
                    catch (Exception ex)
                    {
                        currentBackgroundTask.Exception = ex;
                    }
                },
                (args) =>
                {
                    ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                    currentBackgroundTask = null;

                    if (detailsVideo.VideoInfo.PlaybackOptions != null && detailsVideo.VideoInfo.PlaybackOptions.Count > 1)
                    {
                        ShowPlaybackOptions(detailsVideo.VideoInfo);
                    }
                    else
                    {
                        Play(detailsVideo, urls);
                    }
                });
            }
        }

        public void SelectedPlaybackOption(string selectedItem)
        {
            Play(SelectedVideo, PlaybackOptions[selectedItem]);
        }

        public void StartSearch()
        {
            List<ISearchResultItem> result = null;
            ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
            currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                =>
            {
                try
                {
                    result = SelectedSite.DoSearch(SearchString);
                }
                catch (Exception ex)
                {
                    currentBackgroundTask.Exception = ex;
                }
            },
                (args) =>
                {
                    ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                    if (result != null && result.Count > 0)
                    {
                        if (result[0] is VideoInfo) ShowVideos(null, result.ConvertAll(i => i as VideoInfo));
                        else
                        {
                            Category searchCategory = new Category()
                            {
                                Name = Translation.Instance.SearchResults + " [" + SearchString + "]",
                                HasSubCategories = true,
                                SubCategoriesDiscovered = true,
                            };
                            searchCategory.SubCategories = result.ConvertAll(i => { (i as Category).ParentCategory = searchCategory; return i as Category; });
                            ShowCategories(searchCategory.SubCategories, searchCategory.Name);
                        }
                    }
                    currentBackgroundTask = null;
                });
        }

        void ShowPlaybackOptions(VideoInfo videoInfo)
        {
            PlaybackOptions = videoInfo.PlaybackOptions;
            ServiceRegistration.Get<IScreenManager>().ShowDialog("dialogPlaybackOptions");
        }

        void Play(VideoInfoViewModel videoInfo, List<string> urls)
        {
            // todo : if more than one url, playlist
            Play(videoInfo, urls[0]);
        }

        void Play(VideoInfoViewModel videoInfo, string url)
        {
            IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();

            MediaItemAspect providerResourceAspect;
            aspects[ProviderResourceAspect.ASPECT_ID] = providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
            MediaItemAspect mediaAspect;
            aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
            MediaItemAspect videoAspect;
            aspects[VideoAspect.ASPECT_ID] = videoAspect = new MediaItemAspect(VideoAspect.Metadata);

            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlMediaProvider.ToProviderResourcePath(url).Serialize());
            providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

            mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, OnlineVideosPlayer.ONLINEVIDEOS_MIMETYPE);
            mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, videoInfo.Title);
            videoAspect.SetAttribute(VideoAspect.ATTR_STORYPLOT, videoInfo.Description);
            DateTime parsedAirDate;
            if (DateTime.TryParse(videoInfo.VideoInfo.Airdate, out parsedAirDate)) mediaAspect.SetAttribute(MediaAspect.ATTR_RECORDINGTIME, parsedAirDate);

            MediaItem mediaItem = new MediaItem(Guid.Empty, aspects);

            MediaPortal.UiComponents.Media.Models.PlayItemsModel.PlayItem(mediaItem);
        }
        
        void ShowCategories(IList<Category> categories, string navigationLabel)
        {
            CategoriesList = new List<CategoryViewModel>();
            foreach (Category c in categories) CategoriesList.Add(new CategoryViewModel(c) { HasFocus = CategoriesList.Count == 0 });
            ImageDownloader.GetImages<Category>(categories);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateCategories, new NavigationContextConfig() { NavigationContextDisplayLabel = navigationLabel });
        }

        void ShowVideos(Category category, List<VideoInfo> videos)
        {
            SelectedCategory = category;
            VideosList = new ItemsList();
            videos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoInfoViewModel(r) { HasFocus = VideosList.Count == 0 }); });

            if (SelectedSite.HasNextPage) VideosList.Add(new VideoInfoViewModel(Translation.Instance.NextPage, "NextPage.png"));

            ImageDownloader.GetImages<VideoInfo>(videos);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            if (category == null)
            {
                // if previously in videos state - pop that state off the stack
                if (workflowManager.NavigationContextStack.Peek().WorkflowState.StateId == Guids.WorkflowStateVideos)
                {
                    workflowManager.NavigationContextStack.Pop();
                }
                workflowManager.NavigatePushAsync(Guids.WorkflowStateVideos, new NavigationContextConfig() { NavigationContextDisplayLabel = string.Format("Search [{0}]", SearchString) });
            }
            else
                workflowManager.NavigatePushAsync(Guids.WorkflowStateVideos, new NavigationContextConfig() { NavigationContextDisplayLabel = SelectedCategory.Name });
        }

        void ShowDetails(List<VideoInfo> choices)
        {
            DetailsVideosList = new List<VideoInfoViewModel>();
            choices.ForEach(r => { r.CleanDescriptionAndTitle(); DetailsVideosList.Add(new VideoInfoViewModel(r)); });
            ImageDownloader.GetImages<VideoInfo>(choices);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateDetails, new NavigationContextConfig() { NavigationContextDisplayLabel = SelectedVideo.Title });
        }

        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return currentBackgroundTask == null; // only can enter a new state when not doing any background work
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
        {
            // going to sites view
            if (newContext.WorkflowState.StateId == Guids.WorkflowStateSites)
            {
                SelectedCategory = null;
                CategoriesList = null;
                SitesList.ForEach(s => s.HasFocus = s.Site == SelectedSite);
            }
            // going from categories to categories view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateCategories && oldContext.WorkflowState.StateId == Guids.WorkflowStateCategories)
            {
                // going up in hierarchy
                if (oldContext.Predecessor == newContext)
                {
                    CategoriesList = new List<CategoryViewModel>();
                    if (SelectedCategory.ParentCategory != null)
                    {
                        SelectedCategory.ParentCategory.SubCategories.ForEach(c => CategoriesList.Add(new CategoryViewModel(c) { HasFocus = c.Name == SelectedCategory.Name }));
                        ImageDownloader.GetImages<Category>(SelectedCategory.ParentCategory.SubCategories);
                    }
                    else
                    {
                        foreach (Category c in SelectedSite.Settings.Categories) CategoriesList.Add(new CategoryViewModel(c) { HasFocus = c.Name == SelectedCategory.Name });
                        ImageDownloader.GetImages<Category>(SelectedSite.Settings.Categories);
                    }

                    SelectedCategory = SelectedCategory.ParentCategory;
                }
            }
            // going from videos to categories view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateCategories && oldContext.WorkflowState.StateId == Guids.WorkflowStateVideos)
            {
                VideosList = null;
                CategoriesList = new List<CategoryViewModel>();

                if (SelectedCategory != null && SelectedCategory.ParentCategory != null)
                {
                    SelectedCategory.ParentCategory.SubCategories.ForEach(c => CategoriesList.Add(new CategoryViewModel(c) { HasFocus = c.Name == SelectedCategory.Name }));
                    ImageDownloader.GetImages<Category>(SelectedCategory.ParentCategory.SubCategories);
                }
                else
                {
                    foreach (Category c in SelectedSite.Settings.Categories) CategoriesList.Add(new CategoryViewModel(c) { HasFocus = SelectedCategory != null && c.Name == SelectedCategory.Name });
                    ImageDownloader.GetImages<Category>(SelectedSite.Settings.Categories);
                }

                if (SelectedCategory != null) SelectedCategory = SelectedCategory.ParentCategory;
            }
            // going from details to videos view
            else if (newContext.WorkflowState.StateId == Guids.WorkflowStateVideos && oldContext.WorkflowState.StateId == Guids.WorkflowStateDetails)
            {
                DetailsVideosList = null;
                foreach (VideoInfoViewModel vip in VideosList) vip.HasFocus = vip == SelectedVideo;
            }
        }

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            //
        }

        public void EnterModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            //
        }

        public void ExitModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            //
        }

        public Guid ModelId
        {
            get { return Guids.WorkFlowModel; }
        }

        public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            //
        }

        public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
        {
            actions.Remove(new Guid("09eae702-d9ec-4325-82d9-4843502c966b")); // remove "Playlists" from menu, which seems to be always there
        }

        public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }
    }

}
