using System;
using MediaPortal.Utilities;
using OnlineVideos.Helpers;
using System.Threading;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal2.Player
{
    public class STAFormHelper
    {
        ManualResetEvent _readyEvent = new ManualResetEvent(false);
        private Form _form;
        private Thread _thread;

        public Form CreateSTAForm()
        {
            ThreadStart staThread = new ThreadStart(CreateSTAFormInternal);
            _thread = ThreadingUtils.RunSTAThreaded(staThread);
            _readyEvent.WaitOne();
            return _form;
        }

        private void CreateSTAFormInternal()
        {
            _form = new Form();
            _form.AutoScaleMode = AutoScaleMode.None;
            _form.TopMost = true;
            _form.Width = 400;
            _form.Height = 300;

            var webViewControl = WebViewHelper.Instance.GetWebViewForPlayer;
            webViewControl.Dock = DockStyle.Fill;
            _form.Controls.Add(webViewControl);

            webViewControl.Source = new Uri("https://www.npostart.nl/");

            _readyEvent.Set();

            Application.Run(_form);
        }
    }
}
