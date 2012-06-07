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
    /// Interaktionslogik für Notification.xaml
    /// </summary>
    public partial class Notification : UserControl
    {
        System.Timers.Timer timer = new System.Timers.Timer();

        public Notification()
        {
            InitializeComponent();

            timer.Elapsed += timer_Elapsed;
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(Notification), new UIPropertyMetadata(false));

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (this)
            {
                timer.Stop();
                Dispatcher.Invoke((Action)(()=>IsActive = false));
            }
        }

        public void Show(string heading, string text, int timeout = 10)
        {
            lock (this)
            {
                if (Dispatcher.CheckAccess())
                    ShowInternal(heading, text, timeout);
                else
                    Dispatcher.Invoke((Action<string, string, int>)ShowInternal, heading, text, timeout);
            }
        }

        void ShowInternal(string heading, string text, int timeout)
        {
            if (IsActive)
            {
                timer.Stop();
                IsActive = false;
            }

            tbxHeading.Text = heading;
            tbxMessage.Text = text;
            if (timeout > 0)
            {
                timer.Interval = timeout * 1000;
                timer.Start();
            }
            IsActive = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lock (this)
            {
                timer.Stop();
                IsActive = false;
            }
        }
    }
}
