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
        bool Subscribe();

        [OperationContract]
        bool Unsubscribe();

    }
}
