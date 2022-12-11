using Moq;
using NUnit.Framework;
using OnlineVideos.Sites.Proxy.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using System.Windows.Forms;

namespace OnlineVideos.Tests
{
    [TestFixture]
    public class WebBrowserPlayerServiceTests
    {
        [Test]
        public void ShouldSendActionRequest()
        {
            WebBrowserPlayerServiceHost host = new WebBrowserPlayerServiceHost();
            WebBrowserPlayerServiceProxy client = new WebBrowserPlayerServiceProxy();

            string receivedAction = null;
            WebBrowserPlayerService.OnNewActionReceived += action => receivedAction = action;

            client.OnNewAction("action");

            Assert.AreEqual("action", receivedAction);
        }

        [Test]
        public void ShouldSendKeyPressRequest()
        {
            WebBrowserPlayerCallbackServiceHost callbackServiceHost = new WebBrowserPlayerCallbackServiceHost();
            WebBrowserPlayerCallback webBrowserPlayerCallback = new WebBrowserPlayerCallback();
            WebBrowserPlayerCallbackServiceProxy callbackServiceProxy = new WebBrowserPlayerCallbackServiceProxy(webBrowserPlayerCallback);

            int receivedKey = 0;
            webBrowserPlayerCallback.OnBrowserKeyPress += key => receivedKey = key;

            WebBrowserPlayerCallbackService.SendKeyPress(1);

            Assert.AreEqual(1, receivedKey);
        }

        [Test]
        public void ShouldSendWndProc()
        {
            WebBrowserPlayerCallbackServiceHost callbackServiceHost = new WebBrowserPlayerCallbackServiceHost();
            WebBrowserPlayerCallback webBrowserPlayerCallback = new WebBrowserPlayerCallback();
            WebBrowserPlayerCallbackServiceProxy callbackServiceProxy = new WebBrowserPlayerCallbackServiceProxy(webBrowserPlayerCallback);

            Message receivedMessage = default;
            webBrowserPlayerCallback.OnBrowserWndProc += msg =>
            {
                receivedMessage = msg;
                return true;
            };

            bool result = WebBrowserPlayerCallbackService.SendWndProc(Message.Create(new IntPtr(1), 2, new IntPtr(3), new IntPtr(4)));

            Assert.IsTrue(result);
            Assert.AreEqual(new IntPtr(1), receivedMessage.HWnd);
            Assert.AreEqual(2, receivedMessage.Msg);
            Assert.AreEqual(new IntPtr(3), receivedMessage.WParam);
            Assert.AreEqual(new IntPtr(4), receivedMessage.LParam);
        }

        [Test]
        public void ShouldLogException()
        {
            var mockLogger = new Mock<ILog>();
            OnlineVideoSettings.Instance.Logger = mockLogger.Object;

            WebBrowserPlayerCallbackServiceHost callbackServiceHost = new WebBrowserPlayerCallbackServiceHost();
            WebBrowserPlayerCallback webBrowserPlayerCallback = new WebBrowserPlayerCallback();
            WebBrowserPlayerCallbackServiceProxy callbackServiceProxy = new WebBrowserPlayerCallbackServiceProxy(webBrowserPlayerCallback);

            Exception exception = new ArgumentException("Test exception");
            WebBrowserPlayerCallbackService.LogError(exception);

            mockLogger.Verify(l => l.Error(exception.ToString()));
        }

        [Test]
        public void ShouldLogInfo()
        {
            var mockLogger = new Mock<ILog>();
            OnlineVideoSettings.Instance.Logger = mockLogger.Object;

            WebBrowserPlayerCallbackServiceHost callbackServiceHost = new WebBrowserPlayerCallbackServiceHost();
            WebBrowserPlayerCallback webBrowserPlayerCallback = new WebBrowserPlayerCallback();
            WebBrowserPlayerCallbackServiceProxy callbackServiceProxy = new WebBrowserPlayerCallbackServiceProxy(webBrowserPlayerCallback);

            string message = "Test message";
            WebBrowserPlayerCallbackService.LogInfo(message);

            mockLogger.Verify(l => l.Info(message));
        }
    }
}
