using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;

namespace OnlineVideos.Sites.Interfaces.WebBrowserPlayerService
{
    /// <summary>
    /// Callback methods from the web browser player service - in effect these are the messages which the browser host will send back to OV
    /// </summary>
    public interface IWebBrowserPlayerCallback
    {
        [OperationContract(IsOneWay = true)]
        void LogException(Exception exception);
        [OperationContract(IsOneWay = true)]
        void LogInfo(string message);
        [OperationContract(IsOneWay = true)]
        void OnClosing();
        [OperationContract(IsOneWay = true)]
        void OnKeyPress(int keyPressed);
        [OperationContract]
        bool OnWndProc(Message msg);
    }
}
