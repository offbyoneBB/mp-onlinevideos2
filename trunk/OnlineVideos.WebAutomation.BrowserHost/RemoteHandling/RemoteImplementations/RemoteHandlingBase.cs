using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling.RemoteImplementations
{
    /// <summary>
    /// Base class for remote control processing
    /// </summary>
    public abstract class RemoteHandlingBase
    {        
        protected ILog _logger;

        public Action<string> ActionReceived;

        /// <summary>
        /// CTor
        /// </summary>
        /// <param name="logger"></param>
        public RemoteHandlingBase(ILog logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process WndProc messages from the browser form
        /// </summary>
        /// <param name="msg"></param>
        public abstract bool ProcessWndProc(Message msg);

        /// <summary>
        /// Process key press events
        /// </summary>
        /// <param name="keyPressed"></param>
        public abstract void ProcessKeyPress(int keyPressed);

        /// <summary>
        /// Initialise the handler
        /// </summary>
        public abstract void Initialise();
        
        /// <summary>
        /// Handle the event from the service when a client sends us a new action
        /// </summary>
        /// <param name="action"></param>
        protected void OnNewActionFromClient(string action)
        {
            _logger.Debug("Action received {0}", action);
            if (ActionReceived != null) ActionReceived.Invoke(action);
        }
    }
}
