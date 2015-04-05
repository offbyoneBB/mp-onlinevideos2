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
using OnlineVideos.Downloading;

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
            ImageDownloader.StopDownload = true;
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
                var sitesView = ViewModels.GlobalSiteList.GetSitesView(App.Current.MainWindow as OnlineVideosMainWindow);
                lvSites.ItemsSource = sitesView;
                ImageDownloader.GetImages<ViewModels.GlobalSite>(sitesView.SourceCollection as IList<ViewModels.GlobalSite>);
                GridViewSort.ApplySort(lvSites.Items, "LastUpdated", lvSites, FindColumnHeader(lvSites, "LastUpdated"), ListSortDirection.Descending);
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
                OnlineVideos.OnlineVideosWebservice.Site onlineSite = ((sender as FrameworkElement).DataContext as ViewModels.GlobalSite).Model;
                var localSite = OnlineVideoSettings.Instance.SiteSettingsList.FirstOrDefault(i => i.Name == onlineSite.Name);
                if (localSite == null)
                    AddSite(sender, e);
                else
                    RemoveSite(sender, e);

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
            var globalSite = (sender as FrameworkElement).DataContext as ViewModels.GlobalSite;
            bool? result = OnlineVideos.Sites.Updater.UpdateSites(null, new List<OnlineVideos.OnlineVideosWebservice.Site> { globalSite.Model }, false, false);
			if (result != false)
			{
                RefreshList(globalSite);
                changedXml = true;
                if (result == null) newDlls = true;
			}
        }

        private void RemoveSite(object sender, RoutedEventArgs e)
        {
            var globalSite = (sender as FrameworkElement).DataContext as ViewModels.GlobalSite;
            int localSiteIndex = -1;
            for (int i = 0; i < OnlineVideoSettings.Instance.SiteSettingsList.Count; i++)
                if (OnlineVideoSettings.Instance.SiteSettingsList[i].Name == globalSite.Name) 
                {
                    localSiteIndex = i;
                    break;
                }
            if (localSiteIndex != -1)
            {
                OnlineVideoSettings.Instance.RemoveSiteAt(localSiteIndex);
                OnlineVideoSettings.Instance.SaveSites();
                RefreshList(globalSite);
                changedXml = true;
            }
        }

        internal void RefreshList(ViewModels.GlobalSite globalSite)
        {
            var view = lvSites.ItemsSource as ListCollectionView;
            if (view != null) view.Refresh();
            Dispatcher.BeginInvoke((Action<ViewModels.GlobalSite>)((siteToSelect) =>
            {
                ListViewItem itemToFocus = null;
                if (siteToSelect != null)
                    itemToFocus = lvSites.ItemContainerGenerator.ContainerFromItem(globalSite) as ListViewItem;
                else
                    itemToFocus = lvSites.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                if (itemToFocus != null) 
                    itemToFocus.Focus();
            }), DispatcherPriority.Input, globalSite);
        }

        #region helper methods
        private GridViewColumnHeader FindColumnHeader(Visual root, string gridViewSortProperty)
        {
            List<Visual> elementList = new List<Visual>();
            FindAllChildren(typeof(GridViewColumnHeader), root, elementList);
            foreach (Visual element in elementList)
            {
                GridViewColumnHeader header = element as GridViewColumnHeader;
                if (header.Column != null)
                {
                    if (header.Column.GetValue(GridViewSort.PropertyNameProperty) as string  == gridViewSortProperty)
                        return header;
                }
            }
            return null;
        }

        private void FindAllChildren(Type T, Visual root, List<Visual> elementList)
        {
            if (root == null)
                return;
            if (T.Equals(root.GetType()))
            {
                elementList.Add(root);
                return;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                Visual child = VisualTreeHelper.GetChild(root, i) as Visual;
                FindAllChildren(T, child, elementList);
            }
        }
        #endregion
    }
}
