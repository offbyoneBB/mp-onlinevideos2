using OnlineVideos.Sites.Interfaces.WebBrowserPlayerService;
using ServiceWire.NamedPipes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation
{
    /// <summary>
    /// The callback service implementation - track subscribers and send the callback to these
    /// </summary>
    public class WebBrowserPlayerCallbackService : IWebBrowserPlayerCallbackService
    {
        private static readonly ConcurrentDictionary<string, NpClient<IWebBrowserPlayerCallback>> _subscribers = new ConcurrentDictionary<string, NpClient<IWebBrowserPlayerCallback>>();

        /// <summary>
        /// New client is subscribing to the callback service
        /// </summary>
        /// <returns></returns>
        public bool Subscribe(string endpoint)
        {
            try
            {
                _subscribers.GetOrAdd(endpoint, s => new NpClient<IWebBrowserPlayerCallback>(new NpEndPoint(endpoint)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Client is unsubscribing from the callback service
        /// </summary>
        /// <returns></returns>
        public bool Unsubscribe(string endpoint)
        {
            try
            {
                if (_subscribers.TryRemove(endpoint, out var callback))
                    callback.Dispose();
                return true;
            }
            catch
            {
                return false;
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
                    callback.OnClosing();
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
                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    callback.LogException(exceptionToLog);
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
                    callback.LogInfo(message);
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
                    callback.OnKeyPress(keyPressed);
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

                GetActiveCallbacks().ForEach(delegate(IWebBrowserPlayerCallback callback)
                {
                    result = result || callback.OnWndProc(msg);
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
            List<IWebBrowserPlayerCallback> callbacks = new List<IWebBrowserPlayerCallback>();
            try
            {
                foreach (var callback in _subscribers.ToArray())
                {
                    if (callback.Value.IsConnected)
                        callbacks.Add(callback.Value.Proxy);
                    else if (_subscribers.TryRemove(callback))
                        callback.Value.Dispose();

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return callbacks;
        }

    }
}
