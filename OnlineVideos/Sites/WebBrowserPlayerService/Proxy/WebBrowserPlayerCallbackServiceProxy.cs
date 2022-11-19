using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using ServiceWire.NamedPipes;
using System;

namespace OnlineVideos.Sites.Proxy.WebBrowserPlayerService
{
    /// <summary>
    /// Proxy class for consuming messages from the browser host
    /// Use this on the client side of the communication
    /// </summary>
    public class WebBrowserPlayerCallbackServiceProxy : IDisposable
    {
        string _pipeName;
        NpHost _npHost;
        NpClient<IWebBrowserPlayerCallbackService> _npClient;

        /// <summary>
        /// Constructor will automatically subscribe for listening to the service
        /// </summary>
        /// <param name="callback"></param>
        public WebBrowserPlayerCallbackServiceProxy(IWebBrowserPlayerCallback callback)
        {
            _pipeName = WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService" + Guid.NewGuid();
            _npHost = new NpHost(_pipeName);
            _npHost.AddService(callback);
            _npHost.Open();

            _npClient = new NpClient<IWebBrowserPlayerCallbackService>(new NpEndPoint(WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService"));
            _npClient.Proxy.Subscribe(_npHost.PipeName);
        }
        
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            if (_npClient != null)
            {
                _npClient.Proxy.Unsubscribe(_pipeName);
                _npClient.Dispose();
                _npClient = null;
            }
            if (_npHost != null)
            {
                _npHost.Dispose();
                _npHost = null;
            }
        }
    }
}
