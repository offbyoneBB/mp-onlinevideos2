using Grpc.Core;
using GrpcDotNetNamedPipes;
using ProtoBuf.Grpc.Configuration;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceBindings
{
    public class NamedPipeServerBinder : ServerBinder
    {
        protected override bool TryBind<TService, TRequest, TResponse>(ServiceBindContext bindContext, Method<TRequest, TResponse> method, MethodStub<TService> stub)
        {
            var server = (NamedPipeServer)bindContext.State;
            var serviceBinder = server.ServiceBinder;
            switch (method.Type)
            {
                case MethodType.Unary:
                    serviceBinder.AddMethod(method, stub.CreateDelegate<UnaryServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.ClientStreaming:
                    serviceBinder.AddMethod(method, stub.CreateDelegate<ClientStreamingServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.ServerStreaming:
                    serviceBinder.AddMethod(method, stub.CreateDelegate<ServerStreamingServerMethod<TRequest, TResponse>>());
                    break;
                case MethodType.DuplexStreaming:
                    serviceBinder.AddMethod(method, stub.CreateDelegate<DuplexStreamingServerMethod<TRequest, TResponse>>());
                    break;
                default:
                    return false;
            }
            return true;
        }
    }

    public static class NamedPipeServerExtensions
    {
        public static void Bind<TService>(this NamedPipeServer namedPipeServer, TService service)
            where TService : class
        {            
            new NamedPipeServerBinder().Bind(namedPipeServer, BinderConfiguration.Default, service);
        }
    }
}
