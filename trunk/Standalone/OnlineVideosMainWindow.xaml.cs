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

namespace Standalone
{
    /// <summary>
    /// Interaktionslogik für OnlineVideosMainWindow.xaml
    /// </summary>
    public partial class OnlineVideosMainWindow : Window, INotifyPropertyChanged
    {     
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

        public OnlineVideosMainWindow()
        {
            InitializeComponent();
        }

        private string GetBaseDirectory()
        {
            try
            {
                // Attempt to create the Thumbs directory in the applications startup folder
                // This will raise an exception if the path is read only or do not have write access. 
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Thumbs"));
                return AppDomain.CurrentDomain.BaseDirectory;
            }
            catch (Exception)
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OnlineVideos\\");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // The default connection limit is 2 in .net on most platforms! This means downloading two file will block all other WebRequests.
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            string writeableBaseDir = GetBaseDirectory();
            Gui2UtilConnector.Instance.TaskFinishedCallback += () => Dispatcher.Invoke((Action)Gui2UtilConnector.Instance.ExecuteTaskResultHandler);

            OnlineVideoSettings.Instance.Logger = new Logger(writeableBaseDir);

            OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Team MediaPortal\MediaPortal\plugins\Windows\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.DllsDir)) OnlineVideoSettings.Instance.DllsDir = writeableBaseDir;

            OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\thumbs\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ThumbsDir)) OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(writeableBaseDir, "Thumbs");

            OnlineVideoSettings.Instance.ConfigDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ConfigDir)) OnlineVideoSettings.Instance.ConfigDir = writeableBaseDir;

            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asf")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asf", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asx")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asx", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".flv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".flv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".m4v")) OnlineVideoSettings.Instance.VideoExtensions.Add(".m4v", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mkv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mkv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mov")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mov", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mp4")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mp4", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".wmv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".wmv", false);

            // add a special reversed proxy handler for rtmp
            ReverseProxy.AddHandler(RTMP_LIB.RTMPRequestHandler.Instance);

            new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (o, ev) => txtPlayPos.Text = mediaPlayer != null && mediaPlayer.Source != null && mediaPlayer.HasVideo ? string.Format("{0} / {1}", new DateTime(mediaPlayer.MediaPosition).ToString("HH:mm:ss"), new DateTime(mediaPlayer.MediaDuration).ToString("HH:mm:ss")) : "",
                Dispatcher)
                .Start();

            OnlineVideoSettings.Instance.LoadSites();

            Title = "OnlineVideos - Checking for Updates ...";
            waitCursor.Visibility = System.Windows.Visibility.Visible;
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                delegate()
                {
                    SiteManager.AutomaticUpdate();
                    return null;
                },
                delegate(Gui2UtilConnector.ResultInfo resultInfo)
                {
                    Title = "OnlineVideos";
                    waitCursor.Visibility = System.Windows.Visibility.Hidden;
                    ReactToResult(resultInfo, Translation.AutomaticUpdate);
                    OnlineVideoSettings.Instance.BuildSiteUtilsList();
                    listViewMain.ItemsSource = OnlineVideoSettings.Instance.SiteUtilsList;
                    SelectAndFocusItem();
                }, false);
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
        }

        protected void HandleItemClicked(object sender, MouseButtonEventArgs e)
        {
            OnItemSelected(sender);
            e.Handled = true;
        }

        protected void OnItemSelected(object sender)
        {
            if (Gui2UtilConnector.Instance.IsBusy) return; // don't do anything if currently working on a background task

            object boundObject = ((ListViewItem)sender).Content;
            if (boundObject is KeyValuePair<string, OnlineVideos.Sites.SiteUtilBase>)
            {
				SiteUtilBase site = ((KeyValuePair<string, OnlineVideos.Sites.SiteUtilBase>)((ListViewItem)sender).Content).Value;
				SiteSelected(site);
            }
            else if (boundObject is Category)
            {
                Category cat = boundObject as Category;
				CategorySelected(cat);
            }
            else if (boundObject is VideoInfo)
            {
                VideoInfo video = boundObject as VideoInfo;
				VideoSelected(video);
            }
        }

		void SiteSelected(SiteUtilBase site)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;

			Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
				delegate()
				{
					if (!site.Settings.DynamicCategoriesDiscovered) site.DiscoverDynamicCategories();
					return null;
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.GettingDynamicCategories))
					{
						SelectedSite = site;
						listViewMain.ItemsSource = SelectedSite.Settings.Categories;
						SelectAndFocusItem();
						ImageDownloader.GetImages<Category>((IList<Category>)listViewMain.ItemsSource);
					}
				}
			);
		}

		void CategorySelected(Category category)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;

			if (category.HasSubCategories)
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
						if (ReactToResult(resultInfo, Translation.GettingDynamicCategories))
						{
							SelectedCategory = category;
							listViewMain.ItemsSource = category.SubCategories;
							SelectAndFocusItem();
							ImageDownloader.GetImages<Category>((IList<Category>)listViewMain.ItemsSource);
						}
					}
				);
			}
			else
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return SelectedSite.getVideoList(category);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.GettingCategoryVideos))
						{
							SelectedCategory = category;
							List<VideoInfo> result = resultInfo.ResultObject as List<VideoInfo>;
							result.ForEach(r => r.CleanDescriptionAndTitle());
							if (SelectedSite.HasNextPage) result.Add(new VideoInfo() { Title = Translation.NextPage, ImageUrl = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png") });
							listViewMain.ItemsSource = result;
							SelectAndFocusItem();
							ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listViewMain.ItemsSource);
						}
					}
				);
			}
		}

		void VideoSelected(VideoInfo video)
		{
			waitCursor.Visibility = System.Windows.Visibility.Visible;

			if (video.Title == Translation.NextPage)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return SelectedSite.getNextPageVideos();
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.GettingNextPageVideos))
						{
							List<VideoInfo> result = resultInfo.ResultObject as List<VideoInfo>;
							result.ForEach(r => r.CleanDescriptionAndTitle());
							if (SelectedSite.HasNextPage) result.Add(new VideoInfo() { Title = Translation.NextPage, ImageUrl = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png") });
							List<VideoInfo> currentSource = listViewMain.ItemsSource as List<VideoInfo>;
							int indexToSelect = currentSource.Count - 1;
							currentSource.RemoveAt(indexToSelect);
							result.InsertRange(0, currentSource);
							listViewMain.ItemsSource = result;
							(listViewMain.ItemContainerGenerator.ContainerFromIndex(indexToSelect) as ListBoxItem).Focus();
							ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listViewMain.ItemsSource);
						}
					}
				);
			}
			else if (SelectedSite is IChoice && video.HasDetails && detailsView.Visibility == System.Windows.Visibility.Hidden)
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return ((IChoice)SelectedSite).getVideoChoices(video);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.GettingVideoDetails))
						{
							listViewMain.Visibility = System.Windows.Visibility.Hidden;
							detailsView.DataContext = video;
							if (video.Other is IVideoDetails)
							{
								var extendedInfos = ((IVideoDetails)video.Other).GetExtendedProperties();
								if (extendedInfos.ContainsKey("Plot")) detailsView.txtSynopsis.Text = extendedInfos["Plot"];
								// todo : display all extended infos in details view
							}
							detailsView.listViewTrailers.ItemsSource = resultInfo.ResultObject as List<VideoInfo>;
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
				Play_Step1(new PlayListItem()
					{
						Video = video,
						Util = SelectedSite is OnlineVideos.Sites.FavoriteUtil ? OnlineVideoSettings.Instance.SiteUtilsList[video.SiteName] : SelectedSite
					}, false);
			}
		}

		private void Play_Step1(PlayListItem playItem, bool goFullScreen)
		{
			if (!string.IsNullOrEmpty(playItem.FileName))
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
				{
					return SelectedSite.getPlaylistItemUrl(playItem.Video, CurrentPlayList[0].ChosenPlaybackOption, CurrentPlayList.IsPlayAll);
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.GettingPlaybackUrlsForVideo))
						Play_Step2(playItem, new List<String>() { resultInfo.ResultObject as string }, goFullScreen);
					else if (CurrentPlayList != null && CurrentPlayList.Count > 1) 
						PlayNextPlaylistItem();
				}
				, true);
			}
			else
			{
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
				{
					return SelectedSite.getMultipleVideoUrls(playItem.Video, CurrentPlayList != null && CurrentPlayList.Count > 1);
				},
				delegate(Gui2UtilConnector.ResultInfo resultInfo)
				{
					waitCursor.Visibility = System.Windows.Visibility.Hidden;
					if (ReactToResult(resultInfo, Translation.GettingPlaybackUrlsForVideo))
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
				MessageBox.Show(this, Translation.Error, Translation.UnableToPlayVideo, MessageBoxButton.OK);
				return;
			}

			// create playlist entries if more than one url
			if (urlList.Count > 1)
			{
				PlayList playbackItems = new PlayList();
				foreach (string url in urlList)
				{
					VideoInfo vi = playItem.Video.CloneForPlayList(url, url == urlList[0]);
					string url_new = url;
					if (url == urlList[0])
					{
						url_new = SelectedSite.getPlaylistItemUrl(vi, string.Empty, CurrentPlayList != null && CurrentPlayList.IsPlayAll);
					}
					playbackItems.Add(new PlayListItem()
					{
						FileName = url_new, Video = vi, Util = playItem.Util
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
					waitCursor.Visibility = System.Windows.Visibility.Visible;
					Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(delegate()
					{
						return playItem.Video.GetPlaybackOptionUrl(choice);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.GettingPlaybackUrlsForVideo))
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
				!Utils.IsValidUri((urlToPlay.IndexOf("&&&&") > 0) ? urlToPlay.Substring(0, urlToPlay.IndexOf("&&&&")) : urlToPlay))
			{
				MessageBox.Show(this, Translation.Error, Translation.UnableToPlayVideo, MessageBoxButton.OK);
				return;
			}

			// translate rtmp urls to the local proxy
			if (new Uri(urlToPlay).Scheme.ToLower().StartsWith("rtmp"))
			{
				urlToPlay = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
								string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}", System.Web.HttpUtility.UrlEncode(urlToPlay)));
			}

			// Play
			CurrentPlayListItem = null;
			mediaPlayer.Source = new Uri(urlToPlay);
			CurrentPlayListItem = playItem;
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
                    MessageBox.Show(this, string.Format("{0} {1}", Translation.Error, taskDescription), result.TaskError.Message, MessageBoxButton.OK);
                }
                else
                {
                    if (!result.AbortedByUser)
                    {
                        string header = result.TaskSuccess.HasValue ? Translation.Error : Translation.Timeout;
                        MessageBox.Show(this, taskDescription, header, MessageBoxButton.OK);
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
							return SelectedSite.DoSearch(search.Trim());
						},
						delegate(Gui2UtilConnector.ResultInfo resultInfo)
						{
							waitCursor.Visibility = System.Windows.Visibility.Hidden;
							if (ReactToResult(resultInfo, Translation.GettingCategoryVideos))
							{
								List<ISearchResultItem> result = resultInfo.ResultObject as List<ISearchResultItem>;
								if (result.Count > 0)
								{
									if (result[0] is VideoInfo)
									{
										SelectedCategory = new Category() { Name = Translation.SearchResults + " [" + search + "]" };
										List<VideoInfo> converted = result.ConvertAll(i => i as VideoInfo);
										converted.ForEach(r => ((VideoInfo)r).CleanDescriptionAndTitle());
										if (SelectedSite.HasNextPage) converted.Add(new VideoInfo() { Title = Translation.NextPage, ImageUrl = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\NextPage.png") });
										listViewMain.ItemsSource = converted;
										SelectAndFocusItem();
										ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listViewMain.ItemsSource);
									}
									else
									{
										SelectedCategory = new Category()
										{
											Name = Translation.SearchResults + " [" + search + "]", HasSubCategories = true, SubCategoriesDiscovered = true,
										};
										SelectedCategory.SubCategories = result.ConvertAll(i => { (i as Category).ParentCategory = SelectedCategory; return i as Category; });
										listViewMain.ItemsSource = SelectedCategory.SubCategories;
										SelectAndFocusItem();
										ImageDownloader.GetImages<Category>((IList<Category>)listViewMain.ItemsSource);
									}
								}
							}
						}
					);
				}
			}
		}

        private void Back_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite != null;
        }

		private void Back_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (IsFullScreen)
			{
				ToggleFullscreen(sender, e);
			}
			else
			{
				if (SelectedCategory == null)
				{
					listViewMain.ItemsSource = OnlineVideoSettings.Instance.SiteUtilsList;
					listViewMain.SelectedValue = OnlineVideoSettings.Instance.SiteUtilsList.FirstOrDefault(o => o.Value == SelectedSite);
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
									if (ReactToResult(resultInfo, Translation.GettingDynamicCategories))
									{
										listViewMain.ItemsSource = SelectedSite.Settings.Categories;

										listViewMain.SelectedValue = SelectedSite.Settings.Categories.FirstOrDefault(o => o.Name == SelectedCategory.Name);
										(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();

										ImageDownloader.GetImages<Category>((IList<Category>)listViewMain.ItemsSource);
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
									if (ReactToResult(resultInfo, Translation.GettingDynamicCategories))
									{
										listViewMain.ItemsSource = SelectedCategory.ParentCategory.SubCategories;

										listViewMain.SelectedValue = SelectedCategory.ParentCategory.SubCategories.FirstOrDefault(o => o.Name == SelectedCategory.Name);
										(listViewMain.ItemContainerGenerator.ContainerFromIndex(listViewMain.SelectedIndex) as ListBoxItem).Focus();

										ImageDownloader.GetImages<Category>((IList<Category>)listViewMain.ItemsSource);
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
            MessageBox.Show(this, Translation.UnableToPlayVideo + ": " +e.Message, Translation.Error, MessageBoxButton.OK);
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
					if (String.IsNullOrEmpty(loUrlList[i]) || !Utils.IsValidUri(loUrlList[i]))
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
    }
}
