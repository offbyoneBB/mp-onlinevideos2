using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.ServiceModel;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerCallbackService - construct this to get the server part of the callback service running
    /// </summary>
    public class WebBrowserPlayerCallbackServiceHost: ServiceHost
    {

        public WebBrowserPlayerCallbackServiceHost()
            : base(typeof(WebBrowserPlayerCallbackService), new Uri[] { new Uri(WebBrowserPlayerServiceHost.PIPE_ROOT) })
        {
            var binding = new NetNamedPipeBinding
            {
                SendTimeout = TimeSpan.FromMilliseconds(100),
                ReceiveTimeout = TimeSpan.MaxValue,
                Security = {Mode = NetNamedPipeSecurityMode.None}
            };
            AddServiceEndpoint(typeof(IWebBrowserPlayerCallbackService), 
                binding,
                "WebBrowserPlayerCallbackService");
            Open();
        }
    }
}
