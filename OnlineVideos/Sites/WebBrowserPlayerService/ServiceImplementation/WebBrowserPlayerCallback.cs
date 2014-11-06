using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Class to handle the callbacks from the browser host
    /// </summary>
    [CallbackBehavior(UseSynchronizationContext = false)] 
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
            OnlineVideos.Log.Error(exception);
        }

        /// <summary>
        /// Browser host has requested an information message be logged
        /// </summary>
        /// <param name="message"></param>
        public void LogInfo(string message)
        {
            OnlineVideos.Log.Info(message);
        }

        /// <summary>
        /// Browser host is closing
        /// </summary>
        public void OnClosing()
        {
            OnBrowserClosing.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Key was pressed in the browser
        /// </summary>
        /// <param name="keyPressed"></param>
        public void OnKeyPress(int keyPressed)
        {
            OnBrowserKeyPress.Invoke(keyPressed);
        }

        /// <summary>
        /// Handle WndProc so that we can process media key events
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool OnWndProc(Message msg)
        {
            return OnBrowserWndProc.Invoke(msg);
        }
    }
}
