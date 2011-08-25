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
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        System.Timers.Timer timeClick = new System.Timers.Timer((int)GetDoubleClickTime()) { AutoReset = false };

        public event PropertyChangedEventHandler PropertyChanged;
        SiteUtilBase _SelectedSite;
        public SiteUtilBase SelectedSite { get { return _SelectedSite; } set { _SelectedSite = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedSite")); } }
        Category _SelectedCategory;
        public Category SelectedCategory { get { return _SelectedCategory; } set { _SelectedCategory = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedCategory")); } }

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

            waitCursor.Visibility = System.Windows.Visibility.Visible;
            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                delegate()
                {
                    SiteManager.AutomaticUpdate();
                    return null;
                },
                delegate(Gui2UtilConnector.ResultInfo resultInfo)
                {
                    waitCursor.Visibility = System.Windows.Visibility.Hidden;
                    ReactToResult(resultInfo, Translation.AutomaticUpdate);
                    OnlineVideoSettings.Instance.BuildSiteUtilsList();
                    listViewMain.ItemsSource = OnlineVideoSettings.Instance.SiteUtilsList;
                    SelectAndFocusFirstItem();
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
						SelectAndFocusFirstItem();
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
							SelectAndFocusFirstItem();
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
							SelectAndFocusFirstItem();
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
				Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
					delegate()
					{
						return SelectedSite.getMultipleVideoUrls(video);
					},
					delegate(Gui2UtilConnector.ResultInfo resultInfo)
					{
						waitCursor.Visibility = System.Windows.Visibility.Hidden;
						if (ReactToResult(resultInfo, Translation.GettingPlaybackUrlsForVideo))
						{
							List<string> urls = resultInfo.ResultObject as List<string>;
							if (urls != null && urls.Count > 0)
							{
								string url = urls[0];
								if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
								{
									if (video.PlaybackOptions.Count == 1) url = video.PlaybackOptions.Values.First();
									else
									{
										PlaybackChoices dlg = new PlaybackChoices();
										dlg.Owner = this;
										dlg.lvChoices.ItemsSource = video.PlaybackOptions.Keys;

										var preSelectedItem = video.PlaybackOptions.FirstOrDefault(kvp => kvp.Value == url);
										if (!string.IsNullOrEmpty(preSelectedItem.Key)) dlg.lvChoices.SelectedValue = preSelectedItem.Key;
										if (dlg.lvChoices.SelectedIndex < 0) dlg.lvChoices.SelectedIndex = 0;

										if (dlg.ShowDialog() == true)
										{
                                            url = video.GetPlaybackOptionUrl(dlg.lvChoices.SelectedItem.ToString());
										}
										else
										{
											return;
										}
									}
								}
								// translate rtmp urls to the local proxy
								if (new Uri(url).Scheme.ToLower().StartsWith("rtmp"))
								{
									url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
													string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}", System.Web.HttpUtility.UrlEncode(url)));
								}
								txtPlayTitle.Text = video.Title;
								mediaPlayer.Source = new Uri(url);
							}
						}
					}
				);
			}
		}

        private void SelectAndFocusFirstItem()
        {
            if (listViewMain.Items.Count > 0)
            {
                listViewMain.SelectedIndex = 0;
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
			e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite != null && !Gui2UtilConnector.Instance.IsBusy && SelectedSite.CanSearch;
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
										SelectAndFocusFirstItem();
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
										SelectAndFocusFirstItem();
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
            e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite != null && !Gui2UtilConnector.Instance.IsBusy;
        }

		private void Back_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (fullScreen)
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
            if (IsPaused) mediaPlayer.Play();
            else mediaPlayer.Pause();
            IsPaused = !IsPaused;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (fullScreen) ToggleFullscreen(sender, e);
            txtPlayTitle.Text = "";
            mediaPlayer.Close();
            mediaPlayer.Source = null;
            IsPaused = false;
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

        bool fullScreen = false;        
        private void mediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!mediaPlayer.HasVideo) return;
            if (!timeClick.Enabled)
            {
                timeClick.Enabled = true;
                return;
            }

            if (timeClick.Enabled)
            {
                ToggleFullscreen(null, null);
            }
        }

		private void ToggleFullscreen(object sender, ExecutedRoutedEventArgs e)
        {
            if (!fullScreen)
            {
                mediaPlayerBorder.Margin = new Thickness(0);
                mediaPlayerBorder.VerticalAlignment = VerticalAlignment.Stretch;
                mediaPlayerBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                mediaPlayerBorder.Width = double.NaN;
                mediaPlayerBorder.Height = double.NaN;
                mediaPlayerBorder.Background = new SolidColorBrush(Colors.Black);
                //this.WindowStyle = WindowStyle.None;
                //this.WindowState = WindowState.Maximized;
            }
            else
            {
                mediaPlayerBorder.Margin = new Thickness(8);
                mediaPlayerBorder.VerticalAlignment = VerticalAlignment.Bottom;
                mediaPlayerBorder.HorizontalAlignment = HorizontalAlignment.Left;
                mediaPlayerBorder.Width = 184;
                mediaPlayerBorder.Height = 104;
                mediaPlayerBorder.Background = null;
                //this.WindowStyle = WindowStyle.SingleBorderWindow;
                //this.WindowState = WindowState.Normal;
            }
            fullScreen = !fullScreen;
        }

        private void mediaPlayer_MediaFailed(object sender, WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs e)
        {
            MessageBox.Show(this, Translation.UnableToPlayVideo + ": " +e.Message, Translation.Error, MessageBoxButton.OK);
            Dispatcher.Invoke((Action)(() => { Stop_Executed(sender, null); }));
        }

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop_Executed(sender, null);
        }
    }
}
