using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerService - construct this to get the server part of the service running
    /// </summary>
    public class WebBrowserPlayerServiceHost: ServiceHost
    {

        public WebBrowserPlayerServiceHost()
            : base(typeof(WebBrowserPlayerService), new Uri[] { new Uri("net.pipe://localhost/") })
        {
            var binding = new NetNamedPipeBinding()
            {
                SendTimeout = TimeSpan.FromMilliseconds(100),
                ReceiveTimeout = TimeSpan.MaxValue
            };
            
            AddServiceEndpoint(typeof(IWebBrowserPlayerService), 
                binding, 
                "WebBrowserPlayerService");
            Open();
        }
    }
}
