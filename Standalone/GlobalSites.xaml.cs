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
using System.ComponentModel;
using OnlineVideos;
using System.Windows.Threading;

namespace Standalone
{
    /// <summary>
    /// Interaktionslogik für GlobalSites.xaml
    /// </summary>
    public partial class GlobalSites : UserControl
    {
        public GlobalSites()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
            descriptor.AddValueChanged(this, new EventHandler(VisibilityChanged));           
        }
        
        bool changedXml = false;
        bool newDlls = false;
        int rememberedIndex = -1;

        void VisibilityChanged(object sender, EventArgs e)
        {
            if (Visibility == System.Windows.Visibility.Hidden)
            {
                if (changedXml)
                {
                    (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.ItemsSource = null;
                    if (newDlls)
                    {
                        OnlineVideoSettings.Reload();
                        TranslationLoader.SetTranslationsToSingleton();
                        GC.Collect();
                        GC.WaitForFullGCComplete();
                        newDlls = false;
                    }
                    OnlineVideoSettings.Instance.BuildSiteUtilsList();
                    (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.ItemsSource = ViewModels.SiteList.GetSitesView(App.Current.MainWindow as OnlineVideosMainWindow);
                    changedXml = false;
                }
                (App.Current.MainWindow as OnlineVideosMainWindow).SelectAndFocusItem(rememberedIndex);
            }
            else if (Visibility == System.Windows.Visibility.Visible)
            {
                rememberedIndex = (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.SelectedIndex;
                lvSites.ItemsSource = OnlineVideos.Sites.Updater.OnlineSites;
                (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.SelectedIndex = -1;
            }
        }

        protected void HandleItemKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // add or remove
                OnlineVideos.OnlineVideosWebservice.Site onlineSite = (sender as ListViewItem).DataContext as OnlineVideos.OnlineVideosWebservice.Site;
                SiteSettings localSite = OnlineVideoSettings.Instance.SiteSettingsList.FirstOrDefault(i => i.Name == onlineSite.Name);
                if (localSite == null)
                    AddSite(sender, new RoutedEventArgs());
                else
                    RemoveSite(sender, new RoutedEventArgs());

                e.Handled = true;
            }
        }

        private void AddSite(object sender, RoutedEventArgs e)
        {
            OnlineVideos.OnlineVideosWebservice.Site site = (sender as FrameworkElement).DataContext as OnlineVideos.OnlineVideosWebservice.Site;
			bool? result = OnlineVideos.Sites.Updater.UpdateSites(null, new List<OnlineVideos.OnlineVideosWebservice.Site> { site }, false, false);
			if (result != false)
			{
                RefreshList(site);
                if (result == null) newDlls = true;
			}
        }

        private void RemoveSite(object sender, RoutedEventArgs e)
        {
            OnlineVideos.OnlineVideosWebservice.Site site = (sender as FrameworkElement).DataContext as OnlineVideos.OnlineVideosWebservice.Site;
            int localSiteIndex = -1;
            for (int i = 0; i < OnlineVideoSettings.Instance.SiteSettingsList.Count; i++) 
                if (OnlineVideoSettings.Instance.SiteSettingsList[i].Name == site.Name) 
                {
                    localSiteIndex = i;
                    break;
                }
            if (localSiteIndex != -1)
            {
                OnlineVideoSettings.Instance.RemoveSiteAt(localSiteIndex);
                OnlineVideoSettings.Instance.SaveSites();
                RefreshList(site);
            }
        }

        private void RefreshList(OnlineVideos.OnlineVideosWebservice.Site site)
        {
            // refresh this list
            lvSites.ItemsSource = null;
            lvSites.ItemsSource = OnlineVideos.Sites.Updater.OnlineSites;
            Dispatcher.BeginInvoke((Action)(() => { (lvSites.ItemContainerGenerator.ContainerFromItem(site) as ListViewItem).Focus(); }), DispatcherPriority.Input);
            changedXml = true;
        }

    }
}
