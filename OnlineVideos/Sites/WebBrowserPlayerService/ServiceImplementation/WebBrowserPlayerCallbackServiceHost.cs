using GrpcDotNetNamedPipes;
using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceBindings;
using System;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerCallbackService - construct this to get the server part of the callback service running
    /// </summary>
    public class WebBrowserPlayerCallbackServiceHost : IDisposable
    {
        NamedPipeServer _server;

        public WebBrowserPlayerCallbackServiceHost()
        {
            _server = new NamedPipeServer(WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService");
            _server.Bind<IWebBrowserPlayerCallbackService>(new WebBrowserPlayerCallbackService());
            _server.Start();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
