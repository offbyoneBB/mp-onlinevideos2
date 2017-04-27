using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using System;
using System.ServiceModel;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerService - construct this to get the server part of the service running
    /// </summary>
    public class WebBrowserPlayerServiceHost : ServiceHost
    {
        public const string PIPE_ROOT = "net.pipe://localhost/MediaPortal/OnlineVideos/";

        public WebBrowserPlayerServiceHost()
            : base(typeof(WebBrowserPlayerService), new Uri[] { new Uri(PIPE_ROOT) })
        {
            var binding = new NetNamedPipeBinding
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
