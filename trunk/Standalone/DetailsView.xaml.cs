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

namespace Standalone
{
    /// <summary>
    /// Interaktionslogik für DetailsView.xaml
    /// </summary>
    public partial class DetailsView : UserControl
    {
        public DetailsView()
        {
            InitializeComponent();
        }

        public event Action<object> ItemSelected;

        protected void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ItemSelected(sender);
                e.Handled = true;
            }
        }

        protected void HandleItemClick(object sender, MouseButtonEventArgs e)
        {
            ItemSelected(sender);
            e.Handled = true;
        }
    }
}
