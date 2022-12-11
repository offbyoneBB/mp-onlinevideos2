using System.Runtime.Serialization;
using System.ServiceModel;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// Service for messages being sent to the web browser player
    /// This is separate from the callback service because WCF does lots of blocking when calling back and sending in the same service
    /// </summary>
    [ServiceContract]
    public interface IWebBrowserPlayerService
    {
        [OperationContract]
        void OnNewAction(ActionRequest request);
    }

    [DataContract]
    public class ActionRequest
    {
        [DataMember(Order = 1)]
        public string Action { get; set; }
    }
}
