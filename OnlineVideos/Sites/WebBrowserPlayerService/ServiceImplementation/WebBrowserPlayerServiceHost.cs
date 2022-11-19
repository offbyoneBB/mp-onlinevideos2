using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using ServiceWire.NamedPipes;
using System;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerService - construct this to get the server part of the service running
    /// </summary>
    public class WebBrowserPlayerServiceHost : IDisposable
    {
        public const string PIPE_ROOT = "net.pipe://localhost/MediaPortal/OnlineVideos/";

        NpHost _npHost;

        public WebBrowserPlayerServiceHost()
        {
            _npHost = new NpHost(PIPE_ROOT + "WebBrowserPlayerService");
            _npHost.AddService<IWebBrowserPlayerService>(new WebBrowserPlayerService());
            _npHost.Open();
        }

        public void Dispose()
        {
            _npHost.Dispose();
        }
    }
}
