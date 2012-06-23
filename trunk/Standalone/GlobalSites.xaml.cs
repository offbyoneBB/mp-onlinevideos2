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
                // deselect any site on the main view but remember the index
                rememberedIndex = (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.SelectedIndex;
                (App.Current.MainWindow as OnlineVideosMainWindow).listViewMain.SelectedIndex = -1;
                lvSites.ItemsSource = new ListCollectionView(OnlineVideos.Sites.Updater.OnlineSites)
                {
                    Filter = new Predicate<object>(cat => (((OnlineVideos.OnlineVideosWebservice.Site)cat).Name ?? "").ToLower().Contains((App.Current.MainWindow as OnlineVideosMainWindow).CurrentFilter))
                };
                // focus the first item when this list becomes visible /use dispatcher to let WPF create the items first)
                Dispatcher.BeginInvoke((Action)(()=>
                {
                    var itemToFocus = lvSites.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    if (itemToFocus != null) 
                        itemToFocus.Focus();
                }), DispatcherPriority.Loaded);
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
            else
            {
                char c = Util.GetCharFromKey(e.Key);
                if (char.IsLetterOrDigit(c))
                {
                    (App.Current.MainWindow as OnlineVideosMainWindow).FilterItems(c);
                    e.Handled = true;
                }
            }
        }

        private void AddSite(object sender, RoutedEventArgs e)
        {
            OnlineVideos.OnlineVideosWebservice.Site site = (sender as FrameworkElement).DataContext as OnlineVideos.OnlineVideosWebservice.Site;
			bool? result = OnlineVideos.Sites.Updater.UpdateSites(null, new List<OnlineVideos.OnlineVideosWebservice.Site> { site }, false, false);
			if (result != false)
			{
                RefreshList(site);
                changedXml = true;
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
                changedXml = true;
            }
        }

        internal void RefreshList(OnlineVideos.OnlineVideosWebservice.Site site)
        {
            var view = lvSites.ItemsSource as ListCollectionView;
            if (view != null) view.Refresh();
            Dispatcher.BeginInvoke((Action<OnlineVideos.OnlineVideosWebservice.Site>)((siteToSelect) =>
            {
                ListViewItem itemToFocus = null;
                if (siteToSelect != null)
                    itemToFocus = lvSites.ItemContainerGenerator.ContainerFromItem(site) as ListViewItem;
                else
                    itemToFocus = lvSites.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                if (itemToFocus != null) itemToFocus.Focus();
            }), DispatcherPriority.Input, site);
        }
    }
}
