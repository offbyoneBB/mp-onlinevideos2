using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.ServiceModel;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Class to handle the callbacks from the browser host
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)] 
    public class WebBrowserPlayerCallback : IWebBrowserPlayerCallback
    {
        /// <summary>
        /// Event triggered when the browser host lets us know that its closing
        /// </summary>
        public event EventHandler OnBrowserClosing;
        public event Action<int> OnBrowserKeyPress;
        public event Func<Message, bool> OnBrowserWndProc;

        /// <summary>
        /// Browser host has requested an exception to be logged
        /// </summary>
        /// <param name="exception"></param>
        public void LogException(Exception exception)
        {
            Log.Error(exception);
        }

        /// <summary>
        /// Browser host has requested an information message be logged
        /// </summary>
        /// <param name="message"></param>
        public void LogInfo(string message)
        {
            Log.Info(message);
        }

        /// <summary>
        /// Browser host is closing
        /// </summary>
        public void OnClosing()
        {
            if (OnBrowserClosing != null) OnBrowserClosing.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Key was pressed in the browser
        /// </summary>
        /// <param name="keyPressed"></param>
        public void OnKeyPress(int keyPressed)
        {
            if (OnBrowserKeyPress != null) OnBrowserKeyPress.Invoke(keyPressed);
        }

        /// <summary>
        /// Handle WndProc so that we can process media key events
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool OnWndProc(Message msg)
        {
            return OnBrowserWndProc != null && OnBrowserWndProc.Invoke(msg);
        }
    }
}
