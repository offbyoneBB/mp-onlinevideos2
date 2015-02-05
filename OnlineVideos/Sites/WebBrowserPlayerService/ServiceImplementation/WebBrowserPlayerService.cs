using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Player service implementation - these are messages from the client (OV) to the server (BrowserHost)
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, 
                    ConcurrencyMode = ConcurrencyMode.Multiple, 
                    UseSynchronizationContext = false)]
    public class WebBrowserPlayerService :  IWebBrowserPlayerService
    {
        /// <summary>
        /// Let server applications know we have a new action
        /// </summary>
        public static event Action<string> OnNewActionReceived;

        /// <summary>
        /// New action has been received from the client
        /// </summary>
        /// <param name="action"></param>
        public void OnNewAction(string action)
        {
            OnNewActionReceived.Invoke(action);
        }

    }
}
