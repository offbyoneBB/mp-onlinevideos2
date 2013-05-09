using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace OnlineVideos.MediaPortal2
{
    public class OnlineVideosWorkflowModel : IWorkflowModel
    {
        public OnlineVideosWorkflowModel()
        {
			OnlineVideosAppDomain.UseSeperateDomain = true;

            Configuration.Settings settings = ServiceRegistration.Get<ISettingsManager>().Load<Configuration.Settings>();
            string ovConfigPath = ServiceRegistration.Get<IPathManager>().GetPath(string.Format(@"<CONFIG>\{0}\", Environment.UserName));
            string ovDataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OnlineVideos");

            OnlineVideoSettings.Instance.Logger = new LogDelegator();
			OnlineVideoSettings.Instance.UserStore = new Configuration.UserSiteSettingsStore();

			OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(ovDataPath, "SiteUtils");
            OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(ovDataPath, "Thumbs");
            OnlineVideoSettings.Instance.ConfigDir = ovConfigPath;

            OnlineVideoSettings.Instance.UseAgeConfirmation = settings.UseAgeConfirmation;
            OnlineVideoSettings.Instance.CacheTimeout = settings.CacheTimeout;
            OnlineVideoSettings.Instance.UtilTimeout = settings.UtilTimeout;

            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asf")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asf", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asx")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asx", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".flv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".flv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".m4v")) OnlineVideoSettings.Instance.VideoExtensions.Add(".m4v", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mov")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mov", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mkv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mkv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mp4")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mp4", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".wmv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".wmv", false);

			// clear cache files that might be left over from an application crash
			MPUrlSourceFilter.MPUrlSourceFilterDownloader.ClearDownloadCache();
			// load translation strings in other AppDomain, so SiteUtils can use localized language strings
			TranslationLoader.LoadTranslations(ServiceRegistration.Get<ILocalization>().CurrentCulture.TwoLetterISOLanguageName, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GetType().Assembly.Location), "Language"), "en", "strings_{0}.xml");
			// The default connection limit is 2 in .Net on most platforms! This means downloading two files will block all other WebRequests.
			System.Net.ServicePointManager.DefaultConnectionLimit = 100;
			// The default .Net implementation for URI parsing removes trailing dots, which is not correct
			Utils.FixUriTrailingDots();

			OnlineVideoSettings.Instance.LoadSites();
			OnlineVideoSettings.Instance.BuildSiteUtilsList();

            SitesList = new ItemsList();
			RebuildSitesList();
        }

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

		public ItemsList SitesList { get; protected set; }
		public ItemsList CategoriesList { get; protected set; }
        public ItemsList VideosList { get; protected set; }
        public List<VideoViewModel> DetailsVideosList { get; protected set; }

        /// <summary>The MP2 simple dialog requires Items to be of type <see cref="ListItem"/> and have a Name to show a label in the GUI.</summary>
        public ItemsList PlaybackOptions { get; protected set; }
        
        public bool IsExecutingBackgroundTask { get { return currentBackgroundTask != null; } }
        IWork currentBackgroundTask = null;

		public void RebuildSitesList()
		{
			SitesList.Clear();
			OnlineVideoSettings.Instance.SiteUtilsList.Values.ToList().ForEach(s => SitesList.Add(new SiteViewModel(s)));
		}

        public void SelectSite(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            SiteViewModel siteModel = selectedItem as SiteViewModel;
            if (siteModel != null)
            {
                if (!siteModel.Site.Settings.DynamicCategoriesDiscovered)
                {
                    ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                    currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                        =>
                        {
                            try
                            {
                                siteModel.Site.DiscoverDynamicCategories();
                            }
                            catch (Exception ex)
                            {
                                currentBackgroundTask.Exception = ex;
                            }
                        },
                        (args) =>
                        {
                            ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
                            SelectedSite = siteModel;
                            ShowCategories(siteModel.Site.Settings.Categories, SelectedSite.Name);
                            currentBackgroundTask = null;
                        });
                }
                else
                {
                    SelectedSite = siteModel;
                    ShowCategories(siteModel.Site.Settings.Categories, SelectedSite.Name);
                }
            }
        }

        public void SelectCategory(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            CategoryViewModel categoryModel = selectedItem as CategoryViewModel;
            if (categoryModel != null)
            {
				if (categoryModel.Category is NextPageCategory)
                {
                    // append next page categories
                    ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                    currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                    =>
                    {
                        try
                        {
							SelectedSite.Site.DiscoverNextPageCategories(categoryModel.Category as NextPageCategory);
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
                        int selectNr = CategoriesList.Count - 1;
                        CategoriesList.Clear();
						IList<Category> catList = categoryModel.Category.ParentCategory == null ? (IList<Category>)SelectedSite.Site.Settings.Categories : categoryModel.Category.ParentCategory.SubCategories;
						foreach (Category c in catList) CategoriesList.Add(new CategoryViewModel(c) { Selected = CategoriesList.Count == selectNr });
                        ImageDownloader.GetImages<Category>(catList);
                        CategoriesList.FireChange();
                    });
                }
                else
                {
					if (categoryModel.Category.HasSubCategories)
                    {
						if (!categoryModel.Category.SubCategoriesDiscovered)
                        {
                            ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                            currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                                =>
                                {
                                    try
                                    {
										SelectedSite.Site.DiscoverSubCategories(categoryModel.Category);
                                    }
                                    catch (Exception ex)
                                    {
                                        currentBackgroundTask.Exception = ex;
                                    }
                                },
                                (args) =>
                                {
                                    ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
									SelectedCategory = categoryModel;
									ShowCategories(categoryModel.Category.SubCategories, categoryModel.Name);
                                    currentBackgroundTask = null;
                                });
                        }
                        else
                        {
							SelectedCategory = categoryModel;
							ShowCategories(categoryModel.Category.SubCategories, categoryModel.Name);
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
									videos = SelectedSite.Site.getVideoList(categoryModel.Category);
                                }
                                catch (Exception ex)
                                {
                                    currentBackgroundTask.Exception = ex;
                                }
                            },
                            (args) =>
                            {
                                ServiceRegistration.Get<ISuperLayerManager>().HideBusyScreen();
								ShowVideos(categoryModel, videos);
                                currentBackgroundTask = null;
                            });
                    }
                }
            }
        }

        public void SelectVideo(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            SelectedVideo = selectedItem as VideoViewModel;
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
                                nextPageVideos = SelectedSite.Site.getNextPageVideos();
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
							nextPageVideos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoViewModel(r) { Selected = VideosList.Count == selectNr }); });
                            if (SelectedSite.Site.HasNextPage) VideosList.Add(new VideoViewModel(Translation.Instance.NextPage, "NextPage.png"));
                            VideosList.FireChange();
                            ImageDownloader.GetImages<VideoInfo>(nextPageVideos);
                        });
                }
                else
                {
                    if (SelectedSite.Site is IChoice && SelectedVideo.VideoInfo.HasDetails)
                    {
                        // show details view
                        List<VideoInfo> choices = null;
                        ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                        currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                            =>
                            {
                                try
                                {
                                    choices = ((IChoice)SelectedSite.Site).getVideoChoices(SelectedVideo.VideoInfo);
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
                                    urls = SelectedSite.Site.getMultipleVideoUrls(SelectedVideo.VideoInfo);
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

								// when no PlaybackOptions are set, directly go to playing the result
								if (SelectedVideo.VideoInfo.PlaybackOptions == null || SelectedVideo.VideoInfo.PlaybackOptions.Count == 0)
								{
									Play(SelectedVideo, urls);
								}
								else
								{
									ShowPlaybackOptions(SelectedVideo, urls);
								}
							});
                    }
                }
            }
        }

        public void SelectDetailsVideo(object selectedItem)
        {
            if (currentBackgroundTask != null) return;
            SelectedDetailsVideo = selectedItem as VideoViewModel;
			if (SelectedDetailsVideo != null)
            {
                List<string> urls = null;
                ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
                currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
                    =>
                {
                    try
                    {
						urls = SelectedSite.Site.getMultipleVideoUrls(SelectedDetailsVideo.VideoInfo);
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

					if (SelectedDetailsVideo.VideoInfo.PlaybackOptions != null && SelectedDetailsVideo.VideoInfo.PlaybackOptions.Count > 1)
                    {
						ShowPlaybackOptions(SelectedDetailsVideo, urls);
                    }
                    else
                    {
						Play(SelectedDetailsVideo, urls);
                    }
                });
            }
        }

        public void SelectPlaybackOption(ListItem selectedItem)
        {
			IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
			var video = (workflowManager.CurrentNavigationContext.WorkflowState.StateId == Guids.WorkflowStateVideos) ? SelectedVideo : SelectedDetailsVideo;
            // resolve playback option
			string resolvedUrl = null;
			ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
			currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
				=>
			{
				try
				{
					resolvedUrl = video.VideoInfo.GetPlaybackOptionUrl(((KeyValuePair<string, string>)selectedItem.AdditionalProperties[Consts.KEY_MEDIA_ITEM]).Key);
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
				var urls = (List<string>)selectedItem.AdditionalProperties[Constants.KEY_PLAYBACK_URLS];
				urls[(int)selectedItem.AdditionalProperties[Consts.KEY_INDEX]] = resolvedUrl;
				Play(video, urls);
			});
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
                    result = SelectedSite.Site.DoSearch(SearchString);
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
                        // pop all states up to the site state from the current navigation stack
                        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                        while (!(workflowManager.NavigationContextStack.Peek().WorkflowState.Name == Guids.WorkflowStateCategoriesName &&
                            workflowManager.NavigationContextStack.Peek().WorkflowState.DisplayLabel == SelectedSite.Name))
                        {
                            workflowManager.NavigationContextStack.Pop();
                        }
                        // dislay results
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

        void Play(VideoViewModel videoInfo, List<string> urls)
        {
			if (urls.Count == 1)
				MediaPortal.UiComponents.Media.Models.PlayItemsModel.PlayItem(new PlaylistItem(videoInfo, urls[0]));
			else
				MediaPortal.UiComponents.Media.Models.PlayItemsModel.PlayItems(
					new MediaPortal.UiComponents.Media.Models.GetMediaItemsDlgt(() => 
					{
						return new List<MediaItem>(urls.ConvertAll<MediaItem>(u => new PlaylistItem(videoInfo, u)));
					}), 
					MediaPortal.UI.Presentation.Players.AVType.Video);
        }

        void ShowCategories(IList<Category> categories, string navigationLabel)
        {
            CategoriesList = new ItemsList();
			foreach (Category c in categories) CategoriesList.Add(new CategoryViewModel(c));
            ImageDownloader.GetImages<Category>(categories);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
			workflowManager.NavigatePushTransientAsync(
				WorkflowState.CreateTransientState(Guids.WorkflowStateCategoriesName, navigationLabel, false, "categories", false, WorkflowType.Workflow), 
				new NavigationContextConfig());
        }

        void ShowVideos(CategoryViewModel category, List<VideoInfo> videos)
        {
            SelectedCategory = category;
            VideosList = new ItemsList();
			videos.ForEach(r => { r.CleanDescriptionAndTitle(); VideosList.Add(new VideoViewModel(r)); });

            if (SelectedSite.Site.HasNextPage) VideosList.Add(new VideoViewModel(Translation.Instance.NextPage, "NextPage.png"));

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
            DetailsVideosList = new List<VideoViewModel>();
            choices.ForEach(r => { r.CleanDescriptionAndTitle(); DetailsVideosList.Add(new VideoViewModel(r)); });
            ImageDownloader.GetImages<VideoInfo>(choices);
            IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
            workflowManager.NavigatePushAsync(Guids.WorkflowStateDetails, new NavigationContextConfig() { NavigationContextDisplayLabel = SelectedVideo.Title });
        }

		void ShowPlaybackOptions(VideoViewModel video, List<string> urls)
		{
			// if just one option set, resolve it and play that one
			if (video.VideoInfo.PlaybackOptions.Count == 1)
			{
				ServiceRegistration.Get<ISuperLayerManager>().ShowBusyScreen();
				currentBackgroundTask = ServiceRegistration.Get<IThreadPool>().Add(()
					=>
				{
					try
					{
						urls[0] = video.VideoInfo.GetPlaybackOptionUrl(video.VideoInfo.PlaybackOptions.First().Key);
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
					Play(video, urls);
				});
			}
			else
			{
				string defaultUrl = urls[0];
				PlaybackOptions = new ItemsList();
				foreach (var item in video.VideoInfo.PlaybackOptions)
				{
					var listItem = new ListItem(Consts.KEY_NAME, item.Key);
					listItem.AdditionalProperties.Add(Consts.KEY_MEDIA_ITEM, item);
					listItem.AdditionalProperties.Add(Consts.KEY_INDEX, 0);
					listItem.AdditionalProperties.Add(Constants.KEY_PLAYBACK_URLS, urls);
					listItem.Selected = item.Value == defaultUrl;
					PlaybackOptions.Add(listItem);
				}
				ServiceRegistration.Get<IScreenManager>().ShowDialog("dialogPlaybackOptions");
			}
		}

        public bool CanEnterState(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            return currentBackgroundTask == null; // only can enter a new state when not doing any background work
        }

        public void ChangeModelContext(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext, bool push)
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
			else if (newContext.WorkflowState.Name == Guids.WorkflowStateCategoriesName && oldContext.WorkflowState.Name == Guids.WorkflowStateCategoriesName)
            {
                // going up in hierarchy
                if (oldContext.Predecessor == newContext)
                {
                    CategoriesList = new ItemsList();
                    if (SelectedCategory.Category.ParentCategory != null)
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
			else if (newContext.WorkflowState.Name == Guids.WorkflowStateCategoriesName && oldContext.WorkflowState.StateId == Guids.WorkflowStateVideos)
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

        public void Deactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
        {
            //
        }

		public void Reactivate(MediaPortal.UI.Presentation.Workflow.NavigationContext oldContext, MediaPortal.UI.Presentation.Workflow.NavigationContext newContext)
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
            get { return Guids.WorkFlowModelOV; }
        }

        public void UpdateMenuActions(MediaPortal.UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, MediaPortal.UI.Presentation.Workflow.WorkflowAction> actions)
        {
            //
        }

        public ScreenUpdateMode UpdateScreen(MediaPortal.UI.Presentation.Workflow.NavigationContext context, ref string screen)
        {
            return ScreenUpdateMode.AutoWorkflowManager;
        }
    }

}
