using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Player service implementation - these are messages from the client (OV) to the server (BrowserHost)
    /// </summary>
    public class WebBrowserPlayerService :  IWebBrowserPlayerService
    {
        /// <summary>
        /// Let server applications know we have a new action
        /// </summary>
        public static event Action<string> OnNewActionReceived;

        /// <summary>
        /// New action has been received from the client
        /// </summary>
        /// <param name="request"></param>
        public void OnNewAction(ActionRequest request)
        {
            if (OnNewActionReceived != null) OnNewActionReceived.Invoke(request.Action);
        }
    }
}
