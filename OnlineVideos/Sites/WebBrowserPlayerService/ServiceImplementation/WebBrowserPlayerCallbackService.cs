using GrpcDotNetNamedPipes;
using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using ProtoBuf.Grpc.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// The callback service implementation - track subscribers and send the callback to these
    /// </summary>
    public class WebBrowserPlayerCallbackService : IWebBrowserPlayerCallbackService
    {
        private static readonly ConcurrentDictionary<string, IWebBrowserPlayerCallback> _subscribers = new ConcurrentDictionary<string, IWebBrowserPlayerCallback>();

        /// <summary>
        /// New client is subscribing to the callback service
        /// </summary>
        /// <returns></returns>
        public BoolResponse Subscribe(SubscribeRequest request)
        {
            try
            {
                _subscribers.GetOrAdd(request.Endpoint, s => new NamedPipeChannel(".", request.Endpoint).CreateGrpcService<IWebBrowserPlayerCallback>());
                return new BoolResponse { Result = true };
            }
            catch
            {
                return new BoolResponse { Result = false };
            }
        }

        /// <summary>
        /// Client is unsubscribing from the callback service
        /// </summary>
        /// <returns></returns>
        public BoolResponse Unsubscribe(SubscribeRequest request)
        {
            try
            {
                _subscribers.TryRemove(request.Endpoint, out _);
                return new BoolResponse { Result = true };
            }
            catch
            {
                return new BoolResponse { Result = false };
            }
        }

        /// <summary>
        /// Let the client know we're closing
        /// </summary>
        public static void OnBrowserClosing()
        {
            try
            {
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    callback.OnClosing(new ClosingRequest());
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Request that the client logs an error
        /// </summary>
        /// <param name="exceptionToLog"></param>
        public static void LogError(Exception exceptionToLog)
        {
            try
            {
                string message = exceptionToLog.ToString();
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    callback.LogError(new LogRequest { Message = message });
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Request that the client logs an info message
        /// </summary>
        /// <param name="message"></param>
        public static void LogInfo(string message)
        {
            try
            {
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    callback.LogInfo(new LogRequest { Message = message });
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Send the key press to all active callback listeners
        /// </summary>
        /// <param name="keyPressed"></param>
        public static void SendKeyPress(int keyPressed)
        {
            try
            {
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    callback.OnKeyPress(new KeyPressRequest { KeyPressed = keyPressed });
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Send the wndproc to all active callback listeners, return true if any listener returned true
        /// </summary>
        /// <param name="msg"></param>
        public static bool SendWndProc(Message msg)
        {
            var result = false;
            try
            {
                WndProcRequest request = new WndProcRequest
                {
                    HWnd = msg.HWnd.ToInt64(),
                    Msg = msg.Msg,
                    WParam = msg.WParam.ToInt64(),
                    LParam = msg.LParam.ToInt64()
                };
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    result = result || callback.OnWndProc(request).Result;
                });

            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return result;
        }

        /// <summary>
        /// Maintain the list of active subscribers
        /// </summary>
        /// <returns></returns>
        private static List<IWebBrowserPlayerCallback> GetActiveCallbacks()
        {
            return new List<IWebBrowserPlayerCallback>(_subscribers.ToArray().Select(s => s.Value));
        }

    }
}
