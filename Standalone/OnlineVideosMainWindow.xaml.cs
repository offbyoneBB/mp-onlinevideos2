using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OnlineVideos;
using OnlineVideos.Sites;
using OnlineVideos.MPUrlSourceFilter;
using OnlineVideos.Downloading;
using OnlineVideos.CrossDomain;
using OnlineVideos.Helpers;
using Standalone.Configuration;

namespace Standalone
{
    /// <summary>
    /// Interaktionslogik für OnlineVideosMainWindow.xaml
    /// </summary>
    public partial class OnlineVideosMainWindow : Window, INotifyPropertyChanged
    {
        readonly string[] videoExtensions = new string[] { ".asf", ".asx", ".flv", ".m4v", ".mkv", ".mov", ".mp4", ".wmv" };

        public event PropertyChangedEventHandler PropertyChanged;
        SiteUtilBase _SelectedSite;
        public SiteUtilBase SelectedSite { get { return _SelectedSite; } set { _SelectedSite = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedSite")); } }
        Category _SelectedCategory;
        public Category SelectedCategory { get { return _SelectedCategory; } set { _SelectedCategory = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedCategory")); } }
		PlayList _CurrentPlayList;
		public PlayList CurrentPlayList { get { return _CurrentPlayList; } set { _CurrentPlayList = value; PropertyChanged(this, new PropertyChangedEventArgs("CurrentPlayList")); } }
		PlayListItem _CurrentPlayListItem;
		public PlayListItem CurrentPlayListItem { get { return _CurrentPlayListItem; } set { _CurrentPlayListItem = value; PropertyChanged(this, new PropertyChangedEventArgs("CurrentPlayListItem")); } }
        bool _IsFullScreen = false;
        public bool IsFullScreen { get { return _IsFullScreen; } set { _IsFullScreen = value; PropertyChanged(this, new PropertyChangedEventArgs("IsFullScreen")); } }
        string _CurrentFilter = "";
        public string CurrentFilter { get { return _CurrentFilter; } set { _CurrentFilter = value; PropertyChanged(this, new PropertyChangedEventArgs("CurrentFilter")); } }

        public OnlineVideosMainWindow()
        {
            // default culture is en-us for all xaml, set the current ui culture, so it is used for all conversions
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentUICulture.IetfLanguageTag)));

			OnlineVideosAppDomain.UseSeperateDomain = true;

			// The default connection limit is 2 in .net on most platforms! This means downloading two files will block all other WebRequests.
			System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            // set and create folders at CommonApplicationData/OnlineVideos
            string writeableBaseDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OnlineVideos\\");
            if (!System.IO.Directory.Exists(writeableBaseDir)) System.IO.Directory.CreateDirectory(writeableBaseDir);
            Settings.Load(writeableBaseDir);
            OnlineVideoSettings.Instance.ConfigDir = writeableBaseDir;
            OnlineVideoSettings.Instance.Logger = new Logger(System.IO.Path.Combine(writeableBaseDir, "Logs"));
            OnlineVideoSettings.Instance.UserStore = new Configuration.UserSettings(writeableBaseDir);
            OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(writeableBaseDir, "SiteUtilDlls");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.DllsDir)) System.IO.Directory.CreateDirectory(OnlineVideoSettings.Instance.DllsDir);
			OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(writeableBaseDir, "Thumbs");
			if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ThumbsDir)) System.IO.Directory.CreateDirectory(OnlineVideoSettings.Instance.ThumbsDir);
			OnlineVideoSettings.Instance.DownloadDir = System.IO.Path.Combine(writeableBaseDir, "Downloads");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.DownloadDir)) System.IO.Directory.CreateDirectory(OnlineVideoSettings.Instance.DownloadDir);

			OnlineVideoSettings.Instance.AddSupportedVideoExtensions(videoExtensions);

			TranslationLoader.LoadTranslations(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages"));

			Gui2UtilConnector.Instance.TaskFinishedCallback += () => Dispatcher.Invoke((Action)Gui2UtilConnector.Instance.ExecuteTaskResultHandler);

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
                mediaPlayer.VideoRenderer = WPFMediaKit.DirectShow.MediaPlayers.VideoRendererType.VideoMixingRenderer9;

            new DispatcherTimer(
                TimeSpan.FromSeconds(0.5),
                DispatcherPriority.Normal,
                (o, ev) =>
                    {
                        if (mediaPlayer != null && mediaPlayer.Source != null)
                        {
                            // only set position/duration when playing
                            txtPlayPos.Text = mediaPlayer.HasVideo && (mediaPlayer.MediaPosition != 0 || mediaPlayer.MediaDuration != 0) ?
                            string.Format("{0} / {1}", new DateTime(mediaPlayer.MediaPosition).ToString("HH:mm:ss"), new DateTime(mediaPlayer.MediaDuration).ToString("HH:mm:ss")) : "";
                        }
                        else
                        {
                            txtPlayPos.Text = "";
                        }
                    },
                Dispatcher)
                .Start();

            OnlineVideoSettings.Instance.LoadSites();
			// force autoupdate when no dlls or icons or banners are found -> fresh install
			bool forceUpdate = System.IO.Directory.GetFiles(OnlineVideoSettings.Instance.DllsDir, "OnlineVideos.Sites.*.dll").Length == 0 || System.IO.Directory.GetFiles(OnlineVideoSettings.Instance.ThumbsDir, "*.png", System.IO.SearchOption.AllDirectories).Length == 0;
			if (forceUpdate || (DateTime.Now - Settings.Instance.LastAutoUpdate > TimeSpan.FromHours(1) && MessageBox.Show(Translation.Instance.PerformAutomaticUpdate, Translation.Instance.AutomaticUpdate, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes))
            {
				Title = "OnlineVideos - " + Translation.Instance.AutomaticUpdate + " ...";
                waitCursor.Visibility = System.Windows.Visibility.Visible;
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                    delegate()
                    {
						OnlineVideos.Sites.Updater.UpdateSites((string action, byte? percent) => 
						{
							if (percent != null || !string.IsNullOrEmpty(action))
								Dispatcher.Invoke((Action)(() => {
									if (!string.IsNullOrEmpty(action)) Title = string.Format("OnlineVideos - {0} ... {1}", Translation.Instance.AutomaticUpdate, action);
									if (percent != null) waitCursor.tbxCenter.Text = string.Format("{0}%", percent); }));
							return true; 
						});
                        return null;
                    },
                    delegate(Gui2UtilConnector.ResultInfo resultInfo)
                    {
						Settings.Instance.LastAutoUpdate = DateTime.Now;
                        Title = "OnlineVideos";
                        waitCursor.tbxCenter.Text = "";
                        waitCursor.Visibility = System.Windows.Visibility.Hidden;
                        ReactToResult(resultInfo, Translation.Instance.AutomaticUpdate);
                        OnlineVideoSettings.Instance.BuildSiteUtilsList();
						listViewMain.ItemsSource = ViewModels.SiteList.GetSitesView(this);
                        SelectAndFocusItem();
                    }, false);
            }
            else
            {
                OnlineVideoSettings.Instance.BuildSiteUtilsList();
				listViewMain.ItemsSource = ViewModels.SiteList.GetSitesView(this);
                SelectAndFocusItem();
            }
        }

        protected void HandleItemMouseEnter(object sender, MouseEventArgs e)
        {
            listViewMain.SelectedItem = (sender as ListViewItem).DataContext;
            (sender as ListViewItem).Focus();
        }

        protected void HandleItemKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnItemSelected(sender);
                e.Handled = true;
            }
            else if (e.Key == Key.F9)
            {
                OnItemContextMenuRequested(sender);
                e.Handled = true;
            }
            else
            {
                char c = Util.GetCharFromKey(e.Key);
                if (char.IsLetterOrDigit(c))
                {
                    FilterItems(c);
                    e.Handled = true;
                }
            }
        }

        internal void FilterItems(char newChar)
        {
            if (newChar == char.MinValue)
            {
                CurrentFilter = CurrentFilter.Substring(0, CurrentFilter.Length - 1);
            }
            else
            {
                CurrentFilter += newChar;
            }
            if (listViewMain.Visibility == System.Windows.Visibility.Visible)
            {
                var view = (listViewMain.ItemsSource as System.Windows.Data.ListCollectionView);
                if (view != null)
                {
                    view.Refresh();
                    SelectAndFocusItem();
                }
            }
            else
            {
                globalSitesView.RefreshList(null);
            }
        }

        protected void HandleItemRightClicked(object sender, MouseButtonEventArgs e)
        {
            OnItemContextMenuRequested(sender);
            e.Handled = true;
        }

        protected void HandleItemClicked(object sender, MouseButtonEventArgs e)
        {
            OnItemSelected(sender);
            e.Handled = true;
        }

        protected void OnItemSelected(object sender)
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // don't do anything if currently working on a background task

            CurrentFilter = "";

            object boundObject = ((ListViewItem)sender).Content;
            if (boundObject is ViewModels.Site)
            {
				SiteUtilBase site = ((ViewModels.Site)boundObject).Model;
				SiteSelected(site);
            }
            else if (boundObject is ViewModels.Category)
            {
				Category cat = ((ViewModels.Category)boundObject).Model;
				CategorySelected(cat);
            }
            else if (boundObject is ViewModels.Video)
            {
				VideoSelected((ViewModels.Video)boundObject);
            }
        }

        protected void OnItemContextMenuRequested(object sender)
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // don't do anything if currently working on a background task
            object boundObject = ((ListViewItem)sender).Content;
            if (boundObject is ViewModels.Video)
            {
                VideoInfo video = (boundObject as ViewModels.Video).Model;
                ShowContextMenuForVideo(video);
            }
        }

		void SiteSelected(SiteUtilBase site)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;
            Log.Info("Entering Site: '{0}'", site.Settings.Name);
			Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
				delegate()
				{
					if (!site.Settings.DynamicCategoriesDiscovered) site.DiscoverDynamicCategories();
					return null;
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.Instance.GettingDynamicCategories))
					{
						SelectedSite = site;
                        if (SelectedSite.Settings.Categories != null && SelectedSite.Settings.Categories.Count > 0 && SelectedSite.Settings.Categories[SelectedSite.Settings.Categories.Count - 1] is NextPageCategory)
                        {
                            SelectedSite.Settings.Categories[SelectedSite.Settings.Categories.Count - 1].ThumbnailImage = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png");
                        }
                        SelectedSite.Settings.Categories.RaiseListChangedEvents = false;
						listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, SelectedSite.Settings.Categories);
						SelectAndFocusItem();
                        ImageDownloader.GetImages<Category>(SelectedSite.Settings.Categories);
					}
				}
			);
		}

		void CategorySelected(Category category)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;
            Log.Info("Entering Category: '{0}'", category.Name);
            if (category is NextPageCategory)
            {
                int selectedIndex = listViewMain.Items.Count - 1;
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                    delegate()
                    {
                        return SelectedSite.DiscoverNextPageCategories(category as NextPageCategory);
                    },
                    delegate(Gui2UtilConnector.ResultInfo resultInfo)
                    {
                        waitCursor.Visibility = System.Windows.Visibility.Hidden;
                        if (ReactToResult(resultInfo, Translation.Instance.GettingNextPageVideos))
                        {
                            listViewMain.ItemsSource = null;
                            if (category.ParentCategory == null)
                            {
                                if (SelectedSite.Settings.Categories != null && SelectedSite.Settings.Categories.Count > 0 && SelectedSite.Settings.Categories[SelectedSite.Settings.Categories.Count - 1] is NextPageCategory)
                                {
                                    SelectedSite.Settings.Categories[SelectedSite.Settings.Categories.Count - 1].ThumbnailImage = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png");
                                }
								listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, SelectedSite.Settings.Categories);
                                ImageDownloader.GetImages<Category>(SelectedSite.Settings.Categories);
                            }
                            else
                            {
                                if (category.ParentCategory.SubCategories != null && category.ParentCategory.SubCategories.Count > 0 && category.ParentCategory.SubCategories[category.ParentCategory.SubCategories.Count - 1] is NextPageCategory)
                                {
                                    category.ParentCategory.SubCategories[category.ParentCategory.SubCategories.Count - 1].ThumbnailImage = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png");
                                }
								listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, category.ParentCategory.SubCategories);
                                ImageDownloader.GetImages<Category>(category.ParentCategory.SubCategories);
                            }
                            SelectAndFocusItem(selectedIndex);
                        }
                    }
                );
            }
			else if (category.HasSubCategories)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						if (!category.SubCategoriesDiscovered) SelectedSite.DiscoverSubCategories(category);
						return null;
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.Instance.GettingDynamicCategories))
						{
							SelectedCategory = category;
                            if (category.SubCategories != null && category.SubCategories.Count > 0 && category.SubCategories[category.SubCategories.Count - 1] is NextPageCategory)
                            {
                                category.SubCategories[category.SubCategories.Count - 1].ThumbnailImage = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png");
                            }
							listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, category.SubCategories);
                            ImageDownloader.GetImages<Category>(category.SubCategories);
                            SelectAndFocusItem();
						}
					}
				);
			}
			else
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return SelectedSite.GetVideos(category);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.Instance.GettingCategoryVideos))
						{
							SelectedCategory = category;
							List<VideoInfo> videoList = resultInfo.ResultObject as List<VideoInfo>;
							listViewMain.ItemsSource = ViewModels.VideoList.GetVideosView(this, videoList, SelectedSite.HasNextPage);
							SelectAndFocusItem();
							ImageDownloader.GetImages<VideoInfo>(videoList);
						}
					}
				);
			}
		}

		void VideoSelected(ViewModels.Video video)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;

			if (video.Name == Translation.Instance.NextPage)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return SelectedSite.GetNextPageVideos();
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.Instance.GettingNextPageVideos))
						{
							int indexToSelect = listViewMain.SelectedIndex;
							List<VideoInfo> videoList = resultInfo.ResultObject as List<VideoInfo>;
							ViewModels.VideoList.AppendNextPageVideos(listViewMain.ItemsSource, videoList, SelectedSite.HasNextPage);
							SelectAndFocusItem(indexToSelect);
							ImageDownloader.GetImages<VideoInfo>(videoList);
						}
					}
				);
			}
			else if (SelectedSite is IChoice && video.Model.HasDetails && detailsView.Visibility == System.Windows.Visibility.Hidden)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return ((IChoice)SelectedSite).GetVideoChoices(video.Model);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.Instance.GettingVideoDetails))
						{
							listViewMain.Visibility = System.Windows.Visibility.Hidden;
							detailsView.DataContext = video;

                            var extendedInfos = video.Model.GetExtendedProperties();
                            if (extendedInfos != null)
                            {
                                if (extendedInfos.ContainsKey("Plot")) detailsView.txtSynopsis.Text = extendedInfos["Plot"];
                                // todo : display all extended infos in details view
                            }
							detailsView.listViewTrailers.ItemsSource = ViewModels.VideoList.GetVideosView(this, resultInfo.ResultObject as List<DetailVideoInfo>, false, true);
							if (detailsView.listViewTrailers.Items.Count > 0)
							{
								detailsView.listViewTrailers.SelectedIndex = 0;
								Dispatcher.BeginInvoke((Action)(() =>
								{
									(detailsView.listViewTrailers.ItemContainerGenerator.ContainerFromIndex(detailsView.listViewTrailers.SelectedIndex) as ListBoxItem).Focus();
								}), DispatcherPriority.Input);
							}
							detailsView.Visibility = System.Windows.Visibility.Visible;
						}
					}
				);
			}
			else
			{
				Play_Step1(new PlayListItem(video.Model, SelectedSite is OnlineVideos.Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[(video.Model as FavoriteDbVideoInfo).SiteName] : SelectedSite), false);
			}
		}

        void ShowContextMenuForVideo(VideoInfo video)
        {
            List<KeyValuePair<string, ContextMenuEntry>> dialogOptions = new List<KeyValuePair<string, ContextMenuEntry>>();
            if (!(SelectedSite is DownloadedVideoUtil))
            {
                dialogOptions.Add(new KeyValuePair<string, ContextMenuEntry>(string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Concurrent), null));
                dialogOptions.Add(new KeyValuePair<string, ContextMenuEntry>(string.Format("{0} ({1})", Translation.Instance.Download, Translation.Instance.Queued), null));
            }
            foreach (var entry in SelectedSite.GetContextMenuEntries(SelectedCategory, video))
            {
                dialogOptions.Add(new KeyValuePair<string, ContextMenuEntry>(entry.DisplayText, entry));
            }
            if (dialogOptions.Count > 0)
            {
                PlaybackChoices dlg = new PlaybackChoices() { Owner = this };
                dlg.lblHeading.Content = string.Format("{0}: {1}", Translation.Instance.Actions, video.Title);
                dlg.lvChoices.ItemsSource = dialogOptions.Select(dO => dO.Key).ToList();
                dlg.lvChoices.SelectedIndex = 0;
                if (dlg.ShowDialog() == true)
                {
                    if (dialogOptions[dlg.lvChoices.SelectedIndex].Value == null)
                    {
                        if (dlg.lvChoices.SelectedItem.ToString().Contains(Translation.Instance.Concurrent))
                        {
                            SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(video, SelectedCategory, SelectedSite)));
                        }
                        else if (dlg.lvChoices.SelectedItem.ToString().Contains(Translation.Instance.Queued))
                        {
                            SaveVideo_Step1(DownloadList.Create(DownloadInfo.Create(video, SelectedCategory, SelectedSite)), true);
                        }
                    }
                    else
                        HandleCustomContextMenuEntry(dialogOptions[dlg.lvChoices.SelectedIndex].Value, SelectedCategory, video);
                }
            }
        }

        void HandleCustomContextMenuEntry(ContextMenuEntry currentEntry, Category aCategory, VideoInfo aVideo)
        {
            List<KeyValuePair<string, ContextMenuEntry>> dialogOptions = new List<KeyValuePair<string, ContextMenuEntry>>();
            while (true)
            {
                bool execute = currentEntry.Action == ContextMenuEntry.UIAction.Execute;

                if (currentEntry.Action == ContextMenuEntry.UIAction.GetText)
                {
                    SearchDialog dlg = new SearchDialog() { Owner = this };
                    dlg.tbxSearch.Text = currentEntry.UserInputText ?? "";
                    dlg.lblHeading.Text = currentEntry.DisplayText;
                    if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.tbxSearch.Text))
                    {
                        currentEntry.UserInputText = dlg.tbxSearch.Text;
                        execute = true;
                    }
                    else break;
                }
                if (currentEntry.Action == ContextMenuEntry.UIAction.ShowList)
                {
                    PlaybackChoices dlg = new PlaybackChoices() { Owner = this };
                    dlg.lblHeading.Content = currentEntry.DisplayText;
                    dialogOptions.Clear();
                    foreach (var subEntry in currentEntry.SubEntries)
                    {
                        dialogOptions.Add(new KeyValuePair<string, ContextMenuEntry>(subEntry.DisplayText, subEntry));
                    }
                    dlg.lvChoices.ItemsSource = dialogOptions.Select(dO => dO.Key).ToList();
                    dlg.lvChoices.SelectedIndex = 0;
                    if (dlg.ShowDialog() != true)
                        break;
                    else
                        currentEntry = dialogOptions[dlg.lvChoices.SelectedIndex].Value;
                }
                if (execute)
                {
                    waitCursor.Visibility = System.Windows.Visibility.Visible;
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                        delegate()
                        {
                            return SelectedSite.ExecuteContextMenuEntry(aCategory, aVideo, currentEntry);
                        },
                        delegate(Gui2UtilConnector.ResultInfo resultInfo)
                        {
                            waitCursor.Visibility = System.Windows.Visibility.Hidden;
                            if (ReactToResult(resultInfo, currentEntry.DisplayText))
                            {
                                if (resultInfo.ResultObject is ContextMenuExecutionResult)
                                {
                                    var cmer = resultInfo.ResultObject as ContextMenuExecutionResult;
                                    if (!string.IsNullOrEmpty(cmer.ExecutionResultMessage))
                                    {
                                        notification.Show(currentEntry.DisplayText, cmer.ExecutionResultMessage);
                                    }
                                    if (cmer.RefreshCurrentItems)
                                    {
                                        CategorySelected(SelectedCategory);
                                    }
                                    if (cmer.ResultItems != null && cmer.ResultItems.Count > 0)
                                    {
                                        DisplaySearchResultItems(currentEntry.DisplayText, cmer.ResultItems);
                                    }
                                }
                            }
                        });
                    break;
                }
            }
        }

		private void Play_Step1(PlayListItem playItem, bool goFullScreen)
		{
			if (!string.IsNullOrEmpty(playItem.FileName))
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
				{
					return SelectedSite.GetPlaylistItemVideoUrl(playItem.Video, CurrentPlayList[0].ChosenPlaybackOption, CurrentPlayList.IsPlayAll);
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
						Play_Step2(playItem, new List<String>() { resultInfo.ResultObject as string }, goFullScreen);
					else if (CurrentPlayList != null && CurrentPlayList.Count > 1) 
						PlayNextPlaylistItem();
				}
				, true);
			}
			else
			{
                Log.Info("Going to play: '{0}'", playItem.Video.Title);
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
				{
					return SelectedSite.GetMultipleVideoUrls(playItem.Video, CurrentPlayList != null && CurrentPlayList.Count > 1);
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
						Play_Step2(playItem, resultInfo.ResultObject as List<String>, goFullScreen);
					else if (CurrentPlayList != null && CurrentPlayList.Count > 1) 
						PlayNextPlaylistItem();
				}
				, true);
			}
		}

		private void Play_Step2(PlayListItem playItem, List<String> urlList, bool goFullScreen)
		{
			RemoveInvalidUrls(urlList);

			// if no valid urls were returned show error msg
			if (urlList == null || urlList.Count == 0) {
				notification.Show(Translation.Instance.Error, Translation.Instance.UnableToPlayVideo);
				return;
			}

			// create playlist entries if more than one url
			if (urlList.Count > 1)
			{
				PlayList playbackItems = new PlayList();
				foreach (string url in urlList)
				{
					VideoInfo vi = playItem.Video.CloneForPlaylist(url, url == urlList[0]);
					string url_new = url;
					if (url == urlList[0])
					{
						url_new = SelectedSite.GetPlaylistItemVideoUrl(vi, string.Empty, CurrentPlayList != null && CurrentPlayList.IsPlayAll);
					}
					playbackItems.Add(new PlayListItem(vi, playItem.Util)
					{
						FileName = url_new
					});
				}
				if (CurrentPlayList == null)
				{
					CurrentPlayList = playbackItems;
				}
				else
				{
					int currentPlaylistIndex = CurrentPlayListItem != null ? CurrentPlayList.IndexOf(CurrentPlayListItem) : 0;
					CurrentPlayList.InsertRange(currentPlaylistIndex, playbackItems);
				}
				// make the first item the current to be played now
				playItem = playbackItems[0];
				urlList = new List<string>(new string[] { playItem.FileName });
			}

			// play the first or only item
			string urlToPlay = urlList[0];
			if (playItem.Video.PlaybackOptions != null && playItem.Video.PlaybackOptions.Count > 0)
			{
				string choice = null;
				if (playItem.Video.PlaybackOptions.Count > 1)
				{
					PlaybackChoices dlg = new PlaybackChoices();
					dlg.Owner = this;
					dlg.lvChoices.ItemsSource = playItem.Video.PlaybackOptions.Keys;
					var preSelectedItem = playItem.Video.PlaybackOptions.FirstOrDefault(kvp => kvp.Value == urlToPlay);
					if (!string.IsNullOrEmpty(preSelectedItem.Key)) dlg.lvChoices.SelectedValue = preSelectedItem.Key;
					if (dlg.lvChoices.SelectedIndex < 0) dlg.lvChoices.SelectedIndex = 0;
					if (dlg.ShowDialog() == true) choice = dlg.lvChoices.SelectedItem.ToString();
				}
				else
				{
					choice = playItem.Video.PlaybackOptions.Keys.First();
				}

				if (choice != null)
				{
					playItem.ChosenPlaybackOption = choice;
                    Log.Info("Chosen quality: '{0}'", choice);
					waitCursor.Visibility = System.Windows.Visibility.Visible;
					Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
					{
						return playItem.Video.GetPlaybackOptionUrl(choice);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
						{
							Play_Step3(playItem, resultInfo.ResultObject as string, goFullScreen);
						}
					}, true);
				}
			}
			else
			{
				Play_Step3(playItem, urlToPlay, goFullScreen);
			}
		}

		void Play_Step3(PlayListItem playItem, string urlToPlay, bool goFullScreen)
		{
            // check for valid url and cut off additional parameter
            if (String.IsNullOrEmpty(urlToPlay) ||
				!UriUtils.IsValidUri((urlToPlay.IndexOf(OnlineVideos.MPUrlSourceFilter.SimpleUrl.ParameterSeparator) > 0) ? urlToPlay.Substring(0, urlToPlay.IndexOf(OnlineVideos.MPUrlSourceFilter.SimpleUrl.ParameterSeparator)) : urlToPlay))
			{
				notification.Show(Translation.Instance.Error, Translation.Instance.UnableToPlayVideo);
				return;
			}

            // decode and make an url valid for our filter
            var uri = new Uri(urlToPlay);
            string protocol = uri.Scheme.Substring(0, Math.Min(uri.Scheme.Length, 4));
            if (protocol == "http" ||protocol == "rtmp") uri = new Uri(UrlBuilder.GetFilterUrl(playItem.Util, urlToPlay));

			// Play
			CurrentPlayListItem = null;
            Log.Info("Starting Playback: '{0}'", uri);
			mediaPlayer.SubtitleFilePath = GetSubtitleFile(playItem);
            mediaPlayer.Source = uri;
			CurrentPlayListItem = playItem;
		}

		private static string GetSubtitleFile(PlayListItem playItem)
		{
			string subFile = null;

			Uri subtitleUri = null;
			bool validUri = !String.IsNullOrEmpty(playItem.Video.SubtitleUrl) && Uri.TryCreate(playItem.Video.SubtitleUrl, UriKind.Absolute, out subtitleUri);
			if (!string.IsNullOrEmpty(playItem.Video.SubtitleText) || (validUri && !subtitleUri.IsFile))
			{
				string subs = string.IsNullOrEmpty(playItem.Video.SubtitleText) ? WebCache.Instance.GetWebData(playItem.Video.SubtitleUrl) : playItem.Video.SubtitleText;
				if (!string.IsNullOrEmpty(subs))
				{
					subFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OnlineVideoSubtitles.txt");
					System.IO.File.WriteAllText(subFile, subs, System.Text.Encoding.UTF8);
				}
			}
			else
			{
				if (validUri && subtitleUri.IsFile)
					subFile = subtitleUri.AbsolutePath;
			}

			return subFile;
		}

		bool PlayNextPlaylistItem()
		{
			if (CurrentPlayList != null)
			{
				int currentPlaylistIndex = CurrentPlayListItem != null ? CurrentPlayList.IndexOf(CurrentPlayListItem) : 0;
				if (CurrentPlayList.Count > currentPlaylistIndex + 1)
				{
					// if playing a playlist item, move to the next            
					currentPlaylistIndex++;
					Play_Step1(CurrentPlayList[currentPlaylistIndex], IsFullScreen);
					return true;
				}
				else
				{
					// if last item -> clear the list
					CurrentPlayList = null;
					CurrentPlayListItem = null;
				}
			}
			return false;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveItems"></param>
        /// <param name="enque">null : download the next item in a DownloadList that is already in the Manager</param>
        private void SaveVideo_Step1(DownloadList saveItems, bool? enque = false)
        {
            if (enque != null) 
            {
                // when the DownloadManager already contains the current DownloadInfo of the given list - show already downloading message
                if (DownloadManager.Instance.Contains(saveItems.CurrentItem))
                {
                    notification.Show(Translation.Instance.Error, Translation.Instance.AlreadyDownloading);
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
                waitCursor.Visibility = System.Windows.Visibility.Visible;
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                    delegate()
                    {
                        return saveItems.CurrentItem.Util.GetPlaylistItemVideoUrl(saveItems.CurrentItem.VideoInfo, saveItems.ChosenPlaybackOption);
                    },
                    delegate(Gui2UtilConnector.ResultInfo resultInfo)
                    {
                        waitCursor.Visibility = System.Windows.Visibility.Hidden;
                        if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
                        {
                            SaveVideo_Step2(saveItems, new List<string>() { resultInfo.ResultObject as string }, enque);
                        }
                    }
                );
            }
            else
            {
                waitCursor.Visibility = System.Windows.Visibility.Visible;
                Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                    delegate()
                    {
                        return saveItems.CurrentItem.Util.GetMultipleVideoUrls(saveItems.CurrentItem.VideoInfo);
                    },
                    delegate(Gui2UtilConnector.ResultInfo resultInfo)
                    {
                        waitCursor.Visibility = System.Windows.Visibility.Hidden;
                        if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
                        {
                            SaveVideo_Step2(saveItems, resultInfo.ResultObject as List<String>, enque);
                        }
                    }
                );
            }
        }

        private void SaveVideo_Step2(DownloadList saveItems, List<String> loUrlList, bool? enque)
        {
            RemoveInvalidUrls(loUrlList);

            // if no valid urls were returned show error msg
            if (loUrlList == null || loUrlList.Count == 0)
            {
                notification.Show(Translation.Instance.Error, Translation.Instance.UnableToDownloadVideo);
                return;
            }

            // create download list if more than one url
            if (loUrlList.Count > 1)
            {
                saveItems.DownloadItems = new List<DownloadInfo>();
                foreach (string url in loUrlList)
                {
                    VideoInfo vi = saveItems.CurrentItem.VideoInfo.CloneForPlaylist(url, url == loUrlList[0]);
                    string url_new = url;
                    if (url == loUrlList[0])
                    {
                        url_new = saveItems.CurrentItem.Util.GetPlaylistItemVideoUrl(vi, string.Empty);
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
            string urlToSave = loUrlList[0];
            if (saveItems.CurrentItem.VideoInfo.PlaybackOptions != null && saveItems.CurrentItem.VideoInfo.PlaybackOptions.Count > 0)
            {
                string choice = null;
                if (saveItems.CurrentItem.VideoInfo.PlaybackOptions.Count > 1)
                {
                    PlaybackChoices dlg = new PlaybackChoices();
                    dlg.Owner = this;
                    dlg.lvChoices.ItemsSource = saveItems.CurrentItem.VideoInfo.PlaybackOptions.Keys;
                    var preSelectedItem = saveItems.CurrentItem.VideoInfo.PlaybackOptions.FirstOrDefault(kvp => kvp.Value == urlToSave);
                    if (!string.IsNullOrEmpty(preSelectedItem.Key)) dlg.lvChoices.SelectedValue = preSelectedItem.Key;
                    if (dlg.lvChoices.SelectedIndex < 0) dlg.lvChoices.SelectedIndex = 0;
                    if (dlg.ShowDialog() == true) choice = dlg.lvChoices.SelectedItem.ToString();
                }
                else
                {
                    choice = saveItems.CurrentItem.VideoInfo.PlaybackOptions.Keys.First();
                }

                if (choice != null)
                {
                    waitCursor.Visibility = System.Windows.Visibility.Visible;
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
                    {
                        return saveItems.CurrentItem.VideoInfo.GetPlaybackOptionUrl(choice);
                    },
                    delegate(Gui2UtilConnector.ResultInfo resultInfo)
                    {
                        waitCursor.Visibility = System.Windows.Visibility.Hidden;
                        if (ReactToResult(resultInfo, Translation.Instance.GettingPlaybackUrlsForVideo))
                        {
                            SaveVideo_Step3(saveItems, resultInfo.ResultObject as string, enque);
                        }
                    }, true);
                }
            }
            else
            {
                SaveVideo_Step3(saveItems, urlToSave, enque);
            }
        }

        private void SaveVideo_Step3(DownloadList saveItems, string url, bool? enque)
        {
            // check for valid url and cut off additional parameter
            if (String.IsNullOrEmpty(url) ||
                !UriUtils.IsValidUri((url.IndexOf(SimpleUrl.ParameterSeparator) > 0) ? url.Substring(0, url.IndexOf(SimpleUrl.ParameterSeparator)) : url))
            {
                notification.Show(Translation.Instance.Error, Translation.Instance.UnableToDownloadVideo);
                return;
            }

            saveItems.CurrentItem.Url = url;
            if (string.IsNullOrEmpty(saveItems.CurrentItem.Title)) saveItems.CurrentItem.Title = saveItems.CurrentItem.VideoInfo.Title;

            if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFolder))
            {
                if (!string.IsNullOrEmpty(saveItems.CurrentItem.OverrideFileName))
                    saveItems.CurrentItem.LocalFile = System.IO.Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.OverrideFileName);
                else
                    saveItems.CurrentItem.LocalFile = System.IO.Path.Combine(saveItems.CurrentItem.OverrideFolder, saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
            }
            else
            {
                saveItems.CurrentItem.LocalFile = System.IO.Path.Combine(System.IO.Path.Combine(OnlineVideoSettings.Instance.DownloadDir, saveItems.CurrentItem.Util.Settings.Name), saveItems.CurrentItem.Util.GetFileNameForDownload(saveItems.CurrentItem.VideoInfo, saveItems.CurrentItem.Category, url));
            }

            if (saveItems.DownloadItems != null && saveItems.DownloadItems.Count > 1)
            {
                saveItems.CurrentItem.LocalFile = string.Format(@"{0}\{1} - {2}#{3}{4}",
                    System.IO.Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
                    System.IO.Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile),
                    (saveItems.DownloadItems.IndexOf(saveItems.CurrentItem) + 1).ToString().PadLeft((saveItems.DownloadItems.Count).ToString().Length, '0'),
                    (saveItems.DownloadItems.Count).ToString(),
                    System.IO.Path.GetExtension(saveItems.CurrentItem.LocalFile));
            }

            saveItems.CurrentItem.LocalFile = FileUtils.GetNextFileName(saveItems.CurrentItem.LocalFile);
            saveItems.CurrentItem.ThumbFile = string.IsNullOrEmpty(saveItems.CurrentItem.VideoInfo.ThumbnailImage) ? saveItems.CurrentItem.VideoInfo.Thumb : saveItems.CurrentItem.VideoInfo.ThumbnailImage;

            // make sure the target dir exists
            if (!(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(saveItems.CurrentItem.LocalFile))))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(saveItems.CurrentItem.LocalFile));
            }

            if (enque == true)
                DownloadManager.Instance.Add(saveItems.CurrentItem.Util.Settings.Name, saveItems);
            else if (enque == false)
                DownloadManager.Instance.Add(null, saveItems);

            downloadNotifier.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
            downloadNotifier.GetBindingExpression(TextBlock.VisibilityProperty).UpdateTarget();

            System.Threading.Thread downloadThread = new System.Threading.Thread((System.Threading.ParameterizedThreadStart)delegate(object o)
            {
                DownloadList dlList = o as DownloadList;
                try
                {
                    IDownloader dlHelper = null;
                    if (dlList.CurrentItem.Url.ToLower().StartsWith("mms://")) dlHelper = new MMSDownloader();
                    else dlHelper = new OnlineVideos.MPUrlSourceFilter.Downloader();
                    dlList.CurrentItem.Downloader = dlHelper;
                    dlList.CurrentItem.Start = DateTime.Now;
                    Log.Info("Starting download of '{0}' to '{1}' from Site '{2}'", dlList.CurrentItem.Url, dlList.CurrentItem.LocalFile, dlList.CurrentItem.Util.Settings.Name);
                    Exception exception = dlHelper.Download(dlList.CurrentItem);
                    if (exception != null) Log.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, exception.Message);
                    OnDownloadFileCompleted(dlList, exception);
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // the thread was aborted on purpose, let it finish gracefully
                    System.Threading.Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    Log.Warn("Error downloading '{0}', Msg: {1}", dlList.CurrentItem.Url, ex.Message);
                    OnDownloadFileCompleted(dlList, ex);
                }
            });
            downloadThread.IsBackground = true;
            downloadThread.Name = "OVDownload";
            downloadThread.Start(saveItems);
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
                Log.Warn("Error on Util.OnDownloadEnded: {0}", ex.ToString());
            }

            bool preventMessageDuetoAdult = (saveItems.CurrentItem.Util != null && saveItems.CurrentItem.Util.Settings.ConfirmAge && OnlineVideoSettings.Instance.UseAgeConfirmation && !OnlineVideoSettings.Instance.AgeConfirmed);

            if (error != null && !saveItems.CurrentItem.Downloader.Cancelled)
            {
                if (!preventMessageDuetoAdult)
                {
                    notification.Show(Translation.Instance.Error, string.Format(Translation.Instance.DownloadFailed, saveItems.CurrentItem.Title));
                }
            }
            else
            {
                try
                {
                    // if the image given was an url -> check if thumb exists otherwise download
                    if (saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http"))
                    {
                        string thumbFile = FileUtils.GetThumbFile(saveItems.CurrentItem.ThumbFile);
                        if (System.IO.File.Exists(thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
                        else if (ImageDownloader.DownloadAndCheckImage(saveItems.CurrentItem.ThumbFile, thumbFile)) saveItems.CurrentItem.ThumbFile = thumbFile;
                    }
                    // save thumb for this video as well if it exists
                    if (!saveItems.CurrentItem.ThumbFile.ToLower().StartsWith("http") && System.IO.File.Exists(saveItems.CurrentItem.ThumbFile))
                    {
                        string localImageName = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(saveItems.CurrentItem.LocalFile),
                            System.IO.Path.GetFileNameWithoutExtension(saveItems.CurrentItem.LocalFile))
                            + System.IO.Path.GetExtension(saveItems.CurrentItem.ThumbFile);
                        System.IO.File.Copy(saveItems.CurrentItem.ThumbFile, localImageName, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Error saving thumbnail for download: {0}", ex.ToString());
                }

                // get file size
                int fileSize = saveItems.CurrentItem.KbTotal;
                if (fileSize <= 0)
                {
                    try { fileSize = (int)((new System.IO.FileInfo(saveItems.CurrentItem.LocalFile)).Length / 1024); }
                    catch { }
                }

                Log.Info("{3} download of '{0}' - {1} KB in {2}", saveItems.CurrentItem.LocalFile, fileSize, (DateTime.Now - saveItems.CurrentItem.Start).ToString(), saveItems.CurrentItem.Downloader.Cancelled ? "Cancelled" : "Finished");

                if (!preventMessageDuetoAdult)
                {
                    notification.Show(saveItems.CurrentItem.Downloader.Cancelled ? Translation.Instance.DownloadCancelled : Translation.Instance.DownloadComplete,
                        string.Format("{0}{1}", saveItems.CurrentItem.Title, fileSize > 0 ? " ( " + fileSize.ToString("n0") + " KB)" : ""));
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
                    SaveVideo_Step1(saveItems, null);
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

            Dispatcher.Invoke((Action)(() =>
            {
                downloadNotifier.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                downloadNotifier.GetBindingExpression(TextBlock.VisibilityProperty).UpdateTarget();
            }));

            if (!string.IsNullOrEmpty(site))
            {
                var continuationList = DownloadManager.Instance.GetNext(site);
                if (continuationList != null)
                {
                    SaveVideo_Step1(continuationList, null);
                }
            }
        }

        public void SelectAndFocusItem(int index = 0)
        {
            if (listViewMain.Items.Count > 0)
            {
                listViewMain.SelectedIndex = Math.Max(0, Math.Min(index, listViewMain.Items.Count-1));
                (listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();
            }
        }

        bool ReactToResult(Gui2UtilConnector.ResultInfo result, string taskDescription)
        {
            // show an error message if task was not completed successfully
            if (result.TaskSuccess != true)
            {
                if (result.TaskError != null)
                {
                    Log.Warn(string.Format("Error {0}: {1}", taskDescription, result.TaskError));
                    notification.Show(string.Format("{0} {1}", Translation.Instance.Error, taskDescription), result.TaskError.Message);
                }
                else
                {
                    if (!result.AbortedByUser)
                    {
                        notification.Show(result.TaskSuccess.HasValue ? Translation.Instance.Error : Translation.Instance.Timeout, taskDescription);
                    }
                }
                return false;
            }
            return true;
        }

		private void Search_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite != null && SelectedSite.CanSearch;
		}

		private void Search_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			SearchDialog dlg = new SearchDialog();
			dlg.Owner = this;
			if (dlg.ShowDialog() == true)
			{
				string search = dlg.tbxSearch.Text;
				if (search.Trim().Length > 0)
				{
					waitCursor.Visibility = System.Windows.Visibility.Visible;
					Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
						delegate()
						{
							return SelectedSite.Search(search.Trim());
						},
						delegate(Gui2UtilConnector.ResultInfo resultInfo)
						{
							waitCursor.Visibility = System.Windows.Visibility.Hidden;
							if (ReactToResult(resultInfo, Translation.Instance.GettingCategoryVideos))
							{
                                DisplaySearchResultItems(Translation.Instance.SearchResults + " [" + search + "]", resultInfo.ResultObject as List<SearchResultItem>);
							}
						}
					);
				}
			}
		}

        private void DisplaySearchResultItems(string title, List<SearchResultItem> result)
        {
            if (result.Count > 0)
            {
                if (result[0] is VideoInfo)
                {
                    SelectedCategory = new Category() { Name = title };
                    List<VideoInfo> videoList = result.ConvertAll(i => i as VideoInfo);
					listViewMain.ItemsSource = ViewModels.VideoList.GetVideosView(this, videoList, SelectedSite.HasNextPage);
					SelectAndFocusItem();
					ImageDownloader.GetImages<VideoInfo>(videoList);
                }
                else
                {
                    SelectedCategory = new Category()
                    {
                        Name = title,
                        HasSubCategories = true,
                        SubCategoriesDiscovered = true,
                    };
                    SelectedCategory.SubCategories = result.ConvertAll(i => { (i as Category).ParentCategory = SelectedCategory; return i as Category; });
					listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, SelectedCategory.SubCategories);
                    SelectAndFocusItem();
					ImageDownloader.GetImages<Category>(SelectedCategory.SubCategories);
                }
            }
        }

        private void Back_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && (SelectedSite != null || CurrentFilter != "");
        }

		private void Back_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (IsFullScreen)
			{
				ToggleFullscreen(sender, e);
			}
			else
			{
                if (CurrentFilter != "")
                {
                    FilterItems(char.MinValue);
                    return;
                }

                ImageDownloader.StopDownload = true;
				if (SelectedCategory == null)
				{
					listViewMain.ItemsSource = ViewModels.SiteList.GetSitesView(this, SelectedSite.Settings.Name);
					(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();
					SelectedSite = null;
				}
				else
				{
					if (detailsView.Visibility == System.Windows.Visibility.Visible)
					{
						listViewMain.Visibility = System.Windows.Visibility.Visible;
						detailsView.Visibility = System.Windows.Visibility.Hidden;
						(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();
					}
					else
					{
						waitCursor.Visibility = System.Windows.Visibility.Visible;

						if (SelectedCategory.ParentCategory == null)
						{
							Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
								delegate()
								{
                                    if (!SelectedSite.Settings.DynamicCategoriesDiscovered) SelectedSite.DiscoverDynamicCategories();
									return null;
								},
								delegate(Gui2UtilConnector.ResultInfo resultInfo)
								{
									waitCursor.Visibility = System.Windows.Visibility.Hidden;
									if (ReactToResult(resultInfo, Translation.Instance.GettingDynamicCategories))
									{
                                        SelectedSite.Settings.Categories.RaiseListChangedEvents = false;
										listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, SelectedSite.Settings.Categories, SelectedCategory.Name);
										if (listViewMain.SelectedIndex >= 0)
										{
											(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();
										}
										ImageDownloader.GetImages<Category>(SelectedSite.Settings.Categories);
									}
									SelectedCategory = null;
								}
							);
						}
						else
						{
							Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
								delegate()
								{
									if (!SelectedCategory.ParentCategory.SubCategoriesDiscovered) SelectedSite.DiscoverSubCategories(SelectedCategory.ParentCategory);
									return null;
								},
								delegate(Gui2UtilConnector.ResultInfo resultInfo)
								{
									waitCursor.Visibility = System.Windows.Visibility.Hidden;
									if (ReactToResult(resultInfo, Translation.Instance.GettingDynamicCategories))
									{
										listViewMain.ItemsSource = ViewModels.CategoryList.GetCategoriesView(this, SelectedCategory.ParentCategory.SubCategories, SelectedCategory.Name);
										(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();
										ImageDownloader.GetImages<Category>(SelectedCategory.ParentCategory.SubCategories);
									}
									SelectedCategory = SelectedCategory.ParentCategory;
								}
							);
						}
					}
				}
			}
		}

        private void PlayPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayer != null && mediaPlayer.Source != null && mediaPlayer.HasVideo;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayer != null && mediaPlayer.Source != null && mediaPlayer.HasVideo;
        }

        bool IsPaused;
        private void PlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsPaused)
            {
                mediaPlayer.Play();
                if (!OSD.IsMouseOver) OSD.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                mediaPlayer.Pause();
                if (IsFullScreen) OSD.Visibility = System.Windows.Visibility.Visible;
            }
            IsPaused = !IsPaused;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsFullScreen) ToggleFullscreen(sender, e);
            mediaPlayer.Close();
            mediaPlayer.Source = null;
            IsPaused = false;
			CurrentPlayList = null;
			CurrentPlayListItem = null;
        }

        private void listViewMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var o = (sender as ListView).ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]);
                if (o != null) o.SetValue(Panel.ZIndexProperty, 1000);
            }
            if (e.RemovedItems.Count > 0)
            {
                var o = (sender as ListView).ItemContainerGenerator.ContainerFromItem(e.RemovedItems[0]);
                if (o != null) o.SetValue(Panel.ZIndexProperty, 0);
            }
        }

        private void mediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!mediaPlayer.HasVideo) return;
            ToggleFullscreen(null, null);
        }

        IInputElement focusedElementBeforeFullscreen = null;
		private void ToggleFullscreen(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsFullScreen)
            {
                mediaPlayerBorder.Margin = new Thickness(0);
                mediaPlayerBorder.VerticalAlignment = VerticalAlignment.Stretch;
                mediaPlayerBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                mediaPlayerBorder.Width = double.NaN;
                mediaPlayerBorder.Height = double.NaN;
                mediaPlayerBorder.Background = new SolidColorBrush(Colors.Black);
                this.WindowStyle = WindowStyle.None;
                if (IsPaused) OSD.Visibility = System.Windows.Visibility.Visible;
                focusedElementBeforeFullscreen = Keyboard.FocusedElement;
                Keyboard.Focus(mediaPlayer); // set Keyboard focus to the mediaPlayer
            }
            else
            {
                mediaPlayerBorder.Margin = new Thickness(8);
                mediaPlayerBorder.VerticalAlignment = VerticalAlignment.Bottom;
                mediaPlayerBorder.HorizontalAlignment = HorizontalAlignment.Left;
                mediaPlayerBorder.Width = 184;
                mediaPlayerBorder.Height = 104;
                mediaPlayerBorder.Background = null;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                OSD.Visibility = System.Windows.Visibility.Hidden;
                Keyboard.Focus(focusedElementBeforeFullscreen); // set Keyboard focus back
            }
            IsFullScreen = !IsFullScreen;
        }

        private void mediaPlayer_MediaFailed(object sender, WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs e)
        {
            notification.Show(Translation.Instance.UnableToPlayVideo, e.Message);
            Dispatcher.Invoke((Action)(() => { Stop_Executed(sender, null); }));
        }

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
			if (!PlayNextPlaylistItem()) Stop_Executed(sender, null);
        }

		private void RemoveInvalidUrls(List<string> loUrlList)
		{
			// remove all invalid entries from the list of playback urls
			if (loUrlList != null)
			{
				int i = 0;
				while (i < loUrlList.Count)
				{
					if (String.IsNullOrEmpty(loUrlList[i]) || !UriUtils.IsValidUri(loUrlList[i]))
					{
						OnlineVideoSettings.Instance.Logger.Debug("removed invalid url {0}", loUrlList[i]);
						loUrlList.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
			}
		}

        private void SiteManager_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite == null;
        }

        private void SiteManager_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (listViewMain.ItemsSource as System.Windows.Data.ListCollectionView).Refresh();
            if (listViewMain.Visibility == System.Windows.Visibility.Visible)
            {
                listViewMain.Visibility = System.Windows.Visibility.Hidden;
                detailsView.Visibility = System.Windows.Visibility.Hidden;
                globalSitesView.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                listViewMain.Visibility = System.Windows.Visibility.Visible;
                detailsView.Visibility = System.Windows.Visibility.Hidden;
                globalSitesView.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void OSDMouseEnter(object sender, MouseEventArgs e)
        {
            if (IsFullScreen) OSD.Visibility = System.Windows.Visibility.Visible;
        }

        private void OSDMouseLeave(object sender, MouseEventArgs e)
        {
            if (OSD.Visibility == System.Windows.Visibility.Visible && !IsPaused) OSD.Visibility = System.Windows.Visibility.Hidden;
        }

        private void HelpExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Help dlg = new Help();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

		private void Window_Closed(object sender, EventArgs e)
		{
			Settings.Instance.Save();
		}

        double volumeToRestore = 0.0d;
        private void Mute_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (mediaPlayer.Volume > 0.0d)
            {
                volumeToRestore = mediaPlayer.Volume;
                mediaPlayer.Volume = 0.0d;
            }
            else
            {
                if (volumeToRestore <= 0.0d) volumeToRestore = 0.05d;
                mediaPlayer.Volume = volumeToRestore;
                volumeToRestore = 0.0d;
            }
        }

        private void VolumeUp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            double result = mediaPlayer.Volume + 0.05;
            result = Math.Min(result, 1.0d);
            if (result != mediaPlayer.Volume) mediaPlayer.Volume = result;
        }

        private void VolumeDown_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            double result = mediaPlayer.Volume - 0.05;
            result = Math.Max(result, 0.0d);
            if (result != mediaPlayer.Volume) mediaPlayer.Volume = result;
        }
    }
}