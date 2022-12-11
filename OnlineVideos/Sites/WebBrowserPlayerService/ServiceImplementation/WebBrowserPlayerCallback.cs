using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Class to handle the callbacks from the browser host
    /// </summary>
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
        /// <param name="request"></param>
        public void LogError(LogRequest request)
        {
            Log.Error(request.Message);
        }

        /// <summary>
        /// Browser host has requested an information message be logged
        /// </summary>
        /// <param name="request"></param>
        public void LogInfo(LogRequest request)
        {
            Log.Info(request.Message);
        }

        /// <summary>
        /// Browser host is closing
        /// </summary>
        /// <param name="request"></param>
        public void OnClosing(ClosingRequest request)
        {
            if (OnBrowserClosing != null) OnBrowserClosing.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Key was pressed in the browser
        /// </summary>
        /// <param name="request"></param>
        public void OnKeyPress(KeyPressRequest request)
        {
            if (OnBrowserKeyPress != null) OnBrowserKeyPress.Invoke(request.KeyPressed);
        }

        /// <summary>
        /// Handle WndProc so that we can process media key events
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public BoolResponse OnWndProc(WndProcRequest request)
        {
            bool result = OnBrowserWndProc != null && OnBrowserWndProc.Invoke(
                Message.Create(new IntPtr(request.HWnd), request.Msg, new IntPtr(request.WParam), new IntPtr(request.LParam))
                );
            return new BoolResponse { Result = result };
        }
    }
}
