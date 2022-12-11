using GrpcDotNetNamedPipes;
using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceBindings;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using ProtoBuf.Grpc.Client;
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
        NamedPipeServer _server;
        IWebBrowserPlayerCallbackService _callback;

        /// <summary>
        /// Constructor will automatically subscribe for listening to the service
        /// </summary>
        /// <param name="callback"></param>
        public WebBrowserPlayerCallbackServiceProxy(IWebBrowserPlayerCallback callback)
        {            
            _pipeName = WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService" + Guid.NewGuid();
            _server = new NamedPipeServer(_pipeName);
            _server.Bind(callback);
            _server.Start();

            NamedPipeChannel channel = new NamedPipeChannel(".", WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerCallbackService");
            _callback = channel.CreateGrpcService<IWebBrowserPlayerCallbackService>();
            _callback.Subscribe(new SubscribeRequest { Endpoint = _pipeName });
        }
        
        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
