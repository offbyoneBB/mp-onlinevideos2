using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OnlineVideos;
using System.Windows.Threading;
using System.ComponentModel;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Gui2UtilConnector.Instance.TaskFinishedCallback += () => Dispatcher.Invoke((Action)Gui2UtilConnector.Instance.ExecuteTaskResultHandler);

            OnlineVideoSettings.Instance.DllsDir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Team MediaPortal\MediaPortal\plugins\Windows\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.DllsDir)) OnlineVideoSettings.Instance.DllsDir = AppDomain.CurrentDomain.BaseDirectory;

            OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\thumbs\OnlineVideos\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ThumbsDir)) OnlineVideoSettings.Instance.ThumbsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Thumbs");

            OnlineVideoSettings.Instance.ConfigDir = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MediaPortal\");
            if (!System.IO.Directory.Exists(OnlineVideoSettings.Instance.ConfigDir)) OnlineVideoSettings.Instance.ConfigDir = AppDomain.CurrentDomain.BaseDirectory;

            OnlineVideoSettings.Instance.LoadSites();
            OnlineVideoSettings.Instance.BuildSiteUtilsList();
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asf")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asf", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".asx")) OnlineVideoSettings.Instance.VideoExtensions.Add(".asx", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".flv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".flv", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".m4v")) OnlineVideoSettings.Instance.VideoExtensions.Add(".m4v", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mov")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mov", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".mp4")) OnlineVideoSettings.Instance.VideoExtensions.Add(".mp4", false);
            if (!OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(".wmv")) OnlineVideoSettings.Instance.VideoExtensions.Add(".wmv", false);
            listView1.ItemsSource = OnlineVideoSettings.Instance.SiteUtilsList;

            new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                (o, ev) => txtPlayPos.Text = mediaElem != null && mediaElem.Source != null && mediaElem.HasVideo ? string.Format("{0} / {1}", new DateTime(mediaElem.Position.Ticks).ToString("HH:mm:ss"), mediaElem.NaturalDuration.HasTimeSpan ? new DateTime(mediaElem.NaturalDuration.TimeSpan.Ticks).ToString("HH:mm:ss") : "00:00:00") : "",
                Dispatcher)
                .Start();
        }

        protected void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnItemSelected(sender);
                e.Handled = true;
            }
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
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
                SelectedSite = ((KeyValuePair<string, OnlineVideos.Sites.SiteUtilBase>)((ListViewItem)sender).Content).Value;
                waitCursor.Visibility = System.Windows.Visibility.Visible;
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
                            listView1.ItemsSource = SelectedSite.Settings.Categories;
                            ImageDownloader.GetImages<Category>((IList<Category>)listView1.ItemsSource);
                        }
                    }
                );
            }
            else if (boundObject is Category)
            {
                Category cat = boundObject as Category;

                waitCursor.Visibility = System.Windows.Visibility.Visible;

                if (cat.HasSubCategories)
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                        delegate()
                        {
                            if (!cat.SubCategoriesDiscovered) SelectedSite.DiscoverSubCategories(cat);
                            return null;
                        },
                        delegate(Gui2UtilConnector.ResultInfo resultInfo)
                        {
                            waitCursor.Visibility = System.Windows.Visibility.Hidden;
                            if (ReactToResult(resultInfo, Translation.GettingDynamicCategories))
                            {
                                listView1.ItemsSource = cat.SubCategories;
                                ImageDownloader.GetImages<Category>((IList<Category>)listView1.ItemsSource);
                            }
                        }
                    );
                }
                else
                {
                    Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                        delegate()
                        {
                            return SelectedSite.getVideoList(cat);
                        },
                        delegate(Gui2UtilConnector.ResultInfo resultInfo)
                        {
                            waitCursor.Visibility = System.Windows.Visibility.Hidden;
                            if (ReactToResult(resultInfo, Translation.GettingCategoryVideos))
                            {
                                List<VideoInfo> result = resultInfo.ResultObject as List<VideoInfo>;
                                result.ForEach(r => r.CleanDescriptionAndTitle());
                                listView1.ItemsSource = result;
                                ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listView1.ItemsSource);
                            }
                        }
                    );
                }
                SelectedCategory = cat;
            }
            else if (boundObject is VideoInfo)
            {
                VideoInfo video = boundObject as VideoInfo;

                waitCursor.Visibility = System.Windows.Visibility.Visible;

                if (SelectedSite is IChoice && detailsView.Visibility == System.Windows.Visibility.Hidden)
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
                                listView1.Visibility = System.Windows.Visibility.Hidden;
                                detailsView.DataContext = video;
                                if (video.Other is IVideoDetails)
                                {
                                    var extendedInfos = ((IVideoDetails)video.Other).GetExtendedProperties();
                                    detailsView.txtSynopsis.Text = extendedInfos["Synopsis"];
                                    // todo : display all
                                }
                                detailsView.listView.ItemsSource = resultInfo.ResultObject as List<VideoInfo>;
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
                                            if (dlg.ShowDialog() == true)
                                            {
                                                url = video.PlaybackOptions[dlg.lvChoices.SelectedItem.ToString()];
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
                                    mediaElem.Source = new Uri(url);
                                    mediaElem.Play();
                                }
                            }
                        }
                    );
                }
            }
        }

        bool ReactToResult(Gui2UtilConnector.ResultInfo result, string taskDescription)
        {
            // show an error message if task was not completed successfully
            if (result.TaskSuccess != true)
            {
                if (result.TaskError != null)
                {
                    MessageBox.Show(string.Format("{0} {1}", Translation.Error, taskDescription), result.TaskError.Message, MessageBoxButton.OK);
                }
                else
                {
                    if (!result.AbortedByUser)
                    {
                        string header = result.TaskSuccess.HasValue ? Translation.Error : Translation.Timeout;
                        MessageBox.Show(taskDescription, header, MessageBoxButton.OK);
                    }
                }
                return false;
            }
            return true;
        }

        private void Back_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedSite != null && !Gui2UtilConnector.Instance.IsBusy;
        }

        private void Back_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedCategory == null)
            {
                listView1.ItemsSource = OnlineVideoSettings.Instance.SiteUtilsList;
                SelectedSite = null;
            }
            else
            {
                if (detailsView.Visibility == System.Windows.Visibility.Visible)
                {
                    listView1.Visibility = System.Windows.Visibility.Visible;
                    detailsView.Visibility = System.Windows.Visibility.Hidden;
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
                                    listView1.ItemsSource = SelectedSite.Settings.Categories;
                                    ImageDownloader.GetImages<Category>((IList<Category>)listView1.ItemsSource);
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
                                    listView1.ItemsSource = SelectedCategory.ParentCategory.SubCategories;
                                    ImageDownloader.GetImages<Category>((IList<Category>)listView1.ItemsSource);
                                }
                                SelectedCategory = SelectedCategory.ParentCategory;
                            }
                        );
                    }
                }
            }
        }

        private void PreviousPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try { e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedCategory != null && !SelectedCategory.HasSubCategories && SelectedSite.HasPreviousPage; }
            catch { }
        }

        private void NextPage_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try { e.CanExecute = !Gui2UtilConnector.Instance.IsBusy && SelectedCategory != null && !SelectedCategory.HasSubCategories && SelectedSite.HasNextPage; }
            catch { }
        }

        private void PreviousPage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            waitCursor.Visibility = System.Windows.Visibility.Visible;

            Gui2UtilConnector.Instance.ExecuteInBackgroundAndCallback(
                delegate()
                {
                    return SelectedSite.getPreviousPageVideos();
                },
                delegate(Gui2UtilConnector.ResultInfo resultInfo)
                {
                    waitCursor.Visibility = System.Windows.Visibility.Hidden;
                    if (ReactToResult(resultInfo, Translation.GettingPreviousPageVideos))
                    {
                        List<VideoInfo> result = resultInfo.ResultObject as List<VideoInfo>;
                        result.ForEach(r => r.CleanDescriptionAndTitle());
                        listView1.ItemsSource = result;
                        ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listView1.ItemsSource);
                    }
                }
            );
        }

        private void NextPage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            waitCursor.Visibility = System.Windows.Visibility.Visible;

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
                        listView1.ItemsSource = result;
                        ImageDownloader.GetImages<VideoInfo>((IList<VideoInfo>)listView1.ItemsSource);
                    }
                }
            );
        }

        private void PlayPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaElem != null && mediaElem.Source != null && mediaElem.HasVideo;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaElem != null && mediaElem.Source != null && mediaElem.HasVideo;
        }

        bool IsPaused;
        private void PlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsPaused) mediaElem.Play();
            else mediaElem.Pause();
            IsPaused = !IsPaused;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (fullScreen) ToggleFullscreen();
            txtPlayTitle.Text = "";
            mediaElem.Close();
            mediaElem.Source = null;
            IsPaused = false;
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                (sender as ListView).ItemContainerGenerator.ContainerFromItem(e.AddedItems[0]).SetValue(Panel.ZIndexProperty, 1000);
            }
            if (e.RemovedItems.Count > 0)
            {
                var o = (sender as ListView).ItemContainerGenerator.ContainerFromItem(e.RemovedItems[0]);
                if (o != null) o.SetValue(Panel.ZIndexProperty, 0);
            }
        }

        private void mediaElem_BufferingStarted(object sender, RoutedEventArgs e)
        {
            // todo : show MediaElement.DownloadedProgress + ProgressBar for CurrentPlayPosition + Seeking + OSD in fullscreen?
            // todo : contextmenu, search, filter + all functionality from hidden menu in mepo
        }

        private void mediaElem_BufferingEnded(object sender, RoutedEventArgs e)
        {

        }

        private void mediaElem_MediaOpened(object sender, RoutedEventArgs e)
        {

        }

        private void mediaElem_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(Translation.UnableToPlayVideo, Translation.Error, MessageBoxButton.OK);
            Stop_Executed(sender, null);
        }

        private void mediaElem_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop_Executed(sender, null);
        }

        bool fullScreen = false;        
        private void mediaElem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!timeClick.Enabled)
            {
                timeClick.Enabled = true;
                return;
            }

            if (timeClick.Enabled)
            {
                ToggleFullscreen();
            }
        }

        private void ToggleFullscreen()
        {
            if (!fullScreen)
            {
                mediaElemBorder.Child = null;
                this.Background = new SolidColorBrush(Colors.Black);
                this.Content = mediaElem;
                //this.WindowStyle = WindowStyle.None;
                //this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.Content = LayoutRoot;
                mediaElemBorder.Child = mediaElem;
                this.Background = new SolidColorBrush(Colors.White);
                //this.WindowStyle = WindowStyle.SingleBorderWindow;
                //this.WindowState = WindowState.Normal;
            }
            fullScreen = !fullScreen;
        }

    }
}
