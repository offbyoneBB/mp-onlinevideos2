using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// Service for messages being sent to the web browser player
    /// This is separate from the callback service because WCF does lots of blocking when calling back and sending in the same service
    /// </summary>
    [ServiceContract]
    public interface IWebBrowserPlayerService
    {
        [OperationContract(IsOneWay = true)]
        void OnNewAction(string action);
    }
}
