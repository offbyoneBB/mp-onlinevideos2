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
    public partial class PlaybackChoices : Window
    {
        public PlaybackChoices()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var item = lvChoices.ItemContainerGenerator.ContainerFromIndex(lvChoices.SelectedIndex) as ListBoxItem;
            if (item != null) { item.Focus(); }
        }

        protected void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        protected void HandleItemClicked(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
            e.Handled = true;
        }

        protected void HandleItemMouseEnter(object sender, MouseEventArgs e)
        {
            lvChoices.SelectedItem = (sender as ListViewItem).DataContext;
            (sender as ListViewItem).Focus();
        }

		private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}
    }
}
