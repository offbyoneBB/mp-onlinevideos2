using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;

namespace OnlineVideos.Sites.Proxy.WebBrowserPlayerService
{
    /// <summary>
    /// Proxy class for communicating with the browser host
    /// Use this on the client side of the communication
    /// </summary>
    public class WebBrowserPlayerServiceProxy : ClientBase<IWebBrowserPlayerService>, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback"></param>
        public WebBrowserPlayerServiceProxy()
            : base(
                new NetNamedPipeBinding()
                {
                    SendTimeout = TimeSpan.FromMilliseconds(300), // We don't care about waiting for responses, so we'll ignore timeouts
                    ReceiveTimeout = TimeSpan.MaxValue // Basically this is the connection idle timeout
                }, 
                    new EndpointAddress("net.pipe://localhost/WebBrowserPlayerService"))
        {
            Open();
        }
        
        /// <summary>
        /// Send a new action message to the browser host
        /// </summary>
        /// <param name="action"></param>
        public void OnNewAction(string action)
        {
            if (State == CommunicationState.Opened)
            {
                try
                {
                    Channel.OnNewAction(action);
                }
                catch (TimeoutException)
                {
                    // we'll just ignore timeouts for now
                    Log.Error("WebBrowserPlayerServiceProxy, timeout sending " + action + " action to server");
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
            Close();
        }
    }
}
