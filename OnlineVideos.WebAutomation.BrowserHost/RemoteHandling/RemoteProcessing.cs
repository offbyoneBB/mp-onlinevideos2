using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling.RemoteImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling
{
    /// <summary>
    /// Handle remote messgae processing 
    /// </summary>
    public class RemoteProcessing
    {
        public Action<string> ActionReceived;

        private static List<RemoteHandlingBase> _remoteHandlers = new List<RemoteHandlingBase> { new WebServiceRemoteHandling(new DebugLogger())};//, new MediaPortal1RemoteHandling(new DebugLogger()) };

        public void InitHandlers()
        {
            _remoteHandlers.ForEach(handler => {
                handler.Initialise();
                handler.ActionReceived += handler_ActionReceived;
            });
        }

        /// <summary>
        /// When an action is received from one of the handlers we'll raise the event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handler_ActionReceived(string action)
        {
            ActionReceived.Invoke(action);
        }

        /// <summary>        
        /// Some of the WNDProc messages we'll actually respond to - I've taken this list from the remote implementations
        /// I want to filter the messages to reduce the amount of processing overhead
        /// </summary>
        /// <param name="msg"></param>
        public bool ProcessWndProc(Message msg)
        {
            var result = false;

            if (msg.Msg == ProcessHelper.WM_APPCOMMAND || msg.Msg == ProcessHelper.WM_KEYDOWN || msg.Msg == ProcessHelper.WM_LBUTTONDOWN ||
                   msg.Msg == ProcessHelper.WM_RBUTTONDOWN || msg.Msg == ProcessHelper.WM_SYSKEYDOWN || msg.Msg == ProcessHelper.WM_COPYDATA)
            {
                _remoteHandlers.ForEach(handler => { result = result || handler.ProcessWndProc(msg); });
            }
            return result;
        }

        /// <summary>
        /// Process key press events
        /// </summary>
        /// <param name="keyPressed"></param>
        public void ProcessKeyPress(int keyPressed)
        {
            _remoteHandlers.ForEach(handler => handler.ProcessKeyPress(keyPressed));
        }
    }
}
