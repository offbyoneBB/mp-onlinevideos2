using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling.RemoteImplementations
{
    /// <summary>
    /// Remote handling using the web service to pass messages back/forth 
    /// The actual remote transalation to actions will take place in the web service code, which will be the MP1 or MP2 specific OnlineVideos dll
    /// Currently, MediaPortal can't fully translate these messages until the RemotePlugins dll is updated, so I've left the passing of these messages to the services disabled for now
    /// </summary>
    public class WebServiceRemoteHandling : RemoteHandlingBase
    {
        private ServiceHost _service;
        private ServiceHost _callbackService;
        private bool _shouldSendEventsToService = true;
        /// <summary>
        /// CTor
        /// </summary>
        /// <param name="logger"></param>
        public WebServiceRemoteHandling(ILog logger, bool shouldSendEventsToService)
            : base(logger)
        {
            _shouldSendEventsToService = shouldSendEventsToService;
        }

        /// <summary>
        /// Connect to the web service and attach the action handler
        /// </summary>
        public override void Initialise()
        {
            _logger.Debug("Initialising services");
            _callbackService = new WebBrowserPlayerCallbackServiceHost();
            _service = new WebBrowserPlayerServiceHost();

            WebBrowserPlayerService.ServiceImplementation.WebBrowserPlayerService.OnNewActionReceived += OnNewActionFromClient;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override bool ProcessWndProc(Message msg)
        {
            if (!_shouldSendEventsToService) return false;
            _logger.Debug(string.Format("WebServiceRemoteHandling - WndProc message to be processed {0}, appCommand {1}, LParam {2}, WParam {3}", msg.Msg, ProcessHelper.GetLparamToAppCommand(msg.LParam), msg.LParam, msg.WParam));
            if (WebBrowserPlayerCallbackService.SendWndProc(msg))
                return true;

            return false;
        }
        
        /// <summary>
        /// Process key press events
        /// </summary>
        /// <param name="keyPressed"></param>
        public override void ProcessKeyPress(int keyPressed)
        {
            if (!_shouldSendEventsToService) return;
            _logger.Debug("WebServiceRemoteHandling - ProcessKeyPress {0} {1}", keyPressed, ((Keys)keyPressed).ToString());
            // Get the client implementation to translate the key press - this means we can truly detach the browser host from MediaPortal
            // The client handler for this event should fire the OnNewAction when the key has been translated
            WebBrowserPlayerCallbackService.SendKeyPress(keyPressed);
        }

    }
}
