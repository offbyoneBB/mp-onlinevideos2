using ServiceWire.NamedPipes;
using System.ServiceModel;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// The service contract for messages sent by the browser host
    /// </summary>
    [ServiceContract(SessionMode = SessionMode.Required, 
                        CallbackContract = typeof(IWebBrowserPlayerCallback)
                        )]
    public interface IWebBrowserPlayerCallbackService
    {

        [OperationContract]
        bool Subscribe(string endpoint);

        [OperationContract]
        bool Unsubscribe(string endpoint);

    }
}
