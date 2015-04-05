using OnlineVideos;
using OnlineVideos.Sites;
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
using System.Windows.Shapes;

namespace Standalone
{
    /// <summary>
    /// Interaktionslogik für PlaybackChoices.xaml
    /// </summary>
    public partial class WindowSiteSettings : Window
    {
        SiteUtilBase site;
        bool changes = false;

        public WindowSiteSettings()
        {
            InitializeComponent();
        }

        public WindowSiteSettings(SiteUtilBase site) : this()
        {
            this.site = site;
            GenerateOptions();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var item = lvChoices.ItemContainerGenerator.ContainerFromIndex(lvChoices.SelectedIndex) as ListBoxItem;
            if (item != null) { item.Focus(); }
        }

        void GenerateOptions(int selectedIndex = 0)
        {
            var fields = site.GetUserConfigurationProperties();

            // limit to what the UI can show
            fields = fields.Where(prop => (prop.IsEnum || prop.Namespace == "System")).ToList();

            if (fields.Count > 0)
            {
                lvChoices.ItemsSource = fields.Select(f => new { 
                    Name = f.DisplayName, 
                    Description = f.Description,
                    Value = f.IsPassword ? new string('*', site.GetConfigValueAsString(f).Length) : site.GetConfigValueAsString(f), 
                    Field = f }).ToList();
                lvChoices.SelectedIndex = selectedIndex;
                Dispatcher.BeginInvoke((Action<object,RoutedEventArgs>)OnLoaded, System.Windows.Threading.DispatcherPriority.Loaded, this, new RoutedEventArgs());
            }
            else
                Close(); // nothing to show
        }

        protected void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HandleItemAction();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = changes;
                Close();
                e.Handled = true;
            }
        }

        protected void HandleItemClicked(object sender, MouseButtonEventArgs e)
        {
            HandleItemAction();
            e.Handled = true;
        }

        protected void HandleItemMouseEnter(object sender, MouseEventArgs e)
        {
            lvChoices.SelectedItem = (sender as ListViewItem).DataContext;
            (sender as ListViewItem).Focus();
        }

		private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
		{
            DialogResult = changes;
			this.Close();
		}

        void HandleItemAction()
        {
            var prop = lvChoices.SelectedValue as OnlineVideos.Reflection.FieldPropertyDescriptorByRef;
            string value = site.GetConfigValueAsString(prop);
            if (prop.IsBool)
            {
                var dlgTrueFalse = new PlaybackChoices() { Owner = this };
                dlgTrueFalse.lblHeading.Content = string.Format("{0}: {1}", site.Settings.Name, prop.DisplayName);
                dlgTrueFalse.lvChoices.ItemsSource = new string[] { true.ToString(), false.ToString() };
                dlgTrueFalse.lvChoices.SelectedValue = value;
                if (dlgTrueFalse.ShowDialog() == true)
                {
                    if (value != dlgTrueFalse.lvChoices.SelectedValue.ToString())
                    {
                        site.SetConfigValueFromString(prop, dlgTrueFalse.lvChoices.SelectedValue.ToString());
                        changes = true;
                        GenerateOptions(lvChoices.SelectedIndex);
                    }
                }
            }
            else if (prop.IsEnum)
            {
                var dlgEnum = new PlaybackChoices() { Owner = this };
                dlgEnum.lblHeading.Content = string.Format("{0}: {1}", site.Settings.Name, prop.DisplayName);
                dlgEnum.lvChoices.ItemsSource = prop.GetEnumValues();
                dlgEnum.lvChoices.SelectedValue = value;
                if (dlgEnum.ShowDialog() == true)
                {
                    if (value != dlgEnum.lvChoices.SelectedValue.ToString())
                    {
                        site.SetConfigValueFromString(prop, dlgEnum.lvChoices.SelectedValue.ToString());
                        changes = true;
                        GenerateOptions(lvChoices.SelectedIndex);
                    }
                }
            }
            else
            {
                var dlgText = new SearchDialog() { Owner = this };
                dlgText.tbxSearch.Text = value ?? "";
                dlgText.lblHeading.Text = string.Format("{0}: {1}", site.Settings.Name, prop.DisplayName);
                if (prop.IsPassword)
                {
                    dlgText.tbxSearch.Visibility = System.Windows.Visibility.Collapsed;
                    dlgText.tbxPasswrd.Visibility = System.Windows.Visibility.Visible;
                    dlgText.tbxPasswrd.Focus();
                }
                if (dlgText.ShowDialog() == true)
                {
                    var newValue = (prop.IsPassword ? dlgText.tbxPasswrd.Password : dlgText.tbxSearch.Text);
                    if (value != newValue)
                    {
                        try
                        {
                            site.SetConfigValueFromString(prop, newValue);
                            changes = true;
                            GenerateOptions(lvChoices.SelectedIndex);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, Translation.Instance.Error);
                        }
                    }
                }
            }
        }
    }
}
