using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using ServiceWire.NamedPipes;
using System;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// Service host for the WebBrowserPlayerCallbackService - construct this to get the server part of the callback service running
    /// </summary>
    public class WebBrowserPlayerCallbackServiceHost : IDisposable
    {
        NpHost _npHost;

        public WebBrowserPlayerCallbackServiceHost()
        {
            _npHost = new NpHost(WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService");
            _npHost.AddService<IWebBrowserPlayerCallbackService>(new WebBrowserPlayerCallbackService());
            _npHost.Open();
        }

        public void Dispose()
        {
            _npHost.Dispose();
        }
    }
}
