using System.Runtime.Serialization;
using System.ServiceModel;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// Callback methods from the web browser player service - in effect these are the messages which the browser host will send back to OV
    /// </summary>
    [ServiceContract]
    public interface IWebBrowserPlayerCallback
    {
        [OperationContract]
        void LogError(LogRequest request);
        [OperationContract]
        void LogInfo(LogRequest request);
        [OperationContract]
        void OnClosing(ClosingRequest request);
        [OperationContract]
        void OnKeyPress(KeyPressRequest request);
        [OperationContract]
        BoolResponse OnWndProc(WndProcRequest request);
    }

    [DataContract]
    public class LogRequest
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }

    [DataContract]
    public class ClosingRequest
    {
    }

    [DataContract]
    public class KeyPressRequest
    {
        [DataMember(Order = 1)]
        public int KeyPressed { get; set; }
    }

    [DataContract]
    public class WndProcRequest
    {
        [DataMember(Order = 1)]
        public long HWnd { get; set; }

        [DataMember(Order = 2)]
        public int Msg { get; set; }

        [DataMember(Order = 3)]
        public long WParam { get; set; }

        [DataMember(Order = 4)]
        public long LParam { get; set; }
    }
}
