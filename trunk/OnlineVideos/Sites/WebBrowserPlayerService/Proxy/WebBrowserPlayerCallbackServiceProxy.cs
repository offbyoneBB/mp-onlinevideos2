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
    /// Proxy class for consuming messages from the browser host
    /// Use this on the client side of the communication
    /// </summary>
    public class WebBrowserPlayerCallbackServiceProxy : DuplexClientBase<IWebBrowserPlayerCallbackService>, IDisposable
    {
        /// <summary>
        /// Build the binding with no security - if we enable security the first duplex call times out
        /// </summary>
        /// <returns></returns>
        private static NetNamedPipeBinding GetBinding()
        {
            var binding = new NetNamedPipeBinding()
                {
                    SendTimeout = TimeSpan.FromMilliseconds(300), // We don't care about waiting for responses, so we'll ignore timeouts
                    ReceiveTimeout = TimeSpan.MaxValue // Basically this is the connection idle timeout
                };
            binding.Security.Mode = NetNamedPipeSecurityMode.None;
            return binding;
        }

        /// <summary>
        /// Constructor will automatically subscribe for listening to the service
        /// </summary>
        /// <param name="callback"></param>
        public WebBrowserPlayerCallbackServiceProxy(IWebBrowserPlayerCallback callback)
            : base(new InstanceContext(callback),
                    GetBinding(),
                    new EndpointAddress("net.pipe://localhost/WebBrowserPlayerCallbackService"))
        {
            Open();
            Channel.Subscribe();
        }
        
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            Channel.Unsubscribe();
            Close();
        }
    }
}
