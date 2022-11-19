using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using ServiceWire.NamedPipes;
using System;

namespace OnlineVideos.Sites.Proxy.WebBrowserPlayerService
{
    /// <summary>
    /// Proxy class for communicating with the browser host
    /// Use this on the client side of the communication
    /// </summary>
    public class WebBrowserPlayerServiceProxy : IDisposable
    {
        NpClient<IWebBrowserPlayerService> _npClient;

        /// <summary>
        /// 
        /// </summary>
        public WebBrowserPlayerServiceProxy()
        {
            _npClient = new NpClient<IWebBrowserPlayerService>(new NpEndPoint(WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerService"));
        }
        
        /// <summary>
        /// Send a new action message to the browser host
        /// </summary>
        /// <param name="action"></param>
        public void OnNewAction(string action)
        {
            if (_npClient.IsConnected)
            {
                try
                {
                    _npClient.Proxy.OnNewAction(action);
                }
                catch (Exception)
                {
                    // we'll just ignore timeouts for now
                    Log.Error("WebBrowserPlayerServiceProxy, exception sending " + action + " action to server");
                }
            }
            else
                Log.Error("WebBrowserPlayerServiceProxy, channel to server not opened");
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            _npClient.Dispose();
        }
    }
}
