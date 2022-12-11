using GrpcDotNetNamedPipes;
using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceBindings;
using System;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerService - construct this to get the server part of the service running
    /// </summary>
    public class WebBrowserPlayerServiceHost : IDisposable
    {
        public const string PIPE_ROOT = "net.pipe://localhost/MediaPortal/OnlineVideos/";

        NamedPipeServer _server;

        public WebBrowserPlayerServiceHost()
        {            
            _server = new NamedPipeServer(PIPE_ROOT + "WebBrowserPlayerService");
            _server.Bind<IWebBrowserPlayerService>(new WebBrowserPlayerService());
            _server.Start();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
