using System.Runtime.Serialization;
using System.ServiceModel;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// The service contract for messages sent by the browser host
    /// </summary>
    [ServiceContract]
    public interface IWebBrowserPlayerCallbackService
    {
        [OperationContract]
        BoolResponse Subscribe(SubscribeRequest request);

        [OperationContract]
        BoolResponse Unsubscribe(SubscribeRequest request);
    }

    [DataContract]
    public class SubscribeRequest
    {
        [DataMember(Order = 1)]
        public string Endpoint { get; set; }
    }

    [DataContract]
    public class BoolResponse
    {
        [DataMember(Order = 1)]
        public bool Result { get; set; }
    }
}
