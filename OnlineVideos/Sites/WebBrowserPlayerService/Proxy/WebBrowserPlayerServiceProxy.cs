using GrpcDotNetNamedPipes;
using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using ProtoBuf.Grpc.Client;
using System;

namespace OnlineVideos.Sites.Proxy.WebBrowserPlayerService
{
    /// <summary>
    /// Proxy class for communicating with the browser host
    /// Use this on the client side of the communication
    /// </summary>
    public class WebBrowserPlayerServiceProxy : IDisposable
    {
        NamedPipeChannel _channel;
        IWebBrowserPlayerService _service;

        /// <summary>
        /// 
        /// </summary>
        public WebBrowserPlayerServiceProxy()
        {
            _channel = new NamedPipeChannel(".", WebBrowserPlayerServiceHost.PIPE_ROOT + "WebBrowserPlayerService");
            _service = _channel.CreateGrpcService<IWebBrowserPlayerService>();
        }

        /// <summary>
        /// Send a new action message to the browser host
        /// </summary>
        /// <param name="action"></param>
        public void OnNewAction(string action)
        {
            try
            {
                _service.OnNewAction(new ActionRequest { Action = action });
            }
            catch (Exception)
            {
                // we'll just ignore timeouts for now
                Log.Error("WebBrowserPlayerServiceProxy, exception sending " + action + " action to server");
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
        }
    }
}
